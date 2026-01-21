namespace TWL.Server.Simulation.Networking;

/// <summary>
///     Representa un personaje en el lado del servidor.
///     Podrías tener stats completos, estado de combate, etc.
/// </summary>
public class ServerCharacter
{
    private readonly object _syncRoot = new object();

    public int Hp;
    public int Id;
    public string Name;

    public int Str;
    // Resto de stats (Con, Int, Spd, etc.)

    /// <summary>
    /// Apply damage to the character in a thread-safe manner.
    /// </summary>
    /// <param name="damage">Amount of damage to deal.</param>
    /// <returns>The new HP value.</returns>
    public int TakeDamage(int damage)
    {
        lock (_syncRoot)
        {
            Hp -= damage;
            if (Hp < 0) Hp = 0;
            return Hp;
        }
    }
}
