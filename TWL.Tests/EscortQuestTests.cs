using System.Collections.Generic;
using Moq;
using Xunit;
using TWL.Server.Simulation.Networking.Components;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Quests;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Interactions;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests
{
    public class EscortQuestTests
    {
        [Fact]
        public void HandleCombatantDeath_ShouldFailQuest_WhenConditionMet()
        {
            // Arrange
            var mockQuestManager = new Mock<ServerQuestManager>();
            var questId = 5003;
            var def = new QuestDefinition
            {
                QuestId = questId,
                Title = "Escort Duty",
                Description = "Protect Leader",
                Objectives = new List<ObjectiveDefinition>(),
                Rewards = new RewardDefinition(0, 0, new List<ItemReward>()),
                FailConditions = new List<QuestFailCondition>
                {
                    new QuestFailCondition("NpcDeath", "CaravanLeader", "Leader died")
                }
            };

            mockQuestManager.Setup(m => m.GetDefinition(questId)).Returns(def);

            var component = new PlayerQuestComponent(mockQuestManager.Object, null);

            // Manually force start quest (bypassing StartQuest checks which might need more mocks)
            component.QuestStates[questId] = QuestState.InProgress;

            // Act
            component.HandleCombatantDeath("CaravanLeader");

            // Assert
            Assert.Equal(QuestState.Failed, component.QuestStates[questId]);
        }

        [Fact]
        public void InteractionManager_ShouldConsumeItems_ForCompound()
        {
            // Arrange
            var manager = new InteractionManager();
            // We need to inject definitions or load a file.
            // InteractionManager loads from file. We can create a temp file.

            var interactionJson = @"
            [
              {
                ""TargetName"": ""AlchemyTable"",
                ""Type"": ""Compound"",
                ""ConsumeRequiredItems"": true,
                ""RequiredItems"": [ { ""ItemId"": 1, ""Quantity"": 1 } ],
                ""RewardItems"": [ { ""ItemId"": 2, ""Quantity"": 1 } ]
              }
            ]";
            var tempFile = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, interactionJson);
            manager.Load(tempFile);

            var character = new ServerCharacter();
            character.AddItem(1, 1); // Add ingredient

            var questComponent = new PlayerQuestComponent(new Mock<ServerQuestManager>().Object);

            // Act
            var result = manager.ProcessInteraction(character, questComponent, "AlchemyTable");

            // Assert
            Assert.Equal("Compound", result);
            Assert.False(character.HasItem(1, 1)); // Should be consumed
            Assert.True(character.HasItem(2, 1)); // Should be rewarded

            System.IO.File.Delete(tempFile);
        }
    }
}
