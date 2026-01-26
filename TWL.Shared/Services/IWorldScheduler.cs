using System;

namespace TWL.Shared.Services;

/// <summary>
/// Defines a contract for scheduling world events and loops.
/// Allows decoupling the game loop implementation from consumers.
/// </summary>
public interface IWorldScheduler
{
    /// <summary>
    /// Starts the scheduler loop.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the scheduler loop.
    /// </summary>
    void Stop();

    /// <summary>
    /// Schedules an action to be executed after a specified delay.
    /// </summary>
    void Schedule(Action action, TimeSpan delay);

    /// <summary>
    /// Schedules an action to be executed repeatedly at a specified interval.
    /// </summary>
    void ScheduleRepeating(Action action, TimeSpan interval);
}
