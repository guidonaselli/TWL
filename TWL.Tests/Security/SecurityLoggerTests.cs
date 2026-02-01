using System.Diagnostics;
using TWL.Server.Security;

namespace TWL.Tests.Security;

public class SecurityLoggerTests
{
    private const string LogFile = "security_audit.log";

    [Fact]
    public async Task LogSecurityEvent_ShouldBeNonBlocking()
    {
        // Clean up previous runs if possible (not thread safe if other tests run in parallel)
        // Given it's a file, we might just append and check content.

        var correlationId = Guid.NewGuid().ToString();
        var sw = Stopwatch.StartNew();

        // Act
        SecurityLogger.LogSecurityEvent("TestEvent", 999, "PerformanceCheck", correlationId);

        sw.Stop();

        // Assert: It should be extremely fast (microsecond/millisecond range)
        // < 5ms is a very safe upper bound for channel write
        Assert.True(sw.ElapsedMilliseconds < 50, $"Logging took too long: {sw.ElapsedMilliseconds}ms");

        // Verify it was written (Eventual consistency)
        var maxRetries = 20;
        var found = false;
        for (var i = 0; i < maxRetries; i++)
        {
            await Task.Delay(100);
            if (File.Exists(LogFile))
            {
                // Read with sharing to avoid lock contention with the logger itself
                using var fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                var content = await reader.ReadToEndAsync();
                if (content.Contains(correlationId))
                {
                    found = true;
                    break;
                }
            }
        }

        Assert.True(found, "Log entry was not flushed to disk within 2 seconds.");
    }
}
