using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;
using TWL.Server.Security;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using TWL.Shared.Constants;
using Xunit;

namespace TWL.Tests.Security;

public class ClientSessionMovementValidationTests
{
    [Fact]
    public async Task HandleMessageAsync_ValidMove_UpdatesPosition()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var options = new MovementValidationOptions { MaxDistancePerTick = 5.0f, MaxAxisDeltaPerTick = 5.0f };
        var validator = new MovementValidator(options);
        var mockTriggerService = new Mock<IWorldTriggerService>();

        var session = new TestableClientSession(metrics, validator, mockTriggerService.Object);

        // Initial position
        Assert.Equal(0f, session.Character.X);
        Assert.Equal(0f, session.Character.Y);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            JsonPayload = "{\"dx\":2.0,\"dy\":3.0}",
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        Assert.Equal(2.0f, session.Character.X);
        Assert.Equal(3.0f, session.Character.Y);
        Assert.Equal(0, metrics.GetSnapshot().ValidationErrors);

        // Verify triggers were checked
        mockTriggerService.Verify(ts => ts.CheckTriggers(session.Character), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidMove_RejectsAndRecordsMetrics()
    {
        // Arrange
        var metrics = new ServerMetrics();
        var options = new MovementValidationOptions { MaxDistancePerTick = 5.0f, MaxAxisDeltaPerTick = 5.0f };
        var validator = new MovementValidator(options);
        var mockTriggerService = new Mock<IWorldTriggerService>();

        var session = new TestableClientSession(metrics, validator, mockTriggerService.Object);

        // Initial position
        Assert.Equal(0f, session.Character.X);
        Assert.Equal(0f, session.Character.Y);

        var msg = new NetMessage
        {
            Op = Opcode.MoveRequest,
            JsonPayload = "{\"dx\":10.0,\"dy\":0.0}", // Exceeds MaxAxisDelta and MaxDistance
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await session.InvokeHandleMessageAsync(msg);

        // Assert
        // Position should not change
        Assert.Equal(0f, session.Character.X);
        Assert.Equal(0f, session.Character.Y);

        // Error recorded
        Assert.Equal(1, metrics.GetSnapshot().ValidationErrors);

        // Triggers shouldn't be checked for rejected move
        mockTriggerService.Verify(ts => ts.CheckTriggers(It.IsAny<ServerCharacter>()), Times.Never);
    }

    private class TestableClientSession : ClientSession
    {
        public TestableClientSession(ServerMetrics metrics, MovementValidator validator, IWorldTriggerService triggerService)
            : base() // Use protected parameterless constructor
        {
            UserId = 1;
            Character = new ServerCharacter { Id = 1, Name = "TestPlayer", X = 0f, Y = 0f };

            // Setup mocks and required services using reflection
            SetPrivateField("_metrics", metrics);
            SetPrivateField("_movementValidator", validator);
            SetPrivateField("_worldTriggerService", triggerService);
            SetPrivateField("_rateLimiter", new RateLimiter());

            var mockCombatResolver = new Mock<ICombatResolver>();
            var mockRandom = new Mock<IRandomService>();
            var mockSkillCatalog = new Mock<ISkillCatalog>();
            var mockStatusEngine = new Mock<IStatusEngine>();
            var combatManager = new CombatManager(mockCombatResolver.Object, mockRandom.Object, mockSkillCatalog.Object, mockStatusEngine.Object);
            SetPrivateField("_combatManager", combatManager);

            // Just to prevent NullReferenceExceptions in base.HandleMessageAsync if needed
            var guardOptions = new ReplayGuardOptions { NonceTtlSeconds = 60 };
            SetPrivateField("_replayGuard", new ReplayGuard(guardOptions, () => DateTime.UtcNow));

            // _spawnManager is nullable in ClientSession so it's fine to leave it null
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
