using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests;

public class QuestRuinsExpansionTests
{
    private ServerQuestManager _questManager;
    private InteractionManager _interactionManager;
    private ServerCharacter _character;
    private PlayerQuestComponent _questComponent;

    public QuestRuinsExpansionTests()
    {
        _questManager = new ServerQuestManager();
        _interactionManager = new InteractionManager();
        _character = new ServerCharacter { Id = 1, Name = "Explorer" };
        _questComponent = new PlayerQuestComponent(_questManager);

        // Load Real Data
        // Adjust path to find Content/Data from bin/Debug/net8.0/
        string contentPath = Path.Combine("..", "..", "..", "..", "Content", "Data");
        // If running in CI or specific env, fallback
        if (!Directory.Exists(contentPath)) contentPath = Path.Combine("Content", "Data");

        _questManager.Load(Path.Combine(contentPath, "quests.json"));
        _interactionManager.Load(Path.Combine(contentPath, "interactions.json"));
    }

    private void SetQuestCompleted(int questId)
    {
        _questComponent.QuestStates[questId] = QuestState.Completed;
    }

    [Fact]
    public void SecretsOfTheRuins_Chain_Progression()
    {
        // Setup Prereq: 1204 Completed (Entering Ruins)
        SetQuestCompleted(1204);

        // --- Quest 1205: Dark Corridors ---
        Assert.True(_questComponent.StartQuest(1205), "Should be able to start 1205");

        // Talk to Console
        _questComponent.TryProgress("Talk", "RuinsConsole");

        // Kill Bats and Spider
        // Simulate kills via direct progress update
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "GiantSpider");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1205]);
        Assert.True(_questComponent.ClaimReward(1205));

        // --- Quest 1206: Ancient Power ---
        Assert.True(_questComponent.StartQuest(1206), "Should be able to start 1206");

        // Collect Power Crystal
        // Check interaction logic first
        var interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "PowerCrystalSource");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8108, 1), "Should have Power Crystal (8108)");

        _questComponent.TryProgress("Collect", "PowerCrystalSource");

        // Talk to Console
        _questComponent.TryProgress("Talk", "RuinsConsole");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1206]);
        Assert.True(_questComponent.ClaimReward(1206));

        // --- Quest 1207: The Hologram ---
        Assert.True(_questComponent.StartQuest(1207), "Should be able to start 1207");

        // Interact Projector
        _questComponent.TryProgress("Interact", "HologramProjector");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1207]);
    }

    [Fact]
    public void Sidequest_BatWings()
    {
        // Setup Prereq: 1205 Completed
        SetQuestCompleted(1205);

        // Start 2203
        Assert.True(_questComponent.StartQuest(2203), "Should be able to start 2203");

        // Gather 5 Wings
        for (int i = 0; i < 5; i++)
        {
            var result = _interactionManager.ProcessInteraction(_character, _questComponent, "BatNest");
            Assert.NotNull(result);
            _questComponent.TryProgress("Collect", "BatNest");
        }

        Assert.True(_character.HasItem(8106, 5), "Should have 5 Bat Wings (8106)");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[2203]);
    }
}
