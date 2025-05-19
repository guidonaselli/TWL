namespace TWL.Shared.Net.Network;

public interface INetworkChannel
{
    void Send<T>(T packet);
    event Action<byte[]> OnPacket;
}