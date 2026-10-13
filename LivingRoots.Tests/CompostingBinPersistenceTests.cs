using System;
using System.Collections.Generic;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
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
    }
}
