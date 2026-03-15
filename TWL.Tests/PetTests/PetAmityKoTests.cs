using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;
using Xunit;
using TWL.Shared.Domain.Skills;
using Microsoft.Extensions.Logging;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Battle;

namespace TWL.Tests.PetTests;

public class PetAmityKoTests
{
    private readonly PetService _petService;
    private readonly PetManager _petManager;
    private readonly CombatManager _combatManager;
    private readonly PlayerService _playerService;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<ILogger<PetService>> _mockLogger;

    public PetAmityKoTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _playerService = new PlayerService(_mockRepo.Object, new ServerMetrics());
        _petManager = new PetManager();
        
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_ko_test.json", @"
[
  {
    ""PetTypeId"": 1,
    ""Name"": ""Ko Pet"",
    ""Type"": ""Quest"",
    ""IsQuestPet"": true,
    ""Element"": ""Fire"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 10,
    ""BaseWis"": 10,
    ""BaseAgi"": 10,
    ""GrowthModel"": { ""HpGrowthPerLevel"": 10, ""SpGrowthPerLevel"": 5 }
  }
]");
        _petManager.Load("Content/Data/pets_ko_test.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();
        _mockLogger = new Mock<ILogger<PetService>>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object, _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, _mockLogger.Object);
    }

    [Fact]
    public void PetAmity_DecreasesByExactlyOne_OnDeath()
    {
        // Setup session and pet
        var chara = new ServerCharacter { Id = 1, Name = "Owner" };
        var session = new ClientSessionForTest { Character = chara };
        _playerService.RegisterSession(session);

        var petId = _petService.CreatePet(1, 1);
        var pet = chara.Pets[0];
        pet.Amity = 50;
        _combatManager.RegisterCombatant(pet);

        // Simulate death event
        // We can manually call the handler if we want to be surgical, 
        // but PetService subscribes to CombatManager.OnCombatantDeath in its constructor.
        
        _mockResolver.Setup(r => r.CalculateDamage(It.IsAny<ServerCombatant>(), It.IsAny<ServerCombatant>(), It.IsAny<UseSkillRequest>()))
            .Returns(9999);
        
        _mockSkills.Setup(s => s.GetSkillById(1)).Returns(new Skill { SkillId = 1 });

        var enemy = new ServerCharacter { Id = 2, Team = Team.Enemy };
        _combatManager.RegisterCombatant(enemy);

        // Act
        _combatManager.UseSkill(new UseSkillRequest { PlayerId = 2, TargetId = pet.Id, SkillId = 1 });

        // Assert
        Assert.True(pet.IsDead);
        Assert.Equal(49, pet.Amity);
    }

    private class ClientSessionForTest : ClientSession
    {
        public ClientSessionForTest() { UserId = 1; }
        public new ServerCharacter Character { get => base.Character; set => base.Character = value; }
    }
}
