namespace TWL.Shared.Domain.DTO;

using System;

public class PartyChatMessage
{
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class PartyChatRequest
{
    public string Content { get; set; } = string.Empty;
}
