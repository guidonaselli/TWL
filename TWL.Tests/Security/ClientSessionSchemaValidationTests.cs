using System.Reflection;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;
using TWL.Server.Security;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Constants;
using TWL.Shared.Net.Network;
using Xunit;

namespace TWL.Tests.Security;

public class ClientSessionSchemaValidationTests
{
    [Fact]
    public async Task HandleMessageAsync_MissingSchemaVersion_Rejected()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-missing-version",
            TimestampUtc = DateTime.UtcNow,
            JsonPayload = "{}",
            SchemaVersion = null // Missing
        };

        var initialErrors = metrics.GetSnapshot().ValidationErrors;

        // Act
        // This should NOT throw exception if handled correctly.
        // It should just reject the message and increment validation errors.
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        Assert.Equal(initialErrors + 1, metrics.GetSnapshot().ValidationErrors);
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidSchemaVersion_Rejected()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-wrong-version",
            TimestampUtc = DateTime.UtcNow,
            JsonPayload = "{}",
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion + 1 // Mismatch
        };

        var initialErrors = metrics.GetSnapshot().ValidationErrors;

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        Assert.Equal(initialErrors + 1, metrics.GetSnapshot().ValidationErrors);
    }

    [Fact]
    public async Task HandleMessageAsync_ValidSchemaVersion_ProceedsToDispatch()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-correct-version",
            TimestampUtc = DateTime.UtcNow,
            JsonPayload = "{}",
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion // Correct
        };

        var initialErrors = metrics.GetSnapshot().ValidationErrors;

        // Act
        // It should NOT throw. If schema matches, it proceeds to dispatch.
        // In this test harness, HandleMoveAsync returns early because Character is null.
        // This counts as "success" (no validation error).
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        Assert.Equal(initialErrors, metrics.GetSnapshot().ValidationErrors);
    }

    // Test helper wrapper using the protected constructor
    private class TestableClientSession : ClientSession
    {
        private readonly ServerMetrics _metrics;
        private readonly ReplayGuard _replayGuard;
        private readonly RateLimiter _rateLimiter;

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

            try
            {
                var task = method.Invoke(this, new object[] { msg, "test-trace-id" }) as Task;
                return task ?? Task.CompletedTask;
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap
                throw ex.InnerException ?? ex;
            }
        }

        public override Task SendAsync(NetMessage msg)
        {
            return Task.CompletedTask;
        }
    }
}
