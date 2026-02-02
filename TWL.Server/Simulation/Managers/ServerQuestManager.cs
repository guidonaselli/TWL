using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Quests;

namespace TWL.Server.Simulation.Managers;

public class ServerQuestManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly Dictionary<int, QuestDefinition> _questDefinitions = new();

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
            Console.WriteLine($"Warning: Quest path not found at {path}");
            return;
        }

        Console.WriteLine($"Total Loaded Quests: {_questDefinitions.Count}");
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
                Console.WriteLine($"QUEST VALIDATION FAILED ({path}):");
                foreach (var err in errors)
                {
                    Console.WriteLine($" - {err}");
                }

                throw new InvalidOperationException($"Quest validation failed in {path} with {errors.Count} errors.");
            }

            foreach (var def in list)
            {
                if (_questDefinitions.ContainsKey(def.QuestId))
                {
                    Console.WriteLine($"Warning: Duplicate QuestId {def.QuestId} in {path}. Overwriting.");
                }

                _questDefinitions[def.QuestId] = def;
            }

            Console.WriteLine($"Loaded {list.Count} quests from {path}");
        }
    }

    public void AddQuest(QuestDefinition quest)
    {
        var list = new List<QuestDefinition> { quest };
        var errors = QuestValidator.Validate(list);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Quest validation failed: {string.Join(", ", errors)}");
        }

        if (_questDefinitions.ContainsKey(quest.QuestId))
        {
            Console.WriteLine($"Warning: Duplicate QuestId {quest.QuestId}. Overwriting.");
        }

        _questDefinitions[quest.QuestId] = quest;
    }

    public virtual QuestDefinition? GetDefinition(int questId) => _questDefinitions.GetValueOrDefault(questId);
}