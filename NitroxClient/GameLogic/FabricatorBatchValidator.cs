using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NitroxClient.GameLogic;

internal enum FabricatorBatchFailure
{
    None,
    MissingIngredients,
    InventoryFull
}

internal readonly record struct FabricatorBatchValidation(int Maximum, FabricatorBatchFailure Failure)
{
    public bool CanCraft(int quantity) => quantity > 0 && quantity <= Maximum;
}

internal readonly record struct FabricatorBatchItem(TechType TechType, Vector2int Size);

internal readonly record struct FabricatorBatchIngredient(TechType TechType, int Amount);

internal sealed record FabricatorBatchSnapshot(
    int Width,
    int Height,
    IReadOnlyList<FabricatorBatchItem> InventoryItems,
    IReadOnlyList<FabricatorBatchIngredient> Ingredients,
    IReadOnlyList<Vector2int> OutputsPerRecipe,
    bool RequiresIngredients);

internal static class FabricatorBatchValidator
{
    public static FabricatorBatchValidation Validate(TechType recipeType)
    {
        if (!Inventory.main || Inventory.main.container == null)
        {
            return new FabricatorBatchValidation(0, FabricatorBatchFailure.MissingIngredients);
        }

        return Validate(CreateSnapshot(recipeType));
    }

    internal static FabricatorBatchValidation Validate(FabricatorBatchSnapshot snapshot)
    {
        FabricatorBatchFailure firstFailure = ValidateQuantity(snapshot, 1);
        if (firstFailure != FabricatorBatchFailure.None)
        {
            return new FabricatorBatchValidation(0, firstFailure);
        }

        int upperBound = GetUpperBound(snapshot);
        int maximum = 1;
        for (int quantity = 2; quantity <= upperBound; quantity++)
        {
            if (ValidateQuantity(snapshot, quantity) != FabricatorBatchFailure.None)
            {
                break;
            }
            maximum = quantity;
        }

        return new FabricatorBatchValidation(maximum, FabricatorBatchFailure.None);
    }

    internal static FabricatorBatchFailure ValidateQuantity(FabricatorBatchSnapshot snapshot, int quantity)
    {
        if (quantity < 1)
        {
            return FabricatorBatchFailure.MissingIngredients;
        }

        if (snapshot.RequiresIngredients && !HasIngredients(snapshot, quantity))
        {
            return FabricatorBatchFailure.MissingIngredients;
        }

        // A batch must fit after every recipe instance. Checking only the final layout can be
        // incorrect when differently-sized ingredients and outputs make packing non-monotonic.
        for (int completedRecipes = 1; completedRecipes <= quantity; completedRecipes++)
        {
            List<Vector2int> finalSizes = BuildInventoryLayout(snapshot, completedRecipes);
            ItemsContainer capacityProbe = new(snapshot.Width, snapshot.Height, null, string.Empty, null);
            if (!capacityProbe.HasRoomFor(finalSizes))
            {
                return FabricatorBatchFailure.InventoryFull;
            }
        }

        return FabricatorBatchFailure.None;
    }

