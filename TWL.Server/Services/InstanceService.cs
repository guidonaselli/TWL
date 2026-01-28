using System;
using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Managers;

namespace TWL.Server.Services;

public class InstanceService
{
    private readonly ServerMetrics _metrics;

    public InstanceService(ServerMetrics metrics)
    {
        _metrics = metrics;
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

    public void FailInstance(ClientSession session, string instanceId)
    {
        CompleteInstance(session, instanceId, false);
    }
}
