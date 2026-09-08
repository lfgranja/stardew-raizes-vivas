using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using Xunit;

namespace LivingRoots.Tests;

public class SoilDecayServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly SeasonProviderStub _seasonProviderStub;
    private readonly SeasonalDecayMultiplier _seasonalDecayMultiplier;
    private readonly SoilDecayService _service;

    public SoilDecayServiceTests()
    {
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _seasonProviderStub = new SeasonProviderStub();
        _seasonalDecayMultiplier = new SeasonalDecayMultiplier();
        _service = new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalDecayMultiplier,
            _seasonProviderStub);
    }

    private GameLocation SetupLocationWithBareTile(string locationName, Vector2 tile, float initialHealth)
    {
        var location = GameLocationFixture.CreateLocation(name: locationName);
        GameLocationFixture.AddHoeDirtTile(location, tile);

        _mockSoilHealthService
            .Setup(s => s.GetSoilHealth(locationName, tile))
            .Returns(initialHealth);

        return location;
    }

    [Fact]
    public void ProcessDayStart_BareTile_DecaysHealth()
    {
        // Arrange
        var tile = new Vector2(5, 5);
        const float initialHealth = 50f;
        SetupLocationWithBareTile("Farm", tile, initialHealth);
        _seasonProviderStub.CurrentSeason = "spring";

        // Act
        _service.ProcessDayStart("Farm");

        // Assert: spring multiplier = 0.5, DailyDecayRate = 2.0, decay = 1.0
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth("Farm", tile, -1.0f),
            Times.Once);
    }

    [Fact]
    public void ProcessDayStart_LowHealth_DoesNotGoNegative()
    {
        // Arrange
        var tile = new Vector2(5, 5);
        const float initialHealth = 0.5f;
        SetupLocationWithBareTile("Farm", tile, initialHealth);
        _seasonProviderStub.CurrentSeason = "spring";

        // Act
        _service.ProcessDayStart("Farm");

        // Assert: health 0.5 - 1.0 decay = clamped to 0, actual delta = -0.5
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth("Farm", tile, -0.5f),
            Times.Once);
    }

    [Fact]
    public void ProcessDayStart_AtZeroHealth_StaysAtZero()
    {
        // Arrange
        var tile = new Vector2(5, 5);
        SetupLocationWithBareTile("Farm", tile, 0f);
        _seasonProviderStub.CurrentSeason = "spring";

        // Act
        _service.ProcessDayStart("Farm");

        // Assert: no decay applied when health is already 0
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    [Fact]
    public void ProcessDayStart_SummerDecay_HigherThanSpring()
    {
        // Arrange
        var springTile = new Vector2(5, 5);
        var summerTile = new Vector2(6, 6);
        const float initialHealth = 50f;

        SetupLocationWithBareTile("Farm", springTile, initialHealth);
        SetupLocationWithBareTile("Farm", summerTile, initialHealth);

        // Act
        _seasonProviderStub.CurrentSeason = "spring";
        _service.ProcessDayStart("Farm");

        _seasonProviderStub.CurrentSeason = "summer";
        _service.ProcessDayStart("Farm");

        // Assert: summer (1.5x = 3.0) > spring (0.5x = 1.0)
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth("Farm", springTile, -1.0f),
            Times.Once);

        _mockSoilHealthService.Verify(
            s => s.UpdateHealth("Farm", summerTile, -3.0f),
            Times.Once);
    }

    [Fact]
    public void ProcessDayStart_Winter_NoDecay()
    {
        // Arrange
        var tile = new Vector2(5, 5);
        const float initialHealth = 50f;
        SetupLocationWithBareTile("Farm", tile, initialHealth);
        _seasonProviderStub.CurrentSeason = "winter";

        // Act
        _service.ProcessDayStart("Farm");

        // Assert: winter multiplier = 0.0, no decay calls
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    [Fact]
    public void ProcessDayStart_CoveredTile_NoDecay()
    {
        // Arrange
        var tile = new Vector2(5, 5);
        var location = GameLocationFixture.CreateLocation(name: "Farm");
        var hoeDirt = new HoeDirt(0, location)
        {
            crop = new Crop() // Green Bean seed — tile is not bare
        };
        location.terrainFeatures.Add(tile, hoeDirt);

        _seasonProviderStub.CurrentSeason = "spring";

        // Act
        _service.ProcessDayStart("Farm");

        // Assert: tile has crop, no decay calls
        _mockSoilHealthService.Verify(
            s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }
}
