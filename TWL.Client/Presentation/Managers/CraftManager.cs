using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TWL.Client.Presentation.Crafting;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Managers;

public class CraftManager
{
    private readonly Dictionary<int, CraftRecipe> _recipes;

    public CraftManager()
    {
        _recipes = new Dictionary<int, CraftRecipe>();
    }

    public int RecipeCount => _recipes.Count;

    public void LoadRecipesFromJson(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Recipes file not found at path: {path}");

        var jsonContent = File.ReadAllText(path);
        var recipeDefinitions = JsonConvert.DeserializeObject<List<RecipeDefinition>>(jsonContent);

        _recipes.Clear();
        foreach (var definition in recipeDefinitions)
        {
            var recipe = new CraftRecipe
            {
                RecipeId = definition.RecipeId,
                RecipeName = definition.Name,
                RequiredItems = definition.RequiredItems,
                ResultItemId = definition.ResultItemId,
                ResultQuantity = definition.ResultQuantity
            };

            _recipes[recipe.RecipeId] = recipe;
        }
    }

    public bool CanCraft(int recipeId, Inventory inv)
    {
        if (!_recipes.TryGetValue(recipeId, out var recipe)) return false;

        foreach (var requirement in recipe.RequiredItems)
        {
            var itemId = requirement.Key;
            var requiredQuantity = requirement.Value;

            if (!inv.HasItem(itemId, requiredQuantity)) return false;
        }

        return true;
    }

    public bool CraftItem(int recipeId, Inventory inv)
    {
        if (!CanCraft(recipeId, inv)) return false;

        var recipe = _recipes[recipeId];

        // Remove required items
        foreach (var requirement in recipe.RequiredItems)
        {
            var itemId = requirement.Key;
            var quantity = requirement.Value;
            inv.RemoveItem(itemId, quantity);
        }

        // Add crafted item
        inv.AddItem(recipe.ResultItemId, recipe.ResultQuantity);

        return true;
    }

    public IEnumerable<CraftRecipe> GetAllRecipes()
    {
        return _recipes.Values;
    }

    public CraftRecipe GetRecipe(int recipeId)
    {
        return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
    }
}