using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Skills;
using Xunit;

namespace TWL.Tests
{
    public class ContentValidationTests
    {
        private string GetContentRoot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Try to find the path by going up levels
            var current = new DirectoryInfo(baseDir);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "Content/Data");
                if (Directory.Exists(candidate)) return candidate;
                current = current.Parent;
            }
            // Fallback for direct test execution if structure differs
             return "../../../../Content/Data";
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
            var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/skills.json");
            // Ensure path exists
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find skills.json at {Path.GetFullPath(path)}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Skill>>(json, GetJsonOptions()) ?? new List<Skill>();
        }

        private List<QuestDefinition> LoadQuests()
        {
            var root = GetContentRoot();
            var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/quests.json");
            // Ensure path exists
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find quests.json at {Path.GetFullPath(path)}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<QuestDefinition>>(json, GetJsonOptions()) ?? new List<QuestDefinition>();
        }

        private List<PetDefinition> LoadPets()
        {
            var root = GetContentRoot();
            var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Content/Data/pets.json");
            // Ensure path exists
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find pets.json at {Path.GetFullPath(path)}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<PetDefinition>>(json, GetJsonOptions()) ?? new List<PetDefinition>();
        }

        [Fact]
        public void ValidateGoddessSkills()
        {
            var skills = LoadSkills();
            var goddessMap = new Dictionary<int, string>
            {
                { 2001, "Shrink" },
                { 2002, "Blockage" },
                { 2003, "Hotfire" },
                { 2004, "Vanish" }
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
                    Assert.True(string.IsNullOrEmpty(skill.UnlockRules.QuestFlag), $"Goddess Skill {skill.SkillId} cannot have QuestFlag.");
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

                bool hasQuestOrigin = skillsGrantedByQuests.Contains(skill.SkillId);
                bool hasFlagOrigin = !string.IsNullOrEmpty(skill.UnlockRules?.QuestFlag);
                bool hasQuestIdOrigin = skill.UnlockRules?.QuestId.HasValue ?? false;

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
                             Assert.Equal(skill.StageUpgradeRules.RankThreshold, nextSkill.UnlockRules.ParentSkillRank.Value);
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
    }
}
