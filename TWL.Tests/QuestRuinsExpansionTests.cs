using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests;

public class QuestRuinsExpansionTests
{
    private readonly ServerCharacter _character;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerQuestManager _questManager;

    public QuestRuinsExpansionTests()
    {
        _questManager = new ServerQuestManager();
        _interactionManager = new InteractionManager();
        _character = new ServerCharacter { Id = 1, Name = "Explorer" };
        _questComponent = new PlayerQuestComponent(_questManager);

        // Load Real Data
        // Adjust path to find Content/Data from bin/Debug/net10.0/
        var contentPath = Path.Combine("..", "..", "..", "..", "Content", "Data");
        // If running in CI or specific env, fallback
        if (!Directory.Exists(contentPath))
        {
            contentPath = Path.Combine("Content", "Data");
        }

        _questManager.Load(Path.Combine(contentPath, "quests.json"));
        _interactionManager.Load(Path.Combine(contentPath, "interactions.json"));
    }

    private void SetQuestCompleted(int questId) => _questComponent.QuestStates[questId] = QuestState.Completed;

    [Fact]
    public void SecretsOfTheRuins_Chain_Progression()
    {
        // Setup Prereq: 1304 Completed (Entering Ruins)
        SetQuestCompleted(1304);

        // --- Quest 1305: Dark Corridors ---
        Assert.True(_questComponent.StartQuest(1305), "Should be able to start 1305");

        // Talk to Console
        _questComponent.TryProgress("Talk", "RuinsConsole");

        // Kill Bats and Spider
        // Simulate kills via direct progress update
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "RuinsBat");
        _questComponent.TryProgress("Kill", "GiantSpider");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1305]);
        Assert.True(_questComponent.ClaimReward(1305));

        // --- Quest 1306: Ancient Power ---
        Assert.True(_questComponent.StartQuest(1306), "Should be able to start 1306");

        // Collect Power Crystal
        // Check interaction logic first
        var interactionType = _interactionManager.ProcessInteraction(_character, _questComponent, "PowerCrystalSource");
        Assert.NotNull(interactionType);
        Assert.True(_character.HasItem(8108, 1), "Should have Power Crystal (8108)");

        _questComponent.TryProgress("Collect", "PowerCrystalSource");

        // Talk to Console
        Assert.NotNull(_interactionManager.ProcessInteraction(_character, _questComponent, "RuinsConsole"));
        _questComponent.TryProgress("Talk", "RuinsConsole");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1306]);
        Assert.True(_questComponent.ClaimReward(1306));

        // --- Quest 1307: The Hologram ---
        Assert.True(_questComponent.StartQuest(1307), "Should be able to start 1307");

        // Interact Projector
        _questComponent.TryProgress("Interact", "HologramProjector");

        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[1307]);
    }

    [Fact]
    public void Sidequest_BatWings()
    {
        // Setup Prereq: 1305 Completed
        SetQuestCompleted(1305);

        // Start 2303
        Assert.True(_questComponent.StartQuest(2303), "Should be able to start 2303");

        // Gather 5 Wings
        for (var i = 0; i < 5; i++)
        {
            var result = _interactionManager.ProcessInteraction(_character, _questComponent, "BatNest");
            Assert.NotNull(result);
            _questComponent.TryProgress("Collect", "BatNest");
        }

        Assert.True(_character.HasItem(8106, 5), "Should have 5 Bat Wings (8106)");
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[2303]);
    }
}