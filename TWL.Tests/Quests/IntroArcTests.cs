using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class IntroArcTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;
    private readonly ServerCharacter _character;

    public IntroArcTests()
    {
        _questManager = new ServerQuestManager();

        // Locate Content/Data/quests.json
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../Content/Data");
        if (!Directory.Exists(dir))
        {
            // Fallback for different runners
            dir = Path.Combine(Directory.GetCurrentDirectory(), "Content/Data");
        }

        var path = Path.Combine(dir, "quests.json");
        // Ensure we load the specific file
        if (File.Exists(path))
        {
            _questManager.Load(path);
        }
        else
        {
            throw new FileNotFoundException($"Could not find quests.json at {path}");
        }

        _playerQuests = new PlayerQuestComponent(_questManager);
        _character = new ServerCharacter { Id = 1, Name = "TestPlayer", Exp = 0, Gold = 0 };
        _playerQuests.Character = _character;
    }

    [Fact]
    public void Quest1001_DespertarEnLaPlaya_Flow()
    {
        int questId = 1001;
        Assert.True(_playerQuests.StartQuest(questId), "Should start Quest 1001");

        // Objective: Talk to Capitana Maren
        _playerQuests.TryProgress("Talk", "Capitana Maren");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[questId]);
        Assert.True(_playerQuests.ClaimReward(questId));
        Assert.Equal(10, _character.Exp); // 10 Exp reward
        Assert.Equal(5, _character.Gold); // 5 Gold reward
    }

    [Fact]
    public void IntroArc_FullProgression()
    {
        // 1001: Talk to Maren
        _playerQuests.StartQuest(1001);
        _playerQuests.TryProgress("Talk", "Capitana Maren");
        _playerQuests.ClaimReward(1001);

        // 1002: Collect Coconuts
        Assert.True(_playerQuests.CanStartQuest(1002), "Should unlock 1002");
        _playerQuests.StartQuest(1002);

        // Objective: Collect 3 Coconuts (ID 7330)
        _character.AddItem(7330, 3);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1002]);
        _playerQuests.ClaimReward(1002);

        // 1003: Talk to Calloway
        _playerQuests.StartQuest(1003);
        _playerQuests.TryProgress("Talk", "Dr. Calloway");
        _playerQuests.ClaimReward(1003);

        // 1004: Collect Driftwood
        _playerQuests.StartQuest(1004);
        _character.AddItem(7316, 5); // Driftwood
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1004]);
        _playerQuests.ClaimReward(1004);

        // 1005: Fire and Shelter
        _playerQuests.StartQuest(1005);
        _playerQuests.TryProgress("Interact", "Fresh Water Source");
        _character.AddItem(301, 3); // Kindling
        _character.AddItem(8101, 2); // Flint
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1005]);
        _playerQuests.ClaimReward(1005);

        // 1006: Kill Crabs
        _playerQuests.StartQuest(1006);
        // Objective: Kill 5 Tide Crabs (ID 2002)
        // Test new ID based progress
        for(int i=0; i<5; i++)
        {
             _playerQuests.TryProgress("Kill", "Tide Crab (Water)", 1, 2002);
        }
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[1006]);
        _playerQuests.ClaimReward(1006);
    }
}
