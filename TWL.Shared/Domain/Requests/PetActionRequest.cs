namespace TWL.Shared.Domain.Requests;

public enum PetActionType
{
    Switch,
    Dismiss,
    Utility,
    Rebirth,
    Evolve
}

public class PetActionRequest
{
    public PetActionType Action { get; set; }
    public string PetInstanceId { get; set; }
    public string AdditionalData { get; set; } // For utility params if needed
}