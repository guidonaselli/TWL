using Microsoft.Extensions.Logging;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Server.Services;

public class PetService : IPetService
{
    private readonly CombatManager _combatManager;
    private readonly PetManager _petManager;
    private readonly MonsterManager _monsterManager;
    private readonly PlayerService _playerService;
    private readonly IRandomService _random;
    private readonly ILogger<PetService> _logger;

    public PetService(PlayerService playerService, PetManager petManager, MonsterManager monsterManager,
        CombatManager combatManager, IRandomService random, ILogger<PetService> logger)
    {
        _playerService = playerService;
        _petManager = petManager;
        _monsterManager = monsterManager;
        _combatManager = combatManager;
        _random = random;
        _logger = logger;

        _combatManager.OnCombatantDeath += HandlePetDeath;
    }

    public string CreatePet(int ownerId, int definitionId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return null;
        }

        var def = _petManager.GetDefinition(definitionId);
        if (def == null)
        {
            return null;
        }

        if (session.Character.Pets.Count >= 5)
        {
            return null;
        }

        var pet = new ServerPet(def);
        session.Character.AddPet(pet);
        return pet.InstanceId;
    }

    public string CaptureEnemy(int ownerId, int enemyCombatantId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return null;
        }

        // Ownership Limit Check
        if (session.Character.Pets.Count >= 5)
        {
            _logger.LogWarning("Capture failed: Owner {OwnerId} has full pet slots.", ownerId);
            return null;
        }

        // 1. Validate Target
        var target = _combatManager.GetCombatant(enemyCombatantId);
        if (target is not ServerCharacter enemy || enemy.Team != Team.Enemy)
        {
            return null; // Can only capture enemies (ServerCharacter)
        }

        // Security: Prevent capturing dead enemies (Anti-dupe)
        if (enemy.Hp <= 0)
        {
            return null;
        }

        // 2. Validate Enemy Definition via MonsterManager
        var monsterDef = _monsterManager.GetDefinition(enemy.MonsterId);
        if (monsterDef == null)
        {
            return null;
        }

        if (!monsterDef.IsCapturable || monsterDef.PetTypeId == null)
        {
            return null;
        }

        // 3. Validate HP Threshold
        var hpPercent = (float)enemy.Hp / enemy.MaxHp;
        if (hpPercent > monsterDef.CaptureThreshold)
        {
            return null; // Too healthy
        }

        // 4. Validate Pet Definition Rules
        var petDef = _petManager.GetDefinition(monsterDef.PetTypeId.Value);
        if (petDef == null || petDef.CaptureRules == null || !petDef.CaptureRules.IsCapturable)
        {
            return null;
        }

        // 5. Level Check
        if (session.Character.Level < petDef.CaptureRules.LevelLimit)
        {
            return null;
        }

        // 6. Check Required Flag
        if (!string.IsNullOrEmpty(petDef.CaptureRules.RequiredFlag))
        {
            if (!session.QuestComponent.Flags.Contains(petDef.CaptureRules.RequiredFlag))
            {
                return null;
            }
        }

        // 7. Check Item Requirement & Consume (On Attempt)
        if (petDef.CaptureRules.RequiredItemId.HasValue)
        {
            if (!session.Character.RemoveItem(petDef.CaptureRules.RequiredItemId.Value, 1))
            {
                return null; // Missing item
            }
        }

        // 8. Calculate Chance
        var baseChance = petDef.CaptureRules.BaseChance;
        var hpBonus = (1.0f - hpPercent) * 0.5f; // Up to +50% capture rate if 0 HP
        var totalChance = baseChance + hpBonus;

        // 8. Roll
        if (_random.NextFloat("CapturePetRoll") > totalChance)
        {
            return null; // Failed capture
        }

        // 9. Success!
        var pet = new ServerPet(petDef);
        pet.Amity = 40; // Default wild amity

        session.Character.AddPet(pet);
        _logger.LogInformation("Pet captured: {PetName} by {OwnerId}. Instance: {PetId}", pet.Name, ownerId, pet.InstanceId);

        // 12. Remove Enemy (Die)
        enemy.Hp = 0; // Force death
        _combatManager.UnregisterCombatant(enemy.Id); // Or better: trigger death logic

        return pet.InstanceId;
    }

    public const int ItemRevive1Hp = 801;
    public const int ItemRevive100Hp = 802;
    public const int ItemRevive500Hp = 803;

    public bool RevivePet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return false;
        }

        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        if (!pet.IsDead)
        {
            return false;
        }

        if (pet.IsExpired)
        {
            return false; // Cannot revive expired pets
        }

        // Priority: Use cheapest/lowest HP item first
        if (session.Character.RemoveItem(ItemRevive1Hp, 1))
        {
            pet.Revive(1);
            _logger.LogInformation("Pet {PetId} revived with 1 HP by {OwnerId}", petInstanceId, ownerId);
            return true;
        }

        if (session.Character.RemoveItem(ItemRevive100Hp, 1))
        {
            pet.Revive(100);
            _logger.LogInformation("Pet {PetId} revived with 100 HP by {OwnerId}", petInstanceId, ownerId);
            return true;
        }

        if (session.Character.RemoveItem(ItemRevive500Hp, 1))
        {
            pet.Revive(500);
            _logger.LogInformation("Pet {PetId} revived with 500 HP by {OwnerId}", petInstanceId, ownerId);
            return true;
        }

        return false;
    }

    public bool DismissPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return false;
        }

        // If active, unregister first
        var active = session.Character.GetActivePet();
        if (active != null && active.InstanceId == petInstanceId)
        {
            _combatManager.UnregisterCombatant(active.Id);
            session.Character.SetActivePet(null);
        }

        _logger.LogInformation("Pet {PetId} dismissed (abandoned) by {OwnerId}", petInstanceId, ownerId);
        return session.Character.RemovePet(petInstanceId);
    }

    public bool AddExperience(int ownerId, string petInstanceId, int amount)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        pet.AddExp(amount);
        return true;
    }

    public bool ModifyAmity(int ownerId, string petInstanceId, int amount)
    {
        return AwardAmity(ownerId, petInstanceId, amount, "Manual Modification");
    }

    public bool AwardAmity(int ownerId, string petInstanceId, int amount, string reason)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        pet.ChangeAmity(amount);
        _logger.LogInformation("Pet {PetId} Amity changed by {Amount} ({Reason}). New Amity: {Amity}", petInstanceId, amount, reason, pet.Amity);
        return true;
    }

    public void ProcessCombatWin(int ownerId, string petInstanceId)
    {
        AwardAmity(ownerId, petInstanceId, 1, "Combat Win");
    }

    public bool TryRebirth(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        if (pet.TryRebirth())
        {
             _logger.LogInformation("Pet {PetId} Reborn! Owner: {OwnerId}", petInstanceId, ownerId);
             return true;
        }
        return false;
    }

    public bool SwitchPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return false;
        }

        var chara = session.Character;

        // Cooldown Check (e.g. 60 seconds)
        if (DateTime.UtcNow < chara.LastPetSwitchTime.AddSeconds(60))
        {
            return false;
        }

        var targetPet = GetPet(ownerId, petInstanceId);
        if (targetPet == null || targetPet.IsExpired || targetPet.IsDead)
        {
            return false;
        }

        var oldPet = chara.GetActivePet();
        var inCombat = oldPet != null && oldPet.EncounterId > 0;

        // 1. Unregister existing pet from Combat
        if (oldPet != null)
        {
            _combatManager.UnregisterCombatant(oldPet.Id);
        }

        // 2. Set new Active Pet
        var success = chara.SetActivePet(petInstanceId);
        if (!success)
        {
            return false;
        }

        chara.LastPetSwitchTime = DateTime.UtcNow;

        var newPet = chara.GetActivePet();
        if (newPet != null)
        {
            // 3. Assign Runtime ID: -OwnerId
            // This ensures strict 1-pet-per-player mapping for easy identification
            newPet.Id = -ownerId;
            // Inherit EncounterId if switching during combat
            if (inCombat)
            {
                newPet.EncounterId = oldPet.EncounterId;

                // Consumes Turn: Apply 1 turn cooldown to all skills
                foreach(var skillId in newPet.UnlockedSkillIds)
                {
                    newPet.SetSkillCooldown(skillId, 1);
                }
            }

            _combatManager.RegisterCombatant(newPet);
        }

        return true;
    }

    private void HandlePetDeath(ServerCombatant victim)
    {
        if (victim is ServerPet pet)
        {
            pet.Die();
            // Amity loss is handled in Die(), logging here
             _logger.LogInformation("Pet {PetId} died in combat. Amity reduced.", pet.InstanceId);
        }
    }

    public bool UseUtility(int ownerId, string petInstanceId, PetUtilityType type, string? args = null)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return false;
        }

        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        var value = pet.GetUtilityValue(type);
        if (value <= 0)
        {
            return false;
        }

        var chara = session.Character;

        switch (type)
        {
            case PetUtilityType.Mount:
                if (chara.IsMounted)
                {
                    chara.IsMounted = false;
                    chara.MoveSpeedModifier = 1.0f;
                }
                else
                {
                    chara.IsMounted = true;
                    // value is treated as the bonus (e.g. 0.5 -> 1.5x speed)
                    chara.MoveSpeedModifier = 1.0f + value;
                }

                break;

            case PetUtilityType.Gathering:
                // Toggle Gathering Mode/Bonus
                if (chara.GatheringBonus > 0)
                {
                    chara.GatheringBonus = 0f;
                }
                else
                {
                    chara.GatheringBonus = value;
                }

                break;

            case PetUtilityType.CraftingAssist:
                // Toggle Crafting Assist
                if (chara.CraftingAssistBonus > 0)
                {
                    chara.CraftingAssistBonus = 0f;
                }
                else
                {
                    chara.CraftingAssistBonus = value;
                }

                break;

            case PetUtilityType.Delivery:
                // Delivery: Send Item to Bank
                // args: slotIndex (int)
                if (string.IsNullOrEmpty(args) || !int.TryParse(args, out var slotIndex))
                {
                    return false;
                }

                // Get Item from Inventory (Snapshot)
                // Since we rely on index, we need to be careful with concurrency, but UseUtility is likely sequential per user request.
                // However, Character.Inventory is thread-safe snapshot.
                var inventory = chara.Inventory;
                if (slotIndex < 0 || slotIndex >= inventory.Count)
                {
                    return false;
                }

                var item = inventory[slotIndex];

                // Attempt to remove EXACTLY this item (using policy filter if needed, but here we assume unbound/bound specific)
                // RemoveItem takes quantity. We move the whole stack.
                if (chara.RemoveItem(item.ItemId, item.Quantity, item.Policy))
                {
                    // Success removed -> Add to Bank
                    chara.AddToBank(item);
                    _logger.LogInformation("Pet {PetId} delivered Item {ItemId} x{Qty} to Bank for Owner {OwnerId}",
                        petInstanceId, item.ItemId, item.Quantity, ownerId);
                }
                else
                {
                    return false;
                }

                break;

            default:
                return false;
        }

        _logger.LogInformation("Pet Utility {UtilityType} used by {OwnerId} with pet {PetId}", type, ownerId, petInstanceId);
        return true;
    }

    public bool UseAmityItem(int ownerId, string petInstanceId, int itemId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return false;
        }

        var amityGain = _petManager.GetAmityValue(itemId);
        if (amityGain <= 0)
        {
            return false;
        }

        if (session.Character.RemoveItem(itemId, 1))
        {
            return AwardAmity(ownerId, petInstanceId, amityGain, $"Item {itemId} Used");
        }
        return false;
    }

    public void CheckPetAvailability(int ownerId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return;
        }

        var flags = session.QuestComponent.Flags;
        var petsToRemove = new List<ServerPet>();

        lock (session.Character.Pets) // Ensure thread safety if iterating
        {
            foreach (var pet in session.Character.Pets)
            {
                // Check Definition Flag (for Quest Pets/Permanence)
                // Note: We need access to definition. ServerPet has DefinitionId.
                var def = _petManager.GetDefinition(pet.DefinitionId);
                if (def != null && !string.IsNullOrEmpty(def.RequiredFlag))
                {
                    if (!flags.Contains(def.RequiredFlag))
                    {
                        petsToRemove.Add(pet);
                    }
                }
            }
        }

        foreach (var pet in petsToRemove)
        {
            DismissPet(ownerId, pet.InstanceId);
            _logger.LogInformation("Pet {PetId} removed due to missing flag.", pet.InstanceId);
        }
    }

    private ServerPet? GetPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return null;
        }

        return session.Character.Pets.FirstOrDefault(p => p.InstanceId == petInstanceId);
    }
}
