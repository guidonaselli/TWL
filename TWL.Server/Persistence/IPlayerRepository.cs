using System.Threading.Tasks;

namespace TWL.Server.Persistence;

public interface IPlayerRepository
{
    PlayerSaveData? Load(int userId);

    Task<PlayerSaveData?> LoadAsync(int userId);
}
