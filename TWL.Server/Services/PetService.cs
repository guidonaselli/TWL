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

    public PetService(PlayerService playerService, PetManager petManager, CombatManager combatManager)
    {
        _playerService = playerService;
        _petManager = petManager;
        _combatManager = combatManager;
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

    public string CapturePet(int ownerId, int petTypeId, float roll)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return null;

        var def = _petManager.GetDefinition(petTypeId);
        if (def == null || !def.IsCapturable) return null;

        if (session.Character.Level < def.CaptureLevelLimit) return null;
        if (roll > def.CaptureChance) return null;

        var pet = new ServerPet(def);
        pet.Amity = 40;

        session.Character.AddPet(pet);
        return pet.InstanceId;
    }

    public bool RevivePet(int ownerId, string petInstanceId)
    {
        var pet = GetPet(ownerId, petInstanceId);
        if (pet == null) return false;

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
            _combatManager.UnregisterCombatant(oldPet.Id);
        }

        bool success = chara.SetActivePet(petInstanceId);
        if (!success) return false;

        var newPet = chara.GetActivePet();
        if (newPet != null)
        {
            // Assign runtime ID for combat: Negative OwnerID
            // This ensures 1 pet per owner in combat map
            newPet.Id = -chara.Id;
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
