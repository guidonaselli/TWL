// File: `TWL.Client/Managers/GameClientManager.cs`

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWL.Client.Presentation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Requests;
using TWL.Shared.Domain.State;
using TWL.Shared.Net.Network;

namespace TWL.Client.Presentation.Managers;

/// <summary>
///     Contains all client managers and exposes methods to process
///     server-sent results.
/// </summary>
public class GameClientManager
{
    // Example character lists
    private List<Character> _allies;
    private List<Character> _enemies;

    public GameClientManager(ILogger<NetworkClient> log)
    {
        var ip = "localhost";
        var port = 7777;

        NetworkClient = new NetworkClient(ip, port, this, log);
        var allies = new List<Character>();
        var enemies = new List<Character>();
        // Initialize example characters
        // allies.Add(new Character { Id = 101, Name = "Player1", Health = 100, MaxHealth = 100 });
        // enemies.Add(new Character { Id = 201, Name = "Slime", Health = 50, MaxHealth = 50 });

        CombatManager = new ClientCombatManager();
        var questDataManager = new QuestDataManager();
        QuestManager = new ClientQuestManager(questDataManager);
        InventoryManager = new ClientInventoryManager();
        MarketplaceManager = new ClientMarketplaceManager();
        TradeManager = new ClientTradeManager();
    }

    public Guid PlayerId { get; }
        = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public NetworkClient NetworkClient { get; }

    // Specific managers
    public ClientCombatManager CombatManager { get; }
    public ClientQuestManager QuestManager { get; }
    public ClientInventoryManager InventoryManager { get; }
    public ClientMarketplaceManager MarketplaceManager { get; }
    public ClientTradeManager TradeManager { get; }

    public PartyState Party { get; } = new();

    /// <summary>
    ///     Called by NetworkClient when a CombatResult is received from the server.
    /// </summary>
    public void HandleCombatResult(CombatResult result) => CombatManager.OnCombatResult(result);

    /// <summary>
    ///     Delegates a request to use a skill to the CombatManager.
    /// </summary>
    public void RequestUseSkill(int playerId, int targetId, int skillId) =>
        CombatManager.RequestUseSkill(playerId, targetId, skillId);

    /// <summary>
    ///     Called by NetworkClient when an InventoryUpdate is received from the server.
    /// </summary>
    public void HandleInventoryUpdate(InventoryUpdate update) => InventoryManager.OnInventoryUpdate(update);

    /// <summary>
    ///     Called by NetworkClient when a MarketplaceUpdate is received from the server.
    /// </summary>
    public void HandleMarketplaceUpdate(MarketplaceUpdate update) => MarketplaceManager.OnMarketplaceUpdate(update);

    // File: `TWL.Client/Managers/GameClientManager.cs`
    public void HandleQuestUpdate(QuestUpdate update) => QuestManager.OnQuestUpdate(update);

    /// \summary
    /// Returns the current list of allied characters.
    /// \summary>
    public List<Character> GetAllies() => _allies;

    /// \summary
    /// Returns the current list of enemy characters.
    /// \summary>
    public List<Character> GetEnemies() => _enemies;

    // Guild State
    public List<TWL.Shared.Domain.DTO.GuildMemberDto> GuildRoster { get; } = new();
    public List<TWL.Shared.Domain.DTO.GuildChatMessageDto> GuildChatLogs { get; } = new();

    public event Action? OnCompoundWindowRequested;
    public event Action<CompoundResponseDTO>? OnCompoundResponseReceived;

    public void HandleCompoundStartAck()
    {
        OnCompoundWindowRequested?.Invoke();
    }

    public void HandleCompoundResponse(CompoundResponseDTO response)
    {
        OnCompoundResponseReceived?.Invoke(response);
    }

    public void SendCompoundRequest(Guid targetId, Guid ingredientId, Guid? catalystId = null)
    {
        var request = new CompoundRequestDTO
        {
            TargetItemId = targetId,
            IngredientItemId = ingredientId,
            CatalystItemId = catalystId
        };
        NetworkClient.SendNetMessage(new NetMessage
        {
            Op = Opcode.CompoundRequest,
            JsonPayload = JsonSerializer.Serialize(request)
        });
    }

    public void HandleGuildRosterSync(TWL.Shared.Domain.DTO.GuildRosterSyncEvent syncEvent)
    {
        GuildRoster.Clear();
        GuildRoster.AddRange(syncEvent.Members);
    }

    public void HandleGuildRosterUpdate(TWL.Shared.Domain.DTO.GuildRosterUpdateEvent updateEvent)
    {
        var existing = GuildRoster.FirstOrDefault(m => m.CharacterId == updateEvent.Member.CharacterId);
        if (updateEvent.IsRemoved)
        {
            if (existing != null) GuildRoster.Remove(existing);
        }
        else
        {
            if (existing != null) GuildRoster.Remove(existing);
            GuildRoster.Add(updateEvent.Member);
        }
    }

    public void HandleGuildChatEvent(TWL.Shared.Domain.DTO.GuildChatEvent chatEvent)
    {
        GuildChatLogs.Add(chatEvent.Message);
    }

    public void HandleGuildChatBacklog(TWL.Shared.Domain.DTO.GuildChatBacklog backlog)
    {
        GuildChatLogs.AddRange(backlog.Messages);
        GuildChatLogs.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
    }

    public event Action<JsonElement>? OnStatsUpdated;

    public void HandleStatsUpdate(JsonElement payload)
    {
        OnStatsUpdated?.Invoke(payload);
    }
}
