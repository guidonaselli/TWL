using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Database.Entities;
using TWL.Server.Persistence.Repositories.Queries;
using TWL.Shared.Domain.Guilds;

namespace TWL.Server.Persistence.Repositories;

public class DbGuildRepository : IGuildRepository
{
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly IDapperService _dapperService;
    private readonly ILogger<DbGuildRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DbGuildRepository(
        IDbContextFactory<GameDbContext> contextFactory,
        IDapperService dapperService,
        ILogger<DbGuildRepository> logger)
    {
        _contextFactory = contextFactory;
        _dapperService = dapperService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<IEnumerable<Guild>> LoadAllAsync()
    {
        try
        {
            var entities = await _dapperService.QueryAsync<GuildEntity>(GuildQueries.SelectAll);
            var guilds = new List<Guild>();
            foreach (var entity in entities)
            {
                guilds.Add(MapToDomain(entity));
            }
            return guilds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load all guilds");
            return Array.Empty<Guild>();
        }
    }

    public async Task<Guild?> LoadAsync(int guildId)
    {
        try
        {
            var entity = await _dapperService.QueryFirstOrDefaultAsync<GuildEntity>(
                GuildQueries.SelectById, new { GuildId = guildId });

            return entity != null ? MapToDomain(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load guild {GuildId}", guildId);
            return null;
        }
    }

    public async Task<Guild?> LoadByNameAsync(string name)
    {
        try
        {
            var entity = await _dapperService.QueryFirstOrDefaultAsync<GuildEntity>(
                GuildQueries.SelectByName, new { Name = name });

            return entity != null ? MapToDomain(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load guild by name {Name}", name);
            return null;
        }
    }

    public async Task<bool> SaveAsync(Guild guild)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.Guilds.FindAsync(guild.GuildId);
            if (entity == null)
            {
                entity = new GuildEntity { GuildId = guild.GuildId };
                context.Guilds.Add(entity);
            }

            // Map domain properties to entity
            entity.Name = guild.Name;
            entity.LeaderId = guild.LeaderId;
            entity.MemberIdsJson = JsonSerializer.Serialize(guild.MemberIds, _jsonOptions);
            entity.MemberRanksJson = JsonSerializer.Serialize(guild.MemberRanks, _jsonOptions);
            entity.MemberJoinDatesJson = JsonSerializer.Serialize(guild.MemberJoinDates, _jsonOptions);
            entity.StorageItemsJson = JsonSerializer.Serialize(guild.StorageItems, _jsonOptions);
            entity.LastSaved = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict saving guild {GuildId}", guild.GuildId);
            return false; // Let caller retry or fail
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving guild {GuildId}", guild.GuildId);
            return false;
        }
    }

    public async Task DeleteAsync(int guildId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.Guilds.FindAsync(guildId);
            if (entity != null)
            {
                context.Guilds.Remove(entity);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting guild {GuildId}", guildId);
        }
    }

    private Guild MapToDomain(GuildEntity entity)
    {
        var guild = new Guild
        {
            GuildId = entity.GuildId,
            Name = entity.Name,
            LeaderId = entity.LeaderId
        };

        if (!string.IsNullOrEmpty(entity.MemberIdsJson))
        {
            guild.MemberIds = JsonSerializer.Deserialize<List<int>>(entity.MemberIdsJson, _jsonOptions) ?? new();
        }

        if (!string.IsNullOrEmpty(entity.MemberRanksJson))
        {
            guild.MemberRanks = JsonSerializer.Deserialize<Dictionary<int, GuildRank>>(entity.MemberRanksJson, _jsonOptions) ?? new();
        }

        if (!string.IsNullOrEmpty(entity.MemberJoinDatesJson))
        {
            guild.MemberJoinDates = JsonSerializer.Deserialize<Dictionary<int, DateTimeOffset>>(entity.MemberJoinDatesJson, _jsonOptions) ?? new();
        }

        if (!string.IsNullOrEmpty(entity.StorageItemsJson))
        {
            guild.StorageItems = JsonSerializer.Deserialize<Dictionary<int, int>>(entity.StorageItemsJson, _jsonOptions) ?? new();
        }

        return guild;
    }
}
