using System;
using System.Collections.Generic;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Characterization tests for <see cref="MulchApplicationService"/> (013 mulch soil cover).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MulchApplicationService.TryApplyMulch"/> and <see cref="MulchApplicationService.TickMulch"/>
/// previously had no unit-level coverage: the verify path was exercised only through headless Gate 3
/// runs. The test fixtures install the small amount of global game state the service touches
/// (<c>Game1.sounds</c>, the item registry, and a location context) so the service can run without
/// booting the game.
/// </para>
/// <para>
/// These tests pin the current contract: eligibility (farm or Greenhouse, tilled soil, held mulch),
/// refusal message keys, the seven-day lifetime, sparse persistence, and the load-abort guard. They
/// deliberately use real <see cref="GameLocation"/> and <c>HoeDirt</c> instances rather than mocks,
/// because the tile-type gate is part of the behavior under test.
/// </para>
/// </remarks>
public class MulchApplicationServiceTests
{
    private readonly Mock<IModDataService> _modDataService;
    private readonly Mock<IMonitor> _monitor;
    private readonly PlayerProviderStub _playerProvider;
    private readonly MulchApplicationService _service;

    /// <summary>Creates the service under test and installs the minimum required game state.</summary>
    public MulchApplicationServiceTests()
    {
        GameStateFixture.Install();
        _modDataService = new Mock<IModDataService>();
        _monitor = new Mock<IMonitor>();
        _playerProvider = new PlayerProviderStub();
        _service = new MulchApplicationService(_modDataService.Object, _playerProvider, _monitor.Object);
    }

