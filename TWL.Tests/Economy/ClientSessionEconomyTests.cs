using System.Reflection;
using System.Text.Json;
using Moq;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Server.Security;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;

namespace TWL.Tests.Economy;

public class ClientSessionEconomyTests
{
    private readonly Mock<IEconomyService> _economyMock;
    private readonly TestClientSession _session;

    public ClientSessionEconomyTests()
    {
        _economyMock = new Mock<IEconomyService>();
        _session = new TestClientSession(_economyMock.Object);

        // Manually set Character and UserId for testing via TestClientSession helper
        _session.SetCharacter(new ServerCharacter { Id = 123, Name = "TestPlayer" });
        _session.SetUserId(123);
    }

    [Fact]
    public async Task HandleBuyShopItemAsync_PassesOperationId_To_EconomyManager()
    {
        // Arrange
        var operationId = "test-op-id-123";
        var dto = new BuyShopItemDTO
        {
            ShopItemId = 1,
            Quantity = 1,
            OperationId = operationId
        };

        var payload = JsonSerializer.Serialize(dto);
        var msg = new NetMessage
        {
            Op = Opcode.BuyShopItemRequest,
            JsonPayload = payload
        };

        // Act
        // Use reflection to invoke private HandleMessageAsync
        // Note: handleMessageAsync is private in ClientSession
        var method = typeof(ClientSession).GetMethod("HandleMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        await (Task)method.Invoke(_session, new object[] { msg, "trace-id" })!;

        // Assert
        // Verify that BuyShopItem was called with the correct operationId
        // This is expected to FAIL until fixed (currently passes null)
        _economyMock.Verify(e => e.BuyShopItem(
            It.Is<ServerCharacter>(c => c.Id == 123),
            It.Is<int>(id => id == 1),
            It.Is<int>(q => q == 1),
            It.Is<string>(opId => opId == operationId),
            It.IsAny<string>()
        ), Times.Once);
    }

    public class TestClientSession : ClientSession
    {
        public TestClientSession(IEconomyService economyService) : base()
        {
            // Use reflection to set private fields
            SetField("_economyManager", economyService);
            SetField("_rateLimiter", new RateLimiter()); // Required for HandleMessageAsync check
            SetField("_replayGuard", new ReplayGuard(Microsoft.Extensions.Options.Options.Create(new ReplayGuardOptions()))); // Added to prevent NRE

            // Metrics can be null in HandleMessageAsync, null checks exist
        }

        private void SetField(string fieldName, object? value)
        {
            var field = typeof(ClientSession).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, value);
            }
        }

        public void SetCharacter(ServerCharacter character)
        {
            Character = character;
        }

        public void SetUserId(int userId)
        {
            UserId = userId;
        }

        // Mock SendAsync to avoid NRE on _stream
        public override Task SendAsync(NetMessage msg)
        {
            return Task.CompletedTask;
        }
    }
}
