using System.Collections.Generic;

namespace TWL.Server.Simulation.Managers;

public interface IGuildService
{
    // Queries
    Guild? GetGuild(int guildId);
    Guild? GetGuildByMember(int characterId);
    bool IsAuthorizedToKick(int guildId, int characterId);

    // Commands
    (bool Success, string Message) CreateGuild(int leaderId, string leaderName, string guildName);
    (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName);
    (bool Success, string Message) AcceptInvite(int targetId, int inviterId);
    bool DeclineInvite(int targetId, int inviterId);
    bool LeaveGuild(int characterId);
    (bool Success, string Message) KickMember(int kickerId, int targetId);
}

public class Guild
{
    public int GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
