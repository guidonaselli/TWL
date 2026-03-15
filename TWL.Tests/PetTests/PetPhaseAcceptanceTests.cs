using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;
using TWL.Server.Simulation.Networking;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using Moq;
using Microsoft.Extensions.Logging;
using TWL.Shared.Services;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.PetTests;

public class PetPhaseAcceptanceTests : IDisposable
{
    private readonly PetService _petService;
    private readonly CombatManager _combatManager;
    private readonly PlayerService _playerService;
    private readonly PetManager _petManager;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<ILogger<PetService>> _mockLogger;

    private const string TestPetsFile = "Content/Data/pets_acceptance.json";

    public PetPhaseAcceptanceTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _playerService = new PlayerService(_mockRepo.Object, new ServerMetrics());

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText(TestPetsFile, @"
[
  {
    ""PetTypeId"": 1001,
    ""Name"": ""Acceptance Wolf"",
    ""Type"": ""Capture"",
    ""Element"": ""Fire"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""CaptureRules"": { ""IsCapturable"": true, ""BaseChance"": 1.0, ""LevelLimit"": 1 },
    ""SkillSet"": [ { ""SkillId"": 10, ""UnlockLevel"": 1, ""UnlockAmity"": 0 } ],
    ""Utilities"": [ { ""Type"": ""Mount"", ""Value"": 0.5, ""RequiredLevel"": 1, ""RequiredAmity"": 0 } ],
    ""BondTiers"": [
       { ""AmityThreshold"": 90, ""StatMultiplier"": 1.1, ""Name"": ""Intimate"" }
    ]
  }
]");
        _petManager.Load(TestPetsFile);

        _mockMonsterManager = new Mock<MonsterManager>();
        _mockRandom = new Mock<IRandomService>();
        _mockLogger = new Mock<ILogger<PetService>>();

        var mockResolver = new Mock<ICombatResolver>();
        var mockSkills = new Mock<ISkillCatalog>();
        var mockStatus = new Mock<IStatusEngine>();

        _combatManager = new CombatManager(mockResolver.Object, _mockRandom.Object, mockSkills.Object, mockStatus.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, _mockLogger.Object);
    }

    [Fact]
    public void PetLifecycle_Acceptance_Flow()
    {
        // 1. Setup Session
        var session = new ClientSessionForTest();
        var character = new ServerCharacter { Id = 1, Name = "Trainer" };
        character.Level = 10;
        session.SetCharacter(character);
        _playerService.RegisterSession(session);

        // 2. Capture (PET-02 Linkage)
        var monsterDef = new MonsterDefinition { MonsterId = 500, Name = "Wild Wolf", IsCapturable = true, PetTypeId = 1001, CaptureThreshold = 1.0f, Element = Element.Fire };
        _mockMonsterManager.Setup(m => m.GetDefinition(500)).Returns(monsterDef);
        
        var enemy = new ServerCharacter { Id = 500, MonsterId = 500, Team = Team.Enemy };
        enemy.Hp = 10;
        _combatManager.RegisterCombatant(enemy);
        _mockRandom.Setup(r => r.NextFloat(It.IsAny<string>())).Returns(0.0f);

        var petId = _petService.CaptureEnemy(1, 500);
        Assert.NotNull(petId);
        var pet = character.Pets[0];
        Assert.Equal(1001, pet.DefinitionId);

        // 3. Bonding (PET-05, PET-06)
        pet.Amity = 95;
        pet.RecalculateStats();
        // Base Str 10 * 1.1 (BondTier) = 11.
        Assert.Equal(11, pet.Str);
        Assert.Equal(11, pet.Con);

        // 4. Combat Death Amity Loss (PET-05)
        // We use a fresh CombatManager and PetService to ensure event subscription is clean
        var mockResolver = new Mock<ICombatResolver>();
        mockResolver.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>()))
            .Returns(999); // Instakill
            
        var mockSkills = new Mock<ISkillCatalog>();
        mockSkills.Setup(s => s.GetSkillById(0)).Returns(new Skill { SkillId = 0, Name = "Attack", Effects = new List<SkillEffect>() });

        var combatManager = new CombatManager(mockResolver.Object, _mockRandom.Object, mockSkills.Object, new Mock<IStatusEngine>().Object);
        var petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, combatManager, _mockRandom.Object, _mockLogger.Object);
        
        pet.Hp = pet.MaxHp;
        pet.IsDead = false;
        combatManager.RegisterCombatant(pet);
        
        var attacker = new ServerCharacter { Id = 999, Team = Team.Enemy };
        combatManager.RegisterCombatant(attacker);
        
        combatManager.UseSkill(new UseSkillRequest { PlayerId = 999, TargetId = pet.Id, SkillId = 0 });
        
        Assert.True(pet.IsDead);
        Assert.Equal(94, pet.Amity); // 95 -> 94

        // 5. Riding System (PET-07)
        pet.Revive();
        character.SetActivePet(pet.InstanceId);
        
        petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);
        Assert.True(character.IsMounted);
        Assert.Equal(1.5f, character.MoveSpeedModifier); // 1.0 + 0.5

        petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount); // Toggle off
        Assert.False(character.IsMounted);
        Assert.Equal(1.0f, character.MoveSpeedModifier);
    }

    public void Dispose()
    {
        if (File.Exists(TestPetsFile)) File.Delete(TestPetsFile);
    }
}
