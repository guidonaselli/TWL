using System.Net;
using System.Net.Sockets;
using TWL.Server.Persistence.Database;

namespace TWL.Server.Simulation.Networking;

public class NetworkManager
{
    private volatile List<IClientConnection> _clients = new();
    private readonly object _clientsLock = new();
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

        List<IClientConnection> clientsSnapshot;
        lock (_clientsLock)
        {
            clientsSnapshot = _clients;
            _clients = new List<IClientConnection>();
        }

        foreach (var client in clientsSnapshot) client.Disconnect();

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

                lock (_clientsLock)
                {
                    var newClients = new List<IClientConnection>(_clients);
                    newClients.Add(clientConnection);
                    _clients = newClients;
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

    public void RemoveClient(IClientConnection client)
    {
        lock (_clientsLock)
        {
            if (_clients.Contains(client))
            {
                var newClients = new List<IClientConnection>(_clients);
                newClients.Remove(client);
                _clients = newClients;
                Console.WriteLine("Client disconnected");
            }
        }
    }

    public void BroadcastMessage(byte[] data)
    {
        var clients = _clients;
        foreach (var client in clients)
            _ = client.SendMessageAsync(data);
    }
}

public class ClientConnection : IClientConnection
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

                await ProcessMessageAsync(buffer, bytesRead);
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

    private async Task ProcessMessageAsync(byte[] buffer, int bytesRead)
    {
        // Here implement your protocol
        // Example: Login handling
        // string username = ParseUsername(buffer);
        // string passwordHash = ParsePasswordHash(buffer);
        // _userId = _dbService.CheckLogin(username, passwordHash);

        // For now, just echo back the message
        try
        {
            await _stream.WriteAsync(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(byte[] data)
    {
        try
        {
            if (_tcpClient.Connected && _stream.CanWrite) await _stream.WriteAsync(data, 0, data.Length);
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