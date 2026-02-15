using System.Diagnostics;
using TWL.Server.Simulation.Networking;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class InventoryPropertyBenchmark
{
    private readonly ITestOutputHelper _output;

    public InventoryPropertyBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_InventoryProperty_Access()
    {
        var character = new ServerCharacter();
        character.MaxInventorySlots = 1000;

        // Fill inventory with some items
        for (var i = 0; i < 500; i++)
        {
            character.AddItem(i, 1);
        }

        var stopwatch = Stopwatch.StartNew();

        var iterations = 10000;
        int countSum = 0;
        for (var i = 0; i < iterations; i++)
        {
            var inv = character.Inventory;
            countSum += inv.Count;
        }

        stopwatch.Stop();
        _output.WriteLine($"Inventory property access x {iterations} took {stopwatch.ElapsedMilliseconds} ms. Sum: {countSum}");
    }
}
