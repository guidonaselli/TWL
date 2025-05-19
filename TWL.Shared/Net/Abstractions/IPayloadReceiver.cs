namespace TWL.Shared.Net.Abstractions; // o el que prefieras

public interface IPayloadReceiver
{
    void ReceivePayload(object payload);
}