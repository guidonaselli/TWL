using System.Net;
using System.Net.Sockets;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Net.Messages;

namespace TWL.Server.Simulation.Networking;

public class NetworkServer
{
    private readonly DbService _dbService;
    private readonly ServerQuestManager _questManager;
    private readonly CombatManager _combatManager;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerService _playerService;
    private readonly TcpListener _listener;
    private bool _running;
    private CancellationTokenSource _cts;

    public NetworkServer(int port, DbService dbService, ServerQuestManager questManager, CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbService = dbService;
        _questManager = questManager;
        _combatManager = combatManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
    }

    public void Start()
    {
        _running = true;
        _cts = new CancellationTokenSource();
        _listener.Start();
        _ = AcceptLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
        _listener.Stop();
        _cts?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (_running)
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                Console.WriteLine("New client connected!");
                var session = new ClientSession(client, _dbService, _questManager, _combatManager, _interactionManager, _playerService);
                session.StartHandling();
            }
        }
        catch (OperationCanceledException)
        {
            // Server stopping
        }
        catch (ObjectDisposedException)
        {
            // Listener stopped
        }
        catch (SocketException)
        {
            // Socket error or stopped
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Accept loop error: {ex}");
        }
    }

    public void SendMessageToClient(int playerId, ServerMessage msg)
    {
        // Implement sending message to specific client
        // This is a placeholder for the actual implementation
        Console.WriteLine($"Sending message to player {playerId}: {msg}");
    }
}
