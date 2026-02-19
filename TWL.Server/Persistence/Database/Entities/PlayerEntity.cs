namespace TWL.Server.Persistence.Database.Entities;

/// <summary>
/// EF Core entity for the 'players' table. Scalar fields map to individual columns;
/// complex nested data (inventory, equipment, pets, skills, quests) is stored as
/// serialized JSON strings in JSONB columns. Serialization/deserialization is handled
/// by the repository layer, not EF Core value converters, for explicit control.
/// </summary>
public class PlayerEntity
{
    // --- Primary Key ---
    public int PlayerId { get; set; }

    // --- Foreign Key to accounts ---
    public int UserId { get; set; }

    // --- Identity ---
    public string Name { get; set; } = string.Empty;

    // --- Core Stats ---
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

    // --- Element (stored as int, maps to TWL.Shared.Domain.Characters.Element enum) ---
    public int Element { get; set; }

    // --- Currency ---
    public int Gold { get; set; }
    public long PremiumCurrency { get; set; }
    public long DailyGiftAccumulator { get; set; }
    public DateTime LastGiftResetDate { get; set; }

    // --- Location ---
    public int MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }

    // --- Active Pet ---
    public string? ActivePetInstanceId { get; set; }

    // --- JSONB columns (serialized/deserialized by repository) ---
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

    // --- Metadata ---
    public DateTime LastSaved { get; set; }

    // --- Navigation ---
    public AccountEntity? Account { get; set; }
}
