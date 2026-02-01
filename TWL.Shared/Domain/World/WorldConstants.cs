namespace TWL.Shared.Domain.World;

public static class WorldConstants
{
    public static readonly IReadOnlyList<string> RequiredLayers = new[]
    {
        "Ground",
        "Ground_Detail",
        "Water",
        "Cliffs",
        "Rocks",
        "Props_Low",
        "Props_High",
        "Collisions",
        "Spawns",
        "Triggers"
    };

    public static readonly IReadOnlyList<string> ObjectGroupLayers = new[]
    {
        "Collisions",
        "Spawns",
        "Triggers"
    };

    public static readonly IReadOnlySet<string> ValidCollisionTypes = new HashSet<string>
    {
        "Solid", "WaterBlock", "CliffBlock", "OneWay"
    };

    public static readonly IReadOnlySet<string> ValidSpawnTypes = new HashSet<string>
    {
        "PlayerStart", "Monster", "NPC", "ResourceNode"
    };

    public static readonly IReadOnlySet<string> ValidTriggerTypes = new HashSet<string>
    {
        "MapTransition", "QuestHook", "InstanceGate", "CutsceneHook", "Interaction"
    };

    public static class TriggerTypes
    {
        public const string MapTransition = "MapTransition";
        public const string QuestHook = "QuestHook";
        public const string InstanceGate = "InstanceGate";
        public const string CutsceneHook = "CutsceneHook";
        public const string Interaction = "Interaction";
    }
}