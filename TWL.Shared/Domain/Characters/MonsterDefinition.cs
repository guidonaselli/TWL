using System.Collections.Generic;

namespace TWL.Shared.Domain.Characters;

public class MonsterDefinition
{
    public int MonsterId { get; set; }
    public string Name { get; set; }
    public Element Element { get; set; }
    public int Level { get; set; }

    // Base Stats
    public int BaseHp { get; set; }
    public int BaseSp { get; set; }
    public int BaseStr { get; set; }
    public int BaseCon { get; set; }
    public int BaseInt { get; set; }
    public int BaseWis { get; set; }
    public int BaseAgi { get; set; }

    // Assets
    public string SpritePath { get; set; }
    public string PortraitPath { get; set; }

    // Combat
    public List<int> SkillIds { get; set; } = new();
    public bool IsAggressive { get; set; }
    public int ExpReward { get; set; }

    // Drops
    public List<DropItem> Drops { get; set; } = new();

    public MonsterDefinition()
    {
    }
}

public class DropItem
{
    public int ItemId { get; set; }
    public double Chance { get; set; } // 0.0 to 1.0
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
}
