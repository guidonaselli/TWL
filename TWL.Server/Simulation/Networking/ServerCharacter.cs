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
    /// Object dedicated to synchronization to avoid locking on the public instance.
    /// </summary>
    internal readonly object SyncRoot = new object();
}
