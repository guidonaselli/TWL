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
    private readonly Dictionary<int, ServerCharacter> _characters;

    public CombatManager()
    {
        _characters = new Dictionary<int, ServerCharacter>();

        // Llenar con personajes de ejemplo. Ej.:
        // _characters[101] = new ServerCharacter { Id=101, Name="Player1", Hp=100, Str=10, ... };
        // _characters[201] = new ServerCharacter { Id=201, Name="Slime", Hp=50, Str=5, ... };
    }

    /// <summary>
    ///     Usa una skill (basado en la petición del cliente).
    /// </summary>
    public CombatResult UseSkill(UseSkillRequest request)
    {
        // 1) Obtenemos los objetos server-side
        if (!_characters.ContainsKey(request.PlayerId) ||
            !_characters.ContainsKey(request.TargetId))
            // En un caso real, podrías retornar un error o un CombatResult con "invalid target".
            return null;

        var attacker = _characters[request.PlayerId];
        var target = _characters[request.TargetId];

        // 2) Calcular daño (ejemplo muy simple).
        // En un proyecto real, mezclarías:
        // - attacker.Stats
        // - skillDefinition (Power, type, etc.)
        // - Resistencias, sell, etc.
        var baseDamage = attacker.Str * 2;
        target.Hp -= baseDamage;
        if (target.Hp < 0) target.Hp = 0;

        // 3) Retornar el resultado para avisar al cliente.
        var result = new CombatResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Damage = baseDamage,
            NewTargetHp = target.Hp
        };

        return result;
    }

    // Ejemplo: Lógica de turnos (opcional). Podrías llevar un "battleId" y states.
    // public void NextTurn(int battleId) { ... }

    // Podrías agregar más métodos: Revive, ApplyBuff, etc.
}