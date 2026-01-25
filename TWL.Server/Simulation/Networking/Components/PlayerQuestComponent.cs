using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Server.Simulation.Networking;

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

    public ServerCharacter? Character { get; set; }

    public PlayerQuestComponent(ServerQuestManager questManager)
    {
        _questManager = questManager;
    }

    private bool CheckGating(QuestDefinition def)
    {
        if (Character == null)
        {
            // If there are gating requirements but no character attached, we must fail.
            // If there are NO requirements, we allow it (backward compatibility for tests).
            if (def.RequiredLevel > 1 ||
               (def.RequiredStats != null && def.RequiredStats.Count > 0) ||
               (def.RequiredItems != null && def.RequiredItems.Count > 0))
            {
                return false;
            }
            return true;
        }

        // Level Check
        if (Character.Level < def.RequiredLevel) return false;

        // Stat Checks
        if (def.RequiredStats != null)
        {
            foreach (var stat in def.RequiredStats)
            {
                int charStat = 0;
                switch (stat.Key.ToLower())
                {
                    case "str": charStat = Character.Str; break;
                    case "con": charStat = Character.Con; break;
                    case "int": charStat = Character.Int; break;
                    case "wis": charStat = Character.Wis; break;
                    case "agi": charStat = Character.Agi; break;
                    default: break;
                }
                if (charStat < stat.Value) return false;
            }
        }

        // Item Checks
        if (def.RequiredItems != null)
        {
            foreach (var itemReq in def.RequiredItems)
            {
                if (!Character.HasItem(itemReq.ItemId, itemReq.Quantity)) return false;
            }
        }

        return true;
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

            if (!string.IsNullOrEmpty(def.AntiAbuseRules))
            {
                if (def.AntiAbuseRules.Contains("UniquePerCharacter"))
                {
                    if (QuestStates.ContainsKey(questId)) return false;
                }
            }

            // Exclusivity: Special Category
            if (!string.IsNullOrEmpty(def.SpecialCategory))
            {
                foreach (var kvp in QuestStates)
                {
                    if (kvp.Value == QuestState.InProgress)
                    {
                        var otherDef = _questManager.GetDefinition(kvp.Key);
                        // Enforce exclusivity only within the same category (e.g. can't do 2 Dragon quests, but can do Dragon + Fairy)
                        if (otherDef != null && string.Equals(otherDef.SpecialCategory, def.SpecialCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }

            if (!CheckGating(def)) return false;

            return true;
        }
    }

    public bool StartQuest(int questId)
    {
        lock (_lock)
        {
            var def = _questManager.GetDefinition(questId);
            if (def == null) return false;

            // Anti-Abuse: UniquePerCharacter
            if (!string.IsNullOrEmpty(def.AntiAbuseRules) && def.AntiAbuseRules.Contains("UniquePerCharacter"))
            {
                if (QuestStates.ContainsKey(questId)) return false;
            }

            // Exclusivity: Special Category
            if (!string.IsNullOrEmpty(def.SpecialCategory))
            {
                foreach (var kvp in QuestStates)
                {
                    if (kvp.Value == QuestState.InProgress)
                    {
                        var otherDef = _questManager.GetDefinition(kvp.Key);
                        // Enforce exclusivity only within the same category
                        if (otherDef != null && string.Equals(otherDef.SpecialCategory, def.SpecialCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }

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

            if (!CheckGating(def)) return false;

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
    public List<int> TryProgress(string type, string targetName, int amount = 1)
    {
        var updatedQuests = new List<int>();
        TryProgress(updatedQuests, targetName, amount, type);
        return updatedQuests;
    }

    /// <summary>
    /// Optimized overload to check multiple types at once and use an existing collection.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, params string[] types)
    {
        TryProgress(output, targetName, 1, types);
    }

    /// <summary>
    /// Optimized overload to check multiple types at once and use an existing collection with amount.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, int amount, params string[] types)
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
                            UpdateProgressInternal(questId, i, amount);
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
