using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LivingRoots.Verification.Evidence
{
    public sealed class CropPhaseDump
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("dayKey")]
        public string DayKey { get; set; } = string.Empty;

        [JsonPropertyName("capturedAtUtc")]
        public string CapturedAtUtc { get; set; } = string.Empty;

        [JsonPropertyName("isMainPlayer")]
        public bool IsMainPlayer { get; set; }

        [JsonPropertyName("locationsProcessed")]
        public int LocationsProcessed { get; set; }

        [JsonPropertyName("elapsedMilliseconds")]
        public long ElapsedMilliseconds { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("crops")]
        public List<CropPhaseEntry> Crops { get; set; } = new List<CropPhaseEntry>();
    }

    public sealed class CropPhaseEntry
    {
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("tileX")]
        public int TileX { get; set; }

        [JsonPropertyName("tileY")]
        public int TileY { get; set; }

        [JsonPropertyName("tileKey")]
        public string TileKey { get; set; } = string.Empty;

        [JsonPropertyName("seedId")]
        public string SeedId { get; set; } = string.Empty;

        [JsonPropertyName("isForageCrop")]
        public bool IsForageCrop { get; set; }

        [JsonPropertyName("indexOfHarvest")]
        public int IndexOfHarvest { get; set; }

        [JsonPropertyName("currentPhase")]
        public int CurrentPhase { get; set; }

        [JsonPropertyName("dayOfCurrentPhase")]
        public int DayOfCurrentPhase { get; set; }

        [JsonPropertyName("phaseDays")]
        public List<int> PhaseDays { get; set; } = new List<int>();

        [JsonPropertyName("basePhaseDays")]
        public List<int> BasePhaseDays { get; set; } = new List<int>();

        [JsonPropertyName("fullyGrown")]
        public bool FullyGrown { get; set; }

        [JsonPropertyName("dead")]
        public bool Dead { get; set; }

        [JsonPropertyName("soilHealth")]
        public double? SoilHealth { get; set; }

        // Five-value contract (data-model.md): unknown, nonParticipant, poor,
        // moderate, healthy. `unknown` is for missing/NaN; `nonParticipant` is a
        // measured value <= 0 (the growth modifier skips it).
        [JsonPropertyName("healthTier")]
        public string HealthTier { get; set; } = "unknown";

        [JsonPropertyName("soilHealthState")]
        public string SoilHealthState { get; set; } = string.Empty;

        [JsonPropertyName("isSentinelPhase")]
        public bool IsSentinelPhase { get; set; }
    }
}
