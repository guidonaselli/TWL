using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Services;

namespace TWL.Tests.Server.Services.World;

public class WorldTriggerServiceTests
{
    private readonly Mock<ITriggerHandler> _handlerMock;
    private readonly Mock<IWorldScheduler> _schedulerMock;
    private readonly Mock<IMapRegistry> _mapRegistryMock;
    private readonly WorldTriggerService _service;

    public WorldTriggerServiceTests()
    {
        var metrics = new ServerMetrics();
        var repo = new Mock<IPlayerRepository>();
        var playerService = new PlayerService(repo.Object, metrics);
        _schedulerMock = new Mock<IWorldScheduler>();
        _mapRegistryMock = new Mock<IMapRegistry>();
        _service = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, metrics, playerService,
            _schedulerMock.Object, _mapRegistryMock.Object);
        _handlerMock = new Mock<ITriggerHandler>();
    }

    [Fact]
    public void CheckTriggers_InsideBounds_ExecutesHandler()
    {
        var map = new ServerMap { Id = 1 };
        var trigger = new ServerTrigger
        {
            Id = "T1",
            Type = "TestType",
            X = 100,
            Y = 100,
            Width = 50,
            Height = 50
        };
        map.Triggers.Add(trigger);

        _mapRegistryMock.Setup(m => m.GetMap(1)).Returns(map);

        _handlerMock.Setup(h => h.CanHandle("TestType")).Returns(true);
        _service.RegisterHandler(_handlerMock.Object);

        var character = new ServerCharacter { MapId = 1, X = 110, Y = 110 }; // Inside

        _service.CheckTriggers(character);

        _handlerMock.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once);
    }

    [Fact]
    public void CheckTriggers_OutsideBounds_DoesNotExecute()
    {
        var map = new ServerMap { Id = 1 };
        var trigger = new ServerTrigger
        {
            Id = "T1",
            Type = "TestType",
            X = 100,
            Y = 100,
            Width = 50,
            Height = 50
        };
        map.Triggers.Add(trigger);

        _mapRegistryMock.Setup(m => m.GetMap(1)).Returns(map);

        _handlerMock.Setup(h => h.CanHandle("TestType")).Returns(true);
        _service.RegisterHandler(_handlerMock.Object);

        var character = new ServerCharacter { MapId = 1, X = 90, Y = 90 }; // Outside

        _service.CheckTriggers(character);

        _handlerMock.Verify(h => h.ExecuteEnter(It.IsAny<ServerCharacter>(), It.IsAny<ServerTrigger>(), _service),
            Times.Never);
    }

    [Fact]
    public void OnEnterTrigger_EnforcesCooldown()
    {
        var map = new ServerMap { Id = 1 };
        var trigger = new ServerTrigger
        {
            Id = "T1",
            Type = "TestType",
            CooldownMs = 1000 // 20 ticks
        };
        map.Triggers.Add(trigger);

        _mapRegistryMock.Setup(m => m.GetMap(1)).Returns(map);
        _schedulerMock.Setup(s => s.CurrentTick).Returns(100);

        _handlerMock.Setup(h => h.CanHandle("TestType")).Returns(true);
        _service.RegisterHandler(_handlerMock.Object);

        var character = new ServerCharacter { MapId = 1 };

        // First execution - Success
        _service.OnEnterTrigger(character, 1, "T1");
        _handlerMock.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once);

        // Second execution (same tick) - Blocked
        _service.OnEnterTrigger(character, 1, "T1");
        _handlerMock.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once); // Still once

        // Third execution (next tick, but still within cooldown) - Blocked
        _schedulerMock.Setup(s => s.CurrentTick).Returns(110);
        _service.OnEnterTrigger(character, 1, "T1");
        _handlerMock.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Once); // Still once

        // Fourth execution (after cooldown) - Success
        _schedulerMock.Setup(s => s.CurrentTick).Returns(125); // 100 + 20 + 5
        _service.OnEnterTrigger(character, 1, "T1");
        _handlerMock.Verify(h => h.ExecuteEnter(character, trigger, _service), Times.Exactly(2));
    }
}