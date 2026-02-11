using System.Globalization;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Simulation.Networking.Components;

public class PlayerQuestComponent
{
    private readonly object _lock = new();
    private readonly PetManager _petManager;
    private readonly ServerQuestManager _questManager;
    private DateTime _lastFailureCheckTime = DateTime.MinValue;
    private static readonly TimeSpan FailureCheckThrottle = TimeSpan.FromSeconds(1);

    private ServerCharacter? _character;

    public PlayerQuestComponent(ServerQuestManager questManager, PetManager? petManager = null)
    {
        _questManager = questManager;
        _petManager = petManager;
    }

    public bool IsDirty { get; set; }

    // QuestId -> State
    public Dictionary<int, QuestState> QuestStates { get; } = new();

    // QuestId -> List of counts per objective
    public Dictionary<int, List<int>> QuestProgress { get; } = new();

    // Player Flags
    public HashSet<string> Flags { get; } = new();

    public event Action<string>? OnFlagAdded;
    public event Action<string>? OnFlagRemoved;

    public void AddFlag(string flag)
    {
        lock (_lock)
        {
            if (Flags.Add(flag))
            {
                OnFlagAdded?.Invoke(flag);
                IsDirty = true;
            }
        }
    }

    public void RemoveFlag(string flag)
    {
        lock (_lock)
        {
            if (Flags.Remove(flag))
            {
                OnFlagRemoved?.Invoke(flag);
                IsDirty = true;
            }
        }
    }

    public Dictionary<int, DateTime> QuestCompletionTimes { get; } = new();
    public Dictionary<int, DateTime> QuestStartTimes { get; } = new();

    public ServerCharacter? Character
    {
        get => _character;
        set
        {
            if (_character != null)
            {
                _character.OnItemAdded -= HandleItemAdded;
                _character.OnPetAdded -= HandlePetAdded;
                _character.OnTradeCommitted -= HandleTradeCommitted;
                _character.OnMapChanged -= HandleMapChanged;
            }

            _character = value;
            if (_character != null)
            {
                _character.OnItemAdded += HandleItemAdded;
                _character.OnPetAdded += HandlePetAdded;
                _character.OnTradeCommitted += HandleTradeCommitted;
                _character.OnMapChanged += HandleMapChanged;
            }
        }
    }

    private void HandleMapChanged(int mapId)
    {
        var failedQuests = new List<int>();
        lock (_lock)
        {
            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var def = _questManager.GetDefinition(kvp.Key);
                if (def == null) continue;

                if (def.FailConditions != null)
                {
                    foreach (var cond in def.FailConditions)
                    {
                        if (string.Equals(cond.Type, "LeaveMap", StringComparison.OrdinalIgnoreCase))
                        {
                            if (cond.Value != mapId.ToString())
                            {
                                failedQuests.Add(kvp.Key);
                            }
                        }
                    }
                }
            }
        }

        foreach (var qid in failedQuests)
        {
            FailQuest(qid);
        }

