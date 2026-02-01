using System.Diagnostics;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Models;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class PlayerPersistenceBenchmark : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly FilePlayerRepository _repo;
    private readonly string _tempDir;

    public PlayerPersistenceBenchmark(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "TWL_Benchmark_" + Guid.NewGuid());
        _repo = new FilePlayerRepository(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
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