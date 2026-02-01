using TWL.Client.Presentation.Services;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers;

public static class EnemyFactory
{
    public static List<EnemyCharacter> GenerateRandomPack()
    {
        var slime = new EnemyCharacter(Loc.T("ENEMY_Slime"), Element.Water, false) { MaxHealth = 30, Health = 30 };
        return new List<EnemyCharacter> { slime };
    }
}