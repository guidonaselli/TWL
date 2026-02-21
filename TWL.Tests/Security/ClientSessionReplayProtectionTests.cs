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
        
        // We need a dummy TcpClient for the base constructor, but we won't use its stream
        // Actually, ClientSession constructor gets the stream. We must have a connected client or throw.
        // Let's use the parameterless constructor for testing!
        // ClientSession has `protected ClientSession() { } // For testing`
        
        var session = new TestableClientSession(metrics, replayGuard);

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
    
    // Test helper wrapper using the protected constructor
    private class TestableClientSession : ClientSession
    {
        private readonly ServerMetrics _metrics;
        private readonly ReplayGuard _replayGuard;
        private readonly RateLimiter _rateLimiter;

        public TestableClientSession(ServerMetrics metrics, ReplayGuard replayGuard) 
            : base() // Use protected constructor
        {
            UserId = 1;
            
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
    }
}
