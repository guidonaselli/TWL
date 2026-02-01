namespace TWL.Shared.Domain.Events;

public class PetAcquiredEvent
{
    public int OwnerId { get; set; }
    public string PetInstanceId { get; set; }
    public int PetTypeId { get; set; }
    public string Name { get; set; }
}

public class PetDiedEvent
{
    public int OwnerId { get; set; }
    public string PetInstanceId { get; set; }
    public int AmityLoss { get; set; }
}

public class PetLeveledEvent
{
    public int OwnerId { get; set; }
    public string PetInstanceId { get; set; }
    public int NewLevel { get; set; }
}

public class PetRebirthedEvent
{
    public int OwnerId { get; set; }
    public string PetInstanceId { get; set; }
    public int NewGeneration { get; set; }
}

public class PetReleasedEvent
{
    public int OwnerId { get; set; }
    public string PetInstanceId { get; set; }
}