using System.Diagnostics;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Models;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

/// <summary>
/// In-memory repository for persistence benchmarking.
/// Simulates I/O delay without filesystem or DB dependencies.
/// </summary>
internal class InMemoryBenchmarkRepository : IPlayerRepository
{
    private readonly Dictionary<int, PlayerSaveData> _store = new();

    public Task SaveAsync(int userId, PlayerSaveData data)
    {
        _store[userId] = data;
        return Task.CompletedTask;
    }

    public PlayerSaveData? Load(int userId) =>
        _store.TryGetValue(userId, out var data) ? data : null;

    public Task<PlayerSaveData?> LoadAsync(int userId) =>
        Task.FromResult(Load(userId));
}

public class PlayerPersistenceBenchmark
{
    private readonly ITestOutputHelper _output;
    private readonly InMemoryBenchmarkRepository _repo;

    public PlayerPersistenceBenchmark(ITestOutputHelper output)
    {
        _output = output;
        _repo = new InMemoryBenchmarkRepository();
    }

    private PlayerSaveData CreateDummyData()
    {
        return new PlayerSaveData
        {
            Character = new ServerCharacterData
            {
                Id = 1,
                Name = "BenchmarkUser",
                Level = 10,
                Exp = 12345,
                Hp = 100,
                Inventory = new List<Item>
                {
                    new() { ItemId = 1, Quantity = 10 },
                    new() { ItemId = 2, Quantity = 5 }
                }
            },
            LastSaved = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task BenchmarkAsyncSave()
    {
        var data = CreateDummyData();
        var iterations = 100;

        // Warmup
        await _repo.SaveAsync(1, data);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            await _repo.SaveAsync(1, data);
        }

        sw.Stop();

        _output.WriteLine($"Async Save ({iterations} iterations): {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Average per save: {sw.Elapsed.TotalMilliseconds / iterations} ms");
    }
}