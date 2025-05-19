namespace TWL.Client.Presentation.Skills;

public class SkillDefinition
{
    public int SkillId { get; set; }
    public string Name { get; set; }
    public string Element { get; set; }
    public string Type { get; set; }
    public SkillRequirements Requirements { get; set; }
    public int SpCost { get; set; }
    public double Power { get; set; }
    public double? SealChance { get; set; }
    public double? UnsealChance { get; set; }
    public int Level { get; set; }
    public int MaxLevel { get; set; }
    public string Description { get; set; }
}