using System.Threading.Tasks;

namespace TWL.Server.Simulation.Managers;

public interface IPartyChatService
{
    Task SendPartyMessageAsync(int partyId, int senderId, string senderName, string content);
}
