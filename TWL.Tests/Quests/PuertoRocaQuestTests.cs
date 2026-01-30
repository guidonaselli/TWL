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

        Assert.NotNull(questManager.GetDefinition(1100));
        Assert.NotNull(questManager.GetDefinition(1101));
        Assert.NotNull(questManager.GetDefinition(1102));
        Assert.NotNull(questManager.GetDefinition(1103));
        Assert.NotNull(questManager.GetDefinition(1104));
    }

    [Fact]
    public void VerifyQuestFlowAndRewards()
    {
        // Setup
        var questManager = new ServerQuestManager();
        questManager.Load(GetContentPath("quests.json"));

        var petManager = new PetManager(); // Empty is fine

        var component = new PlayerQuestComponent(questManager, petManager);
        var character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
        character.AddExp(10000); // Ensure level reqs met
        component.Character = character;

        // Mock requirements: 1100 requires 1004. Let's force complete 1004 or mock state.
        component.QuestStates[1004] = QuestState.RewardClaimed;

        // 1. Start Quest 1100
        Assert.True(component.CanStartQuest(1100), "Should be able to start 1100");
        Assert.True(component.StartQuest(1100), "Should start quest 1100");
        Assert.Equal(QuestState.InProgress, component.QuestStates[1100]);

        // 2. Progress (Talk to Caravan Leader)
        var updated = component.TryProgress("Talk", "Caravan Leader");
        Assert.Contains(1100, updated);
        Assert.Equal(QuestState.Completed, component.QuestStates[1100]);
        Assert.True(component.ClaimReward(1100));

        // 3. Start 1101 (Requires 1100)
        Assert.True(component.StartQuest(1101));

        // Progress 1101 (Interact Sendero Norte)
        updated = component.TryProgress("Interact", "Sendero Norte");
        Assert.Contains(1101, updated);
        Assert.True(component.ClaimReward(1101));

        // 4. Start 1102 (Requires 1101)
        Assert.True(component.StartQuest(1102));

        // Progress 1102 (Kill Bandido x2)
        component.TryProgress("Kill", "Bandido del Camino", 2);
        Assert.Equal(QuestState.Completed, component.QuestStates[1102]);
        Assert.True(component.ClaimReward(1102));

        // 5. Start 1103 (Requires 1102)
        Assert.True(component.StartQuest(1103));
        component.TryProgress("Interact", "Puerta de la Ciudad");
        Assert.True(component.ClaimReward(1103));

        // 6. Start 1104 (Requires 1103)
        Assert.True(component.StartQuest(1104));
        component.TryProgress("Talk", "Funcionario de Registro");
        Assert.True(component.ClaimReward(1104));

        // Verify Rewards (1104 gives 100 Gold)
        // 1100: 0 Gold
        // 1101: 0 Gold
        // 1102: 50 Gold
        // 1103: 0 Gold
        // 1104: 100 Gold
        // Total: 150 Gold
        Assert.Equal(150, character.Gold);
    }
}
