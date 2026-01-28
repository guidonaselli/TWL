using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
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
    private readonly IEconomyService _economyManager;
    private readonly PlayerService _playerService;
    private readonly TWL.Server.Services.PetService _petService;
    private readonly IMediator _mediator;
    private readonly NetworkStream _stream;
    private readonly RateLimiter _rateLimiter;
    private readonly ServerMetrics _metrics;

    public PlayerQuestComponent QuestComponent { get; protected set; }
    public ServerCharacter? Character { get; protected set; }

    public int UserId = -1; // se setea tras login

    protected ClientSession() { } // For testing

    public ClientSession(TcpClient client, DbService db, PetManager petManager, ServerQuestManager questManager, CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService, IEconomyService economyManager, ServerMetrics metrics, TWL.Server.Services.PetService petService)
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
        _metrics = metrics;
        _petService = petService;
        QuestComponent = new PlayerQuestComponent(questManager, petManager);
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

                _metrics?.RecordNetBytesReceived(read);

                var netMsg = NetMessage.Deserialize(buffer, read);

                if (netMsg != null)
                {
                    _metrics?.RecordNetMessageProcessed();
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await HandleMessageAsync(netMsg);
                    sw.Stop();
                    _metrics?.RecordMessageProcessingTime(sw.ElapsedTicks);
                }
                else
                {
                    _metrics?.RecordValidationError();
                }
            }
        }
        catch (Exception ex)
        {
            _metrics?.RecordNetError();
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
            _metrics?.RecordValidationError();
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
            case Opcode.PetActionRequest:
                await HandlePetActionAsync(msg.JsonPayload);
                break;
            // etc.
        }
    }

    private async Task HandlePetActionAsync(string payload)
    {
        if (UserId <= 0) return;
        var request = JsonSerializer.Deserialize<PetActionRequest>(payload, _jsonOptions);
        if (request == null) return;

        bool success = false;
        switch (request.Action)
        {
            case PetActionType.Switch:
                success = _petService.SwitchPet(UserId, request.PetInstanceId);
                break;
            case PetActionType.Dismiss:
                success = _petService.DismissPet(UserId, request.PetInstanceId);
                break;
            // Utility etc.
        }

        await SendAsync(new NetMessage
        {
            Op = Opcode.PetActionResponse,
            JsonPayload = JsonSerializer.Serialize(new { success }, _jsonOptions)
        });
    }

    private async Task HandlePurchaseGemsIntentAsync(string payload)
    {
        if (UserId <= 0) return;
        var request = JsonSerializer.Deserialize<PurchaseGemsIntentDTO>(payload, _jsonOptions);

        // Validation
        if (request == null || string.IsNullOrEmpty(request.ProductId))
        {
             SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing ProductId");
             return;
        }
        if (request.ProductId.Length > 20 || !request.ProductId.StartsWith("gems_"))
        {
             SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, $"Invalid ProductId format: {request.ProductId}");
             return;
        }

        var result = _economyManager.InitiatePurchase(UserId, request.ProductId);
        if (result == null) return;

        await SendAsync(new NetMessage { Op = Opcode.PurchaseGemsIntent, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandlePurchaseGemsVerifyAsync(string payload)
    {
        if (UserId <= 0 || Character == null) return;
        var request = JsonSerializer.Deserialize<PurchaseGemsVerifyDTO>(payload, _jsonOptions);

        // Validation
        if (request == null || string.IsNullOrEmpty(request.OrderId))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing OrderId");
            return;
        }
        if (string.IsNullOrEmpty(request.ReceiptToken))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing ReceiptToken");
            return;
        }

        var result = _economyManager.VerifyPurchase(UserId, request.OrderId, request.ReceiptToken, Character);

        await SendAsync(new NetMessage { Op = Opcode.PurchaseGemsVerify, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandleBuyShopItemAsync(string payload)
    {
        if (UserId <= 0 || Character == null) return;
        var request = JsonSerializer.Deserialize<BuyShopItemDTO>(payload, _jsonOptions);
        if (request == null) return;

        // Hardening: Quantity Check
        if (request.Quantity <= 0 || request.Quantity > 999)
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, $"Invalid Shop Quantity: {request.Quantity}");
            return;
        }

        var result = _economyManager.BuyShopItem(Character, request.ShopItemId, request.Quantity);

        await SendAsync(new NetMessage { Op = Opcode.BuyShopItemRequest, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandleAttackAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<UseSkillRequest>(payload, _jsonOptions);
        if (request == null) return;

        // Ensure the request comes from this player
        if (request.PlayerId != UserId && Character != null) request.PlayerId = Character.Id;

        // Use Mediator
        var result = await _mediator.Send(new UseSkillCommand(request));

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
                var target = _combatManager.GetCombatant(result.TargetId);
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

        if (Character != null)
        {
             var result = await _mediator.Send(new InteractCommand(Character, QuestComponent, dto.TargetName));

             foreach (var questId in result.UpdatedQuestIds)
             {
                 await SendQuestUpdateAsync(questId);
             }
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
                Console.WriteLine($"Player {UserId} claimed quest {questId}.");
                await SendQuestUpdateAsync(questId);
            }
        }
    }

    protected virtual void GrantGoddessSkills()
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
                skillId = TWL.Shared.Constants.SkillIds.GS_WATER_DIMINUTION; skillName = "Diminution"; break;
            case TWL.Shared.Domain.Characters.Element.Earth:
                skillId = TWL.Shared.Constants.SkillIds.GS_EARTH_SUPPORT_SEAL; skillName = "Support Seal"; break;
            case TWL.Shared.Domain.Characters.Element.Fire:
                skillId = TWL.Shared.Constants.SkillIds.GS_FIRE_EMBER_SURGE; skillName = "Ember Surge"; break;
            case TWL.Shared.Domain.Characters.Element.Wind:
                skillId = TWL.Shared.Constants.SkillIds.GS_WIND_UNTOUCHABLE_VEIL; skillName = "Untouchable Veil"; break;
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

                // Hydrate Pets
                if (Character.Pets != null)
                {
                    foreach (var pet in Character.Pets)
                    {
                        var def = _petManager.GetDefinition(pet.DefinitionId);
                        if (def != null) pet.Hydrate(def);
                    }

                    // Register Active Pet
                    var activePet = Character.GetActivePet();
                    if (activePet != null)
                    {
                        activePet.Id = -Character.Id;
                        _combatManager.RegisterCombatant(activePet);
                    }
                }

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
        _metrics?.RecordNetBytesSent(bytes.Length);
    }

    public void HandleInstanceCompletion(string instanceName)
    {
        if (Character == null) return;

        // Use a HashSet to avoid duplicates
        var uniqueUpdates = new HashSet<int>();
        QuestComponent.TryProgress(uniqueUpdates, instanceName, "Instance", "InstanceComplete");

        // Fire and forget updates (in real scenario we'd await or queue this)
        foreach (var questId in uniqueUpdates)
        {
            _ = SendQuestUpdateAsync(questId);
        }
    }

    public void HandleInstanceFailure(string instanceId)
    {
        if (Character == null) return;

        var failedQuests = QuestComponent.HandleInstanceFailure(instanceId);
        foreach (var questId in failedQuests)
        {
            _ = SendQuestUpdateAsync(questId);
        }
    }
}
