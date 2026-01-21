using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Net;
using TWL.Shared.Net.Messages;

namespace TWL.Client.Presentation.Networking;

public class NetworkClient
{
    private readonly string _ip;
    private readonly ILogger<NetworkClient> _log;
    private readonly int _port;

    // Configuration to be case-insensitive (PascalCase vs camelCase)
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private GameClientManager _gameClientManager;
    private NetworkStream? _stream;
    private TcpClient _tcp;

    private readonly Channel<ClientMessage> _sendChannel;
    private readonly Channel<ServerMessage> _receiveChannel;
    private CancellationTokenSource? _cts;

    public NetworkClient(string ip, int port, GameClientManager gameClientManager, ILogger<NetworkClient> log)
    {
        _log = log;
        _ip = ip;
        _port = port;
        _gameClientManager = gameClientManager;

        _tcp = new TcpClient();

        _sendChannel = Channel.CreateUnbounded<ClientMessage>();
        _receiveChannel = Channel.CreateUnbounded<ServerMessage>();
    }

    public bool IsConnected => _tcp?.Connected ?? false;

    public void Connect()
    {
        try
        {
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
        // Consume received messages from the channel on the main thread
        while (_receiveChannel.Reader.TryRead(out var serverMsg))
        {
            HandleServerMessage(serverMsg);
        }
    }


    private void HandleServerMessage(ServerMessage serverMsg)
    {
        EventBus.Publish(serverMsg);
    }

    public void SendClientMessage(ClientMessage message)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Cannot send message: not connected");
            return;
        }

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
                    if (_stream == null || !IsConnected) return;

                    try
                    {
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
                        await _stream.WriteAsync(bytes, 0, bytes.Length, token);
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

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_stream == null || !IsConnected) break;

                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (read == 0) break; // Connection closed

                try
                {
                    var serverMsg = JsonSerializer.Deserialize<ServerMessage>(buffer.AsSpan(0, read), _jsonOptions);
                    if (serverMsg != null)
                    {
                        _receiveChannel.Writer.TryWrite(serverMsg);
                    }
                }
                catch (JsonException)
                {
                    // Handle malformed JSON or partial reads
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
             Console.WriteLine($"ReceiveLoopAsync error: {ex.Message}");
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

        _tcp.Close();
        _tcp = null;

        Console.WriteLine("Disconnected from server");
    }
}
