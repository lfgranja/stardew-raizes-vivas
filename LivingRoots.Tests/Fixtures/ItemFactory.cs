using StardewValley;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Static factory for creating test items with specific properties.
/// Used by OrganicWasteValidatorTests and other tests requiring Item instances.
/// </summary>
public static class ItemFactory
{
    /// <summary>Creates an item with the specified QualifiedItemId and Category.</summary>
    public static Item CreateItem(string qualifiedItemId, int category = 0)
    {
        var item = new StardewValley.Object(qualifiedItemId, 1, false);
        item.Category = category;
        return item;
    }

    /// <summary>Creates a valid waste item (Seeds category: -74).</summary>
    public static Item CreateValidWasteItem()
    {
        return CreateItem("Object.Sap", -74);
    }

    /// <summary>Creates an invalid waste item (invalid category).</summary>
    public static Item CreateInvalidWasteItem()
    {
        return CreateItem("Object.Stone", -999);
    }
}
