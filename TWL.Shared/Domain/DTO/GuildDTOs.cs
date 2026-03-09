using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO;

public class CreateGuildRequest
{
    public string GuildName { get; set; } = string.Empty;
}

public class CreateGuildResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int GuildId { get; set; }
}

public class GuildInviteRequest
{
    public string TargetCharacterName { get; set; } = string.Empty;
}

public class GuildInviteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class GuildInviteReceived
{
    public int InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public string GuildName { get; set; } = string.Empty;
}

public class GuildAcceptInviteRequest
{
    public int InviterId { get; set; }
}

public class GuildDeclineInviteRequest
{
    public int InviterId { get; set; }
}

public class GuildLeaveRequest
{
}

public class GuildKickRequest
{
    public int TargetMemberId { get; set; }
}

public class GuildKickResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class GuildUpdateBroadcast
{
    public int GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
}
