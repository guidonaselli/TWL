using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using System.Text.Json;

namespace TWL.Client.Presentation.UI;

/// <summary>
/// UI Component for the direct trade window.
/// </summary>
public class UiTradeWindow
{
    private readonly GameClientManager _gameClientManager;
    private TradeStateUpdateDto? _lastState;

    public UiTradeWindow(GameClientManager gameClientManager)
    {
        _gameClientManager = gameClientManager;
        _gameClientManager.TradeManager.OnTradeUpdated += OnTradeUpdated;
    }

    private void OnTradeUpdated(TradeStateUpdateDto state)
    {
        _lastState = state;
        Render();
    }

    public void Render()
    {
        if (_lastState == null) return;

        Console.WriteLine("=== TRADE WINDOW ===");
        Console.WriteLine($"Initiator: {_lastState.InitiatorId} [Confirmed: {_lastState.InitiatorConfirmed}]");
        Console.WriteLine($"Receiver: {_lastState.ReceiverId} [Confirmed: {_lastState.ReceiverConfirmed}]");
        
        Console.WriteLine("--- Your Offer ---");
        var myOffer = _lastState.InitiatorId == (int)_gameClientManager.PlayerId.GetHashCode() ? _lastState.InitiatorOffer : _lastState.ReceiverOffer;
        Console.WriteLine($"Gold: {myOffer.Gold}");
        foreach (var item in myOffer.Items)
        {
            Console.WriteLine($"- Item {item.ItemId} x{item.Quantity}");
        }

        Console.WriteLine("--- Other's Offer ---");
        var otherOffer = _lastState.InitiatorId == (int)_gameClientManager.PlayerId.GetHashCode() ? _lastState.ReceiverOffer : _lastState.InitiatorOffer;
        Console.WriteLine($"Gold: {otherOffer.Gold}");
        foreach (var item in otherOffer.Items)
        {
            Console.WriteLine($"- Item {item.ItemId} x{item.Quantity}");
        }
        
        if (_lastState.IsCompleted) Console.WriteLine(">>> TRADE COMPLETED <<<");
        if (_lastState.IsCancelled) Console.WriteLine(">>> TRADE CANCELLED <<<");
    }

    public void UpdateOffer(long gold, List<TradeItemDto> items)
    {
        var msg = new NetMessage
        {
            Op = Opcode.TradeOfferUpdate,
            JsonPayload = JsonSerializer.Serialize(new TradeOfferUpdateDto
            {
                Gold = gold,
                Items = items
            })
        };
        _gameClientManager.NetworkClient.SendNetMessage(msg);
    }

    public void Confirm()
    {
        var msg = new NetMessage { Op = Opcode.TradeConfirm };
        _gameClientManager.NetworkClient.SendNetMessage(msg);
    }

    public void Cancel()
    {
        var msg = new NetMessage { Op = Opcode.TradeCancel };
        _gameClientManager.NetworkClient.SendNetMessage(msg);
    }
}
