using TWL.Server.Persistence.Database;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation;

public class GameServer
{
    private NetworkManager _netManager;

    // Accesores para DB o lógic
    public DbService DB { get; private set; }
    public ServerQuestManager QuestManager { get; private set; }

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

        // 3) Inicia Network
        _netManager = new NetworkManager(this);
        _netManager.Start(9050); // un puerto a elección

        Console.WriteLine("GameServer started on port 9050.");
    }

    public void Stop()
    {
        _netManager?.Stop();
        DB?.Dispose();
        Console.WriteLine("GameServer stopped.");
    }
}