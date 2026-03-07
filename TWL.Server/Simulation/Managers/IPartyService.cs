using System.Collections.Generic;

namespace TWL.Server.Simulation.Managers;

using TWL.Shared.Domain.Party;

public interface IPartyService
{
    // Queries
    Party? GetParty(int partyId);
    Party? GetPartyByMember(int characterId);
    bool IsLeader(int characterId);

    // Commands
    (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName);
    (bool Success, string Message) AcceptInvite(int targetId, int inviterId);
    bool DeclineInvite(int targetId, int inviterId);
    bool LeaveParty(int characterId);
    (bool Success, string Message) KickMember(int leaderId, int targetId, bool isLeaderInCombat, bool isTargetInCombat);
    (bool Success, string Message) UpdateMemberPosition(int partyId, int characterId, int targetX, int targetY);
}

public class Party
{
    public int PartyId { get; set; }
    public int LeaderId { get; set; }
    public List<int> MemberIds { get; set; } = new();
    public int NextLootMemberIndex { get; set; }
    public TacticalFormation Formation { get; set; } = new();
}
