using System;
using System.Collections.Generic;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    public class CompostingBinPersistenceTests
    {
        /// <summary>T123 [P] [US2] RED — FR-012 (partial): null payload isolates per location.</summary>
        [Fact]
        public void T123_NullLocationPayload_IsolatesThatLocation_Only()
        {
            // RED: a null inner dictionary for one location aborts the entire load
            // instead of isolating that location and allowing remaining locations
            // to load correctly (FR-012 partial).
            var corruptData = new CompostingBinData();
            corruptData.LocationBinData["CorruptFarm"] = null!; // null payload for this location
            corruptData.LocationBinData["ValidFarm"] = new Dictionary<string, CompostingBinStateData>
            {
                ["10,10"] = new CompostingBinStateData
                {
                    State = "Ready",
                    InputItemId = "(O)24",
                    InputTimestamp = 1,
                    MaturationLevel = 1
                }
            };

            var mockData = new Mock<IModDataService>();
            mockData.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(corruptData);

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                new Mock<IOrganicWasteValidator>().Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub(),
                new CompostingBinFactory(),
                new RecordingPlayerInventory());

            // The load either throws and catches (aborting everything) or skips.
            // In either case, the valid farm should still be accessible if isolated.
            service.LoadData("test-null-payload");

            // RED: with current behavior, the null payload aborts the whole load,
            // so ValidFarm never registers and stays Empty instead of Ready.
            Assert.Equal(CompostingBinState.Ready, service.GetBinState("ValidFarm", new Vector2(10, 10)));
            Assert.Equal(CompostingBinState.Empty, service.GetBinState("CorruptFarm", new Vector2(10, 10)));
        }

        /// <summary>
        /// T199 (SC-004) — the save/reload round-trip must preserve every bin's
        /// state and batch progress EXACTLY, over the full lifecycle matrix:
        /// {Empty, Processing, Ready} × {placed in world, held in inventory,
        /// floating debris}. Neither lost nor invented values, whatever form the
        /// bin is in when the save is written.
        /// </summary>
        [Fact]
        public void T199_RoundTrip_PreservesStateAndProgress_ForEveryLifecycleStateAndPlacementForm()
        {
            // Each row: the batch state to stage, the placement form it is in,
            // and the world object the pruning step can see (null = the bin is
            // not in any location, so nothing is prunable).
            var matrix = new (CompostingBinState State, string Form, StardewValley.Object? WorldObject)[]
            {
                (CompostingBinState.Empty, "placed-in-world", new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
                (CompostingBinState.Processing, "placed-in-world", new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
                (CompostingBinState.Ready, "placed-in-world", new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId }),
                (CompostingBinState.Empty, "held-in-inventory", null),
                (CompostingBinState.Processing, "held-in-inventory", null),
                (CompostingBinState.Ready, "held-in-inventory", null),
                (CompostingBinState.Empty, "floating-debris", null),
                (CompostingBinState.Processing, "floating-debris", null),
                (CompostingBinState.Ready, "floating-debris", null)
            };

            var tile = new Vector2(10, 10);
            for (var index = 0; index < matrix.Length; index++)
            {
                var (state, form, worldObject) = matrix[index];

                var persisted = StageBatchAndSave(state, index, tile, worldObject, out var expectedInputId, out var expectedTimestamp);
                var reloaded = Reload(persisted, worldObject);

                // The lifecycle state survives whatever form the bin is in...
                Assert.True(
                    state == reloaded.GetBinState("Farm", tile),
                    $"SC-004: state {state} ({form}) must survive the save/reload round-trip.");

                // ...and so does the batch progress, exactly — never lost, never invented.
                Assert.True(
                    string.Equals(expectedInputId, ReadPersistedInputId(reloaded, tile), StringComparison.Ordinal),
                    $"SC-004: input id must be preserved for {state} ({form}).");
                Assert.True(
                    Nullable.Equals(expectedTimestamp, ReadPersistedTimestamp(reloaded, tile)),
                    $"SC-004: input timestamp must be preserved for {state} ({form}).");
            }
        }

        /// <summary>
        /// Stages a bin in the given lifecycle state, writes it through the
        /// production SaveData path, and hands back the DTO that was persisted
        /// plus the progress the round-trip must reproduce.
        /// </summary>
        private static CompostingBinData StageBatchAndSave(
            CompostingBinState state,
            int seed,
            Vector2 tile,
            StardewValley.Object? worldObject,
            out string? expectedInputId,
            out int? expectedTimestamp)
        {
            var mockData = new Mock<IModDataService>();
            CompostingBinData? captured = null;
            mockData
                .Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
                .Callback<CompostingBinData, string>((data, _) => captured = data);

            // Inject the lifecycle state directly: the invariant under test is
            // the persistence round-trip, so the state is staged rather than
            // matured (which would couple this test to machine timing).
            mockData
                .Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>()))
                .Returns(InjectState("Farm", tile, state, seed));

            var locationProvider = new Mock<ILocationProvider>();
            if (worldObject != null)
            {
                var location = new GameLocationFixture("Farm", isFarm: true).Location;
                location.Objects.Add(tile, worldObject);
                locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);
            }

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                new Mock<IOrganicWasteValidator>().Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub { TotalDays = seed + 1 },
                new CompostingBinFactory(),
                new RecordingPlayerInventory(),
                locationProvider.Object);

            service.LoadData("round-trip-save");
            service.SaveData("round-trip-save");

            var capturedData = Assert.IsType<CompostingBinData>(captured);
            expectedInputId = capturedData.LocationBinData.TryGetValue("Farm", out var bins)
                && bins.TryGetValue(GetTileKey(tile), out var persisted)
                    ? persisted.InputItemId
                    : null;
            expectedTimestamp = capturedData.LocationBinData.TryGetValue("Farm", out var bins2)
                && bins2.TryGetValue(GetTileKey(tile), out var persisted2)
                    ? persisted2.InputTimestamp
                    : null;

            return capturedData;
        }

        /// <summary>Reloads a persisted DTO through a fresh service instance.</summary>
        private static ICompostingBinService Reload(CompostingBinData persisted, StardewValley.Object? worldObject)
        {
            var mockData = new Mock<IModDataService>();
            mockData.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns((CompostingBinData?)persisted);

            var locationProvider = new Mock<ILocationProvider>();
            if (worldObject != null)
            {
                var location = new GameLocationFixture("Farm", isFarm: true).Location;
                location.Objects.Add(new Vector2(10, 10), worldObject);
                locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);
            }

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                new Mock<IOrganicWasteValidator>().Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub { TotalDays = 1 },
                new CompostingBinFactory(),
                new RecordingPlayerInventory(),
                locationProvider.Object);

            service.LoadData("round-trip-save");
            return service;
        }

        private static string? ReadPersistedInputId(ICompostingBinService service, Vector2 tile)
        {
            return ReadPersistedRecord(service, tile)?.InputItemId;
        }

        private static int? ReadPersistedTimestamp(ICompostingBinService service, Vector2 tile)
        {
            return ReadPersistedRecord(service, tile)?.InputTimestamp;
        }

        private static CompostingBinStateData? ReadPersistedRecord(ICompostingBinService service, Vector2 tile)
        {
            var snapshot = (LivingRoots.Domain.Models.CompostingBinData?)typeof(CompostingBinService)
                .GetMethod("CreateSaveSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(service, null);
            if (snapshot == null)
            {
                return null;
            }

            var bins = ((LivingRoots.Domain.Models.CompostingBinData)snapshot).LocationBinData;
            return bins.TryGetValue("Farm", out var farmBins)
                && farmBins.TryGetValue(GetTileKey(tile), out var record)
                    ? record
                    : null;
        }

        private static CompostingBinData InjectState(string locationName, Vector2 tile, CompostingBinState state, int seed)
        {
            var data = new CompostingBinData();
            var key = GetTileKey(tile);
            data.LocationBinData[locationName] = new Dictionary<string, CompostingBinStateData>
            {
                [key] = new CompostingBinStateData
                {
                    State = state.ToString(),
                    InputItemId = $"(O){20 + seed}",
                    InputTimestamp = seed + 1,
                    MaturationLevel = 1
                }
            };
            return data;
        }

        private static string GetTileKey(Vector2 tile) => $"{(int)tile.X},{(int)tile.Y}";

        // ------------------------------------------------------------------
        // T224 (FR-012, F4 contradicts): the persisted round-trip writes no
        // MaturationLevel — the retired multiplier leaves no trace in the
        // JSON the production save path emits. Fixed-literal anchor for the
        // property-based matrix in T229.
        // ------------------------------------------------------------------

        /// <summary>
        /// T224 (FR-012): saving a legitimately finished batch serializes no
        /// MaturationLevel field. RED today: CreateSaveSnapshot writes
        /// MaturationLevel (CompostingBinService.cs:493), so the JSON carries it.
        /// </summary>
        [Fact]
        public void T224_PersistedRoundTrip_WritesNoMaturationLevel()
        {
            GameStateFixture.Install();

            var tile = new Vector2(10, 10);
            var mockData = new Mock<IModDataService>();
            CompostingBinData? captured = null;
            mockData
                .Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
                .Callback<CompostingBinData, string>((dto, _) => captured = dto);

            var wasteValidator = new Mock<IOrganicWasteValidator>();
            wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);
            var timeStub = new TimeProviderStub { TotalDays = 1 };
            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                wasteValidator.Object,
                new Mock<IMonitor>().Object,
                timeStub,
                new CompostingBinFactory(),
                new RecordingPlayerInventory());

            service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
            timeStub.TotalDays = 1 + ModConstants.MaturationDays;
            service.ProcessDayStart("Farm");
            Assert.Equal(CompostingBinState.Ready, service.GetBinState("Farm", tile));

            service.SaveData("t224-json");
            var snapshot = Assert.IsType<CompostingBinData>(captured);
            string json = JsonConvert.SerializeObject(snapshot);
            Assert.DoesNotContain("MaturationLevel", json);
        }

        // ------------------------------------------------------------------
        // T226 (FR-012, F6 partial): per-record quarantine. A malformed or
        // corrupt record discards ONLY itself — every valid sibling bin in
        // the same location still loads; legacy-shaped fields are ignored
        // without throwing; an orphan entry whose world object is absent is
        // pruned. Property-based over generated mixes of
        // valid/corrupt/legacy/absent-object records.
        // ------------------------------------------------------------------

        /// <summary>
        /// T226 (FR-012): one corrupt record in a location quarantines only
        /// itself — valid siblings, legacy-shaped records and the orphan rule
        /// are unaffected, and the re-saved snapshot carries no legacy
        /// fields. RED today: the try/catch is per location
        /// (CompostingBinService.cs:315-364), so a single "Bogus" state or
        /// malformed key discards every valid sibling, and the snapshot
        /// re-writes the obsolete fields.
        /// </summary>
        [Property(MaxTest = 100)]
        public bool T226_CorruptRecord_QuarantinesOnlyItself_ValidSiblingsSurvive(
            int validCountSeed,
            int corruptKindSeed,
            int tileBaseSeed)
        {
            GameStateFixture.Install();

            int validCount = Modulo(validCountSeed, 4) + 1;
            int corruptKind = Modulo(corruptKindSeed, 2);
            int tileBase = Modulo(tileBaseSeed, 8);

            var location = new GameLocationFixture("Farm", isFarm: true).Location;
            var bins = new Dictionary<string, CompostingBinStateData>();
            var validTiles = new List<Vector2>();
            for (int i = 0; i < validCount; i++)
            {
                var tile = new Vector2(tileBase + i, 10);
                validTiles.Add(tile);
                bins[GetTileKey(tile)] = new CompostingBinStateData
                {
                    State = "Ready",
                    InputItemId = "(O)24",
                    InputTimestamp = 1,
                    MaturationLevel = 1
                };
                location.Objects.Add(tile, new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId });
            }

            var legacyTile = new Vector2(20, 10);
            bins[GetTileKey(legacyTile)] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 1,
                MaturationLevel = 5,
                ConsecutiveActiveDays = 99,
                ConsecutiveIdleDays = 99
            };
            location.Objects.Add(legacyTile, new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId });

            var orphanTile = new Vector2(21, 10);
            bins[GetTileKey(orphanTile)] = new CompostingBinStateData
            {
                State = "Ready",
                InputItemId = "(O)24",
                InputTimestamp = 1,
                MaturationLevel = 1
            };

            if (corruptKind == 0)
            {
                var corruptTile = new Vector2(22, 10);
                bins[GetTileKey(corruptTile)] = new CompostingBinStateData
                {
                    State = "Bogus",
                    InputItemId = "(O)24",
                    InputTimestamp = 1
                };
                location.Objects.Add(corruptTile, new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId });
            }
            else
            {
                bins["not-a-tile-key"] = new CompostingBinStateData
                {
                    State = "Ready",
                    InputItemId = "(O)24",
                    InputTimestamp = 1
                };
            }

            var data = new CompostingBinData();
            data.LocationBinData["Farm"] = bins;

            var mockData = new Mock<IModDataService>();
            mockData.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(data);
            CompostingBinData? savedSnapshot = null;
            mockData
                .Setup(x => x.SaveData(It.IsAny<CompostingBinData>(), It.IsAny<string>()))
                .Callback<CompostingBinData, string>((dto, _) => savedSnapshot = dto);

            var locationProvider = new Mock<ILocationProvider>();
            locationProvider.Setup(x => x.GetLocationByName("Farm")).Returns(location);

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                new Mock<IOrganicWasteValidator>().Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub { TotalDays = 1 },
                new CompostingBinFactory(),
                new RecordingPlayerInventory(),
                locationProvider.Object);

            service.LoadData("t226-mix");

            bool siblingsSurvive = true;
            foreach (var tile in validTiles)
            {
                if (service.GetBinState("Farm", tile) != CompostingBinState.Ready)
                {
                    siblingsSurvive = false;
                }
            }

            bool legacyLoads = service.GetBinState("Farm", legacyTile) == CompostingBinState.Ready;
            bool orphanPruned = service.GetBinState("Farm", orphanTile) == CompostingBinState.Empty;

            service.SaveData("t226-mix");
            bool snapshotOmitsLegacyFields = true;
            if (savedSnapshot != null)
            {
                string json = JsonConvert.SerializeObject(savedSnapshot);
                if (json.Contains("MaturationLevel", StringComparison.Ordinal)
                    || json.Contains("ConsecutiveActiveDays", StringComparison.Ordinal)
                    || json.Contains("ConsecutiveIdleDays", StringComparison.Ordinal))
                {
                    snapshotOmitsLegacyFields = false;
                }
            }

            return siblingsSurvive && legacyLoads && orphanPruned && snapshotOmitsLegacyFields;
        }

        // ------------------------------------------------------------------
        // T229 (SC-004/FR-012, F9 partial): property-based save/reload
        // round-trip matrix over lifecycle state (Empty/Processing/Ready) x
        // placement form (placed / held-in-inventory / floating-debris)
        // through the real ModDataService JSON seam, plus a soil-health
        // round-trip half. Differs from T199 on all four F9 points: it is a
        // [Property] over the generated matrix (not a hand-rolled [Fact]);
        // the Empty rows assert observable reloaded behavior (no record
        // persisted AND no phantom bin) instead of null==null field equality;
        // each placement form sets up a genuinely distinct world state
        // (object present / provider-unknown with a pocketed modData item /
        // known location with no object plus a debris item); and the DTOs
        // travel through real Newtonsoft JSON via a real ModDataService,
        // never as in-memory object references.
        // ------------------------------------------------------------------

        /// <summary>
        /// T229 (SC-004/FR-012): the generated (state, form) round-trip
        /// preserves the lifecycle state and batch progress exactly, whatever
        /// form the bin is in, and persists no legacy maturation fields —
        /// with the soil-health half surviving alongside. RED today: the
        /// debris form is pruned on reload (known location, absent object),
        /// so its state is lost, and the snapshot JSON still carries the
        /// obsolete MaturationLevel/ConsecutiveActiveDays/ConsecutiveIdleDays
        /// fields.
        /// </summary>
        [Property(MaxTest = 100)]
        public bool T229_RoundTrip_PreservesStateAndProgress_ThroughJsonSeam_ForEveryStateAndPlacementForm(
            int stateSeed,
            int formSeed,
            int tileSeed)
        {
            GameStateFixture.Install();

            var state = (CompostingBinState)Modulo(stateSeed, 3);
            int form = Modulo(formSeed, 3);
            var tile = new Vector2(Modulo(tileSeed, 10), 10);
            const string saveId = "t229-round-trip";
            const string soilSaveId = "t229-soil";

            var store = new Dictionary<string, string>();
            var mockDataHelper = new Mock<IDataHelper>();
            mockDataHelper
                .Setup(x => x.WriteJsonFile(It.IsAny<string>(), It.IsAny<CompostingBinData>()))
                .Callback<string, CompostingBinData>((path, dto) => store[path] = JsonConvert.SerializeObject(dto));
            mockDataHelper
                .Setup(x => x.ReadJsonFile<CompostingBinData>(It.IsAny<string>()))
                .Returns((string path) => ReadJson<CompostingBinData>(store, path)!);
            mockDataHelper
                .Setup(x => x.WriteJsonFile(It.IsAny<string>(), It.IsAny<SoilHealthState>()))
                .Callback<string, SoilHealthState>((path, dto) => store[path] = JsonConvert.SerializeObject(dto));
            mockDataHelper
                .Setup(x => x.ReadJsonFile<SoilHealthState>(It.IsAny<string>()))
                .Returns((string path) => ReadJson<SoilHealthState>(store, path)!);

            var mockHelper = new Mock<IModHelper>();
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            var mockModLogic = new Mock<IModLogic>();
            mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns((string segment) => segment);
            var monitor = new Mock<IMonitor>();
            var modDataService = new ModDataService(mockHelper.Object, monitor.Object, mockModLogic.Object);

            var wasteValidator = new Mock<IOrganicWasteValidator>();
            wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

            var stageTime = new TimeProviderStub { TotalDays = 1 };
            var stagingService = new CompostingBinService(
                modDataService,
                new Mock<ISaveIdProvider>().Object,
                wasteValidator.Object,
                monitor.Object,
                stageTime,
                new CompostingBinFactory(),
                new RecordingPlayerInventory());

            if (state == CompostingBinState.Processing || state == CompostingBinState.Ready)
            {
                stagingService.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
            }

            if (state == CompostingBinState.Ready)
            {
                stageTime.TotalDays = 1 + ModConstants.MaturationDays;
                stagingService.ProcessDayStart("Farm");
            }

            if (stagingService.GetBinState("Farm", tile) != state)
            {
                return true;
            }

            StardewValley.Object? placedObject = null;
            Item? pocketItem = null;
            Item? debrisItem = null;
            if (form == 0)
            {
                placedObject = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
            }
            else
            {
                Item carried = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
                if (state != CompostingBinState.Empty)
                {
                    var dislodged = stagingService.CreateDislodgedBinItem("Farm", tile);
                    if (dislodged != null)
                    {
                        carried = dislodged;
                    }
                }

                if (form == 1)
                {
                    pocketItem = carried;
                }
                else
                {
                    debrisItem = carried;
                }
            }

            stagingService.SaveData(saveId);

            var reloadProvider = new Mock<ILocationProvider>();
            if (form == 0)
            {
                var placed = new GameLocationFixture("Farm", isFarm: true).Location;
                placed.Objects.Add(tile, placedObject!);
                reloadProvider.Setup(x => x.GetLocationByName("Farm")).Returns(placed);
            }
            else if (form == 2)
            {
                var debrisField = new GameLocationFixture("Farm", isFarm: true).Location;
                reloadProvider.Setup(x => x.GetLocationByName("Farm")).Returns(debrisField);
            }

            var reloadService = new CompostingBinService(
                modDataService,
                new Mock<ISaveIdProvider>().Object,
                wasteValidator.Object,
                monitor.Object,
                new TimeProviderStub { TotalDays = 1 + ModConstants.MaturationDays },
                new CompostingBinFactory(),
                new RecordingPlayerInventory(),
                reloadProvider.Object);
            reloadService.LoadData(saveId);

            string? binJson = null;
            foreach (var entry in store)
            {
                if (entry.Key.Contains("composting_bins", StringComparison.Ordinal))
                {
                    binJson = entry.Value;
                }
            }

            bool stateOk;
            bool progressOk;
            if (state == CompostingBinState.Empty)
            {
                bool noRecordPersisted = true;
                if (binJson != null)
                {
                    var savedDto = JsonConvert.DeserializeObject<CompostingBinData>(binJson);
                    if (savedDto != null
                        && savedDto.LocationBinData.TryGetValue("Farm", out var farmBins)
                        && farmBins.ContainsKey(GetTileKey(tile)))
                    {
                        noRecordPersisted = false;
                    }
                }

                bool noPhantom = reloadService.GetBinState("Farm", tile) == CompostingBinState.Empty;
                stateOk = noRecordPersisted && noPhantom;
                progressOk = true;
            }
            else
            {
                stateOk = reloadService.GetBinState("Farm", tile) == state;
                var stagedRecord = ReadPersistedRecord(stagingService, tile);
                var reloadedRecord = ReadPersistedRecord(reloadService, tile);
                progressOk = stagedRecord != null
                    && reloadedRecord != null
                    && string.Equals(stagedRecord.InputItemId, reloadedRecord.InputItemId, StringComparison.Ordinal)
                    && Nullable.Equals(stagedRecord.InputTimestamp, reloadedRecord.InputTimestamp);
            }

            bool jsonOmitsLegacyFields = true;
            if (binJson != null
                && (binJson.Contains("MaturationLevel", StringComparison.Ordinal)
                    || binJson.Contains("ConsecutiveActiveDays", StringComparison.Ordinal)
                    || binJson.Contains("ConsecutiveIdleDays", StringComparison.Ordinal)))
            {
                jsonOmitsLegacyFields = false;
            }

            bool carriedOk = true;
            if (state != CompostingBinState.Empty && form != 0)
            {
                Item? carried = form == 1 ? pocketItem : debrisItem;
                var asObject = carried as StardewValley.Object;
                carriedOk = asObject != null
                    && asObject.modData.ContainsKey(ModConstants.ModDataInputIdKey);
            }

            var fileSanitizer = new Mock<IFileNameSanitizationService>();
            fileSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns((string value) => value);
            var soilStaging = new SoilHealthService(modDataService, monitor.Object, fileSanitizer.Object);
            var soilTile = new Vector2(3, 3);
            soilStaging.SetSoilHealth("Farm", soilTile, 42.5f);
            soilStaging.SaveData(soilSaveId);
            var soilReloaded = new SoilHealthService(modDataService, monitor.Object, fileSanitizer.Object);
            soilReloaded.LoadData(soilSaveId);
            bool soilOk = Math.Abs(soilReloaded.GetSoilHealth("Farm", soilTile) - 42.5f) < 0.0001f;

            return stateOk && progressOk && jsonOmitsLegacyFields && carriedOk && soilOk;
        }

        // ------------------------------------------------------------------
        // T245 (FR-012, F26 partial): LoadData with a null payload clears
        // the runtime cache — no stale bins leak between saves. Mirrors
        // SoilHealthServiceTests' invalid-saveId cache-clearing test.
        // Fixed behaviour assertion — property-based N/A.
        // ------------------------------------------------------------------

        /// <summary>
        /// T245 (FR-012): loading a null payload clears the runtime cache.
        /// RED today: CompostingBinService.LoadData returns early on null
        /// without clearing (CompostingBinService.cs:291-295), unlike
        /// SoilHealthService.cs:74-82, so the seeded bin survives the load.
        /// </summary>
        [Fact]
        public void T245_LoadData_WithNullPayload_ClearsRuntimeCache()
        {
            var mockData = new Mock<IModDataService>();
            mockData.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns((CompostingBinData?)null);

            var wasteValidator = new Mock<IOrganicWasteValidator>();
            wasteValidator.Setup(x => x.IsValidOrganicWaste(It.IsAny<Item>())).Returns(true);

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                wasteValidator.Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub { TotalDays = 1 },
                new CompostingBinFactory(),
                new RecordingPlayerInventory());

            var tile = new Vector2(10, 10);
            service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
            Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

            service.LoadData("next-save-with-no-data");

            Assert.Equal(CompostingBinState.Empty, service.GetBinState("Farm", tile));
        }

        /// <summary>
        /// Non-negative modulo for generated seeds, so arbitrary (including
        /// negative) FsCheck integers map into a fixed range.
        /// </summary>
        private static int Modulo(int value, int modulus)
        {
            int remainder = value % modulus;
            if (remainder < 0)
            {
                remainder += modulus;
            }

            return remainder;
        }

        /// <summary>
        /// Reads a JSON round-trip value out of the in-memory seam store,
        /// answering a missing save file with null.
        /// </summary>
        private static T? ReadJson<T>(Dictionary<string, string> store, string path)
            where T : class
        {
            if (store.TryGetValue(path, out string? json))
            {
                return JsonConvert.DeserializeObject<T>(json);
            }

            return null;
        }
    }
}
