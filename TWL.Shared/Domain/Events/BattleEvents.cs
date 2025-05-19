using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;

// Item

// PlayerCharacter, EnemyCharacter

namespace TWL.Shared.Domain.Events;

/// Llamado cuando el cliente debe entrar a escena de batalla
public record BattleStarted(
    List<PlayerCharacter> Allies,
    List<EnemyCharacter> Enemies);

/// Emitido al acabar el combate (gane o pierda)
public record BattleFinished(
    bool Victory,
    int ExpGained,
    List<Item> Loot);