using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Tests for <see cref="CompostApplicationService.TryApplyCompost"/>.
/// </summary>
/// <remarks>
/// <para>
/// The service rejects with <c>Game1.playSound("cancel")</c> on every failure path, so these
/// tests install <see cref="GameStateFixture"/>, which substitutes
/// <see cref="NoOpSoundsHelperStub"/> for the game sound helper.
/// </para>
/// <para>
/// <b>Blocked in a unit-test host:</b> four tests need a real <c>HoeDirt</c> in the target
/// location's terrain features, because the service checks the tile type before it looks at
/// soil health or the held item. <c>HoeDirt</c>'s constructors all funnel into
/// <c>initialize(Game1.currentLocation)</c>, and <c>Game1.currentLocation</c> reads
/// <c>Game1.game1</c>, which cannot be constructed without the XNA content service provider.
/// Those tests are retained unmodified in intent and still fail; see
/// <see cref="GameLocationFixture"/>.
/// </para>
/// </remarks>
public class CompostApplicationServiceTests
{
    private readonly Mock<ISoilHealthService> _mockSoilHealthService;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly PlayerProviderStub _playerStub;
    private readonly CompostApplicationService _service;

    /// <summary>Creates the service under test and installs the minimum required game state.</summary>
    public CompostApplicationServiceTests()
    {
        GameStateFixture.Install();
        _mockSoilHealthService = new Mock<ISoilHealthService>();
        _mockMonitor = new Mock<IMonitor>();
        _playerStub = new PlayerProviderStub();

        _service = new CompostApplicationService(
            _mockSoilHealthService.Object,
            _playerStub,
            _mockMonitor.Object);
    }

    /// <summary>Compost raises soil health on a valid tilled farm tile (FR-6.1).</summary>
    [Fact]
    public void TryApplyCompost_ValidTile_IncreasesHealth()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(50f);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }

    /// <summary>A tile already at maximum health is rejected (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_AtMaxHealth_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
    }

    /// <summary>Compost is rejected outside the farm and the Greenhouse (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_InvalidLocation_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Town", isFarm: false);
        var tile = new Vector2(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>Compost is rejected when the player is not holding compost (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_NoCompostHeld_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = null;

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>Compost is rejected when the target tile is not tilled soil (FR-6.2).</summary>
    [Fact]
    public void TryApplyCompost_NonHoeDirtTile_ReturnsFalse()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.False(result);
    }

    /// <summary>
    /// Health is capped at <see cref="ModConstants.MaxSoilHealth"/>: the service reports success
    /// but the applied delta may not push the tile past the maximum (FR-6.1).
    /// </summary>
    [Fact]
    public void TryApplyCompost_HealthCappedAtMax()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);

        _playerStub.CurrentItem = ItemFactory.CreateItem(ModConstants.CompostItemId);

        _mockSoilHealthService.Setup(x => x.GetSoilHealth("Farm", tile)).Returns(90f);

        var result = _service.TryApplyCompost(fixture.Location, tile);

        Assert.True(result);
        _mockSoilHealthService.Verify(x => x.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
    }
}
