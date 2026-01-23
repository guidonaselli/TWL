using System;

namespace TWL.Server.Security;

public static class SecurityLogger
{
    public static void LogSecurityEvent(string eventType, int userId, string details, string correlationId = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var cid = correlationId ?? Guid.NewGuid().ToString();
        // Structured-ish format
        var logEntry = $"[SECURITY] [{timestamp}] [CID:{cid}] [UID:{userId}] [Event:{eventType}] {details}";

        Console.WriteLine(logEntry);
    }
}
