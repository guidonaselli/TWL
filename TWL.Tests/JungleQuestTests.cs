using System.IO;
using System.Linq;
using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests;

public class JungleQuestTests
{
    [Fact]
    public void VerifyJungleQuestsExistAndAreValid()
    {
        // Arrange
        var questManager = new ServerQuestManager();
        // Path might vary depending on where tests run.
        // Assuming running from TWL.Tests/bin/Debug/net8.0/
        // We need to go up 4 levels to root: ../../../../Content/Data/quests.json
        // Or we can try to find it.

        string path = Path.Combine("..", "..", "..", "..", "Content", "Data", "quests.json");
        if (!File.Exists(path))
        {
             // Fallback for different runners
             path = Path.Combine("Content", "Data", "quests.json");
        }

        Assert.True(File.Exists(path), $"Quests file not found at {Path.GetFullPath(path)}");

        // Act
        questManager.Load(path);

        // Assert - Quest 1101
        var q1101 = questManager.GetDefinition(1101);
        Assert.NotNull(q1101);
        Assert.Equal("Into the Green", q1101.Title);
        Assert.Contains(q1101.Objectives, o => o.TargetName == "OldHermit" && o.Type == "Talk");
        Assert.Contains(q1101.Objectives, o => o.TargetName == "JungleEntrance" && o.Type == "Interact");

        // Assert - Quest 1102 (Gather Poison Ivy)
        var q1102 = questManager.GetDefinition(1102);
        Assert.NotNull(q1102);
        Assert.Contains(q1102.Objectives, o => o.TargetName == "PoisonIvyBush" && o.Type == "Collect" && o.RequiredCount == 3);
        Assert.Contains(q1102.Rewards.Items, i => i.ItemId == 7523); // Detox Potion

        // Assert - Quest 1103 (Kill Jaguar)
        var q1103 = questManager.GetDefinition(1103);
        Assert.NotNull(q1103);
        Assert.Contains(q1103.Objectives, o => o.TargetName == "Jaguar" && o.Type == "Kill");
        Assert.Contains(q1103.Rewards.Items, i => i.ItemId == 8003); // Raptor Tooth

        // Assert - Quest 2101 (Sidequest Rare Herbs)
        var q2101 = questManager.GetDefinition(2101);
        Assert.NotNull(q2101);
        Assert.Contains(q2101.Objectives, o => o.TargetName == "RareHerbPatch" && o.Type == "Collect" && o.RequiredCount == 5);
        Assert.Contains(q2101.Rewards.Items, i => i.ItemId == 8002); // Rare Jungle Herb
    }
}
