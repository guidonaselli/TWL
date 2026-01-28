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

    // If captured, what Pet Type ID does it become?
    public int? PetTypeId { get; set; }

    // HP must be below this percentage (0.0 to 1.0) to capture
    public float CaptureThreshold { get; set; } = 0.5f;

    public int Level { get; set; } = 1;

    public int ExpReward => Level * 10;

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        // AI if needed
    }
}
