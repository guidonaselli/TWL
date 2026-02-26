using System.Reflection;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;
using TWL.Server.Security;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Constants;
using TWL.Shared.Net.Network;
using Xunit;

namespace TWL.Tests.Security;

public class ProtocolVersioningTests
{
    [Fact]
    public async Task HandleMessageAsync_ValidVersion_Accepted()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-valid",
            TimestampUtc = DateTime.UtcNow,
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion,
            JsonPayload = "{}"
        };

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        // Validation errors should be 0
        Assert.Equal(0, metrics.GetSnapshot().ValidationErrors);
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidVersion_Rejected()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-invalid-ver",
            TimestampUtc = DateTime.UtcNow,
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion + 1, // Invalid
            JsonPayload = "{}"
        };

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        // Validation errors should be 1
        Assert.Equal(1, metrics.GetSnapshot().ValidationErrors);
    }

    [Fact]
    public async Task HandleMessageAsync_MissingVersion_Rejected()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-missing-ver",
            TimestampUtc = DateTime.UtcNow,
            SchemaVersion = null, // Missing
            JsonPayload = "{}"
        };

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        // Validation errors should be 1
        Assert.Equal(1, metrics.GetSnapshot().ValidationErrors);
    }

    // Test helper wrapper using the protected constructor
    private class TestableClientSession : ClientSession
    {
        public TestableClientSession(ServerMetrics metrics, ReplayGuard replayGuard, int userId = 1)
            : base() // Use protected constructor
        {
            UserId = userId;

            // Set private fields using reflection
            SetPrivateField("_metrics", metrics);
            SetPrivateField("_replayGuard", replayGuard);
            SetPrivateField("_rateLimiter", new RateLimiter());
        }

        private void SetPrivateField(string fieldName, object value)
        {
            var field = typeof(ClientSession).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, value);
            }
        }

        public Task InvokeHandleMessageAsync(NetMessage msg)
        {
            var method = typeof(ClientSession).GetMethod("HandleMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                throw new InvalidOperationException("HandleMessageAsync not found");

            var task = method.Invoke(this, new object[] { msg, "test-trace-id" }) as Task;
            return task ?? Task.CompletedTask;
        }

        public override Task SendAsync(NetMessage msg)
        {
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync(string reason)
        {
            return Task.CompletedTask;
        }
    }
}
