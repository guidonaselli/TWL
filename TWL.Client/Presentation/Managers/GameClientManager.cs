// File: `TWL.Client/Managers/GameClientManager.cs`

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TWL.Client.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Requests;

namespace TWL.Client.Presentation.Managers;

/// <summary>
///     Contains all client managers and exposes methods to process
///     server-sent results.
/// </summary>
public class GameClientManager
{

    public Guid PlayerId { get; }
        = Guid.Parse("00000000-0000-0000-0000-000000000001");


    // Example character lists
    private List<Character> _allies;
    private List<Character> _enemies;

    public NetworkClient NetworkClient { get; }

    public GameClientManager(ILogger<NetworkClient> log)
    {
        var ip = "localhost";
        var port = 7777;

        NetworkClient = new NetworkClient(ip, port, this, log);
        _allies = new List<Character>();
        _enemies = new List<Character>();
        // Initialize example characters
        // _allies.Add(new Character { Id = 101, Name = "Player1", Health = 100, MaxHealth = 100 });
        // _enemies.Add(new Character { Id = 201, Name = "Slime", Health = 50, MaxHealth = 50 });

        CombatManager = new ClientCombatManager();
        var questDataManager = new QuestDataManager();
        QuestManager = new ClientQuestManager(questDataManager);
        InventoryManager = new ClientInventoryManager();
        MarketplaceManager = new ClientMarketplaceManager();
    }

    // Specific managers
    public ClientCombatManager CombatManager { get; }
    public ClientQuestManager QuestManager { get; }
    public ClientInventoryManager InventoryManager { get; }
    public ClientMarketplaceManager MarketplaceManager { get; }

    /// <summary>
    ///     Called by NetworkClient when a CombatResult is received from the server.
    /// </summary>
    public void HandleCombatResult(CombatResult result)
    {
        CombatManager.OnCombatResult(result);
    }

    /// <summary>
    ///     Delegates a request to use a skill to the CombatManager.
    /// </summary>
    public void RequestUseSkill(int playerId, int targetId, int skillId)
    {
        CombatManager.RequestUseSkill(playerId, targetId, skillId);
    }

    /// <summary>
    ///     Called by NetworkClient when an InventoryUpdate is received from the server.
    /// </summary>
    public void HandleInventoryUpdate(InventoryUpdate update)
    {
        InventoryManager.OnInventoryUpdate(update);
    }

    /// <summary>
    ///     Called by NetworkClient when a MarketplaceUpdate is received from the server.
    /// </summary>
    public void HandleMarketplaceUpdate(MarketplaceUpdate update)
    {
        MarketplaceManager.OnMarketplaceUpdate(update);
    }

    // File: `TWL.Client/Managers/GameClientManager.cs`
    public void HandleQuestUpdate(QuestUpdate update)
    {
        QuestManager.OnQuestUpdate(update);
    }

    /// \summary
    /// Returns the current list of allied characters.
    /// \summary>
    public List<Character> GetAllies()
    {
        return _allies;
    }

    /// \summary
    /// Returns the current list of enemy characters.
    /// \summary>
    public List<Character> GetEnemies()
    {
        return _enemies;
    }
}