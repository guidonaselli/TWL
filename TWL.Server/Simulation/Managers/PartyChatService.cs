using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Services;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using TWL.Shared.Net.Payloads;

namespace TWL.Server.Simulation.Managers;

public class PartyChatService : IPartyChatService
{
    private readonly IPartyService _partyService;
    private readonly PlayerService _playerService;
    private readonly ILogger<PartyChatService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PartyChatService(IPartyService partyService, PlayerService playerService, ILogger<PartyChatService> logger)
    {
        _partyService = partyService;
        _playerService = playerService;
        _logger = logger;
    }

    public async Task SendPartyMessageAsync(int partyId, int senderId, string senderName, string content)
    {
        var party = _partyService.GetParty(partyId);
        if (party == null)
        {
            return;
        }

        if (!party.MemberIds.Contains(senderId))
        {
            _logger.LogWarning($"Player {senderId} tried to send message to party {partyId} but is not a member.");
            return;
        }

        var message = new PartyChatMessage
        {
            SenderId = senderId,
            SenderName = senderName,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var netMsg = new NetMessage
        {
            Op = Opcode.PartyChatBroadcast,
            JsonPayload = json
        };

        foreach (var memberId in party.MemberIds)
        {
            var session = _playerService.GetSession(memberId);
            if (session != null)
            {
                await session.SendAsync(netMsg);
            }
        }
    }
}
