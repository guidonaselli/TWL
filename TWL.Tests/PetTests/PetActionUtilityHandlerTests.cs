using Xunit;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Net.Network;
using Moq;
using System.Text.Json;
using TWL.Server.Services;
using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Managers;
using TWL.Server.Persistence.Services;
using TWL.Server.Architecture.Pipeline;
using TWL.Shared.Domain.Characters;
using TWL.Server.Persistence;
using TWL.Server.Architecture.Observability;
using TWL.Shared.Services;
using TWL.Server.Simulation.Managers;

namespace TWL.Tests.PetTests;

public class PetActionUtilityHandlerTests
{
    [Fact]
    public async Task HandlePetActionAsync_Utility_CallsPetService()
    {
        // Arrange
        var petServiceMock = new Mock<PetService>(
            new Mock<PlayerService>(new Mock<IPlayerRepository>().Object, new Mock<ServerMetrics>().Object).Object,
            new Mock<PetManager>().Object,
            new Mock<MonsterManager>().Object,
            new Mock<CombatManager>(
                new Mock<ICombatResolver>().Object,
                new Mock<IRandomService>().Object,
                new Mock<ISkillCatalog>().Object,
                new Mock<IStatusEngine>().Object,
                null
            ).Object,
            new Mock<IRandomService>().Object,
            new Mock<ILogger<PetService>>().Object
        );
        var session = new TestClientSession(petServiceMock.Object);
        session.SetUserId(1);

        var request = new PetActionRequest
        {
            Action = PetActionType.Utility,
            PetInstanceId = "pet123",
            AdditionalData = "Mount"
        };
        var payload = JsonSerializer.Serialize(request);

        // Act
        await session.PublicHandlePetActionAsync(payload);

        // Assert
        petServiceMock.Verify(s => s.UseUtility(1, "pet123", PetUtilityType.Mount, null), Times.Once);
    }

    private class TestClientSession : ClientSession
    {
        public TestClientSession(PetService petService) : base()
        {
            var field = typeof(ClientSession).GetField("_petService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(this, petService);
        }

        public void SetUserId(int id) => UserId = id;

        public Task PublicHandlePetActionAsync(string payload)
        {
            // We need to use reflection or make HandlePetActionAsync protected/internal if we want to test it this way
            // For now, let's assume we can access it or we mock the whole ClientSession
            return typeof(ClientSession)
                .GetMethod("HandlePetActionAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(this, new object[] { payload }) as Task;
        }

        // Mock dependencies if needed
        public override Task SendAsync(NetMessage msg) => Task.CompletedTask;
    }
}
