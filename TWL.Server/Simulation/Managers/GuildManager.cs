using System;
using System.Collections.Concurrent;
using System.Linq;
using TWL.Shared.Domain.Guilds;

namespace TWL.Server.Simulation.Managers;

public class GuildManager : IGuildService
{
    private readonly ConcurrentDictionary<int, Guild> _guilds = new();
    // CharacterId -> GuildId
    private readonly ConcurrentDictionary<int, int> _playerGuildMap = new();

    // TargetId -> GuildInvite
    private readonly ConcurrentDictionary<int, GuildInvite> _pendingInvites = new();

    private readonly GuildPermissionService _permissionService = new();

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
        if (guild == null) return false;
        
        var rank = guild.MemberRanks.GetValueOrDefault(characterId, GuildRank.Recruit);
        return _permissionService.HasPermission(rank, GuildPermissions.Kick);
    }

    public bool HasPermission(int guildId, int characterId, GuildPermissions permission)
    {
        var guild = GetGuild(guildId);
        if (guild == null) return false;
        
        var rank = guild.MemberRanks.GetValueOrDefault(characterId, GuildRank.Recruit);
        return _permissionService.HasPermission(rank, permission);
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
        if (_guilds.Values.Any(g => g.Name.Equals(guildName, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Guild name is already taken.");
        }

        var guildId = System.Threading.Interlocked.Increment(ref _nextGuildId);
        var guild = new Guild
        {
            GuildId = guildId,
            Name = guildName,
            LeaderId = leaderId,
            MemberIds = { leaderId }
        };
        guild.MemberRanks[leaderId] = GuildRank.Leader;
        guild.MemberJoinDates[leaderId] = DateTimeOffset.UtcNow;

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

        var inviterRank = guild.MemberRanks.GetValueOrDefault(inviterId, GuildRank.Recruit);
        if (!_permissionService.HasPermission(inviterRank, GuildPermissions.Invite))
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
            guild.MemberRanks[targetId] = GuildRank.Recruit;
            guild.MemberJoinDates[targetId] = DateTimeOffset.UtcNow;
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
            guild.MemberRanks.Remove(characterId);

            if (guild.MemberIds.Count == 0)
            {
                // Last member left, disband guild
                _guilds.TryRemove(guild.GuildId, out _);
            }
            else if (guild.LeaderId == characterId)
            {
                // Leader left, assign new leader by highest rank or fallback
                var newLeader = guild.MemberIds.OrderByDescending(id => guild.MemberRanks.GetValueOrDefault(id, GuildRank.Recruit)).First();
                guild.LeaderId = newLeader;
                guild.MemberRanks[newLeader] = GuildRank.Leader;
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

        if (!guild.MemberIds.Contains(targetId))
        {
            return (false, "Target player is not in the guild.");
        }
        
        var kickerRank = guild.MemberRanks.GetValueOrDefault(kickerId, GuildRank.Recruit);
        var targetRank = guild.MemberRanks.GetValueOrDefault(targetId, GuildRank.Recruit);

        if (!_permissionService.CanKick(kickerRank, targetRank))
        {
            return (false, "You do not have permission to kick this member.");
        }

        _playerGuildMap.TryRemove(targetId, out _);

        lock (guild.MemberIds)
        {
            guild.MemberIds.Remove(targetId);
            guild.MemberRanks.Remove(targetId);
        }

        return (true, "Player kicked.");
    }

    public (bool Success, string Message) PromoteMember(int actorId, int targetId)
    {
        var guild = GetGuildByMember(actorId);
        if (guild == null) return (false, "You are not in a guild.");
        if (actorId == targetId) return (false, "You cannot promote yourself.");
        if (!guild.MemberIds.Contains(targetId)) return (false, "Target player is not in the guild.");

        var actorRank = guild.MemberRanks.GetValueOrDefault(actorId, GuildRank.Recruit);
        var targetRank = guild.MemberRanks.GetValueOrDefault(targetId, GuildRank.Recruit);
        
        if (targetRank >= GuildRank.Officer) return (false, "Cannot promote further.");
        
        var newRank = targetRank + 1;
        
        if (!_permissionService.CanPromoteDemote(actorRank, targetRank, newRank))
            return (false, "You do not have permission to promote this member.");
            
        guild.MemberRanks[targetId] = newRank;
        return (true, "Member promoted.");
    }

    public (bool Success, string Message) DemoteMember(int actorId, int targetId)
    {
        var guild = GetGuildByMember(actorId);
        if (guild == null) return (false, "You are not in a guild.");
        if (actorId == targetId) return (false, "You cannot demote yourself.");
        if (!guild.MemberIds.Contains(targetId)) return (false, "Target player is not in the guild.");

        var actorRank = guild.MemberRanks.GetValueOrDefault(actorId, GuildRank.Recruit);
        var targetRank = guild.MemberRanks.GetValueOrDefault(targetId, GuildRank.Recruit);
        
        if (targetRank <= GuildRank.Recruit) return (false, "Cannot demote further.");
        
        var newRank = targetRank - 1;
        
        if (!_permissionService.CanPromoteDemote(actorRank, targetRank, newRank))
            return (false, "You do not have permission to demote this member.");
            
        guild.MemberRanks[targetId] = newRank;
        return (true, "Member demoted.");
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
