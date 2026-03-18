using System;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Server.Instances;

public class InstanceRunLimitTests
{
    private readonly InstanceService _instanceService;

    public InstanceRunLimitTests()
    {
        _instanceService = new InstanceService(new ServerMetrics());
    }

    [Fact]
    public void CanEnterInstance_UnderCap_AllowsEntry()
    {
        // Arrange
        var character = new ServerCharacter();
        var instanceId = "TEST_INSTANCE_01";

        // Ensure reset UTC is today so it doesn't reset
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;

        // Set under limit
        character.InstanceDailyRuns[instanceId] = InstanceService.DailyLimit - 1;

        // Act
        var result = _instanceService.CanEnterInstance(character, instanceId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEnterInstance_AtCap_RejectsEntry()
    {
        // Arrange
        var character = new ServerCharacter();
        var instanceId = "TEST_INSTANCE_01";

        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;
        character.InstanceDailyRuns[instanceId] = InstanceService.DailyLimit;

        // Act
        var result = _instanceService.CanEnterInstance(character, instanceId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanEnterInstance_PastResetDate_ResetsAndAllowsEntry()
    {
        // Arrange
        var character = new ServerCharacter();
        var instanceId = "TEST_INSTANCE_01";

        // Set to yesterday
        character.InstanceDailyResetUtc = DateTime.UtcNow.Date.AddDays(-1);
        character.InstanceDailyRuns[instanceId] = InstanceService.DailyLimit;

        // Act
        var result = _instanceService.CanEnterInstance(character, instanceId);

        // Assert
        Assert.True(result);
        Assert.Empty(character.InstanceDailyRuns);
        Assert.Equal(DateTime.UtcNow.Date, character.InstanceDailyResetUtc);
        Assert.True(character.IsDirty);
    }

    [Fact]
    public void RecordInstanceRun_IncrementsCounter_MarksDirty()
    {
        // Arrange
        var character = new ServerCharacter();
        var instanceId = "TEST_INSTANCE_01";

        character.InstanceDailyResetUtc = DateTime.UtcNow.Date;
        character.InstanceDailyRuns[instanceId] = 2;
        character.IsDirty = false;

        // Act
        _instanceService.RecordInstanceRun(character, instanceId);

        // Assert
        Assert.Equal(3, character.InstanceDailyRuns[instanceId]);
        Assert.True(character.IsDirty);
    }
}
