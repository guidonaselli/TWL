using System;
using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO;

public class GuildChatMessageDto
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class GuildChatSendRequest
{
    public string Message { get; set; } = string.Empty;
}

public class GuildChatEvent
{
    public GuildChatMessageDto Message { get; set; } = new();
}

public class GuildChatBacklog
{
    public List<GuildChatMessageDto> Messages { get; set; } = new();
}
