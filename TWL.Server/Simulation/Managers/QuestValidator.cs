using TWL.Shared.Domain.Quests;

namespace TWL.Server.Simulation.Managers;

public static class QuestValidator
{
    // Normalized set of valid objective types
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Kill", "KillCount",
        "Collect", "CollectItem",
        "Deliver", "DeliverItem",
        "Talk", "Interact",
        "Explore", "Visit", "Reach",
        "Craft", "Compound", "Forge",
        "Party", "Guild",
        "Instance",
        "PvP",
        "EventParticipation",
        "Escort", "Protect",
        "Puzzle", "Sequence",
        "Capture",
        "PetAcquired", "Trade",
        "PayGold",
        "UseItem"
    };

    public static List<string> Validate(IEnumerable<QuestDefinition> quests,
        IReadOnlyDictionary<int, QuestDefinition>? externalContext = null)
    {
        var errors = new List<string>();
        // Check for duplicates first
        var questIds = new HashSet<int>();
        var definitions = new Dictionary<int, QuestDefinition>();

        foreach (var quest in quests)
        {
            if (!questIds.Add(quest.QuestId))
            {
                errors.Add($"Duplicate QuestId found: {quest.QuestId}");
            }
            else
            {
                definitions[quest.QuestId] = quest;
            }
        }

        // If strict duplicates exist, we can still validate the unique ones,
        // but it's better to just proceed with the unique set for reference checks.

        foreach (var quest in definitions.Values)
        {
            ValidateQuest(quest, definitions, errors, externalContext);
        }

        return errors;
    }

    private static void ValidateQuest(QuestDefinition quest, Dictionary<int, QuestDefinition> lookup,
        List<string> errors, IReadOnlyDictionary<int, QuestDefinition>? externalContext = null)
    {
        // 1. Basic Metadata
        if (string.IsNullOrWhiteSpace(quest.Title))
        {
            errors.Add($"Quest {quest.QuestId}: Title is missing.");
        }

        if (string.IsNullOrWhiteSpace(quest.Description))
        {
            errors.Add($"Quest {quest.QuestId}: Description is missing.");
        }

        if (quest.TimeLimitSeconds.HasValue && quest.TimeLimitSeconds.Value <= 0)
        {
            errors.Add($"Quest {quest.QuestId}: TimeLimitSeconds must be positive.");
        }

        // 2. Requirements (Prerequisites)
        if (quest.Requirements != null)
        {
            foreach (var reqId in quest.Requirements)
            {
                var existsLocally = lookup.ContainsKey(reqId);
                var existsExternally = externalContext != null && externalContext.ContainsKey(reqId);

                if (!existsLocally && !existsExternally)
                {
                    errors.Add($"Quest {quest.QuestId}: Prerequisite quest {reqId} does not exist.");
                }
                else if (reqId == quest.QuestId)
                {
                    errors.Add($"Quest {quest.QuestId}: Cannot require itself.");
                }
            }
        }

        // 3. Chain ID
        if (quest.ChainId.HasValue)
        {
            var existsLocally = lookup.ContainsKey(quest.ChainId.Value);
            var existsExternally = externalContext != null && externalContext.ContainsKey(quest.ChainId.Value);

            if (!existsLocally && !existsExternally)
            {
                errors.Add($"Quest {quest.QuestId}: ChainId {quest.ChainId} does not exist.");
            }
        }

        // 4. Objectives
        if (quest.Objectives == null || quest.Objectives.Count == 0)
        {
            errors.Add($"Quest {quest.QuestId}: Must have at least one objective.");
        }
        else
        {
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var obj = quest.Objectives[i];
                ValidateObjective(quest.QuestId, i, obj, errors);
            }
        }

        // 5. Rewards
        if (quest.Rewards != null)
        {
            ValidateRewards(quest.QuestId, quest.Rewards, errors);
        }
    }

    private static void ValidateObjective(int questId, int index, ObjectiveDefinition obj, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(obj.Type))
        {
            errors.Add($"Quest {questId} Obj {index}: Type is missing.");
            return;
        }

        if (!ValidTypes.Contains(obj.Type))
        {
            errors.Add($"Quest {questId} Obj {index}: Unknown type '{obj.Type}'.");
        }

        if (string.IsNullOrWhiteSpace(obj.TargetName) && !obj.DataId.HasValue)
        {
            // Enforce TargetName OR DataId
            errors.Add($"Quest {questId} Obj {index}: TargetName or DataId is missing.");
        }

        if (obj.RequiredCount <= 0)
        {
            errors.Add($"Quest {questId} Obj {index}: RequiredCount must be > 0.");
        }

        // Specific Type Rules
        // 'Deliver' requires DataId (ItemId)
        if (obj.Type.Equals("Deliver", StringComparison.OrdinalIgnoreCase) ||
            obj.Type.Equals("DeliverItem", StringComparison.OrdinalIgnoreCase))
        {
            if (!obj.DataId.HasValue)
            {
                errors.Add($"Quest {questId} Obj {index}: 'Deliver' requires DataId (ItemId).");
            }
        }
    }

    private static void ValidateRewards(int questId, RewardDefinition rewards, List<string> errors)
    {
        if (rewards.Exp < 0)
        {
            errors.Add($"Quest {questId}: Exp reward cannot be negative.");
        }

        if (rewards.Gold < 0)
        {
            errors.Add($"Quest {questId}: Gold reward cannot be negative.");
        }

        if (rewards.Items != null)
        {
            foreach (var item in rewards.Items)
            {
                if (item.ItemId <= 0)
                {
                    errors.Add($"Quest {questId}: Invalid ItemReward ID {item.ItemId}.");
                }

                if (item.Quantity <= 0)
                {
                    errors.Add($"Quest {questId}: Invalid ItemReward Quantity {item.Quantity}.");
                }
            }
        }
    }
}