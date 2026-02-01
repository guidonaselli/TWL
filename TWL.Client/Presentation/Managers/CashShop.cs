using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers;

public class CashShop
{
    public void BuyCashPoints(PlayerCharacter player, int realMoneyAmount)
    {
        // 1 realMoney => 100 points (ejemplo)
        var points = realMoneyAmount * 100;
        player.AddTwlPoints(points);
    }

    public bool SpendCashPoints(PlayerCharacter player, int cost, Inventory playerInventory, int itemId)
    {
        if (player.TwlPoints < cost)
        {
            return false;
        }

        player.TwlPoints -= cost;
        // Da el item premium
        playerInventory.AddItem(itemId, 1);
        return true;
    }
}