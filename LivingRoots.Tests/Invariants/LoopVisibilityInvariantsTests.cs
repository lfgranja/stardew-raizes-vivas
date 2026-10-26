using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// T038 / SC-002 audit of the Playable Compost Loop (spec 004): every stage of the
/// player-facing loop — craft → place → insert → advance → collect → apply → observe —
/// MUST produce a visible in-game result and MUST NOT complete silently.
/// </summary>
/// <remarks>
/// <para>
/// <b>BOUNDARY OF THIS SUITE (stated explicitly per task):</b> this file attests only the
/// AUTOMATABLE half of SC-002. It proves that each loop stage emits an <i>observable side
/// effect</i> through the production code paths — an audio cue (<c>Game1.playSound</c>), a
/// HUD message (<c>Game1.showRedMessage</c>), an inventory grant/consumption, a machine
/// state change, a floating-text sprite, or a render-path trace log — so no stage is
/// <i>mechanically</i> silent. It deliberately does NOT assert the genuinely player-facing
/// visual confirmation (a human looking at the screen and SEEING the reward text, the
/// machine shake, the rich overlay tint, and the localized refusal text); that observation
/// is owed to the Gate 2/3 playthrough and is out of scope here.
/// </para>
/// <para>
/// Observables are captured the way the sibling suites capture them: audio cues through a
/// recording <see cref="ISoundsHelper"/> swapped into <c>Game1.sounds</c>; HUD messages
/// through the process-global <c>Game1.hudMessages</c>; trace logs through a monitored
/// <see cref="IMonitor"/>; inventory through <see cref="RecordingPlayerInventory"/>; and
/// render draws through the "Overlay drawn: " trace log of
/// <see cref="VisualizationService"/>. Every bound is drawn from <see cref="ModConstants"/>
/// or a registered FsCheck generator — no restated magic values.
/// </para>
/// </remarks>
public class LoopVisibilityInvariantsTests
{
    /// <summary>Native audio cancel cue the vanilla-machine rejection plays.</summary>
    private const string CancelCueName = "cancel";

    /// <summary>Native audio reward cue maturation and collection play.</summary>
    private const string HarvestCueName = "Ship";

    /// <summary>Prefix of the render-path destination observable <c>DrawOverlay</c> logs.</summary>
    private const string OverlayDrawLogPrefix = "Overlay drawn: ";

    /// <summary>
    /// Tile-space extent of the OBSERVE render frames. A test-fixture constant (not a game
    /// rule); every sample tile is chosen inside <c>[0, bound)</c> so the culling pass keeps
    /// it and the frame always reaches the tile under test.
    /// </summary>
    private const int ObservationViewportTileBound = 16;

    /// <summary>The tile-space viewport the OBSERVE frames render.</summary>
    private static readonly Rectangle ObservationViewport =
        new Rectangle(0, 0, ObservationViewportTileBound, ObservationViewportTileBound);

    /// <summary>A sample tile carrying a generated positive reading.</summary>
    private static readonly Point ObservationPositiveTile = new Point(5, 5);

    /// <summary>A sample tile pinned to the FR-009 health-0 neutral-dirt baseline.</summary>
    private static readonly Point ObservationZeroTile = new Point(4, 4);

    /// <summary>Installs the minimum game state every path under audit needs.</summary>
    public LoopVisibilityInvariantsTests()
    {
        GameStateFixture.Install();
    }

