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

        Console.WriteLine($"Running EconomyManager benchmarks with {ThreadCount} threads, {Iterations} iterations each.");
        Console.WriteLine("Pass 'load' argument to run Full Stack Load Test.");

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
}
