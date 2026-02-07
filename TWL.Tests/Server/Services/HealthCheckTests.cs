using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests.Server.Services;

public class HealthCheckTests
{
    private readonly Mock<DbService> _mockDb;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly ServerMetrics _serverMetrics;
    private readonly Mock<ILogger<HealthCheckService>> _mockLogger;
    private readonly HealthCheckService _service;

    public HealthCheckTests()
    {
        _mockDb = new Mock<DbService>("Host=dummy;");
        _serverMetrics = new ServerMetrics();
        // We pass null for repository as we won't call methods that use it in this test (hopefully)
        // Only Metrics property is accessed which is initialized in ctor.
        _mockPlayerService = new Mock<PlayerService>(null, _serverMetrics);
        _mockLogger = new Mock<ILogger<HealthCheckService>>();

        _service = new HealthCheckService(_mockDb.Object, _mockPlayerService.Object, _serverMetrics, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteHealthJson()
    {
        // Arrange
        _mockDb.Setup(db => db.CheckHealthAsync()).ReturnsAsync(true);
        _mockPlayerService.Setup(ps => ps.ActiveSessionCount).Returns(5);
        _mockPlayerService.Setup(ps => ps.DirtySessionCount).Returns(2);

        using var cts = new CancellationTokenSource();

        // Act
        // Start the service. It should run the loop logic immediately then hit Task.Delay.
        await _service.StartAsync(cts.Token);

        // Give it a moment to write the file (it's async void practically inside the loop)
        await Task.Delay(200);

        // Stop it
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockDb.Verify(db => db.CheckHealthAsync(), Times.AtLeastOnce);
        _mockPlayerService.Verify(ps => ps.ActiveSessionCount, Times.AtLeastOnce);

        // Verify file content
        Assert.True(File.Exists("health.json"), "health.json should be created");
        var content = await File.ReadAllTextAsync("health.json");
        Assert.Contains("\"ActivePlayers\": 5", content);
        Assert.Contains("\"DirtySessions\": 2", content);
        Assert.Contains("\"AppVersion\":", content);
        Assert.Contains("\"Uptime\":", content);

        // Cleanup
        File.Delete("health.json");
    }

    [Fact]
    public async Task SetStatus_ShouldUpdateStatusAndTriggerWrite()
    {
        // Arrange
        _mockDb.Setup(db => db.CheckHealthAsync()).ReturnsAsync(true);
        using var cts = new CancellationTokenSource();

        // Act
        await _service.StartAsync(cts.Token);
        await Task.Delay(100); // Initial write

        _service.SetStatus(ServerStatus.ShuttingDown);
        await Task.Delay(100); // Triggered write

        // Assert
        Assert.Equal(ServerStatus.ShuttingDown, _service.CurrentStatus);

        Assert.True(File.Exists("health.json"));
        var content = await File.ReadAllTextAsync("health.json");
        // Check for ServerStatus property. Default serialization is likely integer.
        // ShuttingDown is 3.
        Assert.Contains("\"ServerStatus\": 3", content);

        await _service.StopAsync(CancellationToken.None);
        File.Delete("health.json");
    }
}
