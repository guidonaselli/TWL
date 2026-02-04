namespace TWL.Server.Simulation.Managers;

// Simple Histogram structure
public class PipelineLatencyHistogram
{
    private long _bucket100ms;
    private long _bucket10ms;
    private long _bucket1ms;
    private long _bucket50ms;
    private long _bucket5ms;
    private long _bucketOver100ms;

    public void Record(double ms)
    {
        if (ms < 1)
        {
            Interlocked.Increment(ref _bucket1ms);
        }
        else if (ms < 5)
        {
            Interlocked.Increment(ref _bucket5ms);
        }
        else if (ms < 10)
        {
            Interlocked.Increment(ref _bucket10ms);
        }
        else if (ms < 50)
        {
            Interlocked.Increment(ref _bucket50ms);
        }
        else if (ms < 100)
        {
            Interlocked.Increment(ref _bucket100ms);
        }
        else
        {
            Interlocked.Increment(ref _bucketOver100ms);
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _bucket1ms, 0);
        Interlocked.Exchange(ref _bucket5ms, 0);
        Interlocked.Exchange(ref _bucket10ms, 0);
        Interlocked.Exchange(ref _bucket50ms, 0);
        Interlocked.Exchange(ref _bucket100ms, 0);
        Interlocked.Exchange(ref _bucketOver100ms, 0);
    }

    public HistogramSnapshot GetSnapshot()
    {
        return new HistogramSnapshot
        {
            Bucket1ms = Interlocked.Read(ref _bucket1ms),
            Bucket5ms = Interlocked.Read(ref _bucket5ms),
            Bucket10ms = Interlocked.Read(ref _bucket10ms),
            Bucket50ms = Interlocked.Read(ref _bucket50ms),
            Bucket100ms = Interlocked.Read(ref _bucket100ms),
            BucketOver100ms = Interlocked.Read(ref _bucketOver100ms)
        };
    }
}

public class HistogramSnapshot
{
    public long Bucket1ms { get; set; }
    public long Bucket5ms { get; set; }
    public long Bucket10ms { get; set; }
    public long Bucket50ms { get; set; }
    public long Bucket100ms { get; set; }
    public long BucketOver100ms { get; set; }

    public override string ToString() =>
        $"<1ms:{Bucket1ms}, <5ms:{Bucket5ms}, <10ms:{Bucket10ms}, <50ms:{Bucket50ms}, <100ms:{Bucket100ms}, >100ms:{BucketOver100ms}";
}

public class ServerMetrics
{
    private readonly PipelineLatencyHistogram _persistHistogram = new();
    private readonly PipelineLatencyHistogram _resolveHistogram = new();

    // Histograms
    private readonly PipelineLatencyHistogram _validateHistogram = new();
    private long _netBytesReceived;
    private long _netBytesSent;
    private long _netErrors;
    private long _netMessagesProcessed;
    private long _persistenceErrors;

    private long _persistenceFlushes;
    private long _pipelineResolveDurationTicks;

    private long _pipelineValidateDurationTicks;
    private long _totalMessageProcessingTimeTicks;
    private long _totalPersistenceDurationMs;
    private long _triggersExecuted;

    private long _validationErrors;
    private long _worldLoopSlowTasks;

    private long _worldLoopSlowTicks;

    // New Observability Metrics
    private long _worldLoopTicks;
    private long _worldLoopTotalDurationMs;
    private long _worldSchedulerQueueDepth;

    private long _loginAttempts;
    private long _loginFailures;

    private long _shutdownDurationMs;
    private long _playersSavedOnShutdown;

    public void RecordShutdown(long durationMs, int playersSaved)
    {
        Interlocked.Exchange(ref _shutdownDurationMs, durationMs);
        Interlocked.Exchange(ref _playersSavedOnShutdown, playersSaved);
    }

    public void RecordLoginAttempt(bool success)
    {
        Interlocked.Increment(ref _loginAttempts);
        if (!success)
        {
            Interlocked.Increment(ref _loginFailures);
        }
    }

    public void RecordNetBytesReceived(long bytes) => Interlocked.Add(ref _netBytesReceived, bytes);
    public void RecordNetBytesSent(long bytes) => Interlocked.Add(ref _netBytesSent, bytes);
    public void RecordNetMessageProcessed() => Interlocked.Increment(ref _netMessagesProcessed);
    public void RecordNetError() => Interlocked.Increment(ref _netErrors);

