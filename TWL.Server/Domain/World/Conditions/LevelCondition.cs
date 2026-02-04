using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Domain.World.Conditions;

public class LevelCondition : ITriggerCondition
{
    public int RequiredLevel { get; }

    public LevelCondition(int requiredLevel)
    {
        RequiredLevel = requiredLevel;
    }

    public bool IsMet(ServerCharacter character, PlayerService playerService)
    {
        return character.Level >= RequiredLevel;
    }
}
