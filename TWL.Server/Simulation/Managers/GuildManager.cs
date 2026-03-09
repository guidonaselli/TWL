using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;
using TWL.Server.Services;

namespace TWL.Server.Simulation.Managers;

// Represents the server-side state of a Guild
public class Guild
{
    public int GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public HashSet<int> MemberIds { get; set; } = new();
}

public class GuildManager : IGuildService
{
    public const long CreationFee = 50000;
    public const int InviteTimeoutSeconds = 60;

    private readonly ConcurrentDictionary<int, Guild> _guilds = new();
    private readonly ConcurrentDictionary<int, int> _memberToGuild = new();

    // TargetId -> InviterId
    private readonly ConcurrentDictionary<int, int> _pendingInvites = new();
    private readonly ConcurrentDictionary<int, DateTime> _inviteExpirations = new();

    private int _nextGuildId = 1;

    private readonly IEconomyService _economyManager;

    public GuildManager(IEconomyService economyManager)
    {
        _economyManager = economyManager;
    }

    public CreateGuildResponse CreateGuild(ServerCharacter leader, string guildName, long currentGold)
    {
        if (string.IsNullOrWhiteSpace(guildName) || guildName.Length > 20)
        {
            return new CreateGuildResponse { Success = false, Message = "Invalid guild name." };
        }

        if (_memberToGuild.ContainsKey(leader.Id))
        {
            return new CreateGuildResponse { Success = false, Message = "You are already in a guild." };
        }

        if (currentGold < CreationFee)
        {
            return new CreateGuildResponse { Success = false, Message = "Insufficient gold to create a guild." };
        }

        lock (_guilds)
        {
            // Case-insensitive unique name check
            if (_guilds.Values.Any(g => g.GuildName.Equals(guildName, StringComparison.OrdinalIgnoreCase)))
            {
                return new CreateGuildResponse { Success = false, Message = "Guild name already exists." };
            }

            if (!_economyManager.TryDeductGold(leader, CreationFee, "GuildCreation", Guid.NewGuid().ToString()))
            {
                return new CreateGuildResponse { Success = false, Message = "Failed to deduct gold." };
            }

            var guildId = _nextGuildId++;
            var guild = new Guild
            {
                GuildId = guildId,
                GuildName = guildName,
                LeaderId = leader.Id,
            };
            guild.MemberIds.Add(leader.Id);

            _guilds[guildId] = guild;
            _memberToGuild[leader.Id] = guildId;

            return new CreateGuildResponse { Success = true, GuildId = guildId };
        }
    }

    public GuildInviteResponse InviteMember(int inviterId, string inviterName, int targetId, string targetName)
    {
        if (!_memberToGuild.TryGetValue(inviterId, out var guildId))
        {
            return new GuildInviteResponse { Success = false, Message = "You are not in a guild." };
        }

        if (_memberToGuild.ContainsKey(targetId))
        {
            return new GuildInviteResponse { Success = false, Message = "Target player is already in a guild." };
        }

        // Enforce only one pending invite per target
        if (_pendingInvites.ContainsKey(targetId))
        {
            if (_inviteExpirations.TryGetValue(targetId, out var expiration) && DateTime.UtcNow < expiration)
            {
                return new GuildInviteResponse { Success = false, Message = "Player already has a pending invite." };
            }
            // Cleanup expired invite
            _pendingInvites.TryRemove(targetId, out _);
            _inviteExpirations.TryRemove(targetId, out _);
        }

        var guild = GetGuild(guildId);
        if (guild == null || guild.LeaderId != inviterId)
        {
             return new GuildInviteResponse { Success = false, Message = "You do not have permission to invite." };
        }

        _pendingInvites[targetId] = inviterId;
        _inviteExpirations[targetId] = DateTime.UtcNow.AddSeconds(InviteTimeoutSeconds);

        return new GuildInviteResponse { Success = true };
    }

