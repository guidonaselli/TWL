using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Services;

namespace TWL.Tests.Services;

public class SeedableRandomServiceTests
{
    private readonly Mock<ILogger<SeedableRandomService>> _mockLogger;

    public SeedableRandomServiceTests()
    {
        _mockLogger = new Mock<ILogger<SeedableRandomService>>();
    }

    [Fact]
    public void Constructor_WithSeed_LogsSeed()
    {
        // Arrange
        var seed = 12345;

        // Act
        var service = new SeedableRandomService(_mockLogger.Object, seed);

        // Assert
        // Verify logger was called with the seed info
        // Note: Logging extensions like LogInformation are extension methods, so we verify Log on the interface.
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(seed.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Determinism_SameSeed_ProducesSameSequence()
    {
        // Arrange
        var seed = 42;
        var service1 = new SeedableRandomService(_mockLogger.Object, seed);
        var service2 = new SeedableRandomService(_mockLogger.Object, seed);

        // Act & Assert
        // We verify a sequence of calls
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(service1.Next(0, 100), service2.Next(0, 100));
            Assert.Equal(service1.NextFloat(), service2.NextFloat());
            Assert.Equal(service1.NextFloat(0, 10), service2.NextFloat(0, 10));
        }
    }

    [Fact]
    public void Independence_DifferentSeeds_ProducesDifferentSequence()
    {
        // Arrange
        var service1 = new SeedableRandomService(_mockLogger.Object, 123);
        var service2 = new SeedableRandomService(_mockLogger.Object, 456);

        // Act
        // It's theoretically possible they produce same first number, but unlikely for a sequence.
        var allEqual = true;
        for (var i = 0; i < 10; i++)
        {
            if (service1.NextFloat() != service2.NextFloat())
            {
                allEqual = false;
                break;
            }
        }

        // Assert
        Assert.False(allEqual, "Different seeds should produce different sequences.");
    }

    [Fact]
    public void ThreadSafety_ConcurrentAccess_DoesNotThrow()
    {
        // Arrange
        var service = new SeedableRandomService(_mockLogger.Object, 999);
        var results = new ConcurrentBag<float>();

        // Act
        Parallel.For(0, 1000, i => { results.Add(service.NextFloat()); });

        // Assert
        Assert.Equal(1000, results.Count);
    }

    [Fact]
    public void Next_WithContext_LogsContext()
    {
        // Arrange
        var seed = 123;
        var context = "TestContext";
        _mockLogger.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(true);
        var service = new SeedableRandomService(_mockLogger.Object, seed);

        // Act
        service.NextFloat(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(context)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}