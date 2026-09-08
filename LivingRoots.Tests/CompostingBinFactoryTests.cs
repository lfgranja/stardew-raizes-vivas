using LivingRoots.Domain;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests;

public class CompostingBinFactoryTests
{
    private readonly CompostingBinFactory _factory;

    public CompostingBinFactoryTests()
    {
        _factory = new CompostingBinFactory();
    }

    [Fact]
    public void CreateBin_ReturnsEmptyState()
    {
        var bin = _factory.CreateBin(3, 5);

        Assert.Equal(CompostingBinState.Empty, bin.State);
    }

    [Fact]
    public void CreateBin_HasCorrectDefaults()
    {
        var bin = _factory.CreateBin(7, 12);

        Assert.Equal(1, bin.MaturationLevel);
        Assert.Equal(7, bin.TileX);
        Assert.Equal(12, bin.TileY);
    }

    [Fact]
    public void CreateBin_DifferentCoordinates()
    {
        var bin = _factory.CreateBin(20, 30);

        Assert.Equal(20, bin.TileX);
        Assert.Equal(30, bin.TileY);
    }
}
