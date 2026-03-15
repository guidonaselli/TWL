using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Constants;
using TWL.Shared.Net;
using TWL.Shared.Constants;
using TWL.Shared.Net.Network;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.State;

namespace TWL.Client.Presentation.Networking;

public class NetworkClient
{
    // Configuration to be case-insensitive (PascalCase vs camelCase)
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _ip;
    private readonly ILogger<NetworkClient> _log;
    private readonly int _port;

    // Reusable buffer for receiving data to avoid repeated allocations
    private readonly byte[] _receiveBuffer = new byte[8192];
    private readonly Channel<NetMessage> _receiveChannel;

    private readonly Channel<NetMessage> _sendChannel;
    private CancellationTokenSource? _cts;

    private GameClientManager _gameClientManager;
    private NetworkStream? _stream;
    private TcpClient _tcp;

    public NetworkClient(string ip, int port, GameClientManager gameClientManager, ILogger<NetworkClient> log)
    {
        _log = log;
        _ip = ip;
        _port = port;
        _gameClientManager = gameClientManager;

        _tcp = new TcpClient();

        _sendChannel = Channel.CreateUnbounded<NetMessage>();
        _receiveChannel = Channel.CreateUnbounded<NetMessage>();
    }

    public bool IsConnected => _tcp?.Connected ?? false;
    public GameClientManager GameClientManager => _gameClientManager;

    public void Connect()
    {
        try
        {
            // Re-instantiate TcpClient if it was closed/disposed
            if (_tcp == null)
            {
                _tcp = new TcpClient();
            }

            _tcp.Connect(_ip, _port);
            _stream = _tcp.GetStream();
            Console.WriteLine($"Connected to server at {_ip}:{_port}");

            _cts = new CancellationTokenSource();
            _ = SendLoopAsync(_cts.Token);
            _ = ReceiveLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            throw;
        }
    }

    public void Update()
    {
        // Consume messages from the receive channel and handle them on the main thread
        while (_receiveChannel.Reader.TryRead(out var serverMsg))
        {
            HandleServerMessage(serverMsg);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _stream != null && IsConnected)
            {
                // Async read to avoid blocking threads
                // Use _receiveBuffer instead of allocating a new buffer
                var read = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, token);
                if (read == 0)
                {
                    break;
                }

                try
                {
                    // OPTIMIZATION: Deserialize directly from Span<byte>, avoiding string allocation
                    // Using Source Generator Context for better performance
                    var serverMsg = JsonSerializer.Deserialize(_receiveBuffer.AsSpan(0, read),
                        AppJsonContext.Default.NetMessage);

                    if (serverMsg != null)
                    {
                        await _receiveChannel.Writer.WriteAsync(serverMsg, token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing message: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReceiveLoopAsync error: {ex.Message}");
        }
    }

    private void HandleServerMessage(NetMessage serverMsg)
    {
        switch (serverMsg.Op)
        {
            case Opcode.PartyUpdateBroadcast:
                var update = JsonSerializer.Deserialize<PartyUpdateBroadcast>(serverMsg.JsonPayload, _jsonOptions);
                if (update != null)
                {
                    _gameClientManager.Party.Update(update);
                }
                break;
            case Opcode.PartyChatBroadcast:
                var chat = JsonSerializer.Deserialize<PartyChatMessage>(serverMsg.JsonPayload, _jsonOptions);
                if (chat != null)
                {
                    _gameClientManager.Party.AddMessage(chat);
                }
                break;
            case Opcode.GuildRosterSync:
                var rosterSync = JsonSerializer.Deserialize<GuildRosterSyncEvent>(serverMsg.JsonPayload, _jsonOptions);
                if (rosterSync != null) _gameClientManager.HandleGuildRosterSync(rosterSync);
                break;
            case Opcode.GuildRosterUpdate:
                var rosterUpdate = JsonSerializer.Deserialize<GuildRosterUpdateEvent>(serverMsg.JsonPayload, _jsonOptions);
                if (rosterUpdate != null) _gameClientManager.HandleGuildRosterUpdate(rosterUpdate);
                break;
            case Opcode.GuildChatBroadcast:
                var guildChat = JsonSerializer.Deserialize<GuildChatEvent>(serverMsg.JsonPayload, _jsonOptions);
                if (guildChat != null) _gameClientManager.HandleGuildChatEvent(guildChat);
                break;
            case Opcode.GuildChatBacklog:
                var chatBacklog = JsonSerializer.Deserialize<GuildChatBacklog>(serverMsg.JsonPayload, _jsonOptions);
                if (chatBacklog != null) _gameClientManager.HandleGuildChatBacklog(chatBacklog);
                break;
            case Opcode.TradeRequest:
                var tradeReq = JsonSerializer.Deserialize<TradeRequestReceivedDto>(serverMsg.JsonPayload, _jsonOptions);
                if (tradeReq != null) _gameClientManager.TradeManager.HandleTradeRequest(tradeReq.InviterId, tradeReq.InviterName);
                break;
            case Opcode.TradeDecline:
                _gameClientManager.TradeManager.HandleTradeDecline();
                break;
            case Opcode.TradeStateUpdate:
                var tradeState = JsonSerializer.Deserialize<TradeStateUpdateDto>(serverMsg.JsonPayload, _jsonOptions);
                if (tradeState != null) _gameClientManager.TradeManager.HandleTradeStateUpdate(tradeState);
                break;
            case Opcode.TradeComplete:
                _gameClientManager.TradeManager.HandleTradeComplete();
                break;
            case Opcode.TradeCancel:
                _gameClientManager.TradeManager.HandleTradeCancel();
                break;
            case Opcode.SMSG_COMPOUND_REQUEST_START_ACK:
                _gameClientManager.HandleCompoundStartAck();
                break;
            case Opcode.CompoundResponse:
                var compoundResponse = JsonSerializer.Deserialize<CompoundResponseDTO>(serverMsg.JsonPayload, _jsonOptions);
                if (compoundResponse != null) _gameClientManager.HandleCompoundResponse(compoundResponse);
                break;
        }

        EventBus.Publish(serverMsg);
    }

    public void SendNetMessage(NetMessage message)
    {
        message.SchemaVersion = ProtocolConstants.CurrentSchemaVersion;

        if (!IsConnected)
        {
            Console.WriteLine("Cannot send message: not connected");
            return;
        }

        // Auto-inject Schema Version
        message.SchemaVersion = ProtocolConstants.CurrentSchemaVersion;

        if (!_sendChannel.Writer.TryWrite(message))
        {
            Console.WriteLine("Failed to enqueue message.");
        }
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(token))
            {
                while (_sendChannel.Reader.TryRead(out var message))
                {
                    if (_stream == null || !IsConnected)
                    {
                        return;
                    }

                    try
                    {
                        // Optimization: Use System.Text.Json (SerializeToUtf8Bytes)
                        // to avoid intermediate string allocations and utilize existing _jsonOptions
                        // Using Source Generator Context for better performance
                        var data = JsonSerializer.SerializeToUtf8Bytes(message, AppJsonContext.Default.NetMessage);
                        await _stream.WriteAsync(data, 0, data.Length, token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending client message: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendLoopAsync error: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }

        if (_tcp != null)
        {
            _tcp.Close();
            _tcp = null;
        }

        Console.WriteLine("Disconnected from server");
    }
}