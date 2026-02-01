using System.Diagnostics;
using System.Text;
using TWL.Server.Simulation.Managers;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class EconomyLedgerBenchmark
{
    private readonly ITestOutputHelper _output;

    public EconomyLedgerBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    private string GenerateLedger(int entries)
    {
        var tempFile = Path.GetTempFileName();
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Type,UserId,Details,Delta,NewBalance");

        var startDate = DateTime.UtcNow.AddDays(-30);

        for (var i = 0; i < entries; i++)
        {
            var timestamp = startDate.AddMinutes(i).ToString("O");
            var type = i % 2 == 0 ? "PurchaseVerify" : "ShopBuy";
            var userId = 1000 + i % 100;
            var orderId = Guid.NewGuid().ToString("N");
            var details = "";
            var delta = 0;
            var balance = 1000;

            if (type == "PurchaseVerify")
            {
                details = $"Order:{orderId}, Product:gems_100";
                delta = 100;
            }
            else
            {
                details = $"ShopItem:1, Item:101 x1, OrderId:{orderId}";
                delta = -10;
            }

            sb.AppendLine($"{timestamp},{type},{userId},{details},{delta},{balance}");
        }

        File.WriteAllText(tempFile, sb.ToString());
        return tempFile;
    }

    [Fact]
    public void Benchmark_ReplayLedger()
    {
        var entries = 100000; // 100k entries to make it significant
        var ledgerFile = GenerateLedger(entries);

        try
        {
            // Warmup (optional, but good to load JIT)
            // new EconomyManager(ledgerFile);
            // Actually, we want to measure cold start or just the processing.
            // Since ReadAllLines loads into memory, let's just run it once.

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetAllocatedBytesForCurrentThread();

            var sw = Stopwatch.StartNew();

            using (var manager = new EconomyManager(ledgerFile))
            {
                sw.Stop();
                var finalMemory = GC.GetAllocatedBytesForCurrentThread();
                var allocated = finalMemory - initialMemory;

                _output.WriteLine($"Entries: {entries}");
                _output.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
                _output.WriteLine($"Allocated: {allocated / 1024.0 / 1024.0:F2} MB");

                Assert.True(manager.Metrics.ReplayedTransactionCount > 0);
            }
        }
        finally
        {
            if (File.Exists(ledgerFile))
            {
                File.Delete(ledgerFile);
            }
        }
    }
}