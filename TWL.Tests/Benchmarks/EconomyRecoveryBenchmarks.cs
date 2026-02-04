using System.Diagnostics;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class EconomyRecoveryBenchmarks
{
    private readonly ITestOutputHelper _output;

    public EconomyRecoveryBenchmarks(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_LedgerReplay_vs_Snapshot()
    {
        var ledgerFile = $"bench_ledger_{Guid.NewGuid():N}.log";
        var snapshotFile = Path.ChangeExtension(ledgerFile, ".snapshot.json");
        var transactionCount = 10000;

        try
        {
            // 1. Generate Ledger
            _output.WriteLine($"Generating {transactionCount} transactions...");
            using (var sw = new StreamWriter(ledgerFile))
            {
                sw.WriteLine("Timestamp,Type,UserId,Details,Delta,NewBalance");
                // Ensure all transactions are in the past so snapshot covers them all
                var now = DateTime.UtcNow.AddHours(-4);
                for (int i = 0; i < transactionCount; i++)
                {
                    var orderId = $"ord_{i}";
                    var line = $"{now.AddSeconds(i):O},ShopBuy,1,ShopItem:1, Item:101 x1, OrderId:{orderId},-10,90";
                    sw.WriteLine(line);
                }
            }

            // 2. Measure Cold Start (Full Replay)
            long coldStartMs;
            using (var manager = new EconomyManager(ledgerFile))
            {
                Assert.Equal(transactionCount, manager.Metrics.ActiveTransactions);
                coldStartMs = manager.Metrics.ReplayDurationMs;
                _output.WriteLine($"Cold Start (Replay): {coldStartMs}ms");

                // Create Snapshot
                manager.Dispose();
            }

            Assert.True(File.Exists(snapshotFile));

            // 3. Measure Warm Start (Snapshot)
            long warmStartMs;
            using (var manager = new EconomyManager(ledgerFile))
            {
                var sw = Stopwatch.StartNew();
                // Constructor calls LoadSnapshot
                sw.Stop();
                warmStartMs = sw.ElapsedMilliseconds;

                Assert.Equal(transactionCount, manager.Metrics.ActiveTransactions);
                Assert.Equal(0, manager.Metrics.ReplayedTransactionCount); // Should skip all

                _output.WriteLine($"Warm Start (Snapshot): {warmStartMs}ms");
            }

            Assert.True(warmStartMs < coldStartMs, $"Snapshot load ({warmStartMs}ms) should be faster than replay ({coldStartMs}ms)");
        }
        finally
        {
            if (File.Exists(ledgerFile)) File.Delete(ledgerFile);
            if (File.Exists(snapshotFile)) File.Delete(snapshotFile);
        }
    }
}
