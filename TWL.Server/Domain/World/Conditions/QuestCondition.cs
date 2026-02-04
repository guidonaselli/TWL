using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Domain.World.Conditions;

public class QuestCondition : ITriggerCondition
{
    public int QuestId { get; }
    public string RequiredStatus { get; } // "NotStarted", "InProgress", "Completed", "RewardClaimed"

    public QuestCondition(int questId, string requiredStatus)
    {
        QuestId = questId;
        RequiredStatus = requiredStatus;
    }

    public bool IsMet(ServerCharacter character, PlayerService playerService)
    {
        var session = playerService.GetSession(character.Id);
        if (session == null)
        {
            return false;
        }

        var questState = QuestState.NotStarted;
        if (session.QuestComponent.QuestStates.TryGetValue(QuestId, out var state))
        {
            questState = state;
        }

        return RequiredStatus switch
        {
            "NotStarted" => questState == QuestState.NotStarted,
            "InProgress" => questState == QuestState.InProgress,
            "Completed" => questState == QuestState.Completed,
            "RewardClaimed" => questState == QuestState.RewardClaimed,
            "Finished" => questState == QuestState.Completed || questState == QuestState.RewardClaimed,
            _ => false
        };
    }
}
