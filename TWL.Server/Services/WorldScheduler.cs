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

    private class ScheduledTask
    {
        public Action Action { get; set; } = () => { };
        public DateTime NextRun { get; set; }
        public TimeSpan? Interval { get; set; } // If null, one-off
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

    public void Schedule(Action action, TimeSpan delay)
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                NextRun = DateTime.UtcNow.Add(delay),
                Interval = null
            });
        }
    }

    public void ScheduleRepeating(Action action, TimeSpan interval)
    {
        lock (_lock)
        {
            _scheduledTasks.Add(new ScheduledTask
            {
                Action = action,
                NextRun = DateTime.UtcNow.Add(interval),
                Interval = interval
            });
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        const int TargetTickMs = 50;
        var loopTimer = System.Diagnostics.Stopwatch.StartNew();
        var nextTick = loopTimer.ElapsedMilliseconds;

        while (!token.IsCancellationRequested)
        {
            var now = loopTimer.ElapsedMilliseconds;

            // Calculate slippage (how far behind schedule we are)
            var slippage = now - nextTick;
            if (slippage < 0) slippage = 0; // We are ahead (or on time)

            if (slippage > 0)
            {
                _metrics.RecordWorldLoopSlippage(slippage);
            }

            // Schedule next tick
            nextTick += TargetTickMs;

            // Hard drift correction if we are too far behind (e.g. > 1s), to prevent burst catch-up
            if ((now - nextTick) > 1000)
            {
                _logger.LogWarning("World Loop drifted significantly ({Drift}ms). Resetting schedule.", now - nextTick);
                nextTick = now + TargetTickMs;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int tasksCount = 0;
            try
            {
                tasksCount = ProcessTasks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorldScheduler loop");
            }
            sw.Stop();

            var elapsed = sw.ElapsedMilliseconds;
            _metrics.RecordWorldLoopTick(elapsed, tasksCount);

            if (elapsed > TargetTickMs)
            {
                _logger.LogWarning("Slow World Tick: {Duration}ms > {Target}ms. Tasks: {Count}. Slippage: {Slippage}ms",
                    elapsed, TargetTickMs, tasksCount, slippage);
            }

            // Calculate delay until next scheduled tick
            var delay = nextTick - loopTimer.ElapsedMilliseconds;
            if (delay < 1) delay = 1; // Yield at least

            try
            {
                await Task.Delay((int)delay, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private int ProcessTasks()
    {
        var now = DateTime.UtcNow;
        var tasksToRun = new List<Action>();
        int queueDepth = 0;

        lock (_lock)
        {
            queueDepth = _scheduledTasks.Count;
            // Iterate backwards to allow safe removal
            for (int i = _scheduledTasks.Count - 1; i >= 0; i--)
            {
                var task = _scheduledTasks[i];
                if (task.NextRun <= now)
                {
                    tasksToRun.Add(task.Action);

                    if (task.Interval.HasValue)
                    {
                        // Schedule next run
                        task.NextRun = now.Add(task.Interval.Value);
                    }
                    else
                    {
                        _scheduledTasks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (var action in tasksToRun)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled task");
            }
        }

        return queueDepth; // Return initial queue depth for observability
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
