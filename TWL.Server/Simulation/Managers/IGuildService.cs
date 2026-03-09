using System.Collections.Generic;

namespace TWL.Server.Simulation.Managers;

public interface IGuildService
{
    (bool Success, string Message, int GuildId) CreateGuild(int leaderId, string leaderName, string guildName);
    (bool Success, string Message) InviteMember(int inviterId, string inviterName, int targetId, string targetName);
    (bool Success, string Message) AcceptInvite(int targetId, int inviterId);
    bool DeclineInvite(int targetId, int inviterId);
    bool LeaveGuild(int characterId);
    (bool Success, string Message) KickMember(int requesterId, int targetId);

    Guild? GetGuild(int guildId);
    Guild? GetGuildByMember(int characterId);
}

public class Guild
{
    public int GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public HashSet<int> MemberIds { get; set; } = new();
}

public class GuildInvite
{
    public int InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public System.DateTime ExpiresAt { get; set; }
}
