using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests;

public class ContentValidationExtendedTests
{
    private class ContentIndex
    {
        public List<Skill> Skills { get; set; } = new();
        public List<QuestDefinition> Quests { get; set; } = new();
        public List<PetDefinition> Pets { get; set; } = new();

        public Dictionary<int, Skill> SkillMap { get; set; } = new();
        public Dictionary<int, QuestDefinition> QuestMap { get; set; } = new();
    }

    private ContentIndex LoadContent()
    {
        var index = new ContentIndex();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var root = GetContentRoot();

        // Load Skills
        var skillsPath = Path.Combine(root, "skills.json");
        if (File.Exists(skillsPath))
        {
            var json = File.ReadAllText(skillsPath);
            index.Skills = JsonSerializer.Deserialize<List<Skill>>(json, options) ?? new List<Skill>();
            index.SkillMap = index.Skills.ToDictionary(s => s.SkillId);
        }

        // Load Quests (Multiple files)
        var questFiles = Directory.GetFiles(root, "quests*.json");
        foreach (var file in questFiles)
        {
            var json = File.ReadAllText(file);
            var quests = JsonSerializer.Deserialize<List<QuestDefinition>>(json, options) ?? new List<QuestDefinition>();
            index.Quests.AddRange(quests);
        }
        index.QuestMap = index.Quests.ToDictionary(q => q.QuestId);

        // Load Pets
        var petsPath = Path.Combine(root, "pets.json");
        if (File.Exists(petsPath))
        {
            var json = File.ReadAllText(petsPath);
            index.Pets = JsonSerializer.Deserialize<List<PetDefinition>>(json, options) ?? new List<PetDefinition>();
        }

        return index;
    }

    private string GetContentRoot()
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

    [Fact]
    public void ValidateQuestGrantSkillDuplicates()
    {
        var index = LoadContent();

        // Group quests by the skill they grant
        var skillGrants = index.Quests
            .Where(q => q.Rewards.GrantSkillId.HasValue)
            .GroupBy(q => q.Rewards.GrantSkillId.Value)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in skillGrants)
        {
            var skillId = group.Key;
            var questIds = group.Select(q => q.QuestId).ToList();

            // Check if all quests in the group belong to the same MutualExclusionGroup
            var exclusionGroups = group
                .Select(q => q.MutualExclusionGroup)
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct()
                .ToList();

            // Rule: If multiple quests grant the same skill, they must be mutually exclusive alternatives.
            // This means they must share a MutualExclusionGroup and there should be only 1 distinct group name involved.
            // OR the skill must have UniquePerCharacter=true (which is handled by idempotency check elsewhere, but here we focus on quest structure).

            // If they are not mutually exclusive, it's a potential duplication error unless it's intended.
            // The prompt says: "Rewards de skills por quests son idempotentes y únicos donde aplique."
            // "Dos quests que otorgan la misma GrantSkill sin estar declaradas como alternativa exclusiva." -> FAIL

            bool areMutuallyExclusive = exclusionGroups.Count == 1 &&
                                      group.All(q => q.MutualExclusionGroup == exclusionGroups[0]);

            Assert.True(areMutuallyExclusive,
                $"Skill {skillId} is granted by multiple quests ({string.Join(", ", questIds)}) that are NOT mutually exclusive alternatives.");
        }
    }

    [Fact]
    public void ValidateQuestGrantSkillFamily()
    {
        var index = LoadContent();

        foreach (var quest in index.Quests)
        {
            if (quest.Rewards.GrantSkillId.HasValue)
            {
                var skillId = quest.Rewards.GrantSkillId.Value;
                Assert.True(index.SkillMap.ContainsKey(skillId), $"Quest {quest.QuestId} grants unknown skill {skillId}");

                var skill = index.SkillMap[skillId];

                // Rule: Quests should generally grant Special skills.
                // Exception: Maybe tutorial quests granting basic skills?
                // The prompt says: "la skill es Family=Special salvo: caso GS..."
                // It implies that if a quest grants a skill, it MUST be Special.

                Assert.Equal(SkillFamily.Special, skill.Family);

                // Rule: Quests CANNOT grant Goddess Skills (2001-2004)
                Assert.NotEqual(SkillCategory.Goddess, skill.Category);
            }
        }
    }

    [Fact]
    public void ValidateRebirthJobIntegrity()
    {
        var index = LoadContent();
        var rebirthSkills = index.Skills.Where(s => s.Category == SkillCategory.RebirthJob).ToList();

        foreach (var skill in rebirthSkills)
        {
            // Find quest granting this skill
            var grantingQuest = index.Quests.FirstOrDefault(q => q.Rewards.GrantSkillId == skill.SkillId);

            if (grantingQuest != null)
            {
                // Rule: "si la skill es RebirthJob, la quest exige RebirthClass"
                // Implementation: Check if SpecialCategory matches or if there's a specific flag/requirement.
                // Since "RebirthClass" isn't a standard property, we check if the quest is categorized correctly.

                Assert.Equal("RebirthJob", grantingQuest.SpecialCategory);

                // Also check for some requirement logic if possible.
                // Assuming "RebirthClass" is enforced via flags or explicit mechanics not fully visible in JSON schema yet,
                // but strictly enforcing SpecialCategory is a good start.
            }
        }
    }

    [Fact]
    public void ValidateElementSpecialIntegrity()
    {
        var index = LoadContent();
        var elementSpecialSkills = index.Skills.Where(s => s.Category == SkillCategory.ElementSpecial).ToList();

        foreach (var skill in elementSpecialSkills)
        {
            var grantingQuest = index.Quests.FirstOrDefault(q => q.Rewards.GrantSkillId == skill.SkillId);

            if (grantingQuest != null)
            {
                // Rule: "si la skill es ElementSpecial, la quest exige prereqs fuertes (mínimo level o challenge/instance)"
                bool hasLevelReq = grantingQuest.RequiredLevel >= 10; // Arbitrary threshold based on context?
                bool isInstance = grantingQuest.InstanceRules != null || grantingQuest.Type == "Instance" || grantingQuest.Objectives.Any(o => o.Type == "Instance");

                Assert.True(hasLevelReq || isInstance,
                    $"Quest {grantingQuest.QuestId} granting ElementSpecial skill {skill.SkillId} must have Level >= 10 or be an Instance/Challenge.");
            }
        }
    }

    [Fact]
    public void ValidateSpecialSkillOrigin()
    {
        var index = LoadContent();
        var specialSkills = index.Skills
            .Where(s => s.Family == SkillFamily.Special && s.Category != SkillCategory.Goddess)
            .ToList();

        foreach (var skill in specialSkills)
        {
            // Must have an origin: QuestFlag OR GrantSkillId in a Quest.
            bool hasQuestFlag = !string.IsNullOrEmpty(skill.UnlockRules?.QuestFlag);
            bool grantedByQuest = index.Quests.Any(q => q.Rewards.GrantSkillId == skill.SkillId);
            bool hasQuestIdUnlock = skill.UnlockRules?.QuestId.HasValue == true;

            Assert.True(hasQuestFlag || grantedByQuest || hasQuestIdUnlock,
                $"Special Skill {skill.SkillId} ({skill.Name}) has no origin (QuestFlag, QuestId, or GrantSkillId).");
        }
    }
}
