using System;
using System.Reflection;
using Xunit.Runners;

class Program
{
    static int Main(string[] args)
    {
        var testAssembly = Assembly.LoadFrom("TWL.Tests.dll");
        var runner = AssemblyRunner.WithoutAppDomain(testAssembly.Location);
        var reporter = new ConsoleRunnerReporter();
        var options = new TestAssemblyConfiguration();

        Console.WriteLine("Starting test run...");
        runner.OnDiscoveryComplete = (info) => Console.WriteLine($"Discovered {info.TestCasesToRun} test cases.");
        runner.OnExecutionComplete = (info) => Console.WriteLine($"Finished in {info.ExecutionTime} seconds. Tests run: {info.TotalTests}, Failed: {info.TestsFailed}, Skipped: {info.TestsSkipped}.");
        runner.OnTestFailed = (info) => Console.WriteLine($"[FAIL] {info.TestDisplayName}: {info.ExceptionMessage}");
        runner.OnTestPassed = (info) => Console.WriteLine($"[PASS] {info.TestDisplayName}");

        runner.Start(options);

        while(runner.Status != AssemblyRunnerStatus.Idle)
        {
             System.Threading.Thread.Sleep(100);
        }

        return runner.ExecutionSummary.Failed;
    }
}
