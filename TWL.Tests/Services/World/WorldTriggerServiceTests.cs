using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Domain.World.Conditions;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;

namespace TWL.Tests.Services.World;

public class WorldTriggerServiceTests
{
    private readonly Mock<ServerMetrics> _mockMetrics;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly PlayerService _playerService;
    private readonly WorldTriggerService _service;
    private readonly Mock<ITriggerHandler> _mockHandler;

    public WorldTriggerServiceTests()
    {
        _mockMetrics = new Mock<ServerMetrics>();
        _mockRepo = new Mock<IPlayerRepository>();
        _playerService = new PlayerService(_mockRepo.Object, _mockMetrics.Object);
        _service = new WorldTriggerService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<WorldTriggerService>(), _mockMetrics.Object, _playerService);

        _mockHandler = new Mock<ITriggerHandler>();
        _mockHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(true);
        _service.RegisterHandler(_mockHandler.Object);
    }

    [Fact]
    public void OnEnterTrigger_ShouldExecute_WhenNoConditions()
    {
        // Arrange
        var mapId = 1;
        var triggerId = "t1";
        var trigger = new ServerTrigger { Id = triggerId, Type = "Test", Conditions = new List<ITriggerCondition>() };
        var map = new ServerMap { Id = mapId, Triggers = new List<ServerTrigger> { trigger } };
        _service.LoadMaps(new[] { map });

        var character = new ServerCharacter { Id = 1, MapId = mapId };

        // Act
        _service.OnEnterTrigger(character, mapId, triggerId);

        // Assert
        _mockHandler.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once);
    }

    [Fact]
    public void OnEnterTrigger_ShouldNotExecute_WhenLevelConditionNotMet()
    {
        // Arrange
        var mapId = 1;
        var triggerId = "t1";
        var trigger = new ServerTrigger
        {
            Id = triggerId,
            Type = "Test",
            Conditions = new List<ITriggerCondition>
            {
                new LevelCondition(10)
            }
        };
        var map = new ServerMap { Id = mapId, Triggers = new List<ServerTrigger> { trigger } };
        _service.LoadMaps(new[] { map });

        var character = new ServerCharacter { Id = 1, MapId = mapId };
        character.SetLevel(5);

        // Act
        _service.OnEnterTrigger(character, mapId, triggerId);

        // Assert
        _mockHandler.Verify(h => h.ExecuteEnter(It.IsAny<ServerCharacter>(), It.IsAny<ServerTrigger>(), It.IsAny<IWorldTriggerService>()), Times.Never);
    }

    [Fact]
    public void OnEnterTrigger_ShouldExecute_WhenLevelConditionMet()
    {
        // Arrange
        var mapId = 1;
        var triggerId = "t1";
        var trigger = new ServerTrigger
        {
            Id = triggerId,
            Type = "Test",
            Conditions = new List<ITriggerCondition>
            {
                new LevelCondition(10)
            }
        };
        var map = new ServerMap { Id = mapId, Triggers = new List<ServerTrigger> { trigger } };
        _service.LoadMaps(new[] { map });

        var character = new ServerCharacter { Id = 1, MapId = mapId };
        character.SetLevel(15);

        // Act
        _service.OnEnterTrigger(character, mapId, triggerId);

        // Assert
        _mockHandler.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once);
    }

    [Fact]
    public void OnEnterTrigger_ShouldNotExecute_WhenQuestConditionNotMet()
    {
        // Arrange
        var mapId = 1;
        var triggerId = "t1";
        var questId = 100;
        var trigger = new ServerTrigger
        {
            Id = triggerId,
            Type = "Test",
            Conditions = new List<ITriggerCondition>
            {
                new QuestCondition(questId, "Completed")
            }
        };
        var map = new ServerMap { Id = mapId, Triggers = new List<ServerTrigger> { trigger } };
        _service.LoadMaps(new[] { map });

        var character = new ServerCharacter { Id = 1, MapId = mapId };

        // Setup Session
        var session = new TestClientSession();
        session.UserId = 1;
        session.SetCharacter(character);
        var questManager = new ServerQuestManager();
        var questComp = new PlayerQuestComponent(questManager);
        session.SetQuestComponent(questComp);

        _playerService.RegisterSession(session);

        // Act
        _service.OnEnterTrigger(character, mapId, triggerId);

        // Assert
        _mockHandler.Verify(h => h.ExecuteEnter(It.IsAny<ServerCharacter>(), It.IsAny<ServerTrigger>(), It.IsAny<IWorldTriggerService>()), Times.Never);
    }

    [Fact]
    public void OnEnterTrigger_ShouldExecute_WhenQuestConditionMet()
    {
        // Arrange
        var mapId = 1;
        var triggerId = "t1";
        var questId = 100;
        var trigger = new ServerTrigger
        {
            Id = triggerId,
            Type = "Test",
            Conditions = new List<ITriggerCondition>
            {
                new QuestCondition(questId, "Completed")
            }
        };
        var map = new ServerMap { Id = mapId, Triggers = new List<ServerTrigger> { trigger } };
        _service.LoadMaps(new[] { map });

        var character = new ServerCharacter { Id = 1, MapId = mapId };

        // Setup Session
        var session = new TestClientSession();
        session.UserId = 1;
        session.SetCharacter(character);
        var questManager = new ServerQuestManager();
        // Mock quest definition to allow adding it
        var questDef = new QuestDefinition { QuestId = questId, Objectives = new List<ObjectiveDefinition>() };
        questManager.AddQuest(questDef);

        var questComp = new PlayerQuestComponent(questManager);
        // Force state
        questComp.StartQuest(questId);
        questComp.QuestStates[questId] = QuestState.Completed; // Manually set state

        session.SetQuestComponent(questComp);

        _playerService.RegisterSession(session);

        // Act
        _service.OnEnterTrigger(character, mapId, triggerId);

        // Assert
        _mockHandler.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once);
    }

    public class TestClientSession : ClientSession
    {
        public TestClientSession() : base()
        {
        }

        public void SetCharacter(ServerCharacter c) { Character = c; }
        public void SetQuestComponent(PlayerQuestComponent qc) { QuestComponent = qc; }
    }
}
