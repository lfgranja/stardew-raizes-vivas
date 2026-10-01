using StardewValley;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Static factory for creating test items with specific properties.
/// Used by OrganicWasteValidatorTests and other tests requiring Item instances.
/// </summary>
/// <remarks>
/// The named <c>Object(qualifiedItemId, ...)</c> constructor resolves its type definition
/// through <c>ItemRegistry</c>, which is only populated when the game has booted. Unit tests
/// therefore use the parameterless constructor and assign <see cref="StardewValley.Object.ItemId"/>
/// directly, which makes <see cref="Item.QualifiedItemId"/> report the requested id without
/// needing a registered type definition.
/// </remarks>
public static class ItemFactory
{
    /// <summary>Creates an item with the specified qualified id and category.</summary>
    /// <param name="qualifiedItemId">Value <see cref="Item.QualifiedItemId"/> should report.</param>
    /// <param name="category">Item category, e.g. -74 for seeds.</param>
    /// <returns>A test item carrying the requested id and category.</returns>
    public static Item CreateItem(string qualifiedItemId, int category = 0)
    {
        return new StardewValley.Object
        {
            ItemId = qualifiedItemId,
            Category = category
        };
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

    /// <summary>Returns null for tests covering the null-item validation path.</summary>
    public static Item? CreateNullItem()
    {
        return null;
    }
}
