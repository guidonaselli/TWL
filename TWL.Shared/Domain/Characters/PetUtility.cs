namespace TWL.Shared.Domain.Characters;

public class PetUtility
{
    public PetUtilityType Type { get; set; }
    public float Value { get; set; } // e.g., Speed multiplier for Mount, slot count for Delivery
    public int RequiredLevel { get; set; }
    public int RequiredAmity { get; set; }
}
