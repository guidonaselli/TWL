namespace TWL.Shared.Domain.Quests;

public sealed record QuestFailCondition(
    string Type,
    string Value,
    string Description);