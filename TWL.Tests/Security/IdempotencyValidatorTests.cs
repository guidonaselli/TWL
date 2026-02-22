using TWL.Server.Security.Idempotency;
using Xunit;

namespace TWL.Tests.Security;

public class IdempotencyValidatorTests
{
    [Fact]
    public void TryRegisterOperation_NewOperation_ReturnsTrue()
    {
        var validator = new IdempotencyValidator();
        var result = validator.TryRegisterOperation("op1", 1, out var record);
        
        Assert.True(result);
        Assert.Equal(OperationState.Pending, record.State);
    }
    
    [Fact]
    public void TryRegisterOperation_DuplicateOperation_ReturnsFalse()
    {
        var validator = new IdempotencyValidator();
        validator.TryRegisterOperation("op1", 1, out _);
        
        var result = validator.TryRegisterOperation("op1", 1, out var record);
        
        Assert.False(result);
        Assert.Equal(OperationState.Pending, record.State);
    }
    
    [Fact]
    public void MarkCompleted_UpdatesStateAndResult()
    {
        var validator = new IdempotencyValidator();
        validator.TryRegisterOperation("op1", 1, out var record);
        
        validator.MarkCompleted("op1", "success");
        
        Assert.Equal(OperationState.Completed, record.State);
        Assert.Equal("success", record.SemanticResult);
    }
    
    [Fact]
    public void MarkFailed_UpdatesState()
    {
        var validator = new IdempotencyValidator();
        validator.TryRegisterOperation("op1", 1, out var record);
        
        validator.MarkFailed("op1");
        
        Assert.Equal(OperationState.Failed, record.State);
    }
}
