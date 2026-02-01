// Proyecto Shared

namespace TWL.Shared.Domain.DTO;

public class PlayerColorsDto
{
    // Default colors matching the character reference (orange hair, peach skin, blue eyes, gray clothes)
    public string SkinColor { get; set; } = "#FDBCB4";     // Peach/tan skin tone
    public string HairColor { get; set; } = "#FF9933";     // Orange hair
    public string EyeColor { get; set; } = "#4A90E2";      // Blue eyes
    public string ClothColor { get; set; } = "#6B7280";    // Dark gray clothing
}