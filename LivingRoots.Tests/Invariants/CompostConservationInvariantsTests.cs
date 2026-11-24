using System.Globalization;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// T035a — explicit SC-009 conservation audit for spec 004 (Playable Compost
/// Loop): zero instances of player-provided organic material or finished
/// compost are destroyed. The audit exercises the three scenarios the success
/// criterion names — (a) repeated collect attempts from a full inventory,
/// (b) picking a bin up and putting it back down mid-batch, and (c) a
/// floating-world-debris bin with an unavailable inventory — plus the
/// property-based invariant that <c>Item.modData</c> keys
/// <see cref="ModConstants.ModDataInputIdKey"/> +
/// <see cref="ModConstants.ModDataDaysRemainingKey"/> survive the dislodge /
/// re-place round trip.
/// </summary>
/// <remarks>
/// <para>
/// Every case drives the service to a GENUINE state through the production
/// path (<see cref="CompostingBinService.AddWaste"/> /
/// <see cref="CompostingBinService.ProcessDayStart"/>) and compares against
/// what the service itself produced (the item id fed in via
/// <see cref="CompostingBinService.AddWaste"/>, or the modData the service
/// wrote). No case stages an arbitrary state model and compares it to service
/// output — that compared manufactured input to production output and failed
/// for reasons the audit does not care about.
/// </para>
/// <para>
/// Per-case isolation (T280(b)) is mandatory: a fresh
/// <see cref="TimeProviderStub"/>, <see cref="CompostingBinService"/> and
/// <see cref="RecordingPlayerInventory"/> are constructed as method locals in
/// every test, because the service's per-location day-start guard and its
/// runtime cache leak state across FsCheck cases when instances are shared.
/// </para>
/// </remarks>
public class CompostConservationInvariantsTests
{
    /// <summary>
    /// Installs the minimum game state the service touches through the paths
    /// under audit (<c>Game1.playSound</c>, <c>Game1.showRedMessage</c>).
    /// </summary>
    public CompostConservationInvariantsTests()
    {
        GameStateFixture.Install();
    }

