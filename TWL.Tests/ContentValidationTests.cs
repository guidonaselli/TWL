using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.World;

namespace TWL.Tests;

public class ContentValidationTests
{
    private string GetContentRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        // Try to find the path by going up levels
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

    private JsonSerializerOptions GetJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private List<Skill> LoadSkills()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "skills.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find skills.json at {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Skill>>(json, GetJsonOptions()) ?? new List<Skill>();
    }

    private List<QuestDefinition> LoadQuests()
    {
        var root = GetContentRoot();
        var quests = new List<QuestDefinition>();

        // Load quests.json
        var path = Path.Combine(root, "quests.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            quests.AddRange(JsonSerializer.Deserialize<List<QuestDefinition>>(json, GetJsonOptions()) ?? new List<QuestDefinition>());
        }

        // Load quests_messenger.json
        var pathMessenger = Path.Combine(root, "quests_messenger.json");
        if (File.Exists(pathMessenger))
        {
             var json = File.ReadAllText(pathMessenger);
             quests.AddRange(JsonSerializer.Deserialize<List<QuestDefinition>>(json, GetJsonOptions()) ?? new List<QuestDefinition>());
        }

        if (quests.Count == 0)
        {
             throw new FileNotFoundException($"Could not find any quest files in {root}");
        }
        return quests;
    }

    private List<PetDefinition> LoadPets()
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

    [Fact]
    public void ValidateSkillCategories()
    {
        var skills = LoadSkills();
        var allowedCategories = new HashSet<SkillCategory>
        {
            SkillCategory.None,
            SkillCategory.RebirthJob,
            SkillCategory.ElementSpecial,
            SkillCategory.Fairy,
            SkillCategory.Dragon,
            SkillCategory.Goddess
        };

        foreach (var skill in skills)
        {
            Assert.Contains(skill.Category, allowedCategories);
        }
    }

    [Fact]
    public void ValidateGoddessSkills()
    {
        var skills = LoadSkills();
        var goddessMap = new Dictionary<int, string>
        {
            { 2001, "Diminution" },
            { 2002, "Support Seal" },
            { 2003, "Ember Surge" },
            { 2004, "Untouchable Veil" }
        };

        foreach (var kvp in goddessMap)
        {
            var skill = skills.FirstOrDefault(s => s.SkillId == kvp.Key);
            Assert.NotNull(skill); // Must exist
            Assert.Equal(kvp.Value, skill.Name); // Must have exact name
            Assert.Equal(SkillFamily.Special, skill.Family);
            Assert.Equal(SkillCategory.Goddess, skill.Category);

            // Goddess Skills: Initial grant only. No UnlockRules.
            if (skill.UnlockRules != null)
            {
                Assert.Equal(0, skill.UnlockRules.Level);
                Assert.Null(skill.UnlockRules.QuestId);
                Assert.True(string.IsNullOrEmpty(skill.UnlockRules.QuestFlag),
                    $"Goddess Skill {skill.SkillId} cannot have QuestFlag.");
            }
        }
    }

    [Fact]
    public void ValidateStageUpgradeRulesIntegrity()
    {
        var skills = LoadSkills();
        foreach (var skill in skills)
        {
            if (skill.StageUpgradeRules != null)
            {
                // Rule: If StageUpgradeRules exists, NextSkillId MUST be present.
                Assert.True(skill.StageUpgradeRules.NextSkillId.HasValue,
                    $"Skill {skill.SkillId} has StageUpgradeRules but missing NextSkillId.");

                Assert.True(skill.StageUpgradeRules.RankThreshold > 0,
                    $"Skill {skill.SkillId} has StageUpgradeRules but invalid RankThreshold {skill.StageUpgradeRules.RankThreshold}");
            }
        }
    }

    [Fact]
    public void ValidateContentIntegrity()
    {
        var skills = LoadSkills();
        var quests = LoadQuests();

        // 1. Check for Duplicate SkillIds
        var duplicateSkillIds = skills.GroupBy(s => s.SkillId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(duplicateSkillIds.Count == 0, $"Duplicate SkillIds found: {string.Join(", ", duplicateSkillIds)}");

        // 2. Check for Duplicate QuestIds
        var duplicateQuestIds = quests.GroupBy(q => q.QuestId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(duplicateQuestIds.Count == 0, $"Duplicate QuestIds found: {string.Join(", ", duplicateQuestIds)}");

        // 3. Integrity Quest -> Skill (GrantSkillId must exist)
        var skillIds = skills.Select(s => s.SkillId).ToHashSet();
        foreach (var quest in quests)
        {
            if (quest.Rewards.GrantSkillId.HasValue)
            {
                Assert.True(skillIds.Contains(quest.Rewards.GrantSkillId.Value),
                    $"Quest {quest.QuestId} ({quest.Title}) grants non-existent SkillId {quest.Rewards.GrantSkillId}");
            }
        }

        // 4. Integrity Skill -> Quest (Special Skills must have origin)
        var goddessSkillIds = new HashSet<int> { 2001, 2002, 2003, 2004 }; // Exception GS
        var specialSkills = skills.Where(s => s.Family == SkillFamily.Special).ToList();

        var skillsGrantedByQuests = quests
            .Where(q => q.Rewards.GrantSkillId.HasValue)
            .Select(q => q.Rewards.GrantSkillId.Value)
            .ToHashSet();

        var questIds = quests.Select(q => q.QuestId).ToHashSet();

        foreach (var skill in specialSkills)
        {
            // GS Exception: Should NOT be granted by Quests
            if (goddessSkillIds.Contains(skill.SkillId))
            {
                Assert.False(skillsGrantedByQuests.Contains(skill.SkillId),
                    $"Goddess Skill {skill.SkillId} ({skill.Name}) should NOT be granted by any Quest.");
                continue;
            }

            // Stage Rules Rule: Special Skills should not have StageUpgradeRules
            Assert.Null(skill.StageUpgradeRules);

            var hasQuestOrigin = skillsGrantedByQuests.Contains(skill.SkillId);
            var hasFlagOrigin = !string.IsNullOrEmpty(skill.UnlockRules?.QuestFlag);
            var hasQuestIdOrigin = skill.UnlockRules?.QuestId.HasValue ?? false;

            Assert.True(hasQuestOrigin || hasFlagOrigin || hasQuestIdOrigin,
                $"Special Skill {skill.SkillId} ({skill.Name}) has no origin (QuestFlag/QuestId in UnlockRules or GrantSkillId in a Quest).");

            // If Skill refers to a QuestId in UnlockRules, verify that quest exists
            if (skill.UnlockRules?.QuestId is int qId)
            {
                Assert.True(questIds.Contains(qId),
                    $"Skill {skill.SkillId} refers to non-existent QuestId {qId} in UnlockRules.");
            }
        }

        // 5. Stage Upgrade Rules Integrity (Anti-Snowball)
        // Ensure NextSkillId exists
        foreach (var skill in skills)
        {
            if (skill.StageUpgradeRules?.NextSkillId.HasValue == true)
            {
                Assert.True(skillIds.Contains(skill.StageUpgradeRules.NextSkillId.Value),
                    $"Skill {skill.SkillId} StageUpgrade refers to non-existent NextSkillId {skill.StageUpgradeRules.NextSkillId}");
            }
        }
    }

    [Fact]
    public void ValidateStageUpgradeConsistency()
    {
        var skills = LoadSkills();
        var skillMap = skills.ToDictionary(s => s.SkillId);

        foreach (var skill in skills)
        {
            if (skill.StageUpgradeRules?.NextSkillId is int nextId)
            {
                Assert.True(skillMap.ContainsKey(nextId),
                    $"Skill {skill.SkillId} upgrades to non-existent skill {nextId}");

                var nextSkill = skillMap[nextId];
                if (nextSkill.UnlockRules?.ParentSkillId.HasValue == true)
                {
                    Assert.Equal(skill.SkillId, nextSkill.UnlockRules.ParentSkillId.Value); // Must point back to parent

                    if (skill.StageUpgradeRules.RankThreshold > 0 && nextSkill.UnlockRules.ParentSkillRank.HasValue)
                    {
                        Assert.Equal(skill.StageUpgradeRules.RankThreshold,
                            nextSkill.UnlockRules.ParentSkillRank.Value);
                    }
                }
            }
        }
    }

    [Fact]
    public void ValidateSkillRewardsConsistency()
    {
        var skills = LoadSkills();
        var quests = LoadQuests();
        var skillMap = skills.ToDictionary(s => s.SkillId);

        foreach (var quest in quests)
        {
            if (quest.Rewards.GrantSkillId is int skillId)
            {
                Assert.True(skillMap.ContainsKey(skillId), $"Quest {quest.QuestId} grants unknown skill {skillId}");
                var skill = skillMap[skillId];

                // Rule: Skills granted by quests must have UniquePerCharacter=true
                Assert.True(skill.Restrictions?.UniquePerCharacter == true,
                    $"Skill {skillId} ({skill.Name}) granted by Quest {quest.QuestId} must have Restrictions.UniquePerCharacter = true");

                // Rule: Quests CANNOT grant Goddess Skills
                Assert.NotEqual(SkillCategory.Goddess, skill.Category);
            }
        }
    }

    [Fact]
    public void ValidateQuestIdempotency()
    {
        var quests = LoadQuests();
        foreach (var quest in quests)
        {
            if (quest.Rewards.GrantSkillId.HasValue)
            {
                // If a quest grants a skill, it must be idempotent.
                // Either the quest itself is one-off (Repeatability.None)
                // OR it has explicit AntiAbuseRules containing "UniquePerCharacter".

                var isOneOff = quest.Repeatability == QuestRepeatability.None;
                var hasUniqueRule = !string.IsNullOrEmpty(quest.AntiAbuseRules) &&
                                    quest.AntiAbuseRules.Contains("UniquePerCharacter");

                Assert.True(isOneOff || hasUniqueRule,
                    $"Quest {quest.QuestId} grants a skill but is repeatable and lacks 'UniquePerCharacter' AntiAbuseRule.");
            }
        }
    }

    [Fact]
    public void ValidateUniqueDisplayNameKeys()
    {
        var skills = LoadSkills();
        var duplicates = skills
            .Where(s => !string.IsNullOrEmpty(s.DisplayNameKey))
            .GroupBy(s => s.DisplayNameKey)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, $"Duplicate DisplayNameKeys found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void ValidateCrossDomainIntegrity()
    {
        var skills = LoadSkills();
        var quests = LoadQuests();
        var pets = LoadPets();

        var petIds = pets.Select(p => p.PetTypeId).ToHashSet();
        var questFlagsSet = quests
            .SelectMany(q => q.FlagsSet ?? new List<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1. Skill -> Quest Flags
        // Verify that if a skill requires a flag, that flag is set by some quest.
        foreach (var skill in skills)
        {
            if (!string.IsNullOrEmpty(skill.UnlockRules?.QuestFlag))
            {
                Assert.True(questFlagsSet.Contains(skill.UnlockRules.QuestFlag),
                    $"Skill {skill.SkillId} ({skill.Name}) requires QuestFlag '{skill.UnlockRules.QuestFlag}' but no quest sets it.");
            }
        }

        // 2. Quest -> Pet Unlock
        // Verify that if a quest unlocks a pet, the PetId exists.
        foreach (var quest in quests)
        {
            if (quest.Rewards.PetUnlockId.HasValue)
            {
                Assert.True(petIds.Contains(quest.Rewards.PetUnlockId.Value),
                    $"Quest {quest.QuestId} ({quest.Title}) unlocks non-existent PetId {quest.Rewards.PetUnlockId.Value}.");
            }
        }
    }

    [Fact]
    public void ValidateSkillRequirements()
    {
        var skills = LoadSkills();
        // Verify that at least one skill has requirements (to ensure loading isn't broken)
        var anyRequirements = skills.Any(s =>
            s.Requirements.Str > 0 ||
            s.Requirements.Con > 0 ||
            s.Requirements.Int > 0 ||
            s.Requirements.Wis > 0 ||
            s.Requirements.Agi > 0);

        Assert.True(anyRequirements, "No skill requirements detected. Check JSON loading/serialization.");
    }

    [Fact]
    public void ValidateTierBudgets()
    {
        var skills = LoadSkills();

        // Tier 1 (Core): SP 5-20, Cooldown 0-2
        var coreT1 = skills.Where(s => s.Family == SkillFamily.Core && s.Tier == 1).ToList();
        foreach (var skill in coreT1)
        {
            Assert.True(skill.SpCost >= 5 && skill.SpCost <= 20,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 1 Core violated SP Budget [5-20]. Value: {skill.SpCost}");
            Assert.True(skill.Cooldown >= 0 && skill.Cooldown <= 2,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 1 Core violated CD Budget [0-2]. Value: {skill.Cooldown}");
        }

        // Tier 2 (Core): SP 15-40, Cooldown 1-3
        var coreT2 = skills.Where(s => s.Family == SkillFamily.Core && s.Tier == 2).ToList();
        foreach (var skill in coreT2)
        {
            Assert.True(skill.SpCost >= 15 && skill.SpCost <= 40,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 2 Core violated SP Budget [15-40]. Value: {skill.SpCost}");
            Assert.True(skill.Cooldown >= 1 && skill.Cooldown <= 3,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 2 Core violated CD Budget [1-3]. Value: {skill.Cooldown}");
        }

        // Tier 3 (Core / Special): SP 30-100, Cooldown 3-6
        var tier3 = skills.Where(s => s.Tier == 3 &&
                                      (s.Family == SkillFamily.Core || s.Family == SkillFamily.Special)).ToList();
        foreach (var skill in tier3)
        {
            Assert.True(skill.SpCost >= 30 && skill.SpCost <= 100,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 3 violated SP Budget [30-100]. Value: {skill.SpCost}");
            Assert.True(skill.Cooldown >= 3 && skill.Cooldown <= 6,
                $"Skill {skill.SkillId} ({skill.Name}) Tier 3 violated CD Budget [3-6]. Value: {skill.Cooldown}");
        }
    }

    private List<MonsterDefinition> LoadMonsters()
    {
        var root = GetContentRoot();
        var path = Path.Combine(root, "monsters.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find monsters.json at {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<MonsterDefinition>>(json, GetJsonOptions()) ??
               new List<MonsterDefinition>();
    }

    private List<ZoneSpawnConfig> LoadSpawnConfigs()
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

    [Fact]
    public void ValidateMonsterElements()
    {
        var monsters = LoadMonsters();
        foreach (var monster in monsters)
        {
            if (monster.Element == Element.None)
            {
                Assert.Contains("QuestOnly", monster.Tags);
            }
        }
    }

    [Fact]
    public void ValidatePetElements()
    {
        var pets = LoadPets();
        foreach (var pet in pets)
        {
            Assert.NotEqual(Element.None, pet.Element);
        }
    }

    [Fact]
    public void ValidateSpawnConfigs()
    {
        var configs = LoadSpawnConfigs();
        var monsters = LoadMonsters();
        var monsterIds = monsters.Select(m => m.MonsterId).ToHashSet();

        foreach (var config in configs)
        {
            Assert.True(config.MapId > 0, "MapId must be positive");

            foreach (var region in config.SpawnRegions)
            {
                foreach (var mid in region.AllowedMonsterIds)
                {
                    Assert.True(monsterIds.Contains(mid),
                        $"Spawn config for map {config.MapId} references unknown MonsterId {mid}");
                }
            }
        }
    }
}
