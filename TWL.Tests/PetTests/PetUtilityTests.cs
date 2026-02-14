using TWL.Shared.Domain.Battle;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Tests.PetTests;

public class PetUtilityTests : IDisposable
{
    private readonly CombatManager _combatManager;
    private readonly ServerMetrics _metrics;
    private readonly Mock<IRandomService> _mockRandom;
    private readonly Mock<IPlayerRepository> _mockRepo;
    private readonly Mock<ICombatResolver> _mockResolver;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IStatusEngine> _mockStatusEngine;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly PetManager _petManager;
    private readonly PetService _petService;
    private readonly PlayerService _playerService;

    public PetUtilityTests()
    {
        _mockRepo = new Mock<IPlayerRepository>();
        _metrics = new ServerMetrics();
        _playerService = new PlayerService(_mockRepo.Object, _metrics);

        _petManager = new PetManager();
        Directory.CreateDirectory("Content/Data");

        // Define pets with utilities
        var json = @"
[
  {
    ""PetTypeId"": 1001,
    ""Name"": ""Horse"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 5,
    ""BaseWis"": 5,
    ""BaseAgi"": 10,
    ""Utilities"": [
        { ""Type"": ""Mount"", ""Value"": 0.5, ""RequiredLevel"": 1, ""RequiredAmity"": 0 }
    ]
  },
  {
    ""PetTypeId"": 1002,
    ""Name"": ""Gatherer"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 5,
    ""BaseWis"": 5,
    ""BaseAgi"": 10,
    ""Utilities"": [
        { ""Type"": ""Gathering"", ""Value"": 0.2, ""RequiredLevel"": 1, ""RequiredAmity"": 0 }
    ]
  },
  {
     ""PetTypeId"": 1003,
     ""Name"": ""Crafter"",
     ""Type"": ""Capture"",
     ""Element"": ""Fire"",
     ""BaseHp"": 100,
     ""BaseStr"": 10,
     ""BaseCon"": 10,
     ""BaseInt"": 5,
     ""BaseWis"": 5,
     ""BaseAgi"": 10,
     ""Utilities"": [
         { ""Type"": ""CraftingAssist"", ""Value"": 10.0, ""RequiredLevel"": 1, ""RequiredAmity"": 0 }
     ]
   }
]";
        File.WriteAllText("Content/Data/pets_utility.json", json);
        _petManager.Load("Content/Data/pets_utility.json");

        _mockStatusEngine = new Mock<IStatusEngine>();
        _mockResolver = new Mock<ICombatResolver>();
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _mockMonsterManager = new Mock<MonsterManager>();

        _combatManager = new CombatManager(_mockResolver.Object, _mockRandom.Object, _mockSkills.Object,
            _mockStatusEngine.Object);
        _petService = new PetService(_playerService, _petManager, _mockMonsterManager.Object, _combatManager, _mockRandom.Object, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<PetService>>().Object);
    }

    public void Dispose()
    {
        if (File.Exists("Content/Data/pets_utility.json"))
        {
            File.Delete("Content/Data/pets_utility.json");
        }
        if (File.Exists("Content/Data/pets_utility_req.json"))
        {
            File.Delete("Content/Data/pets_utility_req.json");
        }
    }

    private ClientSessionForTest SetupSession()
    {
        var session = new ClientSessionForTest();
        session.SetCharacter(new ServerCharacter { Id = 1, Name = "Player" });
        _playerService.RegisterSession(session);
        return session;
    }

    [Fact]
    public void UseUtility_Mount_TogglesSpeed()
    {
        var session = SetupSession();
        var def = _petManager.GetDefinition(1001); // Horse
        var pet = new ServerPet(def);
        session.Character.AddPet(pet);

        // 1. Mount
        var result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);

        Assert.True(result);
        Assert.True(session.Character.IsMounted);
        Assert.Equal(1.5f, session.Character.MoveSpeedModifier);

        // 2. Dismount (Toggle)
        result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);

        Assert.True(result);
        Assert.False(session.Character.IsMounted);
        Assert.Equal(1.0f, session.Character.MoveSpeedModifier);
    }

    [Fact]
    public void UseUtility_Gathering_TogglesBonus()
    {
        var session = SetupSession();
        var def = _petManager.GetDefinition(1002); // Gatherer
        var pet = new ServerPet(def);
        session.Character.AddPet(pet);

        // 1. Enable
        var result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Gathering);

        Assert.True(result);
        Assert.Equal(0.2f, session.Character.GatheringBonus);

        // 2. Disable
        result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Gathering);

        Assert.True(result);
        Assert.Equal(0.0f, session.Character.GatheringBonus);
    }

    [Fact]
    public void UseUtility_CraftingAssist_TogglesBonus()
    {
        var session = SetupSession();
        var def = _petManager.GetDefinition(1003); // Crafter
        var pet = new ServerPet(def);
        session.Character.AddPet(pet);

        // 1. Enable
        var result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.CraftingAssist);

        Assert.True(result);
        Assert.Equal(10.0f, session.Character.CraftingAssistBonus);

        // 2. Disable
        result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.CraftingAssist);

        Assert.True(result);
        Assert.Equal(0.0f, session.Character.CraftingAssistBonus);
    }

    [Fact]
    public void UseUtility_FailsIfRequirementsNotMet()
    {
        // Define high requirement pet manually or modify setup
         var json = @"
[
  {
    ""PetTypeId"": 9000,
    ""Name"": ""Elite Horse"",
    ""Type"": ""Capture"",
    ""Element"": ""Earth"",
    ""BaseHp"": 100,
    ""BaseStr"": 10,
    ""BaseCon"": 10,
    ""BaseInt"": 5,
    ""BaseWis"": 5,
    ""BaseAgi"": 10,
    ""Utilities"": [
        { ""Type"": ""Mount"", ""Value"": 0.8, ""RequiredLevel"": 50, ""RequiredAmity"": 0 }
    ]
  }
]";
        File.WriteAllText("Content/Data/pets_utility_req.json", json);
        _petManager.Load("Content/Data/pets_utility_req.json");

        var session = SetupSession();
        var def = _petManager.GetDefinition(9000);
        var pet = new ServerPet(def); // Level 1
        session.Character.AddPet(pet);

        // Act
        var result = _petService.UseUtility(1, pet.InstanceId, PetUtilityType.Mount);

        // Assert
        Assert.False(result);
        Assert.False(session.Character.IsMounted);
    }
}