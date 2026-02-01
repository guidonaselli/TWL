// File: `TWL.Client/Managers/ClientQuestManager.cs`

using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Client.Presentation.Managers;

public class ActiveQuest
{
    // For each objective, how many have been completed.
    public List<int> CurrentCounts;
    public int QuestId;
    public QuestState State;

    public ActiveQuest(int questId, int objectiveCount)
    {
        QuestId = questId;
        State = QuestState.InProgress;
        CurrentCounts = new List<int>(new int[objectiveCount]);
    }
}

public class PlayerQuestSave
{
    public List<int> CurrentCounts;
    public int QuestId;
    public QuestState State;
}

public class ClientQuestManager
{
    // Active quests of the player.
    private readonly Dictionary<int, ActiveQuest> _activeQuests;

    // Data manager with quest definitions.
    private readonly QuestDataManager _dataManager;

    public ClientQuestManager(QuestDataManager dataManager)
    {
        _dataManager = dataManager;
        _activeQuests = new Dictionary<int, ActiveQuest>();
    }

    // Check if a quest can be started (e.g. prerequisites met).
    public bool CanStartQuest(int questId)
    {
        var def = _dataManager.GetDefinition(questId);
        if (def == null)
        {
            return false;
        }

        if (_activeQuests.ContainsKey(questId))
        {
            return false;
        }

        foreach (var requiredQuestId in def.Requirements)
        {
            if (!_activeQuests.ContainsKey(requiredQuestId))
            {
                return false;
            }

            if (_activeQuests[requiredQuestId].State != QuestState.RewardClaimed &&
                _activeQuests[requiredQuestId].State != QuestState.Completed)
            {
                return false;
            }
        }

        return true;
    }

    // Start a quest if requirements are met.
    public bool StartQuest(int questId)
    {
        if (!CanStartQuest(questId))
        {
            return false;
        }

        var def = _dataManager.GetDefinition(questId);
        var objCount = def.Objectives.Count;
        var active = new ActiveQuest(questId, objCount);
        _activeQuests[questId] = active;
        return true;
    }

    // Get the current state of a quest.
    public QuestState GetState(int questId)
    {
        if (_activeQuests.ContainsKey(questId))
        {
            return _activeQuests[questId].State;
        }

        return QuestState.NotStarted;
    }

    // Update quest progress for a particular objective.
    public void UpdateQuestProgress(int questId, int objectiveIndex, int amount = 1)
    {
        if (!_activeQuests.ContainsKey(questId))
        {
            return;
        }

        var def = _dataManager.GetDefinition(questId);
        if (def == null)
        {
            return;
        }

        var active = _activeQuests[questId];
        if (active.State != QuestState.InProgress)
        {
            return;
        }

        if (objectiveIndex < 0 || objectiveIndex >= def.Objectives.Count)
        {
            return;
        }

        active.CurrentCounts[objectiveIndex] += amount;
        var required = def.Objectives[objectiveIndex].RequiredCount;
        if (active.CurrentCounts[objectiveIndex] >= required)
        {
            active.CurrentCounts[objectiveIndex] = required;
        }

        if (AllObjectivesCompleted(def, active))
        {
            active.State = QuestState.Completed;
        }
    }

    // Checks if all objectives in a quest are completed.
    private bool AllObjectivesCompleted(QuestDefinition def, ActiveQuest active)
    {
        for (var i = 0; i < def.Objectives.Count; i++)
        {
            if (active.CurrentCounts[i] < def.Objectives[i].RequiredCount)
            {
                return false;
            }
        }

        return true;
    }

    // Claim quest reward once completed.
    public void ClaimReward(int questId, PlayerCharacter player)
    {
        if (!_activeQuests.ContainsKey(questId))
        {
            return;
        }

        var active = _activeQuests[questId];
        var def = _dataManager.GetDefinition(questId);
        if (active.State == QuestState.Completed)
        {
            player.GainExp(def.Rewards.Exp);
            player.Gold += def.Rewards.Gold;
            foreach (var item in def.Rewards.Items)
            {
                // Add items to the player's inventory as needed.
            }

            active.State = QuestState.RewardClaimed;
        }
    }

    // Handle enemy kill events and update quest progress.
    public void OnEnemyKilled(string enemyName)
    {
        foreach (var kv in _activeQuests)
        {
            if (kv.Value.State != QuestState.InProgress)
            {
                continue;
            }

            var def = _dataManager.GetDefinition(kv.Key);
            if (def == null)
            {
                continue;
            }

            for (var i = 0; i < def.Objectives.Count; i++)
            {
                var objDef = def.Objectives[i];
                if (objDef.Type == "Kill" && objDef.TargetName == enemyName)
                {
                    UpdateQuestProgress(kv.Key, i);
                }
            }
        }
    }

    // New method: updates quest based on server-sent QuestUpdate.
    public void OnQuestUpdate(QuestUpdate update)
    {
        if (_activeQuests.ContainsKey(update.QuestId))
        {
            var active = _activeQuests[update.QuestId];
            active.State = update.State;
            if (update.CurrentCounts != null && update.CurrentCounts.Count == active.CurrentCounts.Count)
            {
                active.CurrentCounts = new List<int>(update.CurrentCounts);
            }
        }
        else
        {
            var def = _dataManager.GetDefinition(update.QuestId);
            if (def != null)
            {
                var active = new ActiveQuest(update.QuestId, def.Objectives.Count);
                active.State = update.State;
                if (update.CurrentCounts != null && update.CurrentCounts.Count == def.Objectives.Count)
                {
                    active.CurrentCounts = new List<int>(update.CurrentCounts);
                }

                _activeQuests.Add(update.QuestId, active);
            }
        }
    }

    // Build quest save data for persistence.
    public List<PlayerQuestSave> BuildSaveData()
    {
        var list = new List<PlayerQuestSave>();
        foreach (var kv in _activeQuests)
        {
            var aQuest = kv.Value;
            list.Add(new PlayerQuestSave
            {
                QuestId = aQuest.QuestId,
                State = aQuest.State,
                CurrentCounts = new List<int>(aQuest.CurrentCounts)
            });
        }

        return list;
    }
}