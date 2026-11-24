using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LivingRoots.Domain;

namespace LivingRoots.Tests.Verification
{
    /// <summary>
    /// One entry in the save comparator diff output.
    /// </summary>
    public sealed class SaveComparatorEntry
    {
        public string Location { get; set; } = string.Empty;
        public string Tile { get; set; } = string.Empty;
        public float Before { get; set; }
        public float After { get; set; }
    }

    /// <summary>
    /// Result of comparing two <c>modData/soil_health_data_{saveId}.json</c> files.
    /// </summary>
    public sealed class SaveComparatorResult
    {
        public List<SaveComparatorEntry> Added { get; } = new();
        public List<SaveComparatorEntry> Removed { get; } = new();
        public List<SaveComparatorEntry> Changed { get; } = new();

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    /// <summary>
    /// C# test utility that diffs two soil-health save files.
    /// Keyed by (LocationName, "X,Y"). Float tolerance 0.0001f.
    /// Missing keys (sparse cache) treated as 0. NaN tiles ignored.
    /// </summary>
    public static class SaveComparator
    {
        public const float Tolerance = 0.0001f;

        public static SaveComparatorResult Compare(string beforePath, string afterPath)
        {
            return Compare(Load(beforePath), Load(afterPath));
        }

        public static SaveComparatorResult Compare(SoilHealthState before, SoilHealthState after)
        {
            var result = new SaveComparatorResult();

            var locations = new HashSet<string>();
            foreach (var k in before.LocationHealthData.Keys) locations.Add(k);
            foreach (var k in after.LocationHealthData.Keys) locations.Add(k);

            foreach (var location in locations)
            {
                var beforeTiles = before.LocationHealthData.TryGetValue(location, out var bt) ? bt : new Dictionary<string, float>();
                var afterTiles = after.LocationHealthData.TryGetValue(location, out var at) ? at : new Dictionary<string, float>();

                var tiles = new HashSet<string>();
                foreach (var k in beforeTiles.Keys) tiles.Add(k);
                foreach (var k in afterTiles.Keys) tiles.Add(k);

                foreach (var tile in tiles)
                {
                    float beforeVal = beforeTiles.TryGetValue(tile, out var bv) ? bv : 0f;
                    float afterVal = afterTiles.TryGetValue(tile, out var av) ? av : 0f;

                    // NaN tiles are out of scope for verified — skip.
                    if (float.IsNaN(beforeVal) || float.IsNaN(afterVal)) continue;

                    bool beforePresent = beforeTiles.ContainsKey(tile);
                    bool afterPresent = afterTiles.ContainsKey(tile);

                    if (!beforePresent && afterPresent && Math.Abs(afterVal) > 0f)
                    {
                        // Added (was 0/missing, now non-zero).
                        if (Math.Abs(beforeVal - afterVal) > Tolerance)
                            result.Added.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                        continue;
                    }

                    if (beforePresent && !afterPresent && Math.Abs(beforeVal) > 0f)
                    {
                        // Removed (was non-zero, now 0/missing).
                        result.Removed.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                        continue;
                    }

                    if (beforePresent && afterPresent)
                    {
                        if (Math.Abs(beforeVal - afterVal) > Tolerance)
                        {
                            // Could be added/removed if crossing zero? Treat as changed.
                            if (Math.Abs(beforeVal) < Tolerance && Math.Abs(afterVal) > Tolerance)
                            {
                                result.Added.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                            }
                            else if (Math.Abs(afterVal) < Tolerance && Math.Abs(beforeVal) > Tolerance)
                            {
                                result.Removed.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                            }
                            else
                            {
                                result.Changed.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static SoilHealthState Load(string path)
        {
            if (!File.Exists(path)) return new SoilHealthState();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new SoilHealthState();
            try
            {
                return JsonSerializer.Deserialize<SoilHealthState>(json) ?? new SoilHealthState();
            }
            catch
            {
                return new SoilHealthState();
            }
        }
    }
}
