namespace TWL.Shared.Domain.Characters;

public class PetDefinition
{
    public int BaseHp;
    public int BaseStr, BaseCon, BaseInt, BaseWis, BaseAgi;

    public CaptureRules CaptureRules; // Structured capture rules
    public int? DeathQuestId;
    public int? DurationSeconds; // For temporary pets
    public Element Element;

    public PetGrowthModel GrowthModel;

    // New WLO-like properties
    public bool IsQuestUnique;
    public bool IsTemporary;
    public string? RequiredFlag;

    public bool IsUnique;
    public string Name;
    public int PetTypeId;
    public string PortraitPath;
    public PetRarity Rarity;
    public bool RebirthEligible;
    public int RebirthSkillId; // skill si renace
    public string RebirthSprite;
    public bool RecoveryServiceEnabled;

    // Legacy/Simple support
    public List<int> SkillIds; // skill fijos

    public List<PetSkillSet> SkillSet;

    // Asset paths (new)
    public string SpritePath;
    public PetType Type;
    public List<PetUtility> Utilities;

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
    public string? RequiredFlag { get; set; }
}