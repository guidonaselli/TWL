using System.Text.Json;
using TWL.Server.Persistence;
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

    public const int ItemRevive1Hp = 801;
    public const int ItemRevive100Hp = 802;
    public const int ItemRevive500Hp = 803;

    public PetService(PlayerService playerService, PetManager petManager, MonsterManager monsterManager,
        CombatManager combatManager, IRandomService random)
    {
        _playerService = playerService;
        _petManager = petManager;
        _monsterManager = monsterManager;
        _combatManager = combatManager;
        _random = random;

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

        // 6. Check Item Requirement & Consume (On Attempt)
        if (petDef.CaptureRules.RequiredItemId.HasValue)
        {
            if (!session.Character.RemoveItem(petDef.CaptureRules.RequiredItemId.Value, 1))
            {
                return null; // Missing item
            }
        }

        // 7. Calculate Chance
        var baseChance = petDef.CaptureRules.BaseChance;
        var hpBonus = (1.0f - hpPercent) * 0.5f; // Up to +50% capture rate if 0 HP
        var totalChance = baseChance + hpBonus;

        // 8. Roll
        if (_random.NextFloat() > totalChance)
        {
            return null; // Failed capture
        }

        // 9. Success!
        var pet = new ServerPet(petDef);

        session.Character.AddPet(pet);

        // Initialize Amity for Capture
        ProcessAmity(ownerId, pet.InstanceId, AmityAction.Capture);

        // 12. Remove Enemy (Die)
        enemy.Hp = 0; // Force death
        _combatManager.UnregisterCombatant(enemy.Id); // Or better: trigger death logic

        return pet.InstanceId;
    }

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
            return true;
        }

        if (session.Character.RemoveItem(ItemRevive100Hp, 1))
        {
            pet.Revive(100);
            return true;
        }

        if (session.Character.RemoveItem(ItemRevive500Hp, 1))
        {
            pet.Revive(500);
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

        return session.Character.RemovePet(petInstanceId);
    }

    public bool AddExperience(int ownerId, string petInstanceId, int amount)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        var oldLevel = pet.Level;
        pet.AddExp(amount);

        if (pet.Level > oldLevel)
        {
            ProcessAmity(ownerId, petInstanceId, AmityAction.LevelUp);
        }

        return true;
    }

    public bool ModifyAmity(int ownerId, string petInstanceId, int amount)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        pet.ChangeAmity(amount);
        return true;
    }

    public bool ProcessAmity(int ownerId, string petInstanceId, AmityAction action)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

        int change = 0;
        switch (action)
        {
            case AmityAction.BattleWin:
                // Only gain amity if not already maxed, maybe with diminishing returns or RNG
                if (pet.Amity < 100)
                {
                    change = 1;
                }
                break;
            case AmityAction.Death:
                change = -10; // Significant penalty
                break;
            case AmityAction.Feed:
                change = 5; // Placeholder for feed logic
                break;
            case AmityAction.LevelUp:
                change = 2;
                break;
            case AmityAction.StayOnline:
                change = 1;
                break;
            case AmityAction.Capture:
                // Reset/Set to wild baseline (40)
                change = 40 - pet.Amity;
                break;
            case AmityAction.Abandon:
                change = -50;
                break;
        }

        if (change != 0)
        {
            pet.ChangeAmity(change);
        }
        return true;
    }

    public bool TryRebirth(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null)
        {
            return false;
        }

        return pet.TryRebirth();
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

            _combatManager.RegisterCombatant(newPet);
        }

        return true;
    }

    private void HandlePetDeath(ServerCombatant victim)
    {
        if (victim is ServerPet pet)
        {
            pet.Die();

            // Deduce Owner ID from Runtime ID if it follows the convention -OwnerId
            if (pet.Id < 0)
            {
                var ownerId = -pet.Id;
                ProcessAmity(ownerId, pet.InstanceId, AmityAction.Death);
            }
        }
    }

    public bool UseUtility(int ownerId, string petInstanceId, PetUtilityType type)
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
            default:
                // Not implemented or unsupported type
                return false;
        }

        return true;
    }

    public void CheckLifecycle(int ownerId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null)
        {
            return;
        }

        // Use a copy list to modify the collection safely
        var expiredPets = session.Character.Pets.Where(p => p.IsExpired).ToList();
        foreach (var pet in expiredPets)
        {
             DismissPet(ownerId, pet.InstanceId);
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
