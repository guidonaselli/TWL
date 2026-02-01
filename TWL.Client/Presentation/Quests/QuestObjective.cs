namespace TWL.Client.Presentation.Quests;

public class QuestObjective
{
    public int CurrentCount; // cuántos llevas
    public bool IsCompleted; // marca si está completo
    public string ObjectiveDescription; // texto descriptivo ("Mata 5 Slimes", etc.)
    public ObjectiveType ObjectiveType;
    public int RequiredCount; // cuántos debes matar / recolectar
    public string TargetName; // "Slime", "Wolf", "ItemID: 101", "NpcBob"

    public QuestObjective(ObjectiveType type, string targetName, int requiredCount, string description = "")
    {
        ObjectiveType = type;
        TargetName = targetName;
        RequiredCount = requiredCount;
        ObjectiveDescription = description;
        CurrentCount = 0;
        IsCompleted = false;
    }

    public void IncrementProgress(int amount = 1)
    {
        CurrentCount += amount;
        if (CurrentCount >= RequiredCount)
        {
            IsCompleted = true;
        }
    }
}