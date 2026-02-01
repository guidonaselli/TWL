using TWL.Shared.Domain.Quests;

namespace TWL.Shared.Domain.Interactions;

public sealed record InteractionDefinition
{
    public required string TargetName { get; init; }
    public required string Type { get; init; } // "Gather", "Craft"
    public List<ItemReward> RequiredItems { get; init; } = new();
    public List<ItemReward> RewardItems { get; init; } = new();
    public int? RequiredQuestId { get; init; }
    public bool ConsumeRequiredItems { get; init; } = true;
}