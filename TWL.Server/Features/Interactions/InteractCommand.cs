using TWL.Server.Architecture.Pipeline;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Networking.Components;

namespace TWL.Server.Features.Interactions;

public class InteractCommand : ICommand<InteractResult>
{
    public InteractCommand(ServerCharacter character, PlayerQuestComponent questComponent, string targetName)
    {
        Character = character;
        QuestComponent = questComponent;
        TargetName = targetName;
    }

    public ServerCharacter Character { get; }
    public PlayerQuestComponent QuestComponent { get; }
    public string TargetName { get; }
}