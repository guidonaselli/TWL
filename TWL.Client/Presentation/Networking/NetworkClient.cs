using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        _buffer = new byte[4096];
    }

    public bool IsConnected => _tcp?.Connected ?? false;

    public void Connect()
    {
        try
        {
            _tcp.Connect(_ip, _port);
            _stream = _tcp.GetStream();
            Console.WriteLine($"Connected to server at {_ip}:{_port}");
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
            var serverMsg = JsonSerializer.Deserialize<ServerMessage>(_buffer.AsSpan(0, read), _jsonOptions);

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
        if (!IsConnected || _stream == null)
        {
            Console.WriteLine("Cannot send message: not connected");
            return;
        }

        try
        {
            // OPTIMIZATION: Serialize directly to UTF-8 bytes
            var data = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
            _stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending client message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
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
