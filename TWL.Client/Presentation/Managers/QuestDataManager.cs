using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TWL.Client.Presentation.Quests;

namespace TWL.Client.Presentation.Managers;

public class QuestDataManager
{
    private readonly Dictionary<int, QuestDefinition> _questDefinitions;

    public QuestDataManager()
    {
        _questDefinitions = new Dictionary<int, QuestDefinition>();
    }

    public void LoadQuestDefinitions(string path)
    {
        var json = File.ReadAllText(path);
        var list = JsonConvert.DeserializeObject<List<QuestDefinition>>(json);
        foreach (var def in list) _questDefinitions[def.QuestId] = def;
    }

    public QuestDefinition GetDefinition(int questId)
    {
        if (_questDefinitions.ContainsKey(questId))
            return _questDefinitions[questId];
        return null;
    }

    public IEnumerable<int> GetAllQuestIds()
    {
        return _questDefinitions.Keys;
    }
}