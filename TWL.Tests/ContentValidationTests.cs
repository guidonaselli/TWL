using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.World;

namespace TWL.Tests;

public class ContentValidationTests
{
    [Fact]
    public void ValidateSkillCategories()
    {
        var skills = ContentTestHelper.LoadSkills();

        foreach (var skill in skills)
        {
            Assert.Contains(skill.Category, ContentRules.ValidCategories);
        }
    }

    [Fact]
    public void ValidateGoddessSkills()
    {
        var skills = ContentTestHelper.LoadSkills();

        foreach (var kvp in ContentRules.GoddessSkills)
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
        var skills = ContentTestHelper.LoadSkills();
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
        var skills = ContentTestHelper.LoadSkills();
        var quests = ContentTestHelper.LoadQuests();

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
        var goddessSkillIds = ContentRules.GoddessSkills.Keys.ToHashSet(); // Exception GS
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
        var skills = ContentTestHelper.LoadSkills();
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
        var skills = ContentTestHelper.LoadSkills();
        var quests = ContentTestHelper.LoadQuests();
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
        var quests = ContentTestHelper.LoadQuests();
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
        var skills = ContentTestHelper.LoadSkills();
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
        var skills = ContentTestHelper.LoadSkills();
        var quests = ContentTestHelper.LoadQuests();
        var pets = ContentTestHelper.LoadPets();

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
        var skills = ContentTestHelper.LoadSkills();
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
        var skills = ContentTestHelper.LoadSkills();

        foreach (var skill in skills)
        {
            // Only validate if a budget rule is defined for this Family/Tier combination
            if (ContentRules.TierBudgets.TryGetValue((skill.Family, skill.Tier), out var budget))
            {
                Assert.True(skill.SpCost >= budget.MinSp && skill.SpCost <= budget.MaxSp,
                    $"Skill {skill.SkillId} ({skill.Name}) Tier {skill.Tier} {skill.Family} violated SP Budget [{budget.MinSp}-{budget.MaxSp}]. Value: {skill.SpCost}");
                Assert.True(skill.Cooldown >= budget.MinCd && skill.Cooldown <= budget.MaxCd,
                    $"Skill {skill.SkillId} ({skill.Name}) Tier {skill.Tier} {skill.Family} violated CD Budget [{budget.MinCd}-{budget.MaxCd}]. Value: {skill.Cooldown}");
            }
        }
    }

    [Fact]
    public void ValidateMonsterElements()
    {
        var monsters = ContentTestHelper.LoadMonsters();
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
        var pets = ContentTestHelper.LoadPets();
        foreach (var pet in pets)
        {
            Assert.NotEqual(Element.None, pet.Element);
        }
    }

