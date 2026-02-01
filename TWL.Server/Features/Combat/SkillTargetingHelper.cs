using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Features.Combat;

public static class SkillTargetingHelper
{
    public static List<ServerCombatant> GetTargets(
        Skill skill,
        ServerCombatant attacker,
        ServerCombatant primaryTarget,
        IEnumerable<ServerCombatant> allCombatants)
    {
        var targets = new List<ServerCombatant>();

        if (primaryTarget == null) return targets;

        switch (skill.TargetType)
        {
            case SkillTargetType.Self:
                targets.Add(attacker);
                break;

            case SkillTargetType.SingleEnemy:
                if (primaryTarget.Team != attacker.Team)
                    targets.Add(primaryTarget);
                break;

            case SkillTargetType.SingleAlly:
                if (primaryTarget.Team == attacker.Team)
                    targets.Add(primaryTarget);
                break;

            case SkillTargetType.AllEnemies:
                targets.AddRange(allCombatants.Where(c => c.Team != attacker.Team && c.Hp > 0));
                break;

            case SkillTargetType.AllAllies:
                targets.AddRange(allCombatants.Where(c => c.Team == attacker.Team && c.Hp > 0));
                break;

            case SkillTargetType.RowEnemies:
            case SkillTargetType.ColumnEnemies:
            case SkillTargetType.CrossEnemies:
                // Fallback to Single Enemy until Grid is implemented
                // TODO: Implement grid-based targeting
                if (primaryTarget.Team != attacker.Team)
                    targets.Add(primaryTarget);
                break;

             case SkillTargetType.RowAllies:
                // Fallback to Single Ally
                if (primaryTarget.Team == attacker.Team)
                    targets.Add(primaryTarget);
                break;
        }

        return targets;
    }
}
