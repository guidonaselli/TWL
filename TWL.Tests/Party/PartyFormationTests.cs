using System.Linq;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Party;
using Xunit;

namespace TWL.Tests.Party;

public class PartyFormationTests
{
    private readonly PartyManager _partyManager;

    public PartyFormationTests()
    {
        _partyManager = new PartyManager();
    }

    [Fact]
    public void AcceptInvite_ShouldAssignDefaultGridPositions()
    {
        // Arrange
        int leaderId = 100;
        int memberId = 200;
        _partyManager.InviteMember(leaderId, "Leader", memberId, "Member");

        // Act
        var result = _partyManager.AcceptInvite(memberId, leaderId);

        // Assert
        Assert.True(result.Success);
        
        var party = _partyManager.GetPartyByMember(leaderId);
        Assert.NotNull(party);
        
        Assert.True(party.Formation.MemberPositions.ContainsKey(leaderId));
        Assert.True(party.Formation.MemberPositions.ContainsKey(memberId));
        
        // Leader defaults to Mid row (X=1), Col 0
        var leaderPos = party.Formation.MemberPositions[leaderId];
        Assert.Equal(1, leaderPos.X);
        Assert.Equal(0, leaderPos.Y);
        
        // Next member defaults to Mid row (X=1), next available col (Col 1)
        var memberPos = party.Formation.MemberPositions[memberId];
        Assert.Equal(1, memberPos.X);
        Assert.True(memberPos.Y > 0 && memberPos.Y < 4); // Usually 1
    }

    [Fact]
    public void UpdateMemberPosition_ShouldFail_WhenPositionIsOutOfBounds()
    {
        // Arrange
        int leaderId = 100;
        int memberId = 200;
        _partyManager.InviteMember(leaderId, "Leader", memberId, "Member");
        _partyManager.AcceptInvite(memberId, leaderId);
        
        var party = _partyManager.GetPartyByMember(leaderId);
        int partyId = party!.PartyId;

        // Act & Assert
        // Row (X) out of bounds
        var result = _partyManager.UpdateMemberPosition(partyId, leaderId, -1, 0);
        Assert.False(result.Success);
        
        result = _partyManager.UpdateMemberPosition(partyId, leaderId, 3, 0);
        Assert.False(result.Success);

        // Col (Y) out of bounds
        result = _partyManager.UpdateMemberPosition(partyId, leaderId, 1, -1);
        Assert.False(result.Success);
        
        result = _partyManager.UpdateMemberPosition(partyId, leaderId, 1, 4);
        Assert.False(result.Success);
    }

    [Fact]
    public void UpdateMemberPosition_ShouldFail_WhenPositionIsOccupied()
    {
        // Arrange
        int leaderId = 100;
        int memberId = 200;
        _partyManager.InviteMember(leaderId, "Leader", memberId, "Member");
        _partyManager.AcceptInvite(memberId, leaderId);
        
        var party = _partyManager.GetPartyByMember(leaderId);
        int partyId = party!.PartyId;
        
        // Force member to 0,0
        _partyManager.UpdateMemberPosition(partyId, memberId, 0, 0);

        // Act
        // Leader tries to move to 0,0
        var result = _partyManager.UpdateMemberPosition(partyId, leaderId, 0, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Position is already occupied by a party member.", result.Message);
    }

    [Fact]
    public void UpdateMemberPosition_ShouldSucceed_WhenPositionIsFree()
    {
        // Arrange
        int leaderId = 100;
        int memberId = 200;
        _partyManager.InviteMember(leaderId, "Leader", memberId, "Member");
        _partyManager.AcceptInvite(memberId, leaderId);
        
        var party = _partyManager.GetPartyByMember(leaderId);
        int partyId = party!.PartyId;

        // Act
        var result = _partyManager.UpdateMemberPosition(partyId, leaderId, 0, 0);

        // Assert
        Assert.True(result.Success);
        
        var leaderPos = party.Formation.MemberPositions[leaderId];
        Assert.Equal(0, leaderPos.X);
        Assert.Equal(0, leaderPos.Y);
    }
}
