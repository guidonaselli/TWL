// NpcDialogue.cs

namespace TWL.Client.Presentation.Quests;

public sealed class NpcDialogue
{
    // lista inmutable de líneas por NPC
    private readonly Dictionary<int, IReadOnlyList<DialogueLine>> _dialogues = new();

    public NpcDialogue()
    {
        LoadAllDialogues();
    }

    private void LoadAllDialogues()
    {
        _dialogues[1] = new List<DialogueLine>
        {
            new(
                "Bob the NPC",
                "¡Hola! ¿Quieres ayudarme con una misión?",
                new List<string> { "Sí, cuéntame más.", "No, estoy ocupado." },
                -1)
        };
    }

    public IReadOnlyList<DialogueLine> GetDialoguesForNpc(int npcId) =>
        _dialogues.TryGetValue(npcId, out var list) ? list : new List<DialogueLine>();
}