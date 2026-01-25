using System.Collections.Generic;

namespace TWL.Shared.Domain.Characters;

public class PetSkillSet
{
    public int SkillId { get; set; }
    public int UnlockLevel { get; set; }
    public int UnlockAmity { get; set; }
    public bool RequiresRebirth { get; set; }
}
