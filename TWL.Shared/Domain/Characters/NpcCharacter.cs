using Microsoft.Xna.Framework;

namespace TWL.Shared.Domain.Characters;

public class NpcCharacter : Character
{
    public NpcCharacter(string name, Element element, int npcId, string dialogue)
        : base(name, element)
    {
        NpcId = npcId;
        DialogueLine = dialogue;
    }

    public int NpcId { get; set; }
    public string DialogueLine { get; set; }

    public new void Update(GameTime gameTime)
    {
        // Podrías poner comportamiento de NPC: moverse un poco, etc.
        base.Update(gameTime);
    }
}