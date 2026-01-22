using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Battle;

public class Combatant
{
    public int BattleId { get; set; } // Unique ID within the battle
    public Character Character { get; private set; }
    public double Atb { get; set; } // 0 to 100
    public bool IsDefending { get; set; }

    public int AttackBuffTurns { get; set; }

    public Combatant(Character character)
    {
        Character = character;
        Atb = 0;
        IsDefending = false;
    }

    public void ResetTurn()
    {
        Atb = 0;
        IsDefending = false;
    }

    public bool IsReady()
    {
        return Atb >= 100;
    }
}
