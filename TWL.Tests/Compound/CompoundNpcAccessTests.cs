using Moq;
using TWL.Server.Features.Interactions;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Server.Services.World;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Net.Network;
using Xunit;
using System.Text.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TWL.Tests.Compound;

public class CompoundNpcAccessTests
{
    private class TestClientSession : ClientSession
    {
        public List<NetMessage> SentMessages { get; } = new();

        public TestClientSession(ICompoundService compoundService) 
            : base()
        {
            var field = typeof(ClientSession).GetField("_compoundService", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(this, compoundService);
        }

        public void SetCharacter(ServerCharacter character) => Character = character;
        public void SetUserId(int userId) => UserId = userId;

        public override Task SendAsync(NetMessage msg)
        {
            SentMessages.Add(msg);
            return Task.CompletedTask;
        }

        public Task PublicHandleCompoundRequestAsync(string payload, string traceId)
        {
            var method = typeof(ClientSession).GetMethod("HandleCompoundRequestAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task)method.Invoke(this, new object[] { payload, traceId });
        }
    }

    [Fact]
    public async Task InteractWithCompoundNPC_ShouldReturnCompoundType()
    {
        // Arrange
        var interactionManager = new InteractionManager();
        var tempFile = "test_interactions_compound.json";
        var interactions = new List<object>
        {
            new { TargetName = "CompoundMaster", Type = "Compound", RewardItems = new List<object>() }
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(interactions));
        interactionManager.Load(tempFile);

        var mapRegistryMock = new Mock<IMapRegistry>();
        mapRegistryMock.Setup(m => m.GetEntityPosition(It.IsAny<int>(), "CompoundMaster"))
            .Returns((0f, 0f));

        var handler = new InteractHandler(interactionManager, mapRegistryMock.Object);
        var character = new ServerCharacter { Id = 1, MapId = 1, X = 0, Y = 0 };
        var questComponent = new PlayerQuestComponent(new ServerQuestManager()) { Character = character };

        var command = new InteractCommand(character, questComponent, "CompoundMaster");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InteractionType.Compound, result.InteractionType);

        // Cleanup
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }

    [Fact]
    public async Task CompoundRequest_WithMissingItems_ShouldReject()
    {
        // Arrange
        var compoundServiceMock = new Mock<ICompoundService>();
        var session = new TestClientSession(compoundServiceMock.Object);
        var character = new ServerCharacter { Id = 1 };
        session.SetCharacter(character);
        session.SetUserId(1);

        var request = new CompoundRequestDTO
        {
            TargetItemId = Guid.NewGuid(),
            IngredientItemId = Guid.NewGuid()
        };
        var payload = JsonSerializer.Serialize(request);

        // Act
        await session.PublicHandleCompoundRequestAsync(payload, "trace-123");

        // Assert
        Assert.Single(session.SentMessages);
        var responseMsg = session.SentMessages[0];
        Assert.Equal(Opcode.CompoundResponse, responseMsg.Op);
        
        var responseDto = JsonSerializer.Deserialize<CompoundResponseDTO>(responseMsg.JsonPayload);
        Assert.False(responseDto.Success);
        Assert.Equal("ERR_COMPOUND_MISSING_ITEMS", responseDto.Message);
        
        compoundServiceMock.Verify(s => s.ProcessCompoundRequest(It.IsAny<ServerCharacter>(), It.IsAny<CompoundRequestDTO>()), Times.Never);
    }

    [Fact]
    public async Task CompoundRequest_WithValidItems_ShouldProceed()
    {
        // Arrange
        var compoundServiceMock = new Mock<ICompoundService>();
        compoundServiceMock.Setup(s => s.ProcessCompoundRequest(It.IsAny<ServerCharacter>(), It.IsAny<CompoundRequestDTO>()))
            .ReturnsAsync(new CompoundResponseDTO { Success = true, Outcome = CompoundOutcome.Success });

        var session = new TestClientSession(compoundServiceMock.Object);
        var character = new ServerCharacter { Id = 1 };
        
        var targetId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        
        var data = character.GetSaveData();
        data.Inventory = new List<Item>
        {
            new Item { ItemId = 1001, InstanceId = targetId, Quantity = 1 },
            new Item { ItemId = 2001, InstanceId = ingredientId, Quantity = 1 }
        };
        character.LoadSaveData(data);
        
        session.SetCharacter(character);
        session.SetUserId(1);

        var request = new CompoundRequestDTO
        {
            TargetItemId = targetId,
            IngredientItemId = ingredientId
        };
        var payload = JsonSerializer.Serialize(request);

        // Act
        await session.PublicHandleCompoundRequestAsync(payload, "trace-123");

        // Assert
        compoundServiceMock.Verify(s => s.ProcessCompoundRequest(character, It.Is<CompoundRequestDTO>(r => r.TargetItemId == targetId)), Times.Once);
        Assert.Equal(2, session.SentMessages.Count); // CompoundResponse + InventoryUpdate
        Assert.Contains(session.SentMessages, m => m.Op == Opcode.CompoundResponse);
        Assert.Contains(session.SentMessages, m => m.Op == Opcode.InventoryUpdate);
    }

    [Fact]
    public void RealInteractionsJson_ShouldContainCompoundMaster()
    {
        // Arrange
        var interactionManager = new InteractionManager();
        // Path needs to be relative to the test execution directory, 
        // which usually copies Content/Data there.
        var path = Path.Combine("Content", "Data", "interactions.json");
        
        // Act
        interactionManager.Load(path);
        
        // Assert
        var character = new ServerCharacter { Id = 1, MapId = 1, X = 0, Y = 0 };
        var questComponent = new PlayerQuestComponent(new ServerQuestManager()) { Character = character };
        
        var type = interactionManager.ProcessInteraction(character, questComponent, "CompoundMaster");
        
        Assert.Equal(InteractionType.Compound, type);
    }
}
