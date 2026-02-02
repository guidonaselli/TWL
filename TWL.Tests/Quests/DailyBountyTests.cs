using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Tests.Quests;

public class DailyBountyTests
{
    private readonly ServerCharacter _character;
    private readonly PlayerQuestComponent _questComponent;
    private readonly ServerQuestManager _questManager;

    public DailyBountyTests()
    {
        _questManager = new ServerQuestManager();
        _character = new ServerCharacter { Id = 1, Name = "BountyHunter" };
        _questComponent = new PlayerQuestComponent(_questManager) { Character = _character };

        // Load Real Data
        var contentPath = Path.Combine("..", "..", "..", "..", "Content", "Data");
        if (!Directory.Exists(contentPath))
        {
            contentPath = Path.Combine("Content", "Data");
        }

        // We only really need quests.json for this test
        _questManager.Load(Path.Combine(contentPath, "quests.json"));
    }

    [Fact]
    public void CanStart_OnlyOneBounty_PerDay()
    {
        // 1. Start Quest 2050 (Wolf)
        Assert.True(_questComponent.StartQuest(2050), "Should start Wolf bounty");

        // 2. Try Start Quest 2051 (Crab) - Should Fail due to Exclusion
        Assert.False(_questComponent.StartQuest(2051), "Should NOT start Crab bounty while Wolf is active");

        // 3. Complete Wolf Bounty
        _questComponent.TryProgress("Kill", "Wolf", 5);
        Assert.Equal(QuestState.Completed, _questComponent.QuestStates[2050]);

        // 4. Claim Reward
        Assert.True(_questComponent.ClaimReward(2050));
        Assert.Equal(QuestState.RewardClaimed, _questComponent.QuestStates[2050]);

        // 5. Try Start Quest 2051 (Crab) - Should Fail due to Daily Exclusion
        Assert.False(_questComponent.StartQuest(2051), "Should NOT start Crab bounty after Wolf completed today");
    }

    [Fact]
    public void NextDay_UnblocksGroup()
    {
        // 1. Start & Complete Quest 2050 (Wolf)
        Assert.True(_questComponent.StartQuest(2050));
        _questComponent.TryProgress("Kill", "Wolf", 5);
        _questComponent.ClaimReward(2050);

        // Verify we can't do it again today
        Assert.False(_questComponent.StartQuest(2050));

        // Verify we can't do 2051 today
        Assert.False(_questComponent.StartQuest(2051));

        // 2. Manipulate Time (Simulate Reset)
        var times = _questComponent.QuestCompletionTimes;
        if (times.ContainsKey(2050))
        {
            // Set to yesterday
            times[2050] = DateTime.UtcNow.AddDays(-1);
        }

        // 3. Try Start Quest 2051 (Crab) - Should Succeed now
        Assert.True(_questComponent.StartQuest(2051), "Should start Crab bounty on next day");

        // 4. Verify 2050 is blocked (since 2051 is active)
        Assert.False(_questComponent.StartQuest(2050), "Should NOT start Wolf bounty while Crab is active");
    }
}
