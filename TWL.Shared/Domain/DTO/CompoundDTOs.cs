namespace TWL.Shared.Domain.DTO;

public enum CompoundOutcome
{
    Success,
    Fail,
    Break
}

public class CompoundRequestDTO
{
    public Guid TargetItemId { get; set; }
    public Guid IngredientItemId { get; set; }
    public Guid? CatalystItemId { get; set; }
}

public class CompoundResponseDTO
{
    public bool Success { get; set; }
    public CompoundOutcome Outcome { get; set; }
    public string Message { get; set; }
    public int NewEnhancementLevel { get; set; }
    public Dictionary<string, float> NewBonusStats { get; set; }
}
