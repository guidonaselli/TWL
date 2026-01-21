using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;

// donde tienes CombatResult, UseSkillRequest, etc.

namespace TWL.Server.Simulation.Managers;

/// <summary>
///     CombatManager vive en el servidor. Gestiona turnos y el cálculo de daño real.
/// </summary>
public class CombatManager
{
    // Supongamos que guardamos todos los personajes en un diccionario
    // (en un MMO real podrías tener combates instanciados).
    private readonly ConcurrentDictionary<int, ServerCharacter> _characters;

    public CombatManager()
    {
        _characters = new ConcurrentDictionary<int, ServerCharacter>();

        // Llenar con personajes de ejemplo. Ej.:
        // _characters[101] = new ServerCharacter { Id=101, Name="Player1", Hp=100, Str=10, ... };
        // _characters[201] = new ServerCharacter { Id=201, Name="Slime", Hp=50, Str=5, ... };
    }

    public void AddCharacter(ServerCharacter character)
    {
        _characters[character.Id] = character;
    }

    /// <summary>
    ///     Usa una skill (basado en la petición del cliente).
    /// </summary>
    public CombatResult UseSkill(UseSkillRequest request)
    {
        // 1) Obtenemos los objetos server-side
        if (!_characters.TryGetValue(request.PlayerId, out var attacker) ||
            !_characters.TryGetValue(request.TargetId, out var target))
            // En un caso real, podrías retornar un error o un CombatResult con "invalid target".
            return null;

        int newTargetHp;
        var baseDamage = attacker.Str * 2;

        // 2) Calcular daño (ejemplo muy simple).
        // Bloqueamos el target para asegurar integridad de HP
        newTargetHp = target.TakeDamage(baseDamage);

        // 3) Retornar el resultado para avisar al cliente.
        var result = new CombatResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Damage = baseDamage,
            NewTargetHp = newTargetHp
        };

        return result;
    }

    // Ejemplo: Lógica de turnos (opcional). Podrías llevar un "battleId" y states.
    // public void NextTurn(int battleId) { ... }

    // Podrías agregar más métodos: Revive, ApplyBuff, etc.

    public void RemoveCharacter(int id)
    {
        _characters.TryRemove(id, out _);
    }

    public List<ServerCharacter> GetAllCharacters()
    {
        return _characters.Values.ToList();
    }
}