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
        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                LoadFile(file);
            }
        }
        else if (File.Exists(path))
        {
            LoadFile(path);
        }
        else
        {
            System.Console.WriteLine($"Warning: Quest path not found at {path}");
            return;
        }

        System.Console.WriteLine($"Total Loaded Quests: {_questDefinitions.Count}");
    }

    private void LoadFile(string path)
    {
        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<QuestDefinition>>(json, _jsonOptions);
        if (list != null)
        {
            var errors = QuestValidator.Validate(list);
            if (errors.Count > 0)
            {
                System.Console.WriteLine($"QUEST VALIDATION FAILED ({path}):");
                foreach (var err in errors)
                {
                    System.Console.WriteLine($" - {err}");
                }
                throw new System.InvalidOperationException($"Quest validation failed in {path} with {errors.Count} errors.");
            }

            foreach (var def in list)
            {
                if (_questDefinitions.ContainsKey(def.QuestId))
                {
                    System.Console.WriteLine($"Warning: Duplicate QuestId {def.QuestId} in {path}. Overwriting.");
                }
                _questDefinitions[def.QuestId] = def;
            }
            System.Console.WriteLine($"Loaded {list.Count} quests from {path}");
        }
    }

    public virtual QuestDefinition? GetDefinition(int questId)
    {
        return _questDefinitions.GetValueOrDefault(questId);
    }
}
