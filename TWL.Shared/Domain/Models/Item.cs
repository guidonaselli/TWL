using System.Text.Json.Serialization;

namespace TWL.Shared.Domain.Models;

public class Item
{
    [JsonPropertyName("id")] public int ItemId { get; set; }
    [JsonPropertyName("uid")] public Guid InstanceId { get; set; } = Guid.NewGuid();
    [JsonPropertyName("n")] public string Name { get; set; } = "";
    [JsonPropertyName("t")] public ItemType Type { get; set; }
    [JsonPropertyName("m")] public int MaxStack { get; set; } = 99;
    [JsonPropertyName("q")] public int Quantity { get; set; }
    [JsonPropertyName("f")] public float ForgeSuccessRateBonus { get; set; }
    [JsonPropertyName("el")] public int EnhancementLevel { get; set; }
    [JsonPropertyName("es")] public Dictionary<string, float>? EnhancementStats { get; set; }

    [JsonPropertyName("bp")] public BindPolicy Policy { get; set; } = BindPolicy.Unbound;
    [JsonPropertyName("bid")] public int? BoundToId { get; set; }
}