using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Net.Messages;

namespace TWL.Server.Simulation.Networking;

public class NetworkServer : INetworkServer
{
    private readonly CombatManager _combatManager;
    private readonly DbService _dbService;
    private readonly IEconomyService _economyManager;
    private readonly InteractionManager _interactionManager;
    private readonly TcpListener _listener;
    private readonly ServerMetrics _metrics;
    private readonly IMediator _mediator;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;
    private readonly ServerQuestManager _questManager;
    private readonly ReplayGuard _replayGuard;
    private readonly MovementValidator _movementValidator;
    private readonly SpawnManager _spawnManager;
    private readonly IWorldTriggerService _worldTriggerService;
    private readonly IPartyService _partyService;
    private readonly IOptions<RateLimiterOptions> _rateLimiterOptions;
    private CancellationTokenSource _cts;
    private bool _running;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public NetworkServer(int port, DbService dbService, PetManager petManager, ServerQuestManager questManager,
        CombatManager combatManager, InteractionManager interactionManager, PlayerService playerService,
        IEconomyService economyManager, ServerMetrics metrics, PetService petService, IMediator mediator,
        IWorldTriggerService worldTriggerService, SpawnManager spawnManager, ReplayGuard replayGuard, MovementValidator movementValidator, IPartyService partyService,
        IOptions<RateLimiterOptions> rateLimiterOptions)
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
        _mediator = mediator;
        _worldTriggerService = worldTriggerService;
        _spawnManager = spawnManager;
        _replayGuard = replayGuard;
        _movementValidator = movementValidator;
        _partyService = partyService;
        _rateLimiterOptions = rateLimiterOptions;
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
                var session = new ClientSession(client, _dbService, _petManager, _questManager, _combatManager,
                    _interactionManager, _playerService, _economyManager, _metrics, _petService, _mediator,
                    _worldTriggerService,
                    _spawnManager, _replayGuard, _movementValidator, _partyService, _rateLimiterOptions.Value);
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
