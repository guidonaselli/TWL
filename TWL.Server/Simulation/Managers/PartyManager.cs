using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TWL.Server.Simulation.Managers;

public class PartyManager : IPartyService
{
    private readonly ConcurrentDictionary<int, Party> _parties = new();
    // CharacterId -> PartyId
    private readonly ConcurrentDictionary<int, int> _playerPartyMap = new();

    // TargetId -> PartyInvite
    // Enforcing 1 pending invite per target to prevent spam
    private readonly ConcurrentDictionary<int, PartyInvite> _pendingInvites = new();

    private int _nextPartyId = 1;

    public const int MaxPartySize = 4;
    public const int InviteTimeoutSeconds = 30;

    public Party? GetParty(int partyId)
    {
        return _parties.TryGetValue(partyId, out var party) ? party : null;
    }

    public Party? GetPartyByMember(int characterId)
    {
        if (_playerPartyMap.TryGetValue(characterId, out var partyId))
        {
            return GetParty(partyId);
        }
        return null;
    }

    public bool IsLeader(int characterId)
    {
        var party = GetPartyByMember(characterId);
        return party?.LeaderId == characterId;
    }

    public (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName)
    {
        if (inviterId == targetId) return (false, "Cannot invite yourself.");

        var inviterParty = GetPartyByMember(inviterId);
        if (inviterParty != null)
        {
            if (inviterParty.LeaderId != inviterId)
                return (false, "Only the party leader can invite members.");

            if (inviterParty.MemberIds.Count >= MaxPartySize)
                return (false, "Party is already full.");
        }

        var targetParty = GetPartyByMember(targetId);
        if (targetParty != null)
            return (false, $"{targetName} is already in a party.");

        // Check if target already has a pending invite
        if (_pendingInvites.TryGetValue(targetId, out var existingInvite))
        {
            if (DateTime.UtcNow < existingInvite.ExpiresAtUtc)
            {
                return (false, $"{targetName} already has a pending party invite.");
            }
        }

        // Register the invite
        _pendingInvites[targetId] = new PartyInvite
        {
            InviterId = inviterId,
            TargetId = targetId,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(InviteTimeoutSeconds)
        };

        return (true, "Invite sent.");
    }

    public (bool Success, string Message) AcceptInvite(int targetId, int inviterId)
    {
        if (!_pendingInvites.TryGetValue(targetId, out var invite))
        {
            return (false, "No pending invite found.");
        }

        if (invite.InviterId != inviterId)
        {
            // This could happen if inviter ID doesn't match the one stored for this target
            return (false, "Invite mismatch.");
        }

        if (DateTime.UtcNow > invite.ExpiresAtUtc)
        {
            _pendingInvites.TryRemove(targetId, out _);
            return (false, "Invite expired.");
        }

        // Cleanup the pending invite
        _pendingInvites.TryRemove(targetId, out _);

        var targetParty = GetPartyByMember(targetId);
        if (targetParty != null)
            return (false, "You are already in a party.");

        var party = GetPartyByMember(inviterId);
        if (party == null)
        {
            // Create a new party
            party = new Party
            {
                PartyId = System.Threading.Interlocked.Increment(ref _nextPartyId),
                LeaderId = inviterId,
            };
            party.MemberIds.Add(inviterId);
            _parties[party.PartyId] = party;
            _playerPartyMap[inviterId] = party.PartyId;
        }

        if (party.LeaderId != inviterId)
            return (false, "Inviter is no longer the party leader.");

        if (party.MemberIds.Count >= MaxPartySize)
            return (false, "Party is full.");

        lock (party)
        {
            if (!party.MemberIds.Contains(targetId))
            {
                party.MemberIds.Add(targetId);
                _playerPartyMap[targetId] = party.PartyId;
            }
        }

        return (true, "Joined party successfully.");
    }

    public bool DeclineInvite(int targetId, int inviterId)
    {
        if (_pendingInvites.TryGetValue(targetId, out var invite) && invite.InviterId == inviterId)
        {
            _pendingInvites.TryRemove(targetId, out _);
            return true;
        }
        return false;
    }

    public bool LeaveParty(int characterId)
    {
        var party = GetPartyByMember(characterId);
        if (party == null) return false;

        lock (party)
        {
            party.MemberIds.Remove(characterId);
            _playerPartyMap.TryRemove(characterId, out _);

            if (party.MemberIds.Count == 0)
            {
                // Disband party if no members left
                _parties.TryRemove(party.PartyId, out _);
            }
            else if (party.LeaderId == characterId)
            {
                // Transfer leadership to the next available member
                // This applies even if only 1 member remains
                party.LeaderId = party.MemberIds.First();
            }
        }

        return true;
    }

    public (bool Success, string Message) KickMember(int leaderId, int targetId, bool isLeaderInCombat, bool isTargetInCombat)
    {
        if (leaderId == targetId) return (false, "Cannot kick yourself. Use leave instead.");

        var party = GetPartyByMember(leaderId);
        if (party == null || party.LeaderId != leaderId)
            return (false, "You are not the leader of a party.");

        if (!party.MemberIds.Contains(targetId))
            return (false, "Target is not in your party.");

        // PTY-09: Kick is disabled during combat and boss fights
        if (isLeaderInCombat || isTargetInCombat)
            return (false, "Cannot kick a member while in combat or a boss fight.");

        lock (party)
        {
            party.MemberIds.Remove(targetId);
            _playerPartyMap.TryRemove(targetId, out _);
        }

        return (true, "Kicked member successfully.");
    }

    private class PartyInvite
    {
        public int InviterId { get; set; }
        public int TargetId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
