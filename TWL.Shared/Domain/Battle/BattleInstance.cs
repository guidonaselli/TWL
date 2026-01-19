using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Battle;

public enum BattleState
{
    Active,
    Victory,
    Defeat
}

public class BattleInstance
{
    public List<Combatant> Allies { get; private set; }
    public List<Combatant> Enemies { get; private set; }
    public List<Combatant> AllCombatants => Allies.Concat(Enemies).ToList();

    public BattleState State { get; private set; } = BattleState.Active;

    // Queue of combatants who are ready to act (ATB >= 100)
    private Queue<Combatant> _readyQueue = new();

    // Current combatant whose turn it is
    public Combatant CurrentTurnCombatant { get; private set; }

    public BattleInstance(IEnumerable<Character> allies, IEnumerable<Character> enemies)
    {
        int idCounter = 1;
        Allies = allies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
        Enemies = enemies.Select(c => new Combatant(c) { BattleId = idCounter++ }).ToList();
    }

    public void Tick(float deltaTimeSeconds)
    {
        if (State != BattleState.Active) return;
        if (CurrentTurnCombatant != null) return; // Waiting for action

        // If no one is waiting for input, fill ATB
        if (_readyQueue.Count == 0)
        {
            foreach (var c in AllCombatants)
            {
                if (!c.Character.IsAlive())
                {
                    c.Atb = 0;
                    continue;
                }

                // ATB Fill Speed formula: Base + Spd * Factor
                double fillRate = (10 + c.Character.Spd) * 2;
                c.Atb += fillRate * deltaTimeSeconds;

                if (c.Atb >= 100)
                {
                    c.Atb = 100;
                    if (!_readyQueue.Contains(c))
                        _readyQueue.Enqueue(c);
                }
            }
        }

        if (_readyQueue.Count > 0 && CurrentTurnCombatant == null)
        {
            CurrentTurnCombatant = _readyQueue.Dequeue();
        }
    }

    public string ResolveAction(CombatAction action)
    {
        if (CurrentTurnCombatant == null) return "No active turn";
        if (CurrentTurnCombatant.BattleId != action.ActorId) return "Not your turn";

        var actor = CurrentTurnCombatant;
        var targetCombatant = AllCombatants.FirstOrDefault(c => c.BattleId == action.TargetId);

        string resultMessage = "";

        switch (action.Type)
        {
            case CombatActionType.Attack:
                if (targetCombatant != null)
                {
                    int damage = Math.Max(1, actor.Character.CalculatePhysicalDamage() - targetCombatant.Character.CalculateDefense());
                    if (targetCombatant.IsDefending) damage /= 2;

                    targetCombatant.Character.Health = Math.Max(0, targetCombatant.Character.Health - damage);
                    resultMessage = $"{actor.Character.Name} attacks {targetCombatant.Character.Name} for {damage} damage!";
                }
                break;

            case CombatActionType.Defend:
                actor.IsDefending = true;
                resultMessage = $"{actor.Character.Name} defends!";
                break;

            case CombatActionType.Skill:
                resultMessage = UseSkill(actor, targetCombatant, action.SkillId);
                break;

            case CombatActionType.Flee:
                 resultMessage = $"{actor.Character.Name} tries to flee...";
                 break;
        }

        // End turn
        actor.Atb = 0;
        CurrentTurnCombatant = null;

        CheckBattleEnd();

        return resultMessage;
    }

    private string UseSkill(Combatant actor, Combatant target, int skillId)
    {
        if (target == null) return "No target";

        // Cost check could go here (SP)
        // int cost = 10;
        // if (!actor.Character.ConsumeSp(cost)) return "Not enough SP!";

        switch (skillId)
        {
            case 1: // Power Strike (Phys)
                int dmg1 = Math.Max(1, (int)(actor.Character.CalculatePhysicalDamage() * 1.5) - target.Character.CalculateDefense());
                if (target.IsDefending) dmg1 /= 2;
                target.Character.Health = Math.Max(0, target.Character.Health - dmg1);
                return $"{actor.Character.Name} uses Power Strike on {target.Character.Name} for {dmg1}!";

            case 2: // Fireball (Magic)
                int dmg2 = Math.Max(1, actor.Character.CalculateMagicalDamage() * 2 - target.Character.CalculateMagicalDefense());
                target.Character.Health = Math.Max(0, target.Character.Health - dmg2);
                return $"{actor.Character.Name} casts Fireball on {target.Character.Name} for {dmg2}!";

            case 3: // Heal
                int heal = actor.Character.Int * 4;
                target.Character.Health = Math.Min(target.Character.MaxHealth, target.Character.Health + heal);
                return $"{actor.Character.Name} heals {target.Character.Name} for {heal}!";

            default:
                return $"{actor.Character.Name} uses unknown skill!";
        }
    }

    private void CheckBattleEnd()
    {
        if (Allies.All(a => !a.Character.IsAlive()))
        {
            State = BattleState.Defeat;
        }
        else if (Enemies.All(e => !e.Character.IsAlive()))
        {
            State = BattleState.Victory;
        }
    }

    public void ForceEnd()
    {
        State = BattleState.Defeat;
    }
}
