using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Quests;
using Xunit;

namespace TWL.Tests.Quests;

public class QuestValidationTests
{
    [Fact]
    public void Validate_ValidQuest_ReturnsNoErrors()
    {
        var q = new QuestDefinition
        {
            QuestId = 1,
            Title = "Test Quest",
            Description = "Description",
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("Talk", "Target", 1, "Desc", null)
            },
            Rewards = new RewardDefinition(10, 10, new List<ItemReward>())
        };

        var errors = QuestValidator.Validate(new[] { q });
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DuplicateId_ReturnsError()
    {
        var q1 = new QuestDefinition
        {
            QuestId = 1,
            Title = "Q1",
            Description = "D1",
            Objectives = new List<ObjectiveDefinition> { new("Talk", "T", 1, "D", null) },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };
        var q2 = new QuestDefinition
        {
            QuestId = 1, // Duplicate
            Title = "Q2",
            Description = "D2",
            Objectives = new List<ObjectiveDefinition> { new("Talk", "T", 1, "D", null) },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };

        var errors = QuestValidator.Validate(new[] { q1, q2 });
        Assert.Contains(errors, e => e.Contains("Duplicate QuestId"));
    }

    [Fact]
    public void Validate_MissingRequirement_ReturnsError()
    {
        var q = new QuestDefinition
        {
            QuestId = 1,
            Title = "Q1",
            Description = "D1",
            Requirements = new List<int> { 999 }, // Missing
            Objectives = new List<ObjectiveDefinition> { new("Talk", "T", 1, "D", null) },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };

        var errors = QuestValidator.Validate(new[] { q });
        Assert.Contains(errors, e => e.Contains("Prerequisite quest 999 does not exist"));
    }

    [Fact]
    public void Validate_InvalidObjectiveType_ReturnsError()
    {
        var q = new QuestDefinition
        {
            QuestId = 1,
            Title = "Q1",
            Description = "D1",
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("InvalidType", "T", 1, "D", null)
            },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };

        var errors = QuestValidator.Validate(new[] { q });
        Assert.Contains(errors, e => e.Contains("Unknown type 'InvalidType'"));
    }

    [Fact]
    public void Validate_DeliverWithoutDataId_ReturnsError()
    {
        var q = new QuestDefinition
        {
            QuestId = 1,
            Title = "Q1",
            Description = "D1",
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("Deliver", "Target", 1, "Desc", null) // No DataId
            },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };

        var errors = QuestValidator.Validate(new[] { q });
        Assert.Contains(errors, e => e.Contains("'Deliver' requires DataId"));
    }

    [Fact]
    public void Validate_ProductionData_IsValid()
    {
        // Locate Content/Data/quests.json
        // Strategy: Walk up until we find Content/Data/quests.json
        var currentDir = Directory.GetCurrentDirectory();
        string? questsPath = null;

        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Content", "Data", "quests.json");
            if (File.Exists(candidate))
            {
                questsPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.True(questsPath != null, "Could not find Content/Data/quests.json from " + currentDir);

        // Parse and validate
        var json = File.ReadAllText(questsPath);
        var quests = System.Text.Json.JsonSerializer.Deserialize<List<QuestDefinition>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(quests);

        var errors = QuestValidator.Validate(quests);
        if (errors.Count > 0)
        {
            // Dump errors to help debugging
            foreach (var err in errors) Console.WriteLine(err);
            throw new Exception("Production Data Validation Failed:\n" + string.Join("\n", errors));
        }
        Assert.Empty(errors);
    }
}
