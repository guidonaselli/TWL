using System.Collections.Concurrent;
using System.Diagnostics;
using TWL.Server.Architecture.Observability;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Persistence.Services;

public class PersistenceMetrics
{
    public long LastFlushDurationMs { get; set; }
    public int SessionsSavedInLastFlush { get; set; }
    public int TotalSaveErrors { get; set; }
    public int TotalSessionsSaved { get; set; }
}

public class PlayerService
{
    public PersistenceMetrics Metrics { get; } = new();

    private readonly IPlayerRepository _repo;
    private readonly ServerMetrics _serverMetrics;
    private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
    private CancellationTokenSource _cts;
    private Task _backgroundTask;

    public PlayerService(IPlayerRepository repo, ServerMetrics serverMetrics)
    {
        _repo = repo;
        _serverMetrics = serverMetrics;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _backgroundTask = Task.Run(async () => await FlushLoopAsync(_cts.Token));
        PersistenceLogger.LogEvent("ServiceStart", "PlayerService started (persistence).");
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _backgroundTask?.Wait(2000);
        }
        catch { }

        FlushAllDirtyAsync().GetAwaiter().GetResult();
        PersistenceLogger.LogEvent("ServiceStop", "PlayerService stopped and flushed.");
    }

    private async Task FlushLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                await FlushAllDirtyAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PersistenceLogger.LogEvent("FlushLoopError", ex.Message, errors: 1);
            }
        }
    }

    public async Task FlushAllDirtyAsync()
    {
        var flushId = Guid.NewGuid().ToString();
        var sw = Stopwatch.StartNew();

        var dirtySessions = _sessions.Values
            .Where(s => s.Character != null && (s.Character.IsDirty || s.QuestComponent.IsDirty))
            .ToList();

        if (dirtySessions.Count == 0) return;

        int savedCount = 0;
        int errorCount = 0;

        // Limit concurrency to avoid thread pool starvation
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 20 };

        await Parallel.ForEachAsync(dirtySessions, parallelOptions, async (session, token) =>
        {
            var (success, error, userId) = await ProcessSessionSaveAsync(session);
            if (success)
            {
                Interlocked.Increment(ref savedCount);
            }
            else
            {
                Interlocked.Increment(ref errorCount);
                // Log error immediately, but update metrics later
                PersistenceLogger.LogEvent("SaveError", $"UserId:{userId} {error}", errors: 1);
            }
        });

        sw.Stop();

        if (savedCount > 0 || errorCount > 0)
        {
            Metrics.LastFlushDurationMs = sw.ElapsedMilliseconds;
            Metrics.SessionsSavedInLastFlush = savedCount;
            Metrics.TotalSessionsSaved += savedCount;
            Metrics.TotalSaveErrors += errorCount;

            _serverMetrics?.RecordPersistenceFlush(sw.ElapsedMilliseconds, errorCount);

            PersistenceLogger.LogEvent("FlushComplete", "Batch flush finished", count: savedCount, durationMs: sw.ElapsedMilliseconds, errors: errorCount);
            PipelineLogger.LogStage(flushId, "PersistBatch", sw.Elapsed.TotalMilliseconds, $"Count:{savedCount} Errors:{errorCount}");
        }
    }

    private async Task<(bool Success, string? Error, int UserId)> ProcessSessionSaveAsync(ClientSession session)
    {
        try
        {
            await SaveSessionAsync(session);
            return (true, null, session.UserId);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, session.UserId);
        }
    }

    public void RegisterSession(ClientSession session)
    {
        if (session.UserId > 0)
        {
            _sessions.TryAdd(session.UserId, session);
        }
    }

    public void UnregisterSession(int userId)
    {
        _sessions.TryRemove(userId, out _);
    }

    public virtual ClientSession? GetSession(int userId)
    {
        _sessions.TryGetValue(userId, out var session);
        return session;
    }

    public PlayerSaveData? LoadData(int userId)
    {
        // Keeping synchronous LoadData for compatibility if needed
        return _repo.Load(userId);
    }

    public async Task<PlayerSaveData?> LoadDataAsync(int userId)
    {
        return await _repo.LoadAsync(userId);
    }

    public void SaveSession(ClientSession session)
    {
       SaveSessionAsync(session).GetAwaiter().GetResult();
    }

    public async Task SaveSessionAsync(ClientSession session)
    {
        if (session.Character == null) return;

        var charData = session.Character.GetSaveData();
        var questData = session.QuestComponent.GetSaveData();

        var saveData = new PlayerSaveData
        {
            Character = charData,
            Quests = questData,
            LastSaved = DateTime.UtcNow
        };

        await _repo.SaveAsync(session.UserId, saveData);

        session.Character.IsDirty = false;
        session.QuestComponent.IsDirty = false;
    }
}
