using System;
using System.Collections.Concurrent;
using System.Linq;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Simulation.Managers;

public class GuildManager : IGuildService
{
    private readonly ConcurrentDictionary<int, Guild> _guilds = new();
    private readonly ConcurrentDictionary<int, int> _playerGuildMap = new();

    // TargetId -> GuildInvite
    private readonly ConcurrentDictionary<int, GuildInvite> _pendingInvites = new();

    private readonly PlayerService _playerService;

    private int _nextGuildId = 1;
    private readonly object _createLock = new();

    public const int GuildCreationFee = 50000;
    public const int MaxGuildSize = 50;
    public const int InviteTimeoutSeconds = 30;

    public GuildManager(PlayerService playerService)
    {
        _playerService = playerService;
    }

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

    public (bool Success, string Message, int GuildId) CreateGuild(int leaderId, string leaderName, string guildName)
    {
        if (string.IsNullOrWhiteSpace(guildName))
            return (false, "Guild name cannot be empty.", 0);

        if (_playerGuildMap.ContainsKey(leaderId))
            return (false, "You are already in a guild.", 0);

        lock (_createLock)
        {
            // Check for unique name
            bool nameExists = _guilds.Values.Any(g => g.Name.Equals(guildName, StringComparison.OrdinalIgnoreCase));
            if (nameExists)
                return (false, "Guild name is already taken.", 0);

            // Verify and deduct creation fee
            var leaderSession = _playerService.GetSession(leaderId);
            if (leaderSession?.Character == null || !leaderSession.Character.TryConsumeGold(GuildCreationFee))
            {
                return (false, $"Insufficient gold. Guild creation requires {GuildCreationFee} gold.", 0);
            }

            var guildId = System.Threading.Interlocked.Increment(ref _nextGuildId);

            var guild = new Guild
            {
                GuildId = guildId,
                Name = guildName,
                LeaderId = leaderId
            };
            guild.MemberIds.Add(leaderId);

            _guilds[guildId] = guild;
            _playerGuildMap[leaderId] = guildId;

            return (true, "Guild created successfully.", guildId);
        }
    }

    public (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName)
    {
        if (inviterId == targetId)
            return (false, "You cannot invite yourself.");

        var inviterGuild = GetGuildByMember(inviterId);
        if (inviterGuild == null)
            return (false, "You are not in a guild.");

        // Permission stub - checking if inviter is leader
        if (inviterGuild.LeaderId != inviterId)
            return (false, "You do not have permission to invite members.");

        if (inviterGuild.MemberIds.Count >= MaxGuildSize)
            return (false, "Guild is full.");

        var targetGuild = GetGuildByMember(targetId);
        if (targetGuild != null)
            return (false, "Target player is already in a guild.");

        // Clear expired invites
        if (_pendingInvites.TryGetValue(targetId, out var existingInvite))
        {
            if (existingInvite.ExpiresAt > DateTime.UtcNow)
                return (false, "Player already has a pending invite.");
            _pendingInvites.TryRemove(targetId, out _);
        }

        var invite = new GuildInvite
        {
            InviterId = inviterId,
            InviterName = inviterName,
            TargetId = targetId,
            TargetName = targetName,
            ExpiresAt = DateTime.UtcNow.AddSeconds(InviteTimeoutSeconds)
        };

        _pendingInvites[targetId] = invite;
        return (true, "Invite sent.");
    }

    public (bool Success, string Message) AcceptInvite(int targetId, int inviterId)
    {
        if (!_pendingInvites.TryGetValue(targetId, out var invite) || invite.InviterId != inviterId)
            return (false, "No valid invite found.");

        _pendingInvites.TryRemove(targetId, out _);

        if (invite.ExpiresAt < DateTime.UtcNow)
            return (false, "Invite has expired.");

        if (_playerGuildMap.ContainsKey(targetId))
            return (false, "You are already in a guild.");

        var guild = GetGuildByMember(inviterId);
        if (guild == null)
            return (false, "Inviter's guild no longer exists.");

        lock (guild)
        {
            if (guild.MemberIds.Count >= MaxGuildSize)
                return (false, "Guild is full.");

            guild.MemberIds.Add(targetId);
        }
        _playerGuildMap[targetId] = guild.GuildId;

        return (true, "Joined guild.");
    }

    public bool DeclineInvite(int targetId, int inviterId)
    {
        if (_pendingInvites.TryGetValue(targetId, out var invite) && invite.InviterId == inviterId)
        {
            return _pendingInvites.TryRemove(targetId, out _);
        }
        return false;
    }

    public bool LeaveGuild(int characterId)
    {
        var guild = GetGuildByMember(characterId);
        if (guild == null) return false;

        lock (guild)
        {
            guild.MemberIds.Remove(characterId);
            _playerGuildMap.TryRemove(characterId, out _);

            if (guild.MemberIds.Count == 0)
            {
                _guilds.TryRemove(guild.GuildId, out _);
            }
            else if (guild.LeaderId == characterId)
            {
                guild.LeaderId = guild.MemberIds.First();
            }
        }

        return true;
    }

    public (bool Success, string Message) KickMember(int requesterId, int targetId)
    {
        if (requesterId == targetId)
            return (false, "You cannot kick yourself. Leave the guild instead.");

        var guild = GetGuildByMember(requesterId);
        if (guild == null)
            return (false, "You are not in a guild.");

        // Permission stub - checking if requester is leader
        if (guild.LeaderId != requesterId)
            return (false, "You do not have permission to kick members.");

        var targetGuild = GetGuildByMember(targetId);
        if (targetGuild == null || targetGuild.GuildId != guild.GuildId)
            return (false, "Target player is not in your guild.");

        lock (guild)
        {
            guild.MemberIds.Remove(targetId);
            _playerGuildMap.TryRemove(targetId, out _);
        }

        return (true, "Member kicked successfully.");
    }
}
