using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class StandardCombatResolver : ICombatResolver
{
    private readonly IRandomService _random;
    private readonly ISkillCatalog _skills;

    public StandardCombatResolver(IRandomService random, ISkillCatalog skills)
    {
        _random = random;
        _skills = skills;
    }

    public int CalculateDamage(ServerCombatant attacker, ServerCombatant target, UseSkillRequest request)
    {
        var skill = _skills.GetSkillById(request.SkillId);

        // Fallback for legacy calls or unknown skills
        if (skill == null)
        {
            var baseDamage = attacker.Str * 2;
            var variance = _random.NextFloat(0.95f, 1.05f, "DamageVariance");
            return (int)Math.Round(baseDamage * variance);
        }

        if (skill.Branch == SkillBranch.Support)
        {
            return 0;
        }

        float totalValue = 0;

        // Sum up scaling values
        foreach (var scaling in skill.Scaling)
        {
            var statValue = GetStatValue(attacker, scaling.Stat);
            totalValue += statValue * scaling.Coefficient;
        }

        // Elemental Multiplier
        var elemMult = GetElementalMultiplier(skill.Element, target.CharacterElement);
        totalValue *= elemMult;

        // Variance +/- 5%
        var rnd = _random.NextFloat(0.95f, 1.05f, "DamageVariance");
        totalValue *= rnd;

        // Defense subtraction
        // If skill is Magical, use Mdf. Else Def.
        var defense = skill.Branch == SkillBranch.Magical ? target.Mdf : target.Def;

        var damage = Math.Max(1, (int)Math.Round(totalValue) - defense);

        return damage;
    }

    public int CalculateHeal(ServerCombatant healer, ServerCombatant target, UseSkillRequest request)
    {
        var skill = _skills.GetSkillById(request.SkillId);
        if (skill == null)
        {
            return 0;
        }

        float totalValue = 0;

        // Sum up scaling values
        foreach (var scaling in skill.Scaling)
        {
            var statValue = GetStatValue(healer, scaling.Stat);
            totalValue += statValue * scaling.Coefficient;
        }

        // Variance +/- 5%
        var rnd = _random.NextFloat(0.95f, 1.05f, "HealVariance");
        totalValue *= rnd;

        return Math.Max(0, (int)Math.Round(totalValue));
    }

    private float GetStatValue(ServerCombatant c, StatType stat)
    {
        switch (stat)
        {
            case StatType.Str: return c.Str;
            case StatType.Con: return c.Con;
            case StatType.Int: return c.Int;
            case StatType.Wis: return c.Wis;
            case StatType.Agi: return c.Agi;
            case StatType.Atk: return c.Atk;
            case StatType.Def: return c.Def;
            case StatType.Mat: return c.Mat;
            case StatType.Mdf: return c.Mdf;
            case StatType.Spd: return c.Spd;
            default: return 0;
        }
    }

    private float GetElementalMultiplier(Element skillElement, Element targetElement)
    {
        if (skillElement == Element.Earth && targetElement == Element.Water)
        {
            return 1.5f;
        }

        if (skillElement == Element.Water && targetElement == Element.Fire)
        {
            return 1.5f;
        }

        if (skillElement == Element.Fire && targetElement == Element.Wind)
        {
            return 1.5f;
        }

        if (skillElement == Element.Wind && targetElement == Element.Earth)
        {
            return 1.5f;
        }

        if (skillElement == Element.Water && targetElement == Element.Earth)
        {
            return 0.5f;
        }

        if (skillElement == Element.Fire && targetElement == Element.Water)
        {
            return 0.5f;
        }

        if (skillElement == Element.Wind && targetElement == Element.Fire)
        {
            return 0.5f;
        }

        if (skillElement == Element.Earth && targetElement == Element.Wind)
        {
            return 0.5f;
        }

        return 1.0f;
    }
}
