using System;
using System.Collections.Generic;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Simulation.Networking.Components;

public class PlayerQuestComponent
{
    private readonly ServerQuestManager _questManager;

    // QuestId -> State
    public Dictionary<int, QuestState> QuestStates { get; private set; } = new();

    // QuestId -> List of counts per objective
    public Dictionary<int, List<int>> QuestProgress { get; private set; } = new();

    public PlayerQuestComponent(ServerQuestManager questManager)
    {
        _questManager = questManager;
    }

    public bool CanStartQuest(int questId)
    {
        var def = _questManager.GetDefinition(questId);
        if (def == null) return false;

        if (QuestStates.ContainsKey(questId) && QuestStates[questId] != QuestState.NotStarted)
            return false;

        foreach (var reqId in def.Requirements)
        {
            if (!QuestStates.ContainsKey(reqId)) return false;
            var state = QuestStates[reqId];
            if (state != QuestState.Completed && state != QuestState.RewardClaimed)
                return false;
        }

        return true;
    }

    public bool StartQuest(int questId)
    {
        if (!CanStartQuest(questId)) return false;

        var def = _questManager.GetDefinition(questId);
        if (def == null) return false;

        QuestStates[questId] = QuestState.InProgress;
        QuestProgress[questId] = new List<int>(new int[def.Objectives.Count]); // Init with zeros

        return true;
    }

    public void UpdateProgress(int questId, int objectiveIndex, int amount)
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
        if (!QuestStates.ContainsKey(questId) || QuestStates[questId] != QuestState.Completed)
            return false;

        QuestStates[questId] = QuestState.RewardClaimed;
        return true;
    }

    /// <summary>
    /// Attempts to progress any active quest that matches the given type and target.
    /// </summary>
    /// <returns>List of QuestIds that were updated.</returns>
    public List<int> TryProgress(string type, string targetName)
    {
        var updatedQuests = new List<int>();

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
                // Match Type and TargetName
                if (string.Equals(obj.Type, type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(obj.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if not already complete
                    if (QuestProgress[questId][i] < obj.RequiredCount)
                    {
                        UpdateProgress(questId, i, 1);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                updatedQuests.Add(questId);
            }
        }
        return updatedQuests;
    }
}
