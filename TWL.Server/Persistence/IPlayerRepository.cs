using System.Threading.Tasks;

namespace TWL.Server.Persistence;

public interface IPlayerRepository
{
    Task SaveAsync(int userId, PlayerSaveData data);
    PlayerSaveData? Load(int userId);
}
