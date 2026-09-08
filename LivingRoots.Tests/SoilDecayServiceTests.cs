using Xunit;
using Moq;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Tests;

public class SoilDecayServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly SeasonProviderStub _seasonStub;
    private readonly SeasonalDecayMultiplier _seasonalMultiplier;
    private readonly SoilDecayService _service;

    public SoilDecayServiceTests()
    {
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _seasonStub = new SeasonProviderStub();
        _seasonalMultiplier = new SeasonalDecayMultiplier();

        _service = new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalMultiplier,
            _seasonStub);
    }

    private GameLocation CreateLocationWithBareTile(string locationName, int x, int y)
    {
        var location = new GameLocation("Maps\\Farm", locationName);
        var tile = new Vector2(x, y);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = null;
        location.terrainFeatures.Add(tile, hoeDirt);
        return location;
    }

    [Fact]
    public void ProcessDayStart_BareTile_DecaysHealth()
    {
        var location = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -1f), Times.Once);
    }

    [Fact]
    public void ProcessDayStart_LowHealth_DoesNotGoNegative()
    {
        var location = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "summer";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(1f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -3f), Times.Once);
    }

    [Fact]
    public void ProcessDayStart_AtZeroHealth_StaysAtZero()
    {
        var location = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(0f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -1f), Times.Once);
    }

    [Fact]
    public void ProcessDayStart_SummerDecay_HigherThanSpring()
    {
        var springLocation = CreateLocationWithBareTile("SpringFarm", 10, 10);
        var summerLocation = CreateLocationWithBareTile("SummerFarm", 10, 10);
        var tile = new Vector2(10, 10);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("SpringFarm", tile)).Returns(50f);
        _mockSoilHealthService.Setup(x => x.GetSoilHealth("SummerFarm", tile)).Returns(50f);

        _seasonStub.CurrentSeason = "spring";
        _service.ProcessDayStart("SpringFarm");

        _seasonStub.CurrentSeason = "summer";
        _service.ProcessDayStart("SummerFarm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("SpringFarm", tile, -1f), Times.Once);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("SummerFarm", tile, -3f), Times.Once);
    }

    [Fact]
    public void ProcessDayStart_Winter_NoDecay()
    {
        var location = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "winter";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void ProcessDayStart_CoveredTile_NoDecay()
    {
        var location = new GameLocation("Maps\\Farm", "Farm");
        var tile = new Vector2(10, 10);
        var hoeDirt = new HoeDirt(0, location);
        hoeDirt.crop = new Crop("0", 10, 10, location);
        location.terrainFeatures.Add(tile, hoeDirt);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }
}
