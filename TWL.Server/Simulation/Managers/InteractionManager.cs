using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Simulation.Managers;

public class InteractionManager
{
    private readonly Dictionary<string, List<InteractionDefinition>> _interactions = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: Interaction file not found at {path}");
            return;
        }

        var json = File.ReadAllText(path);
        var list = JsonSerializer.Deserialize<List<InteractionDefinition>>(json, _jsonOptions);
        if (list != null)
        {
            foreach (var def in list)
            {
                if (!_interactions.ContainsKey(def.TargetName))
                {
                    _interactions[def.TargetName] = new List<InteractionDefinition>();
                }
                _interactions[def.TargetName].Add(def);
            }
        }
        Console.WriteLine($"Loaded interactions for {_interactions.Count} targets from {path}");
    }

    public bool ProcessInteraction(ServerCharacter character, PlayerQuestComponent questComponent, string targetName)
    {
        if (character == null) return false;

        if (!_interactions.TryGetValue(targetName, out var definitions))
        {
            return false;
        }

        foreach (var def in definitions)
        {
            // Check Quest Requirement
            if (def.RequiredQuestId.HasValue)
            {
                if (questComponent == null ||
                    !questComponent.QuestStates.TryGetValue(def.RequiredQuestId.Value, out var state) ||
                    state != QuestState.InProgress)
                {
                    continue; // Condition not met, try next definition
                }
            }

            // Check Type logic
            if (string.Equals(def.Type, "Craft", StringComparison.OrdinalIgnoreCase))
            {
                // Check Required Items
                bool hasAll = true;
                foreach (var req in def.RequiredItems)
                {
                    if (!character.HasItem(req.ItemId, req.Quantity))
                    {
                        hasAll = false;
                        break;
                    }
                }

                if (!hasAll) continue; // Try next definition

                // Consume Items
                foreach (var req in def.RequiredItems)
                {
                    character.RemoveItem(req.ItemId, req.Quantity);
                }
            }

            // Give Rewards
            foreach (var reward in def.RewardItems)
            {
                character.AddItem(reward.ItemId, reward.Quantity);
            }

            Console.WriteLine($"Player {character.Id} interacted with {targetName} ({def.Type}).");
            return true; // Successfully processed an interaction rule
        }

        return false;
    }
}
