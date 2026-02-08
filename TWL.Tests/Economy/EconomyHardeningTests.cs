using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Economy;

public class EconomyHardeningTests : IDisposable
{
    private readonly ServerCharacter _character;
    private readonly EconomyManager _economy;
    private readonly string _tempLedger;

    public EconomyHardeningTests()
    {
        _tempLedger = Path.GetTempFileName();
        _economy = new EconomyManager(_tempLedger);
        _character = new ServerCharacter
        {
            Id = 1,
            Name = "TestUser",
            MaxInventorySlots = 10,
            PremiumCurrency = 1000 // Start with 1000 gems
        };
    }

    public void Dispose()
    {
        _economy?.Dispose();
        if (File.Exists(_tempLedger))
        {
            try
            {
                File.Delete(_tempLedger);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void RateLimit_ShouldBlockExcessiveRequests()
    {
        // Limit is 10 per minute by default
        for (var i = 0; i < 10; i++)
        {
            var result = _economy.InitiatePurchase(_character.Id, "gems_100");
            Assert.NotNull(result);
        }

        // 11th should fail
        var blocked = _economy.InitiatePurchase(_character.Id, "gems_100");
        Assert.Null(blocked);
    }

    [Fact]
    public void Idempotency_BuyShopItem_ShouldPreventDoubleSpend()
    {
        // Shop Item 1 costs 10 Gems.
        var opId = "unique-op-1";

        var res1 = _economy.BuyShopItem(_character, 1, 1, opId);
        Assert.True(res1.Success);
        Assert.Equal(990, _character.PremiumCurrency);

        // Retry with same opId
        var res2 = _economy.BuyShopItem(_character, 1, 1, opId);
        Assert.True(res2.Success); // Should return success (Idempotent)
        Assert.Equal("Already completed", res2.Message);
        Assert.Equal(990, _character.PremiumCurrency); // Balance should NOT decrease again
    }

    [Fact]
    public void Compensation_ShouldRefund_WhenInventoryFull()
    {
        _character.MaxInventorySlots = 0; // Cannot add any NEW item stacks

        var initialBalance = _character.PremiumCurrency;

        // Try to buy Item 1 (Potion)
        var res = _economy.BuyShopItem(_character, 1, 1, "op-fail-inventory");

        Assert.False(res.Success);
        Assert.Equal("Inventory full", res.Message);
        Assert.Equal(initialBalance, _character.PremiumCurrency);

        // Check Ledger for failure log
        _economy.Dispose(); // Flush logs
        var log = File.ReadAllText(_tempLedger);
        Assert.Contains("ShopBuyFailed", log);
        Assert.Contains("Reason:InventoryFull", log);
    }

    [Fact]
    public void ValidateReceipt_FailClosed_OnEmptyInput()
    {
        var intent = _economy.InitiatePurchase(_character.Id, "gems_100"); // uses 1 rate limit token
        Assert.NotNull(intent);

        // Try verify with empty receipt
        var res = _economy.VerifyPurchase(_character.Id, intent.OrderId, "", _character);
        Assert.False(res.Success);

        // Try verify with valid receipt (HMAC)
        var validSig = _economy.GenerateSignature(intent.OrderId);
        var res2 = _economy.VerifyPurchase(_character.Id, intent.OrderId, validSig, _character);
        Assert.True(res2.Success);
    }

    [Fact]
    public void Receipt_ShouldRejectInvalidSignature()
    {
        var intent = _economy.InitiatePurchase(_character.Id, "gems_100");
        Assert.NotNull(intent);

        // Try with old mock style
        var res = _economy.VerifyPurchase(_character.Id, intent.OrderId, $"mock_sig_{intent.OrderId}", _character);
        Assert.False(res.Success);
        Assert.Equal("Invalid receipt signature", res.Message);

        // Try with random string
        var res2 = _economy.VerifyPurchase(_character.Id, intent.OrderId, "invalid_sig", _character);
        Assert.False(res2.Success);
    }

    [Fact]
    public void Ledger_ShouldDetectTampering()
    {
        // 1. Generate some transactions
        var opId = "tamper_test_op";
        _economy.BuyShopItem(_character, 1, 1, opId);

        // Ensure log is written (Dispose flushes)
        _economy.Dispose();

        // 2. Tamper with the file
        var lines = File.ReadAllLines(_tempLedger);
        Assert.NotEmpty(lines);

        // Find the ShopBuy line (should be last)
        var lastLineIndex = lines.Length - 1;
        var lastLine = lines[lastLineIndex];

        // Modify the balance (last col) or delta.
        // Balance should be 990
        var modifiedLine = lastLine.Replace(",990,", ",9999,"); // safer replace with delimiters
        if (modifiedLine == lastLine)
        {
             // Maybe it's at the end before hashes?
             // Last line format: ...,-10,990,PrevHash,Hash
             modifiedLine = lastLine.Replace(",990,", ",9999,");

             // If still failing (maybe space?), try aggressive replace or just corrupt the hash
             if (modifiedLine == lastLine)
             {
                 // Just corrupt the hash at the end
                 var parts = lastLine.Split(',');
                 if (parts.Length >= 2)
                 {
                     parts[parts.Length - 1] = "badhash";
                     modifiedLine = string.Join(",", parts);
                 }
             }
        }

        lines[lastLineIndex] = modifiedLine;
        File.WriteAllLines(_tempLedger, lines);

        // 3. Re-open economy (simulating server restart)
        // Since we tampered, verify it throws SecurityException (Fail Closed)
        Assert.Throws<System.Security.SecurityException>(() => new EconomyManager(_tempLedger));
    }

    [Fact]
    public void Ledger_ShouldLog_BindPolicy_And_Ownership()
    {
        // Item 3 is BindOnPickup (Cost 100)
        _character.PremiumCurrency = 200;
        var opId = "bind_log_test";
        var res = _economy.BuyShopItem(_character, 3, 1, opId);

        Assert.True(res.Success);
        Assert.Equal(100, _character.PremiumCurrency);

        // Flush
        _economy.Dispose();

        // Read Log
        var log = File.ReadAllText(_tempLedger);

        // Verify Content
        Assert.Contains("Policy:BindOnPickup", log);
        Assert.Contains($"BoundTo:{_character.Id}", log);
    }
}