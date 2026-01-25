
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                var candidate = Path.Combine(current.FullName, "TWL.Server/Content/Data");
                if (Directory.Exists(candidate)) return candidate;
                current = current.Parent;
            }
            // Fallback for direct test execution if structure differs
             return "../../../../TWL.Server/Content/Data";
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
            // Ensure path exists
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find skills.json at {Path.GetFullPath(path)}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Skill>>(json, GetJsonOptions()) ?? new List<Skill>();
        }

        private List<QuestDefinition> LoadQuests()
        {
            var root = GetContentRoot();
            var path = Path.Combine(root, "quests.json");
            // Ensure path exists
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find quests.json at {Path.GetFullPath(path)}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<QuestDefinition>>(json, GetJsonOptions()) ?? new List<QuestDefinition>();
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
                bool hasQuestIdOrigin = !string.IsNullOrEmpty(skill.UnlockRules?.QuestId);

                Assert.True(hasQuestOrigin || hasFlagOrigin || hasQuestIdOrigin,
                    $"Special Skill {skill.SkillId} ({skill.Name}) has no origin (QuestFlag/QuestId in UnlockRules or GrantSkillId in a Quest).");

                // If Skill refers to a QuestId in UnlockRules, verify that quest exists
                if (hasQuestIdOrigin && int.TryParse(skill.UnlockRules!.QuestId, out int qId))
                {
                     Assert.True(questIds.Contains(qId),
                         $"Skill {skill.SkillId} refers to non-existent QuestId {qId} in UnlockRules.");
                }
            }
        }
    }
}
