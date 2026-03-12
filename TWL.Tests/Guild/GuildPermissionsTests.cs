using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Guilds;

namespace TWL.Tests.Guild;

public class GuildPermissionsTests
{
    private readonly GuildPermissionService _service;

    public GuildPermissionsTests()
    {
        _service = new GuildPermissionService();
    }

    [Fact]
    public void Leader_HasAllPermissions()
    {
        Assert.True(_service.HasPermission(GuildRank.Leader, GuildPermissions.Invite));
        Assert.True(_service.HasPermission(GuildRank.Leader, GuildPermissions.Kick));
        Assert.True(_service.HasPermission(GuildRank.Leader, GuildPermissions.Promote));
        Assert.True(_service.HasPermission(GuildRank.Leader, GuildPermissions.Demote));
        Assert.True(_service.HasPermission(GuildRank.Leader, GuildPermissions.WithdrawStorage));
    }

    [Fact]
    public void Officer_HasExpectedPermissions()
    {
        Assert.True(_service.HasPermission(GuildRank.Officer, GuildPermissions.Invite));
        Assert.True(_service.HasPermission(GuildRank.Officer, GuildPermissions.Kick));
        Assert.True(_service.HasPermission(GuildRank.Officer, GuildPermissions.Promote));
        Assert.True(_service.HasPermission(GuildRank.Officer, GuildPermissions.Demote));
        Assert.True(_service.HasPermission(GuildRank.Officer, GuildPermissions.WithdrawStorage));
    }

    [Fact]
    public void Member_HasNoPrivilegedPermissions()
    {
        Assert.False(_service.HasPermission(GuildRank.Member, GuildPermissions.Invite));
        Assert.False(_service.HasPermission(GuildRank.Member, GuildPermissions.Kick));
        Assert.False(_service.HasPermission(GuildRank.Member, GuildPermissions.WithdrawStorage));
    }

    [Fact]
    public void Recruit_HasNoPermissions()
    {
        Assert.False(_service.HasPermission(GuildRank.Recruit, GuildPermissions.Invite));
        Assert.False(_service.HasPermission(GuildRank.Recruit, GuildPermissions.Kick));
    }

    [Fact]
    public void Leader_CanKickAnyoneBelowThem()
    {
        Assert.True(_service.CanKick(GuildRank.Leader, GuildRank.Officer));
        Assert.True(_service.CanKick(GuildRank.Leader, GuildRank.Member));
        Assert.True(_service.CanKick(GuildRank.Leader, GuildRank.Recruit));
        Assert.False(_service.CanKick(GuildRank.Leader, GuildRank.Leader));
    }

    [Fact]
    public void Officer_CanKickMemberAndRecruit_ButNotLeaderOrOfficer()
    {
        Assert.True(_service.CanKick(GuildRank.Officer, GuildRank.Member));
        Assert.True(_service.CanKick(GuildRank.Officer, GuildRank.Recruit));

        Assert.False(_service.CanKick(GuildRank.Officer, GuildRank.Officer));
        Assert.False(_service.CanKick(GuildRank.Officer, GuildRank.Leader));
    }
}
