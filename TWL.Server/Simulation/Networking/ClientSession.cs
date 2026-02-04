using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using TWL.Server.Architecture.Observability;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Constants;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;

namespace TWL.Server.Simulation.Networking;

public class ClientSession
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TcpClient _client;
    private readonly CombatManager _combatManager;
    private readonly DbService _dbService;
    private readonly IEconomyService _economyManager;
    private readonly InteractionManager _interactionManager;
    private readonly IMediator _mediator;
    private readonly ServerMetrics _metrics;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;
    private readonly ServerQuestManager _questManager;
    private readonly RateLimiter _rateLimiter;
    private readonly SpawnManager _spawnManager;
    private readonly NetworkStream _stream;
    private readonly IWorldTriggerService _worldTriggerService;

    public int UserId = -1; // se setea tras login

    protected ClientSession()
    {
    } // For testing

    public ClientSession(TcpClient client, DbService db, PetManager petManager, ServerQuestManager questManager,
        CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService,
        IEconomyService economyManager, ServerMetrics metrics, PetService petService, IMediator mediator,
        IWorldTriggerService worldTriggerService, SpawnManager spawnManager)
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
        _mediator = mediator;
        _worldTriggerService = worldTriggerService;
        _spawnManager = spawnManager;
        QuestComponent = new PlayerQuestComponent(questManager, petManager);
        _rateLimiter = new RateLimiter();

        if (_combatManager != null)
        {
            _combatManager.OnCombatantDeath += OnCombatantDeath;
        }
    }

    public PlayerQuestComponent QuestComponent { get; protected set; }
    public ServerCharacter? Character { get; protected set; }

    private void OnCombatantDeath(ServerCombatant victim)
    {
        if (Character == null)
        {
            return;
        }

        QuestComponent.HandleCombatantDeath(victim.Name);

        // Handle Quest Progress (Kill)
        if (victim.LastAttackerId.HasValue && victim.LastAttackerId.Value == Character.Id)
        {
            int? monsterId = null;
            if (victim is ServerCharacter mob && mob.MonsterId > 0)
            {
                monsterId = mob.MonsterId;
            }

            var updated = QuestComponent.TryProgress("Kill", victim.Name, 1, monsterId);
            foreach (var qid in updated)
            {
                _ = SendQuestUpdateAsync(qid);
            }
        }
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
                if (read <= 0)
                {
                    break;
                }

                _metrics?.RecordNetBytesReceived(read);

                var netMsg = NetMessage.Deserialize(buffer, read);

                if (netMsg != null)
                {
                    _metrics?.RecordNetMessageProcessed();
                    var sw = Stopwatch.StartNew();

                    var traceId = Guid.NewGuid().ToString();
                    PipelineLogger.LogStage(traceId, "NetworkReceive", 0, $"Op:{netMsg.Op} Size:{read}");

                    await HandleMessageAsync(netMsg, traceId);
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
            if (_combatManager != null)
            {
                _combatManager.OnCombatantDeath -= OnCombatantDeath;
            }

            if (UserId > 0)
            {
                await _playerService.SaveSessionAsync(this);
                _playerService.UnregisterSession(UserId);
            }

            _stream.Close();
            _client.Close();
        }
    }

    private async Task HandleMessageAsync(NetMessage msg, string traceId)
    {
        if (msg == null)
        {
            return;
        }

        var swValidate = Stopwatch.StartNew();
        if (!_rateLimiter.Check(msg.Op))
        {
            swValidate.Stop();
            _metrics?.RecordPipelineValidateDuration(swValidate.ElapsedTicks);
            _metrics?.RecordValidationError();
            SecurityLogger.LogSecurityEvent("RateLimitExceeded", UserId, $"Opcode: {msg.Op}");
            PipelineLogger.LogStage(traceId, "Validate", swValidate.Elapsed.TotalMilliseconds, "Failed: RateLimit");
            return;
        }

        swValidate.Stop();
        _metrics?.RecordPipelineValidateDuration(swValidate.ElapsedTicks);
        PipelineLogger.LogStage(traceId, "Validate", swValidate.Elapsed.TotalMilliseconds, "Success");

        var swResolve = Stopwatch.StartNew();
        switch (msg.Op)
        {
            case Opcode.LoginRequest:
                await HandleLoginAsync(msg.JsonPayload, traceId);
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
                await HandleInteractAsync(msg.JsonPayload, traceId);
                break;
            case Opcode.AttackRequest:
                await HandleAttackAsync(msg.JsonPayload);
                break;
            case Opcode.PurchaseGemsIntent:
                await HandlePurchaseGemsIntentAsync(msg.JsonPayload, traceId);
                break;
            case Opcode.PurchaseGemsVerify:
                await HandlePurchaseGemsVerifyAsync(msg.JsonPayload, traceId);
                break;
            case Opcode.BuyShopItemRequest:
                await HandleBuyShopItemAsync(msg.JsonPayload, traceId);
                break;
            case Opcode.PetActionRequest:
                await HandlePetActionAsync(msg.JsonPayload);
                break;
            case Opcode.UseItemRequest:
                await HandleUseItemAsync(msg.JsonPayload, traceId);
                break;
            // etc.
        }

        swResolve.Stop();
        _metrics?.RecordPipelineResolveDuration(swResolve.ElapsedTicks);
        PipelineLogger.LogStage(traceId, "Resolve", swResolve.Elapsed.TotalMilliseconds, $"Op:{msg.Op}");
    }

    private async Task HandleUseItemAsync(string payload, string traceId)
    {
        if (UserId <= 0 || Character == null)
        {
            return;
        }

        UseItemRequestDTO? request = null;
        try
        {
            request = JsonSerializer.Deserialize<UseItemRequestDTO>(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (request == null)
        {
            return;
        }

        if (Character.UseItem(request.SlotIndex, out var modifiedItem))
        {
            var update = new InventoryUpdate
            {
                PlayerId = UserId,
                Items = Character.Inventory.ToList()
            };

            await SendAsync(new NetMessage
            {
                Op = Opcode.InventoryUpdate,
                JsonPayload = JsonSerializer.Serialize(update, _jsonOptions)
            });

            PipelineLogger.LogStage(traceId, "UseItem", 0, $"Success Slot:{request.SlotIndex}");
        }
        else
        {
            PipelineLogger.LogStage(traceId, "UseItem", 0, "Failed");
        }
    }

    private async Task HandlePetActionAsync(string payload)
    {
        if (UserId <= 0)
        {
            return;
        }

        var request = JsonSerializer.Deserialize<PetActionRequest>(payload, _jsonOptions);
        if (request == null)
        {
            return;
        }

        var success = false;
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

    private async Task HandlePurchaseGemsIntentAsync(string payload, string traceId)
    {
        if (UserId <= 0)
        {
            return;
        }

        var request = JsonSerializer.Deserialize<PurchaseGemsIntentDTO>(payload, _jsonOptions);

        // Validation
        if (request == null || string.IsNullOrEmpty(request.ProductId))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing ProductId", traceId);
            return;
        }

        if (request.ProductId.Length > 20 || !request.ProductId.StartsWith("gems_"))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId,
                $"Invalid ProductId format: {request.ProductId}", traceId);
            return;
        }

        var result = _economyManager.InitiatePurchase(UserId, request.ProductId, traceId);
        if (result == null)
        {
            return;
        }

        await SendAsync(new NetMessage
            { Op = Opcode.PurchaseGemsIntent, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandlePurchaseGemsVerifyAsync(string payload, string traceId)
    {
        if (UserId <= 0 || Character == null)
        {
            return;
        }

        var request = JsonSerializer.Deserialize<PurchaseGemsVerifyDTO>(payload, _jsonOptions);

        // Validation
        if (request == null || string.IsNullOrEmpty(request.OrderId))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing OrderId", traceId);
            return;
        }

        if (string.IsNullOrEmpty(request.ReceiptToken))
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId, "Missing ReceiptToken", traceId);
            return;
        }

        var result = _economyManager.VerifyPurchase(UserId, request.OrderId, request.ReceiptToken, Character, traceId);

        await SendAsync(new NetMessage
            { Op = Opcode.PurchaseGemsVerify, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandleBuyShopItemAsync(string payload, string traceId)
    {
        if (UserId <= 0 || Character == null)
        {
            return;
        }

        var request = JsonSerializer.Deserialize<BuyShopItemDTO>(payload, _jsonOptions);
        if (request == null)
        {
            return;
        }

        // Hardening: Quantity Check
        if (request.Quantity <= 0 || request.Quantity > 999)
        {
            SecurityLogger.LogSecurityEvent("InvalidEconomyInput", UserId,
                $"Invalid Shop Quantity: {request.Quantity}", traceId);
            return;
        }

        var result = _economyManager.BuyShopItem(Character, request.ShopItemId, request.Quantity, null, traceId);

        await SendAsync(new NetMessage
            { Op = Opcode.BuyShopItemRequest, JsonPayload = JsonSerializer.Serialize(result, _jsonOptions) });
    }

    private async Task HandleAttackAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<UseSkillRequest>(payload, _jsonOptions);
        if (request == null)
        {
            return;
        }

        // Ensure the request comes from this player
        if (request.PlayerId != UserId && Character != null)
        {
            request.PlayerId = Character.Id;
        }

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
            // (Quest progress is now handled via OnCombatantDeath event)
        }
    }

    private async Task HandleInteractAsync(string payload, string traceId)
    {
        if (string.IsNullOrEmpty(payload) || payload.Length > 256)
        {
            return;
        }

        InteractDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<InteractDTO>(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.TargetName))
        {
            return;
        }

        if (dto.TargetName.Length > 64)
        {
            SecurityLogger.LogSecurityEvent("InvalidInput", UserId, "TargetName too long", traceId);
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
        if (int.TryParse(payload, out var questId) && questId > 0)
        {
            if (QuestComponent.StartQuest(questId))
            {
                await SendQuestUpdateAsync(questId);
            }
        }
    }

    private async Task HandleClaimRewardAsync(string payload)
    {
        if (int.TryParse(payload, out var questId) && questId > 0)
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
        if (Character == null)
        {
            return;
        }

        const string gsFlag = "GS_GRANTED";
        // Optimization: check flag first
        if (QuestComponent.Flags.Contains(gsFlag))
        {
            return;
        }

        var skillId = 0;
        var skillName = "";

        switch (Character.CharacterElement)
        {
            case Element.Water:
                skillId = SkillIds.GS_WATER_DIMINUTION;
                skillName = "Diminution";
                break;
            case Element.Earth:
                skillId = SkillIds.GS_EARTH_SUPPORT_SEAL;
                skillName = "Support Seal";
                break;
            case Element.Fire:
                skillId = SkillIds.GS_FIRE_EMBER_SURGE;
                skillName = "Ember Surge";
                break;
            case Element.Wind:
                skillId = SkillIds.GS_WIND_UNTOUCHABLE_VEIL;
                skillName = "Untouchable Veil";
                break;
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

    private async Task SendLoginError(string errorMessage)
    {
        await SendAsync(new NetMessage
        {
            Op = Opcode.LoginResponse,
            JsonPayload = JsonSerializer.Serialize(new LoginResponseDto
            {
                Success = false,
                ErrorMessage = errorMessage
            }, _jsonOptions)
        });
    }

    private async Task HandleLoginAsync(string payload, string traceId)
    {
        // payload podría ser {"username":"xxx","passHash":"abc"}
        if (string.IsNullOrEmpty(payload) || payload.Length > 512)
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_PAYLOAD_SIZE");
            return;
        }

        LoginDTO? loginDto = null;
        try
        {
            loginDto = JsonSerializer.Deserialize<LoginDTO>(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_INVALID_JSON");
            return;
        }

        if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) ||
            string.IsNullOrWhiteSpace(loginDto.PassHash))
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_MISSING_CREDS");
            return;
        }

        if (loginDto.Username.Length > 50)
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_USER_TOO_LONG");
            return;
        }

        if (loginDto.PassHash.Length < 64 || loginDto.PassHash.Length > 128)
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_HASH_LENGTH");
            return;
        }

        if (!IsHex(loginDto.PassHash))
        {
            _metrics.RecordLoginAttempt(false);
            await SendLoginError("ERR_LOGIN_HASH_FORMAT");
            return;
        }

        var uid = await _dbService.CheckLoginAsync(loginDto.Username, loginDto.PassHash);
        if (uid < 0)
        {
            _metrics.RecordLoginAttempt(false);
            // login fallido
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = JsonSerializer.Serialize(new LoginResponseDto
                {
                    Success = false,
                    ErrorMessage = "ERR_LOGIN_INVALID_CREDS"
                }, _jsonOptions)
            });
        }
        else
        {
            _metrics.RecordLoginAttempt(true);
            UserId = uid;

            var data = await _playerService.LoadDataAsync(uid);
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
                        if (def != null)
                        {
                            pet.Hydrate(def);
                        }
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
                // New Character
                Character = new ServerCharacter
                {
                    Id = uid,
                    Name = loginDto.Username,
                    Hp = 100,
                    CharacterElement = Element.Earth // Default to Earth to prevent Element.None
                };
                QuestComponent.Character = Character;
            }

            // Migration: Ensure Element is not None
            if (Character != null && Character.CharacterElement == Element.None)
            {
                Console.WriteLine($"[Migration] Player {Character.Name} ({Character.Id}) has Element.None. Setting to Earth.");
                Character.CharacterElement = Element.Earth;
                Character.IsDirty = true;
            }

            _playerService.RegisterSession(this);

            GrantGoddessSkills();

            // mandar una LoginResponse
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = JsonSerializer.Serialize(new LoginResponseDto
                {
                    Success = true,
                    UserId = uid,
                    PosX = Character?.X ?? 0f,
                    PosY = Character?.Y ?? 0f,
                    Hp = Character?.Hp ?? 0,
                    MaxHp = Character?.MaxHp ?? 0
                }, _jsonOptions)
            });
        }
    }

    private static bool IsHex(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var isDigit = c >= '0' && c <= '9';
            var isLower = c >= 'a' && c <= 'f';
            var isUpper = c >= 'A' && c <= 'F';
            if (!isDigit && !isLower && !isUpper)
            {
                return false;
            }
        }

        return true;
    }

    private async Task HandleMoveAsync(string payload)
    {
        // EJ: {"dx":1,"dy":0}
        if (UserId < 0 || Character == null)
        {
            return; // no logueado
        }

        // Block movement if in combat
        if (_combatManager.GetCombatant(Character.Id) != null)
        {
            return;
        }

        var moveDto = JsonSerializer.Deserialize<MoveDTO>(payload, _jsonOptions);

        if (moveDto == null)
        {
            return;
        }

        // Actualizar la pos en el server side:
        Character.X += moveDto.dx;
        Character.Y += moveDto.dy;

        // Check for triggers
        _worldTriggerService.CheckTriggers(Character);

        // Check for encounters
        _spawnManager?.OnPlayerMoved(this);

        // Broadcast a otros en la misma zona
        await Task.CompletedTask; // Placeholder for async broadcast
    }

    public virtual async Task SendAsync(NetMessage msg)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
        _metrics?.RecordNetBytesSent(bytes.Length);
    }

    public void HandleInstanceCompletion(string instanceName)
    {
        if (Character == null)
        {
            return;
        }

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
        if (Character == null)
        {
            return;
        }

        var failedQuests = QuestComponent.HandleInstanceFailure(instanceId);
        foreach (var questId in failedQuests)
        {
            _ = SendQuestUpdateAsync(questId);
        }
    }

    public async Task DisconnectAsync(string reason)
    {
        try
        {
            if (_client.Connected)
            {
                var msg = new NetMessage
                {
                    Op = Opcode.Disconnect,
                    JsonPayload = JsonSerializer.Serialize(new { reason }, _jsonOptions)
                };
                await SendAsync(msg);
                // Give a small moment for the packet to flush?
                // The stream closing will happen, but TCP should buffer it.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending disconnect packet to {UserId}: {ex.Message}");
        }
        finally
        {
            _client.Close();
        }
    }
}
