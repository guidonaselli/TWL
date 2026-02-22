using TWL.Server.Simulation.Networking;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Security;

public class EconomyIdempotencyFlowTests : IDisposable
{
    private readonly EconomyManager _economyManager;
    private readonly string _ledgerFile;
    
    public EconomyIdempotencyFlowTests()
    {
        _ledgerFile = Path.GetTempFileName();
        _economyManager = new EconomyManager(_ledgerFile);
    }
    
    public void Dispose()
    {
        _economyManager.Dispose();
        if (File.Exists(_ledgerFile)) File.Delete(_ledgerFile);
        var snapshot = Path.ChangeExtension(_ledgerFile, ".snapshot.json");
        if (File.Exists(snapshot)) File.Delete(snapshot);
    }
    
    [Fact]
    public void BuyShopItem_Idempotent_SubsequentCalls_ReturnSameResult_DoNotDeductCurrency()
    {
        var character = new ServerCharacter { Id = 1, Name = "Test" };
        character.AddPremiumCurrency(100);
        
        // Buy Item 1 (Price: 10)
        var result1 = _economyManager.BuyShopItem(character, 1, 1, "op1");
        
        Assert.True(result1.Success);
        Assert.Equal(90, character.PremiumCurrency);
        Assert.Single(character.GetItems(101));
        
        // Retry
        var result2 = _economyManager.BuyShopItem(character, 1, 1, "op1");
        
        Assert.True(result2.Success);
        Assert.Equal("Already completed", result2.Message);
        
        // Currency should not be deducted again
        Assert.Equal(90, character.PremiumCurrency);
        // Items should not be added again
        Assert.Single(character.GetItems(101));
    }
    
    [Fact]
    public void GiftShopItem_Idempotent_SubsequentCalls_ReturnSameResult()
    {
        var giver = new ServerCharacter { Id = 1, Name = "Giver" };
        giver.AddPremiumCurrency(100);
        var receiver = new ServerCharacter { Id = 2, Name = "Receiver" };
        
        // Gift Item 1 (Price: 10)
        var result1 = _economyManager.GiftShopItem(giver, receiver, 1, 1, "op2");
        
        Assert.True(result1.Success);
        Assert.Equal(90, giver.PremiumCurrency);
        Assert.Single(receiver.GetItems(101));
        
        // Retry
        var result2 = _economyManager.GiftShopItem(giver, receiver, 1, 1, "op2");
        
        Assert.True(result2.Success);
        Assert.Equal("Already completed", result2.Message);
        Assert.Equal(90, giver.PremiumCurrency);
        Assert.Single(receiver.GetItems(101));
    }
}
