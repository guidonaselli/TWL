using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Models;
using TWL.Shared.Services;
using Xunit;

namespace TWL.Tests.Compound
{
    public class CompoundOutcomeTests
    {
        private readonly Mock<ICompoundRatePolicy> _ratePolicyMock;
        private readonly Mock<IRandomService> _randomServiceMock;
        private readonly CompoundManager _compoundManager;

        public CompoundOutcomeTests()
        {
            _ratePolicyMock = new Mock<ICompoundRatePolicy>();
            _randomServiceMock = new Mock<IRandomService>();
            var logger = new NullLogger<CompoundManager>();
            _compoundManager = new CompoundManager(logger, _ratePolicyMock.Object, _randomServiceMock.Object);
        }

        private ServerCharacter CreateCharacterWithItems(Item target, Item material)
        {
            var character = new ServerCharacter { Id = 1, Name = "Tester" };
            character.Inventory.Add(target);
            character.Inventory.Add(material);
            return character;
        }

        [Fact]
        public async Task Successful_Compound_Increases_Enhancement_Level_And_Consumes_Material()
        {
            // Arrange
            var targetItem = new Item { ItemId = 1, EnhancementLevel = 0, InstanceId = System.Guid.NewGuid() };
            var materialItem = new Item { ItemId = 2, Quantity = 1, InstanceId = System.Guid.NewGuid() };
            var character = CreateCharacterWithItems(targetItem, materialItem);
            var request = new CompoundRequestDTO { TargetItemId = targetItem.InstanceId, MaterialItemId = materialItem.InstanceId };

            _ratePolicyMock.Setup(p => p.GetSuccessChance(It.IsAny<Item>(), It.IsAny<Item>())).Returns(0.9); // 90% chance
            _randomServiceMock.Setup(r => r.NextDouble(It.IsAny<string>())).Returns(0.5); // Roll is less than chance

            // Act
            var response = await _compoundManager.ProcessCompoundRequest(character, request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal(CompoundOutcome.Success, response.Outcome);
            Assert.Equal(1, targetItem.EnhancementLevel);
            Assert.DoesNotContain(character.Inventory, i => i.InstanceId == materialItem.InstanceId); // Material consumed
            Assert.Contains(character.Inventory, i => i.InstanceId == targetItem.InstanceId); // Target preserved
        }

        [Fact]
        public async Task Failed_Compound_Consumes_Material_And_Preserves_Target()
        {
            // Arrange
            var targetItem = new Item { ItemId = 1, EnhancementLevel = 0, InstanceId = System.Guid.NewGuid() };
            var materialItem = new Item { ItemId = 2, Quantity = 1, InstanceId = System.Guid.NewGuid() };
            var character = CreateCharacterWithItems(targetItem, materialItem);
            var request = new CompoundRequestDTO { TargetItemId = targetItem.InstanceId, MaterialItemId = materialItem.InstanceId };

            _ratePolicyMock.Setup(p => p.GetSuccessChance(It.IsAny<Item>(), It.IsAny<Item>())).Returns(0.1); // 10% chance
            _randomServiceMock.Setup(r => r.NextDouble(It.IsAny<string>())).Returns(0.5); // Roll is greater than chance

            // Act
            var response = await _compoundManager.ProcessCompoundRequest(character, request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(CompoundOutcome.Fail, response.Outcome);
            Assert.Equal(0, targetItem.EnhancementLevel); // Enhancement level does not change
            Assert.DoesNotContain(character.Inventory, i => i.InstanceId == materialItem.InstanceId); // Material consumed
            Assert.Contains(character.Inventory, i => i.InstanceId == targetItem.InstanceId); // Target preserved
        }

        [Fact]
        public async Task ProcessCompoundRequest_Returns_Failure_For_Invalid_Items()
        {
            // Arrange
            var character = new ServerCharacter { Id = 1, Name = "Tester" };
            var request = new CompoundRequestDTO { TargetItemId = System.Guid.NewGuid(), MaterialItemId = System.Guid.NewGuid() };

            // Act
            var response = await _compoundManager.ProcessCompoundRequest(character, request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Invalid items specified.", response.Message);
        }
    }
}
