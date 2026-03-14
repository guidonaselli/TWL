using System.Text.Json.Serialization;

namespace TWL.Shared.Domain.Models;

public class RebirthRequirements
{
    [JsonPropertyName("min_level")]
    public int MinLevel { get; set; } = 100;

    [JsonPropertyName("required_quest_id")]
    public int? RequiredQuestId { get; set; }

    [JsonPropertyName("required_item_id")]
    public int? RequiredItemId { get; set; }

    [JsonPropertyName("required_item_quantity")]
    public int RequiredItemQuantity { get; set; } = 1;
}
