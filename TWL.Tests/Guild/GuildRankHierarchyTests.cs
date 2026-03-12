using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Guilds;

namespace TWL.Tests.Guild;

public class GuildRankHierarchyTests
{
    private readonly GuildPermissionService _service;

    public GuildRankHierarchyTests()
    {
        _service = new GuildPermissionService();
    }

    [Fact]
    public void Leader_CanPromote_UpToOfficer()
    {
        Assert.True(_service.CanPromoteDemote(GuildRank.Leader, GuildRank.Recruit, GuildRank.Member));
        Assert.True(_service.CanPromoteDemote(GuildRank.Leader, GuildRank.Member, GuildRank.Officer));

        // Cannot promote to Leader
        Assert.False(_service.CanPromoteDemote(GuildRank.Leader, GuildRank.Officer, GuildRank.Leader));
    }

    [Fact]
    public void Officer_CanPromote_UpToMember()
    {
        Assert.True(_service.CanPromoteDemote(GuildRank.Officer, GuildRank.Recruit, GuildRank.Member));

        // Officer cannot promote Member to Officer
        Assert.False(_service.CanPromoteDemote(GuildRank.Officer, GuildRank.Member, GuildRank.Officer));
    }

    [Fact]
    public void Demote_FailsIfTargetIsEqualOrHigherRank()
    {
        Assert.False(_service.CanPromoteDemote(GuildRank.Officer, GuildRank.Officer, GuildRank.Member));
        Assert.False(_service.CanPromoteDemote(GuildRank.Officer, GuildRank.Leader, GuildRank.Officer));
    }
}
