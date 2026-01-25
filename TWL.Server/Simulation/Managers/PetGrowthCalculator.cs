using System;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Managers;

public static class PetGrowthCalculator
{
    private const int STAT_POINTS_PER_LEVEL = 3;

    public static void CalculateStats(PetDefinition def, int level, out int maxHp, out int maxSp, out int str, out int con, out int int_, out int wis, out int agi)
    {
        if (def == null) throw new ArgumentNullException(nameof(def));
        if (level < 1) level = 1;

        var model = def.GrowthModel ?? new PetGrowthModel();
        int levelsGained = level - 1;

        // Calculate HP/SP
        // Base + (Growth * levels)
        maxHp = def.BaseHp + (int)(model.HpGrowthPerLevel * levelsGained);
        // Ensure standard HP calculation logic if needed, but usually pets have their own HP growth
        // Often HP is also derived from CON.
        // Let's stick to the GrowthModel as the primary driver for "Base HP at level".
        // But if we want to follow WLO strictness, HP = Con * 10 + Bonuses.
        // The prompt asks for "HP/SP... GrowthModel".
        // I will return the "Natural" stats.

        // Calculate Stats based on weights
        int totalWeight = model.StrWeight + model.ConWeight + model.IntWeight + model.WisWeight + model.AgiWeight;
        if (totalWeight == 0) totalWeight = 1; // Prevent division by zero

        int totalPoints = levelsGained * STAT_POINTS_PER_LEVEL;

        // We use floating point intermediate to avoid losing points due to rounding errors early on,
        // but for deterministic integer stats we usually floor.
        // To be fairer, we could use a bucket distribution, but simple ratio is fine for "Core" implementation.

        str = def.BaseStr + (totalPoints * model.StrWeight / totalWeight);
        con = def.BaseCon + (totalPoints * model.ConWeight / totalWeight);
        int_ = def.BaseInt + (totalPoints * model.IntWeight / totalWeight);
        wis = def.BaseWis + (totalPoints * model.WisWeight / totalWeight);
        agi = def.BaseAgi + (totalPoints * model.AgiWeight / totalWeight);

        // Curve adjustments (Bonus points based on curve type)
        // Standard: No change
        // EarlyPeaker: +20% stats in first 30 levels?
        // LateBloomer: +20% stats after level 50?
        // Keeping it simple for now as requested "GrowthModel: curvas por tipo".

        if (model.CurveType == GrowthCurveType.EarlyPeaker && level <= 30)
        {
             // Simple boost for example
             int boost = levelsGained / 5;
             str += boost; con += boost; int_ += boost; wis += boost; agi += boost;
        }
        else if (model.CurveType == GrowthCurveType.LateBloomer && level > 50)
        {
             int boost = (level - 50) / 2;
             str += boost; con += boost; int_ += boost; wis += boost; agi += boost;
        }

        // Re-calculate HP/SP based on Con/Int if we want that synergy
        // But the model has HpGrowthPerLevel. Let's use that as specific pet growth.
        // If we want consistency with players: MaxHp = Con * 10.
        // But pets often have different ratios.
        // Let's add Con contribution to the base growth.

        maxHp += (con - def.BaseCon) * 5; // Extra HP from CON growth

        // SP
        maxSp = (def.BaseInt * 5) + (int)(model.SpGrowthPerLevel * levelsGained); // Initial SP is roughly BaseInt*5 ?
        // Or just trust the model completely?
        // Let's say Base SP is not in definition, usually derived.
        // If BaseInt is 0, SP might be 0.
        // Let's assume SP = Int * 5 + Growth.
        maxSp = (int_ * 5) + (int)(model.SpGrowthPerLevel * levelsGained);
    }

    public static int GetExpForLevel(int level)
    {
        // Simple exponential curve
        // Level 1 -> 2: 100
        // Level 2 -> 3: 125
        if (level < 1) return 0;
        // Total Exp to reach next level? Or Exp required for current level?
        // Usually "ExpToNextLevel" for Level X is X^2 * 10 or similar.
        // ServerPet used: ExpToNextLevel = (int)(ExpToNextLevel * 1.25);
        // We should make this stateless.

        // Let's use a standard formula: 50 * (Level) * (Level) + 50 * Level?
        // Or keep the previous geometric progression logic but make it calculable.
        // 100 * 1.25^(Level-1)

        return (int)(100 * Math.Pow(1.25, level - 1));
    }
}
