using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.DTO;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Simulation.Managers;

public class GuildChatService
{
    private readonly GuildManager _guildManager;
    private readonly PlayerService _playerService;

    private readonly ConcurrentDictionary<int, List<GuildChatMessageDto>> _chatBacklogs = new();
    private const int MaxBacklogSize = 100;

    public GuildChatService(GuildManager guildManager, PlayerService playerService)
    {
        _guildManager = guildManager;
        _playerService = playerService;
    }

    public void BroadcastMessage(int senderId, string senderName, string message)
    {
        var guild = _guildManager.GetGuildByMember(senderId);
        if (guild == null) return;

        var chatMsg = new GuildChatMessageDto
        {
            Id = DateTime.UtcNow.Ticks,
            SenderId = senderId,
            SenderName = senderName,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        var backlog = _chatBacklogs.GetOrAdd(guild.GuildId, _ => new List<GuildChatMessageDto>());
        lock (backlog)
        {
            backlog.Add(chatMsg);
            if (backlog.Count > MaxBacklogSize)
            {
                backlog.RemoveAt(0);
            }
        }

        var chatEventDto = new GuildChatEvent { Message = chatMsg };

        // Duplicate the list of members to avoid exceptions if modifed during enum
        int[] members;
        lock (guild.MemberIds)
        {
            members = guild.MemberIds.ToArray();
        }

        foreach (var memberId in members)
        {
            var session = _playerService.GetSession(memberId);
            if (session != null)
            {
                var msg = new TWL.Shared.Net.Network.NetMessage 
                { 
                    Op = TWL.Shared.Net.Network.Opcode.GuildChatBroadcast, 
                    JsonPayload = System.Text.Json.JsonSerializer.Serialize(chatEventDto, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                };
                _ = session.SendAsync(msg);
            }
        }
    }

    public void SendBacklog(int memberId)
    {
        var guild = _guildManager.GetGuildByMember(memberId);
        if (guild == null) return;

        var session = _playerService.GetSession(memberId);
        if (session == null) return;

        if (_chatBacklogs.TryGetValue(guild.GuildId, out var backlog))
        {
            List<GuildChatMessageDto> snapshot;
            lock (backlog)
            {
                snapshot = backlog.ToList();
            }

            if (snapshot.Count > 0)
            {
                var payload = new GuildChatBacklog { Messages = snapshot };
                var msg = new TWL.Shared.Net.Network.NetMessage 
                { 
                    Op = TWL.Shared.Net.Network.Opcode.GuildChatBacklog, 
                    JsonPayload = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                };
                _ = session.SendAsync(msg);
            }
        }
    }
}
