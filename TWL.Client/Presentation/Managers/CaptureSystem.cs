using System;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers;

public class CaptureSystem
{
    private readonly EnemyCharacter _enemyCharacter;
    private readonly Random _rnd = new();

    public bool TryCapture(PlayerCharacter player, EnemyCharacter enemy, Inventory playerInventory)
    {
        if (_enemyCharacter.IsCapturable == false)
            return false;

        // Condición: HP del enemigo < 30%
        var hpRatio = (float)enemy.Health / enemy.MaxHealth;
        if (hpRatio > 0.3f)
            return false;

        // Consumo el PetCaptureItem
        playerInventory.RemoveItem(999, 1);

        // Probabilidad base 30%
        var chance = 0.3f;
        var roll = (float)_rnd.NextDouble();
        return roll < chance;
    }

    public void CapturePet(PlayerCharacter player, EnemyCharacter enemy, Inventory playerInventory)
    {
        var pet = new PetCharacter(enemy.Name, enemy.CharacterElement);

        player.AddPet(pet);
    }
}