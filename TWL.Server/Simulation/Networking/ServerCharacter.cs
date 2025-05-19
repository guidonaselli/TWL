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
}