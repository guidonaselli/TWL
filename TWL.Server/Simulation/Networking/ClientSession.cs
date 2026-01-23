using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Net.Network;
using TWL.Server.Security;

namespace TWL.Server.Simulation.Networking;

public class ClientSession
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TcpClient _client;
    private readonly DbService _dbService;
    private readonly ServerQuestManager _questManager;
    private readonly CombatManager _combatManager;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerService _playerService;
    private readonly NetworkStream _stream;
    private readonly RateLimiter _rateLimiter;

    public PlayerQuestComponent QuestComponent { get; private set; }
    public ServerCharacter? Character { get; private set; }

    public int UserId = -1; // se setea tras login

    public ClientSession(TcpClient client, DbService db, ServerQuestManager questManager, CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService)
    {
        _client = client;
        _stream = client.GetStream();
        _dbService = db;
        _questManager = questManager;
        _combatManager = combatManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
        QuestComponent = new PlayerQuestComponent(questManager);
        _rateLimiter = new RateLimiter();
    }

    public void StartHandling()
    {
        // Fire and forget the async receive loop
        _ = ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;

                var netMsg = NetMessage.Deserialize(buffer, read);

                if (netMsg != null)
                {
                    await HandleMessageAsync(netMsg);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client disconnected: " + ex.Message);
        }
        finally
        {
            if (UserId > 0)
            {
                _playerService.SaveSession(this);
                _playerService.UnregisterSession(UserId);
            }
            _stream.Close();
            _client.Close();
        }
    }

    private async Task HandleMessageAsync(NetMessage msg)
    {
        if (msg == null) return;

        if (!_rateLimiter.Check(msg.Op))
        {
            SecurityLogger.LogSecurityEvent("RateLimitExceeded", UserId, $"Opcode: {msg.Op}");
            return;
        }

        switch (msg.Op)
        {
            case Opcode.LoginRequest:
                await HandleLoginAsync(msg.JsonPayload);
                break;
            case Opcode.MoveRequest:
                await HandleMoveAsync(msg.JsonPayload);
                break;
            case Opcode.StartQuestRequest:
                await HandleStartQuestAsync(msg.JsonPayload);
                break;
            case Opcode.ClaimRewardRequest:
                await HandleClaimRewardAsync(msg.JsonPayload);
                break;
            case Opcode.InteractRequest:
                await HandleInteractAsync(msg.JsonPayload);
                break;
            case Opcode.AttackRequest:
                await HandleAttackAsync(msg.JsonPayload);
                break;
            // etc.
        }
    }

    private async Task HandleAttackAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<UseSkillRequest>(payload, _jsonOptions);
        if (request == null) return;

        // Ensure the request comes from this player
        if (request.PlayerId != UserId && Character != null) request.PlayerId = Character.Id;

        var result = _combatManager.UseSkill(request);

        if (result != null)
        {
            // Broadcast result (in a real game, only to nearby players)
            var responseJson = JsonSerializer.Serialize(result, _jsonOptions);
            await SendAsync(new NetMessage
            {
                Op = Opcode.AttackBroadcast,
                JsonPayload = responseJson
            });

            // Check for death and quest progress
            if (result.NewTargetHp <= 0)
            {
                var target = _combatManager.GetCharacter(result.TargetId);
                if (target != null)
                {
                    // Update quest progress for "Kill" objective
                    var updated = QuestComponent.TryProgress("Kill", target.Name);
                    foreach (var questId in updated)
                    {
                        await SendQuestUpdateAsync(questId);
                    }
                }
            }
        }
    }

    private async Task HandleInteractAsync(string payload)
    {
        if (string.IsNullOrEmpty(payload) || payload.Length > 256) return;

        InteractDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<InteractDTO>(payload, _jsonOptions);
        }
        catch (JsonException) { return; }

        if (dto == null || string.IsNullOrWhiteSpace(dto.TargetName)) return;
        if (dto.TargetName.Length > 64)
        {
            SecurityLogger.LogSecurityEvent("InvalidInput", UserId, "TargetName too long");
            return;
        }

        // Process Interaction Rules (Give Items, Craft, etc.)
        bool interactionSuccess = false;
        if (Character != null)
        {
            interactionSuccess = _interactionManager.ProcessInteraction(Character, QuestComponent, dto.TargetName);
        }

        // Use a HashSet to avoid duplicates and multiple list allocations
        var uniqueUpdates = new HashSet<int>();

        // Try "Talk", "Collect", "Interact"
        QuestComponent.TryProgress(uniqueUpdates, dto.TargetName, "Talk", "Collect", "Interact");

        // If interaction was successful (e.g. Crafting done), try "Craft" objectives
        if (interactionSuccess)
        {
            QuestComponent.TryProgress(uniqueUpdates, dto.TargetName, "Craft");
        }

        foreach (var questId in uniqueUpdates)
        {
            await SendQuestUpdateAsync(questId);
        }
    }

    private async Task HandleStartQuestAsync(string payload)
    {
        if (int.TryParse(payload, out int questId) && questId > 0)
        {
            if (QuestComponent.StartQuest(questId))
            {
                await SendQuestUpdateAsync(questId);
            }
        }
    }

    private async Task HandleClaimRewardAsync(string payload)
    {
        if (int.TryParse(payload, out int questId) && questId > 0)
        {
            if (QuestComponent.ClaimReward(questId))
            {
                var def = _questManager.GetDefinition(questId);
                if (def != null && Character != null)
                {
                    Character.AddExp(def.Rewards.Exp);
                    Character.AddGold(def.Rewards.Gold);

                    var itemsLog = "";
                    if (def.Rewards.Items != null)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in def.Rewards.Items)
                        {
                            Character.AddItem(item.ItemId, item.Quantity);
                            sb.Append($", Item {item.ItemId} x{item.Quantity}");
                        }
                        itemsLog = sb.ToString();
                    }

                    if (def.Rewards.PetUnlockId.HasValue)
                    {
                        Character.AddPet(def.Rewards.PetUnlockId.Value);
                        itemsLog += $", Pet Unlock {def.Rewards.PetUnlockId.Value}";
                    }

                    Console.WriteLine($"Player {UserId} claimed quest {questId}, gained {def.Rewards.Exp} EXP, {def.Rewards.Gold} Gold{itemsLog}.");
                }
                await SendQuestUpdateAsync(questId);
            }
        }
    }

    private async Task SendQuestUpdateAsync(int questId)
    {
        var update = new QuestUpdate
        {
            QuestId = questId,
            State = QuestComponent.QuestStates.GetValueOrDefault(questId, QuestState.NotStarted),
            CurrentCounts = QuestComponent.QuestProgress.GetValueOrDefault(questId, new List<int>())
        };

        var json = JsonSerializer.Serialize(update, _jsonOptions);
        await SendAsync(new NetMessage
        {
            Op = Opcode.QuestUpdateBroadcast,
            JsonPayload = json
        });
    }

    private async Task HandleLoginAsync(string payload)
    {
        // payload podría ser {"username":"xxx","passHash":"abc"}
        if (string.IsNullOrEmpty(payload) || payload.Length > 512) return;

        LoginDTO? loginDto = null;
        try
        {
            loginDto = JsonSerializer.Deserialize<LoginDTO>(payload, _jsonOptions);
        }
        catch (JsonException) { return; }

        if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.PassHash)) return;

        // Using await here instead of .Result prevents thread pool starvation
        // and improves scalability under high load.
        var uid = await _dbService.CheckLoginAsync(loginDto.Username, loginDto.PassHash);
        if (uid < 0)
        {
            // login fallido
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = "{\"success\":false}"
            });
        }
        else
        {
            UserId = uid;

            var data = _playerService.LoadData(uid);
            if (data != null)
            {
                Character = new ServerCharacter();
                Character.LoadSaveData(data.Character);
                QuestComponent.LoadSaveData(data.Quests);
                Console.WriteLine($"Restored session for {loginDto.Username} ({UserId})");
            }
            else
            {
                Character = new ServerCharacter { Id = uid, Name = loginDto.Username, Hp = 100 };
            }

            _playerService.RegisterSession(this);

            // mandar una LoginResponse
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = "{\"success\":true,\"userId\":" + uid + "}"
            });
        }
    }

    private async Task HandleMoveAsync(string payload)
    {
        // EJ: {"dx":1,"dy":0}
        if (UserId < 0) return; // no logueado

        var moveDto = JsonSerializer.Deserialize<MoveDTO>(payload, _jsonOptions);

        if (moveDto == null) return;

        // Actualizar la pos en el server side:
        // PlayerData data = ...
        // data.X += moveDto.dx * speed
        // etc.
        // Broadcast a otros en la misma zona
        await Task.CompletedTask; // Placeholder for async broadcast
    }

    private async Task SendAsync(NetMessage msg)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
