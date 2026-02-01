namespace TWL.Client.Presentation.Crafting;

public sealed class CraftRecipe
{
    public int RecipeId { get; init; }
    public required string RecipeName { get; init; }

    public required IReadOnlyDictionary<int, int> RequiredItems { get; init; } =
        new Dictionary<int, int>();

    public int ResultItemId { get; init; }
    public int ResultQuantity { get; init; }
}