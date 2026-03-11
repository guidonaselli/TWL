using System;
using System.Collections.Generic;
using TWL.Shared.Domain.Guilds;

namespace TWL.Shared.Domain.DTO;

public class CreateGuildRequest
{
    public string GuildName { get; set; } = string.Empty;
}

public class GuildInviteRequest
{
    public string TargetName { get; set; } = string.Empty;
}

public class GuildAcceptRequest
{
    public int GuildId { get; set; }
}

public class GuildDeclineRequest
{
    public int GuildId { get; set; }
}

public class GuildLeaveRequest
{
}

public class GuildKickRequest
{
    public int TargetId { get; set; }
}

public class GuildUpdateBroadcast
{
    public int GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public List<GuildMemberDto> Members { get; set; } = new();
}

public class GuildMemberDto
{
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsOnline { get; set; }
    public GuildRank Rank { get; set; }
    public DateTime LastLoginUtc { get; set; }
}

public class GuildSetRankRequestDto
{
    public int TargetMemberId { get; set; }
}
