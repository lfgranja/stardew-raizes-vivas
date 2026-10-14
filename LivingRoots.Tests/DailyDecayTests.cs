using System.Collections.Generic;
using FsCheck;
using LivingRoots;
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
/// RED-phase tests for daily soil decay (T000-R7).
/// Covers construction guards, seasonal multipliers, Greenhouse exemption,
/// clamping, once-per-day idempotency, and decay invariants.
/// </summary>
public class DailyDecayTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly Mock<ILocationProvider> _mockLocationProvider;
    private readonly SeasonProviderStub _seasonStub;
    private readonly TimeProviderStub _timeStub;
    private readonly SeasonalDecayMultiplier _seasonalMultiplier;

    public DailyDecayTests()
    {
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _mockLocationProvider = new Mock<ILocationProvider>();
        _seasonStub = new SeasonProviderStub();
        _timeStub = new TimeProviderStub();
        _seasonalMultiplier = new SeasonalDecayMultiplier();
    }

    private SoilDecayService CreateService()
    {
        return new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalMultiplier,
            _seasonStub,
            _mockLocationProvider.Object,
            _timeStub);
    }

    private GameLocationFixture CreateLocationWithBareTile(string locationName, int x, int y)
    {
        var fixture = new GameLocationFixture(locationName);
        fixture.AddHoeDirtTile(x, y);
        _mockLocationProvider
            .Setup(m => m.GetLocationByName(locationName))
            .Returns(fixture.Location);
        return fixture;
    }

    // ── Construction guards ───────────────────────────────────

    /// <summary>A null soil health service is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullSoilHealthService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SoilDecayService(
            null!,
            _mockMonitor.Object,
            _seasonalMultiplier,
            _seasonStub,
            _mockLocationProvider.Object,
            _timeStub));
    }

    /// <summary>A null monitor is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SoilDecayService(
            _mockSoilHealthService.Object,
            null!,
            _seasonalMultiplier,
            _seasonStub,
            _mockLocationProvider.Object,
            _timeStub));
    }

    /// <summary>A null seasonal multiplier is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullSeasonalMultiplier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            null!,
            _seasonStub,
            _mockLocationProvider.Object,
            _timeStub));
    }

    /// <summary>A null season provider is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullSeasonProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalMultiplier,
            null!,
            _mockLocationProvider.Object,
            _timeStub));
    }

    /// <summary>A null location provider is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullLocationProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SoilDecayService(
            _mockSoilHealthService.Object,
            _mockMonitor.Object,
            _seasonalMultiplier,
            _seasonStub,
            null!,
            _timeStub));
    }

    // ── Seasonal multiplier ────────────────────────────────────

    /// <summary>
    /// Each season applies its multiplier to DailyDecayRate: spring and fall
    /// decay 1 (2 x 0.5), summer decays 3 (2 x 1.5), winter decays 0.
    /// </summary>
    [Theory]
    [InlineData("spring", 1f)]
    [InlineData("summer", 3f)]
    [InlineData("fall", 1f)]
    [InlineData("winter", 0f)]
    public void ProcessDayStart_SeasonalMultiplier_AppliesCorrectly(string season, float expectedDecay)
    {
        const string locationName = "Farm";
        CreateLocationWithBareTile(locationName, 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = season;
        _mockSoilHealthService
            .Setup(m => m.GetSoilHealth(locationName, tile))
            .Returns(50f);

        var service = CreateService();
        service.ProcessDayStart(locationName);

        if (expectedDecay > 0f)
        {
            _mockSoilHealthService.Verify(m => m.UpdateHealth(locationName, tile, -expectedDecay), Times.Once);
        }
        else
        {
            _mockSoilHealthService.Verify(
                m => m.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
                Times.Never);
        }
    }

    // ── Greenhouse exemption ──────────────────────────────────

    /// <summary>The Greenhouse is fully exempt and never consults locations or soil health.</summary>
    [Fact]
    public void ProcessDayStart_Greenhouse_ExemptFromDecay()
    {
        var service = CreateService();

        service.ProcessDayStart("Greenhouse");

        _mockLocationProvider.Verify(m => m.GetLocationByName(It.IsAny<string>()), Times.Never);
        _mockSoilHealthService.Verify(
            m => m.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    // ── Clamping ──────────────────────────────────────────────

    /// <summary>Decay never drives health below zero: the write is clamped to the floor.</summary>
    [Fact]
    public void ProcessDayStart_LowHealth_ClampedToZero()
    {
        const string locationName = "Farm";
        CreateLocationWithBareTile(locationName, 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";
        _mockSoilHealthService
            .Setup(m => m.GetSoilHealth(locationName, tile))
            .Returns(0.5f);

        var service = CreateService();
        service.ProcessDayStart(locationName);

        // newHealth = max(0.5 - 1, 0) = 0, so actualDecay = 0.5 (not the full 1).
        _mockSoilHealthService.Verify(m => m.UpdateHealth(locationName, tile, -0.5f), Times.Once);
    }

    /// <summary>A tile already at zero health is never written.</summary>
    [Fact]
    public void ProcessDayStart_AtZeroHealth_StaysAtZero()
    {
        const string locationName = "Farm";
        CreateLocationWithBareTile(locationName, 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";
        _mockSoilHealthService
            .Setup(m => m.GetSoilHealth(locationName, tile))
            .Returns(0f);

        var service = CreateService();
        service.ProcessDayStart(locationName);

        _mockSoilHealthService.Verify(
            m => m.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()),
            Times.Never);
    }

    // ── Once-per-day idempotency ──────────────────────────────

    /// <summary>A second ProcessDayStart for the same location and day is a no-op.</summary>
    [Fact]
    public void ProcessDayStart_SecondCallSameDay_IsNoOp()
    {
        const string locationName = "Farm";
        CreateLocationWithBareTile(locationName, 10, 10);
        var tile = new Vector2(10, 10);
        _seasonStub.CurrentSeason = "spring";
        _mockSoilHealthService
            .Setup(m => m.GetSoilHealth(locationName, tile))
            .Returns(50f);

        var service = CreateService();

        service.ProcessDayStart(locationName);
        service.ProcessDayStart(locationName);

        _mockSoilHealthService.Verify(m => m.UpdateHealth(locationName, tile, -1f), Times.Once);

        // Advancing the day allows decay to run again.
        _timeStub.TotalDays = 2;
        _mockSoilHealthService
            .Setup(m => m.GetSoilHealth(locationName, tile))
            .Returns(49f);
        service.ProcessDayStart(locationName);

        _mockSoilHealthService.Verify(m => m.UpdateHealth(locationName, tile, -1f), Times.Exactly(2));
    }

    // ── Property: decay result clamped to [0, 100] ────────────

    /// <summary>For any starting health in range, the decayed health stays within [0, 100].</summary>
    [Property(MaxTest = 100)]
    public Property DecayResult_ClampedToMinMax()
    {
        return Prop.ForAll(
            ValidHealthGenerator(),
            health =>
            {
                var result = ApplyDecay(health, "spring");
                return result >= ModConstants.MinSoilHealth
                    && result <= ModConstants.MaxSoilHealth;
            });
    }

    // ── Property: any season produces valid health ────────────

    /// <summary>For any starting health and any season string, decay yields a valid in-range health.</summary>
    [Property(MaxTest = 100)]
    public Property DecayWithAnySeason_ProducesValidHealth()
    {
        return Prop.ForAll(
            ValidHealthGenerator(),
            NonNullSeasonGenerator(),
            (health, season) =>
            {
                var result = ApplyDecay(health, season);
                return result >= ModConstants.MinSoilHealth
                    && result <= ModConstants.MaxSoilHealth
                    && !float.IsNaN(result)
                    && !float.IsInfinity(result);
            });
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>Generates a health value in [MinSoilHealth, MaxSoilHealth] at 0.01 resolution.</summary>
    private static Gen<float> ValidHealthGenerator()
    {
        return Gen
            .Choose((int)(ModConstants.MinSoilHealth * 100), (int)(ModConstants.MaxSoilHealth * 100))
            .Select(i => i / 100f);
    }

    /// <summary>Generates an arbitrary non-null season string (null is coerced to "unknown").</summary>
    private static Gen<string> NonNullSeasonGenerator()
    {
        return Arb.Generate<string>().Select(s => s ?? "unknown");
    }

    /// <summary>
    /// Runs one decay pass for the given starting health and season against a
    /// fresh service, returning the resulting health (the input health when no
    /// decay was applied).
    /// </summary>
    private float ApplyDecay(float health, string season)
    {
        const string locationName = "Farm";
        var fixture = new GameLocationFixture(locationName);
        fixture.AddHoeDirtTile(10, 10);

        var mockSoil = new Mock<ISoilHealthService>();
        var mockMonitor = new Mock<IMonitor>();
        var mockLoc = new Mock<ILocationProvider>();
        mockLoc.Setup(m => m.GetLocationByName(locationName)).Returns(fixture.Location);

        var tile = new Vector2(10, 10);
        mockSoil.Setup(m => m.GetSoilHealth(locationName, tile)).Returns(health);

        var seasonStub = new SeasonProviderStub { CurrentSeason = season };
        var timeStub = new TimeProviderStub { TotalDays = 1 };

        var service = new SoilDecayService(
            mockSoil.Object,
            mockMonitor.Object,
            new SeasonalDecayMultiplier(),
            seasonStub,
            mockLoc.Object,
            timeStub);

        float capturedDelta = 0f;
        var updated = false;
        mockSoil
            .Setup(m => m.UpdateHealth(locationName, tile, It.IsAny<float>()))
            .Callback<string, Vector2, float>((_, _, delta) =>
            {
                capturedDelta = delta;
                updated = true;
            });

        service.ProcessDayStart(locationName);

        return updated ? health + capturedDelta : health;
    }
}