        TryProgress("Explore", mapId.ToString());
        TryProgress("Reach", mapId.ToString());
    }

    private void HandlePetAdded(ServerPet pet)
    {
        lock (_lock)
        {
            CheckFailures();

            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null)
                {
                    continue;
                }

                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (!string.Equals(obj.Type, "PetAcquired", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var match = false;
                    if (obj.DataId.HasValue)
                    {
                        if (obj.DataId.Value == pet.DefinitionId)
                        {
                            match = true;
                        }
                    }
                    else
                    {
                        if (string.Equals(obj.TargetName, pet.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                        }
                    }

                    if (match)
                    {
                        if (QuestProgress[questId][i] < obj.RequiredCount)
                        {
                            UpdateProgressInternal(questId, i, 1);
                        }
                    }
                }
            }
        }
    }

    private void HandleTradeCommitted(ServerCharacter target, int itemId, int quantity)
    {
        lock (_lock)
        {
            CheckFailures();

            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null)
                {
                    continue;
                }

                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (!string.Equals(obj.Type, "Trade", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Match Target (Person we traded WITH)
                    if (!string.Equals(obj.TargetName, target.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Match Item
                    if (obj.DataId.HasValue && obj.DataId.Value != itemId)
                    {
                        continue;
                    }

                    if (QuestProgress[questId][i] < obj.RequiredCount)
                    {
                        UpdateProgressInternal(questId, i, quantity);
                    }
                }
            }
        }
    }

    private bool IsBlockedByMutualExclusion(QuestDefinition def)
    {
        if (string.IsNullOrEmpty(def.MutualExclusionGroup))
        {
            return false;
        }

        foreach (var kvp in QuestStates)
        {
            var otherDef = _questManager.GetDefinition(kvp.Key);
            if (otherDef == null || !string.Equals(otherDef.MutualExclusionGroup, def.MutualExclusionGroup,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kvp.Value == QuestState.InProgress)
            {
                return true;
            }

            if (kvp.Value == QuestState.Completed || kvp.Value == QuestState.RewardClaimed)
            {
                // Strict Mutual Exclusion for Non-Repeatable Quests (Permanent Branching)
                if (def.Repeatability == QuestRepeatability.None && otherDef.Repeatability == QuestRepeatability.None)
                {
                    return true;
                }

                if (QuestCompletionTimes.TryGetValue(kvp.Key, out var completionTime))
                {
                    if (otherDef.Repeatability == QuestRepeatability.Daily)
                    {
                        if (completionTime.Date == DateTime.UtcNow.Date)
                        {
                            return true;
                        }
                    }
                    else if (otherDef.Repeatability == QuestRepeatability.Weekly)
                    {
                        var cal = CultureInfo.InvariantCulture.Calendar;
                        var week1 = cal.GetWeekOfYear(completionTime, CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                        var week2 = cal.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                        if (week1 == week2 && completionTime.Year == DateTime.UtcNow.Year)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
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
        if (Character.Level < def.RequiredLevel)
        {
            return false;
        }

        // Rebirth Level Check
        if (Character.RebirthLevel < def.RequiredRebirthLevel)
        {
            return false;
        }

        // Stat Checks
        if (def.RequiredStats != null)
        {
            foreach (var stat in def.RequiredStats)
            {
                var charStat = 0;
                switch (stat.Key.ToLower())
                {
                    case "str": charStat = Character.Str; break;
                    case "con": charStat = Character.Con; break;
                    case "int": charStat = Character.Int; break;
                    case "wis": charStat = Character.Wis; break;
                    case "agi": charStat = Character.Agi; break;
                }

                if (charStat < stat.Value)
                {
                    return false;
                }
            }
        }

        // Item Checks
        if (def.RequiredItems != null)
        {
            foreach (var itemReq in def.RequiredItems)
            {
                if (!Character.HasItem(itemReq.ItemId, itemReq.Quantity))
                {
                    return false;
                }
            }
        }

        // Equipment Checks
        if (def.RequiredEquipment != null)
        {
            foreach (var itemId in def.RequiredEquipment)
            {
                if (!Character.HasEquippedItem(itemId))
                {
                    return false;
                }
            }
        }

        // Pet Check
        if (def.RequiredPetId.HasValue)
        {
            if (Character.Pets == null || Character.Pets.All(p => p.DefinitionId != def.RequiredPetId.Value))
            {
                return false;
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
            if (def == null)
            {
                return false;
            }

            // Blocked By Flags
            foreach (var blockedFlag in def.BlockedByFlags)
            {
                if (Flags.Contains(blockedFlag))
                {
                    return false;
                }
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
                    if (def.Repeatability == QuestRepeatability.None)
                    {
                        return false;
                    }

                    if (QuestCompletionTimes.TryGetValue(questId, out var completionTime))
                    {
                        if (def.Repeatability == QuestRepeatability.Daily)
                        {
                            if (completionTime.Date == DateTime.UtcNow.Date)
                            {
                                return false;
                            }
                        }
                        else if (def.Repeatability == QuestRepeatability.Weekly)
                        {
                            var cal = CultureInfo.InvariantCulture.Calendar;
                            var week1 = cal.GetWeekOfYear(completionTime, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);
                            var week2 = cal.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);
                            if (week1 == week2 && completionTime.Year == DateTime.UtcNow.Year)
                            {
                                return false;
                            }
                        }
                        else if (def.Repeatability == QuestRepeatability.Cooldown)
                        {
                            if (def.RepeatCooldown.HasValue)
                            {
                                if (DateTime.UtcNow < completionTime + def.RepeatCooldown.Value)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var reqId in def.Requirements)
            {
                if (!QuestStates.ContainsKey(reqId))
                {
                    return false;
                }

                var state = QuestStates[reqId];
                if (state != QuestState.Completed && state != QuestState.RewardClaimed)
                {
                    return false;
                }
            }

            foreach (var flag in def.RequiredFlags)
            {
                if (!Flags.Contains(flag))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(def.AntiAbuseRules))
            {
                if (def.AntiAbuseRules.Contains("UniquePerCharacter"))
                {
                    if (QuestStates.ContainsKey(questId))
                    {
                        return false;
                    }

                    if (QuestCompletionTimes.ContainsKey(questId))
                    {
                        return false;
                    }
                }
            }

            // Exclusivity: Mutual Exclusion Group
            if (IsBlockedByMutualExclusion(def))
            {
                return false;
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
                        if (otherDef != null && string.Equals(otherDef.SpecialCategory, def.SpecialCategory,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }

            // Party Rules
            if (!string.IsNullOrEmpty(def.PartyRules))
            {
                if (def.PartyRules.Contains("MustBeInParty"))
                {
                    if (Character == null || !Character.PartyId.HasValue)
                    {
                        return false;
                    }
                }
            }

            // Guild Rules
            if (!string.IsNullOrEmpty(def.GuildRules))
            {
                if (def.GuildRules.Contains("MustBeInGuild"))
                {
                    if (Character == null || !Character.GuildId.HasValue)
                    {
                        return false;
                    }
                }
            }

            if (!CheckGating(def))
            {
                return false;
            }

            return true;
        }
    }

    public bool StartQuest(int questId)
    {
        lock (_lock)
        {
            CheckFailures();

            var def = _questManager.GetDefinition(questId);
            if (def == null)
            {
                return false;
            }

            // Blocked By Flags
            foreach (var blockedFlag in def.BlockedByFlags)
            {
                if (Flags.Contains(blockedFlag))
                {
                    return false;
                }
            }

            // Anti-Abuse: UniquePerCharacter
            if (!string.IsNullOrEmpty(def.AntiAbuseRules) && def.AntiAbuseRules.Contains("UniquePerCharacter"))
            {
                if (QuestStates.ContainsKey(questId))
                {
                    return false;
                }

                if (QuestCompletionTimes.ContainsKey(questId))
                {
                    return false;
                }
            }

            // Exclusivity: Mutual Exclusion Group
            if (IsBlockedByMutualExclusion(def))
            {
                return false;
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
                        if (otherDef != null && string.Equals(otherDef.SpecialCategory, def.SpecialCategory,
                                StringComparison.OrdinalIgnoreCase))
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
                    if (def.Repeatability == QuestRepeatability.None)
                    {
                        return false;
                    }

                    if (QuestCompletionTimes.TryGetValue(questId, out var completionTime))
                    {
                        if (def.Repeatability == QuestRepeatability.Daily)
                        {
                            if (completionTime.Date == DateTime.UtcNow.Date)
                            {
                                return false;
                            }
                        }
                        else if (def.Repeatability == QuestRepeatability.Weekly)
                        {
                            var cal = CultureInfo.InvariantCulture.Calendar;
                            var week1 = cal.GetWeekOfYear(completionTime, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);
                            var week2 = cal.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);
                            if (week1 == week2 && completionTime.Year == DateTime.UtcNow.Year)
                            {
                                return false;
                            }
                        }
                        else if (def.Repeatability == QuestRepeatability.Cooldown)
                        {
                            if (def.RepeatCooldown.HasValue)
                            {
                                if (DateTime.UtcNow < completionTime + def.RepeatCooldown.Value)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // Check Requirements (Quest Chains)
            foreach (var reqId in def.Requirements)
            {
                if (!QuestStates.ContainsKey(reqId))
                {
                    return false;
                }

                var state = QuestStates[reqId];
                if (state != QuestState.Completed && state != QuestState.RewardClaimed)
                {
                    return false;
                }
            }

            foreach (var flag in def.RequiredFlags)
            {
                if (!Flags.Contains(flag))
                {
                    return false;
                }
            }

            if (!CheckGating(def))
            {
                return false;
            }

            QuestStates[questId] = QuestState.InProgress;
            QuestProgress[questId] = new List<int>(new int[def.Objectives.Count]); // Init with zeros
            QuestStartTimes[questId] = DateTime.UtcNow;

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
        if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.InProgress)
        {
            return;
        }

        var def = _questManager.GetDefinition(questId);
        if (def == null)
        {
            return;
        }

        if (objectiveIndex < 0 || objectiveIndex >= def.Objectives.Count)
        {
            return;
        }

        var currentList = QuestProgress[questId];
        currentList[objectiveIndex] += amount;

        if (currentList[objectiveIndex] > def.Objectives[objectiveIndex].RequiredCount)
        {
            currentList[objectiveIndex] = def.Objectives[objectiveIndex].RequiredCount;
        }

        CheckCompletion(questId);
        IsDirty = true;
    }

    private void CheckCompletion(int questId)
    {
        var def = _questManager.GetDefinition(questId);
        if (def == null)
        {
            return;
        }

        var counts = QuestProgress[questId];
        var allComplete = true;
        for (var i = 0; i < def.Objectives.Count; i++)
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
            {
                return false;
            }

            var def = _questManager.GetDefinition(questId);
            if (def != null)
            {
                foreach (var f in def.FlagsSet)
                {
                    AddFlag(f);
                }

                foreach (var f in def.FlagsClear)
                {
                    RemoveFlag(f);
                }

                if (Character != null)
                {
                    // Grant Rewards
                    if (def.Rewards.Exp > 0)
                    {
                        Character.AddExp(def.Rewards.Exp);
                    }

                    if (def.Rewards.Gold > 0)
                    {
                        Character.AddGold(def.Rewards.Gold);
                    }

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
            {
                return false;
            }

            QuestStates[questId] = QuestState.Failed;
            IsDirty = true;
            return true;
        }
    }

    private void CheckFailures()
    {
        var now = DateTime.UtcNow;
        if (now - _lastFailureCheckTime < FailureCheckThrottle)
        {
            return;
        }

        _lastFailureCheckTime = now;
        var failedIds = new List<int>();

        foreach (var kvp in QuestStates)
        {
            if (kvp.Value != QuestState.InProgress)
            {
                continue;
            }

            var def = _questManager.GetDefinition(kvp.Key);
            if (def == null)
            {
                continue;
            }

            if (def.Expiry.HasValue && now > def.Expiry.Value)
            {
                failedIds.Add(kvp.Key);
            }
            else if (def.TimeLimitSeconds.HasValue && def.TimeLimitSeconds.Value > 0)
            {
                if (QuestStartTimes.TryGetValue(kvp.Key, out var startTime))
                {
                    if (now > startTime.AddSeconds(def.TimeLimitSeconds.Value))
                    {
                        failedIds.Add(kvp.Key);
                    }
                }
            }
        }

        foreach (var id in failedIds)
        {
            QuestStates[id] = QuestState.Failed;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Attempts to progress any active quest that matches the given type and target.
    /// </summary>
    /// <returns>List of QuestIds that were updated.</returns>
    public List<int> TryProgress(string type, string targetName, int amount = 1, int? dataId = null)
    {
        var updatedQuests = new List<int>();
        TryProgress(updatedQuests, targetName, amount, dataId, type);
        return updatedQuests;
    }

    /// <summary>
    ///     Optimized overload to check multiple types at once and use an existing collection.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, params string[] types) =>
        TryProgress(output, targetName, 1, types);

    /// <summary>
    ///     Optimized overload to check multiple types at once and use an existing collection with amount.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, int amount, params string[] types) =>
        TryProgress(output, targetName, amount, null, types);

    /// <summary>
    ///     Optimized overload to check multiple types at once and use an existing collection with amount and DataID.
    /// </summary>
    public void TryProgress(ICollection<int> output, string targetName, int amount, int? dataId, params string[] types)
    {
        lock (_lock)
        {
            CheckFailures();

            // Iterate directly over QuestStates.
            // CheckCompletion only modifies values (states), does not add/remove keys.
            // Dictionary enumeration is safe against value modifications in .NET Core+.
            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];

                    // Match Logic:
                    // 1. Type must match
                    var typeMatch = false;
                    for (var t = 0; t < types.Length; t++)
                    {
                        if (string.Equals(obj.Type, types[t], StringComparison.OrdinalIgnoreCase))
                        {
                            typeMatch = true;
                            break;
                        }
                    }

                    // Special Case: ShowItem triggered by "Talk" or "Interact"
                    if (!typeMatch && string.Equals(obj.Type, "ShowItem", StringComparison.OrdinalIgnoreCase))
                    {
                        for (var t = 0; t < types.Length; t++)
                        {
                            if (string.Equals(types[t], "Talk", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(types[t], "Interact", StringComparison.OrdinalIgnoreCase))
                            {
                                typeMatch = true;
                                break;
                            }
                        }

                        if (typeMatch && obj.DataId.HasValue && Character != null)
                        {
                            // Must verify item possession
                            if (!Character.HasItem(obj.DataId.Value, 1))
                            {
                                typeMatch = false;
                            }
                        }
                    }

                    if (!typeMatch) continue;

                    // 2. DataId Match (Primary) or TargetName Match (Fallback/Secondary)
                    var match = false;
                    if (dataId.HasValue && obj.DataId.HasValue)
                    {
                        if (obj.DataId.Value == dataId.Value)
                        {
                            match = true;
                        }
                    }

                    // If no DataId match (or not provided), check Name
                    if (!match)
                    {
                        // Match TargetName (string check)
                        if (string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            // If Objective REQUIRES DataId but we didn't match it above, be careful.
                            // But usually if Objective has DataId, we prefer DataId match.
                            // If the event provides DataId but objective has different DataId, it shouldn't match even if name matches.
                            if (dataId.HasValue && obj.DataId.HasValue && dataId.Value != obj.DataId.Value)
                            {
                                match = false;
                            }
                            else
                            {
                                match = true;
                            }
                        }
                    }

                    if (match)
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
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null)
                {
                    continue;
                }

                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (!string.Equals(obj.Type, "Collect", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(obj.Type, "CollectItem", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var match = false;
                    if (obj.DataId.HasValue)
                    {
                        if (obj.DataId.Value == item.ItemId)
                        {
                            match = true;
                        }
                    }
                    else
                    {
                        // Fallback to Name match if DataId not specified
                        if (string.Equals(obj.TargetName, item.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                        }
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

            if (Character == null)
            {
                return updatedQuests;
            }

            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var questId = kvp.Key;
                var def = _questManager.GetDefinition(questId);
                if (def == null)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];

                    // Handle Deliver Item
                    if (string.Equals(obj.Type, "Deliver", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!obj.DataId.HasValue)
                        {
                            continue;
                        }

                        var required = obj.RequiredCount;
                        var current = QuestProgress[questId][i];
                        if (current >= required)
                        {
                            continue;
                        }

                        var needed = required - current;
                        var itemId = obj.DataId.Value;

                        // Check if player has the item
                        var invItems = Character.GetItems(itemId);
                        var totalInv = invItems.Sum(x => x.Quantity);

                        if (totalInv > 0)
                        {
                            var toRemove = Math.Min(needed, totalInv);
                            if (Character.RemoveItem(itemId, toRemove))
                            {
                                UpdateProgressInternal(questId, i, toRemove);
                                changed = true;
                            }
                        }
                    }
                    // Handle Pay Gold
                    else if (string.Equals(obj.Type, "PayGold", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var required = obj.RequiredCount;
                        var current = QuestProgress[questId][i];
                        if (current >= required)
                        {
                            continue;
                        }

                        var needed = required - current;

                        // Check if player has enough gold
                        if (Character.Gold >= needed)
                        {
                            Character.AddGold(-needed);
                            UpdateProgressInternal(questId, i, needed);
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
                CompletionTimes = new Dictionary<int, DateTime>(QuestCompletionTimes),
                StartTimes = new Dictionary<int, DateTime>(QuestStartTimes)
            };

            foreach (var kvp in QuestProgress)
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
                foreach (var kvp in data.Progress)
                {
                    QuestProgress[kvp.Key] = new List<int>(kvp.Value);
                }
            }

            Flags.Clear();
            if (data.Flags != null)
            {
                foreach (var f in data.Flags)
                {
                    Flags.Add(f);
                }
            }

            QuestCompletionTimes.Clear();
            if (data.CompletionTimes != null)
            {
                foreach (var kvp in data.CompletionTimes)
                {
                    QuestCompletionTimes[kvp.Key] = kvp.Value;
                }
            }

            QuestStartTimes.Clear();
            if (data.StartTimes != null)
            {
                foreach (var kvp in data.StartTimes)
                {
                    QuestStartTimes[kvp.Key] = kvp.Value;
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
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var def = _questManager.GetDefinition(kvp.Key);
                if (def == null)
                {
                    continue;
                }

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

    public void HandleCombatantDeath(string victimName)
    {
        lock (_lock)
        {
            var failedQuests = new List<int>();

            foreach (var kvp in QuestStates)
            {
                if (kvp.Value != QuestState.InProgress)
                {
                    continue;
                }

                var def = _questManager.GetDefinition(kvp.Key);
                if (def == null)
                {
                    continue;
                }

                if (def.FailConditions != null)
                {
                    foreach (var cond in def.FailConditions)
                    {
                        if (string.Equals(cond.Type, "NpcDeath", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cond.Type, "TargetDeath", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.Equals(cond.Value, victimName, StringComparison.OrdinalIgnoreCase))
                            {
                                failedQuests.Add(kvp.Key);
                            }
                        }
                        else if (string.Equals(cond.Type, "PlayerDeath", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Character != null && string.Equals(Character.Name, victimName, StringComparison.OrdinalIgnoreCase))
                            {
                                failedQuests.Add(kvp.Key);
                            }
                        }
                    }
                }
            }

            foreach (var qid in failedQuests)
            {
                FailQuest(qid);
            }
        }
    }

    public List<int> HandleCraft(string itemName, int quantity = 1) => TryProgress("Craft", itemName, quantity);

    public List<int> HandleCompound(string resultName, int quantity = 1) =>
        TryProgress("Compound", resultName, quantity);

    public List<int> HandleForge(string resultName, int quantity = 1) => TryProgress("Forge", resultName, quantity);

    public List<int> HandleEventParticipation(string eventName) => TryProgress("EventParticipation", eventName);

    public List<int> HandleEscort(string npcName, bool success)
    {
        if (success)
        {
            return TryProgress("Escort", npcName);
        }

        // If escort failed (e.g. timeout), trigger failure manually or via existing FailCondition check
        // For now, let's assume HandleCombatantDeath covers the death case.
        // If failure is due to timeout/distance, we might need a separate call.
        return new List<int>();
    }

    public List<int> HandlePuzzle(string puzzleId) => TryProgress("Puzzle", puzzleId);

    public List<int> HandlePartyAction(string action) => TryProgress("Party", action);

    public List<int> HandleGuildAction(string action) => TryProgress("Guild", action);

    public List<int> HandleUseItem(int itemId, string itemName) => TryProgress("UseItem", itemName, 1, itemId);
}