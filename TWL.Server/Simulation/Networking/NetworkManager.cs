﻿using System.Net;
using System.Net.Sockets;
using TWL.Server.Persistence.Database;

namespace TWL.Server.Simulation.Networking;

public class NetworkManager
{
    private readonly List<ClientConnection> _clients = new();
    private readonly GameServer _gameServer;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning;
    private TcpListener _listener;

    public NetworkManager(GameServer gameServer)
    {
        _gameServer = gameServer;
    }

    public void Start(int port)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
        Console.WriteLine($"Network manager listening on port {port}");
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _listener.Stop();

        lock (_clients)
        {
            foreach (var client in _clients.ToArray()) client.Disconnect();
            _clients.Clear();
        }

        Console.WriteLine("Network manager stopped");
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync();
                var clientConnection = new ClientConnection(client, this, _gameServer.DB);

                lock (_clients)
                {
                    _clients.Add(clientConnection);
                }

                _ = Task.Run(() => clientConnection.ProcessMessagesAsync(token));
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting clients: {ex.Message}");
        }
    }

    public void RemoveClient(ClientConnection client)
    {
        lock (_clients)
        {
            if (_clients.Contains(client))
            {
                _clients.Remove(client);
                Console.WriteLine("Client disconnected");
            }
        }
    }

    public void BroadcastMessage(byte[] data)
    {
        lock (_clients)
        {
            foreach (var client in _clients.ToArray()) client.SendMessage(data);
        }
    }
}

public class ClientConnection
{
    private readonly DbService _dbService;
    private readonly NetworkManager _networkManager;
    private readonly NetworkStream _stream;
    private readonly TcpClient _tcpClient;
    private int _userId = -1; // Set after authentication

    public ClientConnection(TcpClient tcpClient, NetworkManager networkManager, DbService dbService)
    {
        _tcpClient = tcpClient;
        _networkManager = networkManager;
        _dbService = dbService;
        _stream = tcpClient.GetStream();
    }

    public async Task ProcessMessagesAsync(CancellationToken token)
    {
        try
        {
            var buffer = new byte[4096];

            while (!token.IsCancellationRequested && _tcpClient.Connected)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                if (bytesRead == 0)
                    break; // Client disconnected

                ProcessMessage(buffer, bytesRead);
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"Error processing client messages: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    private void ProcessMessage(byte[] buffer, int bytesRead)
    {
        // Here implement your protocol
        // Example: Login handling
        // string username = ParseUsername(buffer);
        // string passwordHash = ParsePasswordHash(buffer);
        // _userId = _dbService.CheckLogin(username, passwordHash);

        // For now, just echo back the message
        try
        {
            _stream.Write(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response: {ex.Message}");
        }
    }

    public void SendMessage(byte[] data)
    {
        try
        {
            if (_tcpClient.Connected && _stream.CanWrite) _stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            Disconnect();
        }
    }

    public void Disconnect()
    {
        try
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disconnecting client: {ex.Message}");
        }
        finally
        {
            _networkManager.RemoveClient(this);
        }
    }
}