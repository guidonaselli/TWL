using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO;

public class TradeRequestDto
{
    public int TargetUserId { get; set; }
}

public class TradeRequestReceivedDto
{
    public int InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
}

public class TradeResponseDto
{
    public bool Accept { get; set; }
}

public class TradeOfferUpdateDto
{
    public List<TradeItemDto> Items { get; set; } = new();
    public long Gold { get; set; }
}

public class TradeItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class TradeStateUpdateDto
{
    public int InitiatorId { get; set; }
    public int ReceiverId { get; set; }
    
    public TradeOfferDto InitiatorOffer { get; set; } = new();
    public TradeOfferDto ReceiverOffer { get; set; } = new();
    
    public bool InitiatorConfirmed { get; set; }
    public bool ReceiverConfirmed { get; set; }
    
    public bool IsCancelled { get; set; }
    public bool IsCompleted { get; set; }
}

public class TradeOfferDto
{
    public List<TradeItemDto> Items { get; set; } = new();
    public long Gold { get; set; }
}

public class TradeConfirmDto
{
}

public class TradeCancelDto
{
}
