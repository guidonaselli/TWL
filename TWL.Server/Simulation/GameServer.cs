using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation;

public class GameServer
{
    private NetworkServer _netServer;

    // Accesores para DB o lógic
    public DbService DB { get; private set; }
    public ServerQuestManager QuestManager { get; private set; }
    public CombatManager CombatManager { get; private set; }
    public InteractionManager InteractionManager { get; private set; }
    public PlayerService PlayerService { get; private set; }

    public void Start()
    {
        // 1) Inicia DB
        var connString = "Host=localhost;Port=5432;Database=wonderland;Username=postgres;Password=1234";
        DB = new DbService(connString);
        DB.Init();

        // Init Player Persistence
        var playerRepo = new FilePlayerRepository();
        PlayerService = new PlayerService(playerRepo);
        PlayerService.Start();

        // 2) Carga definiciones (items, quests, skills)
        if (System.IO.File.Exists("Content/Data/skills.json"))
        {
            var json = System.IO.File.ReadAllText("Content/Data/skills.json");
            TWL.Shared.Domain.Skills.SkillRegistry.Instance.LoadSkills(json);
            Console.WriteLine("Skills loaded.");
        }
        else
        {
            Console.WriteLine("Warning: Content/Data/skills.json not found.");
        }

        QuestManager = new ServerQuestManager();
        QuestManager.Load("Content/Data/quests.json");

        InteractionManager = new InteractionManager();
        InteractionManager.Load("Content/Data/interactions.json");

        CombatManager = new CombatManager(new SystemRandomService());
        PopulateTestWorld();

        // 3) Inicia Network
        _netServer = new NetworkServer(9050, DB, QuestManager, CombatManager, InteractionManager, PlayerService);
        _netServer.Start();

        Console.WriteLine("GameServer started on port 9050.");
    }

    public void Stop()
    {
        _netServer?.Stop();
        PlayerService?.Stop();
        DB?.Dispose();
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
    }
}