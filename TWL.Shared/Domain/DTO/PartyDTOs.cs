namespace TWL.Shared.Domain.DTO;

using System;
using System.Collections.Generic;

public class PartyInviteRequest
{
    public string TargetCharacterName { get; set; } = string.Empty;
}

public class PartyInviteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PartyInviteReceivedEvent
{
    public int InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class PartyAcceptInviteRequest
{
    public int InviterId { get; set; }
}

public class PartyDeclineInviteRequest
{
    public int InviterId { get; set; }
}

public class PartyLeaveRequest
{
}

public class PartyKickRequest
{
    public int TargetMemberId { get; set; }
}

public class PartyKickResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PartyMemberDto
{
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxMp { get; set; }
    public int CurrentMp { get; set; }
    public bool IsOnline { get; set; }
}

public class PartyUpdateBroadcast
{
    public int PartyId { get; set; }
    public int LeaderId { get; set; }
    public List<PartyMemberDto> Members { get; set; } = new();
}
