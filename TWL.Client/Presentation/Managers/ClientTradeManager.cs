using System;
using System.Collections.Generic;
using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Managers;

/// <summary>
/// Client-side manager for direct player-to-player trade.
/// </summary>
public class ClientTradeManager
{
    public TradeStateUpdateDto? CurrentTrade { get; private set; }
    
    public event Action<TradeStateUpdateDto>? OnTradeUpdated;
    public event Action<string>? OnTradeRequestReceived;
    public event Action? OnTradeCompleted;
    public event Action? OnTradeCancelled;
    public event Action? OnTradeDeclined;

    public void HandleTradeRequest(int inviterId, string inviterName)
    {
        OnTradeRequestReceived?.Invoke(inviterName);
    }

    public void HandleTradeStateUpdate(TradeStateUpdateDto state)
    {
        CurrentTrade = state;
        OnTradeUpdated?.Invoke(state);
    }

    public void HandleTradeComplete()
    {
        CurrentTrade = null;
        OnTradeCompleted?.Invoke();
    }

    public void HandleTradeDecline()
    {
        OnTradeDeclined?.Invoke();
    }

    public void HandleTradeCancel()
    {
        CurrentTrade = null;
        OnTradeCancelled?.Invoke();
    }
}
