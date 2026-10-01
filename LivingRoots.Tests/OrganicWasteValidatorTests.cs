using LivingRoots.Domain.Services;
using LivingRoots.Tests.Fixtures;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Tests for <see cref="OrganicWasteValidator"/> category validation (FR-4.1, FR-4.2, FR-4.3).
/// </summary>
public class OrganicWasteValidatorTests
{
    /// <summary>Vanilla item category: Seeds (wild seeds, tree seeds).</summary>
    private const int SeedsCategory = -74;

    /// <summary>Vanilla item category: Vegetables (crop produce).</summary>
    private const int VegetablesCategory = -75;

    /// <summary>Vanilla item category: Fruit (fruit produce).</summary>
    private const int FruitCategory = -79;

    /// <summary>Vanilla item category: Flowers.</summary>
    private const int FlowerCategory = -80;

    /// <summary>Vanilla item category: Greens / forage.</summary>
    private const int ForageCategory = -81;

    /// <summary>Vanilla item category: Stone (not organic waste).</summary>
    private const int StoneCategory = -12;

    /// <summary>Vanilla item category: Wood (not organic waste).</summary>
    private const int WoodCategory = -14;

    private readonly Mock<IMonitor> _mockMonitor;
    private readonly OrganicWasteValidator _validator;

    /// <summary>
    /// Creates the validator and installs the minimum game state that
    /// <c>Item.HasContextTag</c> needs to answer without a booted game.
    /// </summary>
    public OrganicWasteValidatorTests()
    {
        GameStateFixture.Install();
        _mockMonitor = new Mock<IMonitor>();
        _validator = new OrganicWasteValidator(_mockMonitor.Object);
    }

    private static Item CreateItem(int category)
    {
        return ItemFactory.CreateItem("Object.Test", category);
    }

    /// <summary>Seeds are valid organic waste (FR-4.1).</summary>
    [Fact]
    public void IsValidOrganicWaste_Seeds_ReturnsTrue()
    {
        var item = CreateItem(SeedsCategory);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Vegetable produce is valid organic waste (FR-4.1).</summary>
    [Fact]
    public void IsValidOrganicWaste_Vegetables_ReturnsTrue()
    {
        var item = CreateItem(VegetablesCategory);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Fruit produce is valid organic waste (FR-4.1).</summary>
    [Fact]
    public void IsValidOrganicWaste_Fruits_ReturnsTrue()
    {
        var item = CreateItem(FruitCategory);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Flowers are valid organic waste (FR-4.1).</summary>
    [Fact]
    public void IsValidOrganicWaste_Flowers_ReturnsTrue()
    {
        var item = CreateItem(FlowerCategory);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Forage / greens are valid organic waste (FR-4.1).</summary>
    [Fact]
    public void IsValidOrganicWaste_Forage_ReturnsTrue()
    {
        var item = CreateItem(ForageCategory);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Stone is not organic waste (FR-4.3).</summary>
    [Fact]
    public void IsValidOrganicWaste_Stone_ReturnsFalse()
    {
        var item = CreateItem(StoneCategory);
        Assert.False(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>Wood is not organic waste (FR-4.3).</summary>
    [Fact]
    public void IsValidOrganicWaste_Wood_ReturnsFalse()
    {
        var item = CreateItem(WoodCategory);
        Assert.False(_validator.IsValidOrganicWaste(item));
    }

    /// <summary>A null item is rejected (FR-4.3).</summary>
    [Fact]
    public void IsValidOrganicWaste_NullItem_ReturnsFalse()
    {
        Assert.False(_validator.IsValidOrganicWaste(null!));
    }

    // NOTE: Context tag tests (FR-4.1a, FR-4.2a) are not included because the
    // Stardew Valley API does not provide a public method to set context tags
    // on items. The Item.ContextTags field is internal, and the SetContextTag
    // API is not available in this version. Testing these paths would require
    // reflection, which violates NFR-4. The category-based tests above cover
    // the core validation logic (FR-4.1, FR-4.2, FR-4.3).
}
