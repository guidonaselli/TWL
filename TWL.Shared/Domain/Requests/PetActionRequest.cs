namespace TWL.Shared.Domain.Requests;

public enum PetActionType
{
    Switch,
    Dismiss,
    Utility
}

public class PetActionRequest
{
    public PetActionType Action { get; set; }
    public string PetInstanceId { get; set; }
    public string AdditionalData { get; set; } // For utility params if needed
}
