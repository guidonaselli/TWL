using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Persistence.Services;

public class PlayerService
{
    private readonly IPlayerRepository _repo;
    private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
    private CancellationTokenSource _cts;
    private Task _backgroundTask;

    public PlayerService(IPlayerRepository repo)
    {
        _repo = repo;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _backgroundTask = Task.Run(async () => await FlushLoopAsync(_cts.Token));
        Console.WriteLine("PlayerService started (persistence).");
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _backgroundTask?.Wait(2000);
        }
        catch { }

        FlushAllDirty();
        Console.WriteLine("PlayerService stopped and flushed.");
    }

    private async Task FlushLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                FlushAllDirty();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in flush loop: {ex}");
            }
        }
    }

    public void FlushAllDirty()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                if (session.Character != null && (session.Character.IsDirty || session.QuestComponent.IsDirty))
                {
                    SaveSession(session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session {session.UserId}: {ex}");
            }
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

    public PlayerSaveData? LoadData(int userId)
    {
        return _repo.Load(userId);
    }

    public void SaveSession(ClientSession session)
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

        _repo.Save(session.UserId, saveData);

        session.Character.IsDirty = false;
        session.QuestComponent.IsDirty = false;

        // Console.WriteLine($"Saved session for user {session.UserId}."); // Verbose
    }
}
