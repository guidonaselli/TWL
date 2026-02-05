using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Domain.World.Conditions;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Services.World;

public class TestClientSession : ClientSession
{
    public TestClientSession() : base()
    {
    }

    public TestClientSession(ServerCharacter character) : base()
    {
        Character = character;
    }

    public void SetQuestComponent(PlayerQuestComponent qc)
    {
        QuestComponent = qc;
    }
}

public class WorldTriggerServiceTests
{
    private readonly Mock<IWorldScheduler> _schedulerMock;
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly Mock<ServerMetrics> _metricsMock;
    private readonly Mock<ILogger<WorldTriggerService>> _loggerMock;
    private readonly WorldTriggerService _service;

    public WorldTriggerServiceTests()
    {
        _schedulerMock = new Mock<IWorldScheduler>();

        var repoMock = new Mock<IPlayerRepository>();
        var psLogger = new Mock<ILogger<PlayerService>>();
        _metricsMock = new Mock<ServerMetrics>();

        _playerServiceMock = new Mock<PlayerService>(repoMock.Object, _metricsMock.Object);
        _loggerMock = new Mock<ILogger<WorldTriggerService>>();

        _service = new WorldTriggerService(_loggerMock.Object, _metricsMock.Object, _playerServiceMock.Object, _schedulerMock.Object);
    }

    [Fact]
    public void Start_ShouldScheduleTimerTriggers()
    {
        // Arrange
        var map = new ServerMap { Id = 1 };
        var trigger = new ServerTrigger
        {
            Id = "timer1",
            Type = "TestTimer",
            ActivationType = TriggerActivationType.Timer,
            IntervalMs = 1000
        };
        map.Triggers.Add(trigger);

        _service.LoadMaps(new[] { map });

        // Act
        _service.Start();

        // Assert
        _schedulerMock.Verify(s => s.ScheduleRepeating(It.IsAny<Action>(), TimeSpan.FromMilliseconds(1000), It.Is<string>(n => n.Contains("timer1"))), Times.Once);
    }

    [Fact]
    public void ExecuteTick_ShouldInvokeHandler()
    {
        // Arrange
        var map = new ServerMap { Id = 1 };
        var trigger = new ServerTrigger
        {
            Id = "timer1",
            Type = "TestTimer",
            ActivationType = TriggerActivationType.Timer,
            IntervalMs = 1000
        };
        map.Triggers.Add(trigger);

        _service.LoadMaps(new[] { map });

        Action? scheduledAction = null;
        _schedulerMock.Setup(s => s.ScheduleRepeating(It.IsAny<Action>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
            .Callback<Action, TimeSpan, string>((act, _, _) => scheduledAction = act);

        var handlerMock = new Mock<ITriggerHandler>();
        handlerMock.Setup(h => h.CanHandle("TestTimer")).Returns(true);
        _service.RegisterHandler(handlerMock.Object);

        _service.Start();

        // Act
        Assert.NotNull(scheduledAction);
        scheduledAction.Invoke();

        // Assert
        handlerMock.Verify(h => h.ExecuteTick(trigger, 1, _service), Times.Once);
    }

    [Fact]
    public void GetPlayersInTrigger_ShouldFilterPlayers()
    {
        // Arrange
        var trigger = new ServerTrigger
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10 // Covers 10,10 to 20,20
        };

        var p1 = new ServerCharacter { Id = 1, MapId = 1, X = 15, Y = 15 }; // Inside
        var p2 = new ServerCharacter { Id = 2, MapId = 1, X = 5, Y = 5 };   // Outside
        var p3 = new ServerCharacter { Id = 3, MapId = 2, X = 15, Y = 15 }; // Wrong Map

        var s1 = new TestClientSession(p1);
        var s2 = new TestClientSession(p2);
        var s3 = new TestClientSession(p3);

        _playerServiceMock.Setup(ps => ps.GetSessions(It.IsAny<List<ClientSession>>(), It.IsAny<Func<ClientSession, bool>>()))
            .Callback<List<ClientSession>, Func<ClientSession, bool>>((list, filter) =>
            {
                if (filter(s1)) list.Add(s1);
                if (filter(s2)) list.Add(s2);
                if (filter(s3)) list.Add(s3);
            });

        // Act
        var players = _service.GetPlayersInTrigger(trigger, 1).ToList();

        // Assert
        Assert.Contains(p1, players);
        Assert.DoesNotContain(p2, players);
        Assert.DoesNotContain(p3, players);
    }

    [Fact]
    public void DamageTriggerHandler_ShouldDamagePlayer()
    {
        // Arrange
        _playerServiceMock.Setup(ps => ps.GetSession(It.IsAny<int>())).Returns((ClientSession?)null);

        var handler = new DamageTriggerHandler(new Mock<ILogger<DamageTriggerHandler>>().Object, _playerServiceMock.Object);
        var trigger = new ServerTrigger
        {
            Type = "DamageRegion",
            Properties = new Dictionary<string, string> { { "DamageAmount", "10" } }
        };
        var p1 = new ServerCharacter { Id = 1, Hp = 100 };
        var serviceMock = new Mock<IWorldTriggerService>();

        // Setup Session
        var session = new TestClientSession(p1);
        session.UserId = 1;

        _playerServiceMock.Setup(ps => ps.GetSession(1)).Returns(session);
        serviceMock.Setup(s => s.GetPlayersInTrigger(trigger, 1)).Returns(new[] { p1 });

        // Act
        handler.ExecuteTick(trigger, 1, serviceMock.Object);

        // Assert
        Assert.Equal(90, p1.Hp);
    }
}
