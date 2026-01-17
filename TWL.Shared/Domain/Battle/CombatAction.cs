namespace TWL.Shared.Domain.Battle;

public enum CombatActionType
{
    Attack,
    Skill,
    Defend,
    Item,
    Flee
}

public class CombatAction
{
    public CombatActionType Type { get; set; }
    public int ActorId { get; set; }
    public int TargetId { get; set; }
    public int SkillId { get; set; } // Only if Type == Skill
    public int ItemId { get; set; }  // Only if Type == Item

    public static CombatAction Attack(int actorId, int targetId)
    {
        return new CombatAction { Type = CombatActionType.Attack, ActorId = actorId, TargetId = targetId };
    }

    public static CombatAction Defend(int actorId)
    {
        return new CombatAction { Type = CombatActionType.Defend, ActorId = actorId, TargetId = actorId };
    }

    public static CombatAction UseSkill(int actorId, int targetId, int skillId)
    {
        return new CombatAction { Type = CombatActionType.Skill, ActorId = actorId, TargetId = targetId, SkillId = skillId };
    }
}
