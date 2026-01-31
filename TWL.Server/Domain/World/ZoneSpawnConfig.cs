using System.Collections.Generic;

namespace TWL.Server.Domain.World;

public class ZoneSpawnConfig
{
    public int MapId { get; set; }
    public List<SpawnRegion> SpawnRegions { get; set; } = new();

    // Global settings for the map
    public bool RandomEncounterEnabled { get; set; }
    public float StepChance { get; set; } = 0.05f; // 5% chance per step (simulated)
    public int MinMobCount { get; set; } = 5;
    public int MaxMobCount { get; set; } = 15;
    public int RespawnSeconds { get; set; } = 60;
}

public class SpawnRegion
{
    // Simple rect for now
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public List<int> AllowedFamilyIds { get; set; } = new();
    public List<int> AllowedMonsterIds { get; set; } = new();
}
