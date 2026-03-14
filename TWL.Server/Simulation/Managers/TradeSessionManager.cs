using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Simulation.Managers;

public class TradeSession
{
    public int InitiatorId { get; set; }
    public int ReceiverId { get; set; }
    
    public TradeOfferDto InitiatorOffer { get; set; } = new();
    public TradeOfferDto ReceiverOffer { get; set; } = new();
    
    public bool InitiatorConfirmed { get; set; }
    public bool ReceiverConfirmed { get; set; }
    
    public bool IsCancelled { get; set; }
    public bool IsCompleted { get; set; }
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public bool Involves(int userId) => InitiatorId == userId || ReceiverId == userId;
    
    public int GetOtherParticipant(int userId) => InitiatorId == userId ? ReceiverId : InitiatorId;

    public TradeStateUpdateDto ToDto()
    {
        return new TradeStateUpdateDto
        {
            InitiatorId = InitiatorId,
            ReceiverId = ReceiverId,
            InitiatorOffer = InitiatorOffer,
            ReceiverOffer = ReceiverOffer,
            InitiatorConfirmed = InitiatorConfirmed,
            ReceiverConfirmed = ReceiverConfirmed,
            IsCancelled = IsCancelled,
            IsCompleted = IsCompleted
        };
    }
}

public class TradeSessionManager
{
    private readonly ConcurrentDictionary<string, TradeSession> _activeSessions = new();
    private readonly ConcurrentDictionary<int, int> _playerToSessionMap = new(); // PlayerId -> OtherPlayerId
    private readonly PlayerService _playerService;
    private readonly TradeManager _tradeManager;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TradeSessionManager(PlayerService playerService, TradeManager tradeManager)
    {
        _playerService = playerService;
        _tradeManager = tradeManager;
    }

    private string GetSessionKey(int id1, int id2)
    {
        var min = Math.Min(id1, id2);
        var max = Math.Max(id1, id2);
        return $"{min}:{max}";
    }

    public async Task RequestTradeAsync(int inviterId, int targetId)
    {
        if (inviterId == targetId) return;

        if (_playerToSessionMap.ContainsKey(inviterId) || _playerToSessionMap.ContainsKey(targetId))
        {
            // Already in a trade
            return;
        }

        var inviterSession = _playerService.GetSession(inviterId);
        var targetSession = _playerService.GetSession(targetId);

        if (inviterSession == null || targetSession == null) return;

        // Send invite to target
        await targetSession.SendAsync(new NetMessage
        {
            Op = Opcode.TradeRequest,
            JsonPayload = JsonSerializer.Serialize(new { InviterId = inviterId, InviterName = inviterSession.Character?.Name }, _jsonOptions)
        });
    }

    public async Task AcceptTradeAsync(int receiverId, int initiatorId)
    {
        var key = GetSessionKey(initiatorId, receiverId);
        if (_activeSessions.ContainsKey(key)) return;

        var session = new TradeSession
        {
            InitiatorId = initiatorId,
            ReceiverId = receiverId
        };

        if (_activeSessions.TryAdd(key, session))
        {
            _playerToSessionMap[initiatorId] = receiverId;
            _playerToSessionMap[receiverId] = initiatorId;

            await BroadcastStateAsync(session);
        }
    }

    public async Task DeclineTradeAsync(int receiverId, int initiatorId)
    {
        var initiatorSession = _playerService.GetSession(initiatorId);
        if (initiatorSession != null)
        {
            await initiatorSession.SendAsync(new NetMessage
            {
                Op = Opcode.TradeDecline,
                JsonPayload = JsonSerializer.Serialize(new { ReceiverId = receiverId }, _jsonOptions)
            });
        }
    }

