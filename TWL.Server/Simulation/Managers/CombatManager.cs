using System.Collections.Concurrent;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Services;

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
    private readonly IRandomService _random;

    public CombatManager(IRandomService random)
    {
        _characters = new ConcurrentDictionary<int, ServerCharacter>();
        _random = random;
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

        // Variance +/- 5%
        float variance = _random.NextFloat(0.95f, 1.05f);
        int finalDamage = (int)Math.Round(baseDamage * variance);

        // 2) Calcular daño (ejemplo muy simple).
        newTargetHp = target.ApplyDamage(finalDamage);

        // 3) Retornar el resultado para avisar al cliente.
        var result = new CombatResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Damage = finalDamage,
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

    public ServerCharacter? GetCharacter(int id)
    {
        _characters.TryGetValue(id, out var character);
        return character;
    }

    public List<ServerCharacter> GetAllCharacters()
    {
        return _characters.Values.ToList();
    }
}