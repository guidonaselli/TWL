using System.Diagnostics;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class InventoryBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public InventoryBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_HasItem_LargeList()
    {
        var character = new ServerCharacter();
        character.MaxInventorySlots = 10000;

        int itemCount = 5000;
        for (int i = 0; i < itemCount; i++)
        {
            character.AddItem(i, 10);
        }

        // Verify count
        Assert.Equal(itemCount, character.Inventory.Count);

        var stopwatch = Stopwatch.StartNew();

        int iterations = 100000;
        for (int i = 0; i < iterations; i++)
        {
            // Check for an item at the end of the list
            character.HasItem(itemCount - 1, 1);
            // Check for a non-existent item (worst case, scans whole list)
            character.HasItem(itemCount + 1, 1);
        }

        stopwatch.Stop();
        _output.WriteLine($"HasItem x {iterations} took {stopwatch.ElapsedMilliseconds} ms");
    }
}
