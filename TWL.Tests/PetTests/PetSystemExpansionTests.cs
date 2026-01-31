using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

public class PetSystemExpansionTests : IDisposable
{
    private Mock<IPlayerRepository> _mockRepo;
    private Mock<IStatusEngine> _mockStatusEngine;
    private Mock<ICombatResolver> _mockResolver;
    private Mock<ISkillCatalog> _mockSkills;
    private Mock<IRandomService> _mockRandom;

    private PlayerService _playerService;
    private PetManager _petManager;
    private CombatManager _combatManager;
    private PetService _petService;
    private ServerMetrics _metrics;

    public PetSystemExpansionTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        System.IO.Directory.CreateDirectory("Content/Data");
        System.IO.File.WriteAllText("Content/Data/pets_expansion_test.json", @"
[
  {
    ""PetTypeId"": 1001,
    ""Name"": ""Slime"",
    ""Type"": ""Capture"",
    ""BaseHp"": 100,
    ""GrowthModel"": { ""HpGrowthPerLevel"": 10 },
    ""SkillSet"": [],
    ""Utilities"": [
       { ""Type"": ""Mount"", ""Value"": 1.2, ""RequiredLevel"": 1, ""RequiredAmity"": 10 }
    ],
    ""RebirthEligible"": true,
    ""RebirthSkillId"": 999
  }
]");
        _petManager.Load("Content/Data/pets_expansion_test.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object, _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _combatManager, _mockRandom.Object);
    }

    public void Dispose()
    {
        if (System.IO.File.Exists("Content/Data/pets_expansion_test.json"))
            System.IO.File.Delete("Content/Data/pets_expansion_test.json");
    }

    [Fact]
    public void PetDeath_ReducesAmity()
    {
        // Setup
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        var def = _petManager.GetDefinition(1001);
        var pet = new ServerPet(def);
        pet.Amity = 50;
        session.Character.AddPet(pet);

        // Register combatants
        _combatManager.RegisterCombatant(pet);
        var attacker = new ServerCharacter { Id = 2 };
        _combatManager.RegisterCombatant(attacker);

        _mockSkills.Setup(s => s.GetSkillById(1)).Returns(new TWL.Shared.Domain.Skills.Skill { SkillId = 1, SpCost = 0, Effects = new List<TWL.Shared.Domain.Skills.SkillEffect>() });

        // Mock Damage to kill (Hp is around 100, so 200 damage is safe kill)
        _mockResolver.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>()))
            .Returns(200);

        // Act
        var result = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 2, TargetId = pet.Id, SkillId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, pet.Hp);

        // PetService should have subscribed and handled death
        // Note: ServerPet.Die() is called manually in PetService.HandlePetDeath if not called by CombatManager.
        // Since CombatManager fires event, PetService calls pet.Die(), which reduces Amity.

        Assert.True(pet.IsDead);
        Assert.Equal(40, pet.Amity); // 50 - 10
    }

    [Fact]
    public void Rebirth_ResetsLevel_BoostsStats()
    {
         var def = _petManager.GetDefinition(1001);
         var pet = new ServerPet(def);
         pet.Level = 100;
         pet.RecalculateStats();

         // Act
         bool success = pet.TryRebirth();

         // Assert
         Assert.True(success);
         Assert.Equal(1, pet.Level);
         Assert.True(pet.HasRebirthed);

         // Check stat boost: Level 1 Rebirth vs Level 1 Normal
         var normalPet = new ServerPet(def); // Level 1 default

         // Normal HP: BaseHp (100) + Growth(0)
         // Rebirth HP: (BaseHp + Growth) * 1.1
         Assert.True(pet.MaxHp > normalPet.MaxHp);
    }

    [Fact]
    public void Utility_Usage_Check()
    {
         var session = new ClientSessionForTest();
         session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
         _playerService.RegisterSession(session);

         var def = _petManager.GetDefinition(1001);
         var pet = new ServerPet(def);
         pet.Amity = 5; // Too low (req 10)
         session.Character.AddPet(pet);

         // Act - Fail
         bool resultLowAmity = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);
         Assert.False(resultLowAmity);

         // Act - Success
         pet.Amity = 20;
         bool resultSuccess = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);
         Assert.True(resultSuccess);
    }
}