    // ------------------------------------------------------------------
    // (a) Repeated collect attempts from a full inventory — zero destruction.
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-009 (a): for any number of repeated collect attempts on a Ready
    /// batch while the inventory is full, the batch is retained — the bin
    /// stays <see cref="CompostingBinState.Ready"/>, no compost is ever
    /// granted, and nothing is ever added to the player's inventory.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SC009_RepeatedCollectFromFullInventory_NeverDestroysFinishedCompost(
        PositiveInt arbitraryAttempts,
        PositiveInt arbitraryTileX,
        PositiveInt arbitraryTileY)
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        int attempts = (arbitraryAttempts.Get % 50) + 1;
        var tile = new Vector2(arbitraryTileX.Get % 64, arbitraryTileY.Get % 64);
        const string locationName = "Farm";

        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub();
        var service = CreateService(inventory, timeStub);

        // One waste item in, matured to Ready.
        StageReadyBin(service, timeStub, locationName, tile, startDay: 1);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        int totalGranted = 0;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            totalGranted += service.CollectCompost(locationName, tile);

            // The finished compost is never consumed and never re-granted.
            Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));
            Assert.Empty(inventory.AddedItems);
        }

        Assert.Equal(0, totalGranted);
    }

    /// <summary>
    /// SC-009 (a): for any number of refused attempts from a full inventory,
    /// once the inventory accepts items exactly ONE compost is delivered in
    /// total — the refused attempts never duplicated it, and the retention
    /// path never lost it.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SC009_RefusedCollects_ThenInventoryAccepts_DeliversExactlyOneCompostTotal(
        PositiveInt arbitraryAttempts,
        PositiveInt arbitraryTileX,
        PositiveInt arbitraryTileY)
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        int attempts = (arbitraryAttempts.Get % 50) + 1;
        var tile = new Vector2(arbitraryTileX.Get % 64, arbitraryTileY.Get % 64);
        const string locationName = "Farm";

        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub();
        var service = CreateService(inventory, timeStub);

        StageReadyBin(service, timeStub, locationName, tile, startDay: 1);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // Every refused attempt grants nothing and leaves the batch intact.
        int totalGranted = 0;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            totalGranted += service.CollectCompost(locationName, tile);
            Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));
        }

        Assert.Equal(0, totalGranted);
        Assert.Empty(inventory.AddedItems);

        // The inventory accepts again: the conserved batch is delivered once.
        inventory.AcceptsItems = true;
        totalGranted += service.CollectCompost(locationName, tile);

        Assert.Equal(1, totalGranted);
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));

        // The emptied bin can never re-grant the batch it already produced.
        Assert.Equal(0, service.CollectCompost(locationName, tile));
        Assert.Single(inventory.AddedItems);
    }

    // ------------------------------------------------------------------
    // (b) Pick up a bin mid-batch and put it back down.
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-009 (b): for any start day, any mid-batch moment and any number of
    /// days spent unplaced, picking a Processing bin up and placing it back
    /// down restores <see cref="CompostingBinState.Processing"/> with the same
    /// input item id, the unplaced days do NOT advance the batch, and after
    /// the remaining days elapse the bin reaches
    /// <see cref="CompostingBinState.Ready"/> and yields exactly one compost.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SC009_BinPickedUpMidBatch_RePlaced_RestoresProgressAndYieldsExactlyOneCompost(
        PositiveInt arbitraryStartDay,
        PositiveInt arbitraryElapsedDays,
        PositiveInt arbitraryUnplacedDays,
        PositiveInt arbitraryTileX,
        PositiveInt arbitraryTileY)
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        int startDay = 1 + (arbitraryStartDay.Get % 500);
        int elapsedDays = arbitraryElapsedDays.Get % ModConstants.MaturationDays;
        int unplacedDays = 1 + (arbitraryUnplacedDays.Get % 5);
        var tile = new Vector2(arbitraryTileX.Get % 64, arbitraryTileY.Get % 64);
        const string locationName = "Farm";

        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = startDay };
        var service = CreateService(inventory, timeStub);

        // Drive the service to a genuine mid-batch state and remember the
        // input id it recorded.
        var waste = ItemFactory.CreateValidWasteItem();
        string fedInputId = waste.QualifiedItemId;

        service.AddWaste(locationName, tile, waste);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState(locationName, tile));

        timeStub.TotalDays = startDay + elapsedDays;
        service.ProcessDayStart(locationName);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState(locationName, tile));

        // Pick the bin up: the carried item serializes the batch progress the
        // service recorded, including the days still remaining.
        var dislodged = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(dislodged);
        Assert.Equal(1, dislodged.Stack);
        Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataInputIdKey));
        Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
        Assert.Equal(fedInputId, dislodged.modData[ModConstants.ModDataInputIdKey]);

        int remainingBefore = int.Parse(
            dislodged.modData[ModConstants.ModDataDaysRemainingKey],
            CultureInfo.InvariantCulture);
        Assert.Equal(ModConstants.MaturationDays - elapsedDays, remainingBefore);

        service.OnObjectRemoved(locationName, tile);

        // Days pass while the bin is unplaced: no runtime record exists, so
        // no day-start delivery can advance (or consume) the batch.
        for (int day = 0; day < unplacedDays; day++)
        {
            timeStub.TotalDays++;
            service.ProcessDayStart(locationName);
            Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));
        }

        // Put it back down on a fresh day for the per-location day-start guard.
        timeStub.TotalDays++;
        int placementDay = timeStub.TotalDays;
        service.OnBinPlaced(locationName, tile, dislodged);

        // Restored: Processing, same input, and NOTHING of the batch consumed
        // by the unplaced days — the remaining days are carried over exactly.
        Assert.Equal(CompostingBinState.Processing, service.GetBinState(locationName, tile));

        var reDislodged = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(reDislodged);
        Assert.Equal(fedInputId, reDislodged.modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal(
            remainingBefore.ToString(CultureInfo.InvariantCulture),
            reDislodged.modData[ModConstants.ModDataDaysRemainingKey]);

        // The remaining days elapse: Ready exactly that many days after
        // re-placement — never earlier (a shortcut) and never later (a reset).
        for (int day = 1; day <= remainingBefore; day++)
        {
            timeStub.TotalDays = placementDay + day;
            service.ProcessDayStart(locationName);
            Assert.Equal(
                day >= remainingBefore ? CompostingBinState.Ready : CompostingBinState.Processing,
                service.GetBinState(locationName, tile));
        }

        // The conserved batch yields exactly one compost — nothing destroyed,
        // nothing duplicated.
        Assert.Equal(1, service.CollectCompost(locationName, tile));
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));
    }

    // ------------------------------------------------------------------
    // (c) modData progress preserved through the dislodge / re-place round trip.
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-009 property-based invariant: for any batch the service itself
    /// produced, <c>CreateDislodgedBinItem</c> serializes both
    /// <see cref="ModConstants.ModDataInputIdKey"/> and
    /// <see cref="ModConstants.ModDataDaysRemainingKey"/> into
    /// <c>Item.modData</c>, and <c>OnBinPlaced</c> restores an equivalent
    /// state carrying the same input id. The state under test is always driven
    /// through <see cref="CompostingBinService.AddWaste"/> /
    /// <see cref="CompostingBinService.ProcessDayStart"/> first — never a
    /// manufactured model — so the comparison is service output against the
    /// input the service itself consumed.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SC009_DislodgeToModData_RePlace_RestoresEquivalentBatchState(
        PositiveInt arbitraryStartDay,
        PositiveInt arbitraryElapsedDays,
        PositiveInt arbitraryBatchKind,
        PositiveInt arbitraryTileX,
        PositiveInt arbitraryTileY)
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        int startDay = 1 + (arbitraryStartDay.Get % 500);
        int elapsedDays = arbitraryElapsedDays.Get % ModConstants.MaturationDays;
        bool toReady = (arbitraryBatchKind.Get % 2) == 1;
        var tile = new Vector2(arbitraryTileX.Get % 64, arbitraryTileY.Get % 64);
        const string locationName = "Farm";

        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = startDay };
        var service = CreateService(inventory, timeStub);

        // Drive the service to a genuine Processing or Ready batch, capturing
        // the input id it consumed from the waste item.
        var waste = ItemFactory.CreateValidWasteItem();
        string fedInputId = waste.QualifiedItemId;

        service.AddWaste(locationName, tile, waste);
        Assert.Equal(CompostingBinState.Processing, service.GetBinState(locationName, tile));

        if (toReady)
        {
            timeStub.TotalDays = startDay + ModConstants.MaturationDays;
            service.ProcessDayStart(locationName);
        }
        else
        {
            timeStub.TotalDays = startDay + elapsedDays;
            service.ProcessDayStart(locationName);
        }

        var expectedState = toReady ? CompostingBinState.Ready : CompostingBinState.Processing;
        Assert.Equal(expectedState, service.GetBinState(locationName, tile));

        // Dislodge: the carried item must serialize BOTH progress keys.
        var dislodged = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(dislodged);
        Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataInputIdKey));
        Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
        Assert.Equal(fedInputId, dislodged.modData[ModConstants.ModDataInputIdKey]);

        // Re-place through the modData the service just wrote.
        service.OnObjectRemoved(locationName, tile);
        service.OnBinPlaced(locationName, tile, dislodged);

        // The restored record is equivalent: same state, same input, same
        // remaining days — dislodging again reads back exactly what was written.
        Assert.Equal(expectedState, service.GetBinState(locationName, tile));

        var reDislodged = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(reDislodged);
        Assert.Equal(fedInputId, reDislodged.modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal(
            dislodged.modData[ModConstants.ModDataDaysRemainingKey],
            reDislodged.modData[ModConstants.ModDataDaysRemainingKey]);

        if (expectedState == CompostingBinState.Ready)
        {
            // A restored Ready batch is producible exactly once — the round
            // trip cannot forge a batch grant out of the carried modData.
            Assert.Equal(1, service.CollectCompost(locationName, tile));
            Assert.Single(inventory.AddedItems);
            Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        }
    }

    // ------------------------------------------------------------------
    // (d) Floating-world-debris bin with an unavailable inventory.
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-009 (c): a Ready bin picked up while the inventory is unavailable
    /// lands as floating world debris, the debris carries its batch with
    /// <see cref="ModConstants.ModDataDaysRemainingKey"/> == "0" (no days
    /// remaining, so re-placing can neither shortcut nor extend processing),
    /// and re-placing restores <see cref="CompostingBinState.Ready"/>. The
    /// conserved batch is then delivered exactly once when room appears.
    /// </summary>
    [Fact]
    public void SC009_FloatingWorldDebris_WithUnavailableInventory_RestoresReadyOnRePlacement()
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        const string locationName = "Farm";
        var tile = new Vector2(8, 12);

        // The inventory is unavailable. At the service seam this refuses the
        // finished compost; in production the same unavailable inventory is
        // what makes the picked-up bin land as floating world debris
        // (ModController's debris fallback) instead of an inventory item, so
        // the service contract must conserve the batch with no inventory help.
        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub();
        var service = CreateService(inventory, timeStub);

        StageReadyBin(service, timeStub, locationName, tile, startDay: 4);

        // The full inventory refuses the finished compost, and the batch is
        // retained.
        Assert.Equal(0, service.CollectCompost(locationName, tile));
        Assert.Empty(inventory.AddedItems);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // The bin is picked up; with no inventory room it is floating debris.
        // The item still exists and still carries the batch.
        var debris = service.CreateDislodgedBinItem(locationName, tile);
        Assert.NotNull(debris);
        Assert.True(debris.modData.ContainsKey(ModConstants.ModDataInputIdKey));
        Assert.True(debris.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));

        // "0" is the serialized "no days remaining" marker the service writes
        // for a Ready batch; re-placing it therefore restores Ready.
        Assert.Equal("0", debris.modData[ModConstants.ModDataDaysRemainingKey]);
        Assert.Equal(1, debris.Stack);

        service.OnObjectRemoved(locationName, tile);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));

        // The floating debris is re-placed and the batch survives: Ready again.
        service.OnBinPlaced(locationName, tile, debris);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // With room in the inventory, the conserved batch is delivered once.
        inventory.AcceptsItems = true;
        Assert.Equal(1, service.CollectCompost(locationName, tile));
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));
    }

    // ------------------------------------------------------------------
    // (e) Total conservation across the full lifecycle.
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-009 (total): one waste item in yields exactly one compost item out,
    /// no matter how many refused collect attempts intervened — the refused
    /// attempts neither lose nor duplicate the batch.
    /// </summary>
    [Fact]
    public void SC009_FullLifecycleConservation_OneWasteInExactlyOneCompostOut()
    {
        // T280(b): per-case isolation — fresh instances as method locals.
        GameStateFixture.Install();

        const string locationName = "Farm";
        const int refusedAttempts = 12;
        var tile = new Vector2(10, 10);

        var inventory = new RecordingPlayerInventory { AcceptsItems = false };
        var timeStub = new TimeProviderStub();
        var service = CreateService(inventory, timeStub);

        StageReadyBin(service, timeStub, locationName, tile, startDay: 1);
        Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));

        // Every refusal grants nothing and leaves the batch intact.
        int totalGranted = 0;
        for (int attempt = 0; attempt < refusedAttempts; attempt++)
        {
            totalGranted += service.CollectCompost(locationName, tile);
            Assert.Equal(CompostingBinState.Ready, service.GetBinState(locationName, tile));
        }

        Assert.Equal(0, totalGranted);
        Assert.Empty(inventory.AddedItems);

        // The one delivery that finally lands is the whole batch.
        inventory.AcceptsItems = true;
        totalGranted += service.CollectCompost(locationName, tile);

        Assert.Equal(1, totalGranted);
        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
        Assert.Equal(CompostingBinState.Empty, service.GetBinState(locationName, tile));

        // The emptied bin can never re-grant the batch it already produced.
        Assert.Equal(0, service.CollectCompost(locationName, tile));
        Assert.Single(inventory.AddedItems);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Builds a service wired to the caller's method-local inventory and time
    /// stub, so every FsCheck case runs against fresh state (T280(b)).
    /// </summary>
    private static CompostingBinService CreateService(
        IPlayerInventory inventory,
        ITimeProvider timeProvider)
    {
        var mockModDataService = new Mock<IModDataService>();
        var mockSaveIdProvider = new Mock<ISaveIdProvider>();
        var mockWasteValidator = new Mock<IOrganicWasteValidator>();
        var mockMonitor = new Mock<IMonitor>();
        mockWasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        return new CompostingBinService(
            mockModDataService.Object,
            mockSaveIdProvider.Object,
            mockWasteValidator.Object,
            mockMonitor.Object,
            timeProvider,
            new CompostingBinFactory(),
            inventory);
    }

    /// <summary>
    /// Drives a bin to a genuine <see cref="CompostingBinState.Ready"/> batch
    /// through the production path: one waste item in at
    /// <paramref name="startDay"/>, matured through
    /// <see cref="CompostingBinService.ProcessDayStart"/>.
    /// </summary>
    private static void StageReadyBin(
        CompostingBinService service,
        TimeProviderStub timeProvider,
        string locationName,
        Vector2 tile,
        int startDay)
    {
        timeProvider.TotalDays = startDay;
        service.AddWaste(locationName, tile, ItemFactory.CreateValidWasteItem());
        timeProvider.TotalDays = startDay + ModConstants.MaturationDays;
        service.ProcessDayStart(locationName);
    }
}

// T280(b): per-case isolation — every test constructs a fresh TimeProviderStub,
// CompostingBinService and RecordingPlayerInventory as method locals, following
// the CompostingMachineInvariantsTests pattern. Shared instances leak state
// across FsCheck cases through the service's per-location day-start guard
// (T251) and its runtime cache.
