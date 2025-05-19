namespace TWL.Shared.Net.Network;

[Serializable]
public class NetMessage
{
    public Opcode Op { get; set; }
    public string JsonPayload { get; set; }
}