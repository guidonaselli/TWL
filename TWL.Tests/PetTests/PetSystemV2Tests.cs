using TWL.Shared.Domain.Battle;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Microsoft.Extensions.Logging;

namespace TWL.Tests.PetTests;

public class PetSystemV2Tests : IDisposable
{
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<ILogger<PetService>> _mockLogger;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;

    public PetSystemV2Tests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");
        File.WriteAllText("Content/Data/pets_v2.json", @"
[
  {
    ""PetTypeId"": 2001,
    ""Name"": ""Flag Pet"",
    ""Type"": ""Capture"",
    ""Element"": ""Fire"",
    ""BaseHp"": 100,
    ""CaptureRules"": {
        ""IsCapturable"": true,
        ""LevelLimit"": 1,
        ""BaseChance"": 1.0,
        ""RequiredFlag"": ""QUEST_UNLOCK_PET""
    }
  },
  {
    ""PetTypeId"": 2002,
    ""Name"": ""Delivery Pet"",
    ""Type"": ""Capture"",
    ""Element"": ""Wind"",
    ""BaseHp"": 100,
    ""Utilities"": [
       { ""Type"": ""Delivery"", ""Value"": 1.0, ""RequiredLevel"": 1, ""RequiredAmity"": 0 }
    ]
  },
  {
    ""PetTypeId"": 2003,
    ""Name"": ""Disobedient Pet"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100
  }
]");
        _petManager.Load("Content/Data/pets_v2.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();
        _mockLogger = new Mock<ILogger<PetService>>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object, _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_v2.json"))
        {
            File.Delete("Content/Data/pets_v2.json");
        }
    }

    [Fact]
    public void CombatManager_PetDisobeys_ReturnsIsDisobey()
    {
        // Setup
        var session = new ClientSessionForPetV2Test();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        var petDef = _petManager.GetDefinition(2003);
        var pet = new ServerPet(petDef);
        pet.Id = 100; // Runtime ID
        pet.Amity = 0; // Ensure disobedience chance
        session.Character.AddPet(pet);
        _combatManager.RegisterCombatant(pet);

        var target = new ServerCharacter { Id = 200, Hp = 100 };
        _combatManager.RegisterCombatant(target);

        // Mock RNG for disobedience
        // CheckObedience(roll): return roll > failChance
        // Amity 0 -> failChance ~0.24
        // If roll is 0.1, 0.1 > 0.24 is False -> Disobey
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.1f);

        // Mock Skill
        _mockSkills.Setup(s => s.GetSkillById(1)).Returns(new Skill { SkillId = 1, SpCost = 0 });

        // Act
        var results = _combatManager.UseSkill(new UseSkillRequest { PlayerId = 100, TargetId = 200, SkillId = 1 });

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsDisobey);
        Assert.False(results[0].TargetDied);
    }

    [Fact]
    public void PetService_Capture_RequiresFlag_FailsWithoutFlag()
    {
        // Setup
        var session = new ClientSessionForPetV2Test();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Trainer" });
        _playerService.RegisterSession(session);

        // Define Enemy
        var enemyDef = new MonsterDefinition { MonsterId = 50, PetTypeId = 2001, IsCapturable = true, CaptureThreshold = 1.0f };
        _mockMonsterManager.Setup(m => m.GetDefinition(50)).Returns(enemyDef);

        var enemy = new ServerCharacter { Id = 50, MonsterId = 50, Hp = 10, Team = Team.Enemy };
        _combatManager.RegisterCombatant(enemy);

        // Act - Without Flag
        // Flag "QUEST_UNLOCK_PET" is missing
        var result = _petService.CaptureEnemy(1, 50);

        // Assert
        Assert.Null(result);

        // Act - With Flag
        session.QuestComponent.Flags.Add("QUEST_UNLOCK_PET");
        _mockRandom.Setup(r => r.NextFloat()).Returns(0.0f); // Success

        var resultSuccess = _petService.CaptureEnemy(1, 50);
        Assert.NotNull(resultSuccess);
    }

    [Fact]
    public void PetService_Delivery_MovesToBank()
    {
        // Setup
        var session = new ClientSessionForPetV2Test();
        var chara = new ServerCharacter { Id = 1, Name = "Trainer" };
        session.SetCharacter(chara);
        _playerService.RegisterSession(session);

        // Add Item to Inventory
        chara.AddItem(101, 10); // 10 Potions
        Assert.Single(chara.Inventory);
        Assert.Empty(chara.Bank);

        var petDef = _petManager.GetDefinition(2002);
        var pet = new ServerPet(petDef);
        chara.AddPet(pet);

        // Act
        // Use Delivery on Slot 0
        var success = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Delivery, "0");

        // Assert
        Assert.True(success);
        Assert.Empty(chara.Inventory); // Moved all
        Assert.Single(chara.Bank);
        Assert.Equal(101, chara.Bank[0].ItemId);
        Assert.Equal(10, chara.Bank[0].Quantity);
    }
}

// Helper to expose QuestComponent and Character setting
public class ClientSessionForPetV2Test : ClientSession
{
    public ClientSessionForPetV2Test()
    {
        UserId = 1;
        // Use protected constructor
        // Manually initialize QuestComponent
        var pm = new PetManager();
        var qm = new ServerQuestManager();
        QuestComponent = new PlayerQuestComponent(qm, pm);
    }

    public void SetCharacter(ServerCharacter c) => Character = c;
}