    private static FabricatorBatchSnapshot CreateSnapshot(TechType recipeType)
    {
        ItemsContainer container = Inventory.main.container;
        List<FabricatorBatchItem> inventoryItems = new(container.count);
        foreach (InventoryItem inventoryItem in container)
        {
            inventoryItems.Add(new FabricatorBatchItem(
                inventoryItem.techType,
                new Vector2int(Math.Max(1, inventoryItem.width), Math.Max(1, inventoryItem.height))));
        }

        ReadOnlyCollection<Ingredient> recipeIngredients = TechData.GetIngredients(recipeType);
        List<FabricatorBatchIngredient> ingredients = new(recipeIngredients?.Count ?? 0);
        if (recipeIngredients != null)
        {
            foreach (Ingredient ingredient in recipeIngredients)
            {
                if (ingredient.amount > 0)
                {
                    ingredients.Add(new FabricatorBatchIngredient(ingredient.techType, ingredient.amount));
                }
            }
        }

        List<Vector2int> outputs = [];
        Vector2int primarySize = NormalizeSize(TechData.GetItemSize(recipeType));
        int craftAmount = Math.Max(1, TechData.GetCraftAmount(recipeType));
        for (int index = 0; index < craftAmount; index++)
        {
            outputs.Add(primarySize);
        }

        ReadOnlyCollection<TechType> linkedItems = TechData.GetLinkedItems(recipeType);
        if (linkedItems != null)
        {
            foreach (TechType linkedItem in linkedItems)
            {
                outputs.Add(NormalizeSize(TechData.GetItemSize(linkedItem)));
            }
        }

        return new FabricatorBatchSnapshot(
            container.sizeX,
            container.sizeY,
            inventoryItems,
            ingredients,
            outputs,
            GameModeUtils.RequiresIngredients());
    }

    private static int GetUpperBound(FabricatorBatchSnapshot snapshot)
    {
        int cellCount = Math.Max(1, snapshot.Width * snapshot.Height);
        if (!snapshot.RequiresIngredients || snapshot.Ingredients.Count == 0)
        {
            return cellCount;
        }

        Dictionary<TechType, int> inventoryCounts = GetInventoryCounts(snapshot.InventoryItems);
        int ingredientLimit = int.MaxValue;
        foreach (FabricatorBatchIngredient ingredient in snapshot.Ingredients)
        {
            inventoryCounts.TryGetValue(ingredient.TechType, out int available);
            ingredientLimit = Math.Min(ingredientLimit, available / ingredient.Amount);
        }

        return Math.Min(cellCount, Math.Max(0, ingredientLimit));
    }

    private static bool HasIngredients(FabricatorBatchSnapshot snapshot, int quantity)
    {
        Dictionary<TechType, int> inventoryCounts = GetInventoryCounts(snapshot.InventoryItems);
        foreach (FabricatorBatchIngredient ingredient in snapshot.Ingredients)
        {
            inventoryCounts.TryGetValue(ingredient.TechType, out int available);
            if (available < ingredient.Amount * quantity)
            {
                return false;
            }
        }
        return true;
    }

    private static Dictionary<TechType, int> GetInventoryCounts(IReadOnlyList<FabricatorBatchItem> items)
    {
        Dictionary<TechType, int> counts = [];
        foreach (FabricatorBatchItem item in items)
        {
            counts.TryGetValue(item.TechType, out int count);
            counts[item.TechType] = count + 1;
        }
        return counts;
    }

    private static List<Vector2int> BuildInventoryLayout(FabricatorBatchSnapshot snapshot, int completedRecipes)
    {
        Dictionary<TechType, int> removals = [];
        if (snapshot.RequiresIngredients)
        {
            foreach (FabricatorBatchIngredient ingredient in snapshot.Ingredients)
            {
                removals.TryGetValue(ingredient.TechType, out int current);
                removals[ingredient.TechType] = current + ingredient.Amount * completedRecipes;
            }
        }

        List<Vector2int> sizes = new(snapshot.InventoryItems.Count + snapshot.OutputsPerRecipe.Count * completedRecipes);
        foreach (FabricatorBatchItem item in snapshot.InventoryItems)
        {
            if (removals.TryGetValue(item.TechType, out int remaining) && remaining > 0)
            {
                removals[item.TechType] = remaining - 1;
                continue;
            }
            sizes.Add(NormalizeSize(item.Size));
        }

        for (int recipe = 0; recipe < completedRecipes; recipe++)
        {
            foreach (Vector2int output in snapshot.OutputsPerRecipe)
            {
                sizes.Add(NormalizeSize(output));
            }
        }
        return sizes;
    }

    private static Vector2int NormalizeSize(Vector2int size) => new(Math.Max(1, size.x), Math.Max(1, size.y));
}
