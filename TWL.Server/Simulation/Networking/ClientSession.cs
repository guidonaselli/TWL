using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Server.Security;
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
    private readonly PetManager _petManager;
    private readonly ServerQuestManager _questManager;
    private readonly CombatManager _combatManager;
    private readonly InteractionManager _interactionManager;
    private readonly EconomyManager _economyManager;
    private readonly PlayerService _playerService;
    private readonly NetworkStream _stream;
    private readonly RateLimiter _rateLimiter;

    public PlayerQuestComponent QuestComponent { get; protected set; }
    public ServerCharacter? Character { get; protected set; }

    public int UserId = -1; // se setea tras login

    protected ClientSession() { } // For testing

    public ClientSession(TcpClient client, DbService db, PetManager petManager, ServerQuestManager questManager, CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService, EconomyManager economyManager)
    {
        _client = client;
        _stream = client.GetStream();
        _dbService = db;
        _petManager = petManager;
        _questManager = questManager;
        _combatManager = combatManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
        _economyManager = economyManager;
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
            case Opcode.PurchaseGemsIntent:
                await HandlePurchaseGemsIntentAsync(msg.JsonPayload);
                break;
            case Opcode.PurchaseGemsVerify:
                await HandlePurchaseGemsVerifyAsync(msg.JsonPayload);
                break;
            case Opcode.BuyShopItemRequest:
                await HandleBuyShopItemAsync(msg.JsonPayload);
                break;
            // etc.
        }
    }

    private async Task HandlePurchaseGemsIntentAsync(string payload)
    {
        if (UserId <= 0) return;
        var request = JsonSerializer.Deserialize<PurchaseGemsIntentDTO>(payload, _jsonOptions);
        if (request == null || string.IsNullOrEmpty(request.ProductId)) return;

        var result = _economyManager.InitiatePurchase(UserId, request.ProductId);
        if (result == null) return;

        await SendAsync(new NetMessage { Op = Opcode.PurchaseGemsIntent, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandlePurchaseGemsVerifyAsync(string payload)
    {
        if (UserId <= 0 || Character == null) return;
        var request = JsonSerializer.Deserialize<PurchaseGemsVerifyDTO>(payload, _jsonOptions);
        if (request == null || string.IsNullOrEmpty(request.OrderId)) return;

        var result = _economyManager.VerifyPurchase(UserId, request.OrderId, request.ReceiptToken, Character);

        await SendAsync(new NetMessage { Op = Opcode.PurchaseGemsVerify, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandleBuyShopItemAsync(string payload)
    {
        if (UserId <= 0 || Character == null) return;
        var request = JsonSerializer.Deserialize<BuyShopItemDTO>(payload, _jsonOptions);
        if (request == null) return;

        var result = _economyManager.BuyShopItem(Character, request.ShopItemId, request.Quantity);

        await SendAsync(new NetMessage { Op = Opcode.BuyShopItemRequest, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
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
                        var petDef = _petManager.GetDefinition(def.Rewards.PetUnlockId.Value);
                        if (petDef != null)
                        {
                            var newPet = new ServerPet(petDef);
                            Character.AddPet(newPet);
                            itemsLog += $", Pet Unlock {def.Rewards.PetUnlockId.Value} ({petDef.Name})";
                        }
                        else
                        {
                            Console.WriteLine($"Error: Pet Definition {def.Rewards.PetUnlockId.Value} not found.");
                        }
                    }

                    if (def.Rewards.GrantSkillId.HasValue)
                    {
                        if (Character.LearnSkill(def.Rewards.GrantSkillId.Value))
                        {
                            itemsLog += $", Skill Unlock {def.Rewards.GrantSkillId.Value}";
                        }
                        else
                        {
                            itemsLog += $", Skill {def.Rewards.GrantSkillId.Value} (Already Known)";
                        }
                    }

                    Console.WriteLine($"Player {UserId} claimed quest {questId}, gained {def.Rewards.Exp} EXP, {def.Rewards.Gold} Gold{itemsLog}.");
                }
                await SendQuestUpdateAsync(questId);
            }
        }
    }

    private void GrantGoddessSkills()
    {
        if (Character == null) return;

        const string gsFlag = "GS_GRANTED";
        // Optimization: check flag first
        if (QuestComponent.Flags.Contains(gsFlag)) return;

        int skillId = 0;
        string skillName = "";

        switch (Character.CharacterElement)
        {
            case TWL.Shared.Domain.Characters.Element.Water:
                skillId = 2001; skillName = "Shrink"; break;
            case TWL.Shared.Domain.Characters.Element.Earth:
                skillId = 2002; skillName = "Blockage"; break;
            case TWL.Shared.Domain.Characters.Element.Fire:
                skillId = 2003; skillName = "Hotfire"; break;
            case TWL.Shared.Domain.Characters.Element.Wind:
                skillId = 2004; skillName = "Vanish"; break;
        }

        if (skillId > 0)
        {
            // If already known, just set flag
            if (Character.KnownSkills.Contains(skillId))
            {
                QuestComponent.Flags.Add(gsFlag);
                QuestComponent.IsDirty = true;
                return;
            }

            if (Character.LearnSkill(skillId))
            {
                QuestComponent.Flags.Add(gsFlag);
                QuestComponent.IsDirty = true;
                Console.WriteLine($"[GS] Granted {skillName} ({skillId}) to {Character.Name} ({Character.Id}).");
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
                QuestComponent.Character = Character;
                Console.WriteLine($"Restored session for {loginDto.Username} ({UserId})");
            }
            else
            {
                Character = new ServerCharacter { Id = uid, Name = loginDto.Username, Hp = 100 };
                QuestComponent.Character = Character;
            }

            _playerService.RegisterSession(this);

            GrantGoddessSkills();

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
