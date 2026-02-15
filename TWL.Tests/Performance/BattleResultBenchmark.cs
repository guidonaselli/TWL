using System.Diagnostics;
using TWL.Shared.Domain.Events;
using TWL.Shared.Domain.Models;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class BattleResultBenchmark
{
    private readonly ITestOutputHelper _output;

    public BattleResultBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BenchmarkStringAllocations()
    {
        // Setup
        var loot = new List<Item>
        {
            new Item { Name = "Sword", ItemId = 1 },
            new Item { Name = "Shield", ItemId = 2 },
            new Item { Name = "Potion", ItemId = 3 },
            new Item { Name = "Gold", ItemId = 4 },
            new Item { Name = "Experience Scroll", ItemId = 5 }
        };
        var result = new BattleFinished(true, 1500, loot);

        var iterations = 100_000;
        var stopwatch = Stopwatch.StartNew();

        long totalLength = 0;
        for (var i = 0; i < iterations; i++)
        {
            var msg = result.Victory ? "YOU WON! Press Enter" : "YOU LOST... Press Enter";
            if (result.Victory)
            {
                msg += $"\nEXP Gained: {result.ExpGained}";
                if (result.Loot.Count > 0)
                {
                    msg += "\nLoot: " + string.Join(", ", result.Loot.Select(i => i.Name));
                }
            }
            totalLength += msg.Length;
        }

        stopwatch.Stop();
        _output.WriteLine($"[BENCHMARK] Time taken for {iterations} iterations: {stopwatch.ElapsedMilliseconds} ms");

        // Simple assertion to keep test happy
        Assert.True(totalLength > 0);
    }
}