    /// <summary>A tilled farm tile accepts mulch, stores seven days, and consumes one item.</summary>
    [Fact]
    public void TryApplyMulch_OnTilledFarmTile_StoresSevenDaysAndConsumesOneItem()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);
        var item = ItemFactory.CreateItem(ModConstants.MulchItemId);
        item.Stack = 2;
        _playerProvider.CurrentItem = item;

        var applied = _service.TryApplyMulch(null!, fixture.Location, tile, out var messageKey);

        Assert.True(applied);
        Assert.Equal(string.Empty, messageKey);
        Assert.True(_service.IsMulched("Farm", tile));
        Assert.Equal(1, item.Stack);
        Assert.Single(fixture.Location.TemporarySprites);
        Assert.Equal(1, _service.MulchVersion);
    }

    /// <summary>Mulch expires exactly after the configured seven day ticks.</summary>
    [Fact]
    public void TickMulch_AfterSevenDays_Expires()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);
        Assert.True(_service.TryApplyMulch(null!, fixture.Location, tile, out _));

        for (var day = 0; day < ModConstants.MulchDurationDays; day++)
        {
            Assert.True(_service.IsMulched("Farm", tile));
            _service.TickMulch("Farm", tile);
        }

        Assert.False(_service.IsMulched("Farm", tile));
        Assert.Equal(1 + ModConstants.MulchDurationDays, _service.MulchVersion);
    }

    /// <summary>Ticking a tile that carries no mulch leaves the state and version untouched.</summary>
    [Fact]
    public void TickMulch_UntrackedTile_DoesNothing()
    {
        _service.TickMulch("Farm", new Vector2(4, 4));

        Assert.False(_service.IsMulched("Farm", new Vector2(4, 4)));
        Assert.Equal(0, _service.MulchVersion);
    }

    /// <summary>Non-farm interiors are refused with the invalid-tile message key.</summary>
    [Fact]
    public void TryApplyMulch_NonFarmInterior_RefusesWithInvalidTileKey()
    {
        var fixture = new GameLocationFixture("Town", isFarm: false);
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);

        var applied = _service.TryApplyMulch(null!, fixture.Location, new Vector2(10, 10), out var messageKey);

        Assert.False(applied);
        Assert.Equal("message.mulch.invalid-tile", messageKey);
        Assert.False(_service.IsMulched("Town", new Vector2(10, 10)));
    }

    /// <summary>The Greenhouse is eligible even though it is not flagged as a farm.</summary>
    [Fact]
    public void TryApplyMulch_Greenhouse_Applies()
    {
        var fixture = new GameLocationFixture("Greenhouse", isFarm: false);
        var tile = new Vector2(12, 9);
        fixture.AddHoeDirtTile(12, 9);
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);

        var applied = _service.TryApplyMulch(null!, fixture.Location, tile, out var messageKey);

        Assert.True(applied);
        Assert.Equal(string.Empty, messageKey);
        Assert.True(_service.IsMulched("Greenhouse", tile));
    }

    /// <summary>A farm tile without tilled soil is refused and no state is written.</summary>
    [Fact]
    public void TryApplyMulch_UntilledTile_RefusesWithInvalidTileKey()
    {
        var fixture = new GameLocationFixture("Farm");
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);

        var applied = _service.TryApplyMulch(null!, fixture.Location, new Vector2(10, 10), out var messageKey);

        Assert.False(applied);
        Assert.Equal("message.mulch.invalid-tile", messageKey);
        Assert.False(_service.IsMulched("Farm", new Vector2(10, 10)));
    }

    /// <summary>A second application on the same tile is refused with the already-mulched key.</summary>
    [Fact]
    public void TryApplyMulch_AlreadyMulched_RefusesWithAlreadyKey()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);
        var item = ItemFactory.CreateItem(ModConstants.MulchItemId);
        item.Stack = 2;
        _playerProvider.CurrentItem = item;
        Assert.True(_service.TryApplyMulch(null!, fixture.Location, tile, out _));

        var applied = _service.TryApplyMulch(null!, fixture.Location, tile, out var messageKey);

        Assert.False(applied);
        Assert.Equal("message.mulch.already", messageKey);
        Assert.Equal(1, item.Stack);
        Assert.Equal(1, _service.MulchVersion);
    }

    /// <summary>An empty hand is refused without a refusal key (the caller shows no message).</summary>
    [Fact]
    public void TryApplyMulch_NoHeldItem_RefusesWithoutMessageKey()
    {
        var fixture = new GameLocationFixture("Farm");
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = null;

        var applied = _service.TryApplyMulch(null!, fixture.Location, new Vector2(10, 10), out var messageKey);

        Assert.False(applied);
        Assert.Equal(string.Empty, messageKey);
        Assert.False(_service.IsMulched("Farm", new Vector2(10, 10)));
    }

    /// <summary>Holding an unrelated item is refused without a message key and without mutation.</summary>
    [Fact]
    public void TryApplyMulch_WrongHeldItem_RefusesWithoutMessageKey()
    {
        var fixture = new GameLocationFixture("Farm");
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = ItemFactory.CreateItem("Object.Stone");

        var applied = _service.TryApplyMulch(null!, fixture.Location, new Vector2(10, 10), out var messageKey);

        Assert.False(applied);
        Assert.Equal(string.Empty, messageKey);
        Assert.False(_service.IsMulched("Farm", new Vector2(10, 10)));
    }

    /// <summary>Saving writes the sparse mulch state under the save-scoped key.</summary>
    [Fact]
    public void SaveData_AfterApply_WritesSparseStateUnderSaveKey()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);
        Assert.True(_service.TryApplyMulch(null!, fixture.Location, tile, out _));
        MulchState? saved = null;
        _modDataService
            .Setup(x => x.SaveData(It.IsAny<MulchState>(), "mulch_state_save1"))
            .Callback<MulchState, string>((state, _) => saved = state);

        _service.SaveData("save1");

        Assert.NotNull(saved);
        var entry = saved!.LocationMulchData["Farm"]["10,10"];
        Assert.True(entry.HasMulch);
        Assert.Equal(ModConstants.MulchDurationDays, entry.DaysRemaining);
    }

    /// <summary>Loading a saved state rehydrates the tile so it is reported as mulched.</summary>
    [Fact]
    public void LoadData_WithSavedState_RehydratesMulchedTile()
    {
        var state = new MulchState();
        state.LocationMulchData["Farm"] = new Dictionary<string, MulchTileEntry>
        {
            ["10,10"] = new MulchTileEntry { HasMulch = true, DaysRemaining = 3 }
        };
        _modDataService.Setup(x => x.LoadData<MulchState>("mulch_state_save1")).Returns(state);

        _service.LoadData("save1");

        Assert.True(_service.IsMulched("Farm", new Vector2(10, 10)));
        Assert.False(_service.IsMulched("Farm", new Vector2(11, 10)));
    }

    /// <summary>An invalid save id aborts loading and suppresses the later save.</summary>
    [Fact]
    public void LoadData_InvalidSaveId_AbortsAndSuppressesSave()
    {
        _service.LoadData("   ");

        _service.SaveData("save1");

        _modDataService.Verify(x => x.SaveData(It.IsAny<MulchState>(), It.IsAny<string>()), Times.Never);
        _modDataService.Verify(x => x.LoadData<MulchState>(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Reset clears mulched tiles and the version counter.</summary>
    [Fact]
    public void Reset_AfterApply_ClearsStateAndVersion()
    {
        var fixture = new GameLocationFixture("Farm");
        var tile = new Vector2(10, 10);
        fixture.AddHoeDirtTile(10, 10);
        _playerProvider.CurrentItem = ItemFactory.CreateItem(ModConstants.MulchItemId);
        Assert.True(_service.TryApplyMulch(null!, fixture.Location, tile, out _));

        _service.Reset();

        Assert.False(_service.IsMulched("Farm", tile));
        Assert.Equal(0, _service.MulchVersion);
    }
}
