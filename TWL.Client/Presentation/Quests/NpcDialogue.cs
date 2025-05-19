// NpcDialogue.cs
using System.Collections.Generic;

namespace TWL.Client.Presentation.Quests;

public sealed class NpcDialogue
{
    // lista inmutable de líneas por NPC
    private readonly Dictionary<int, IReadOnlyList<DialogueLine>> _dialogues = new();

    public NpcDialogue() => LoadAllDialogues();

    private void LoadAllDialogues()
    {
        _dialogues[1] = new List<DialogueLine>
        {
            new(
                SpeakerName : "Bob the NPC",
                Text        : "¡Hola! ¿Quieres ayudarme con una misión?",
                Options     : new List<string> { "Sí, cuéntame más.", "No, estoy ocupado." },
                NextLineId  : -1)
        };
    }

    public IReadOnlyList<DialogueLine> GetDialoguesForNpc(int npcId) =>
        _dialogues.TryGetValue(npcId, out var list) ? list : new List<DialogueLine>();
}