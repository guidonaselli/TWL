using Microsoft.Xna.Framework;

namespace TWL.Shared.Domain.Characters;

public class PetCharacter : Character
{
    public float CurrentExp;
    public PetState CurrentPetState; // Rest, Ride, Battle
    public float ExpToNextLevel;
    public bool IsReborn; // si renació
    public bool IsUnique; // si es “única” de quest
    public List<int> KnownSkills; // fijos, excepto 1 skill extra tras renacer
    public int PetLevel;

    public PetCharacter(string name, Element element) : base(name, element)
    {
        IsUnique = false;
        IsReborn = false;
        PetLevel = 1;
        CurrentExp = 0;
        ExpToNextLevel = 100; // Ejemplo, puedes ajustar esto
        CurrentPetState = PetState.Rest;
        KnownSkills = new List<int>();
    }

    public void GainExp(float amount)
    {
        CurrentExp += amount;
        if (CurrentExp >= ExpToNextLevel)
        {
            PetLevel++;
            CurrentExp -= ExpToNextLevel;
            ExpToNextLevel *= 1.25f;

            MaxHealth += 5;
            Health = MaxHealth;
            Str += 1;
            Con += 1;
            Int += 1;
            Wis += 1;
            Spd += 1;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}