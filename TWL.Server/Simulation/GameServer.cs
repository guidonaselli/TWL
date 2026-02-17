using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Domain.World;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Net.Network;

namespace TWL.Server.Simulation;

public class GameServer
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private NetworkServer _netServer;

    // Accesores para DB o lógic
    public DbService DB { get; private set; }
    public PetManager PetManager { get; private set; }

    // Add constructor for DI or manual instantiation with dependencies
    public GameServer(DbService dbService)
    {
        DB = dbService;
    }
    public MonsterManager MonsterManager { get; private set; }
    public NpcManager NpcManager { get; private set; }
    public ServerQuestManager QuestManager { get; private set; }
    public CombatManager CombatManager { get; private set; }
    public InteractionManager InteractionManager { get; private set; }
    public PlayerService PlayerService { get; private set; }
    public PetService PetService { get; private set; }
    public IEconomyService EconomyManager { get; private set; }
    public SpawnManager SpawnManager { get; private set; }
    public ServerMetrics Metrics { get; private set; }

    public void Start()
    {
        // Init Metrics
        Metrics = new ServerMetrics();

        // 1) Inicia DB
        // TODO: This manual instantiation is legacy and conflicts with DI in Program.cs.
        // We should eventually inject DbService into GameServer, but GameServer is currently instantiated manually in Program.cs?
        // No, Program.cs registers services but doesn't seem to use GameServer class directly anymore, it uses ServerWorker.
        // Wait, I see "svcs.AddHostedService<ServerWorker>();" in Program.cs.
        // Let's check ServerWorker.cs to see if it uses GameServer.
        // If GameServer is legacy or not used via DI, we might need to update it or leave it if it's not the entry point.
        // Assuming GameServer is the old entry point or helper.
        var connString = "Host=localhost;Port=5432;Database=wonderland;Username=postgres;Password=1234";
        // We can't easily pass IServiceProvider here without changing the signature.
        // For now, let's just null it out or fix it later if this class is used.
        // DB = new DbService(connString, null); // This will crash if Init() is called and tries to CreateScope.

        // BETTER: If GameServer is used, it should receive DbService via constructor.
        // But for this task (PERS-001a), I just need to make it compile.
        // DB = new DbService(connString, null);
        // DB.InitDatabase(); // Call the legacy init directly to avoid scope issues?

        // Actually, let's look at Program.cs again. It registers "svcs.AddHostedService<ServerWorker>();"
        // It does NOT register GameServer.
        // So GameServer might be dead code or used by ServerWorker.
        // Let's check ServerWorker.cs.

        // Init Player Persistence
        var playerRepo = new FilePlayerRepository();
        PlayerService = new PlayerService(playerRepo, Metrics);
        PlayerService.Start();

        // 2) Carga definiciones (items, quests, skills)
        if (File.Exists("Content/Data/skills.json"))
        {
            var json = File.ReadAllText("Content/Data/skills.json");
            SkillRegistry.Instance.LoadSkills(json);
            Console.WriteLine("Skills loaded.");
        }
        else
        {
            Console.WriteLine("Warning: Content/Data/skills.json not found.");
        }

        PetManager = new PetManager();
        PetManager.Load("Content/Data/pets.json");
        PetManager.LoadAmityItems("Content/Data/amity_items.json");

        MonsterManager = new MonsterManager();
        MonsterManager.Load("Content/Data/monsters.json");

        NpcManager = new NpcManager();
        NpcManager.Load("Content/Data/npcs.json");

        QuestManager = new ServerQuestManager();
        QuestManager.Load("Content/Data/quests.json");

        InteractionManager = new InteractionManager();
        InteractionManager.Load("Content/Data/interactions.json");

        var random = new SeedableRandomService(NullLogger<SeedableRandomService>.Instance);
        var combatResolver = new StandardCombatResolver(random, SkillRegistry.Instance);
        var statusEngine = new StatusEngine();
        var autoBattleManager = new AutoBattleManager(SkillRegistry.Instance);
        CombatManager = new CombatManager(combatResolver, random, SkillRegistry.Instance, statusEngine, autoBattleManager);

        CombatManager.OnCombatActionResolved += (encounterId, results) =>
        {
            var participants = CombatManager.GetParticipants(encounterId);
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(results, _jsonOptions);
            var netMsg = new NetMessage
            {
                Op = Opcode.AttackBroadcast,
                JsonPayload = payloadJson
            };

            var notifiedPlayers = new HashSet<int>();

            foreach (var p in participants)
            {
                int? playerIdToNotify = null;
                if (p is ServerCharacter sc && sc.MonsterId == 0)
                {
                    playerIdToNotify = sc.Id;
                }
                else if (p is ServerPet pet && pet.OwnerId > 0)
                {
                    playerIdToNotify = pet.OwnerId;
                }

                if (playerIdToNotify.HasValue && !notifiedPlayers.Contains(playerIdToNotify.Value))
                {
                    var session = PlayerService.GetSession(playerIdToNotify.Value);
                    if (session != null)
                    {
                        _ = session.SendAsync(netMsg);
                        notifiedPlayers.Add(playerIdToNotify.Value);
                    }
                }
            }
        };

        PetService = new PetService(PlayerService, PetManager, MonsterManager, CombatManager, random, NullLogger<PetService>.Instance);
        EconomyManager = new EconomyManager();

        SpawnManager = new SpawnManager(MonsterManager, CombatManager, random, PlayerService);
        SpawnManager.Load("Content/Data/spawns");

        // Init World System
        var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, Metrics);
        scheduler.ScheduleRepeating(() => SpawnManager.Update(0.05f), 1, "SpawnManager.Update");
        scheduler.ScheduleRepeating(() => CombatManager.Update(scheduler.CurrentTick), 1, "CombatManager.Update");
        scheduler.Start();

        var mapLoader = new MapLoader(NullLogger<MapLoader>.Instance);
        var mapRegistry = new MapRegistry(NullLogger<MapRegistry>.Instance, mapLoader);
        var worldTriggerService = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, Metrics, PlayerService, scheduler, mapRegistry);
        worldTriggerService.RegisterHandler(new MapTransitionHandler());

        // Load Maps
        if (Directory.Exists("Content/Maps"))
        {
            mapRegistry.Load("Content/Maps");
        }
        else
        {
            Console.WriteLine("Warning: Content/Maps not found.");
        }

        PopulateTestWorld();

        // Setup Mediator
        var mediator = new Mediator();
        mediator.Register(new UseSkillHandler(CombatManager));
        mediator.Register(new InteractHandler(InteractionManager));

        // 3) Inicia Network
        _netServer = new NetworkServer(9050, DB, PetManager, QuestManager, CombatManager, InteractionManager,
            PlayerService, EconomyManager, Metrics, PetService, mediator, worldTriggerService, SpawnManager);
        _netServer.Start();

        Console.WriteLine("GameServer started on port 9050.");
    }

    public async Task StopAsync()
    {
        _netServer?.Stop();
        if (PlayerService != null)
        {
            await PlayerService.StopAsync();
        }
        DB?.Dispose();
        (EconomyManager as IDisposable)?.Dispose();
        Console.WriteLine("GameServer stopped.");
    }

    private void PopulateTestWorld()
    {
        // Add a Jaguar for Quest 1103
        var jaguar = new ServerCharacter
        {
            Id = 9001,
            Name = "Jaguar",
            Hp = 50,
            Str = 15,
            Exp = 0,
            Gold = 0
        };
        CombatManager.AddCharacter(jaguar);
        Console.WriteLine("Test World Populated: Added Jaguar (9001).");

        // Add Ruins Guardian for Quest 1203
        var guardian = new ServerCharacter
        {
            Id = 9002,
            Name = "RuinsGuardian",
            Hp = 200,
            Str = 25,
            Exp = 300,
            Gold = 50
        };
        CombatManager.AddCharacter(guardian);
        Console.WriteLine("Test World Populated: Added RuinsGuardian (9002).");

        // Add Ruins Bat for Quest 1205
        var bat = new ServerCharacter
        {
            Id = 9003,
            Name = "RuinsBat",
            Hp = 30,
            Str = 10,
            Exp = 100,
            Gold = 10
        };
        CombatManager.AddCharacter(bat);
        Console.WriteLine("Test World Populated: Added RuinsBat (9003).");

        // Add Giant Spider for Quest 1205
        var spider = new ServerCharacter
        {
            Id = 9004,
            Name = "GiantSpider",
            Hp = 80,
            Str = 20,
            Exp = 200,
            Gold = 20
        };
        CombatManager.AddCharacter(spider);
        Console.WriteLine("Test World Populated: Added GiantSpider (9004).");

        // Add Electric Eel for Quest 2302
        var eel = new ServerCharacter
        {
            Id = 9005,
            Name = "ElectricEel",
            Hp = 60,
            Str = 18,
            Exp = 150,
            Gold = 25
        };
        CombatManager.AddCharacter(eel);
        Console.WriteLine("Test World Populated: Added ElectricEel (9005).");

        // Add Cave Bat for Quest 1051
        var caveBat = new ServerCharacter
        {
            Id = 9101,
            Name = "Cave Bat",
            Hp = 40,
            Str = 12,
            Exp = 40,
            Gold = 5
        };
        CombatManager.AddCharacter(caveBat);
        Console.WriteLine("Test World Populated: Added Cave Bat (9101).");

        // Add Stone Golem for Quest 1053
        var stoneGolem = new ServerCharacter
        {
            Id = 9102,
            Name = "Stone Golem",
            Hp = 150,
            Str = 25,
            Con = 20, // Higher def
            Exp = 150,
            Gold = 50
        };
        CombatManager.AddCharacter(stoneGolem);
        Console.WriteLine("Test World Populated: Added Stone Golem (9102).");

        // Add Bandido del Camino for Quest 1102
        var bandido = new ServerCharacter
        {
            Id = 9200,
            Name = "Bandido del Camino",
            Hp = 80,
            Str = 15,
            Exp = 80,
            Gold = 20
        };
        CombatManager.AddCharacter(bandido);
        Console.WriteLine("Test World Populated: Added Bandido del Camino (9200).");

        // Add Lobo del Bosque for Quest 3003
        var lobo = new ServerCharacter
        {
            Id = 9202,
            Name = "Lobo del Bosque",
            Hp = 60,
            Str = 12,
            Exp = 60,
            Gold = 10
        };
        CombatManager.AddCharacter(lobo);
        Console.WriteLine("Test World Populated: Added Lobo del Bosque (9202).");

        // Add Caravan Leader for Quest 1100
        var caravanLeader = new ServerCharacter
        {
            Id = 9301,
            Name = "Caravan Leader",
            Hp = 100,
            Str = 10,
            Exp = 0,
            Gold = 0
        };
        CombatManager.AddCharacter(caravanLeader);
        Console.WriteLine("Test World Populated: Added Caravan Leader (9301).");

        // Add Sendero Norte for Quest 1101
        var sendero = new ServerCharacter
        {
            Id = 9302,
            Name = "Sendero Norte",
            Hp = 1000,
            Str = 0,
            Exp = 0,
            Gold = 0
        };
        CombatManager.AddCharacter(sendero);
        Console.WriteLine("Test World Populated: Added Sendero Norte (9302).");

        // Add Puerta de la Ciudad for Quest 1103
        var puerta = new ServerCharacter
        {
            Id = 9303,
            Name = "Puerta de la Ciudad",
            Hp = 1000,
            Str = 0,
            Exp = 0,
            Gold = 0
        };
        CombatManager.AddCharacter(puerta);
        Console.WriteLine("Test World Populated: Added Puerta de la Ciudad (9303).");

        // Add Funcionario de Registro for Quest 1104
        var funcionario = new ServerCharacter
        {
            Id = 9304,
            Name = "Funcionario de Registro",
            Hp = 50,
            Str = 5,
            Exp = 0,
            Gold = 0
        };
        CombatManager.AddCharacter(funcionario);
        Console.WriteLine("Test World Populated: Added Funcionario de Registro (9304).");
    }
}