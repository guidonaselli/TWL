using TWL.Server.Persistence.Database;
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

    public void Start()
    {
        // 1) Inicia DB
        var connString = "Host=localhost;Port=5432;Database=wonderland;Username=postgres;Password=1234";
        DB = new DbService(connString);
        DB.Init();

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

        CombatManager = new CombatManager();
        PopulateTestWorld();

        // 3) Inicia Network
        _netServer = new NetworkServer(9050, DB, QuestManager, CombatManager, InteractionManager);
        _netServer.Start();

        Console.WriteLine("GameServer started on port 9050.");
    }

    public void Stop()
    {
        _netServer?.Stop();
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
    }
}