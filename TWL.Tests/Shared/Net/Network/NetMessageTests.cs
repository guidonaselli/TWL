using System.Text;
using TWL.Shared.Net.Network;

namespace TWL.Tests.Shared.Net.Network;

public class NetMessageTests
{
    [Fact]
    public void Deserialize_ValidJson_ReturnsNetMessage()
    {
        var opCode = (int)Opcode.LoginRequest;
        var json = $"{{\"Op\": {opCode}, \"JsonPayload\": \"test\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var msg = NetMessage.Deserialize(bytes, bytes.Length);

        Assert.NotNull(msg);
        Assert.Equal(Opcode.LoginRequest, msg.Op);
        Assert.Equal("test", msg.JsonPayload);
    }

    [Fact]
    public void Deserialize_CaseInsensitive_ReturnsNetMessage()
    {
        var opCode = (int)Opcode.LoginRequest;
        var json = $"{{\"op\": {opCode}, \"jsonPayload\": \"test\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var msg = NetMessage.Deserialize(bytes, bytes.Length);

        Assert.NotNull(msg);
        Assert.Equal(Opcode.LoginRequest, msg.Op);
        Assert.Equal("test", msg.JsonPayload);
    }
}