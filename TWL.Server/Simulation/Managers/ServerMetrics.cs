using System;
using System.Threading;

namespace TWL.Server.Simulation.Managers;

public class ServerMetrics
{
    private long _netBytesReceived;
    private long _netBytesSent;
    private long _netMessagesProcessed;
    private long _netErrors;

    private long _validationErrors;

    private long _persistenceFlushes;
    private long _persistenceErrors;
    private long _totalPersistenceDurationMs;
    private long _totalMessageProcessingTimeTicks;

    // New Observability Metrics
    private long _worldLoopTicks;
    private long _worldLoopTotalDurationMs;
    private long _worldSchedulerQueueDepth;

    private long _worldLoopSlowTicks;
    private long _worldLoopSlowTasks;
    private long _triggersExecuted;

    private long _pipelineValidateDurationTicks;
    private long _pipelineResolveDurationTicks;

    public void RecordNetBytesReceived(long bytes) => Interlocked.Add(ref _netBytesReceived, bytes);
    public void RecordNetBytesSent(long bytes) => Interlocked.Add(ref _netBytesSent, bytes);
    public void RecordNetMessageProcessed() => Interlocked.Increment(ref _netMessagesProcessed);
    public void RecordNetError() => Interlocked.Increment(ref _netErrors);

    public void RecordValidationError() => Interlocked.Increment(ref _validationErrors);

    public void RecordPersistenceFlush(long durationMs, int errors)
    {
        Interlocked.Increment(ref _persistenceFlushes);
        Interlocked.Add(ref _totalPersistenceDurationMs, durationMs);
        if (errors > 0) Interlocked.Add(ref _persistenceErrors, errors);
    }

    public void RecordMessageProcessingTime(long ticks)
    {
         Interlocked.Add(ref _totalMessageProcessingTimeTicks, ticks);
    }

    public void RecordWorldLoopTick(long durationMs, int queueDepth)
    {
        Interlocked.Increment(ref _worldLoopTicks);
        Interlocked.Add(ref _worldLoopTotalDurationMs, durationMs);
        Interlocked.Exchange(ref _worldSchedulerQueueDepth, queueDepth);
    }

    public void RecordSlowWorldTick(long durationMs)
    {
        Interlocked.Increment(ref _worldLoopSlowTicks);
    }

    public void RecordSlowWorldTask(string taskName, long durationMs)
    {
        Interlocked.Increment(ref _worldLoopSlowTasks);
    }

    public void RecordTriggerExecuted(string triggerType)
    {
        Interlocked.Increment(ref _triggersExecuted);
    }

    public void RecordPipelineValidateDuration(long ticks)
    {
        Interlocked.Add(ref _pipelineValidateDurationTicks, ticks);
    }

    public void RecordPipelineResolveDuration(long ticks)
    {
        Interlocked.Add(ref _pipelineResolveDurationTicks, ticks);
    }

    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            NetBytesReceived = Interlocked.Read(ref _netBytesReceived),
            NetBytesSent = Interlocked.Read(ref _netBytesSent),
            NetMessagesProcessed = Interlocked.Read(ref _netMessagesProcessed),
            NetErrors = Interlocked.Read(ref _netErrors),
            ValidationErrors = Interlocked.Read(ref _validationErrors),
            PersistenceFlushes = Interlocked.Read(ref _persistenceFlushes),
            PersistenceErrors = Interlocked.Read(ref _persistenceErrors),
            TotalPersistenceDurationMs = Interlocked.Read(ref _totalPersistenceDurationMs),
            TotalMessageProcessingTimeTicks = Interlocked.Read(ref _totalMessageProcessingTimeTicks),

            WorldLoopTicks = Interlocked.Read(ref _worldLoopTicks),
            WorldLoopTotalDurationMs = Interlocked.Read(ref _worldLoopTotalDurationMs),
            WorldSchedulerQueueDepth = Interlocked.Read(ref _worldSchedulerQueueDepth),
            WorldLoopSlowTicks = Interlocked.Read(ref _worldLoopSlowTicks),
            WorldLoopSlowTasks = Interlocked.Read(ref _worldLoopSlowTasks),
            TriggersExecuted = Interlocked.Read(ref _triggersExecuted),

