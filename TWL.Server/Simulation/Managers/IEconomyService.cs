using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

public interface IEconomyService
{
    PurchaseGemsIntentResponseDTO InitiatePurchase(int userId, string productId);
    EconomyOperationResultDTO VerifyPurchase(int userId, string orderId, string receiptToken, ServerCharacter character);
    EconomyOperationResultDTO BuyShopItem(ServerCharacter character, int shopItemId, int quantity, string operationId = null);
    EconomyOperationResultDTO GiftShopItem(ServerCharacter giver, ServerCharacter receiver, int shopItemId, int quantity, string operationId);
}
