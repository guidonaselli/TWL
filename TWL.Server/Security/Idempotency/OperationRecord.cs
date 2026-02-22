namespace TWL.Server.Security.Idempotency;

public enum OperationState
{
    Pending,
    Completed,
    Failed
}

public class OperationRecord
{
    public string OperationKey { get; set; } = string.Empty;
    public int UserId { get; set; }
    public OperationState State { get; set; }
    public DateTime Timestamp { get; set; }
    public object? SemanticResult { get; set; } // Optional: to return the same result from a previously successful operation
}
