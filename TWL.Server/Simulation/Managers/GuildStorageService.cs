using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Guilds;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation.Managers;

public class GuildStorageService
{
    private readonly GuildManager _guildManager;
    private readonly IGuildRepository _guildRepository;
    private readonly ILogger<GuildStorageService> _logger;
    private readonly GuildAuditLogService _auditLogService;

    // Default tenure to 14 days
    public TimeSpan WithdrawalTenureGate { get; set; } = TimeSpan.FromDays(14);

    public GuildStorageService(GuildManager guildManager, IGuildRepository guildRepository, GuildAuditLogService auditLogService, ILogger<GuildStorageService> logger)
    {
        _guildManager = guildManager;
        _guildRepository = guildRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<GuildStorageViewEvent> ViewStorage(ServerCharacter character)
    {
        var guild = _guildManager.GetGuildByMember(character.Id);
        if (guild == null)
            return new GuildStorageViewEvent();

        var semaphore = _guildLocks.GetOrAdd(guild.GuildId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            return new GuildStorageViewEvent
            {
                Items = guild.StorageItems.Select(kvp => new GuildStorageItemDto
                {
                    ItemId = kvp.Key,
                    Quantity = kvp.Value
                }).ToList()
            };
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<GuildStorageOperationResultEvent> DepositItem(ServerCharacter character, int itemId, int quantity, string operationId)
    {
        if (!string.IsNullOrEmpty(operationId) && _processedOperations.TryGetValue(operationId, out var existingResult))
        {
            return existingResult;
        }

        if (quantity <= 0)
        {
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "Invalid quantity." });
        }

        var guild = _guildManager.GetGuildByMember(character.Id);
        if (guild == null)
        {
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "You are not in a guild." });
        }

        var semaphore = _guildLocks.GetOrAdd(guild.GuildId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            // Atomically remove from player inventory first
            // We assume Unbound for simplicity in guild storage, or we'd need DTO to specify policy
            if (!character.RemoveItem(itemId, quantity, BindPolicy.Unbound))
            {
                return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "Insufficient items in inventory." });
            }

            // Add to guild storage
            if (guild.StorageItems.ContainsKey(itemId))
                guild.StorageItems[itemId] += quantity;
            else
                guild.StorageItems[itemId] = quantity;

            await _guildRepository.SaveAsync(guild);
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = true });
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Track processed operations for idempotency
    private readonly ConcurrentDictionary<string, GuildStorageOperationResultEvent> _processedOperations = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _guildLocks = new();

    public async Task<GuildStorageOperationResultEvent> WithdrawItem(ServerCharacter character, int itemId, int quantity, string operationId)
    {
        if (!string.IsNullOrEmpty(operationId) && _processedOperations.TryGetValue(operationId, out var existingResult))
        {
            return existingResult;
        }

        if (quantity <= 0)
        {
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "Invalid quantity." });
        }

        var guild = _guildManager.GetGuildByMember(character.Id);
        if (guild == null)
        {
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "You are not in a guild." });
        }

        if (!_guildManager.HasPermission(guild.GuildId, character.Id, GuildPermissions.WithdrawStorage))
        {
            _auditLogService.AppendWithdrawalEntry(guild.GuildId, character.Id, itemId, quantity, false, "Permission denied");
            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "You do not have permission to withdraw items." });
        }

        if (guild.MemberJoinDates.TryGetValue(character.Id, out var joinDate))
        {
            if (DateTimeOffset.UtcNow - joinDate < WithdrawalTenureGate)
            {
                _auditLogService.AppendWithdrawalEntry(guild.GuildId, character.Id, itemId, quantity, false, "Tenure gate not met");
                return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "You have not been in the guild long enough to withdraw items." });
            }
        }

        var semaphore = _guildLocks.GetOrAdd(guild.GuildId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            if (!guild.StorageItems.TryGetValue(itemId, out int currentQuantity) || currentQuantity < quantity)
            {
                _auditLogService.AppendWithdrawalEntry(guild.GuildId, character.Id, itemId, quantity, false, "Insufficient quantity");
                return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "Not enough items in storage." });
            }

            // Check if player can carry more (inventory slots)
            if (!character.CanAddItem(itemId, quantity, BindPolicy.Unbound))
            {
                 _auditLogService.AppendWithdrawalEntry(guild.GuildId, character.Id, itemId, quantity, false, "Inventory full");
                return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = false, Message = "Your inventory is full." });
            }

            // Perform storage mutation
            guild.StorageItems[itemId] -= quantity;
            if (guild.StorageItems[itemId] == 0)
            {
                guild.StorageItems.Remove(itemId);
            }

            // Add to character inventory
            character.AddItem(itemId, quantity, BindPolicy.Unbound);

            _auditLogService.AppendWithdrawalEntry(guild.GuildId, character.Id, itemId, quantity, true, "Success");
            await _guildRepository.SaveAsync(guild);

            return RecordResult(operationId, new GuildStorageOperationResultEvent { OperationId = operationId, Success = true });
        }
        finally
        {
            semaphore.Release();
        }
    }

    private GuildStorageOperationResultEvent RecordResult(string operationId, GuildStorageOperationResultEvent result)
    {
        if (!string.IsNullOrEmpty(operationId))
        {
            _processedOperations.TryAdd(operationId, result);
        }
        return result;
    }
}
