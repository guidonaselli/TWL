using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit.Runners;

class Program
{
    static int Main(string[] args)
    {
        var testAssemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TWL.Tests.dll");
        if (!File.Exists(testAssemblyPath))
        {
            Console.WriteLine($"Could not find test assembly at {testAssemblyPath}");
            return 1;
        }

        Console.WriteLine($"Running tests in {testAssemblyPath}");

        using var runner = AssemblyRunner.WithoutAppDomain(testAssemblyPath);

        var finished = new ManualResetEvent(false);
        int failed = 0;

        runner.OnDiscoveryComplete = info => Console.WriteLine($"Discovered {info.TestCasesToRun} tests");
        runner.OnExecutionComplete = info => {
            Console.WriteLine($"Finished: {info.TotalTests} tests, {info.TestsFailed} failed, {info.TestsSkipped} skipped");
            failed = info.TestsFailed;
            finished.Set();
        };
        runner.OnTestFailed = info => Console.WriteLine($"[FAIL] {info.TestDisplayName}: {info.ExceptionMessage}");
        runner.OnTestPassed = info => Console.WriteLine($"[PASS] {info.TestDisplayName}");

        Console.WriteLine("Starting tests...");
        runner.Start();

        finished.WaitOne();

        return failed;
    }
}
