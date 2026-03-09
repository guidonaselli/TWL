using System;
using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO
{
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

    public class GuildInviteReceivedEvent
    {
        public int InviterId { get; set; }
        public string InviterName { get; set; } = string.Empty;
        public string GuildName { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
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

    public class GuildMemberDto
    {
        public int CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Rank { get; set; }
        public bool IsOnline { get; set; }
    }

    public class GuildUpdateBroadcast
    {
        public int GuildId { get; set; }
        public string GuildName { get; set; } = string.Empty;
        public int LeaderId { get; set; }
        public List<GuildMemberDto> Members { get; set; } = new();
    }
}