    public async Task UpdateOfferAsync(int userId, TradeOfferUpdateDto update)
    {
        if (!_playerToSessionMap.TryGetValue(userId, out var otherId)) return;
        var key = GetSessionKey(userId, otherId);
        if (!_activeSessions.TryGetValue(key, out var session)) return;

        if (session.IsCompleted || session.IsCancelled) return;

        if (session.InitiatorId == userId)
        {
            session.InitiatorOffer = new TradeOfferDto { Gold = update.Gold, Items = update.Items };
            session.InitiatorConfirmed = false;
        }
        else
        {
            session.ReceiverOffer = new TradeOfferDto { Gold = update.Gold, Items = update.Items };
            session.ReceiverConfirmed = false;
        }
        
        // Reset both confirmations when offer changes (security requirement)
        session.InitiatorConfirmed = false;
        session.ReceiverConfirmed = false;
        session.LastActivity = DateTime.UtcNow;

        await BroadcastStateAsync(session);
    }

    public async Task ConfirmTradeAsync(int userId)
    {
        if (!_playerToSessionMap.TryGetValue(userId, out var otherId)) return;
        var key = GetSessionKey(userId, otherId);
        if (!_activeSessions.TryGetValue(key, out var session)) return;

        if (session.IsCompleted || session.IsCancelled) return;

        if (session.InitiatorId == userId) session.InitiatorConfirmed = true;
        else session.ReceiverConfirmed = true;

        session.LastActivity = DateTime.UtcNow;

        if (session.InitiatorConfirmed && session.ReceiverConfirmed)
        {
            await ExecuteTradeAsync(session);
        }
        else
        {
            await BroadcastStateAsync(session);
        }
    }

    public async Task CancelTradeAsync(int userId)
    {
        if (!_playerToSessionMap.TryGetValue(userId, out var otherId)) return;
        var key = GetSessionKey(userId, otherId);
        if (!_activeSessions.TryGetValue(key, out var session)) return;

        if (session.IsCompleted || session.IsCancelled) return;

        session.IsCancelled = true;
        await BroadcastStateAsync(session);
        CleanupSession(session);
    }

    private async Task ExecuteTradeAsync(TradeSession session)
    {
        var initiator = _playerService.GetSession(session.InitiatorId)?.Character;
        var receiver = _playerService.GetSession(session.ReceiverId)?.Character;

        if (initiator == null || receiver == null)
        {
            session.IsCancelled = true;
            await BroadcastStateAsync(session);
            CleanupSession(session);
            return;
        }

        // Execute transfers
        var p1ToP2Items = session.InitiatorOffer.Items.Select(i => (i.ItemId, i.Quantity)).ToList();
        var p2ToP1Items = session.ReceiverOffer.Items.Select(i => (i.ItemId, i.Quantity)).ToList();

        bool success = _tradeManager.TransferItemsBatch(initiator, receiver, 
            p1ToP2Items, session.InitiatorOffer.Gold, 
            p2ToP1Items, session.ReceiverOffer.Gold);

        if (success)
        {
            session.IsCompleted = true;
            await BroadcastStateAsync(session);
            
            // Notify completion
            var msg = new NetMessage { Op = Opcode.TradeComplete };
            await _playerService.GetSession(session.InitiatorId)!.SendAsync(msg);
            await _playerService.GetSession(session.ReceiverId)!.SendAsync(msg);
        }
        else
        {
            session.IsCancelled = true;
            await BroadcastStateAsync(session);
        }

        CleanupSession(session);
    }

    private void CleanupSession(TradeSession session)
    {
        var key = GetSessionKey(session.InitiatorId, session.ReceiverId);
        _activeSessions.TryRemove(key, out _);
        _playerToSessionMap.TryRemove(session.InitiatorId, out _);
        _playerToSessionMap.TryRemove(session.ReceiverId, out _);
    }

    private async Task BroadcastStateAsync(TradeSession session)
    {
        var dto = session.ToDto();
        var msg = new NetMessage
        {
            Op = Opcode.TradeStateUpdate,
            JsonPayload = JsonSerializer.Serialize(dto, _jsonOptions)
        };

        var s1 = _playerService.GetSession(session.InitiatorId);
        var s2 = _playerService.GetSession(session.ReceiverId);

        if (s1 != null) await s1.SendAsync(msg);
        if (s2 != null) await s2.SendAsync(msg);
    }
}
