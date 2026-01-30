using System;
using System.Linq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class PetService : IPetService
{
    private readonly PlayerService _playerService;
    private readonly PetManager _petManager;
    private readonly CombatManager _combatManager;
    private readonly IRandomService _random;

    public PetService(PlayerService playerService, PetManager petManager, CombatManager combatManager, IRandomService random)
    {
        _playerService = playerService;
        _petManager = petManager;
        _combatManager = combatManager;
        _random = random;
    }

    public string CreatePet(int ownerId, int definitionId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return null;

        var def = _petManager.GetDefinition(definitionId);
        if (def == null) return null;

        if (session.Character.Pets.Count >= 5) return null;

        var pet = new ServerPet(def);
        session.Character.AddPet(pet);
        return pet.InstanceId;
    }

    public string CaptureEnemy(int ownerId, int enemyCombatantId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return null;

        // 1. Validate Target
        var target = _combatManager.GetCombatant(enemyCombatantId);
        if (target is not ServerEnemy enemy) return null; // Can only capture enemies

        // Security: Prevent capturing dead enemies (Anti-dupe)
        if (enemy.Hp <= 0) return null;

        // 2. Validate Enemy Definition
        if (!enemy.Definition.IsCapturable || enemy.Definition.PetTypeId == null) return null;

        // 3. Validate HP Threshold
        float hpPercent = (float)enemy.Hp / enemy.MaxHp;
        if (hpPercent > enemy.Definition.CaptureThreshold) return null; // Too healthy

        // 4. Validate Pet Definition Rules
        var petDef = _petManager.GetDefinition(enemy.Definition.PetTypeId.Value);
        if (petDef == null || petDef.CaptureRules == null || !petDef.CaptureRules.IsCapturable) return null;

        // 5. Level Check
        if (session.Character.Level < petDef.CaptureRules.LevelLimit) return null;

        // 6. Check Item Requirement & Consume (On Attempt)
        if (petDef.CaptureRules.RequiredItemId.HasValue)
        {
            if (!session.Character.RemoveItem(petDef.CaptureRules.RequiredItemId.Value, 1))
            {
                return null; // Missing item
            }
        }

        // 7. Calculate Chance
        float baseChance = petDef.CaptureRules.BaseChance;
        float hpBonus = (1.0f - hpPercent) * 0.5f; // Up to +50% capture rate if 0 HP
        float totalChance = baseChance + hpBonus;

        // 8. Roll
        if (_random.NextFloat() > totalChance)
        {
             return null; // Failed capture
        }

        // 9. Success!
        var pet = new ServerPet(petDef);
        pet.Amity = 40; // Default wild amity

        session.Character.AddPet(pet);

        // 12. Remove Enemy (Die)
        enemy.Die();
        // CombatManager will handle death event via subscription

        return pet.InstanceId;
    }

    public bool RevivePet(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        if (!pet.IsDead) return false;

        // Cost Logic could go here

        pet.Revive();
        return true;
    }

    public bool DismissPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return false;

        // If active, unregister first
        var active = session.Character.GetActivePet();
        if (active != null && active.InstanceId == petInstanceId)
        {
            _combatManager.UnregisterCombatant(active.Id);
            session.Character.SetActivePet(null);
        }

        return session.Character.RemovePet(petInstanceId);
    }

    public bool AddExperience(int ownerId, string petInstanceId, int amount)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        pet.AddExp(amount);
        return true;
    }

    public bool ModifyAmity(int ownerId, string petInstanceId, int amount)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        pet.ChangeAmity(amount);
        return true;
    }

    public bool TryRebirth(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        return pet.TryRebirth();
    }

    public bool SwitchPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return false;

        var chara = session.Character;
        var oldPet = chara.GetActivePet();

        // 1. Unregister existing pet from Combat
        if (oldPet != null)
        {
            _combatManager.UnregisterCombatant(oldPet.Id);

            // Note: If switching to the SAME pet, we unregister then register?
            // Usually switch means "Switch TO another".
            // If ID is same, unregister might remove it.
        }

        // 2. Set new Active Pet
        bool success = chara.SetActivePet(petInstanceId);
        if (!success) return false;

        var newPet = chara.GetActivePet();
        if (newPet != null)
        {
            // 3. Assign Runtime ID: -OwnerId
            // This ensures strict 1-pet-per-player mapping for easy identification
            newPet.Id = -ownerId;

            _combatManager.RegisterCombatant(newPet);
        }

        return true;
    }

    private ServerPet? GetPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return null;

        return session.Character.Pets.FirstOrDefault(p => p.InstanceId == petInstanceId);
    }
}
