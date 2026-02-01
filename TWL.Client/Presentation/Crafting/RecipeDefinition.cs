namespace TWL.Client.Presentation.Crafting;

public sealed record RecipeDefinition
{
    public required int RecipeId { get; init; }
    public required string Name { get; init; }

    public required IReadOnlyDictionary<int, int> RequiredItems { get; init; } =
        new Dictionary<int, int>();

    public required int ResultItemId { get; init; }
    public required int ResultQuantity { get; init; }
}