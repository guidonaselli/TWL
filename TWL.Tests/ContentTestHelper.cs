using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.World;

namespace TWL.Tests;

public static class ContentTestHelper
{
    private static JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static string GetContentRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6; i++)
        {
            if (current == null) break;
            var candidate = Path.Combine(current.FullName, "Content/Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find Content/Data directory starting from {baseDir}");
    }

    public static List<Skill> LoadSkills()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "skills.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find skills.json at {path}");
        }

        var json = File.ReadAllText(path);
        var skills = JsonSerializer.Deserialize<List<Skill>>(json, GetJsonOptions()) ?? new List<Skill>();

        // Post-Load: Enforce Stage Upgrade Consistency (Mirroring SkillRegistry logic)
        SkillRegistry.ApplyStageUpgradeConsistency(skills);

        return skills;
    }

    public static List<QuestDefinition> LoadQuests()
    {
        var root = GetContentRoot();
        var quests = new List<QuestDefinition>();

        var files = Directory.GetFiles(root, "quests*.json");
        if (files.Length == 0)
        {
            throw new FileNotFoundException($"Could not find any quest files in {root}");
        }

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            quests.AddRange(JsonSerializer.Deserialize<List<QuestDefinition>>(json, GetJsonOptions()) ?? new List<QuestDefinition>());
        }

        return quests;
    }

    public static List<PetDefinition> LoadPets()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "pets.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find pets.json at {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<PetDefinition>>(json, GetJsonOptions()) ?? new List<PetDefinition>();
    }

    public static List<MonsterDefinition> LoadMonsters()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "monsters.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find monsters.json at {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<MonsterDefinition>>(json, GetJsonOptions()) ?? new List<MonsterDefinition>();
    }

    public static List<ZoneSpawnConfig> LoadSpawnConfigs()
    {
        var root = GetContentRoot();
        var spawnDir = Path.Combine(root, "spawns");
        if (!Directory.Exists(spawnDir))
        {
            return new List<ZoneSpawnConfig>();
        }

        var files = Directory.GetFiles(spawnDir, "*.spawns.json", SearchOption.AllDirectories);
        var list = new List<ZoneSpawnConfig>();

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var config = JsonSerializer.Deserialize<ZoneSpawnConfig>(json, GetJsonOptions());
            if (config != null)
            {
                list.Add(config);
            }
        }

        return list;
    }

    public static List<AmityItemDefinition> LoadAmityItems()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "amity_items.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find amity_items.json at {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<AmityItemDefinition>>(json, GetJsonOptions()) ?? new List<AmityItemDefinition>();
    }
}
