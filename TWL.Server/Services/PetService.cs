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
        // If Player Level is significantly lower than Pet Capture Limit?
        // Actually usually strictly "Player Level >= Pet Level + X" or similar.
        // Or "LevelLimit" in CaptureRules means "Required Player Level".
        if (session.Character.Level < petDef.CaptureRules.LevelLimit) return null;

        // 6. Calculate Chance
        // Base Chance + Bonus for low HP
        float baseChance = petDef.CaptureRules.BaseChance;
        float hpBonus = (1.0f - hpPercent) * 0.5f; // Up to +50% capture rate if 0 HP (impossible but close)
        float totalChance = baseChance + hpBonus;

        // 7. Roll
        if (_random.NextFloat() > totalChance)
        {
             return null; // Failed capture
        }

        // 8. Success!
        var pet = new ServerPet(petDef);
        pet.Amity = 40; // Default wild amity

        if (session.Character.Pets.Count >= 5) // Hardcoded slot limit for now
        {
            // Or send to bank? fail for now.
            return null;
        }

        session.Character.AddPet(pet);

        // 9. Remove Enemy (Die)
        enemy.Die();
        // CombatManager should handle death event

        return pet.InstanceId;
    }

    public bool RevivePet(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        if (!pet.IsDead) return false;

        // Cost Logic?
        // E.g. consume Gold or Item.
        // For now, free or minimal check.
        // if (owner.Gold < 100) return false;

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

        if (oldPet != null)
        {
            // Despawn old pet from combat
            _combatManager.UnregisterCombatant(oldPet.Id);

            // Should consume turn? logic handled by Handler, not Service usually.
        }

        bool success = chara.SetActivePet(petInstanceId);
        if (!success) return false;

        var newPet = chara.GetActivePet();
        if (newPet != null)
        {
            // Assign runtime ID for combat: Negative OwnerID - Slot Index?
            // Or just ensure unique ID. ServerCombatant.Id usually must be unique.
            // If we use negative IDs for pets: -1000 * PlayerId - SlotId?
            // For now, simple approach:

            // Important: We must not conflict with other entities.
            // ServerCharacter uses its Database ID (positive).
            // Mobs use... something.
            // Pets should use generated IDs or transient IDs.
            // Let's assume GetHashCode or similar for now, or just negative random.
            newPet.Id = -System.Math.Abs(petInstanceId.GetHashCode());

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
