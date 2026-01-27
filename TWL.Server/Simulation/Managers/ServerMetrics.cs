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
            TotalMessageProcessingTimeTicks = Interlocked.Read(ref _totalMessageProcessingTimeTicks)
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

    public double AverageMessageProcessingTimeMs => NetMessagesProcessed > 0
        ? TimeSpan.FromTicks(TotalMessageProcessingTimeTicks).TotalMilliseconds / NetMessagesProcessed
        : 0;

    public double AveragePersistenceFlushTimeMs => PersistenceFlushes > 0
        ? (double)TotalPersistenceDurationMs / PersistenceFlushes
        : 0;

    public override string ToString()
    {
        return $"[Metrics] Net: {NetMessagesProcessed} msgs ({NetBytesReceived} B in / {NetBytesSent} B out), " +
               $"AvgProcess: {AverageMessageProcessingTimeMs:F2}ms, Errors: {NetErrors} net / {ValidationErrors} val. " +
               $"Persist: {PersistenceFlushes} flushes (Avg {AveragePersistenceFlushTimeMs:F2}ms), {PersistenceErrors} errs.";
    }
}
