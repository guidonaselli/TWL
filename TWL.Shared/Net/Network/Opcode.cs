namespace TWL.Shared.Net.Network;

public enum Opcode
{
    // Autenticación
    LoginRequest,
    LoginResponse,

    // Movimiento
    MoveRequest,
    MoveBroadcast,

    // Combate
    AttackRequest,
    AttackBroadcast,

    // Ítem / Inventario
    UseItemRequest,
    InventoryUpdate,

    // Quest
    StartQuestRequest,
    QuestUpdateBroadcast

    // etc. los que necesites
}