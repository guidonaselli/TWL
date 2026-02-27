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
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Party;

public class PartyProximityRulesTests
{
    private readonly Mock<IPartyService> _mockPartyService;
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly Mock<MonsterManager> _mockMonsterManager;
    private readonly Mock<IRandomService> _mockRandomService;
    private readonly PartyRewardDistributor _distributor;

    public PartyProximityRulesTests()
    {
        _mockPartyService = new Mock<IPartyService>();

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

    private void SetupMonster(int monsterId, int xp)
    {
        var def = new MonsterDefinition
        {
            MonsterId = monsterId,
            ExpReward = xp
        };
        _mockMonsterManager.Setup(m => m.GetDefinition(monsterId)).Returns(def);
    }

    [Fact]
    public void Distribute_FiltersOut_DifferentMap()
    {
        var p1 = CreateCharacter(1, 1, 10, 10); // Close to mob
        var p2 = CreateCharacter(2, 2, 10, 10); // Diff map
        var monster = new ServerCharacter { Id = 999, MonsterId = 10, MapId = 1, X = 10, Y = 10, Hp = 0 };

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = 1,
            MemberIds = new List<int> { 1, 2 },
            LeaderId = 1
        };

        SetupMonster(10, 50); // Avoid level up
        _mockPartyService.Setup(p => p.GetPartyByMember(It.IsAny<int>())).Returns(party);

        _distributor.DistributeKillRewards(p1, monster);

        Assert.Equal(50, p1.Exp); // Solo share effectively (50 / 1)
        Assert.Equal(0, p2.Exp);
    }

    [Fact]
    public void Distribute_FiltersOut_TooFar()
    {
        var p1 = CreateCharacter(1, 1, 10, 10); // Close
        var p2 = CreateCharacter(2, 1, 50, 10); // Far (> 30)
        var monster = new ServerCharacter { Id = 999, MonsterId = 10, MapId = 1, X = 10, Y = 10, Hp = 0 };

        var party = new TWL.Server.Simulation.Managers.Party
        {
            PartyId = 1,
            MemberIds = new List<int> { 1, 2 },
            LeaderId = 1
        };

        SetupMonster(10, 50); // Avoid level up
        _mockPartyService.Setup(p => p.GetPartyByMember(It.IsAny<int>())).Returns(party);

        _distributor.DistributeKillRewards(p1, monster);

        Assert.Equal(50, p1.Exp);
        Assert.Equal(0, p2.Exp);
    }

    [Fact]
    public void Distribute_Includes_JustInEdge()
    {
        var p1 = CreateCharacter(1, 1, 10, 10);
        var p2 = CreateCharacter(2, 1, 40, 10); // Distance 30 exactly
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
}
