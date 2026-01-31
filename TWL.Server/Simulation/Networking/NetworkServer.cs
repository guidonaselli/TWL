using System.Net;
using System.Net.Sockets;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Net.Messages;

namespace TWL.Server.Simulation.Networking;

public class NetworkServer
{
    private readonly DbService _dbService;
    private readonly PetManager _petManager;
    private readonly ServerQuestManager _questManager;
    private readonly CombatManager _combatManager;
    private readonly InteractionManager _interactionManager;
    private readonly PlayerService _playerService;
    private readonly IEconomyService _economyManager;
    private readonly ServerMetrics _metrics;
    private readonly TWL.Server.Services.PetService _petService;
    private readonly IWorldTriggerService _worldTriggerService;
    private readonly SpawnManager _spawnManager;
    private readonly TcpListener _listener;
    private bool _running;
    private CancellationTokenSource _cts;

    public NetworkServer(int port, DbService dbService, PetManager petManager, ServerQuestManager questManager, CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService, IEconomyService economyManager, ServerMetrics metrics, TWL.Server.Services.PetService petService, IWorldTriggerService worldTriggerService, SpawnManager spawnManager)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbService = dbService;
        _petManager = petManager;
        _questManager = questManager;
        _combatManager = combatManager;
        _interactionManager = interactionManager;
        _playerService = playerService;
        _economyManager = economyManager;
        _metrics = metrics;
        _petService = petService;
        _worldTriggerService = worldTriggerService;
        _spawnManager = spawnManager;
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
                var session = new ClientSession(client, _dbService, _petManager, _questManager, _combatManager, _interactionManager, _playerService, _economyManager, _metrics, _petService, _worldTriggerService, _spawnManager);
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
