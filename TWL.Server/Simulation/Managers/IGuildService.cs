using System.Collections.Generic;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation.Managers;

public interface IGuildService
{
    CreateGuildResponse CreateGuild(ServerCharacter leader, string guildName, long currentGold);
    GuildInviteResponse InviteMember(int inviterId, string inviterName, int targetId, string targetName);
    bool AcceptInvite(int targetId, int inviterId);
    bool DeclineInvite(int targetId, int inviterId);
    bool LeaveGuild(int memberId);
    GuildKickResponse KickMember(int kickerId, int targetMemberId);

    // Read operations for broadcasting
    Guild? GetGuildByMember(int memberId);
    Guild? GetGuild(int guildId);
}
