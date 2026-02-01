namespace TWL.Server.Persistence;

public interface IPlayerRepository
{
    PlayerSaveData? Load(int userId);

    Task SaveAsync(int userId, PlayerSaveData data);

    Task<PlayerSaveData?> LoadAsync(int userId);
}