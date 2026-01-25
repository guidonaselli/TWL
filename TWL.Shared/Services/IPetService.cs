namespace TWL.Shared.Services;

public interface IPetService
{
    string CreatePet(int ownerId, int definitionId);
    bool AddExperience(int ownerId, string petInstanceId, int amount);
    bool ModifyAmity(int ownerId, string petInstanceId, int amount);
    bool TryRebirth(int ownerId, string petInstanceId);
    bool SwitchPet(int ownerId, string petInstanceId);
}
