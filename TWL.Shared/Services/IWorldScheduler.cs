namespace TWL.Shared.Services;

public interface IWorldScheduler
{
    /// <summary>
    ///     The current monotonic tick count of the world.
    ///     Increments by 1 every fixed time step (e.g. 50ms).
    /// </summary>
    long CurrentTick { get; }

    void Start();
    void Stop();

    /// <summary>
    ///     Event fired every world tick.
    ///     Subscribers should be lightweight to avoid stalling the loop.
    ///     Argument is the CurrentTick.
    /// </summary>
    event Action<long> OnTick;

    void Schedule(Action action, TimeSpan delay, string name = "Unnamed");
    void Schedule(Action action, int delayTicks, string name = "Unnamed");
    void ScheduleRepeating(Action action, TimeSpan interval, string name = "Unnamed");
    void ScheduleRepeating(Action action, int intervalTicks, string name = "Unnamed");
}