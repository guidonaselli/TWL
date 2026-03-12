using System;

namespace TWL.Shared.Domain.DTO;

public class CharacterRebirthRequest
{
    // Client could optionally send items/catalysts here later
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
}

public class CharacterRebirthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int NewRebirthLevel { get; set; }
    public int StatPointsGained { get; set; }
}

public class RebirthHistoryRecord
{
    public string OperationId { get; set; } = string.Empty;
    public int CharacterId { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldRebirthCount { get; set; }
    public int NewRebirthCount { get; set; }
    public int StatPointsGranted { get; set; }
    public DateTime TimestampUtc { get; set; }
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
}