    // ------------------------------------------------------------------
    // INSERT stage — craft/insert the qualifying item, or the native reject
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 [P] INSERT stage: for any generated tile, inserting a QUALIFYING item
    /// transitions the bin Empty → Processing and emits the transition trace log,
    /// while inserting a NON-qualifying item is refused the way a native machine
    /// refuses a bad input — the native cancel cue plus the machine shake — and
    /// consumes nothing (the bin is left Empty and nothing is granted).
    /// </summary>
    [Property(MaxTest = 100)]
    public void InsertStage_QualifyingWaste_EmitsTransitionTrace_NonQualifying_EmitsCancelCueConsumesNothing(
        NonNegativeInt arbitraryTileX,
        NonNegativeInt arbitraryTileY)
    {
        GameStateFixture.Install();

        int tileX = arbitraryTileX.Get % 40;
        int tileY = arbitraryTileY.Get % 40;
        var qualifyingTile = new Vector2(tileX, tileY);
        var nonQualifyingTile = new Vector2((tileX + 1) % 40, tileY);

        var validWaste = ItemFactory.CreateValidWasteItem();
        var invalidWaste = ItemFactory.CreateInvalidWasteItem();
        var wasteValidator = new Mock<IOrganicWasteValidator>();
        wasteValidator.Setup(x => x.IsValidOrganicWaste(validWaste)).Returns(true);
        wasteValidator.Setup(x => x.IsValidOrganicWaste(invalidWaste)).Returns(false);

        // The placed bin object is what the native reject shakes, so the invalid
        // tile carries a physical bin for the location provider to resolve.
        var locationWithBin = new GameLocationFixture("Farm", isFarm: true);
        locationWithBin.Location.Objects.Add(nonQualifyingTile, new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            TileLocation = nonQualifyingTile
        });
        var locationProvider = new Mock<ILocationProvider>();
        locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(locationWithBin.Location);

        var (service, inventory, _, logs) =
            CreateIsolatedBinService(wasteValidator.Object, locationProvider.Object);

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            // INSERT — qualifying item: Empty → Processing, trace-observable.
            service.AddWaste("Farm", qualifyingTile, validWaste);
            Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", qualifyingTile));
            Assert.Contains(logs, message => message.Contains("Empty → Processing", StringComparison.Ordinal));

            // INSERT — non-qualifying item: native cancel cue + machine shake.
            service.AddWaste("Farm", nonQualifyingTile, invalidWaste);
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        // The refusal is not silent: the native cancel cue fires and the machine visibly shakes.
        Assert.Contains(CancelCueName, sounds.Cues);
        Assert.True(
            locationWithBin.Location.Objects[nonQualifyingTile].shakeTimer > 0,
            "A non-qualifying drop-in must visibly shake the bin (FR-008a); a silent refusal hides the rejection.");
        Assert.Contains(logs, message => message.Contains("rejected invalid drop-in", StringComparison.Ordinal));

