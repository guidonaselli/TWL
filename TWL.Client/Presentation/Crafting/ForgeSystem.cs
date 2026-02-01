using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Crafting;

public class ForgeSystem
{
    public float GetSuccessRate(int enhanceLevel)
    {
        var baseRate = 0.8f - enhanceLevel * 0.1f;
        return baseRate < 0.1f ? 0.1f : baseRate;
    }

    public bool Enhance(EquipmentData equip, Inventory inv, int forgeStoneId)
    {
        // Chequear si el jugador tiene la piedra (forgeStoneId)
        if (inv.GetItemCount(forgeStoneId) < 1)
        {
            return false;
        }

        inv.RemoveItem(forgeStoneId, 1);

        var chance = GetSuccessRate(equip.EnhanceLevel);
        var roll = RandomManager.NextFloat();

        if (roll < chance)
        {
            equip.EnhanceLevel++;
            equip.StatBonus += 2;
            return true;
        }

        // fallback
        if (equip.EnhanceLevel > 0)
        {
            equip.EnhanceLevel--;
        }

        return false;
    }
}