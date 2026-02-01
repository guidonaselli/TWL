using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Tests.Economy;

public class EconomyServiceMockTests
{
    [Fact]
    public void Can_Mock_EconomyService()
    {
        // Arrange
        var mockService = new MockEconomyService();
        var character = new ServerCharacter { Id = 99 };
        IEconomyService service = mockService;

        // Act
        service.InitiatePurchase(99, "gems_100");
        service.VerifyPurchase(99, "ORD_123", "TOK_ABC", character);
        service.BuyShopItem(character, 5, 10);

        // Assert
        Assert.Equal(3, mockService.CallLog.Count);
        Assert.Equal("InitiatePurchase(99, gems_100)", mockService.CallLog[0]);
        Assert.Equal("VerifyPurchase(99, ORD_123, TOK_ABC)", mockService.CallLog[1]);
        Assert.Equal("BuyShopItem(99, 5, 10)", mockService.CallLog[2]);
    }

    // Mock implementation of IEconomyService
    public class MockEconomyService : IEconomyService
    {
        public List<string> CallLog { get; } = new();

        public PurchaseGemsIntentResponseDTO InitiatePurchase(int userId, string productId, string? traceId = null)
        {
            CallLog.Add($"InitiatePurchase({userId}, {productId})");
            return new PurchaseGemsIntentResponseDTO { OrderId = "MOCK_ORDER" };
        }

        public EconomyOperationResultDTO VerifyPurchase(int userId, string orderId, string receiptToken,
            ServerCharacter character, string? traceId = null)
        {
            CallLog.Add($"VerifyPurchase({userId}, {orderId}, {receiptToken})");
            return new EconomyOperationResultDTO { Success = true, Message = "Mock Success" };
        }

        public EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity,
            string operationId = null, string? traceId = null)
        {
            CallLog.Add($"BuyShopItem({character.Id}, {shopItemId}, {quantity})");
            return new EconomyOperationResultDTO { Success = true, Message = "Mock Success" };
        }

        public EconomyOperationResultDTO GiftShopItem(ServerCharacter giver, ServerCharacter receiver, int shopItemId,
            int quantity, string operationId, string? traceId = null)
        {
            CallLog.Add($"GiftShopItem({giver.Id}, {receiver.Id}, {shopItemId}, {quantity})");
            return new EconomyOperationResultDTO { Success = true, Message = "Mock Success" };
        }
    }
}