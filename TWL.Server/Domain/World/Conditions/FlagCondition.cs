using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Domain.World.Conditions;

public class FlagCondition : ITriggerCondition
{
    public string RequiredFlag { get; }
    public bool Inverted { get; }

    public FlagCondition(string requiredFlag, bool inverted = false)
    {
        RequiredFlag = requiredFlag;
        Inverted = inverted;
    }

    public bool IsMet(ServerCharacter character, PlayerService playerService)
    {
        var session = playerService.GetSession(character.Id);
        if (session == null) return false;

        var hasFlag = session.QuestComponent.Flags.Contains(RequiredFlag);
        return Inverted ? !hasFlag : hasFlag;
    }
}
