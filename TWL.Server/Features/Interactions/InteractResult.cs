using TWL.Shared.Domain.Interactions;

namespace TWL.Server.Features.Interactions;

public class InteractResult
{
    public bool Success { get; set; }
    public List<int> UpdatedQuestIds { get; set; } = new();
    public InteractionType? InteractionType { get; set; }
}