using System.Collections.Generic;
using System.Threading.Tasks;

namespace TWL.Shared.Domain.Guilds;

public interface IGuildRepository
{
    Task<Guild?> LoadAsync(int guildId);
    Task<Guild?> LoadByNameAsync(string name);
    Task<IEnumerable<Guild>> LoadAllAsync();
    Task<bool> SaveAsync(Guild guild);
    Task DeleteAsync(int guildId);
}
