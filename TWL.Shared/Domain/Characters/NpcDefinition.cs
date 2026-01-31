namespace TWL.Shared.Domain.Characters;

public class NpcDefinition
{
    public int NpcId { get; set; }
    public string Name { get; set; }
    public string Title { get; set; } // e.g. "Harbor Quartermaster"

    // Assets
    public string SpritePath { get; set; }
    public string PortraitPath { get; set; }

    // Dialogue/Behavior
    public string DefaultDialogueKey { get; set; }
    public string Region { get; set; } // e.g. "Tropical Coast"

    // Optional: Static position for some global NPCs?
    // Usually handled by Map/Spawn logic.

    public NpcDefinition()
    {
    }
}
