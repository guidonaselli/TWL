using System;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Simulation.Managers;

public interface IRebirthService
{
    int GetDiminishingReturnsBonus(int currentRebirthCount);
    (bool Success, string Message, int StatPointsGained) TryRebirthCharacter(ServerCharacter character, string operationId);
}
