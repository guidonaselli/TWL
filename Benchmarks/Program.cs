using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Benchmarks;
using TWL.Server.Simulation.Managers;

public class Program
{
    private const string LedgerFile = "benchmark_economy.log";
    private const int Iterations = 1000;
    private const int ThreadCount = 20;

    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0].ToLower() == "load")
        {
            var test = new LoadTest();
            await test.RunAsync();
            return;
        }

        if (args.Length > 0 && args[0].ToLower() == "cleanup")
        {
            await RunCleanupBenchmark();
            return;
        }

        Console.WriteLine($"Running EconomyManager benchmarks with {ThreadCount} threads, {Iterations} iterations each.");
        Console.WriteLine("Pass 'load' argument to run Full Stack Load Test. Pass 'cleanup' to run Cleanup Benchmark.");

        if (File.Exists(LedgerFile)) File.Delete(LedgerFile);

        var manager = new EconomyManager(LedgerFile);

        Console.WriteLine("Measuring...");
        var elapsed = Measure(manager);
        Console.WriteLine($"Time: {elapsed} ms");

        // Dispose if implemented (for the second run)
        if (manager is IDisposable d)
        {
            d.Dispose();
            Console.WriteLine("Disposed manager.");
        }
    }

    private static long Measure(EconomyManager manager)
    {
        var tasks = new Task[ThreadCount];
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < ThreadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < Iterations; j++)
                {
                    // Use unique UserID to bypass RateLimit (10 per user per minute)
                    // threadId * 10000 + j ensure unique IDs across threads and iterations
                    manager.InitiatePurchase(threadId * 10000 + j, "gems_100");
                }
            });
        }

        Task.WaitAll(tasks);
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static async Task RunCleanupBenchmark()
    {
        Console.WriteLine("Running EconomyManager Cleanup Benchmark...");
        var ledgerFile = "cleanup_economy.log";
        if (File.Exists(ledgerFile)) File.Delete(ledgerFile);
        if (File.Exists(ledgerFile + ".snapshot.json")) File.Delete(ledgerFile + ".snapshot.json");

        // 1. Generate expired transactions
        Console.WriteLine("Generating expired transactions...");
        using (var writer = new StreamWriter(ledgerFile))
        {
            writer.WriteLine("Timestamp,Type,UserId,Details,Delta,NewBalance");
            var expiredTime = DateTime.UtcNow.AddMinutes(-20).ToString("O");
            for (int i = 0; i < 50000; i++)
            {
                writer.WriteLine($"{expiredTime},PurchaseIntent,{i},Order:ord_{i}, Product:gems_100,0,0");
            }
        }

        // 2. Load Manager
        Console.WriteLine("Initializing Manager (loading ledger)...");
        var manager = new EconomyManager(ledgerFile);

        // 3. Trigger Cleanup
        // Cleanup happens every 100 calls.
        // We will call InitiatePurchase 200 times.
        Console.WriteLine("Triggering purchases...");
        long maxTime = 0;
        long totalTime = 0;

        // Use a unique base user ID to avoid collisions if re-run quickly, though file delete handles it.
        int baseUserId = 100000;

        for (int i = 1; i <= 200; i++)
        {
            var sw = Stopwatch.StartNew();
            manager.InitiatePurchase(baseUserId + i, "gems_100");
            sw.Stop();
            var ticks = sw.ElapsedTicks;

            totalTime += ticks;
            if (ticks > maxTime) maxTime = ticks;

            if (i % 50 == 0) Console.WriteLine($"Processed {i} requests.");
        }

        Console.WriteLine($"Max Time: {maxTime / (double)TimeSpan.TicksPerMillisecond:F4} ms");
        Console.WriteLine($"Avg Time: {(totalTime / 200.0) / (double)TimeSpan.TicksPerMillisecond:F4} ms");

        if (manager is IDisposable d) d.Dispose();

        // Cleanup files
        if (File.Exists(ledgerFile)) File.Delete(ledgerFile);
        if (File.Exists(ledgerFile + ".snapshot.json")) File.Delete(ledgerFile + ".snapshot.json");
    }
}
