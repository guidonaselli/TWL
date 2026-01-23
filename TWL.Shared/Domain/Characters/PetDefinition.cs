namespace TWL.Shared.Domain.Characters;

public class PetDefinition
{
    public int BaseHp;
    public int BaseStr, BaseCon, BaseInt, BaseWis, BaseAgi;
    public Element Element;
    public bool IsUnique;
    public string Name;
    public int PetTypeId;
    public int RebirthSkillId; // skill si renace
    public string RebirthSprite;
    public List<int> SkillIds; // skill fijos

    // New WLO-like properties
    public bool IsCapturable;
    public bool IsQuestUnique;
    public bool RecoveryServiceEnabled;
    public bool RebirthEligible;
    public int? DeathQuestId;
    public int CaptureLevelLimit;
    public float CaptureChance;
}