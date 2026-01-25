using System;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyPersistenceTests
{
    [Fact]
    public void BuyShopItem_Idempotency_PersistsAcrossRestarts()
    {
        var ledgerFile = $"test_ledger_{Guid.NewGuid():N}.log";
        var opId = $"op_restart_{Guid.NewGuid():N}";
        try
        {
            // 1. Initial Session
            var manager1 = new EconomyManager(ledgerFile);
            var character1 = new ServerCharacter { Id = 1, PremiumCurrency = 100 };

            // Buy Shop Item 1 (Cost 10)
            var result1 = manager1.BuyShopItem(character1, 1, 1, opId);
            Assert.True(result1.Success, "First purchase should succeed");
            Assert.Equal(90, character1.PremiumCurrency);

            // 2. Simulate Restart (New Manager, same ledger)
            var manager2 = new EconomyManager(ledgerFile);
            var character2 = new ServerCharacter { Id = 1, PremiumCurrency = 100 }; // Fresh state (e.g. from DB)

            // Retry Purchase with same opId
            var result2 = manager2.BuyShopItem(character2, 1, 1, opId);

            // 3. Assert Idempotency
            Assert.True(result2.Success, "Retry should be considered successful (idempotent)");
            Assert.Equal("Already completed", result2.Message);
            Assert.Equal(100, character2.PremiumCurrency); // Should NOT deduct again
        }
        finally
        {
            if (File.Exists(ledgerFile))
            {
                File.Delete(ledgerFile);
            }
        }
    }
}
