using Xunit;
using Moq;
using LivingRoots.Domain.Services;
using StardewModdingAPI;
using StardewValley;


namespace LivingRoots.Tests;

public class OrganicWasteValidatorTests
{
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly OrganicWasteValidator _validator;

    public OrganicWasteValidatorTests()
    {
        _mockMonitor = new Mock<IMonitor>();
        _validator = new OrganicWasteValidator(_mockMonitor.Object);
    }

    private Item CreateItem(int category)
    {
        return ItemFactory.CreateItem("Object.Test", category);
    }

    [Fact]
    public void IsValidOrganicWaste_Seeds_ReturnsTrue()
    {
        var item = CreateItem(-74);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Vegetables_ReturnsTrue()
    {
        var item = CreateItem(-75);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Fruits_ReturnsTrue()
    {
        var item = CreateItem(-79);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Flowers_ReturnsTrue()
    {
        var item = CreateItem(-80);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Forage_ReturnsTrue()
    {
        var item = CreateItem(-81);
        Assert.True(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Stone_ReturnsFalse()
    {
        var item = CreateItem(-12);
        Assert.False(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_Wood_ReturnsFalse()
    {
        var item = CreateItem(-14);
        Assert.False(_validator.IsValidOrganicWaste(item));
    }

    [Fact]
    public void IsValidOrganicWaste_NullItem_ReturnsFalse()
    {
        Assert.False(_validator.IsValidOrganicWaste(null!));
    }


}
