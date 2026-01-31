using System;
using System.IO;

namespace TWL.Server.Security;

public static class SecurityLogger
{
    private static readonly object _lock = new();
    private const string LogFile = "security_audit.log";

    public static void LogSecurityEvent(string eventType, int userId, string details, string correlationId = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var cid = correlationId ?? Guid.NewGuid().ToString();
        // Structured-ish format
        var logEntry = $"[SECURITY] [{timestamp}] [CID:{cid}] [UID:{userId}] [Event:{eventType}] {details}";

        Console.WriteLine(logEntry);

        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFile, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SECURITY] FAILED TO WRITE LOG: {ex.Message}");
        }
    }
}
