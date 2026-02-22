using System.Reflection;
using System.Text.Json;
using TWL.Server.Architecture.Observability;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using Moq;
using Xunit;
using System.Net.Sockets;
using System.Net;

namespace TWL.Tests.Security;

public class ClientSessionReplayProtectionTests
{
    [Fact]
    public async Task HandleMessageAsync_DuplicateNonce_RejectedAndMetricsRecorded()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        var session = new TestableClientSession(metrics, replayGuard); // Default userId=1

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            Nonce = "nonce-123",
            TimestampUtc = DateTime.UtcNow,
            JsonPayload = "{}"
        };

        // Act 1: First message should be accepted (metrics validate duration recorded, but no validation error)
        var initialErrors = metrics.GetSnapshot().ValidationErrors;
        await session.InvokeHandleMessageAsync(msg);
        Assert.Equal(initialErrors, metrics.GetSnapshot().ValidationErrors);

        // Act 2: Duplicate message should be rejected
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        Assert.Equal(initialErrors + 1, metrics.GetSnapshot().ValidationErrors);
    }

    [Fact]
    public async Task HandleMessageAsync_PreLogin_DuplicateNonce_Rejected()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60, AllowedClockSkewSeconds = 30 };
        var replayGuard = new ReplayGuard(guardOptions, () => DateTime.UtcNow);

        // Use userId = -1 (Pre-login state)
        var session = new TestableClientSession(metrics, replayGuard, -1);

        var timestamp = DateTime.UtcNow;
        var nonce = "nonce-prelogin";

        // Create TWO distinct message objects with SAME content to simulate deserialization
        // This is critical because the bug relies on GetHashCode() being different for distinct objects
        var msg1 = new NetMessage
        {
            Op = Opcode.LoginRequest,
            Nonce = nonce,
            TimestampUtc = timestamp,
            JsonPayload = "{}"
        };

        var msg2 = new NetMessage
        {
            Op = Opcode.LoginRequest,
            Nonce = nonce,
            TimestampUtc = timestamp,
            JsonPayload = "{}"
        };

        // Act 1: First message should be accepted
        var initialErrors = metrics.GetSnapshot().ValidationErrors;
        await session.InvokeHandleMessageAsync(msg1);
        Assert.Equal(initialErrors, metrics.GetSnapshot().ValidationErrors);

        // Act 2: Duplicate message (different object, same content) should be rejected
        await session.InvokeHandleMessageAsync(msg2);

        // Assert
        Assert.Equal(initialErrors + 1, metrics.GetSnapshot().ValidationErrors);
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

            var task = method.Invoke(this, new object[] { msg, "test-trace-id" }) as Task;
            return task ?? Task.CompletedTask;
        }

        public override Task SendAsync(NetMessage msg)
        {
            return Task.CompletedTask;
        }
    }
}
