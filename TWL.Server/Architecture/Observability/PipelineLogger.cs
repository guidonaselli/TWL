using System.Threading.Channels;

namespace TWL.Server.Architecture.Observability;

public static class PipelineLogger
{
    private const string LogFile = "pipeline_audit.log";
    private static readonly Channel<string> _logChannel;

    static PipelineLogger()
    {
        // Unbounded channel to avoid blocking producers (hot path).
        _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Fire and forget writer loop
        _ = Task.Run(WriteLoopAsync);
    }

    public static void LogStage(string traceId, string stage, double durationMs, string details = "")
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var logEntry =
            $"[PIPELINE] [{timestamp}] [TraceID:{traceId}] [Stage:{stage}] Duration:{durationMs:F2}ms {details}";
        _logChannel.Writer.TryWrite(logEntry);
    }

    public static void LogEvent(string traceId, string eventName, string details = "")
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var logEntry = $"[PIPELINE] [{timestamp}] [TraceID:{traceId}] [Event:{eventName}] {details}";
        _logChannel.Writer.TryWrite(logEntry);
    }

    private static async Task WriteLoopAsync()
    {
        try
        {
            // Open with sharing to allow reading while writing
            using var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fs);

            while (await _logChannel.Reader.WaitToReadAsync())
            {
                while (_logChannel.Reader.TryRead(out var msg))
                {
                    await writer.WriteLineAsync(msg);
                }

                await writer.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            // Fallback to console if file fails, but minimally
            Console.Error.WriteLine($"[PIPELINE] FAILED TO WRITE LOG: {ex.Message}");
        }
    }
}