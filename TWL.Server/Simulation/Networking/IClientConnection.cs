using System.Threading.Tasks;

namespace TWL.Server.Simulation.Networking;

public interface IClientConnection
{
    Task SendMessageAsync(byte[] data);
    void Disconnect();
}
