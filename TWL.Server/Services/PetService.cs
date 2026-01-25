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

    public PetService(PlayerService playerService, PetManager petManager)
    {
        _playerService = playerService;
        _petManager = petManager;
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

        return session.Character.SetActivePet(petInstanceId);
    }

    private ServerPet? GetPet(int ownerId, string petInstanceId)
    {
        var session = _playerService.GetSession(ownerId);
        if (session == null || session.Character == null) return null;

        return session.Character.Pets.FirstOrDefault(p => p.InstanceId == petInstanceId);
    }
}
