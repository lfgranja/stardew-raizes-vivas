using System;
using System.Collections.Generic;

namespace LivingRoots.Verification.Evidence
{
    /// <summary>
    /// Produces the deterministic tile order the harvest matrix scans. The
    /// whole map is covered so a probe cell with a rare roll cannot disappear
    /// because the scenario's anchor tile happens to sit in a region where the
    /// hash never lands in that cell's window. Nearest-first keeps the probes
    /// close to the anchor when the same cell is available nearby.
    /// </summary>
    public static class HarvestProbeScanner
    {
        private readonly struct ScoredTile
        {
            public ScoredTile(int x, int y, long distance)
            {
                X = x;
                Y = y;
                Distance = distance;
            }

            public int X { get; }

            public int Y { get; }

            public long Distance { get; }
        }

        public static IReadOnlyList<(int X, int Y)> OrderedTiles(int baseX, int baseY, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return Array.Empty<(int X, int Y)>();
            }

            var scored = new List<ScoredTile>(width * height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    long dx = x - baseX;
                    long dy = y - baseY;
                    scored.Add(new ScoredTile(x, y, (dx * dx) + (dy * dy)));
                }
            }

            scored.Sort(Compare);
            var ordered = new List<(int X, int Y)>(scored.Count);
            foreach (var tile in scored)
            {
                ordered.Add((tile.X, tile.Y));
            }

            return ordered;
        }

        private static int Compare(ScoredTile left, ScoredTile right)
        {
            int byDistance = left.Distance.CompareTo(right.Distance);
            if (byDistance != 0)
            {
                return byDistance;
            }

            int byY = left.Y.CompareTo(right.Y);
            if (byY != 0)
            {
                return byY;
            }

            return left.X.CompareTo(right.X);
        }
    }
}
