namespace TWL.Shared.Domain.Quests;

public sealed record ObjectiveDefinition(
    string Type,
    string TargetName,
    int RequiredCount,
    string Description,
    int? DataId = null);

public sealed record ItemReward(int ItemId, int Quantity);

public sealed record ItemRequirement(int ItemId, int Quantity);

public sealed record RewardDefinition(
    int Exp,
    int Gold,
    IReadOnlyList<ItemReward> Items,
    int? PetUnlockId = null,
    int? GrantSkillId = null);

public sealed record InstanceRules(
    string InstanceId,
    int DifficultyLevel,
    int TimeLimitSeconds,
    string CompletionCriteria);

public sealed record QuestDefinition
{
    public required int QuestId { get; init; }
    public required string Title { get; init; }
    public string? TitleKey { get; init; }
    public required string Description { get; init; }
    public string? DescriptionKey { get; init; }
    public IReadOnlyList<int> Requirements { get; init; } = [];
    public required IReadOnlyList<ObjectiveDefinition> Objectives { get; init; }
    public required RewardDefinition Rewards { get; init; }
    public string? OnStartScript { get; init; }
    public string? OnProgressScript { get; init; }
    public string? OnCompleteScript { get; init; }

    public int? ChainId { get; init; }
    public IReadOnlyList<string> FlagsSet { get; init; } = [];
    public IReadOnlyList<string> FlagsClear { get; init; } = [];
    public IReadOnlyList<string> RequiredFlags { get; init; } = [];
    public IReadOnlyList<string> BlockedByFlags { get; init; } = [];

    public int RequiredLevel { get; init; }
    public int RequiredRebirthLevel { get; init; }
    public IReadOnlyDictionary<string, int> RequiredStats { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<ItemRequirement> RequiredItems { get; init; } = [];
    public IReadOnlyList<int> RequiredEquipment { get; init; } = [];
    public int? RequiredPetId { get; init; }

    public QuestRepeatability Repeatability { get; init; } = QuestRepeatability.None;
    public TimeSpan? RepeatCooldown { get; init; }
    public DateTime? Expiry { get; init; }
    public int? TimeLimitSeconds { get; init; }
    public string? PartyRules { get; init; }
    public string? GuildRules { get; init; }
    public InstanceRules? InstanceRules { get; init; }

    // Enhanced properties for Special Skill Quests
    public string Type { get; init; } = "Regular"; // "Regular" | "SpecialSkill"
    public string? SpecialCategory { get; init; } // "RebirthJob" | "ElementSpecial" | "Fairy" | "Dragon"
    public string? MutualExclusionGroup { get; init; }
    public string? AntiAbuseRules { get; init; }

    public IReadOnlyList<QuestFailCondition> FailConditions { get; init; } = [];
    public IReadOnlyList<string> BranchingRules { get; init; } = [];
}