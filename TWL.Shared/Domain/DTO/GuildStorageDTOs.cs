using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO;

public class GuildStorageItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class GuildStorageViewEvent
{
    public List<GuildStorageItemDto> Items { get; set; } = new();
}

public class GuildStorageDepositRequest
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public string OperationId { get; set; } = string.Empty;
}

public class GuildStorageWithdrawRequest
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public string OperationId { get; set; } = string.Empty;
}

public class GuildStorageUpdateEvent
{
    public int ItemId { get; set; }
    public int TotalQuantity { get; set; }
}

public class GuildStorageOperationResultEvent
{
    public string OperationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
