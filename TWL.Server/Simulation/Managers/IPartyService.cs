using System.Collections.Generic;

namespace TWL.Server.Simulation.Managers;

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
}

public class Party
{
    public int PartyId { get; set; }
    public int LeaderId { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
