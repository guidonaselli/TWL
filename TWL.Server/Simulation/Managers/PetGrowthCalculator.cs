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

        // Curve adjustments (Bonus points based on curve type)
        // Standard: No change
        // EarlyPeaker: +20% stats in first 30 levels?
        // LateBloomer: +20% stats after level 50?
        // Keeping it simple for now as requested "GrowthModel: curvas por tipo".

        // Curve Logic
        float multiplier = 1.0f;
        if (model.CurveType == GrowthCurveType.EarlyPeaker)
        {
            // Stronger early, weaker late
            if (level <= 40) multiplier = 1.2f;
            else multiplier = 0.8f;
        }
        else if (model.CurveType == GrowthCurveType.LateBloomer)
        {
            // Weaker early, stronger late
            if (level <= 40) multiplier = 0.8f;
            else multiplier = 1.3f;
        }

        // Apply multiplier to points gained
        int adjustedPoints = (int)(totalPoints * multiplier);

        str = def.BaseStr + (adjustedPoints * model.StrWeight / totalWeight);
        con = def.BaseCon + (adjustedPoints * model.ConWeight / totalWeight);
        int_ = def.BaseInt + (adjustedPoints * model.IntWeight / totalWeight);
        wis = def.BaseWis + (adjustedPoints * model.WisWeight / totalWeight);
        agi = def.BaseAgi + (adjustedPoints * model.AgiWeight / totalWeight);

        // HP/SP Calculation
        // Base HP + (Con Contribution) + (Growth Per Level)
        // Con Contribution: (Con - BaseCon) * 5
        int conBonus = (con - def.BaseCon) * 5;
        maxHp = def.BaseHp + conBonus + (int)(model.HpGrowthPerLevel * levelsGained);

        // Base SP is derived from Int usually, let's assume BaseInt * 5 is the starting SP if not defined
        // We don't have BaseSp in definition, so we derive from Int.
        int intBonus = (int_ - def.BaseInt) * 5;
        // Let's assume initial SP is roughly BaseInt * 5
        int baseSp = def.BaseInt * 5;
        maxSp = baseSp + intBonus + (int)(model.SpGrowthPerLevel * levelsGained);
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
