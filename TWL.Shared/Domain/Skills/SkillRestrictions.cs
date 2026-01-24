namespace TWL.Shared.Domain.Skills;

public class SkillRestrictions
{
    public bool UniquePerCharacter { get; set; }
    public bool BindOnAcquire { get; set; }
    public bool NotTradeable { get; set; }
    public bool NotDropable { get; set; }
    public RebirthClass? RebirthClassRequirement { get; set; }
}
