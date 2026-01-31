using System.Linq;
using System.IO;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyTransactionTests
{
    [Fact]
    public void BuyShopItem_Idempotency_PreventDoubleCharge()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using var manager = new EconomyManager(tempFile);
            var character = new ServerCharacter { Id = 1, PremiumCurrency = 100 };
            // Shop Item 1: Cost 10, ItemId 101

            var opId = "op_unique_123";

            // First Call
            // This assumes the method signature will be updated to accept operationId
            var result1 = manager.BuyShopItem(character, 1, 1, opId);
            Assert.True(result1.Success);
            Assert.Equal(90, character.PremiumCurrency); // 100 - 10

            // Second Call (Retry)
            var result2 = manager.BuyShopItem(character, 1, 1, opId);
            Assert.True(result2.Success); // Should succeed (idempotent)
            Assert.Equal("Already completed", result2.Message); // Message indicating idempotency
            Assert.Equal(90, character.PremiumCurrency); // Should NOT be 80

            // Verify item was not added twice
            Assert.Single(character.Inventory);
            var item = character.Inventory[0];
            Assert.Equal(1, item.Quantity);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
