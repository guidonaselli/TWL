using System.Collections.Generic;
using System.Text.Json;
using TWL.Shared.Domain.State;
using TWL.Shared.Domain.DTO;
using Xunit;

namespace TWL.Tests.Party;

public class PartyStateSyncTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Update_ShouldClearState_WhenPartyIdIsZero()
    {
        // Arrange
        var partyState = new PartyState();
        partyState.PartyId = 123;
        partyState.LeaderId = 1;
        partyState.Members.Add(new PartyMemberDto { CharacterId = 1, Name = "Leader" });

        var update = new TWL.Shared.Domain.DTO.PartyUpdateBroadcast
        {
            PartyId = 0
        };

        // Act
        partyState.Update(update);

        // Assert
        Assert.Null(partyState.PartyId);
        Assert.Null(partyState.LeaderId);
        Assert.Empty(partyState.Members);
        Assert.Empty(partyState.ChatLog);
    }

    [Fact]
    public void Update_ShouldPopulateState_WhenPartyIdIsSet()
    {
        // Arrange
        var partyState = new PartyState();

        var update = new TWL.Shared.Domain.DTO.PartyUpdateBroadcast
        {
            PartyId = 456,
            LeaderId = 10,
            Members = new List<PartyMemberDto>
            {
                new PartyMemberDto { CharacterId = 10, Name = "Alice", CurrentHp = 100, MaxHp = 100 },
                new PartyMemberDto { CharacterId = 11, Name = "Bob", CurrentHp = 50, MaxHp = 100 }
            }
        };

        // Act
        partyState.Update(update);

        // Assert
        Assert.Equal(456, partyState.PartyId);
        Assert.Equal(10, partyState.LeaderId);
        Assert.Equal(2, partyState.Members.Count);
        Assert.Equal("Alice", partyState.Members[0].Name);
        Assert.Equal(50, partyState.Members[1].CurrentHp);
    }

    [Fact]
    public void AddMessage_ShouldLimitChatLogSize()
    {
        // Arrange
        var partyState = new PartyState();
        for (int i = 0; i < 55; i++)
        {
            partyState.AddMessage(new TWL.Shared.Domain.DTO.PartyChatMessage
            {
                SenderId = i,
                Content = $"Msg {i}"
            });
        }

        // Act & Assert
        Assert.Equal(50, partyState.ChatLog.Count);
        Assert.Equal("Msg 5", partyState.ChatLog[0].Content); // Should have removed 0-4
    }
}
