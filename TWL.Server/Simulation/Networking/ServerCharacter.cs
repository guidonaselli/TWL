using System.Threading;

namespace TWL.Server.Simulation.Networking;

/// <summary>
///     Representa un personaje en el lado del servidor.
///     Podrías tener stats completos, estado de combate, etc.
/// </summary>
public class ServerCharacter
{
    public int Hp;
    public int Id;
    public string Name;

    public int Str;
    // Resto de stats (Con, Int, Spd, etc.)

    /// <summary>
    /// Applies damage to the character in a thread-safe manner.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    /// <returns>The new HP value.</returns>
    public int ApplyDamage(int damage)
    {
        int initialHp, newHp;
        do
        {
            initialHp = Hp;
            newHp = initialHp - damage;
            if (newHp < 0) newHp = 0;
        }
        while (Interlocked.CompareExchange(ref Hp, newHp, initialHp) != initialHp);

        return newHp;
    }
}
