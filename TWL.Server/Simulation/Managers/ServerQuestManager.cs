using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Shared.Domain.Quests;

namespace TWL.Server.Simulation.Managers;

public class ServerQuestManager
{
    private readonly Dictionary<int, QuestDefinition> _questDefinitions = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            System.Console.WriteLine($"Warning: Quest file not found at {path}");
            return;
        }

        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<QuestDefinition>>(json, _jsonOptions);
        if (list != null)
        {
            foreach (var def in list)
            {
                _questDefinitions[def.QuestId] = def;
            }
        }
        System.Console.WriteLine($"Loaded {_questDefinitions.Count} quests from {path}");
    }

    public QuestDefinition? GetDefinition(int questId)
    {
        return _questDefinitions.GetValueOrDefault(questId);
    }
}
