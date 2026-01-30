using System.Collections.Generic;

namespace TWL.Shared.Domain.Characters;

public class PetDefinition
{
    public int PetTypeId;
    public string Name;
    public PetType Type;
    public PetRarity Rarity;
    public Element Element;

    public int BaseHp;
    public int BaseStr, BaseCon, BaseInt, BaseWis, BaseAgi;

    public PetGrowthModel GrowthModel;

    public List<PetSkillSet> SkillSet;
    public List<PetUtility> Utilities;

    public bool IsUnique;
    public int RebirthSkillId; // skill si renace
    public string RebirthSprite;

    // Legacy/Simple support
    public List<int> SkillIds; // skill fijos

    // New WLO-like properties
    public bool IsQuestUnique;
    public bool IsTemporary;
    public int? DurationSeconds; // For temporary pets
    public bool RecoveryServiceEnabled;
    public bool RebirthEligible;
    public int? DeathQuestId;

    public CaptureRules CaptureRules; // Structured capture rules

    // Legacy fields removed/deprecated in favor of CaptureRules

    public PetDefinition()
    {
        GrowthModel = new PetGrowthModel();
        SkillSet = new List<PetSkillSet>();
        Utilities = new List<PetUtility>();
        SkillIds = new List<int>();
        CaptureRules = new CaptureRules();
    }
}

public class CaptureRules
{
    public bool IsCapturable { get; set; }
    public int LevelLimit { get; set; }
    public float BaseChance { get; set; }
    public int? RequiredItemId { get; set; }
}
