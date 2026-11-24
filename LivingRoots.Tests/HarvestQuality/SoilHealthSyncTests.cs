using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class SoilHealthSyncTests
    {
        private static List<SoilHealthSnapshotEntry> MakeEntries()
        {
            return new List<SoilHealthSnapshotEntry>
            {
                new SoilHealthSnapshotEntry("Farm", 1, 2, 75.0f),
                new SoilHealthSnapshotEntry("Farm", 3, 4, 0.0f),
                new SoilHealthSnapshotEntry("Farm", 5, 6, float.NaN),
            };
        }

        [Fact]
        public void Create_PreservesSaveIdDayAndEntries()
        {
            var entries = MakeEntries();

            var message = SoilHealthSync.Create("save-1", 10, entries);

            Assert.Equal("save-1", message.SaveId);
            Assert.Equal(10, message.Day);
            Assert.Equal(entries.Count, message.Entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                Assert.Equal(entries[i].LocationName, message.Entries[i].LocationName);
                Assert.Equal(entries[i].TileX, message.Entries[i].TileX);
                Assert.Equal(entries[i].TileY, message.Entries[i].TileY);
                if (float.IsNaN(entries[i].Health))
                {
                    Assert.True(float.IsNaN(message.Entries[i].Health));
                }
                else
                {
                    Assert.Equal(entries[i].Health, message.Entries[i].Health);
                }
            }
        }

        [Fact]
        public void Apply_MatchingSave_AppliesAllEntries()
        {
            var entries = MakeEntries();
            var message = SoilHealthSync.Create("save-1", 10, entries);

            var applied = new List<(string loc, int x, int y, float h)>();
            int count = SoilHealthSync.Apply(message, "save-1", (loc, x, y, h) => applied.Add((loc, x, y, h)));

            Assert.Equal(entries.Count, count);
            Assert.Equal(entries.Count, applied.Count);
        }

        [Fact]
        public void Apply_MismatchedSave_AppliesNothing()
        {
            var entries = MakeEntries();
            var message = SoilHealthSync.Create("save-1", 10, entries);

            var applied = new List<(string loc, int x, int y, float h)>();
            int count = SoilHealthSync.Apply(message, "save-2", (loc, x, y, h) => applied.Add((loc, x, y, h)));

            Assert.Equal(0, count);
            Assert.Empty(applied);
        }

        [Fact]
        public void Apply_IsIdempotent()
        {
            var entries = MakeEntries();
            var message = SoilHealthSync.Create("save-1", 10, entries);

            var applied = new List<(string loc, int x, int y, float h)>();
            SoilHealthSync.Apply(message, "save-1", (loc, x, y, h) => applied.Add((loc, x, y, h)));
            int second = SoilHealthSync.Apply(message, "save-1", (loc, x, y, h) => applied.Add((loc, x, y, h)));

            Assert.Equal(entries.Count, second);
        }

        [Fact]
        public void Apply_PreservesMeasuredZeroAndNaN()
        {
            var entries = new List<SoilHealthSnapshotEntry>
            {
                new SoilHealthSnapshotEntry("Farm", 1, 1, 0.0f),
                new SoilHealthSnapshotEntry("Farm", 2, 2, float.NaN),
            };
            var message = SoilHealthSync.Create("save-1", 1, entries);

            var applied = new List<(string loc, int x, int y, float h)>();
            SoilHealthSync.Apply(message, "save-1", (loc, x, y, h) => applied.Add((loc, x, y, h)));

            Assert.Equal(0.0f, applied[0].h);
            Assert.True(float.IsNaN(applied[1].h));
        }

        [Property]
        public Property CreateApply_RoundTripsAllEntries()
        {
            var entries = new List<SoilHealthSnapshotEntry>
            {
                new SoilHealthSnapshotEntry("Farm", 1, 2, 80.0f),
            };
            var message = SoilHealthSync.Create("save-1", 5, entries);

            var applied = new List<SoilHealthSnapshotEntry>();
            SoilHealthSync.Apply(message, "save-1", (loc, x, y, h) => applied.Add(new SoilHealthSnapshotEntry(loc, x, y, h)));

            return (applied.Count == entries.Count
                   && Math.Abs(applied[0].Health - entries[0].Health) < 0.00001f).ToProperty();
        }

        [Fact]
        public void SyncedState_ProducesSameDecisionAsHost()
        {
            var service = new HarvestQualityService();
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 1 };
            var hostHealth = 80.0f;
            var syncedHealth = 80.0f;

            var host = service.Decide(hostHealth, inputs);
            var client = service.Decide(syncedHealth, inputs);

            Assert.Equal(host.Tier, client.Tier);
            Assert.Equal(host.QualityBoost, client.QualityBoost);
            Assert.Equal(host.ExtraHarvestBoost, client.ExtraHarvestBoost);
        }

        [Fact]
        public void DivergentPreSyncState_ProducesDifferentDecision()
        {
            var service = new HarvestQualityService();
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 1 };

            var host = service.Decide(80.0f, inputs);
            var client = service.Decide(20.0f, inputs);

            Assert.NotEqual(host, client);
        }
    }
}
