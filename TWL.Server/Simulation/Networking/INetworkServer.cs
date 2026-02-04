namespace TWL.Server.Simulation.Networking;

public interface INetworkServer
{
    int Port { get; }
    void Start();
    void Stop();
}
