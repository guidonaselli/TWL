using System.Collections.Concurrent;
using System.Diagnostics;
using TWL.Server.Architecture.Observability;
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
    private readonly IPlayerRepository _repo;
    private readonly ServerMetrics _serverMetrics;
    private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public PlayerService(IPlayerRepository repo, ServerMetrics serverMetrics)
    {
        _repo = repo;
        _serverMetrics = serverMetrics;
    }

    public PersistenceMetrics Metrics { get; } = new();

    public virtual int ActiveSessionCount => _sessions.Count;
    public virtual int DirtySessionCount => _sessions.Values.Count(s => (s.Character != null && s.Character.IsDirty) || (s.QuestComponent != null && s.QuestComponent.IsDirty));

    public virtual void Start()
    {
        _cts = new CancellationTokenSource();
        _backgroundTask = Task.Run(async () => await FlushLoopAsync(_cts.Token));
        PersistenceLogger.LogEvent("ServiceStart", "PlayerService started (persistence).");
    }

    public virtual async Task StopAsync()
    {
        _cts?.Cancel();
        try
        {
            if (_backgroundTask != null)
            {
                await _backgroundTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
        }
        catch (TimeoutException)
        {
            // Ignore timeout
        }
        catch (Exception ex)
        {
            PersistenceLogger.LogEvent("ServiceStopError", ex.Message, errors: 1);
        }

        await FlushAllDirtyAsync();
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
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                PersistenceLogger.LogEvent("FlushLoopError", ex.Message, errors: 1);
            }
        }
    }

    public virtual async Task DisconnectAllAsync(string reason)
    {
        var sessions = _sessions.Values.ToList();
        if (sessions.Count == 0) return;

        PersistenceLogger.LogEvent("DisconnectAll", $"Disconnecting {sessions.Count} sessions. Reason: {reason}");

        await Parallel.ForEachAsync(sessions, async (session, token) =>
        {
            await session.DisconnectAsync(reason);
        });
    }

    public async Task FlushAllDirtyAsync()
    {
        var flushId = Guid.NewGuid().ToString();
        var sw = Stopwatch.StartNew();

        var dirtySessions = _sessions.Values
            .Where(s => s.Character != null && (s.Character.IsDirty || s.QuestComponent.IsDirty))
            .ToList();

        if (dirtySessions.Count == 0)
        {
            return;
        }

        var savedCount = 0;
        var errorCount = 0;

        // Limit concurrency to avoid thread pool starvation
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 20 };

        await Parallel.ForEachAsync(dirtySessions, parallelOptions, async (session, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

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

            _serverMetrics.RecordPersistenceFlush(sw.ElapsedMilliseconds, errorCount);

            PersistenceLogger.LogEvent("FlushComplete", "Batch flush finished", savedCount, sw.ElapsedMilliseconds,
                errorCount);
            PipelineLogger.LogStage(flushId, "PersistBatch", sw.Elapsed.TotalMilliseconds,
                $"Count:{savedCount} Errors:{errorCount}");
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

    public void UnregisterSession(int userId) => _sessions.TryRemove(userId, out _);

    public virtual ClientSession? GetSession(int userId)
    {
        _sessions.TryGetValue(userId, out var session);
        return session;
    }

    public virtual ClientSession? GetSessionByUserId(int userId) => GetSession(userId);

    public virtual ClientSession? GetSessionByName(string name)
    {
        return _sessions.Values.FirstOrDefault(s =>
            s.Character != null &&
            string.Equals(s.Character.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public virtual IEnumerable<ClientSession> GetAllSessions() => _sessions.Values;

    public virtual void GetSessions(List<ClientSession> buffer, Func<ClientSession, bool>? filter = null)
    {
        buffer.Clear();
        foreach (var kvp in _sessions)
        {
            var session = kvp.Value;
            if (filter == null || filter(session))
            {
                buffer.Add(session);
            }
        }
    }

    public PlayerSaveData? LoadData(int userId)
    {
        // Keeping synchronous LoadData for compatibility if needed
        return _repo.Load(userId);
    }

    public async Task<PlayerSaveData?> LoadDataAsync(int userId) => await _repo.LoadAsync(userId);

    public void SaveSession(ClientSession session) => SaveSessionAsync(session).GetAwaiter().GetResult();

    public async Task SaveSessionAsync(ClientSession session)
    {
        if (session.Character == null)
        {
            return;
        }

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