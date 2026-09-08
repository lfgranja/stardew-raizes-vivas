using Xunit;
using Moq;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Tests;

public class CompostApplicationServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly PlayerProviderStub _playerStub;
    private readonly CompostApplicationService _service;

    public CompostApplicationServiceTests()
    {
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _playerStub = new PlayerProviderStub();

        _service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object);
    }

    [Fact]
    public void TryApplyCompost_ValidTile_IncreasesHealth()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = null;
        location.terrainFeatures.Add(tile, hoeDirt);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _playerStub.CurrentItem = compostItem;

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        var result = _service.TryApplyCompost(location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }

    [Fact]
    public void TryApplyCompost_AtMaxHealth_ReturnsFalse()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = null;
        location.terrainFeatures.Add(tile, hoeDirt);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _playerStub.CurrentItem = compostItem;

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

        var result = _service.TryApplyCompost(location, tile);

        Assert.False(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void TryApplyCompost_InvalidLocation_ReturnsFalse()
    {
        var location = new GameLocation("Maps\\Town", "Town");
        var tile = new Vector2(10, 10);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _playerStub.CurrentItem = compostItem;

        var result = _service.TryApplyCompost(location, tile);

        Assert.False(result);
    }

    [Fact]
    public void TryApplyCompost_NoCompostHeld_ReturnsFalse()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = null;
        location.terrainFeatures.Add(tile, hoeDirt);

        _playerStub.CurrentItem = null;

        var result = _service.TryApplyCompost(location, tile);

        Assert.False(result);
    }

    [Fact]
    public void TryApplyCompost_NonHoeDirtTile_ReturnsFalse()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _playerStub.CurrentItem = compostItem;

        var result = _service.TryApplyCompost(location, tile);

        Assert.False(result);
    }

    [Fact]
    public void TryApplyCompost_HealthCappedAtMax()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = null;
        location.terrainFeatures.Add(tile, hoeDirt);

        var compostItem = ItemFactory.CreateItem(ModConstants.CompostItemId);
        _playerStub.CurrentItem = compostItem;

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(90f);

        var result = _service.TryApplyCompost(location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }
}
