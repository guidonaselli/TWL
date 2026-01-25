namespace TWL.Shared.Domain.DTO;

public class PurchaseGemsIntentDTO
{
    public string ProductId { get; set; }
}

public class PurchaseGemsIntentResponseDTO
{
    public string OrderId { get; set; }
    public string ProviderUrl { get; set; }
}

public class PurchaseGemsVerifyDTO
{
    public string OrderId { get; set; }
    public string ReceiptToken { get; set; }
}

public class BuyShopItemDTO
{
    public int ShopItemId { get; set; }
    public int Quantity { get; set; }
    public string OperationId { get; set; }
}

public class EconomyOperationResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long NewBalance { get; set; }
    public string OrderId { get; set; } // Optional, for context
}
