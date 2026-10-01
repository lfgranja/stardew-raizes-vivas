using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Tests for <see cref="SoilDecayService.ProcessDayStart"/>.
/// </summary>
/// <remarks>
/// Locations are supplied through the injected <see cref="ILocationProvider"/>, so these tests
/// never touch the live <c>Game1.locations</c> registry. Tiles are real <c>HoeDirt</c> instances
/// built by <see cref="GameLocationFixture"/>, which installs the minimum game state
/// (<see cref="GameStateFixture"/>) those constructors require.
/// </remarks>
public class SoilDecayServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<ILocationProvider> _mockLocationProvider;
    private readonly SeasonProviderStub _seasonStub;
    private readonly SeasonalDecayMultiplier _seasonalMultiplier;
    private readonly SoilDecayService _service;

    /// <summary>Creates the service under test with mocked collaborators.</summary>
    public SoilDecayServiceTests()
    {
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockLocationProvider = new Mock<ILocationProvider>();
        _seasonStub = new SeasonProviderStub();
        _seasonalMultiplier = new SeasonalDecayMultiplier();

        _service = new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalMultiplier,
            _seasonStub,
            _mockLocationProvider.Object);
    }

    /// <summary>Builds a farm location holding one bare tilled tile.</summary>
    /// <param name="locationName">Name the service will look the location up by.</param>
    /// <param name="x">X tile coordinate.</param>
    /// <param name="y">Y tile coordinate.</param>
    /// <returns>The fixture wrapping the location.</returns>
    private GameLocationFixture CreateLocationWithBareTile(string locationName, int x, int y)
    {
        var fixture = new GameLocationFixture(locationName);
        fixture.AddHoeDirtTile(x, y);
        _mockLocationProvider
            .Setup(x => x.GetLocationByName(locationName))
            .Returns(fixture.Location);
        return fixture;
    }

    /// <summary>A bare tilled tile loses the seasonal decay amount each morning.</summary>
    [Fact]
    public void ProcessDayStart_BareTile_DecaysHealth()
    {
        var fixture = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -1f), Times.Once);
    }

    /// <summary>A tile near zero health still decays; clamping happens in the health service.</summary>
    [Fact]
    public void ProcessDayStart_LowHealth_DoesNotGoNegative()
    {
        var fixture = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "summer";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(1f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, -1f), Times.Once);
    }

    /// <summary>A tile already at zero health is not written to.</summary>
    [Fact]
    public void ProcessDayStart_AtZeroHealth_StaysAtZero()
    {
        var fixture = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(0f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>Summer decay exceeds spring decay by the seasonal multiplier.</summary>
    [Fact]
    public void ProcessDayStart_SummerDecay_HigherThanSpring()
    {
        var springFixture = CreateLocationWithBareTile("SpringFarm", 10, 10);
        var summerFixture = CreateLocationWithBareTile("SummerFarm", 10, 10);
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

    /// <summary>Winter applies no decay at all.</summary>
    [Fact]
    public void ProcessDayStart_Winter_NoDecay()
    {
        var fixture = CreateLocationWithBareTile("Farm", 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "winter";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>A tilled tile covered by a crop does not decay.</summary>
    [Fact]
    public void ProcessDayStart_CoveredTile_NoDecay()
    {
        var fixture = new GameLocationFixture("Farm");
        fixture.AddHoeDirtTile(10, 10, hasCrop: true);
        _mockLocationProvider
            .Setup(x => x.GetLocationByName("Farm"))
            .Returns(fixture.Location);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        _service.ProcessDayStart("Farm");

        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }
}