    public bool AcceptInvite(int targetId, int inviterId)
    {
        if (_memberToGuild.ContainsKey(targetId)) return false;

        if (_pendingInvites.TryGetValue(targetId, out var expectedInviterId) && expectedInviterId == inviterId)
        {
             if (_inviteExpirations.TryGetValue(targetId, out var expiration) && DateTime.UtcNow > expiration)
             {
                 _pendingInvites.TryRemove(targetId, out _);
                 _inviteExpirations.TryRemove(targetId, out _);
                 return false;
             }

             _pendingInvites.TryRemove(targetId, out _);
             _inviteExpirations.TryRemove(targetId, out _);

             if (_memberToGuild.TryGetValue(inviterId, out var guildId))
             {
                 var guild = GetGuild(guildId);
                 if (guild != null)
                 {
                     lock (guild)
                     {
                         guild.MemberIds.Add(targetId);
                         _memberToGuild[targetId] = guildId;
                         return true;
                     }
                 }
             }
        }
        return false;
    }

    public bool DeclineInvite(int targetId, int inviterId)
    {
        if (_pendingInvites.TryGetValue(targetId, out var expectedInviterId) && expectedInviterId == inviterId)
        {
            _pendingInvites.TryRemove(targetId, out _);
            _inviteExpirations.TryRemove(targetId, out _);
            return true;
        }
        return false;
    }

    public bool LeaveGuild(int memberId)
    {
        if (_memberToGuild.TryGetValue(memberId, out var guildId))
        {
            var guild = GetGuild(guildId);
            if (guild != null)
            {
                lock (guild)
                {
                    if (guild.LeaderId == memberId)
                    {
                        // Transfer leadership or disband? Basic foundation: disband or pass to first available.
                        guild.MemberIds.Remove(memberId);
                        _memberToGuild.TryRemove(memberId, out _);

                        if (guild.MemberIds.Count > 0)
                        {
                            guild.LeaderId = guild.MemberIds.First();
                        }
                        else
                        {
                            _guilds.TryRemove(guildId, out _);
                        }
                        return true;
                    }
                    else
                    {
                        guild.MemberIds.Remove(memberId);
                        _memberToGuild.TryRemove(memberId, out _);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public GuildKickResponse KickMember(int kickerId, int targetMemberId)
    {
        if (kickerId == targetMemberId)
        {
             return new GuildKickResponse { Success = false, Message = "You cannot kick yourself." };
        }

        if (!_memberToGuild.TryGetValue(targetMemberId, out var targetGuildId))
        {
            return new GuildKickResponse { Success = false, Message = "Target not found in your guild." };
        }

        if (_memberToGuild.TryGetValue(kickerId, out var guildId))
        {
            if (guildId != targetGuildId)
            {
                return new GuildKickResponse { Success = false, Message = "Target not found in your guild." };
            }

            var guild = GetGuild(guildId);
            if (guild == null) return new GuildKickResponse { Success = false, Message = "Guild not found." };

            if (guild.LeaderId != kickerId)
            {
                return new GuildKickResponse { Success = false, Message = "You do not have permission to kick." };
            }

            lock (guild)
            {
                if (!guild.MemberIds.Contains(targetMemberId))
                {
                    return new GuildKickResponse { Success = false, Message = "Target not found in your guild." };
                }

                guild.MemberIds.Remove(targetMemberId);
                _memberToGuild.TryRemove(targetMemberId, out _);
                return new GuildKickResponse { Success = true };
            }
        }

        return new GuildKickResponse { Success = false, Message = "You are not in a guild." };
    }

    public Guild? GetGuildByMember(int memberId)
    {
        if (_memberToGuild.TryGetValue(memberId, out var guildId))
        {
            return GetGuild(guildId);
        }
        return null;
    }

    public Guild? GetGuild(int guildId)
    {
        if (_guilds.TryGetValue(guildId, out var guild))
        {
            return guild;
        }
        return null;
    }
}
