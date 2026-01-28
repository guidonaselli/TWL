using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation.Networking.Components;

public class PlayerQuestComponent
{
    private readonly ServerQuestManager _questManager;
    private readonly PetManager _petManager;
    private readonly object _lock = new();

    public bool IsDirty { get; set; }

    // QuestId -> State
    public Dictionary<int, QuestState> QuestStates { get; private set; } = new();

    // QuestId -> List of counts per objective
    public Dictionary<int, List<int>> QuestProgress { get; private set; } = new();

    // Player Flags
    public HashSet<string> Flags { get; private set; } = new();

    public Dictionary<int, DateTime> QuestCompletionTimes { get; private set; } = new();

    private ServerCharacter? _character;
    public ServerCharacter? Character
    {
        get => _character;
        set
        {
            if (_character != null)
            {
                _character.OnItemAdded -= HandleItemAdded;
            }
            _character = value;
            if (_character != null)
            {
                _character.OnItemAdded += HandleItemAdded;
            }
        }
    }

    public PlayerQuestComponent(ServerQuestManager questManager, PetManager? petManager = null)
    {
        _questManager = questManager;
        _petManager = petManager;
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
            CheckFailures();

            var def = _questManager.GetDefinition(questId);
            if (def == null) return false;

            // Blocked By Flags
            foreach (var blockedFlag in def.BlockedByFlags)
            {
                if (Flags.Contains(blockedFlag)) return false;
            }

            // Repeatability Checks
            if (QuestStates.ContainsKey(questId) && QuestStates[questId] != QuestState.NotStarted)
            {
                if (QuestStates[questId] == QuestState.Failed)
                {
                     // Failed quests can be retried immediately (or we could add cooldown here)
                }
                else if (QuestStates[questId] != QuestState.RewardClaimed)
                {
                     // Still in progress or just completed but not claimed
                     return false;
                }
                else
                {
                    // Reward Claimed - Check Repeatability
                    if (def.Repeatability == QuestRepeatability.None) return false;

                    if (QuestCompletionTimes.TryGetValue(questId, out var completionTime))
                    {
                         if (def.Repeatability == QuestRepeatability.Daily)
                         {
                             if (completionTime.Date == DateTime.UtcNow.Date) return false;
                         }
                         else if (def.Repeatability == QuestRepeatability.Weekly)
                         {
                             var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
                             var week1 = cal.GetWeekOfYear(completionTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                             var week2 = cal.GetWeekOfYear(DateTime.UtcNow, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                             if (week1 == week2 && completionTime.Year == DateTime.UtcNow.Year) return false;
                         }
                         else if (def.Repeatability == QuestRepeatability.Cooldown)
                         {
                             if (def.RepeatCooldown.HasValue)
                             {
                                 if (DateTime.UtcNow < completionTime + def.RepeatCooldown.Value) return false;
                             }
                         }
                    }
                }
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

            // Exclusivity: Mutual Exclusion Group
            if (!string.IsNullOrEmpty(def.MutualExclusionGroup))
            {
                foreach (var kvp in QuestStates)
                {
                    if (kvp.Value == QuestState.InProgress)
                    {
                        var otherDef = _questManager.GetDefinition(kvp.Key);
                        if (otherDef != null && string.Equals(otherDef.MutualExclusionGroup, def.MutualExclusionGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }

            // Exclusivity: Mutual Exclusion Group
            if (!string.IsNullOrEmpty(def.MutualExclusionGroup))
            {
                foreach (var kvp in QuestStates)
                {
                    if (kvp.Value == QuestState.InProgress)
                    {
                        var otherDef = _questManager.GetDefinition(kvp.Key);
                        if (otherDef != null && string.Equals(otherDef.MutualExclusionGroup, def.MutualExclusionGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
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
            CheckFailures();

            var def = _questManager.GetDefinition(questId);
            if (def == null) return false;

            // Blocked By Flags
            foreach (var blockedFlag in def.BlockedByFlags)
            {
                if (Flags.Contains(blockedFlag)) return false;
            }

            // Anti-Abuse: UniquePerCharacter
            if (!string.IsNullOrEmpty(def.AntiAbuseRules) && def.AntiAbuseRules.Contains("UniquePerCharacter"))
            {
                if (QuestStates.ContainsKey(questId)) return false;
            }

            // Exclusivity: Mutual Exclusion Group
            if (!string.IsNullOrEmpty(def.MutualExclusionGroup))
            {
                foreach (var kvp in QuestStates)
                {
                    if (kvp.Value == QuestState.InProgress)
                    {
                        var otherDef = _questManager.GetDefinition(kvp.Key);
                        if (otherDef != null && string.Equals(otherDef.MutualExclusionGroup, def.MutualExclusionGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
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
                if (QuestStates[questId] == QuestState.Failed)
                {
                     // Failed quests can be retried
                }
                else if (QuestStates[questId] != QuestState.RewardClaimed)
                {
                    return false;
                }
                else
                {
                    if (def.Repeatability == QuestRepeatability.None) return false;

                    if (QuestCompletionTimes.TryGetValue(questId, out var completionTime))
                    {
                         if (def.Repeatability == QuestRepeatability.Daily)
                         {
                             if (completionTime.Date == DateTime.UtcNow.Date) return false;
                         }
                         else if (def.Repeatability == QuestRepeatability.Weekly)
                         {
                             var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
                             var week1 = cal.GetWeekOfYear(completionTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                             var week2 = cal.GetWeekOfYear(DateTime.UtcNow, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                             if (week1 == week2 && completionTime.Year == DateTime.UtcNow.Year) return false;
                         }
                         else if (def.Repeatability == QuestRepeatability.Cooldown)
                         {
                             if (def.RepeatCooldown.HasValue)
                             {
                                 if (DateTime.UtcNow < completionTime + def.RepeatCooldown.Value) return false;
                             }
                         }
                    }
                }
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
            CheckFailures();
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
            CheckFailures();

            if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.Completed)
                return false;

            var def = _questManager.GetDefinition(questId);
            if (def != null)
            {
                foreach (var f in def.FlagsSet) Flags.Add(f);
                foreach (var f in def.FlagsClear) Flags.Remove(f);

                if (Character != null)
                {
                    // Grant Rewards
                    if (def.Rewards.Exp > 0) Character.AddExp(def.Rewards.Exp);
                    if (def.Rewards.Gold > 0) Character.AddGold(def.Rewards.Gold);

                    if (def.Rewards.Items != null)
                    {
                        foreach (var item in def.Rewards.Items)
                        {
                            Character.AddItem(item.ItemId, item.Quantity);
                        }
                    }

                    if (def.Rewards.PetUnlockId.HasValue && _petManager != null)
                    {
                        var petDef = _petManager.GetDefinition(def.Rewards.PetUnlockId.Value);
                        if (petDef != null)
                        {
                            var newPet = new ServerPet(petDef);
                            Character.AddPet(newPet);
                        }
                    }

                    if (def.Rewards.GrantSkillId.HasValue)
                    {
                        Character.LearnSkill(def.Rewards.GrantSkillId.Value);
                    }
                }
            }

            QuestStates[questId] = QuestState.RewardClaimed;
            QuestCompletionTimes[questId] = DateTime.UtcNow;
            IsDirty = true;
            return true;
        }
    }

    public bool FailQuest(int questId)
    {
        lock (_lock)
        {
            if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.InProgress)
                return false;

            QuestStates[questId] = QuestState.Failed;
            IsDirty = true;
            return true;
        }
    }

    private void CheckFailures()
    {
        var now = DateTime.UtcNow;
        var failedIds = new List<int>();

        foreach (var kvp in QuestStates)
        {
            if (kvp.Value != QuestState.InProgress) continue;

            var def = _questManager.GetDefinition(kvp.Key);
            if (def == null) continue;

            if (def.Expiry.HasValue && now > def.Expiry.Value)
            {
                failedIds.Add(kvp.Key);
            }
        }

        foreach (var id in failedIds)
        {
            QuestStates[id] = QuestState.Failed;
            IsDirty = true;
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
            CheckFailures();

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

    private void HandleItemAdded(Item item, int quantity)
    {
        lock (_lock)
        {
            CheckFailures();

            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress) continue;

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null) continue;

                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (!string.Equals(obj.Type, "Collect", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(obj.Type, "CollectItem", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool match = false;
                    if (obj.DataId.HasValue)
                    {
                        if (obj.DataId.Value == item.ItemId) match = true;
                    }
                    else
                    {
                        // Fallback to Name match if DataId not specified
                        if (string.Equals(obj.TargetName, item.Name, StringComparison.OrdinalIgnoreCase)) match = true;
                    }

                    if (match)
                    {
                        // Check if we need more
                        if (QuestProgress[questId][i] < obj.RequiredCount)
                        {
                            UpdateProgressInternal(questId, i, quantity);
                        }
                    }
                }
            }
        }
    }

    public List<int> TryDeliver(string targetName)
    {
        var updatedQuests = new List<int>();
        lock (_lock)
        {
            CheckFailures();

            if (Character == null) return updatedQuests;

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
                    if (!string.Equals(obj.Type, "Deliver", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!obj.DataId.HasValue) continue;

                    var required = obj.RequiredCount;
                    var current = QuestProgress[questId][i];
                    if (current >= required) continue;

                    var needed = required - current;
                    var itemId = obj.DataId.Value;

                    // Check if player has the item
                    var invItems = Character.GetItems(itemId);
                    var totalInv = invItems.Sum(x => x.Quantity);

                    if (totalInv > 0)
                    {
                        var toRemove = Math.Min(needed, (int)totalInv);
                        if (Character.RemoveItem(itemId, toRemove))
                        {
                            UpdateProgressInternal(questId, i, toRemove);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    updatedQuests.Add(questId);
                }
            }
        }
        return updatedQuests;
    }

    public QuestData GetSaveData()
    {
        lock (_lock)
        {
            var data = new QuestData
            {
                States = new Dictionary<int, QuestState>(QuestStates),
                Progress = new Dictionary<int, List<int>>(),
                Flags = new HashSet<string>(Flags),
                CompletionTimes = new Dictionary<int, DateTime>(QuestCompletionTimes)
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

            QuestCompletionTimes.Clear();
            if (data.CompletionTimes != null)
            {
                foreach (var kvp in data.CompletionTimes)
                {
                    QuestCompletionTimes[kvp.Key] = kvp.Value;
                }
            }

            IsDirty = false;
        }
    }

    public List<int> HandleInstanceFailure(string instanceId)
    {
        var failedQuests = new List<int>();
        lock (_lock)
        {
            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress) continue;

                var def = _questManager.GetDefinition(kvp.Key);
                if (def == null) continue;

                // Check if quest is bound to this instance
                if (def.InstanceRules != null &&
                    string.Equals(def.InstanceRules.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    // Fail the quest
                    // FailQuest returns true if state changed
                    QuestStates[kvp.Key] = QuestState.Failed;
                    IsDirty = true;
                    failedQuests.Add(kvp.Key);
                }
            }
        }
        return failedQuests;
    }
}
