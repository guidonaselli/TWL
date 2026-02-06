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
using TWL.Shared.Domain.Skills;

namespace TWL.Server.Simulation;

public class GameServer
{
    private NetworkServer _netServer;

    // Accesores para DB o lógic
    public DbService DB { get; private set; }
    public PetManager PetManager { get; private set; }
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
        var connString = "Host=localhost;Port=5432;Database=wonderland;Username=postgres;Password=1234";
        DB = new DbService(connString);
        DB.Init();

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
        CombatManager = new CombatManager(combatResolver, random, SkillRegistry.Instance, statusEngine);

        PetService = new PetService(PlayerService, PetManager, MonsterManager, CombatManager, random, NullLogger<PetService>.Instance);
        EconomyManager = new EconomyManager();

        SpawnManager = new SpawnManager(MonsterManager, CombatManager, random, PlayerService);
        SpawnManager.Load("Content/Data/spawns");

        // Init World System
        var scheduler = new WorldScheduler(NullLogger<WorldScheduler>.Instance, Metrics);
        scheduler.Start();

        var mapLoader = new MapLoader(NullLogger<MapLoader>.Instance);
        var worldTriggerService = new WorldTriggerService(NullLogger<WorldTriggerService>.Instance, Metrics, PlayerService, scheduler);
        worldTriggerService.RegisterHandler(new MapTransitionHandler());

        // Load Maps
        if (Directory.Exists("Content/Maps"))
        {
            var mapFiles = Directory.GetFiles("Content/Maps", "*.tmx", SearchOption.AllDirectories);
            var loadedMaps = new List<ServerMap>();
            foreach (var file in mapFiles)
            {
                try
                {
                    var map = mapLoader.LoadMap(file);
                    loadedMaps.Add(map);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load map {file}: {ex.Message}");
                }
            }

            worldTriggerService.LoadMaps(loadedMaps);
            Console.WriteLine($"Loaded {loadedMaps.Count} maps.");
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

    public void Stop()
    {
        _netServer?.Stop();
        PlayerService?.Stop();
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