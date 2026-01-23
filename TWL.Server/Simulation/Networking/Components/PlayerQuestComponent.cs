using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Simulation.Networking.Components;

public class PlayerQuestComponent
{
    private readonly ServerQuestManager _questManager;
    private readonly object _lock = new();

    public bool IsDirty { get; set; }

    // QuestId -> State
    public Dictionary<int, QuestState> QuestStates { get; private set; } = new();

    // QuestId -> List of counts per objective
    public Dictionary<int, List<int>> QuestProgress { get; private set; } = new();

    // Player Flags
    public HashSet<string> Flags { get; private set; } = new();

    public PlayerQuestComponent(ServerQuestManager questManager)
    {
        _questManager = questManager;
    }

    public bool CanStartQuest(int questId)
    {
        lock (_lock)
        {
            var def = _questManager.GetDefinition(questId);
            if (def == null) return false;

            if (QuestStates.ContainsKey(questId) && QuestStates[questId] != QuestState.NotStarted)
            {
                if (!def.Repeatable || QuestStates[questId] != QuestState.RewardClaimed)
                    return false;
            }

            foreach (var reqId in def.Requirements)
            {
                if (!QuestStates.ContainsKey(reqId)) return false;
                var state = QuestStates[reqId];
                if (state != QuestState.Completed && state != QuestState.RewardClaimed)
                    return false;
            }

            foreach (var flag in def.RequiredFlags)
            {
                if (!Flags.Contains(flag)) return false;
            }

            return true;
        }
    }

    public bool StartQuest(int questId)
    {
        lock (_lock)
        {
            var def = _questManager.GetDefinition(questId);
            if (def == null) return false;

            // Check if already started/completed, unless Repeatable
            if (QuestStates.ContainsKey(questId) && QuestStates[questId] != QuestState.NotStarted)
            {
                if (!def.Repeatable || QuestStates[questId] != QuestState.RewardClaimed)
                    return false;
            }

            // Check Requirements (Quest Chains)
            foreach (var reqId in def.Requirements)
            {
                if (!QuestStates.ContainsKey(reqId)) return false;
                var state = QuestStates[reqId];
                if (state != QuestState.Completed && state != QuestState.RewardClaimed)
                    return false;
            }

            foreach (var flag in def.RequiredFlags)
            {
                if (!Flags.Contains(flag)) return false;
            }

            QuestStates[questId] = QuestState.InProgress;
            QuestProgress[questId] = new List<int>(new int[def.Objectives.Count]); // Init with zeros

            IsDirty = true;
            return true;
        }
    }

    public void UpdateProgress(int questId, int objectiveIndex, int amount)
    {
        lock (_lock)
        {
            UpdateProgressInternal(questId, objectiveIndex, amount);
        }
    }

    private void UpdateProgressInternal(int questId, int objectiveIndex, int amount)
    {
        if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.InProgress) return;

        var def = _questManager.GetDefinition(questId);
        if (def == null) return;

        if (objectiveIndex < 0 || objectiveIndex >= def.Objectives.Count) return;

        var currentList = QuestProgress[questId];
        currentList[objectiveIndex] += amount;

        if (currentList[objectiveIndex] > def.Objectives[objectiveIndex].RequiredCount)
            currentList[objectiveIndex] = def.Objectives[objectiveIndex].RequiredCount;

        CheckCompletion(questId);
        IsDirty = true;
    }

    private void CheckCompletion(int questId)
    {
        var def = _questManager.GetDefinition(questId);
        if (def == null) return;

        var counts = QuestProgress[questId];
        bool allComplete = true;
        for (int i = 0; i < def.Objectives.Count; i++)
        {
            if (counts[i] < def.Objectives[i].RequiredCount)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete)
        {
            QuestStates[questId] = QuestState.Completed;
        }
    }

    public bool ClaimReward(int questId)
    {
        lock (_lock)
        {
            if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.Completed)
                return false;

            var def = _questManager.GetDefinition(questId);
            if (def != null)
            {
                foreach (var f in def.FlagsSet) Flags.Add(f);
                foreach (var f in def.FlagsClear) Flags.Remove(f);
            }

            QuestStates[questId] = QuestState.RewardClaimed;
            IsDirty = true;
            return true;
        }
    }

    /// <summary>
    /// Attempts to progress any active quest that matches the given type and target.
    /// </summary>
    /// <returns>List of QuestIds that were updated.</returns>
    public List<int> TryProgress(string type, string targetName)
    {
        var updatedQuests = new List<int>();
        TryProgress(updatedQuests, targetName, type);
        return updatedQuests;
    }

    /// <summary>
    /// Optimized overload to check multiple types at once and use an existing collection.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, params string[] types)
    {
        lock (_lock)
        {
            // Iterate directly over QuestStates.
            // CheckCompletion only modifies values (states), does not add/remove keys.
            // Dictionary enumeration is safe against value modifications in .NET Core+.
            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress) continue;

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null) continue;

                bool changed = false;
                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];

                    // Match TargetName first (fast string check)
                    if (!string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Match Type
                    bool typeMatch = false;
                    for (int t = 0; t < types.Length; t++)
                    {
                        if (string.Equals(obj.Type, types[t], StringComparison.OrdinalIgnoreCase))
                        {
                            typeMatch = true;
                            break;
                        }
                    }

                    if (typeMatch)
                    {
                        // Check if not already complete
                        if (QuestProgress[questId][i] < obj.RequiredCount)
                        {
                            UpdateProgressInternal(questId, i, 1);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    output.Add(questId);
                }
            }
        }
    }

    public QuestData GetSaveData()
    {
        lock (_lock)
        {
            var data = new QuestData
            {
                States = new Dictionary<int, QuestState>(QuestStates),
                Progress = new Dictionary<int, List<int>>(),
                Flags = new HashSet<string>(Flags)
            };

            foreach(var kvp in QuestProgress)
            {
                data.Progress[kvp.Key] = new List<int>(kvp.Value);
            }

            return data;
        }
    }

    public void LoadSaveData(QuestData data)
    {
        lock (_lock)
        {
            QuestStates.Clear();
            if (data.States != null)
            {
                foreach (var kvp in data.States)
                {
                    QuestStates[kvp.Key] = kvp.Value;
                }
            }

            QuestProgress.Clear();
            if (data.Progress != null)
            {
                foreach(var kvp in data.Progress)
                {
                    QuestProgress[kvp.Key] = new List<int>(kvp.Value);
                }
            }

            Flags.Clear();
            if (data.Flags != null)
            {
                foreach(var f in data.Flags) Flags.Add(f);
            }

            IsDirty = false;
        }
    }
}
