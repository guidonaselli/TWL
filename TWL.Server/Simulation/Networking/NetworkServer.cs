using System.Net;
using System.Net.Sockets;
using TWL.Server.Persistence.Database;
using TWL.Shared.Net.Messages;

namespace TWL.Server.Simulation.Networking;

public class NetworkServer
{
    private readonly DbService _dbService;
    private readonly TcpListener _listener;
    private bool _running;

    public NetworkServer(int port, DbService dbService)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbService = dbService;
    }

    public void Start()
    {
        _running = true;
        _listener.Start();
        var acceptThread = new Thread(AcceptLoop);
        acceptThread.Start();
    }

    public void Stop()
    {
        _running = false;
        _listener.Stop();
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            if (!_listener.Pending())
            {
                Thread.Sleep(100);
                continue;
            }

            var client = _listener.AcceptTcpClient();
            Console.WriteLine("New client connected!");
            var session = new ClientSession(client, _dbService);
            session.StartHandling();
        }
    }

    public void SendMessageToClient(int playerId, ServerMessage msg)
    {
        // Implement sending message to specific client
        // This is a placeholder for the actual implementation
        Console.WriteLine($"Sending message to player {playerId}: {msg}");
    }
}