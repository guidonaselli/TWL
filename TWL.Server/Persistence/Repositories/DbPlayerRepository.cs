using System.Text.Json;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Database.Entities;
using TWL.Server.Persistence.Repositories.Queries;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Persistence.Repositories;

/// <summary>
/// Hybrid EF Core (writes) + Dapper (reads) implementation of IPlayerRepository.
/// Uses IDbContextFactory to create short-lived contexts per save operation (safe for singleton lifecycle).
/// Uses NpgsqlDataSource for Dapper reads (high-performance single-query loads).
/// </summary>
public class DbPlayerRepository : IPlayerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DbPlayerRepository> _logger;

    public DbPlayerRepository(
        IDbContextFactory<GameDbContext> contextFactory,
        NpgsqlDataSource dataSource,
        ILogger<DbPlayerRepository> logger)
    {
        _contextFactory = contextFactory;
        _dataSource = dataSource;
        _logger = logger;
    }

    /// <summary>
    /// Saves player data atomically using EF Core.
    /// Uses default Read Committed isolation — Serializable is only needed for
    /// multi-party operations (market, guild bank) in future phases.
    /// </summary>
    public async Task SaveAsync(int userId, PlayerSaveData data)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.Players
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (entity == null)
            {
                entity = new PlayerEntity { UserId = userId };
                context.Players.Add(entity);
            }

            // Map scalar fields from ServerCharacterData
            var c = data.Character;
            entity.Name = c.Name ?? string.Empty;
            entity.Hp = c.Hp;
            entity.Sp = c.Sp;
            entity.Level = c.Level;
            entity.RebirthLevel = c.RebirthLevel;
            entity.Exp = c.Exp;
            entity.ExpToNextLevel = c.ExpToNextLevel;
            entity.StatPoints = c.StatPoints;
            entity.Str = c.Str;
            entity.Con = c.Con;
            entity.Int = c.Int;
            entity.Wis = c.Wis;
            entity.Agi = c.Agi;
            entity.Element = (int)c.Element;
            entity.Gold = c.Gold;
            entity.PremiumCurrency = c.PremiumCurrency;
            entity.DailyGiftAccumulator = c.DailyGiftAccumulator;
            entity.LastGiftResetDate = DateTime.SpecifyKind(c.LastGiftResetDate, DateTimeKind.Utc);
            entity.MapId = c.MapId;
            entity.X = c.X;
            entity.Y = c.Y;
            entity.ActivePetInstanceId = c.ActivePetInstanceId;

            // Serialize JSONB fields
            entity.InventoryJson = JsonSerializer.Serialize(c.Inventory, JsonOptions);
            entity.EquipmentJson = JsonSerializer.Serialize(c.Equipment, JsonOptions);
            entity.BankJson = JsonSerializer.Serialize(c.Bank, JsonOptions);
            entity.PetsJson = JsonSerializer.Serialize(c.Pets, JsonOptions);
            entity.SkillsJson = JsonSerializer.Serialize(c.Skills, JsonOptions);
            entity.WorldFlagsJson = JsonSerializer.Serialize(c.WorldFlags, JsonOptions);
            entity.ProcessedOrdersJson = JsonSerializer.Serialize(c.ProcessedOrders, JsonOptions);
            entity.InstanceLockoutsJson = JsonSerializer.Serialize(c.InstanceLockouts, JsonOptions);

            // Serialize quest data
            var q = data.Quests;
            entity.QuestStatesJson = JsonSerializer.Serialize(q.States, JsonOptions);
            entity.QuestProgressJson = JsonSerializer.Serialize(q.Progress, JsonOptions);
            entity.QuestFlagsJson = JsonSerializer.Serialize(q.Flags, JsonOptions);
            entity.QuestCompletionTimesJson = JsonSerializer.Serialize(q.CompletionTimes, JsonOptions);
            entity.QuestStartTimesJson = JsonSerializer.Serialize(q.StartTimes, JsonOptions);

            entity.LastSaved = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save player data for UserId {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Loads player data using Dapper with a single high-performance SQL query.
    /// </summary>
    public async Task<PlayerSaveData?> LoadAsync(int userId)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            var dto = await conn.QueryFirstOrDefaultAsync<PlayerDto>(
                PlayerQueries.LoadByUserId,
                new { UserId = userId });

            if (dto == null)
                return null;

            return MapDtoToSaveData(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load player data for UserId {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Synchronous load wrapper. Prefer LoadAsync when possible.
    /// </summary>
    [Obsolete("Use LoadAsync instead")]
    public PlayerSaveData? Load(int userId)
    {
        return LoadAsync(userId).GetAwaiter().GetResult();
    }

    private static PlayerSaveData MapDtoToSaveData(PlayerDto dto)
    {
        var character = new ServerCharacterData
        {
            Id = dto.PlayerId,
            Name = dto.Name,
            Hp = dto.Hp,
            Sp = dto.Sp,
            Level = dto.Level,
            RebirthLevel = dto.RebirthLevel,
            Exp = dto.Exp,
            ExpToNextLevel = dto.ExpToNextLevel,
            StatPoints = dto.StatPoints,
            Str = dto.Str,
            Con = dto.Con,
            Int = dto.Int,
            Wis = dto.Wis,
            Agi = dto.Agi,
            Element = (Element)dto.Element,
            Gold = dto.Gold,
            PremiumCurrency = dto.PremiumCurrency,
            DailyGiftAccumulator = dto.DailyGiftAccumulator,
            LastGiftResetDate = dto.LastGiftResetDate,
            MapId = dto.MapId,
            X = dto.X,
            Y = dto.Y,
            ActivePetInstanceId = dto.ActivePetInstanceId,
            Inventory = DeserializeOrDefault<List<Item>>(dto.InventoryJson) ?? new List<Item>(),
            Equipment = DeserializeOrDefault<List<Item>>(dto.EquipmentJson) ?? new List<Item>(),
            Bank = DeserializeOrDefault<List<Item>>(dto.BankJson) ?? new List<Item>(),
            Pets = DeserializeOrDefault<List<ServerPetData>>(dto.PetsJson) ?? new List<ServerPetData>(),
            Skills = DeserializeOrDefault<List<SkillMasteryData>>(dto.SkillsJson) ?? new List<SkillMasteryData>(),
            WorldFlags = DeserializeOrDefault<HashSet<string>>(dto.WorldFlagsJson) ?? new HashSet<string>(),
            ProcessedOrders = DeserializeOrDefault<HashSet<string>>(dto.ProcessedOrdersJson) ?? new HashSet<string>(),
            InstanceLockouts = DeserializeOrDefault<Dictionary<string, DateTime>>(dto.InstanceLockoutsJson) ?? new Dictionary<string, DateTime>()
        };

        var quests = new QuestData
        {
            States = DeserializeOrDefault<Dictionary<int, QuestState>>(dto.QuestStatesJson) ?? new Dictionary<int, QuestState>(),
            Progress = DeserializeOrDefault<Dictionary<int, List<int>>>(dto.QuestProgressJson) ?? new Dictionary<int, List<int>>(),
            Flags = DeserializeOrDefault<HashSet<string>>(dto.QuestFlagsJson) ?? new HashSet<string>(),
            CompletionTimes = DeserializeOrDefault<Dictionary<int, DateTime>>(dto.QuestCompletionTimesJson) ?? new Dictionary<int, DateTime>(),
            StartTimes = DeserializeOrDefault<Dictionary<int, DateTime>>(dto.QuestStartTimesJson) ?? new Dictionary<int, DateTime>()
        };

        return new PlayerSaveData
        {
            Character = character,
            Quests = quests,
            LastSaved = dto.LastSaved
        };
    }

    private static T? DeserializeOrDefault<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
