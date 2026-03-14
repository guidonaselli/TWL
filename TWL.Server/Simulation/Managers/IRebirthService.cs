using System;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

public interface IRebirthService
{
    int GetDiminishingReturnsBonus(int currentRebirthCount);
    (bool Success, string Message, int StatPointsGained) TryRebirthCharacter(ServerCharacter character, string operationId);
    (bool Success, string Message, int StatPointsGained) TryRebirthCharacter(ServerCharacter character, TWL.Server.Simulation.Networking.Components.PlayerQuestComponent? questComponent, string operationId);
    void SetRequirements(TWL.Shared.Domain.Models.RebirthRequirements requirements);
    void LoadRequirements(string path);
}
