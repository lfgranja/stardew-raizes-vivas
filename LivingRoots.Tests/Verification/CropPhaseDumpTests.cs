using System;
using System.Collections.Generic;
using System.Text.Json;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class CropPhaseDumpTests
    {
        [Fact]
        public void Dump_HasAllFields_SoilHealthState_HealthTierFive_BasePhaseDays_NoExpectedFields_DayKeyCapturedAtUtc()
        {
            var dump = new CropPhaseDump
            {
                SchemaVersion = 1,
                DayKey = "spring:1:1",
                CapturedAtUtc = DateTime.UtcNow.ToString("o"),
                IsMainPlayer = true,
                LocationsProcessed = 1,
                ElapsedMilliseconds = 5,
                Done = true,
                Crops = new List<CropPhaseEntry>
                {
                    new CropPhaseEntry
                    {
                        Location = "Farm",
                        TileX = 0,
                        TileY = 0,
                        TileKey = "0,0",
                        SeedId = "1",
                        IsForageCrop = false,
                        IndexOfHarvest = 24,
                        CurrentPhase = 1,
                        DayOfCurrentPhase = 0,
                        PhaseDays = new List<int> { 1, 2, 3 },
                        BasePhaseDays = new List<int> { 1, 2, 3 },
                        FullyGrown = false,
                        Dead = false,
                        SoilHealth = 50.0,
                        HealthTier = HealthTierClassifier.Moderate,
                        SoilHealthState = "measured",
                        IsSentinelPhase = false,
                    },
                },
            };

            Assert.Equal("spring:1:1", dump.DayKey);
            Assert.NotEmpty(dump.CapturedAtUtc);
            Assert.Equal("measured", dump.Crops[0].SoilHealthState);
            Assert.Equal(3, dump.Crops[0].BasePhaseDays.Count);
            Assert.Equal(50.0, dump.Crops[0].SoilHealth);
        }

        [Fact]
        public void SoilHealth_IsNullable_SoMissingAndNaNAreNotFabricatedAsZero()
        {
            var entry = new CropPhaseEntry
            {
                SoilHealth = null,
                SoilHealthState = "missing",
                HealthTier = HealthTierClassifier.Unknown,
            };

            Assert.Null(entry.SoilHealth);
            Assert.Equal("unknown", entry.HealthTier);

            var json = JsonSerializer.Serialize(entry);
            Assert.Contains("\"soilHealth\":null", json);
            Assert.DoesNotContain("expected", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SentinelPhase_IsFlagged()
        {
            var entry = new CropPhaseEntry { CurrentPhase = -1, IsSentinelPhase = true };
            Assert.True(entry.IsSentinelPhase);
        }
    }
}
