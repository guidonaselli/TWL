using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Repositories;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Party;

public class TestableClientSession : ClientSession
{
    public TestableClientSession()
    {
    }

    public void SetCharacter(ServerCharacter character)
    {
        Character = character;
    }

    public override Task SendAsync(NetMessage msg)
    {
        return Task.CompletedTask;
    }
}

public class PartyRewardDistributionTests
{
    private readonly Mock<IPartyService> _mockPartyService;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<IRandomService> _mockRandomService;
    private readonly PartyRewardDistributor _distributor;

    public PartyRewardDistributionTests()
    {
        _mockPartyService = new Mock<IPartyService>();

        // Mock PlayerService dependencies
        var mockRepo = new Mock<IPlayerRepository>();
        var metrics = new ServerMetrics();
        _mockPlayerService = new Mock<PlayerService>(mockRepo.Object, metrics);

        _mockMonsterManager = new Mock<MonsterManager>();
        _mockRandomService = new Mock<IRandomService>();

        _distributor = new PartyRewardDistributor(
            _mockPartyService.Object,
            _mockPlayerService.Object,
            _mockMonsterManager.Object,
            _mockRandomService.Object
        );
    }

    private ServerCharacter CreateCharacter(int id, int mapId, float x, float y)
    {
        var c = new ServerCharacter
        {
            Id = id,
            Name = $"Char{id}",
            MapId = mapId,
            X = x,
            Y = y
        };

        var session = new TestableClientSession();
        session.SetCharacter(c);
        session.UserId = id;

        _mockPlayerService.Setup(ps => ps.GetSession(id)).Returns(session);
        return c;
    }

    private void SetupMonster(int monsterId, int xp, List<DropItem> drops = null)
    {
        var def = new MonsterDefinition
        {
            MonsterId = monsterId,
            ExpReward = xp,
            Drops = drops ?? new List<DropItem>()
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(monsterId)).Returns(def);
    }

    [Fact]
    public void Distribute_Solo_GivesFullReward()
    {
        var player = CreateCharacter(1, 1, 10, 10);
        var monster = new ServerCharacter { Id = 999, MonsterId = 10, MapId = 1, X = 10, Y = 10, Hp = 0 };

        SetupMonster(10, 50); // Reduce XP to avoid level up (100 triggers level up resetting Exp to 0)
        _mockPartyService.Setup(p => p.GetPartyByMember(1)).Returns((TWL.Server.Simulation.Managers.Party)null);

        _distributor.DistributeKillRewards(player, monster);

        Assert.Equal(50, player.Exp);
    }

    [Fact]
    public void Distribute_Party_SplitsXp_Evenly()
    {
        var p1 = CreateCharacter(1, 1, 10, 10);
        var p2 = CreateCharacter(2, 1, 12, 10);
        var monster = new ServerCharacter { Id = 999, MonsterId = 10, MapId = 1, X = 10, Y = 10, Hp = 0 };

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = 1,
            MemberIds = new List<int> { 1, 2 },
            LeaderId = 1
        };

        SetupMonster(10, 100);
        _mockPartyService.Setup(p => p.GetPartyByMember(It.IsAny<int>())).Returns(party);

        _distributor.DistributeKillRewards(p1, monster);

        Assert.Equal(50, p1.Exp);
        Assert.Equal(50, p2.Exp);
    }

    [Fact]
    public void Distribute_Party_LootRoundRobin()
    {
        var p1 = CreateCharacter(1, 1, 10, 10);
        var p2 = CreateCharacter(2, 1, 10, 10);
        var monster = new ServerCharacter { Id = 999, MonsterId = 10, MapId = 1, X = 10, Y = 10, Hp = 0 };

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = 1,
            MemberIds = new List<int> { 1, 2 },
            LeaderId = 1,
            NextLootMemberIndex = 0
        };

        var drops = new List<DropItem>
        {
            new DropItem { ItemId = 100, Chance = 1.0, MinQuantity = 1, MaxQuantity = 1 },
            new DropItem { ItemId = 101, Chance = 1.0, MinQuantity = 1, MaxQuantity = 1 }
        };

        SetupMonster(10, 0, drops); // 0 XP to focus on loot
        _mockPartyService.Setup(p => p.GetPartyByMember(It.IsAny<int>())).Returns(party);

        // Mock random to always hit chance and quantity 1
        _mockRandomService.Setup(r => r.NextDouble()).Returns(0.0);
        _mockRandomService.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(1);

        // First Kill (or first item drop)
        _distributor.DistributeKillRewards(p1, monster);

        // Should distribute 2 items.
        // Item 1 -> Member[0] (p1)
        // Item 2 -> Member[1] (p2)
        // Order of drops matters. Assume iteration order.

        // Check inventories
        Assert.True(p1.HasItem(100, 1) || p1.HasItem(101, 1));
        Assert.True(p2.HasItem(100, 1) || p2.HasItem(101, 1));

        // Ensure they didn't get duplicates (assuming distinct items)
        Assert.NotEqual(p1.Inventory[0].ItemId, p2.Inventory[0].ItemId);

        // Next index should have rotated twice (0 -> 1 -> 0)
        Assert.Equal(0, party.NextLootMemberIndex);
    }
}
