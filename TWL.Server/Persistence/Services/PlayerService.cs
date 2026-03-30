using System.Collections.Concurrent;
using System.Diagnostics;
using TWL.Server.Architecture.Observability;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;

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

    public virtual PlayerSaveData? LoadData(int userId)
    {
        // Keeping synchronous LoadData for compatibility if needed
        return _repo.Load(userId);
    }

    public virtual async Task<PlayerSaveData?> LoadDataAsync(int userId) => await _repo.LoadAsync(userId);

    public virtual async Task<IEnumerable<PlayerSaveData>> LoadDataBatchAsync(IEnumerable<int> userIds) => await _repo.LoadBatchAsync(userIds);

    public virtual void SaveSession(ClientSession session) => SaveSessionAsync(session).GetAwaiter().GetResult();

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

    /// <summary>
    /// Returns an item to a player's inventory, handling both online and offline cases.
    /// Used by the market system when listings expire or are cancelled for offline users.
    /// </summary>
    public virtual async Task<bool> ReturnMarketItemAsync(int userId, int itemId, int quantity)
    {
        // 1. Check if the player is online
        var session = GetSession(userId);
        if (session?.Character != null)
        {
            // Player is online, add to active character (Thread-safe)
            return session.Character.AddItem(itemId, quantity);
        }

        // 2. Player is offline, load their data from persistence
        var saveData = await LoadDataAsync(userId);
        if (saveData == null)
        {
            PersistenceLogger.LogEvent("ReturnItemError", $"UserId:{userId} ItemId:{itemId} - Player data not found.", errors: 1);
            return false;
        }

        // 3. Add to inventory (handling basic stacking)
        // Note: For offline users we don't strictly enforce MaxInventorySlots here to prevent item loss,
        // but we follow basic stacking rules.
        var existing = saveData.Character.Inventory.Find(i => i.ItemId == itemId && i.Policy == BindPolicy.Unbound && i.BoundToId == null);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            saveData.Character.Inventory.Add(new Item
            {
                ItemId = itemId,
                Quantity = quantity,
                Policy = BindPolicy.Unbound,
                BoundToId = null
            });
        }

        saveData.LastSaved = DateTime.UtcNow;

        // 4. Persist the change
        try
        {
            await _repo.SaveAsync(userId, saveData);
            PersistenceLogger.LogEvent("ReturnItemOffline", $"UserId:{userId} ItemId:{itemId} Qty:{quantity} - Success.");
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogger.LogEvent("ReturnItemError", $"UserId:{userId} ItemId:{itemId} - {ex.Message}", errors: 1);
            return false;
        }
        }

        /// <summary>
        /// Adds gold to a player's balance, handling both online and offline cases.
        /// Used by the market system when listings are sold to credit the seller.
        /// </summary>
        public virtual async Task<bool> AddGoldAsync(int userId, int amount)
        {
        // 1. Check if the player is online
        var session = GetSession(userId);
        if (session?.Character != null)
        {
            // Player is online, add to active character (Thread-safe)
            session.Character.AddGold(amount);
            return true;
        }

        // 2. Player is offline, load their data from persistence
        var saveData = await LoadDataAsync(userId);
        if (saveData == null)
        {
            PersistenceLogger.LogEvent("AddGoldError", $"UserId:{userId} Amount:{amount} - Player data not found.", errors: 1);
            return false;
        }

        // 3. Add to gold
        saveData.Character.Gold += amount;
        saveData.LastSaved = DateTime.UtcNow;

        // 4. Persist the change
        try
        {
            await _repo.SaveAsync(userId, saveData);
            PersistenceLogger.LogEvent("AddGoldOffline", $"UserId:{userId} Amount:{amount} - Success.");
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogger.LogEvent("AddGoldError", $"UserId:{userId} Amount:{amount} - {ex.Message}", errors: 1);
            return false;
        }
        }
        }