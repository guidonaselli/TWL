using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class WorldScheduler : IWorldScheduler, IDisposable
{
    private const int TickRateMs = 50; // 20 TPS
    private const int MaxTicksPerFrame = 10; // Avoid spiral of death (max 500ms catchup)
    private readonly object _lock = new();
    private readonly ILogger<WorldScheduler> _logger;
    private readonly ServerMetrics _metrics;
    private readonly List<ScheduledTask> _scheduledTasks = new();
    private CancellationTokenSource? _cts;

    private Task? _loopTask;

    public WorldScheduler(ILogger<WorldScheduler> logger, ServerMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }

    public long CurrentTick { get; private set; }

    public event Action<long>? OnTick;

    public void Start()
    {
        if (_loopTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        _logger.LogInformation("WorldScheduler started.");
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _loopTask?.Wait(2000);
        }
        catch (AggregateException)
        {
        } // Ignore cancel exception
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping WorldScheduler");
        }

        _loopTask = null;
        _logger.LogInformation("WorldScheduler stopped.");
    }

    public void Schedule(Action action, TimeSpan delay, string name = "Unnamed")
    {
        var delayTicks = (int)(delay.TotalMilliseconds / TickRateMs);
        if (delayTicks < 1) delayTicks = 1;
        Schedule(action, delayTicks, name);
    }

    public void Schedule(Action action, int delayTicks, string name = "Unnamed")
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                TargetTick = CurrentTick + delayTicks,
                IntervalTicks = null,
                Name = name
            });
        }
    }

    public void ScheduleRepeating(Action action, TimeSpan interval, string name = "Unnamed")
    {
        var intervalTicks = (int)(interval.TotalMilliseconds / TickRateMs);
        if (intervalTicks < 1) intervalTicks = 1;
        ScheduleRepeating(action, intervalTicks, name);
    }

    public void ScheduleRepeating(Action action, int intervalTicks, string name = "Unnamed")
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                TargetTick = CurrentTick + intervalTicks,
                IntervalTicks = intervalTicks,
                Name = name
            });
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        double accumulator = 0;
        long lastMs = 0;

        while (!token.IsCancellationRequested)
        {
            var currentMs = stopwatch.ElapsedMilliseconds;
            double frameTime = currentMs - lastMs;
            lastMs = currentMs;

            // Cap frame time to avoid spiral of death
            if (frameTime > 250)
            {
                frameTime = 250;
            }

            accumulator += frameTime;

            var ticksProcessed = 0;
            while (accumulator >= TickRateMs && ticksProcessed < MaxTicksPerFrame)
            {
                try
                {
                    ProcessTick();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical Error in WorldScheduler ProcessTick");
                }

                accumulator -= TickRateMs;
                ticksProcessed++;
            }

            // If we are still behind, discard the accumulator to catch up
            if (accumulator >= TickRateMs)
            {
                _logger.LogWarning("WorldScheduler skipping ticks to catch up. Accumulator: {Accumulator}ms",
                    accumulator);
                var skipped = (int)(accumulator / TickRateMs);
                _metrics.RecordWorldLoopSkippedTicks(skipped);
                accumulator = 0;
            }

            // Record Drift (Total elapsed real time vs. Total elapsed simulated time)
            // Real Time = stopwatch.ElapsedMilliseconds
            // Sim Time = CurrentTick * TickRateMs
            var drift = stopwatch.ElapsedMilliseconds - (CurrentTick * TickRateMs);
            _metrics.RecordWorldLoopDrift(drift);

            // Sleep to yield CPU
            var sleepTime = TickRateMs - (int)accumulator;
            if (sleepTime > 0)
            {
                try
                {
                    await Task.Delay(sleepTime, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            else
            {
                // Yield to prevent thread starvation if we are running hot
                await Task.Yield();
            }
        }
    }

    private void ProcessTick()
    {
        var sw = Stopwatch.StartNew();
        CurrentTick++;

        // 1. Invoke Event Listeners
        try
        {
            OnTick?.Invoke(CurrentTick);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OnTick listeners");
        }

        // 2. Process Scheduled Tasks
        var tasksCount = ProcessScheduledTasks();

        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;

        // Metrics
        _metrics.RecordWorldLoopTick(elapsed, tasksCount);
        if (elapsed > TickRateMs)
        {
            _logger.LogWarning("Slow World Loop Tick: {Duration}ms > {Limit}ms", elapsed, TickRateMs);
            _metrics.RecordSlowWorldTick(elapsed);
        }
    }

    private int ProcessScheduledTasks()
    {
        var tasksToRun = new List<ScheduledTask>();
        var queueDepth = 0;

        lock (_lock)
        {
            queueDepth = _scheduledTasks.Count;
            for (var i = _scheduledTasks.Count - 1; i >= 0; i--)
            {
                var task = _scheduledTasks[i];
                if (task.TargetTick <= CurrentTick)
                {
                    tasksToRun.Add(task);

                    if (task.IntervalTicks.HasValue)
                    {
                        // Schedule next run
                        task.TargetTick = CurrentTick + task.IntervalTicks.Value;
                    }
                    else
                    {
                        _scheduledTasks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (var task in tasksToRun)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                task.Action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled task '{TaskName}'", task.Name);
            }

            sw.Stop();

            if (sw.ElapsedMilliseconds > 10)
            {
                _logger.LogWarning("Slow World Task '{TaskName}': {Duration}ms", task.Name, sw.ElapsedMilliseconds);
                _metrics.RecordSlowWorldTask(task.Name, sw.ElapsedMilliseconds);
            }
        }

        return queueDepth;
    }

    private class ScheduledTask
    {
        public Action Action { get; set; } = () => { };
        public long TargetTick { get; set; }
        public int? IntervalTicks { get; set; } // If null, one-off
        public string Name { get; set; } = "Unnamed";
    }
}