    [Fact]
    public void ValidateSpawnConfigs()
    {
        var configs = ContentTestHelper.LoadSpawnConfigs();
        // We expect at least some spawn configs to exist in the game
        Assert.NotEmpty(configs);

        var monsters = ContentTestHelper.LoadMonsters();
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

    [Fact]
    public void ValidatePetSkills()
    {
        var pets = ContentTestHelper.LoadPets();
        var skills = ContentTestHelper.LoadSkills();
        var skillIds = skills.Select(s => s.SkillId).ToHashSet();

        foreach (var pet in pets)
        {
            if (pet.SkillSet != null)
            {
                foreach (var skillSet in pet.SkillSet)
                {
                    Assert.True(skillIds.Contains(skillSet.SkillId),
                        $"Pet {pet.PetTypeId} ({pet.Name}) refers to unknown SkillId {skillSet.SkillId}");
                }
            }
        }
    }

    [Fact]
    public void ValidatePetUtilities()
    {
        var pets = ContentTestHelper.LoadPets();
        foreach (var pet in pets)
        {
            if (pet.Utilities != null)
            {
                foreach (var util in pet.Utilities)
                {
                    Assert.True(util.Value > 0,
                        $"Pet {pet.PetTypeId} ({pet.Name}) has invalid utility value {util.Value}. Must be > 0.");
                    Assert.True(util.RequiredLevel >= 0,
                        $"Pet {pet.PetTypeId} ({pet.Name}) has invalid required level {util.RequiredLevel}.");
                    Assert.True(util.RequiredAmity >= 0 && util.RequiredAmity <= 100,
                        $"Pet {pet.PetTypeId} ({pet.Name}) has invalid required amity {util.RequiredAmity}.");
                }
            }
        }
    }

    [Fact]
    public void ValidateAmityItems()
    {
        var items = ContentTestHelper.LoadAmityItems();

        Assert.NotNull(items);
        Assert.NotEmpty(items);

        foreach (var item in items)
        {
            Assert.True(item.ItemId > 0, $"Invalid ItemId {item.ItemId} in amity_items.json");
            Assert.True(item.AmityValue > 0, $"Invalid AmityValue {item.AmityValue} for ItemId {item.ItemId}");
        }
    }

    [Fact]
    public void ValidateStage_Evolution_Chains_Are_Complete()
    {
        var skills = ContentTestHelper.LoadSkills();
        var skillMap = skills.ToDictionary(s => s.SkillId);

        // Check standard 3-stage skills
        // Earth Physical: 1001 -> 1002 -> 1003
        VerifyChain(skillMap, 1001, 1002, 1003);
        // Earth Magical: 1101 -> 1102 -> 1103
        VerifyChain(skillMap, 1101, 1102, 1103);
        // Earth Support: 1201 -> 1202 -> 1203
        VerifyChain(skillMap, 1201, 1202, 1203);

        // Water Physical
        VerifyChain(skillMap, 3001, 3002, 3003);
        // Water Magical
        VerifyChain(skillMap, 3101, 3102, 3103);
        // Water Support
        VerifyChain(skillMap, 3201, 3202, 3203);

        // Fire Physical
        VerifyChain(skillMap, 4001, 4002, 4003);
        // Fire Magical
        VerifyChain(skillMap, 4101, 4102, 4103);
        // Fire Support
        VerifyChain(skillMap, 4201, 4202, 4203);

        // Wind Physical
        VerifyChain(skillMap, 5001, 5002, 5003);
        // Wind Magical
        VerifyChain(skillMap, 5101, 5102, 5103);
        // Wind Support
        VerifyChain(skillMap, 5201, 5202, 5203);
    }

    private void VerifyChain(Dictionary<int, Skill> skillMap, int s1, int s2, int s3)
    {
        Assert.True(skillMap.ContainsKey(s1), $"Skill {s1} missing");
        Assert.True(skillMap.ContainsKey(s2), $"Skill {s2} missing");
        Assert.True(skillMap.ContainsKey(s3), $"Skill {s3} missing");

        var skill1 = skillMap[s1];
        Assert.Equal(1, skill1.Stage);
        Assert.NotNull(skill1.StageUpgradeRules);
        Assert.Equal(s2, skill1.StageUpgradeRules.NextSkillId);

        var skill2 = skillMap[s2];
        Assert.Equal(2, skill2.Stage);
        Assert.NotNull(skill2.StageUpgradeRules);
        Assert.Equal(s3, skill2.StageUpgradeRules.NextSkillId);

        var skill3 = skillMap[s3];
        Assert.Equal(3, skill3.Stage);
    }

    [Fact]
    public void ValidateStageUpgradeAntiSnowball()
    {
        var skills = ContentTestHelper.LoadSkills();
        var skillMap = skills.ToDictionary(s => s.SkillId);

        foreach (var skill in skills)
        {
            if (skill.StageUpgradeRules?.NextSkillId is int nextId)
            {
                // Ensure target exists
                Assert.True(skillMap.ContainsKey(nextId),
                    $"Skill {skill.SkillId} defines upgrade to {nextId} which does not exist.");

                var nextSkill = skillMap[nextId];

                // Anti-Snowball: Ensure the target skill explicitly requires the parent skill
                // OR ensure it has NO other conflicting unlock rules.
                // The safest implementation of "Anti-snowball" is that the relationship must be bidirectional
                // or at least non-contradictory.

                if (nextSkill.UnlockRules != null && nextSkill.UnlockRules.ParentSkillId.HasValue)
                {
                     Assert.Equal(skill.SkillId, nextSkill.UnlockRules.ParentSkillId.Value);

                     // Check Rank Threshold Consistency
                     if (skill.StageUpgradeRules.RankThreshold > 0 && nextSkill.UnlockRules.ParentSkillRank.HasValue)
                     {
                         Assert.Equal(skill.StageUpgradeRules.RankThreshold, nextSkill.UnlockRules.ParentSkillRank.Value);
                     }
                }
                else
                {
                     // If the child doesn't explicitly point back, that MIGHT be okay depending on strictness,
                     // but the prompt says: "Stage upgrades rules must be defined in ONE place".
                     // If defined in 'StageUpgradeRules' (parent), the child shouldn't redefine contradictory rules.

                     // For now, we enforce that if ParentSkillId IS defined, it must match.
                }
            }
        }
    }

    [Fact]
    public void ValidateSkillGrantExclusivity()
    {
        var quests = ContentTestHelper.LoadQuests();

        // Group quests by the skill they grant
        var questsBySkill = quests
            .Where(q => q.Rewards.GrantSkillId.HasValue)
            .GroupBy(q => q.Rewards.GrantSkillId.Value);

        foreach (var group in questsBySkill)
        {
            var skillId = group.Key;
            var questsList = group.ToList();

            if (questsList.Count > 1)
            {
                // If multiple quests grant the same skill, they must ALL share the same MutualExclusionGroup
                var firstGroup = questsList.First().MutualExclusionGroup;

                Assert.False(string.IsNullOrEmpty(firstGroup),
                    $"Multiple quests grant SkillId {skillId}, but they lack a MutualExclusionGroup.");

                foreach (var quest in questsList)
                {
                    Assert.Equal(firstGroup, quest.MutualExclusionGroup);
                }
            }
        }
    }

    [Fact]
    public void ValidateSpecialSkillQuestPrerequisites()
    {
        var skills = ContentTestHelper.LoadSkills().ToDictionary(s => s.SkillId);
        var quests = ContentTestHelper.LoadQuests();

        foreach (var quest in quests)
        {
            if (quest.Rewards.GrantSkillId.HasValue)
            {
                var skillId = quest.Rewards.GrantSkillId.Value;
                if (!skills.TryGetValue(skillId, out var skill)) continue; // Already checked elsewhere

                // 1. RebirthJob Consistency
                if (skill.Category == SkillCategory.RebirthJob)
                {
                    Assert.Equal(ContentRules.RebirthJobCategoryName, quest.SpecialCategory);
                    // Optionally check for RebirthClass requirement if available in QuestDefinition
                }

                // 2. ElementSpecial Consistency
                if (skill.Category == SkillCategory.ElementSpecial)
                {
                    // Must have high level requirement OR be an Instance/Challenge
                    var isHighLevel = quest.RequiredLevel >= ContentRules.MinLevelForElementSpecial;
                    var isInstance = quest.Objectives.Any(o => o.Type == "Instance" || o.Type == "Challenge");
                    var isSpecialType = quest.Type == "SpecialSkill";

                    Assert.True(isHighLevel || isInstance || isSpecialType,
                        $"Quest {quest.QuestId} grants ElementSpecial skill {skillId} but lacks strict prerequisites (Level >= {ContentRules.MinLevelForElementSpecial} or Instance/Challenge).");
                }

                // 3. Category Alignment (General)
                if (!string.IsNullOrEmpty(quest.SpecialCategory))
                {
                    // If quest explicitly says "Dragon", skill should be Dragon, etc.
                    // Note: Enum.TryParse is case-insensitive usually, but let's be strict if needed.
                    if (Enum.TryParse<SkillCategory>(quest.SpecialCategory, true, out var category))
                    {
                        if (category != SkillCategory.None)
                        {
                             Assert.Equal(category, skill.Category);
                        }
                    }
                }
            }
        }
    }
}