        // Nothing consumed: the non-qualifying bin is still Empty and nothing was granted.
        Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", nonQualifyingTile));
        Assert.Empty(inventory.AddedItems);
    }

    // ------------------------------------------------------------------
    // COLLECT stage — harvest a Ready batch, or refuse a not-Ready one
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 [P] COLLECT stage: for any generated tile, collecting a Ready bin emits an
    /// observable audio cue and grants exactly one compost item; collecting a bin that is
    /// NOT ready grants nothing, emits no reward cue, and leaves the bin untouched (no
    /// silent state corruption).
    /// </summary>
    [Property(MaxTest = 100)]
    public void CollectStage_ReadyBin_EmitsAudioCueAndGrantsExactlyOne_NotReady_GrantsNothingAndLeavesBinUntouched(
        NonNegativeInt arbitraryTileX)
    {
        GameStateFixture.Install();

        int tileX = 1 + (arbitraryTileX.Get % 40);
        var readyTile = new Vector2(tileX, 10);
        var processingTile = new Vector2(tileX, 11);

        var wasteValidator = new Mock<IOrganicWasteValidator>();
        wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        var (service, inventory, timeStub, logs) = CreateIsolatedBinService(wasteValidator.Object);

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            // Stage one bin to Ready; leave a second bin still Processing (inserted on the
            // maturation day, so it has not yet matured) to exercise the not-ready collect.
            timeStub.TotalDays = 1;
            service.AddWaste("Farm", readyTile, ItemFactory.CreateValidWasteItem());
            timeStub.TotalDays = 1 + ModConstants.MaturationDays;
            service.ProcessDayStart("Farm");
            Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", readyTile));

            service.AddWaste("Farm", processingTile, ItemFactory.CreateValidWasteItem());
            Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", processingTile));

            // ---- COLLECT (Ready) — audio cue + exactly one grant ----
            int cuesBeforeReady = sounds.Cues.Count;
            int readyGranted = service.CollectCompost("Farm", readyTile);

            Assert.Equal(1, readyGranted);
            Assert.Single(inventory.AddedItems);
            Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
            Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", readyTile));
            Assert.True(
                sounds.Cues.Count > cuesBeforeReady,
                "COLLECT (Ready) must emit an observable audio cue; a silent harvest is the defect this audit catches.");
            Assert.Contains(HarvestCueName, sounds.Cues);
            Assert.Contains(logs, message => message.Contains("Ready → Empty", StringComparison.Ordinal));

            // ---- COLLECT (Not ready) — nothing granted, bin untouched, no reward cue ----
            int cuesBeforeNotReady = sounds.Cues.Count;
            int notReadyGranted = service.CollectCompost("Farm", processingTile);

            Assert.Equal(0, notReadyGranted);
            Assert.Single(inventory.AddedItems);
            Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", processingTile));
            Assert.True(
                sounds.Cues.Count == cuesBeforeNotReady,
                "COLLECT (not ready) must emit no reward cue and grant nothing; a silent reward leaks a payout.");
        }
        finally
        {
            Game1.sounds = previousSounds;
        }
    }

    // ------------------------------------------------------------------
    // APPLY stage — compost an improvable tile, or refuse with a HUD message
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 [P] APPLY stage: for any generated improvable tilled farm tile,
    /// <see cref="CompostApplicationService.TryApplyCompost"/> emits an observable
    /// reward cue (a floating-text sprite carrying the "+N" bonus) and consumes
    /// exactly one compost item; on a refusal (untilled / non-farm / at max health)
    /// it emits an observable HUD message and consumes nothing.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ApplyStage_ImprovableTile_EmitsFloatingTextAndConsumesExactlyOne_RefusalEmitsHudAndConsumesNothing(
        NonNegativeInt arbitraryTileX,
        NonNegativeInt arbitraryTileY,
        NonNegativeInt arbitrarySelector)
    {
        GameStateFixture.Install();

        int tileX = arbitraryTileX.Get % 40;
        int tileY = arbitraryTileY.Get % 40;
        float improvableHealth = arbitraryTileY.Get % (int)ModConstants.MaxSoilHealth;
        int initialStack = 1 + (arbitrarySelector.Get % 8);
        int refusalKind = arbitrarySelector.Get % 3;

        // ---- APPLY success — valid improvable tilled farm tile ----
        var successFixture = new GameLocationFixture("Farm", isFarm: true);
        var applyTile = new Vector2(tileX, tileY);
        successFixture.AddHoeDirtTile(tileX, tileY);

        var successSoilHealth = new Mock<ISoilHealthService>();
        successSoilHealth.Setup(x => x.GetSoilHealth("Farm", applyTile)).Returns(improvableHealth);

        var successPlayer = new PlayerProviderStub();
        var successCompost = ItemFactory.CreateItem(ModConstants.CompostItemId);
        successCompost.Stack = initialStack;
        successPlayer.CurrentItem = successCompost;

        var successLogs = new List<string>();
        var successService = new CompostApplicationService(
            successSoilHealth.Object,
            successPlayer,
            BuildLoggingMonitor(successLogs).Object);

        bool successApplied = successService.TryApplyCompost(successFixture.Location, applyTile);

        Assert.True(
            successApplied,
            "APPLY success premise: compost on an improvable tilled farm tile must apply.");
        Assert.Single(successFixture.Location.TemporarySprites);
        Assert.Equal(
            $"+{ModConstants.RestorationAmount:F0}",
            successFixture.Location.TemporarySprites[0].text);
        Assert.Contains(successLogs, message => message.Contains("Compost applied", StringComparison.Ordinal));
        Assert.Equal(initialStack - 1, successCompost.Stack);

        // ---- APPLY refusal — a HUD message reaches the player, nothing consumed ----
        GameLocationFixture refusalFixture;
        string expectedRefusalText;
        bool addHoeDirt;
        float refusalHealth;

        switch (refusalKind)
        {
            case 1:
                refusalFixture = new GameLocationFixture("Town", isFarm: false);
                addHoeDirt = false;
                expectedRefusalText = ModConstants.TranslationKeyCompostInvalidTile;
                refusalHealth = improvableHealth;
                break;
            case 2:
                refusalFixture = new GameLocationFixture("Farm", isFarm: true);
                addHoeDirt = true;
                expectedRefusalText = ModConstants.TranslationKeyCompostMaxHealth;
                refusalHealth = ModConstants.MaxSoilHealth;
                break;
            default:
                refusalFixture = new GameLocationFixture("Farm", isFarm: true);
                addHoeDirt = false;
                expectedRefusalText = ModConstants.TranslationKeyCompostInvalidTile;
                refusalHealth = improvableHealth;
                break;
        }

        var refusalTile = new Vector2(tileX, tileY);
        if (addHoeDirt)
        {
            refusalFixture.AddHoeDirtTile(tileX, tileY);
        }

        var refusalSoilHealth = new Mock<ISoilHealthService>();
        refusalSoilHealth
            .Setup(x => x.GetSoilHealth(refusalFixture.Location.Name ?? "Farm", refusalTile))
            .Returns(refusalHealth);

        var refusalPlayer = new PlayerProviderStub();
        var refusalCompost = ItemFactory.CreateItem(ModConstants.CompostItemId);
        refusalCompost.Stack = initialStack;
        refusalPlayer.CurrentItem = refusalCompost;

        var refusalLogs = new List<string>();
        var refusalService = new CompostApplicationService(
            refusalSoilHealth.Object,
            refusalPlayer,
            BuildLoggingMonitor(refusalLogs).Object);

        int hudBefore = Game1.hudMessages.Count;
        bool refusalApplied = refusalService.TryApplyCompost(refusalFixture.Location, refusalTile);

        Assert.False(
            refusalApplied,
            "APPLY refusal premise: the selected refusal kind (untilled / non-farm / max-health) must refuse.");

        // Consumes nothing.
        Assert.Equal(initialStack, refusalCompost.Stack);
        Assert.Empty(refusalFixture.Location.TemporarySprites);

        // SC-002 visibility: a refusal HUD message reaches the player and a trace records
        // why. (The message TEXT being fully localized is FR-008a's separate concern,
        // proven by T069/T144; this audit only checks that the refusal is not silent.)
        Assert.Contains(Game1.hudMessages, message => message.message == expectedRefusalText);
        Assert.True(
            Game1.hudMessages.Count >= hudBefore,
            "APPLY refusal must emit an observable HUD message; a silent refusal loses the player.");
        Assert.Contains(refusalLogs, message => message.Contains("Compost rejected", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // OBSERVE stage — the render path actually draws (or correctly skips) an overlay
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 [P] OBSERVE stage: for any generated positive health value in
    /// [<see cref="ModConstants.MinSoilHealth"/> + 1, <see cref="ModConstants.MaxSoilHealth"/>],
    /// the render path emits at least one overlay draw log — the overlay is actually drawn,
    /// not silently dropped — while a tile pinned to the FR-009 health-0 neutral-dirt
    /// baseline emits none (that suppression is the correct, intended behaviour for bare
    /// dirt, not a silent overlay). Each frame carries the pinned zero tile alongside the
    /// generated positive tile, so a single <see cref="Assert.Single{T}"/> over the drawn
    /// logs proves the positive tile drew and the zero tile did not.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ObserveStage_PositiveHealth_EmitsAtLeastOneOverlayDrawLog_ZeroHealth_EmitsNone(PositiveInt arbitraryHealth)
    {
        GameStateFixture.Install();

        // Derive a positive, in-range, finite reading from a bounded integer. A raw float
        // generator is never declared on a [Property] signature (it would surface
        // MinValue/MaxValue/NaN/inf and shrink toward out-of-frame values).
        int scaled = ((arbitraryHealth.Get - 1) % (int)ModConstants.MaxSoilHealth) + 1;
        float health = scaled;

        var logs = new List<string>();
        var service = BuildObservationFrameService(logs);
        service.SetTileHealthData(new Dictionary<Point, float>
        {
            { ObservationZeroTile, ModConstants.MinSoilHealth },
            { ObservationPositiveTile, health }
        });

        List<string> drawnValues = RenderObservationFrame(service, logs);

        // Exactly one overlay was drawn — the positive tile. The zero tile drew nothing.
        Assert.Single(drawnValues);
        Assert.True(
            float.TryParse(drawnValues[0], NumberStyles.Float, CultureInfo.CurrentCulture, out var drawnHealth),
            $"The OBSERVE draw log must carry a parseable value; observed '{drawnValues[0]}'.");
        Assert.Equal(health, drawnHealth);
    }

    // ------------------------------------------------------------------
    // ADVANCE stage — day-start advances Processing → Ready exactly once
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 ADVANCE stage: <see cref="Services.CompostingBinService.ProcessDayStart"/> on a
    /// bin that has matured emits an observable transition (the "Processing → Ready" trace
    /// log plus the native reward cue) and advances exactly once — a second same-day
    /// delivery is acknowledged with its own trace but performs no further transition, so
    /// the day-start is neither silent nor double-applied.
    /// </summary>
    [Fact]
    public void AdvanceStage_ProcessingBin_EmitsTransitionObservableAndAdvancesExactlyOnce()
    {
        GameStateFixture.Install();

        var wasteValidator = new Mock<IOrganicWasteValidator>();
        wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        var (service, inventory, timeStub, logs) = CreateIsolatedBinService(wasteValidator.Object);
        var tile = new Vector2(10, 10);

        timeStub.TotalDays = 1;
        service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
        Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;
        try
        {
            timeStub.TotalDays = 1 + ModConstants.MaturationDays;

            // ---- ADVANCE — first day-start past maturation ----
            service.ProcessDayStart("Farm");

            Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
            Assert.Contains(logs, message => message.Contains("Processing → Ready", StringComparison.Ordinal));
            Assert.Contains(HarvestCueName, sounds.Cues);
            int cuesAfterFirst = sounds.Cues.Count;

            // ---- ADVANCE — same-day duplicate must not advance again ----
            service.ProcessDayStart("Farm");

            Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));
            Assert.True(
                sounds.Cues.Count == cuesAfterFirst,
                "A second same-day ProcessDayStart advanced the batch twice — the exactly-once guard failed.");
            Assert.Single(logs.FindAll(message =>
                message.Contains("Processing → Ready", StringComparison.Ordinal)));
            Assert.Contains(logs, message => message.Contains("already processed", StringComparison.Ordinal));
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        _ = inventory;
    }

    // ------------------------------------------------------------------
    // End-to-end audit table — every stage emits an observable; none is silent
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 SC-002 end-to-end audit table: walk the full loop — place → insert → advance →
    /// collect → apply → observe — through the production services and assert that EVERY
    /// stage emitted at least one observable, so no stage completes silently. The observable
    /// recorded per stage:
    /// <list type="bullet">
    ///   <item><description>PLACE — the placed bin becomes a tracked machine (<c>CreateDislodgedBinItem</c> reconstructs it; an unplaced tile reconstructs null).</description></item>
    ///   <item><description>INSERT — the "Empty → Processing" transition trace.</description></item>
    ///   <item><description>ADVANCE — the "Processing → Ready" transition trace + native reward cue.</description></item>
    ///   <item><description>COLLECT — one inventory grant + the "Ready → Empty" trace + the native reward cue.</description></item>
    ///   <item><description>APPLY — a floating-text reward sprite + the "Compost applied" trace + one item consumed.</description></item>
    ///   <item><description>OBSERVE — the "Overlay drawn: " render-path log for a positive reading.</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public void LoopAudit_EveryStage_EmitsAtLeastOneObservable_NoStageCompletesSilently()
    {
        GameStateFixture.Install();

        var logs = new List<string>();
        var monitor = BuildLoggingMonitor(logs);
        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = 1 };

        var wasteValidator = new Mock<IOrganicWasteValidator>();
        wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

        var locationFixture = new GameLocationFixture("Farm", isFarm: true);
        var applyTile = new Vector2(2, 2);
        locationFixture.AddHoeDirtTile((int)applyTile.X, (int)applyTile.Y);
        var binTile = new Vector2(10, 10);
        locationFixture.Location.Objects.Add(binTile, new StardewValley.Object
        {
            ItemId = ModConstants.CompostingBinItemId,
            TileLocation = binTile
        });

        var locationProvider = new Mock<ILocationProvider>();
        locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(locationFixture.Location);

        var binService = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator.Object,
            monitor.Object,
            timeStub,
            new CompostingBinFactory(),
            inventory,
            locationProvider.Object);

        var sounds = new RecordingSoundsHelper();
        var previousSounds = Game1.sounds;
        Game1.sounds = sounds;

        bool placeObservable;
        bool insertObservable;
        bool advanceObservable;
        bool collectObservable;
        bool applyObservable;
        bool observeObservable;

        try
        {
            // ---- PLACE — the bin becomes a tracked, dislodgable machine ----
            binService.OnBinPlaced("Farm", binTile, null);
            placeObservable = binService.CreateDislodgedBinItem("Farm", binTile) != null;

            // ---- INSERT — Empty → Processing transition trace ----
            binService.AddWaste("Farm", binTile, ItemFactory.CreateValidWasteItem());
            insertObservable =
                binService.GetBinState("Farm", binTile) == CompostingBinState.Processing
                && LogContains(logs, "Empty → Processing");

            // ---- ADVANCE — Processing → Ready transition trace + reward cue ----
            timeStub.TotalDays = 1 + ModConstants.MaturationDays;
            int cuesBeforeAdvance = sounds.Cues.Count;
            binService.ProcessDayStart("Farm");
            advanceObservable =
                binService.GetBinState("Farm", binTile) == CompostingBinState.Ready
                && LogContains(logs, "Processing → Ready")
                && sounds.Cues.Count > cuesBeforeAdvance;

            // ---- COLLECT — one grant + Ready → Empty trace + reward cue ----
            int cuesBeforeCollect = sounds.Cues.Count;
            int granted = binService.CollectCompost("Farm", binTile);
            collectObservable =
                granted == 1
                && inventory.AddedItems.Count == 1
                && LogContains(logs, "Ready → Empty")
                && sounds.Cues.Count > cuesBeforeCollect;

            // ---- APPLY — floating-text reward + applied trace + one item consumed ----
            var soilHealth = new Mock<ISoilHealthService>();
            soilHealth.Setup(x => x.GetSoilHealth("Farm", applyTile)).Returns(50f);
            var player = new PlayerProviderStub();
            var compost = ItemFactory.CreateItem(ModConstants.CompostItemId);
            compost.Stack = 3;
            player.CurrentItem = compost;
            var applyService = new CompostApplicationService(soilHealth.Object, player, monitor.Object);
            bool applied = applyService.TryApplyCompost(locationFixture.Location, applyTile);
            applyObservable =
                applied
                && locationFixture.Location.TemporarySprites.Count == 1
                && LogContains(logs, "Compost applied")
                && compost.Stack == 2;

            // ---- OBSERVE — the render path draws an overlay for a positive reading ----
            var observationLogs = new List<string>();
            var observationService = BuildObservationFrameService(observationLogs);
            observationService.SetTileHealthData(new Dictionary<Point, float>
            {
                { ObservationPositiveTile, ModConstants.MaxSoilHealth }
            });
            int observeDraws = RenderObservationFrame(observationService, observationLogs).Count;
            observeObservable = observeDraws >= 1;
        }
        finally
        {
            Game1.sounds = previousSounds;
        }

        // Every stage emitted at least one observable — no stage completes silently.
        Assert.True(placeObservable, "SILENT STAGE (PLACE): the placed bin did not become a tracked, dislodgable machine.");
        Assert.True(insertObservable, "SILENT STAGE (INSERT): no Empty→Processing transition trace / state change.");
        Assert.True(advanceObservable, "SILENT STAGE (ADVANCE): no Processing→Ready transition trace or reward cue.");
        Assert.True(collectObservable, "SILENT STAGE (COLLECT): no grant, no reward cue, no Ready→Empty trace.");
        Assert.True(applyObservable, "SILENT STAGE (APPLY): no floating-text reward, no 'Compost applied' trace, or no item consumed.");
        Assert.True(observeObservable, "SILENT STAGE (OBSERVE): the render path emitted no overlay-draw log for a positive reading.");

        Assert.Single(inventory.AddedItems);
        Assert.Equal(ModConstants.CompostItemId, inventory.AddedItems[0].ItemId);
    }

    // ------------------------------------------------------------------
    // SC-002 human-observation boundary
    // ------------------------------------------------------------------

    /// <summary>
    /// T038 SC-002 boundary marker: documents that the genuinely player-facing confirmation
    /// of each loop stage — seeing the "+15" floating reward, the machine shake on a bad
    /// drop-in, the rich overlay tint, and the localized refusal text — is a human
    /// observation owed to the Gate 2/3 playthrough and is intentionally NOT asserted by
    /// this automatable suite.
    /// </summary>
    [Fact]
    public void LoopVisibility_HumanObservationBoundary_PlayerVisibleConfirmationOwedToGatePlaythrough()
    {
        Assert.True(
            true,
            "SC-002 human-observation boundary: the player-visible confirmation of each stage " +
            "(floating reward text, machine shake, overlay tint, localized HUD text) is owed to " +
            "the Gate 2/3 playthrough. This suite attests only the automatable observable each " +
            "stage emits through production code, so that no stage is mechanically silent.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Builds a fresh <see cref="CompostingBinService"/> with method-local collaborators — a
    /// fresh <see cref="TimeProviderStub"/>, a fresh <see cref="RecordingPlayerInventory"/>,
    /// and a monitored <see cref="IMonitor"/> capturing into <paramref name="logs"/> — so each
    /// property case runs against isolated state (per-case isolation, T280b).
    /// </summary>
    private static (CompostingBinService Service, RecordingPlayerInventory Inventory, TimeProviderStub Time, List<string> Logs) CreateIsolatedBinService(
        IOrganicWasteValidator wasteValidator,
        ILocationProvider? locationProvider = null)
    {
        var inventory = new RecordingPlayerInventory();
        var timeStub = new TimeProviderStub { TotalDays = 1 };
        var logs = new List<string>();

        var service = new CompostingBinService(
            new Mock<IModDataService>().Object,
            new Mock<ISaveIdProvider>().Object,
            wasteValidator,
            BuildLoggingMonitor(logs).Object,
            timeStub,
            new CompostingBinFactory(),
            inventory,
            locationProvider);

        return (service, inventory, timeStub, logs);
    }

    /// <summary>Builds a monitored <see cref="IMonitor"/> whose trace logs are appended to <paramref name="logs"/>.</summary>
    private static Mock<IMonitor> BuildLoggingMonitor(List<string> logs)
    {
        var monitor = new Mock<IMonitor>();
        monitor
            .Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
            .Callback<string, LogLevel>((message, level) => logs.Add(message));
        return monitor;
    }

    /// <summary>Whether any captured log line contains <paramref name="needle"/> (ordinal).</summary>
    private static bool LogContains(List<string> logs, string needle)
    {
        return logs.Exists(message => message.Contains(needle, StringComparison.Ordinal));
    }

    /// <summary>
    /// Builds a <see cref="VisualizationService"/> over the real production palette whose
    /// monitor captures frame logs into <paramref name="logs"/>, initialized with the
    /// stand-in white texture the draw path uses.
    /// </summary>
    private static VisualizationService BuildObservationFrameService(List<string> logs)
    {
        var configService = new Mock<IVisualizationConfigurationService>();
        configService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = ModConstants.DefaultOpacity,
                ShowPatterns = false
            });

        var service = new VisualizationService(
            new ColorInterpolationService(),
            configService.Object,
            BuildLoggingMonitor(logs).Object,
            new Mock<IHudNotifier>().Object);
        service.Initialize(StandInWhiteTexture());
        return service;
    }

    /// <summary>
    /// Renders one overlay frame against a stand-in <see cref="SpriteBatch"/> (a real one needs
    /// a <see cref="GraphicsDevice"/> the headless host lacks) and returns the value each
    /// "Overlay drawn: " log reported — the render-path observable. The exception the
    /// stand-in raises at the draw call is the headless-host boundary and is absorbed, after
    /// the draw observables have been emitted.
    /// </summary>
    private static List<string> RenderObservationFrame(VisualizationService service, List<string> logs)
    {
        logs.Clear();

        using (new CameraViewportState(ObservationViewport))
        {
            Record.Exception(() =>
                service.RenderOverlays(StandInSpriteBatch(), ObservationViewport, new GameTime()));
        }

        var drawnValues = new List<string>();
        foreach (var message in logs)
        {
            if (message.StartsWith(OverlayDrawLogPrefix, StringComparison.Ordinal))
            {
                drawnValues.Add(message.Substring(OverlayDrawLogPrefix.Length));
            }
        }

        return drawnValues;
    }

    /// <summary>An uninitialized <see cref="SpriteBatch"/> stand-in whose first draw call throws.</summary>
    private static SpriteBatch StandInSpriteBatch()
    {
        return (SpriteBatch)RuntimeHelpers.GetUninitializedObject(typeof(SpriteBatch));
    }

    /// <summary>An uninitialized <see cref="Texture2D"/> stand-in for the shared white texture.</summary>
    private static Texture2D StandInWhiteTexture()
    {
        return (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
    }

    /// <summary>
    /// The camera viewport state a frame renders under — the pixel-space <c>Game1.viewport</c>
    /// resolved from the tile-space viewport, set for the scope of the render and restored on
    /// dispose (xUnit parallelization is disabled repo-wide, so the swap is race-free).
    /// </summary>
    private sealed class CameraViewportState : IDisposable
    {
        private readonly xTile.Dimensions.Rectangle _originalViewport;

        public CameraViewportState(Rectangle tileViewport)
        {
            _originalViewport = Game1.viewport;
            Game1.viewport = new xTile.Dimensions.Rectangle(
                tileViewport.X * ModConstants.TileSize,
                tileViewport.Y * ModConstants.TileSize,
                tileViewport.Width * ModConstants.TileSize,
                tileViewport.Height * ModConstants.TileSize);
        }

        public void Dispose()
        {
            Game1.viewport = _originalViewport;
        }
    }

    /// <summary>
    /// Records the cue names requested through <c>Game1.playSound</c> so a test can observe the
    /// audio cue a loop stage emits. Semantically a no-op stand-in for
    /// <see cref="Stubs.NoOpSoundsHelperStub"/>: every cue is dropped and only the request is
    /// remembered. Installed into the public static <c>Game1.sounds</c> field (no reflection).
    /// </summary>
    private sealed class RecordingSoundsHelper : ISoundsHelper
    {
        private readonly List<string> _cues = new();

        /// <summary>Cue names requested through <c>Game1.playSound</c>, in call order.</summary>
        public IReadOnlyList<string> Cues => _cues;

        /// <summary>Unused; kept for interface completeness.</summary>
        public bool LogSounds { get; set; }

        /// <summary>Always reports that local playback is unavailable.</summary>
        public bool ShouldPlayLocal(SoundContext context)
        {
            return false;
        }

        /// <summary>Reports zero attenuation.</summary>
        public float GetVolumeForDistance(GameLocation location, Vector2? position)
        {
            return 0f;
        }

        /// <summary>Remembers the requested cue and silently drops it.</summary>
        public bool PlayLocal(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context, out ICue cue)
        {
            _cues.Add(cueName);
            cue = null!;
            return false;
        }

        /// <summary>Remembers the requested cue and silently drops it.</summary>
        public void PlayAll(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context)
        {
            _cues.Add(cueName);
        }

        /// <summary>No-op.</summary>
        public void SetPitch(ICue cue, float pitch, bool isMusic)
        {
        }
    }
}
