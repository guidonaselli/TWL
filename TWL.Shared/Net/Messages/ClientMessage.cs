namespace TWL.Shared.Net.Messages;

public class ClientMessage
{
    public ClientMessageType MessageType { get; set; }
    public string Payload { get; set; } // JSON string u otro formato
}