using System.Collections.Generic;
using TWL.Shared.Domain.Guilds;

namespace TWL.Server.Simulation.Managers;

using System.Threading.Tasks;

public interface IGuildService
{
    Task InitializeAsync();

    // Queries
    Guild? GetGuild(int guildId);
    Guild? GetGuildByMember(int characterId);
    bool IsAuthorizedToKick(int guildId, int characterId);

    // Commands
    Task<(bool Success, string Message)> CreateGuild(int leaderId, string leaderName, string guildName);
    (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName);
    Task<(bool Success, string Message)> AcceptInvite(int targetId, int inviterId);
    bool DeclineInvite(int targetId, int inviterId);
    Task<bool> LeaveGuild(int characterId);
    Task<(bool Success, string Message)> KickMember(int kickerId, int targetId);
    Task<(bool Success, string Message)> PromoteMember(int actorId, int targetId);
    Task<(bool Success, string Message)> DemoteMember(int actorId, int targetId);
}
