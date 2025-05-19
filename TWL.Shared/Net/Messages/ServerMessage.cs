namespace TWL.Shared.Net.Messages;

public class ServerMessage
{
    public ServerMessageType MessageType { get; set; }
    public string Payload { get; set; }
}