namespace TWL.Server.Persistence;

public interface IPlayerRepository
{
    void Save(int userId, PlayerSaveData data);
    PlayerSaveData? Load(int userId);

    Task SaveAsync(int userId, PlayerSaveData data);
    Task<PlayerSaveData?> LoadAsync(int userId);
}
