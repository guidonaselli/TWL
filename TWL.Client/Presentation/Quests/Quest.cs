using TWL.Shared.Domain.Requests;

namespace TWL.Client.Presentation.Quests;

public class Quest
{
    public string Description;
    public List<QuestObjective> Objectives;
    public int QuestID;
    public int RewardExp;
    public int RewardGold;
    public QuestState State;
    public string Title;

    public Quest(int id, string title, string desc, int rewardExp, int rewardGold)
    {
        QuestID = id;
        Title = title;
        Description = desc;
        RewardExp = rewardExp;
        RewardGold = rewardGold;
        State = QuestState.NotStarted;
        Objectives = new List<QuestObjective>();
    }

    public bool IsAllObjectivesCompleted() => Objectives.All(o => o.IsCompleted);
}