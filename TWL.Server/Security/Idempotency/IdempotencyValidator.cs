using System.Collections.Concurrent;

namespace TWL.Server.Security.Idempotency;

public class IdempotencyValidator
{
    // High-performance concurrency-safe store for operation states
    private readonly ConcurrentDictionary<string, OperationRecord> _store = new();
    
    // Limits the lifetime of idempotency records to prevent infinite growth
    private readonly TimeSpan _retentionPeriod;

    public IdempotencyValidator(TimeSpan? retentionPeriod = null)
    {
        _retentionPeriod = retentionPeriod ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Attempts to register a new operation key. 
    /// If it's a new operation, it returns true and marks it as Pending.
    /// If the operation already exists, it retrieves the existing record and returns false.
    /// </summary>
    public bool TryRegisterOperation(string operationKey, int userId, out OperationRecord existingRecord)
    {
        CleanupExpired();

        var record = new OperationRecord
        {
            OperationKey = operationKey,
            UserId = userId,
            State = OperationState.Pending,
            Timestamp = DateTime.UtcNow
        };

        if (_store.TryAdd(operationKey, record))
        {
            existingRecord = record; // Though it's newly created
            return true;
        }

        // Operation already exists
        _store.TryGetValue(operationKey, out var existing);
        existingRecord = existing!;
        return false;
    }

    /// <summary>
    /// Updates an existing operation to Completed state, optionally attaching a semantic result for replays.
    /// </summary>
    public void MarkCompleted(string operationKey, object? semanticResult = null)
    {
        if (_store.TryGetValue(operationKey, out var record))
        {
            lock (record)
            {
                record.State = OperationState.Completed;
                record.SemanticResult = semanticResult;
            }
        }
    }

    /// <summary>
    /// Updates an existing operation to Failed state, allowing for future retries if logic dictates.
    /// </summary>
    public void MarkFailed(string operationKey)
    {
        if (_store.TryGetValue(operationKey, out var record))
        {
            lock (record)
            {
                record.State = OperationState.Failed;
            }
        }
    }

    /// <summary>
    /// Removes expired operation records periodically.
    /// </summary>
    private void CleanupExpired()
    {
        // For performance, you wouldn't do this on every call in production,
        // but perhaps via a background sweep. This is a simple implementation.
        var cutoff = DateTime.UtcNow - _retentionPeriod;
        foreach (var kvp in _store)
        {
            if (kvp.Value.Timestamp < cutoff)
            {
                _store.TryRemove(kvp.Key, out _);
            }
        }
    }
}
