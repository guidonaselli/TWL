using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class WorldScheduler : IWorldScheduler, IDisposable
{
    private readonly ILogger<WorldScheduler> _logger;
    private readonly ServerMetrics _metrics;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private readonly List<ScheduledTask> _scheduledTasks = new();
    private readonly object _lock = new();

    private const int TickRateMs = 50; // 20 TPS
    private const int MaxTicksPerFrame = 10; // Avoid spiral of death (max 500ms catchup)

    private long _currentTick;
    public long CurrentTick => _currentTick;

    public event Action<long>? OnTick;

    private class ScheduledTask
    {
        public Action Action { get; set; } = () => { };
        public DateTime NextRun { get; set; }
        public TimeSpan? Interval { get; set; } // If null, one-off
        public string Name { get; set; } = "Unnamed";
    }

    public WorldScheduler(ILogger<WorldScheduler> logger, ServerMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void Start()
    {
        if (_loopTask != null) return;
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
        catch (AggregateException) { } // Ignore cancel exception
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping WorldScheduler");
        }
        _loopTask = null;
        _logger.LogInformation("WorldScheduler stopped.");
    }

    public void Schedule(Action action, TimeSpan delay, string name = "Unnamed")
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                NextRun = DateTime.UtcNow.Add(delay),
                Interval = null,
                Name = name
            });
        }
    }

    public void ScheduleRepeating(Action action, TimeSpan interval, string name = "Unnamed")
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                NextRun = DateTime.UtcNow.Add(interval),
                Interval = interval,
                Name = name
            });
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double accumulator = 0;
        long lastMs = 0;

        while (!token.IsCancellationRequested)
        {
            long currentMs = stopwatch.ElapsedMilliseconds;
            double frameTime = currentMs - lastMs;
            lastMs = currentMs;

            // Cap frame time to avoid spiral of death
            if (frameTime > 250) frameTime = 250;

            accumulator += frameTime;

            int ticksProcessed = 0;
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
                _logger.LogWarning("WorldScheduler skipping ticks to catch up. Accumulator: {Accumulator}ms", accumulator);
                accumulator = 0;
            }

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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _currentTick++;

        // 1. Invoke Event Listeners
        try
        {
            OnTick?.Invoke(_currentTick);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OnTick listeners");
        }

        // 2. Process Legacy Scheduled Tasks
        // Note: This is still checking time, not ticks, to respect the DateTime.UtcNow semantic of the existing Schedule API.
        // It's checked every tick now, which is consistent.
        int tasksCount = ProcessScheduledTasks();

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
        var now = DateTime.UtcNow;
        var tasksToRun = new List<ScheduledTask>();
        int queueDepth = 0;

        lock (_lock)
        {
            queueDepth = _scheduledTasks.Count;
            for (int i = _scheduledTasks.Count - 1; i >= 0; i--)
            {
                var task = _scheduledTasks[i];
                if (task.NextRun <= now)
                {
                    tasksToRun.Add(task);

                    if (task.Interval.HasValue)
                    {
                        // Schedule next run
                        // Prevent drift by adding interval to previous NextRun, or set to Now + Interval?
                        // Now + Interval is safer against burst execution after lag.
                        task.NextRun = now.Add(task.Interval.Value);
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
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

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
