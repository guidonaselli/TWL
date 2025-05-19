using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;

// CombatResult, etc.

namespace TWL.Client.Presentation.Managers;

/// <summary>
///     Maneja la parte de combate a nivel de cliente (estadísticas locales,
///     animaciones, etc.). No calcula daño real; eso lo hace el servidor.
/// </summary>
public class ClientCombatManager
{
    // Ejemplo: listados de personajes locales
    private List<Character> _allies;
    private List<Character> _enemies;

    public ClientCombatManager()
    {
        _allies = new List<Character>();
        _enemies = new List<Character>();
    }

    // Podrías pasar listas de personajes al constructor o exponer métodos
    public void SetAllies(List<Character> allies)
    {
        _allies = allies;
    }

    public void SetEnemies(List<Character> enemies)
    {
        _enemies = enemies;
    }

    /// <summary>
    ///     Cuando llega un CombatResult del servidor,
    ///     actualizamos la HP del target, etc.
    /// </summary>
    public void OnCombatResult(CombatResult result)
    {
        var target = _allies.FirstOrDefault(a => a.Id == result.TargetId)
                     ?? _enemies.FirstOrDefault(e => e.Id == result.TargetId);

        if (target != null)
        {
            target.Health = result.NewTargetHp;
            Console.WriteLine($"[ClientCombatManager] {target.Name} HP => {target.Health}");
        }
    }

    /// <summary>
    ///     Sends a request to use a skill.
    /// </summary>
    public void RequestUseSkill(int playerId, int targetId, int skillId)
    {
        var request = new RequestUseSkill
        {
            PlayerId = playerId,
            TargetId = targetId,
            SkillId = skillId
        };
        Console.WriteLine(
            $"[ClientCombatManager] RequestUseSkill: playerId={playerId}, targetId={targetId}, skillId={skillId}");
        // Forward the request to the server using your network client.
        // Example: _networkClient.SendClientMessage(request);
    }
}