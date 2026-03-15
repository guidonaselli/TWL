using Xunit;
using Moq;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

/// <summary>
/// Validates pet evolution (skill unlock and level reset) on rebirth and multi-generation progression.
/// Covers requirements PET-04 (evolution/action routing).
/// </summary>
public class PetRebirthEvolutionTests : IDisposable
{
    private const int RebirthSkillId = 5001;

    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;
    private readonly Mock<MonsterManager> _mockMonsterManager;

    public PetRebirthEvolutionTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);
        _petManager = new PetManager();

        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_evolution_test.json", @"
[
  {
    ""PetTypeId"": 1002,
    ""Name"": ""Evolving Quest Pet"",
    ""IsQuestPet"": true,
    ""RebirthEligible"": true,
    ""RebirthSkillId"": 5001,
    ""BaseHp"": 200,
    ""Element"": ""Fire"",
    ""EvolutionId"": 2002
  },
  {
    ""PetTypeId"": 2001,
    ""Name"": ""Young Dragon"",
    ""IsQuestPet"": true,
    ""EvolutionId"": 2002,
    ""BaseHp"": 150,
    ""Element"": ""Fire""
  },
  {
    ""PetTypeId"": 2002,
    ""Name"": ""Elder Dragon"",
    ""IsQuestPet"": true,
    ""BaseHp"": 300,
    ""Element"": ""Fire""
  },
  {
    ""PetTypeId"": 2003,
    ""Name"": ""Wild Cat"",
    ""IsQuestPet"": false,
    ""EvolutionId"": 2004,
    ""BaseHp"": 80,
    ""Element"": ""Wind""
  }
]");
        _petManager.Load("Content/Data/pets_evolution_test.json");

        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();
        
        var mockResolver = new Mock<ICombatResolver>();
        var mockSkills = new Mock<ISkillCatalog>();
        var mockStatus = new Mock<IStatusEngine>();
        
        _combatManager = new CombatManager(mockResolver.Object, _mockRandom.Object, mockSkills.Object, mockStatus.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, new Mock<Microsoft.Extensions.Logging.ILogger<PetService>>().Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_evolution_test.json"))
            File.Delete("Content/Data/pets_evolution_test.json");
    }

    private static PetDefinition MakeQuestPetDefWithRebirthSkill() => new()
    {
        PetTypeId = 1002,
        Name = "Evolving Quest Pet",
        Type = PetType.Quest,
        IsQuestPet = true,
        RebirthEligible = true,
        RebirthSkillId = RebirthSkillId,
        Element = Element.Fire,
        BaseHp = 200,
        BaseStr = 10, BaseCon = 10, BaseInt = 10, BaseWis = 10, BaseAgi = 10,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 5, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }
    };

    private static PetDefinition MakeQuestPetDefNoSkill() => new()
    {
        PetTypeId = 1003,
        Name = "Simple Quest Pet",
        Type = PetType.Quest,
        IsQuestPet = true,
        RebirthEligible = true,
        RebirthSkillId = 0, // No rebirth skill
        Element = Element.Wind,
        BaseHp = 150,
        BaseStr = 8, BaseCon = 8, BaseInt = 8, BaseWis = 8, BaseAgi = 8,
        GrowthModel = new PetGrowthModel { HpGrowthPerLevel = 3, StrWeight = 20, ConWeight = 20, IntWeight = 20, WisWeight = 20, AgiWeight = 20 }
    };

    // ─── Unit Tests from HEAD ───────────────────────────────────────────────────

    [Fact]
    public void TryRebirth_WithRebirthSkillId_UnlocksSkillOnFirstRebirth()
    {
        var pet = new ServerPet(MakeQuestPetDefWithRebirthSkill());
        pet.SetLevel(100);

        var result = pet.TryRebirth();

        Assert.True(result);
        Assert.Contains(RebirthSkillId, pet.UnlockedSkillIds);
    }

    [Fact]
    public void TryRebirth_SecondRebirth_DoesNotDuplicateRebirthSkill()
    {
        var pet = new ServerPet(MakeQuestPetDefWithRebirthSkill());
        pet.SetLevel(100);
        pet.TryRebirth();         // Gen 1 — skill unlocked
        int skillCountAfterGen1 = pet.UnlockedSkillIds.Count(id => id == RebirthSkillId);

        pet.SetLevel(100);
        pet.TryRebirth();         // Gen 2 — skill should not be duplicated

        int skillCountAfterGen2 = pet.UnlockedSkillIds.Count(id => id == RebirthSkillId);
        Assert.Equal(skillCountAfterGen1, skillCountAfterGen2);
    }

    [Fact]
    public void TryRebirth_WithNoRebirthSkill_DoesNotAddAnyRebirthSkill()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);
        var skillsBefore = pet.UnlockedSkillIds.ToList();

        pet.TryRebirth();

        // Skill list unchanged (no rebirth skill to unlock)
        Assert.Equal(skillsBefore.Count, pet.UnlockedSkillIds.Count);
    }

    [Fact]
    public void TryRebirth_ResetsLevelToOne()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());
        pet.SetLevel(100);

        pet.TryRebirth();

        Assert.Equal(1, pet.Level);
        Assert.Equal(0, pet.Exp);
    }

    [Fact]
    public void TryRebirth_IncreasesGenerationWithEachRebirth()
    {
        var pet = new ServerPet(MakeQuestPetDefNoSkill());

        for (int gen = 1; gen <= 5; gen++)
        {
            pet.SetLevel(100);
            pet.TryRebirth();
            Assert.Equal(gen, pet.RebirthGeneration);
        }
    }

    // ─── Integration Tests from S06 ─────────────────────────────────────────────

    [Fact]
    public void QuestPet_CanEvolve_AtLevel100()
    {
        // Setup session
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Player" });
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(2001);
        var pet = new ServerPet(def);
        pet.SetLevel(100);
        session.Character.AddPet(pet);

        // Act
        var success = _petService.TryEvolve(1, pet.InstanceId);
        
        // Assert
        Assert.True(success);
        Assert.Equal(2002, pet.DefinitionId);
        Assert.Equal("Elder Dragon", pet.Name);
        Assert.True(pet.MaxHp >= 300);
    }

    [Fact]
    public void QuestPet_CannotEvolve_BelowLevel100()
    {
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1 });
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(2001);
        var pet = new ServerPet(def);
        pet.SetLevel(99);
        session.Character.AddPet(pet);

        var success = _petService.TryEvolve(1, pet.InstanceId);
        Assert.False(success);
        Assert.Equal(2001, pet.DefinitionId);
    }

}
