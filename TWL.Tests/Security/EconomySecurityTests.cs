using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Security;

public class EconomySecurityTests
{
    [Fact]
    public void VerifyPurchase_InvalidReceiptSignature_Fails()
    {
        using var manager = new EconomyManager();
        var charId = 1;
        var character = new ServerCharacter { Id = charId, PremiumCurrency = 0 };

        var intent = manager.InitiatePurchase(charId, "gems_100");

        // Use an invalid token (not matching mock_sig_{orderId})
        var result = manager.VerifyPurchase(charId, intent.OrderId, "invalid_sig", character);

        Assert.False(result.Success);
        Assert.Equal("Invalid receipt signature", result.Message);
        Assert.Equal(0, character.PremiumCurrency);
    }

    [Fact]
    public void VerifyPurchase_ValidReceiptSignature_Succeeds()
    {
        using var manager = new EconomyManager();
        var charId = 1;
        var character = new ServerCharacter { Id = charId, PremiumCurrency = 0 };

        var intent = manager.InitiatePurchase(charId, "gems_100");

        // Use a valid token
        var validToken = manager.GenerateSignature(intent.OrderId);
        var result = manager.VerifyPurchase(charId, intent.OrderId, validToken, character);

        Assert.True(result.Success);
        Assert.Equal(100, character.PremiumCurrency);
    }

    [Fact]
    public void VerifyPurchase_Replay_Idempotency()
    {
        using var manager = new EconomyManager();
        var charId = 1;
        var character = new ServerCharacter { Id = charId, PremiumCurrency = 0 };

        var intent = manager.InitiatePurchase(charId, "gems_100");
        var validToken = manager.GenerateSignature(intent.OrderId);

        // First verification
        var result1 = manager.VerifyPurchase(charId, intent.OrderId, validToken, character);
        Assert.True(result1.Success);
        Assert.Equal(100, character.PremiumCurrency);

        // Second verification (Replay)
        var result2 = manager.VerifyPurchase(charId, intent.OrderId, validToken, character);
        Assert.True(result2.Success); // Should still say success
        Assert.Equal("Already completed", result2.Message);
        Assert.Equal(100, character.PremiumCurrency); // Balance should NOT increase
    }

    [Fact]
    public void BuyShopItem_InvalidQuantity_Fails()
    {
        using var manager = new EconomyManager();
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 1000 };

        // Test Negative
        var resultNegative = manager.BuyShopItem(character, 1, -1);
        Assert.False(resultNegative.Success);

        // Test Zero
        var resultZero = manager.BuyShopItem(character, 1, 0);
        Assert.False(resultZero.Success);

        // Test Too Large
        var resultHuge = manager.BuyShopItem(character, 1, 10000);
        Assert.False(resultHuge.Success);
        Assert.Contains("Invalid quantity", resultHuge.Message);
    }

    [Fact]
    public void BuyShopItem_ValidQuantity_Succeeds()
    {
        using var manager = new EconomyManager();
        var character = new ServerCharacter { Id = 1, PremiumCurrency = 1000 };

        // ShopItem 1 costs 10. Buy 5 = 50 cost.
        var result = manager.BuyShopItem(character, 1, 5);

        Assert.True(result.Success);
        Assert.Equal(950, character.PremiumCurrency);
        Assert.True(character.HasItem(101, 5));
    }
}