using System.Collections.Generic;
using System.Text.Json;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class HarvestMatrixDumpTests
    {
        [Fact]
        public void CropPhaseDump_SerializesHarvestRecords_WithStableFieldNames()
        {
            var dump = new CropPhaseDump
            {
                SchemaVersion = 1,
                DayKey = "spring_1",
                CapturedAtUtc = "2026-11-05T00:00:00Z",
                IsMainPlayer = true,
                LocationsProcessed = 1,
                ElapsedMilliseconds = 3,
                Done = true,
                Harvests = new List<HarvestRecord>
                {
                    new HarvestRecord
                    {
                        Location = "Farm",
                        TileX = 60,
                        TileY = 15,
                        TileKey = "60,15",
                        Probe = HarvestMatrixPlanner.HealthyQualityProbe,
                        SoilHealth = 90,
                        HealthTier = "healthy",
                        QualityRoll = 0.25,
                        ExtraRoll = 0.50,
                        VanillaQuality = 0,
                        MainItemId = "24",
                        MainQuality = 1,
                        ExtraCount = 0,
                        ExtraQuality = null,
                        Harvested = true,
                    },
                },
            };

            var json = JsonSerializer.Serialize(dump);
            using var document = JsonDocument.Parse(json);
            var harvest = document.RootElement.GetProperty("harvests")[0];
            Assert.Equal("Farm", harvest.GetProperty("location").GetString());
            Assert.Equal(60, harvest.GetProperty("tileX").GetInt32());
            Assert.Equal(15, harvest.GetProperty("tileY").GetInt32());
            Assert.Equal("60,15", harvest.GetProperty("tileKey").GetString());
            Assert.Equal("healthy-quality", harvest.GetProperty("probe").GetString());
            Assert.Equal(90, harvest.GetProperty("soilHealth").GetDouble());
            Assert.Equal("healthy", harvest.GetProperty("healthTier").GetString());
            Assert.Equal(0.25, harvest.GetProperty("qualityRoll").GetDouble());
            Assert.Equal(0.50, harvest.GetProperty("extraRoll").GetDouble());
            Assert.Equal(0, harvest.GetProperty("vanillaQuality").GetInt32());
            Assert.Equal("24", harvest.GetProperty("mainItemId").GetString());
            Assert.Equal(1, harvest.GetProperty("mainQuality").GetInt32());
            Assert.Equal(0, harvest.GetProperty("extraCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, harvest.GetProperty("extraQuality").ValueKind);
            Assert.True(harvest.GetProperty("harvested").GetBoolean());

            var roundTrip = JsonSerializer.Deserialize<CropPhaseDump>(json);
            Assert.NotNull(roundTrip);
            Assert.Single(roundTrip!.Harvests);
            Assert.Equal(HarvestMatrixPlanner.HealthyQualityProbe, roundTrip.Harvests[0].Probe);
            Assert.Null(roundTrip.Harvests[0].ExtraQuality);
        }

        [Fact]
        public void CropPhaseDump_DefaultsToAnEmptyHarvestList()
        {
            var dump = new CropPhaseDump();
            Assert.NotNull(dump.Harvests);
            Assert.Empty(dump.Harvests);
        }
    }
}
