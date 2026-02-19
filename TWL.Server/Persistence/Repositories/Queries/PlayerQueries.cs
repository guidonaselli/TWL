namespace TWL.Server.Persistence.Repositories.Queries;

/// <summary>
/// Dapper SQL queries for high-performance player data reads.
/// All column aliases match PlayerDto property names for automatic mapping.
/// </summary>
public static class PlayerQueries
{
    public const string LoadByUserId = @"
        SELECT
            player_id AS PlayerId,
            user_id AS UserId,
            name AS Name,
            hp AS Hp, sp AS Sp,
            level AS Level,
            rebirth_level AS RebirthLevel,
            exp AS Exp,
            exp_to_next_level AS ExpToNextLevel,
            stat_points AS StatPoints,
            str AS Str, con AS Con, ""int"" AS Int, wis AS Wis, agi AS Agi,
            element AS Element,
            gold AS Gold,
            premium_currency AS PremiumCurrency,
            daily_gift_accumulator AS DailyGiftAccumulator,
            last_gift_reset_date AS LastGiftResetDate,
            map_id AS MapId, x AS X, y AS Y,
            active_pet_instance_id AS ActivePetInstanceId,
            inventory_json AS InventoryJson,
            equipment_json AS EquipmentJson,
            bank_json AS BankJson,
            pets_json AS PetsJson,
            skills_json AS SkillsJson,
            world_flags_json AS WorldFlagsJson,
            processed_orders_json AS ProcessedOrdersJson,
            instance_lockouts_json AS InstanceLockoutsJson,
            quest_states_json AS QuestStatesJson,
            quest_progress_json AS QuestProgressJson,
            quest_flags_json AS QuestFlagsJson,
            quest_completion_times_json AS QuestCompletionTimesJson,
            quest_start_times_json AS QuestStartTimesJson,
            last_saved AS LastSaved
        FROM players
        WHERE user_id = @UserId";
}

/// <summary>
/// Internal DTO for Dapper result mapping. Property names match SQL aliases exactly.
/// </summary>
public class PlayerDto
{
    public int PlayerId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Hp { get; set; }
    public int Sp { get; set; }
    public int Level { get; set; }
    public int RebirthLevel { get; set; }
    public int Exp { get; set; }
    public int ExpToNextLevel { get; set; }
    public int StatPoints { get; set; }
    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Agi { get; set; }
    public int Element { get; set; }
    public int Gold { get; set; }
    public long PremiumCurrency { get; set; }
    public long DailyGiftAccumulator { get; set; }
    public DateTime LastGiftResetDate { get; set; }
    public int MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string? ActivePetInstanceId { get; set; }
    public string InventoryJson { get; set; } = "[]";
    public string EquipmentJson { get; set; } = "[]";
    public string BankJson { get; set; } = "[]";
    public string PetsJson { get; set; } = "[]";
    public string SkillsJson { get; set; } = "[]";
    public string WorldFlagsJson { get; set; } = "[]";
    public string ProcessedOrdersJson { get; set; } = "[]";
    public string InstanceLockoutsJson { get; set; } = "{}";
    public string QuestStatesJson { get; set; } = "{}";
    public string QuestProgressJson { get; set; } = "{}";
    public string QuestFlagsJson { get; set; } = "[]";
    public string QuestCompletionTimesJson { get; set; } = "{}";
    public string QuestStartTimesJson { get; set; } = "{}";
    public DateTime LastSaved { get; set; }
}
