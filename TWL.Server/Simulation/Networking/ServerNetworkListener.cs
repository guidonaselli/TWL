using Newtonsoft.Json;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Net.Messages;

namespace TWL.Server.Simulation.Networking;

public class ServerNetworkListener
{
    private readonly CombatManager _combatManager;
    private readonly NetworkServer _network;

    public ServerNetworkListener(CombatManager combatManager, NetworkServer network)
    {
        _combatManager = combatManager;
        _network = network;
    }

    public void OnReceiveMessage(ClientMessage msg)
    {
        switch (msg.MessageType)
        {
            case ClientMessageType.UseSkill:
                // Deserializamos la petición
                var req = JsonConvert.DeserializeObject<UseSkillRequest>(msg.Payload);
                var combatResult = _combatManager.UseSkill(req);

                if (combatResult != null)
                {
                    // Empaquetar la respuesta
                    var serverMsg = new ServerMessage
                    {
                        MessageType = ServerMessageType.CombatResult,
                        Payload = JsonConvert.SerializeObject(combatResult)
                    };
                    SendToClient(req.PlayerId, serverMsg);
                }

                break;

            // Casos adicionales para otros tipos de mensaje
        }
    }

    private void SendToClient(int playerId, ServerMessage msg)
    {
        _network.SendMessageToClient(playerId, msg);
    }
}