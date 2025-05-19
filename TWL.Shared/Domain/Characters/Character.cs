using Microsoft.Xna.Framework;

namespace TWL.Shared.Domain.Characters;

public enum Team
{
    Player,
    Enemy
}

public enum FacingDirection
{
    Down,
    Up,
    Left,
    Right
}

public abstract class Character
{
    public List<int> KnownSkills;

    protected Character(string name, Element element)
    {
        Name = name;
        CharacterElement = element;

        Health = 100;
        MaxHealth = 100;

        // Sugerimos Sp y MaxSp
        Sp = 50;
        MaxSp = 50;

        Str = 5;
        Con = 5;
        Int = 5;
        Wis = 5;
        Spd = 5;
        Position = Vector2.Zero;

        KnownSkills = new List<int>();
    }

    public int Id { get; protected set; }
    public string Name { get; protected set; }
    public Element CharacterElement { get; protected set; }
    public Team Team { get; set; }

    public int Health { get; set; }
    public int MaxHealth { get; set; }

    // Añadimos SP
    public int Sp { get; set; }
    public int MaxSp { get; set; }

    // Stats
    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Spd { get; set; }

    // Flags o estados
    public bool IsSealed { get; set; } // para 'seal'
    // Podrías añadir otros flags: IsStunned, IsPoisoned, etc.

    public float MovementSpeed { get; set; } = 2f;

    public Vector2 Position { get; set; }
    public FacingDirection CurrentDirection { get; set; }

    public virtual void Update(GameTime gameTime)
    {
    }

    public bool IsAlive()
    {
        return Health > 0;
    }

    public bool IsAlly()
    {
        // Suponiendo que los personajes aliados están en el equipo del jugador
        return Team == Team.Player;
    }

    public int GetSpd()
    {
        return Spd;
    }

    public virtual int CalculatePhysicalDamage()
    {
        // Ejemplo básico (puedes dejarlo o usar CombatManager's logic)
        return Str * 2;
    }

    public virtual int CalculateDefense()
    {
        // Ejemplo básico: Con * 2
        return Con * 2;
    }

    // Método para calcular la defensa mágica, si la usas:
    public virtual int CalculateMagicalDefense()
    {
        // Ej: Wis * 2
        return Wis * 2;
    }

    // Verificar si tenemos suficiente SP y restarlo
    public bool ConsumeSp(int cost)
    {
        if (Sp < cost) return false;
        Sp -= cost;
        if (Sp < 0) Sp = 0;
        return true;
    }

    // Lógica para aprender skills según requisitos:
    public void CheckNewSkills(ISkillCatalog skills)
    {
        foreach (var id in skills.GetAllSkillIds())
        {
            var skill = skills.GetSkillById(id);
            if (skill == null) continue;

            if (skill.Element != CharacterElement) continue;
            if (KnownSkills.Contains(skill.SkillId)) continue;

            var ok =
                Str >= skill.StrRequirement &&
                Con >= skill.ConRequirement &&
                Int >= skill.IntRequirement &&
                Wis >= skill.WisRequirement &&
                Spd >= skill.AgiRequirement;

            if (ok) KnownSkills.Add(skill.SkillId);
        }
    }
}