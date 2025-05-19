// LoopbackChannel.cs   –  TWL.Client/Networking

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using TWL.Shared.Net.Network;

namespace TWL.Client.Presentation.Networking;

public sealed class LoopbackChannel : INetworkChannel
{
    private readonly Channel<byte[]> _queue = Channel.CreateUnbounded<byte[]>();
    public event Action<byte[]>? OnPacket;

    public LoopbackChannel()
    {
        _ = Task.Run(async () =>
        {
            await foreach (var pkt in _queue.Reader.ReadAllAsync())
                OnPacket?.Invoke(pkt);
        });
    }

    public void Send<T>(T packet)
    {
        var bytes = MessagePackSerializer.Serialize(packet);
        _queue.Writer.TryWrite(bytes);
    }
}