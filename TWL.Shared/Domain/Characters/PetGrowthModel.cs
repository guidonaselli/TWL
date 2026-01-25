namespace TWL.Shared.Domain.Characters;

public class PetGrowthModel
{
    public GrowthCurveType CurveType { get; set; } = GrowthCurveType.Standard;
    public float HpGrowthPerLevel { get; set; } = 10f;
    public float SpGrowthPerLevel { get; set; } = 5f;

    // Weights for stat distribution (total usually 100 or relative)
    public int StrWeight { get; set; } = 20;
    public int ConWeight { get; set; } = 20;
    public int IntWeight { get; set; } = 20;
    public int WisWeight { get; set; } = 20;
    public int AgiWeight { get; set; } = 20;
}
