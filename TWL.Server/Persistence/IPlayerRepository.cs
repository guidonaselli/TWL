namespace TWL.Server.Persistence;

public interface IPlayerRepository
{
    void Save(int userId, PlayerSaveData data);
    PlayerSaveData? Load(int userId);
}
