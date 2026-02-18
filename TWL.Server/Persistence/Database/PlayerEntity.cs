using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using TWL.Server.Persistence;

namespace TWL.Server.Persistence.Database;

[Table("Players")]
public class PlayerEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Core Stats
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

    public Element Element { get; set; }

    // Currency
    public int Gold { get; set; }
    public long PremiumCurrency { get; set; }
    public long DailyGiftAccumulator { get; set; }
    public DateTime LastGiftResetDate { get; set; }

    // Location
    public int MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }

    // Complex Objects stored as JSONB
    [Column(TypeName = "jsonb")]
    public PlayerSaveDataWrapper Data { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public QuestData Quests { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public Dictionary<string, DateTime> InstanceLockouts { get; set; } = new();

    public DateTime LastSaved { get; set; }
}

public class PlayerSaveDataWrapper
{
    public List<string> ProcessedOrders { get; set; } = new();
    public List<string> WorldFlags { get; set; } = new();

    public List<Item> Inventory { get; set; } = new();
    public List<Item> Equipment { get; set; } = new();
    public List<Item> Bank { get; set; } = new();
    public List<ServerPetData> Pets { get; set; } = new();
    public List<SkillMasteryData> Skills { get; set; } = new();

    public string ActivePetInstanceId { get; set; } = string.Empty;
}
