using System;
using System.Net.Sockets;
using System.Text;
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
    private readonly byte[] _buffer;
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
    private CancellationTokenSource? _cts;

    public NetworkClient(string ip, int port, GameClientManager gameClientManager, ILogger<NetworkClient> log)
    {
        _log = log;
        _ip = ip;
        _port = port;
        _gameClientManager = gameClientManager;

        _tcp = new TcpClient();
        _buffer = new byte[4096];

        _sendChannel = Channel.CreateUnbounded<ClientMessage>();
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            throw;
        }
    }

    public void Update()
    {
        // 1) si nunca conectó, salimos
        if (!IsConnected || _stream == null)
            return;

        try
        {
            if (!_stream.DataAvailable) return;

            var read = _stream.Read(_buffer, 0, _buffer.Length);
            if (read <= 0) return;

            // OPTIMIZATION: Deserialize directly from Span<byte>, avoiding string allocation
            var serverMsg = System.Text.Json.JsonSerializer.Deserialize<ServerMessage>(_buffer.AsSpan(0, read), _jsonOptions);

            if (serverMsg != null)
                HandleServerMessage(serverMsg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Network error in update: {ex.Message}");
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
                        var json = JsonConvert.SerializeObject(message);
                        var data = Encoding.UTF8.GetBytes(json);
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

        _tcp.Close();
        _tcp = null;

        Console.WriteLine("Disconnected from server");
    }
}
