using System.Text.Json;
using System.Text.Json.Serialization;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class QuestBranchingAndFailureTests
{
    private ServerQuestManager _questManager;
    private PlayerQuestComponent _playerQuest;
    private ServerCharacter _character;

    public QuestBranchingAndFailureTests()
    {
        _questManager = new ServerQuestManager();

        // Find and load the branching demo file
        var currentDir = Directory.GetCurrentDirectory();
        string questsPath = null;
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Content", "Data", "quests_branching_demo.json");
            if (File.Exists(candidate))
            {
                questsPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        if (questsPath == null)
        {
            // Fallback for isolated test runs if file not found (e.g. CI nuances)
            // But we prefer loading the real file.
            throw new FileNotFoundException("Could not find Content/Data/quests_branching_demo.json");
        }

        _questManager.Load(questsPath);

        _character = new ServerCharacter
        {
            MapId = 1001 // Start at Crossroads
        };

        _playerQuest = new PlayerQuestComponent(_questManager);
        _playerQuest.Character = _character;
    }

    [Fact]
    public void BranchingFlow_VerifyMutualExclusionAndFailConditions()
    {
        // 1. Complete Intro Quest (9100)
        // It requires reaching Map 1001. We are there.
        Assert.True(_playerQuest.StartQuest(9100));
        _playerQuest.TryProgress("Reach", "Crossroads Map", 1, 1001);
        Assert.Equal(QuestState.Completed, _playerQuest.QuestStates[9100]);
        Assert.True(_playerQuest.ClaimReward(9100));

        // 2. Start Path of Fire (9101)
        Assert.True(_playerQuest.StartQuest(9101), "Should be able to start Fire Path");

        // 3. Try Start Path of Water (9102) -> Should fail (Mutual Exclusion - Simultaneous)
        Assert.False(_playerQuest.StartQuest(9102), "Should NOT be able to start Water Path while Fire Path is active");

        // 4. Test Fail Condition: Leave Map
        // Move to Map 1002
        _character.MapId = 1002; // Triggers OnMapChanged -> HandleMapChanged

        Assert.Equal(QuestState.Failed, _playerQuest.QuestStates[9101]);

        // 5. Retry Path of Fire
        // Move back to 1001
        _character.MapId = 1001;
        Assert.True(_playerQuest.StartQuest(9101), "Should be able to retry failed quest");
        Assert.Equal(QuestState.InProgress, _playerQuest.QuestStates[9101]);

        // 6. Complete Path of Fire
        _playerQuest.TryProgress("Kill", "Magma Crab (Fire)", 1, 2003);
        Assert.Equal(QuestState.Completed, _playerQuest.QuestStates[9101]);
        Assert.True(_playerQuest.ClaimReward(9101));

        // 7. Try Start Path of Water (9102) -> Should fail (Mutual Exclusion - Permanent)
        Assert.False(_playerQuest.StartQuest(9102), "Should NOT be able to start Water Path after completing Fire Path");

        // 8. Unlock Final Quest (9103)
        // Requires "PathChosen" flag, set by 9101
        Assert.Contains("PathChosen", _playerQuest.Flags);
        Assert.Contains("FirePath", _playerQuest.Flags);
        Assert.True(_playerQuest.CanStartQuest(9103));
        Assert.True(_playerQuest.StartQuest(9103));
    }
}
