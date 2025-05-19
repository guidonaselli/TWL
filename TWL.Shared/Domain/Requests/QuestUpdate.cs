namespace TWL.Shared.Domain.Requests;

public class QuestUpdate
{
    public QuestUpdate()
    {
        CurrentCounts = new List<int>();
    }

    public int QuestId { get; set; }
    public QuestState State { get; set; }
    public List<int> CurrentCounts { get; set; }
}