using Microsoft.Xna.Framework;

namespace TWL.Shared.Domain.Characters;

public class EnemyCharacter : Character
{
    public EnemyCharacter(string name, Element element, bool isCapturable)
        : base(name, element)
    {
        IsCapturable = isCapturable;
        Health = 50;
        MaxHealth = 50;
        Str = 8;
        Con = 3;
    }

    public bool IsCapturable { get; private set; }


    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        // AI if needed
    }
}