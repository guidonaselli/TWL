using Moq;
using TWL.Server.Domain.World.Conditions;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Tests.Services.World;
using Xunit;

namespace TWL.Tests.Domain.World.Conditions;

public class FlagConditionTests
{
    private readonly Mock<PlayerService> _playerService;
    private readonly ServerCharacter _character;
    private readonly TestClientSession _session;

    public FlagConditionTests()
    {
        var repo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _playerService = new Mock<PlayerService>(repo.Object, metrics);
        _character = new ServerCharacter { Id = 1 };
        _session = new TestClientSession(_character) { UserId = 1 };

        var questManager = new Mock<ServerQuestManager>();
        _session.SetQuestComponent(new PlayerQuestComponent(questManager.Object));

        _playerService.Setup(ps => ps.GetSession(1)).Returns(_session);
    }

    [Fact]
    public void IsMet_ReturnsTrue_WhenFlagPresent()
    {
        // Arrange
        _session.QuestComponent.Flags.Add("TestFlag");
        var condition = new FlagCondition("TestFlag");

        // Act
        var result = condition.IsMet(_character, _playerService.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMet_ReturnsFalse_WhenFlagMissing()
    {
        // Arrange
        var condition = new FlagCondition("TestFlag");

        // Act
        var result = condition.IsMet(_character, _playerService.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMet_ReturnsTrue_WhenInvertedAndFlagMissing()
    {
        // Arrange
        var condition = new FlagCondition("TestFlag", inverted: true);

        // Act
        var result = condition.IsMet(_character, _playerService.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMet_ReturnsFalse_WhenInvertedAndFlagPresent()
    {
        // Arrange
        _session.QuestComponent.Flags.Add("TestFlag");
        var condition = new FlagCondition("TestFlag", inverted: true);

        // Act
        var result = condition.IsMet(_character, _playerService.Object);

        // Assert
        Assert.False(result);
    }
}
