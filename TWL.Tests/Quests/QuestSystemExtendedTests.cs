using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Server.Architecture.Observability;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Quests;

public class QuestSystemExtendedTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _questComponent;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly ServerMetrics _metrics;

    public QuestSystemExtendedTests()
    {
        _questManager = new ServerQuestManager();
        _questComponent = new PlayerQuestComponent(_questManager);

        // Define Test Quests
        var questA = new QuestDefinition
        {
            QuestId = 100,
            Title = "Choice A",
            Description = "Option A",
            MutualExclusionGroup = "ExclusiveGroup1",
            Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
            Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "A", 1, "Talk to A") },
            FlagsSet = new List<string> { "FLAG_A" }
        };
        var questB = new QuestDefinition
        {
            QuestId = 101,
            Title = "Choice B",
            Description = "Option B",
            MutualExclusionGroup = "ExclusiveGroup1",
            Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
            Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "B", 1, "Talk to B") },
            FlagsSet = new List<string> { "FLAG_B" }
        };
        var questC = new QuestDefinition
        {
            QuestId = 102,
            Title = "Conclusion",
            Description = "After choice",
            RequiredFlags = new List<string> { "FLAG_A" }, // Simplified for test
            Rewards = new RewardDefinition(100, 0, new List<ItemReward>()),
            Objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition("Talk", "C", 1, "Talk to C") }
        };

        _questManager.AddQuest(questA);
        _questManager.AddQuest(questB);
        _questManager.AddQuest(questC);

        _metrics = new ServerMetrics();
        _mockRepo = new Mock<IPlayerRepository>();
        // We cannot mock PlayerService easily if it doesn't have virtual methods or interface.
        // PlayerService is a class. We can use a real instance with mocked repo.
    }

    [Fact]
    public void MutualExclusion_BlocksStartingSecondQuest()
    {
        // Act
        var startA = _questComponent.StartQuest(100);
        var canStartB = _questComponent.CanStartQuest(101);
        var startB = _questComponent.StartQuest(101);

        // Assert
        Assert.True(startA, "Should be able to start Quest A");
        Assert.False(canStartB, "Should NOT be able to start Quest B while A is in progress");
        Assert.False(startB, "StartQuest B should fail");
    }

    [Fact]
    public void QuestTriggerHandler_ProgressesQuest()
    {
        // Setup
        var qDef = new QuestDefinition
        {
            QuestId = 200,
            Title = "Explore Quest",
            Description = "Explore Test",
            Objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition("Explore", "TestZone", 1, "Explore TestZone")
            },
            Rewards = new RewardDefinition(0, 0, new List<ItemReward>())
        };
        _questManager.AddQuest(qDef);

        var playerService = new PlayerService(_mockRepo.Object, _metrics);
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 10, Name = "Explorer" };

        session.SetCharacter(character);
        session.SetQuestComponent(new PlayerQuestComponent(_questManager));
        playerService.RegisterSession(session);

        var handler = new QuestTriggerHandler(playerService);

        // Act - Start Quest
        session.QuestComponent.StartQuest(200);

        // Trigger
        var trigger = new TWL.Server.Domain.World.ServerTrigger
        {
            Id = "TestZone",
            Type = "Explore",
            Properties = new Dictionary<string, string> { { "TargetName", "TestZone" } }
        };

        handler.ExecuteEnter(character, trigger, null);

        // Assert
        Assert.Equal(QuestState.Completed, session.QuestComponent.QuestStates[200]);
    }

    public class ClientSessionForTest : ClientSession
    {
        public ClientSessionForTest()
        {
            UserId = 10;
        }

        public void SetCharacter(ServerCharacter c) => Character = c;
        public void SetQuestComponent(PlayerQuestComponent qc) => QuestComponent = qc;
    }
}
