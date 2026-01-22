using System;
using System.Collections.Generic;

namespace TWL.Shared.Domain.Quests;

public sealed record ObjectiveDefinition(
    string Type,
    string TargetName,
    int    RequiredCount,
    string Description);

public sealed record ItemReward(int ItemId, int Quantity);

public sealed record RewardDefinition(
    int                      Exp,
    int                      Gold,
    IReadOnlyList<ItemReward> Items);

public sealed record QuestDefinition
{
    public required int QuestId                     { get; init; }
    public required string Title                    { get; init; }
    public required string Description              { get; init; }
    public IReadOnlyList<int> Requirements          { get; init; } = [];
    public required IReadOnlyList<ObjectiveDefinition> Objectives { get; init; }
    public required RewardDefinition Rewards        { get; init; }
    public string? OnStartScript    { get; init; }
    public string? OnProgressScript { get; init; }
    public string? OnCompleteScript { get; init; }
}
