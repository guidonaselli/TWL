using System.Text.Json;
using TWL.Shared.Domain.Quests;

namespace TWL.Client.Presentation.Managers;

public class QuestDataManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Dictionary<int, QuestDefinition> _questDefinitions;

    public QuestDataManager()
    {
        _questDefinitions = new Dictionary<int, QuestDefinition>();
    }

    public void LoadQuestDefinitions(string path)
    {
        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<QuestDefinition>>(json, _jsonOptions);
        if (list != null)
        {
            foreach (var def in list)
            {
                _questDefinitions[def.QuestId] = def;
            }
        }
    }

    public QuestDefinition GetDefinition(int questId)
    {
        if (_questDefinitions.ContainsKey(questId))
        {
            return _questDefinitions[questId];
        }

        return null;
    }

    public IEnumerable<int> GetAllQuestIds() => _questDefinitions.Keys;
}