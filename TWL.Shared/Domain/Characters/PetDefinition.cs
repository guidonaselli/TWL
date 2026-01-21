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
}