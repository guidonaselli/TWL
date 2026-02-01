namespace TWL.Server.Features.Interactions;

public class InteractResult
{
    public bool Success { get; set; }
    public List<int> UpdatedQuestIds { get; set; } = new();
}