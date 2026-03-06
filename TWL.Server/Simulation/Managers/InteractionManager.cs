using System.Text.Json;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Domain.Requests;

using TWL.Server.Services.World;

namespace TWL.Server.Simulation.Managers;

public class InteractionManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Dictionary<string, List<InteractionDefinition>> _interactions = new();
    private readonly IMapRegistry? _mapRegistry;

    public InteractionManager() { } // For tests

    public InteractionManager(IMapRegistry mapRegistry)
    {
        _mapRegistry = mapRegistry;
    }

    public virtual System.Numerics.Vector2? GetTargetPosition(int mapId, string targetName)
    {
        if (_mapRegistry == null) return null;

        var map = _mapRegistry.GetMap(mapId);
        if (map == null) return null;

        var trigger = map.Triggers.FirstOrDefault(t =>
            t.Id == targetName ||
            (t.Properties.TryGetValue("TargetName", out var tn) && tn == targetName));

        if (trigger != null)
        {
            // The coordinates in the map file (Tiled) are in pixels.
            // Using the center of the trigger for distance calculations.
            var centerX = trigger.X + (trigger.Width / 2f);
            var centerY = trigger.Y + (trigger.Height / 2f);
            return new System.Numerics.Vector2(centerX, centerY);
        }

        return null;
    }

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

    public virtual string? ProcessInteraction(ServerCharacter character, PlayerQuestComponent questComponent, string targetName)
    {
        if (character == null)
        {
            return null;
        }

        if (!_interactions.TryGetValue(targetName, out var definitions))
        {
            return null;
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

            // Check Required Items
            var hasAll = true;
            if (def.RequiredItems != null)
            {
                foreach (var req in def.RequiredItems)
                {
                    if (!character.HasItem(req.ItemId, req.Quantity))
                    {
                        hasAll = false;
                        break;
                    }
                }
            }

            if (!hasAll)
            {
                continue; // Try next definition
            }

            // Consume Items
            if (def.ConsumeRequiredItems && def.RequiredItems != null)
            {
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
            return def.Type; // Successfully processed an interaction rule
        }

        return null;
    }
}