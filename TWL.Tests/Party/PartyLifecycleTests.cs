using TWL.Server.Simulation.Managers;
using Xunit;

namespace TWL.Tests.Party;

public class PartyLifecycleTests
{
    private readonly PartyManager _partyManager;

    public PartyLifecycleTests()
    {
        _partyManager = new PartyManager();
    }

    [Fact]
    public void Should_CreateParty_On_FirstAccept()
    {
        // Act
        var inviteResult = _partyManager.InviteMember(1, "Player1", 2, "Player2");
        Assert.True(inviteResult.Success);

        var acceptResult = _partyManager.AcceptInvite(2, 1);
        Assert.True(acceptResult.Success);

        // Assert
        var party = _partyManager.GetPartyByMember(1);
        Assert.NotNull(party);
        Assert.Equal(1, party.LeaderId);
        Assert.Contains(1, party.MemberIds);
        Assert.Contains(2, party.MemberIds);
        Assert.Equal(2, party.MemberIds.Count);

        // Ensure character 2 correctly points to the same party
        var party2 = _partyManager.GetPartyByMember(2);
        Assert.Same(party, party2);
    }

    [Fact]
    public void Should_FailInvite_When_TargetInParty()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        // Target already in party
        var inviteResult = _partyManager.InviteMember(3, "P3", 2, "P2");
        Assert.False(inviteResult.Success);
        Assert.Contains("already in a party", inviteResult.Message);
    }

    [Fact]
    public void Should_FailAccept_When_TargetInParty()
    {
        // 1 invites 2
        _partyManager.InviteMember(1, "P1", 2, "P2");

        // 3 invites 2
        _partyManager.InviteMember(3, "P3", 2, "P2");

        // 2 accepts 1
        var accept1 = _partyManager.AcceptInvite(2, 1);
        Assert.True(accept1.Success);

        // 2 tries to accept 3
        var accept2 = _partyManager.AcceptInvite(2, 3);
        Assert.False(accept2.Success);
        Assert.Contains("already in a party", accept2.Message);
    }

    [Fact]
    public void Should_Enforce_MaxPartySize()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        _partyManager.InviteMember(1, "P1", 3, "P3");
        _partyManager.AcceptInvite(3, 1);

        _partyManager.InviteMember(1, "P1", 4, "P4");
        _partyManager.AcceptInvite(4, 1);

        // Party is full (4 max members)
        var result = _partyManager.InviteMember(1, "P1", 5, "P5");
        Assert.False(result.Success);
        Assert.Contains("full", result.Message.ToLower());
    }

    [Fact]
    public void Should_DeclineInvite()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        var declined = _partyManager.DeclineInvite(2, 1);
        Assert.True(declined);

        // Try accepting afterwards
        var acceptResult = _partyManager.AcceptInvite(2, 1);
        Assert.False(acceptResult.Success);
    }

    [Fact]
    public void Should_OnlyAllowLeader_ToInviteAndKick()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        // Member tries to invite
        var inviteResult = _partyManager.InviteMember(2, "P2", 3, "P3");
        Assert.False(inviteResult.Success);
        Assert.Contains("leader", inviteResult.Message.ToLower());

        // Member tries to kick
        var kickResult = _partyManager.KickMember(2, 1, false, false);
        Assert.False(kickResult.Success);
        Assert.Contains("not the leader", kickResult.Message.ToLower());
    }

    [Fact]
    public void Should_KickMember_And_RestrictDuringCombat()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        // Kick during combat
        var failKick = _partyManager.KickMember(1, 2, isLeaderInCombat: false, isTargetInCombat: true);
        Assert.False(failKick.Success);
        Assert.Contains("combat", failKick.Message.ToLower());

        // Kick out of combat
        var kick = _partyManager.KickMember(1, 2, false, false);
        Assert.True(kick.Success);

        var party = _partyManager.GetPartyByMember(1);
        Assert.DoesNotContain(2, party!.MemberIds);
        Assert.Null(_partyManager.GetPartyByMember(2));
    }

    [Fact]
    public void Should_LeaveParty_And_DisbandIfEmpty()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        // Member leaves
        Assert.True(_partyManager.LeaveParty(2));
        Assert.Null(_partyManager.GetPartyByMember(2));

        // Party should still exist with Leader
        var party = _partyManager.GetPartyByMember(1);
        Assert.NotNull(party);
        Assert.Single(party.MemberIds);
        Assert.Equal(1, party.LeaderId);

        // Leader leaves
        Assert.True(_partyManager.LeaveParty(1));
        Assert.Null(_partyManager.GetPartyByMember(1));
    }

    [Fact]
    public void Should_TransferLeadership_When_LeaderLeaves_And_MembersRemain()
    {
        _partyManager.InviteMember(1, "P1", 2, "P2");
        _partyManager.AcceptInvite(2, 1);

        _partyManager.InviteMember(1, "P1", 3, "P3");
        _partyManager.AcceptInvite(3, 1);

        // Leader leaves
        Assert.True(_partyManager.LeaveParty(1));

        var party = _partyManager.GetPartyByMember(2);
        Assert.NotNull(party);
        Assert.Equal(2, party.MemberIds.Count);
        Assert.DoesNotContain(1, party.MemberIds);

        // Leadership should transfer to the next member (2)
        Assert.Equal(2, party.LeaderId);
    }
}
