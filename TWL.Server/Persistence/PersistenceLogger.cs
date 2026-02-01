namespace TWL.Server.Persistence;

public static class PersistenceLogger
{
    public static void LogEvent(string eventType, string details, int? count = null, long? durationMs = null,
        int? errors = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        // Structured-ish format
        // [PERSISTENCE] [Time] [Event] Details | Count:X | Duration:Yms | Errors:Z
        var logEntry = $"[PERSISTENCE] [{timestamp}] [Event:{eventType}] {details}";

        if (count.HasValue)
        {
            logEntry += $" | Count:{count}";
        }

        if (durationMs.HasValue)
        {
            logEntry += $" | Duration:{durationMs}ms";
        }

        if (errors.HasValue)
        {
            logEntry += $" | Errors:{errors}";
        }

        Console.WriteLine(logEntry);
    }
}