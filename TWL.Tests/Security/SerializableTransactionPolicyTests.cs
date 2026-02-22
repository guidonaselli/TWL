using TWL.Server.Persistence.Database;
using TWL.Server.Security.Idempotency;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;
using Npgsql;

namespace TWL.Tests.Security;

public class SerializableTransactionPolicyTests
{
    private class TestDbService : DbService
    {
        public bool TransactionExecuted { get; private set; }
        public bool ThrewException { get; private set; }
        
        // Dummy values to bypass constructors
        public TestDbService() : base("Host=localhost;", null!) { }
        
        public override Task<T> ExecuteSerializableAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
        {
            TransactionExecuted = true;
            try
            {
                // We return default to simulate the work failure/bypass without hitting physical DB connection
                return Task.FromResult(default(T)!);
            }
            catch
            {
                ThrewException = true;
                throw;
            }
        }
    }

    [Fact]
    public async Task TransferItemAsync_UsesSerializableDbTransaction()
    {
        // Assemble
        var db = new TestDbService();
        var tradeManager = new TradeManager();
        var idemp = new IdempotencyValidator();
        var source = new ServerCharacter { Id = 1 };
        var target = new ServerCharacter { Id = 2 };
        
        // Act
        var result = await tradeManager.TransferItemAsync(db, idemp, "trade1", source, target, 1, 1);
        
        // Assert: The TradeManager correctly deferred to ExecuteSerializableAsync to handle the isolation context
        Assert.True(db.TransactionExecuted);
        Assert.False(result); // Mock returned default (false)
        
        // Validate Idempotency state is managed
        idemp.TryRegisterOperation("trade1", source.Id, out var record);
        // It failed because ExecuteSerializableAsync mock returned false which bubbles to MarkFailed
        Assert.Equal(OperationState.Failed, record.State); 
    }
}
