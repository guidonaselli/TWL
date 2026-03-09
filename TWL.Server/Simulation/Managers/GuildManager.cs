using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TWL.Server.Simulation.Managers;

public class GuildManager : IGuildService
{
    private readonly ConcurrentDictionary<int, Guild> _guilds = new();
    // CharacterId -> GuildId
    private readonly ConcurrentDictionary<int, int> _playerGuildMap = new();

    // TargetId -> GuildInvite
    private readonly ConcurrentDictionary<int, GuildInvite> _pendingInvites = new();

    private int _nextGuildId = 1;

    public const int MaxGuildSize = 50;
    public const int CreationFee = 5000;
    public const int InviteTimeoutSeconds = 30;

    public Guild? GetGuild(int guildId)
    {
        return _guilds.TryGetValue(guildId, out var guild) ? guild : null;
    }

    public Guild? GetGuildByMember(int characterId)
    {
        if (_playerGuildMap.TryGetValue(characterId, out var guildId))
        {
            return GetGuild(guildId);
        }
        return null;
    }

    public bool IsAuthorizedToKick(int guildId, int characterId)
    {
        var guild = GetGuild(guildId);
        // For now, only the leader can kick. Will be expanded in Rank system.
        return guild != null && guild.LeaderId == characterId;
    }

    public (bool Success, string Message) CreateGuild(int leaderId, string leaderName, string guildName)
    {
        if (string.IsNullOrWhiteSpace(guildName))
        {
            return (false, "Guild name cannot be empty.");
        }

        if (GetGuildByMember(leaderId) != null)
        {
            return (false, "You are already in a guild.");
        }

        // Check for unique name
        if (_guilds.Values.Any(g => g.GuildName.Equals(guildName, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Guild name is already taken.");
        }

        var guildId = System.Threading.Interlocked.Increment(ref _nextGuildId);
        var guild = new Guild
        {
            GuildId = guildId,
            GuildName = guildName,
            LeaderId = leaderId,
            MemberIds = { leaderId }
        };

        if (_guilds.TryAdd(guildId, guild))
        {
            _playerGuildMap.TryAdd(leaderId, guildId);
            return (true, "Guild created successfully.");
        }

        return (false, "Failed to create guild.");
    }

    public (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName)
    {
        var guild = GetGuildByMember(inviterId);
        if (guild == null)
        {
            return (false, "You are not in a guild.");
        }

        if (guild.MemberIds.Count >= MaxGuildSize)
        {
            return (false, "Guild is full.");
        }

        if (GetGuildByMember(targetId) != null)
        {
            return (false, "Target player is already in a guild.");
        }

        // For now, only the leader can invite
        if (guild.LeaderId != inviterId)
        {
            return (false, "You do not have permission to invite.");
        }

        if (_pendingInvites.TryGetValue(targetId, out var existingInvite))
        {
            if (existingInvite.InviterId == inviterId && existingInvite.IsActive())
            {
                return (false, "You already sent an invite to this player.");
            }
            if (existingInvite.IsActive())
            {
                return (false, "Target player already has a pending invite.");
            }
        }

        var invite = new GuildInvite
        {
            GuildId = guild.GuildId,
            InviterId = inviterId,
            TargetId = targetId,
            InviterName = inviterName,
            TargetName = targetName,
            ExpireTime = DateTime.UtcNow.AddSeconds(InviteTimeoutSeconds)
        };

        _pendingInvites[targetId] = invite;
        return (true, "Invite sent.");
    }

    public (bool Success, string Message) AcceptInvite(int targetId, int guildId)
    {
        if (!_pendingInvites.TryGetValue(targetId, out var invite) || invite.GuildId != guildId)
        {
            return (false, "No active invite found for this guild.");
        }

        _pendingInvites.TryRemove(targetId, out _);

        if (!invite.IsActive())
        {
            return (false, "Invite has expired.");
        }

        var guild = GetGuild(guildId);
        if (guild == null)
        {
            return (false, "Guild no longer exists.");
        }

        if (guild.MemberIds.Count >= MaxGuildSize)
        {
            return (false, "Guild is full.");
        }

        if (GetGuildByMember(targetId) != null)
        {
            return (false, "You are already in a guild.");
        }

        lock (guild.MemberIds)
        {
            if (guild.MemberIds.Contains(targetId))
            {
                 return (false, "You are already in this guild.");
            }
            guild.MemberIds.Add(targetId);
        }

        _playerGuildMap[targetId] = guildId;

        return (true, "Joined the guild.");
    }

    public bool DeclineInvite(int targetId, int guildId)
    {
        if (_pendingInvites.TryGetValue(targetId, out var invite) && invite.GuildId == guildId)
        {
            return _pendingInvites.TryRemove(targetId, out _);
        }
        return false;
    }

    public bool LeaveGuild(int characterId)
    {
        var guild = GetGuildByMember(characterId);
        if (guild == null) return false;

        _playerGuildMap.TryRemove(characterId, out _);

        lock (guild.MemberIds)
        {
            guild.MemberIds.Remove(characterId);

            if (guild.MemberIds.Count == 0)
            {
                // Last member left, disband guild
                _guilds.TryRemove(guild.GuildId, out _);
            }
            else if (guild.LeaderId == characterId)
            {
                // Leader left, assign new leader
                guild.LeaderId = guild.MemberIds[0];
            }
        }

        return true;
    }

    public (bool Success, string Message) KickMember(int kickerId, int targetId)
    {
        var guild = GetGuildByMember(kickerId);
        if (guild == null)
        {
            return (false, "You are not in a guild.");
        }

        if (!IsAuthorizedToKick(guild.GuildId, kickerId))
        {
            return (false, "You do not have permission to kick.");
        }

        if (kickerId == targetId)
        {
            return (false, "You cannot kick yourself.");
        }

        if (!guild.MemberIds.Contains(targetId))
        {
            return (false, "Target player is not in the guild.");
        }

        _playerGuildMap.TryRemove(targetId, out _);

        lock (guild.MemberIds)
        {
            guild.MemberIds.Remove(targetId);
        }

        return (true, "Player kicked.");
    }

    private class GuildInvite
    {
        public int GuildId { get; set; }
        public int InviterId { get; set; }
        public int TargetId { get; set; }
        public string InviterName { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public DateTime ExpireTime { get; set; }

        public bool IsActive() => DateTime.UtcNow <= ExpireTime;
    }
}
