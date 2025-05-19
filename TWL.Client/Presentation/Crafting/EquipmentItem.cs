namespace TWL.Client.Presentation.Crafting;

/// <summary>
///     Example of an EquipmentItem class that can be forged.
///     In a real scenario, you'd integrate this with your item system.
/// </summary>
public class EquipmentItem
{
    public int AttackBonus;
    public int DefenseBonus;
    public int EnhanceLevel;
    public int ItemId;
    public string Name;

    public EquipmentItem(int itemId, string name)
    {
        ItemId = itemId;
        Name = name;
        EnhanceLevel = 0;
        AttackBonus = 0;
        DefenseBonus = 0;
    }
}