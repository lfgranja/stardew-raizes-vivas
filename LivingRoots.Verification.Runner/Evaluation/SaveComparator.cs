using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LivingRoots.Verification.Runner.Evaluation
{
    public sealed class SaveComparatorEntry
    {
        public string Location { get; set; } = string.Empty;

        public string Tile { get; set; } = string.Empty;

        public float Before { get; set; }

        public float After { get; set; }
    }

    public sealed class SaveComparatorResult
    {
        public List<SaveComparatorEntry> Added { get; } = new List<SaveComparatorEntry>();

        public List<SaveComparatorEntry> Removed { get; } = new List<SaveComparatorEntry>();

        public List<SaveComparatorEntry> Changed { get; } = new List<SaveComparatorEntry>();

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public sealed class SoilHealthState
    {
        public Dictionary<string, Dictionary<string, float>> LocationHealthData { get; set; } = new Dictionary<string, Dictionary<string, float>>();
    }

    public sealed class SaveComparator
    {
        public const float Tolerance = 0.0001f;

        public SaveComparator()
        {
        }

        public static SaveComparatorResult Compare(string beforePath, string afterPath)
        {
            return Compare(Load(beforePath), Load(afterPath));
        }

        public static SaveComparatorResult Compare(SoilHealthState before, SoilHealthState after)
        {
            var result = new SaveComparatorResult();

            var locations = new HashSet<string>();
            foreach (var k in before.LocationHealthData.Keys)
            {
                locations.Add(k);
            }
            foreach (var k in after.LocationHealthData.Keys)
            {
                locations.Add(k);
            }

            foreach (var location in locations)
            {
                var beforeTiles = before.LocationHealthData.TryGetValue(location, out var bt) ? bt : new Dictionary<string, float>();
                var afterTiles = after.LocationHealthData.TryGetValue(location, out var at) ? at : new Dictionary<string, float>();

                var tiles = new HashSet<string>();
                foreach (var k in beforeTiles.Keys)
                {
                    tiles.Add(k);
                }
                foreach (var k in afterTiles.Keys)
                {
                    tiles.Add(k);
                }

                foreach (var tile in tiles)
                {
                    var beforePresent = beforeTiles.ContainsKey(tile);
                    var afterPresent = afterTiles.ContainsKey(tile);
                    var beforeVal = beforePresent ? beforeTiles[tile] : 0f;
                    var afterVal = afterPresent ? afterTiles[tile] : 0f;

                    if (float.IsNaN(beforeVal) || float.IsNaN(afterVal))
                    {
                        continue;
                    }

                    if (!beforePresent && afterPresent && Math.Abs(afterVal) > 0f)
                    {
                        if (Math.Abs(beforeVal - afterVal) > Tolerance)
                        {
                            result.Added.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                        }
                        continue;
                    }

                    if (beforePresent && !afterPresent && Math.Abs(beforeVal) > 0f)
                    {
                        result.Removed.Add(new SaveComparatorEntry { Location = location, Tile = tile, Before = beforeVal, After = afterVal });
                        continue;
                    }

                    if (beforePresent && afterPresent)
                    {
                        if (Math.Abs(beforeVal - afterVal) > Tolerance)
                        {
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
            if (!File.Exists(path))
            {
                return new SoilHealthState();
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SoilHealthState();
            }

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
