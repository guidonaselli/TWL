using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

using TWL.Server.Simulation.Networking; // Ensure namespace is available

namespace TWL.Tests.Quests;

public class QuestExpansionTests
{
    private readonly PlayerQuestComponent _playerQuests;
    private readonly ServerQuestManager _questManager;

    public QuestExpansionTests()
    {
        _questManager = new ServerQuestManager();

        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Content/Data"));
        var baseQuestPath = Path.Combine(baseDir, "quests.json");
        var expansionPath = Path.Combine(baseDir, "quests_islabrisa_expansion.json");

        if (File.Exists(baseQuestPath))
        {
             _questManager.Load(baseQuestPath);
        }
        else
        {
            // Try to find it relative to current dir if baseDir resolution failed
            if (File.Exists("Content/Data/quests.json"))
                _questManager.Load("Content/Data/quests.json");
        }

        if (File.Exists(expansionPath))
        {
             _questManager.Load(expansionPath);
        }
        else
        {
             if (File.Exists("Content/Data/quests_islabrisa_expansion.json"))
                _questManager.Load("Content/Data/quests_islabrisa_expansion.json");
             else
                throw new FileNotFoundException($"Could not find quest file at {expansionPath}");
        }

        _playerQuests = new PlayerQuestComponent(_questManager);
        // Attach a character to enable item rewards and gating checks
        _playerQuests.Character = new ServerCharacter { Id = 1, Name = "TestPlayer" };
    }

    [Fact]
    public void PuzzleObjective_ShouldProgress()
    {
        var questId = 1090;
        Assert.NotNull(_questManager.GetDefinition(questId));

        Assert.True(_playerQuests.StartQuest(questId));

        // Trigger Puzzle
        var updated = _playerQuests.HandlePuzzle("Ruins_Puzzle_01");
        Assert.Contains(questId, updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[questId]);
    }

    [Fact]
    public void PartyObjective_ShouldProgress()
    {
        var questId = 1091;
        Assert.NotNull(_questManager.GetDefinition(questId));

        Assert.True(_playerQuests.StartQuest(questId));

        // Trigger Party
        var updated = _playerQuests.HandlePartyAction("Join");
        Assert.Contains(questId, updated);
        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[questId]);
    }

    [Fact]
    public void QuestChain_1507_to_1509_ShouldWork()
    {
        // Fake complete 1006 as it is a prerequisite for 1507
        _playerQuests.QuestStates[1006] = QuestState.Completed;

        // 1507: Cooking
        var q1507 = 1507;
        Assert.True(_playerQuests.StartQuest(q1507), "Should start 1507");

        // Obj 1: Collect Raw Fish (304) x 3
        // Simulate collecting by ID
        _playerQuests.TryProgress("Collect", "Carne de Cangrejo", 3, 304);

        // Obj 2: Craft Cooked Fish (305) x 1
        // Simulate crafting (Note: HandleCraft relies on Name matching as it doesn't pass DataId)
        _playerQuests.HandleCraft("Cangrejo Asado", 1);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q1507]);
        _playerQuests.ClaimReward(q1507);

        // 1508: Strange Egg
        var q1508 = 1508;
        Assert.True(_playerQuests.StartQuest(q1508), "Should start 1508");

        _playerQuests.TryProgress("Explore", "Nido Oculto");
        _playerQuests.TryProgress("Interact", "Huevo Extraño");

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q1508]);
        _playerQuests.ClaimReward(q1508);

        // 1509: Hatching
        var q1509 = 1509;
        Assert.True(_playerQuests.StartQuest(q1509), "Should start 1509");

        // Deliver
        _playerQuests.TryProgress("Deliver", "Entrenador de Bestias", 1, 7376);

        Assert.Equal(QuestState.Completed, _playerQuests.QuestStates[q1509]);
        _playerQuests.ClaimReward(q1509);
    }
}
