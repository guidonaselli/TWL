using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Services.World;
using TWL.Server.Services.World.Handlers;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Tests.Server.Services.World.Handlers;

public class GenericTriggerHandlerTests
{
    private readonly Mock<PlayerService> _playerServiceMock;
    private readonly Mock<SpawnManager> _spawnManagerMock;
    private readonly Mock<MonsterManager> _monsterManagerMock;
    private readonly Mock<CombatManager> _combatManagerMock;
    private readonly Mock<InstanceService> _instanceServiceMock;
    private readonly GenericTriggerHandler _handler;

    public GenericTriggerHandlerTests()
    {
        var metrics = new ServerMetrics();
        var repo = new Mock<IPlayerRepository>();
        _playerServiceMock = new Mock<PlayerService>(repo.Object, metrics);

        _monsterManagerMock = new Mock<MonsterManager>();

        var resolver = new Mock<ICombatResolver>();
        var random = new Mock<IRandomService>();
        var skills = new Mock<ISkillCatalog>();
        var status = new Mock<IStatusEngine>();
        _combatManagerMock = new Mock<CombatManager>(resolver.Object, random.Object, skills.Object, status.Object);

        _spawnManagerMock = new Mock<SpawnManager>(_monsterManagerMock.Object, _combatManagerMock.Object, random.Object, _playerServiceMock.Object);

        _instanceServiceMock = new Mock<InstanceService>(metrics);

        _handler = new GenericTriggerHandler(_playerServiceMock.Object, _spawnManagerMock.Object, _instanceServiceMock.Object);
    }

    [Fact]
    public void CanHandle_ReturnsTrue_ForSupportedTypes()
    {
        Assert.True(_handler.CanHandle("Generic"));
        Assert.True(_handler.CanHandle("Script"));
        Assert.True(_handler.CanHandle("Event"));
    }

    [Fact]
    public void ExecuteEnter_Teleport_UpdatesCharacterPosition()
    {
        var character = new ServerCharacter { MapId = 1, X = 0, Y = 0 };
        var mapChangedCalled = false;
        character.OnMapChanged += (id) => mapChangedCalled = true;

        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("Teleport", new Dictionary<string, string>
        {
            { "MapId", "2" },
            { "X", "100" },
            { "Y", "200" }
        }));

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        Assert.Equal(2, character.MapId);
        Assert.Equal(100, character.X);
        Assert.Equal(200, character.Y);
        Assert.True(mapChangedCalled);
    }

    [Fact]
    public void ExecuteEnter_Teleport_SameMap_InvokesMapChanged()
    {
        var character = new ServerCharacter { MapId = 1, X = 0, Y = 0 };
        var mapChangedCalled = false;
        character.OnMapChanged += (id) => mapChangedCalled = true;

        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("Teleport", new Dictionary<string, string>
        {
            { "MapId", "1" }, // Same map
            { "X", "50" },
            { "Y", "50" }
        }));

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        Assert.Equal(1, character.MapId);
        Assert.Equal(50, character.X);
        Assert.Equal(50, character.Y);
        Assert.True(mapChangedCalled, "Should invoke OnMapChanged even for same map ID");
    }

    [Fact]
    public void ExecuteEnter_Heal_UpdatesCharacterHp()
    {
        var character = new ServerCharacter { Hp = 50, Con = 10 };
        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("Heal", new Dictionary<string, string>
        {
            { "Amount", "20" }
        }));

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        Assert.Equal(70, character.Hp);
    }

    [Fact]
    public void ExecuteEnter_Damage_UpdatesCharacterHp()
    {
        var character = new ServerCharacter { Hp = 50, Con = 10 };
        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("Damage", new Dictionary<string, string>
        {
            { "Amount", "20" }
        }));

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        Assert.Equal(30, character.Hp);
    }

    [Fact]
    public void ExecuteEnter_GiveItem_AddsItem()
    {
        var character = new ServerCharacter(); // Assuming simple inventory
        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("GiveItem", new Dictionary<string, string>
        {
            { "ItemId", "101" },
            { "Count", "5" }
        }));

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        Assert.Contains(character.Inventory, i => i.ItemId == 101 && i.Quantity == 5);
    }

    [Fact]
    public void ExecuteEnter_Spawn_CallsStartScriptedEncounter()
    {
        var character = new ServerCharacter { Id = 123 };
        var trigger = new ServerTrigger();
        trigger.Actions.Add(new TriggerAction("Spawn", new Dictionary<string, string>
        {
            { "MonsterId", "999" },
            { "Count", "2" }
        }));

        var sessionMock = new Mock<ClientSession>();
        _playerServiceMock.Setup(s => s.GetSession(123)).Returns(sessionMock.Object);

        _handler.ExecuteEnter(character, trigger, Mock.Of<IWorldTriggerService>());

        _spawnManagerMock.Verify(s => s.StartScriptedEncounter(sessionMock.Object, 999, 2), Times.Once);
    }
}
