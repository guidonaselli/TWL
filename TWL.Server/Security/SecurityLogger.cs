using System.Threading.Channels;

namespace TWL.Server.Security;

public static class SecurityLogger
{
    private const string LogFile = "security_audit.log";
    private static readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();
    private static readonly Task _writeTask;
    private static readonly CancellationTokenSource _cts = new();

    static SecurityLogger()
    {
        _writeTask = Task.Run(ProcessLogQueue);
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
    }

    public static void LogSecurityEvent(string eventType, int userId, string details, string correlationId = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var cid = correlationId ?? Guid.NewGuid().ToString();
        // Structured-ish format
        var logEntry = $"[SECURITY] [{timestamp}] [CID:{cid}] [UID:{userId}] [Event:{eventType}] {details}";

        // Non-blocking write
        _logChannel.Writer.TryWrite(logEntry);
    }

    private static async Task ProcessLogQueue()
    {
        try
        {
            // Ensure file exists or create it
            using var fileStream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream) { AutoFlush = true };

            while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_logChannel.Reader.TryRead(out var entry))
                {
                    await writer.WriteLineAsync(entry);
                    // Console.WriteLine is executed in background, so it doesn't block the hot path
                    Console.WriteLine(entry);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SECURITY] CRITICAL LOGGING FAILURE: {ex.Message}");
        }
    }

    public static void Shutdown()
    {
        _cts.Cancel();
        _logChannel.Writer.TryComplete();
        try
        {
            _writeTask.Wait(1000);
        }
        catch
        {
            // Ignore errors during shutdown
        }
    }
}
