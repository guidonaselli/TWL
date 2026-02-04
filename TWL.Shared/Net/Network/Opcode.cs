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
    ClaimRewardRequest,
    QuestUpdateBroadcast,

    // Interacción
    InteractRequest,

    // Economy
    PurchaseGemsIntent,
    PurchaseGemsVerify,
    BuyShopItemRequest,

    // Pet
    PetActionRequest,
    PetActionResponse,

    // Encounter
    EncounterStarted,

    // Status
    StatsUpdate,

    // System
    Disconnect
}