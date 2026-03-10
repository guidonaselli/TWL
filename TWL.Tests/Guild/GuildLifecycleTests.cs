using Moq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Architecture.Observability;
using TWL.Server.Persistence;
using Xunit;
using System.Collections.Generic;

namespace TWL.Tests.GuildTests;

public class GuildLifecycleTests
{
    private readonly Mock<PlayerService> _mockPlayerService;
    private readonly GuildManager _guildManager;

    public GuildLifecycleTests()
    {
        var mockRepo = new Mock<IPlayerRepository>();
        var mockMetrics = new ServerMetrics();
        _mockPlayerService = new Mock<PlayerService>(mockRepo.Object, mockMetrics);
        _guildManager = new GuildManager(_mockPlayerService.Object);
    }

    private void SetupPlayer(int userId, string name, int gold)
    {
        var character = new TWL.Server.Simulation.Networking.ServerCharacter { Name = name, Gold = gold };
        var mockSession = new Mock<TWL.Server.Simulation.Networking.ClientSession>();
        mockSession.SetupGet(s => s.Character).Returns(character);
        mockSession.Object.UserId = userId;
        _mockPlayerService.Setup(ps => ps.GetSession(userId)).Returns(mockSession.Object);
        _mockPlayerService.Setup(ps => ps.GetSessionByName(name)).Returns(mockSession.Object);
    }

    [Fact]
    public void CreateGuild_Success_DeductsFee()
    {
        SetupPlayer(1, "LeaderOne", GuildManager.GuildCreationFee);

        var result = _guildManager.CreateGuild(1, "LeaderOne", "CoolGuild");

        Assert.True(result.Success);
        Assert.True(result.GuildId > 0);

        var guild = _guildManager.GetGuild(result.GuildId);
        Assert.NotNull(guild);
        Assert.Equal("CoolGuild", guild.Name);
        Assert.Equal(1, guild.LeaderId);
        Assert.Contains(1, guild.MemberIds);
    }

    [Fact]
    public void CreateGuild_InsufficientFunds_Fails()
    {
        SetupPlayer(1, "PoorPlayer", 100);

        var result = _guildManager.CreateGuild(1, "PoorPlayer", "PoorGuild");

        Assert.False(result.Success);
        Assert.Contains("Insufficient gold", result.Message);
        Assert.Equal(0, result.GuildId);
    }

    [Fact]
    public void CreateGuild_DuplicateName_Fails()
    {
        SetupPlayer(1, "PlayerOne", GuildManager.GuildCreationFee);
        SetupPlayer(2, "PlayerTwo", GuildManager.GuildCreationFee);

        _guildManager.CreateGuild(1, "PlayerOne", "FirstGuild");
        var result = _guildManager.CreateGuild(2, "PlayerTwo", "FIRSTGUILD");

        Assert.False(result.Success);
        Assert.Contains("already taken", result.Message);
    }

    [Fact]
    public void InviteAndAccept_Flow_Works()
    {
        SetupPlayer(1, "Leader", GuildManager.GuildCreationFee);
        SetupPlayer(2, "Target", 0);

        var createResult = _guildManager.CreateGuild(1, "Leader", "TestGuild");

        var inviteResult = _guildManager.InviteMember(1, "Leader", 2, "Target");
        Assert.True(inviteResult.Success);

        var acceptResult = _guildManager.AcceptInvite(2, 1);
        Assert.True(acceptResult.Success);

        var guild = _guildManager.GetGuild(createResult.GuildId);
        Assert.NotNull(guild);
        Assert.Contains(2, guild.MemberIds);

        var targetGuild = _guildManager.GetGuildByMember(2);
        Assert.NotNull(targetGuild);
        Assert.Equal(createResult.GuildId, targetGuild.GuildId);
    }

    [Fact]
    public void InviteAndDecline_Flow_Works()
    {
        SetupPlayer(1, "Leader", GuildManager.GuildCreationFee);
        SetupPlayer(2, "Target", 0);

        _guildManager.CreateGuild(1, "Leader", "TestGuild");
        _guildManager.InviteMember(1, "Leader", 2, "Target");

        var declineResult = _guildManager.DeclineInvite(2, 1);
        Assert.True(declineResult);

        var acceptResult = _guildManager.AcceptInvite(2, 1);
        Assert.False(acceptResult.Success);
    }

    [Fact]
    public void LeaveGuild_Works()
    {
        SetupPlayer(1, "Leader", GuildManager.GuildCreationFee);
        SetupPlayer(2, "Target", 0);

        var createResult = _guildManager.CreateGuild(1, "Leader", "TestGuild");
        _guildManager.InviteMember(1, "Leader", 2, "Target");
        _guildManager.AcceptInvite(2, 1);

        var leaveResult = _guildManager.LeaveGuild(2);
        Assert.True(leaveResult);

        var guild = _guildManager.GetGuild(createResult.GuildId);
        Assert.NotNull(guild);
        Assert.DoesNotContain(2, guild.MemberIds);
    }

    [Fact]
    public void LeaveGuild_AsLeader_ReassignsLeadership()
    {
        SetupPlayer(1, "Leader", GuildManager.GuildCreationFee);
        SetupPlayer(2, "Target", 0);

        var createResult = _guildManager.CreateGuild(1, "Leader", "TestGuild");
        _guildManager.InviteMember(1, "Leader", 2, "Target");
        _guildManager.AcceptInvite(2, 1);

        var leaveResult = _guildManager.LeaveGuild(1);
        Assert.True(leaveResult);

        var guild = _guildManager.GetGuild(createResult.GuildId);
        Assert.NotNull(guild);
        Assert.DoesNotContain(1, guild.MemberIds);
        Assert.Equal(2, guild.LeaderId);
    }

    [Fact]
    public void KickMember_ByLeader_Works()
    {
        SetupPlayer(1, "Leader", GuildManager.GuildCreationFee);
        SetupPlayer(2, "Target", 0);

        var createResult = _guildManager.CreateGuild(1, "Leader", "TestGuild");
        _guildManager.InviteMember(1, "Leader", 2, "Target");
        _guildManager.AcceptInvite(2, 1);

        var kickResult = _guildManager.KickMember(1, 2);
        Assert.True(kickResult.Success);

        var guild = _guildManager.GetGuild(createResult.GuildId);
        Assert.NotNull(guild);
        Assert.DoesNotContain(2, guild.MemberIds);
    }
}
