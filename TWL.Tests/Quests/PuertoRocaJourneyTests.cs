using System.Collections.Generic;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;
using Xunit;

namespace TWL.Tests.Quests;

public class PuertoRocaJourneyTests
{
    private readonly ServerQuestManager _questManager;
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerCharacter _character;

    public PuertoRocaJourneyTests()
    {
        _questManager = new ServerQuestManager();

        // Locate quests.json
        string path = "../../../../TWL.Server/Content/Data/quests.json";
        if (!File.Exists(path))
        {
             // Try valid fallback if running from root
             path = "TWL.Server/Content/Data/quests.json";
        }

        Assert.True(File.Exists(path), $"Quest file not found at {Path.GetFullPath(path)}");
        _questManager.Load(path);

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestAdventurer" };
        _character.AddExp(1000); // Level up to ensure requirements are met
        _playerQuests.Character = _character;
    }

    [Fact]
    public void JourneyToPuertoRoca_Chain_ShouldProgress_Correctly()
    {
        // Setup Prerequisites: Complete Quest 1004 (Compañero Fiel)
        // We can manually set the state to RewardClaimed
        _playerQuests.QuestStates[1004] = QuestState.RewardClaimed;

        // ---------------------------------------------------------
        // 1. Quest 1100: Partida Inminente
        // ---------------------------------------------------------
        Assert.True(_playerQuests.CanStartQuest(1100), "Should be able to start 1100");
        Assert.True(_playerQuests.StartQuest(1100));

        // Objective: Talk to Caravan Leader
        var updated = _playerQuests.TryProgress("Talk", "Caravan Leader");
        Assert.Single(updated);
        Assert.Equal(1100, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1100]);

        _playerQuests.ClaimReward(1100);
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1100]);

        // ---------------------------------------------------------
        // 2. Quest 1101: El Camino del Bosque
        // ---------------------------------------------------------
        Assert.True(_playerQuests.CanStartQuest(1101), "Should be able to start 1101");
        Assert.True(_playerQuests.StartQuest(1101));

        // Objective: Interact with Sendero Norte
        updated = _playerQuests.TryProgress("Interact", "Sendero Norte");
        Assert.Single(updated);
        Assert.Equal(1101, updated[0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1101]);

        _playerQuests.ClaimReward(1101);

        // ---------------------------------------------------------
        // 3. Quest 1102: Peligros del Camino
        // ---------------------------------------------------------
        Assert.True(_playerQuests.CanStartQuest(1102));
        Assert.True(_playerQuests.StartQuest(1102));

        // Objective: Kill Bandido del Camino x 2
        // Kill 1
        updated = _playerQuests.TryProgress("Kill", "Bandido del Camino");
        Assert.Single(updated);
        Assert.Equal(1, _playerQuests.QuestProgress[1102][0]);
        Assert.Equal(QuestState.InProgress, _playerQuests.QuestStates[1102]);

        // Kill 2
        updated = _playerQuests.TryProgress("Kill", "Bandido del Camino");
        Assert.Single(updated);
        Assert.Equal(2, _playerQuests.QuestProgress[1102][0]);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1102]);

        _playerQuests.ClaimReward(1102);

        // ---------------------------------------------------------
        // 4. Quest 1103: Llegada a Puerto Roca
        // ---------------------------------------------------------
        Assert.True(_playerQuests.CanStartQuest(1103));
        Assert.True(_playerQuests.StartQuest(1103));

        // Objective: Interact with Puerta de la Ciudad
        updated = _playerQuests.TryProgress("Interact", "Puerta de la Ciudad");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1103]);

        _playerQuests.ClaimReward(1103);

        // ---------------------------------------------------------
        // 5. Quest 1104: Ciudadano Nuevo
        // ---------------------------------------------------------
        Assert.True(_playerQuests.CanStartQuest(1104));
        Assert.True(_playerQuests.StartQuest(1104));

        // Objective: Talk to Funcionario de Registro
        updated = _playerQuests.TryProgress("Talk", "Funcionario de Registro");
        Assert.Single(updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1104]);

        _playerQuests.ClaimReward(1104);

        // Final State Check
        Assert.Equal(QuestState.RewardClaimed, _playerQuests.QuestStates[1104]);
    }
}
