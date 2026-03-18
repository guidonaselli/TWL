using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Services;

public class InstanceService
{
    public const int DailyLimit = 5;

    private readonly ServerMetrics _metrics;

    public InstanceService(ServerMetrics metrics)
    {
        _metrics = metrics;
    }

    public bool CanEnterInstance(ServerCharacter character, string instanceId)
    {
        CheckAndResetDailyLimits(character);

        if (character.InstanceDailyRuns.TryGetValue(instanceId, out var count))
        {
            return count < DailyLimit;
        }

        return true;
    }

    public void RecordInstanceRun(ServerCharacter character, string instanceId)
    {
        CheckAndResetDailyLimits(character);

        if (!character.InstanceDailyRuns.ContainsKey(instanceId))
        {
            character.InstanceDailyRuns[instanceId] = 0;
        }

        character.InstanceDailyRuns[instanceId]++;
        character.IsDirty = true;
    }

    private void CheckAndResetDailyLimits(ServerCharacter character)
    {
        var today = DateTime.UtcNow.Date;
        if (today > character.InstanceDailyResetUtc.Date)
        {
            character.InstanceDailyRuns.Clear();
            character.InstanceDailyResetUtc = today;
            character.IsDirty = true;
        }
    }

    public void StartInstance(ClientSession session, string instanceId)
    {
        // Infrastructure stub: In a real system, this would spin up the instance context.
        Console.WriteLine($"[Instance] Player {session.UserId} started instance {instanceId}");

        // Trigger generic 'InstanceStarted' hook if we had one.
    }

    public void CompleteInstance(ClientSession session, string instanceId, bool success)
    {
        Console.WriteLine($"[Instance] Player {session.UserId} completed instance {instanceId} (Success: {success})");

        if (success)
        {
            session.HandleInstanceCompletion(instanceId);
        }
        else
        {
            session.HandleInstanceFailure(instanceId);
        }
    }

    public void FailInstance(ClientSession session, string instanceId) => CompleteInstance(session, instanceId, false);
}