using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;
using System.Collections.Generic;

namespace TWL.Tests.Quests;

public class PuertoRocaQuestTests
{
    private string GetContentPath(string filename)
    {
        // Try standard test runner location first
        string path = Path.Combine("..", "..", "..", "..", "Content", "Data", filename);
        if (File.Exists(path)) return path;

        // Try current directory
        path = Path.Combine("Content", "Data", filename);
        if (File.Exists(path)) return path;

        // Try absolute path resolution relative to repo root (assuming we are in a subfolder)
        // This is a bit hacky but works for different environments
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var target = Path.Combine(current.FullName, "Content", "Data", filename);
            if (File.Exists(target)) return target;
            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find {filename}");
    }

    [Fact]
    public void VerifyPuertoRocaQuestsExist()
    {
        var questManager = new ServerQuestManager();
        questManager.Load(GetContentPath("quests.json"));

        Assert.NotNull(questManager.GetDefinition(4000));
        Assert.NotNull(questManager.GetDefinition(4001));
        Assert.NotNull(questManager.GetDefinition(4002));
        Assert.NotNull(questManager.GetDefinition(4003));
        Assert.NotNull(questManager.GetDefinition(4004));
    }

    [Fact]
    public void VerifyQuestFlowAndRewards()
    {
        // Setup
        var questManager = new ServerQuestManager();
        questManager.Load(GetContentPath("quests.json"));

        var petManager = new PetManager(); // Empty is fine as we don't test pet rewards here

        var component = new PlayerQuestComponent(questManager, petManager);
        var character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        component.Character = character;

        // 1. Start Quest 4000
        Assert.True(component.StartQuest(4000), "Should start quest 4000");
        Assert.Equal(QuestState.InProgress, component.QuestStates[4000]);

        // 2. Progress (Talk to PortMaster)
        var updated = component.TryProgress("Talk", "PortMaster");
        Assert.Contains(4000, updated);
        Assert.Equal(QuestState.Completed, component.QuestStates[4000]);

        // 3. Claim Reward
        Assert.True(component.ClaimReward(4000), "Should claim reward");
        Assert.Equal(QuestState.RewardClaimed, component.QuestStates[4000]);

        // Verify Rewards (Exp 50, Gold 10)
        Assert.Equal(50, character.Exp);
        Assert.Equal(10, character.Gold);

        // 4. Start Quest 4001 (Requires 4000)
        Assert.True(component.StartQuest(4001), "Should start quest 4001");

        // 5. Progress 4001 (Collect 5 Coconuts, 2 Fresh Water)
        component.TryProgress("Collect", "Coconut", 5);
        component.TryProgress("Collect", "FreshWater", 2);

        Assert.Equal(QuestState.Completed, component.QuestStates[4001]);

        // 6. Claim Reward 4001
        component.ClaimReward(4001);

        // Verify Rewards (Exp 100, Gold 20 -> Total 150, 30. Item 101 x2)
        // Note: Character.Exp is current level exp.
        // Start: Lvl 1, 0/100 Exp.
        // Quest 4000: +50 Exp -> Lvl 1, 50/100.
        // Quest 4001: +100 Exp -> Total 150.
        // Level Up! 150 - 100 = 50. Level 2.

        Assert.Equal(2, character.Level);
        Assert.Equal(50, character.Exp);
        Assert.Equal(30, character.Gold);
        Assert.True(character.HasItem(101, 2));

        // 7. Test Idempotency (Claiming again should fail or do nothing)
        Assert.False(component.ClaimReward(4001), "Should not claim reward again");
        Assert.Equal(50, character.Exp); // Exp should not increase
    }
}
