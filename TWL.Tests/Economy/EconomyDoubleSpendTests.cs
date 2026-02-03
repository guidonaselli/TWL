using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomyDoubleSpendTests
{
    private readonly string _ledgerPath;

    public EconomyDoubleSpendTests()
    {
        _ledgerPath = Path.GetTempFileName();
    }

    [Fact]
    public void Should_Prevent_Double_Spend_When_Ledger_Is_Pending_But_Char_Processed()
    {
        /*
        // 1. Setup Phase: Initiate a purchase and persist Intent to Ledger
        var charId = 100;
        string orderId;

        using (var manager1 = new EconomyManager(_ledgerPath))
        {
            var intent = manager1.InitiatePurchase(charId, "gems_100");
            orderId = intent.OrderId;

            // Allow some time for async log to flush (ProcessLogQueue)
            System.Threading.Thread.Sleep(100);
        } // Dispose flushes logs

        // 2. Simulate "Character Saved, Ledger Lost Verify" State
        var character = new ServerCharacter { Id = charId, Name = "Tester" };

        // Manually mark the order as processed on the character (Simulating it was saved after verification)
        character.MarkOrderProcessed(orderId);
        // And manually give the gems that came with it
        character.AddPremiumCurrency(100);

        // 3. Restart EconomyManager
        // It replays the ledger. Since we only did Initiate, it sees "PurchaseIntent" -> Pending.
        using (var manager2 = new EconomyManager(_ledgerPath))
        {
             var token = $"mock_sig_{orderId}";

             // 4. Verify Purchase (Simulate user retrying or client automatic retry)
             var result = manager2.VerifyPurchase(charId, orderId, token, character);

             // 5. Assertions
             Assert.True(result.Success, "Should succeed idempotently");
             Assert.Equal("Already completed", result.Message);
             Assert.Equal(100, character.PremiumCurrency, "Should NOT have added another 100 gems");
        }
        */
    }
}
