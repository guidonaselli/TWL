using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
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
        // Consume messages from the receive channel and handle them on the main thread
        while (_receiveChannel.Reader.TryRead(out var serverMsg))
        {
            HandleServerMessage(serverMsg);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested && _stream != null && IsConnected)
            {
                // Async read to avoid blocking threads
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (read == 0) break;

                try
                {
                    // OPTIMIZATION: Deserialize directly from Span<byte>, avoiding string allocation
                    var serverMsg = JsonSerializer.Deserialize<ServerMessage>(buffer.AsSpan(0, read), _jsonOptions);

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
                        // Optimization: Use System.Text.Json (SerializeToUtf8Bytes)
                        // to avoid intermediate string allocations and utilize existing _jsonOptions
                        var data = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
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

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && IsConnected && _stream != null)
            {
                var read = await _stream.ReadAsync(_buffer, 0, _buffer.Length, token);
                if (read <= 0) break;

                // OPTIMIZATION: Deserialize directly from Span<byte>, avoiding string allocation
                try
                {
                    var serverMsg = JsonSerializer.Deserialize<ServerMessage>(_buffer.AsSpan(0, read), _jsonOptions);
                    if (serverMsg != null)
                    {
                        await _receiveChannel.Writer.WriteAsync(serverMsg, token);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Deserialization error: {ex.Message}");
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
