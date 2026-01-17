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

    public int Sp { get; set; }
    public int MaxSp { get; set; }

    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Spd { get; set; }

    public bool IsSealed { get; set; }
    public float MovementSpeed { get; set; } = 2f;

    public Vector2 Position { get; set; }
    public FacingDirection CurrentDirection { get; set; }

    public virtual void Update(GameTime gameTime)
    {
    }

    public bool IsAlive() => Health > 0;
    public bool IsAlly() => Team == Team.Player;
    public int GetSpd() => Spd;

    public virtual int CalculatePhysicalDamage() => Str * 2;
    public virtual int CalculateMagicalDamage() => Int * 2; // Added

    public virtual int CalculateDefense() => Con * 2;
    public virtual int CalculateMagicalDefense() => Wis * 2;

    public bool ConsumeSp(int cost)
    {
        if (Sp < cost) return false;
        Sp -= cost;
        if (Sp < 0) Sp = 0;
        return true;
    }

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