            PipelineValidateDurationTicks = Interlocked.Read(ref _pipelineValidateDurationTicks),
            PipelineResolveDurationTicks = Interlocked.Read(ref _pipelineResolveDurationTicks)
        };
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _netBytesReceived, 0);
        Interlocked.Exchange(ref _netBytesSent, 0);
        Interlocked.Exchange(ref _netMessagesProcessed, 0);
        Interlocked.Exchange(ref _netErrors, 0);
        Interlocked.Exchange(ref _validationErrors, 0);
        Interlocked.Exchange(ref _persistenceFlushes, 0);
        Interlocked.Exchange(ref _persistenceErrors, 0);
        Interlocked.Exchange(ref _totalPersistenceDurationMs, 0);
        Interlocked.Exchange(ref _totalMessageProcessingTimeTicks, 0);

        Interlocked.Exchange(ref _worldLoopTicks, 0);
        Interlocked.Exchange(ref _worldLoopTotalDurationMs, 0);
        Interlocked.Exchange(ref _worldSchedulerQueueDepth, 0);
        Interlocked.Exchange(ref _worldLoopSlowTicks, 0);
        Interlocked.Exchange(ref _worldLoopSlowTasks, 0);
        Interlocked.Exchange(ref _triggersExecuted, 0);

        Interlocked.Exchange(ref _pipelineValidateDurationTicks, 0);
        Interlocked.Exchange(ref _pipelineResolveDurationTicks, 0);
    }
}

public class MetricsSnapshot
{
    public long NetBytesReceived { get; set; }
    public long NetBytesSent { get; set; }
    public long NetMessagesProcessed { get; set; }
    public long NetErrors { get; set; }
    public long ValidationErrors { get; set; }
    public long PersistenceFlushes { get; set; }
    public long PersistenceErrors { get; set; }
    public long TotalPersistenceDurationMs { get; set; }
    public long TotalMessageProcessingTimeTicks { get; set; }

    public long WorldLoopTicks { get; set; }
    public long WorldLoopTotalDurationMs { get; set; }
    public long WorldSchedulerQueueDepth { get; set; }
    public long WorldLoopSlowTicks { get; set; }
    public long WorldLoopSlowTasks { get; set; }
    public long TriggersExecuted { get; set; }

    public long PipelineValidateDurationTicks { get; set; }
    public long PipelineResolveDurationTicks { get; set; }

    public double AverageMessageProcessingTimeMs => NetMessagesProcessed > 0
        ? TimeSpan.FromTicks(TotalMessageProcessingTimeTicks).TotalMilliseconds / NetMessagesProcessed
        : 0;

    public double AveragePersistenceFlushTimeMs => PersistenceFlushes > 0
        ? (double)TotalPersistenceDurationMs / PersistenceFlushes
        : 0;

    public double AverageWorldLoopDurationMs => WorldLoopTicks > 0
        ? (double)WorldLoopTotalDurationMs / WorldLoopTicks
        : 0;

    public double AverageValidateTimeMs => NetMessagesProcessed > 0
        ? TimeSpan.FromTicks(PipelineValidateDurationTicks).TotalMilliseconds / NetMessagesProcessed
        : 0;

    public double AverageResolveTimeMs => NetMessagesProcessed > 0
        ? TimeSpan.FromTicks(PipelineResolveDurationTicks).TotalMilliseconds / NetMessagesProcessed
        : 0;

    public override string ToString()
    {
        return $"[Metrics] Net: {NetMessagesProcessed} msgs, AvgProc: {AverageMessageProcessingTimeMs:F2}ms (Val: {AverageValidateTimeMs:F2}ms, Res: {AverageResolveTimeMs:F2}ms). " +
               $"World: {WorldLoopTicks} ticks (Avg {AverageWorldLoopDurationMs:F2}ms), Queue: {WorldSchedulerQueueDepth}, SlowTicks: {WorldLoopSlowTicks}, SlowTasks: {WorldLoopSlowTasks}. " +
               $"Triggers: {TriggersExecuted}. " +
               $"Persist: {PersistenceFlushes} flushes, {PersistenceErrors} errs.";
    }
}
