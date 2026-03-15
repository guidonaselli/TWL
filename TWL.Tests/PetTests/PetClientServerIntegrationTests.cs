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
using TWL.Shared.Domain.Characters;
using TWL.Server.Persistence;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

public class PetClientServerIntegrationTests
{
    private readonly Mock<PetService> _petServiceMock;
    private readonly TestClientSession _session;

    public PetClientServerIntegrationTests()
    {
        _petServiceMock = new Mock<PetService>(
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
        _session = new TestClientSession(_petServiceMock.Object);
        _session.SetUserId(42);
    }

    [Fact]
    public async Task HandlePetActionAsync_Switch_CallsPetService()
    {
        var request = new PetActionRequest
        {
            Action = PetActionType.Switch,
            PetInstanceId = "pet-abc"
        };
        var payload = JsonSerializer.Serialize(request);

        await _session.PublicHandlePetActionAsync(payload);

        _petServiceMock.Verify(s => s.SwitchPet(42, "pet-abc"), Times.Once);
    }

    [Fact]
    public async Task HandlePetActionAsync_Rebirth_CallsPetService()
    {
        var request = new PetActionRequest
        {
            Action = PetActionType.Rebirth,
            PetInstanceId = "pet-xyz"
        };
        var payload = JsonSerializer.Serialize(request);

        await _session.PublicHandlePetActionAsync(payload);

        _petServiceMock.Verify(s => s.TryRebirth(42, "pet-xyz"), Times.Once);
    }

    [Fact]
    public async Task HandlePetActionAsync_Dismiss_CallsPetService()
    {
        var request = new PetActionRequest
        {
            Action = PetActionType.Dismiss,
            PetInstanceId = "pet-bye"
        };
        var payload = JsonSerializer.Serialize(request);

        await _session.PublicHandlePetActionAsync(payload);

        _petServiceMock.Verify(s => s.DismissPet(42, "pet-bye"), Times.Once);
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
            var method = typeof(ClientSession).GetMethod("HandlePetActionAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return method.Invoke(this, new object[] { payload }) as Task;
        }

        public override Task SendAsync(NetMessage msg) => Task.CompletedTask;
    }
}