    public void RecordValidationError() => Interlocked.Increment(ref _validationErrors);

    public void RecordPersistenceFlush(long durationMs, int errors)
    {
        Interlocked.Increment(ref _persistenceFlushes);
        Interlocked.Add(ref _totalPersistenceDurationMs, durationMs);
        if (errors > 0)
        {
            Interlocked.Add(ref _persistenceErrors, errors);
        }

        // Also record to histogram
        _persistHistogram.Record(durationMs);
    }

    public void RecordMessageProcessingTime(long ticks) => Interlocked.Add(ref _totalMessageProcessingTimeTicks, ticks);

    public void RecordWorldLoopTick(long durationMs, int queueDepth)
    {
        Interlocked.Increment(ref _worldLoopTicks);
        Interlocked.Add(ref _worldLoopTotalDurationMs, durationMs);
        Interlocked.Exchange(ref _worldSchedulerQueueDepth, queueDepth);
    }

    public void RecordSlowWorldTick(long durationMs) => Interlocked.Increment(ref _worldLoopSlowTicks);

    public void RecordSlowWorldTask(string taskName, long durationMs) => Interlocked.Increment(ref _worldLoopSlowTasks);

    public void RecordTriggerExecuted(string triggerType) => Interlocked.Increment(ref _triggersExecuted);

    public void RecordPipelineValidateDuration(long ticks)
    {
        Interlocked.Add(ref _pipelineValidateDurationTicks, ticks);
        _validateHistogram.Record(TimeSpan.FromTicks(ticks).TotalMilliseconds);
    }

    public void RecordPipelineResolveDuration(long ticks)
    {
        Interlocked.Add(ref _pipelineResolveDurationTicks, ticks);
        _resolveHistogram.Record(TimeSpan.FromTicks(ticks).TotalMilliseconds);
    }

    public void RecordPipelineStageLatency(string stage, long ticks)
    {
        var ms = TimeSpan.FromTicks(ticks).TotalMilliseconds;
        switch (stage.ToLower())
        {
            case "validate":
                _validateHistogram.Record(ms);
                break;
            case "resolve":
                _resolveHistogram.Record(ms);
                break;
            case "persist":
                _persistHistogram.Record(ms);
                break;
        }
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
            PipelineResolveDurationTicks = Interlocked.Read(ref _pipelineResolveDurationTicks),

            LoginAttempts = Interlocked.Read(ref _loginAttempts),
            LoginFailures = Interlocked.Read(ref _loginFailures),

            ShutdownDurationMs = Interlocked.Read(ref _shutdownDurationMs),
            PlayersSavedOnShutdown = Interlocked.Read(ref _playersSavedOnShutdown),

            ValidateHistogram = _validateHistogram.GetSnapshot(),
            ResolveHistogram = _resolveHistogram.GetSnapshot(),
            PersistHistogram = _persistHistogram.GetSnapshot()
        };
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _loginAttempts, 0);
        Interlocked.Exchange(ref _loginFailures, 0);
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

        _validateHistogram.Reset();
        _resolveHistogram.Reset();
        _persistHistogram.Reset();
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

    public long LoginAttempts { get; set; }
    public long LoginFailures { get; set; }

    public long ShutdownDurationMs { get; set; }
    public long PlayersSavedOnShutdown { get; set; }

    public HistogramSnapshot ValidateHistogram { get; set; }
    public HistogramSnapshot ResolveHistogram { get; set; }
    public HistogramSnapshot PersistHistogram { get; set; }

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
        return
            $"[Metrics] Net: {NetMessagesProcessed} msgs, AvgProc: {AverageMessageProcessingTimeMs:F2}ms (Val: {AverageValidateTimeMs:F2}ms, Res: {AverageResolveTimeMs:F2}ms). " +
            $"Logins: {LoginAttempts} (Fail: {LoginFailures}). " +
            $"World: {WorldLoopTicks} ticks (Avg {AverageWorldLoopDurationMs:F2}ms), Queue: {WorldSchedulerQueueDepth}, SlowTicks: {WorldLoopSlowTicks}, SlowTasks: {WorldLoopSlowTasks}. " +
            $"Triggers: {TriggersExecuted}. " +
            $"Persist: {PersistenceFlushes} flushes, {PersistenceErrors} errs. " +
            $"Histograms: [Val: {ValidateHistogram}] [Res: {ResolveHistogram}] [Persist: {PersistHistogram}]";
    }
}