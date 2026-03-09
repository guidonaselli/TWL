using System;
using System.Collections.Generic;

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
    // More fields for ranks to be added in next plan
}
