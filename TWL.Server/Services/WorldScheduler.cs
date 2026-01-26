using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class WorldScheduler : IWorldScheduler, IDisposable
{
    private readonly ILogger<WorldScheduler> _logger;
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

    public WorldScheduler(ILogger<WorldScheduler> logger)
    {
        _logger = logger;
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
        while (!token.IsCancellationRequested)
        {
            try
            {
                ProcessTasks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorldScheduler loop");
            }

            try
            {
                await Task.Delay(50, token); // 20 ticks per second
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void ProcessTasks()
    {
        var now = DateTime.UtcNow;
        var tasksToRun = new List<Action>();

        lock (_lock)
        {
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
                        // We add to 'now' or 'NextRun'? 'now' prevents drift if we lag, but 'NextRun' preserves strict interval.
                        // Usually 'now' is safer to prevent burst after lag.
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
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
