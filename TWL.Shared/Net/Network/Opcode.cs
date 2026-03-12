namespace TWL.Shared.Net.Network;

public enum Opcode
{
    // Autenticación
    LoginRequest,
    LoginResponse,

    // Movimiento
    MoveRequest,
    MoveBroadcast,
    MapChange,

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

    // Party
    PartyInviteRequest,
    PartyInviteResponse,
    PartyInviteReceived,
    PartyAcceptInvite,
    PartyDeclineInvite,
    PartyLeaveRequest,
    PartyKickRequest,
    PartyKickResponse,
    PartyUpdateBroadcast,

    PartyUpdateFormationRequest,

    PartyChatRequest,
    PartyChatBroadcast,

    // Guild
    GuildCreateRequest,
    GuildCreateResponse,
    GuildInviteRequest,
    GuildInviteResponse,
    GuildInviteReceived,
    GuildAcceptInvite,
    GuildDeclineInvite,
    GuildLeaveRequest,
    GuildLeaveResponse,
    GuildKickRequest,
    GuildKickResponse,
    GuildPromoteRequest,
    GuildPromoteResponse,
    GuildDemoteRequest,
    GuildDemoteResponse,
    GuildUpdateBroadcast,
    GuildChatRequest,
    GuildChatBroadcast,
    GuildChatBacklog,
    GuildRosterSync,
    GuildRosterUpdate,

    GuildStorageViewRequest,
    GuildStorageViewEvent,
    GuildStorageDepositRequest,
    GuildStorageWithdrawRequest,
    GuildStorageUpdateEvent,
    GuildStorageOperationResultEvent,

    // Status
    StatsUpdate,

    // Rebirth
    CharacterRebirthRequest,
    CharacterRebirthResponse,

    // System
    Disconnect,
    SystemMessage
}