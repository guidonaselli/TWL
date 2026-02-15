using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TWL.Server.Services;
using TWL.Server.Simulation.Managers;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class WorldSchedulerBenchmark
{
    private readonly ITestOutputHelper _output;

    public WorldSchedulerBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public void Benchmark_ProcessTick_ManyTasks()
    {
        // Setup
        var logger = new NullLogger<WorldScheduler>();
        var metrics = new ServerMetrics();
        using var scheduler = new WorldScheduler(logger, metrics);

        const int taskCount = 100000;
        var random = new Random(42);

        // Schedule tasks with random delays
        for (var i = 0; i < taskCount; i++)
        {
            var delay = random.Next(1, 1000); // 1 to 1000 ticks
            scheduler.Schedule(() => { }, delay);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        const int iterations = 100; // Process 100 ticks

        for (var i = 0; i < iterations; i++)
        {
            scheduler.ProcessTick();
        }

        sw.Stop();

        _output.WriteLine($"Processed {iterations} ticks with {taskCount} tasks initially.");
        _output.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Average Time per Tick: {sw.ElapsedMilliseconds / (double)iterations} ms");
    }
}
