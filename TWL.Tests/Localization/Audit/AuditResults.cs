using System.Collections.Generic;

namespace TWL.Tests.Localization.Audit
{
    public class AuditResults
    {
        public Dictionary<string, List<string>> ResourceKeys { get; set; } = new();
        public UsedKeys UsedKeys { get; set; } = new();
        public List<HardcodedString> HardcodedStrings { get; set; } = new();
        public List<AuditFinding> Findings { get; set; } = new();
    }

    public class UsedKeys
    {
        public List<string> FromContent { get; set; } = new();
        public List<string> FromCode { get; set; } = new();
        public Dictionary<string, List<int>> TfArgCounts { get; set; } = new();
    }

    public class HardcodedString
    {
        public required string File { get; set; }
        public int Line { get; set; }
        public required string Snippet { get; set; }
        public required string Reason { get; set; }
    }

    public class AuditFinding
    {
        public required string Code { get; set; }
        public required string Severity { get; set; } // ERROR, WARN, INFO
        public required string File { get; set; }
        public required string Location { get; set; }
        public required string Message { get; set; }
        public required string SuggestedFix { get; set; }
    }
}
