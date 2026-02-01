using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Networking;

public class ServerEnemy : ServerCombatant
{
    public ServerEnemy(EnemyCharacter definition)
    {
        Definition = definition;
        Id = -1; // Temporary ID, should be assigned by CombatManager
        Name = definition.Name;
        CharacterElement = definition.CharacterElement;

        // Initialize Stats from Definition
        Str = definition.Str;
        Con = definition.Con;
        Int = definition.Int;
        Wis = definition.Wis;
        Agi = definition.Agi;

        // Recalculate derived stats
        // In a real system, we might want to scale this by Level
        // For now, simple mapping

        Hp = MaxHp;
        Sp = MaxSp;

        // Level
        Level = definition.Level;
    }

    public EnemyCharacter Definition { get; }

    public int Level { get; set; }

    public override void ReplaceSkill(int oldId, int newId)
    {
        // Enemies generally don't replace skills at runtime
    }

    public void Die()
    {
        Hp = 0;
        Sp = 0;
        IsDirty = true;
    }
}