using Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Services;

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
        var bin = _factory.CreateBin(10, 20);
        Assert.Equal(CompostingBinState.Empty, bin.State);
    }

    [Fact]
    public void CreateBin_HasCorrectDefaults()
    {
        var bin = _factory.CreateBin(10, 20);
        Assert.Equal(1, bin.MaturationLevel);
        Assert.Equal(10, bin.TileX);
        Assert.Equal(20, bin.TileY);
    }

    [Fact]
    public void CreateBin_DifferentCoordinates()
    {
        var bin1 = _factory.CreateBin(5, 15);
        var bin2 = _factory.CreateBin(30, 40);

        Assert.Equal(5, bin1.TileX);
        Assert.Equal(15, bin1.TileY);
        Assert.Equal(30, bin2.TileX);
        Assert.Equal(40, bin2.TileY);
    }
}
