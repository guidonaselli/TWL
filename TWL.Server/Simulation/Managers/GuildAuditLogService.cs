using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TWL.Server.Simulation.Managers;

public class GuildAuditLogEntry
{
    public int GuildId { get; set; }
    public int CharacterId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

public class GuildAuditLogService
{
    // GuildId -> List of audit entries
    private readonly ConcurrentDictionary<int, List<GuildAuditLogEntry>> _auditLogs = new();

    public void AppendWithdrawalEntry(int guildId, int characterId, int itemId, int quantity, bool success, string reason)
    {
        var entry = new GuildAuditLogEntry
        {
            GuildId = guildId,
            CharacterId = characterId,
            ItemId = itemId,
            Quantity = quantity,
            Success = success,
            Reason = reason,
            Timestamp = DateTimeOffset.UtcNow
        };

        _auditLogs.AddOrUpdate(guildId, 
            id => new List<GuildAuditLogEntry> { entry },
            (id, list) => {
                lock (list)
                {
                    list.Add(entry);
                }
                return list;
            });
    }

    public List<GuildAuditLogEntry> GetAuditLogs(int guildId)
    {
        if (_auditLogs.TryGetValue(guildId, out var logs))
        {
            lock (logs)
            {
                return new List<GuildAuditLogEntry>(logs);
            }
        }
        return new List<GuildAuditLogEntry>();
    }
}
