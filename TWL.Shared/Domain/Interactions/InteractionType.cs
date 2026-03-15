using System.Text.Json.Serialization;

namespace TWL.Shared.Domain.Interactions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InteractionType
    {
        Talk,
        Shop,
        Quest,
        Compound,
        Gather,
        Craft,
        Collect,
        Interact
    }
}
