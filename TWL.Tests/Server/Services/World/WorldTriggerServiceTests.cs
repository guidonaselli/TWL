using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Services.World;
using TWL.Server.Domain.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.World;

namespace TWL.Tests.Server.Services.World;

public class WorldTriggerServiceTests
{
    private readonly WorldTriggerService _service;
    private readonly Mock<ITriggerHandler> _handlerMock;

    public WorldTriggerServiceTests()
    {
        _service = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, new ServerMetrics());
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

        _service.LoadMaps(new[] { map });

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

        _service.LoadMaps(new[] { map });

        _handlerMock.Setup(h => h.CanHandle("TestType")).Returns(true);
        _service.RegisterHandler(_handlerMock.Object);

        var character = new ServerCharacter { MapId = 1, X = 90, Y = 90 }; // Outside

        _service.CheckTriggers(character);

        _handlerMock.Verify(h => h.ExecuteEnter(It.IsAny<ServerCharacter>(), It.IsAny<ServerTrigger>(), _service), Times.Never);
    }
}
