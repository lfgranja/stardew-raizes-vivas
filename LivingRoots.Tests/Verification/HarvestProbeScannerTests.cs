using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class HarvestProbeScannerTests
    {
        [Fact]
        public void OrderedTiles_CoversTheWholeFarmExactlyOnce()
        {
            var tiles = HarvestProbeScanner.OrderedTiles(60, 15, 80, 65);

            Assert.Equal(80 * 65, tiles.Count);
            Assert.Equal(80 * 65, tiles.Distinct().Count());
        }

        [Fact]
        public void OrderedTiles_StartsAtTheBaseTileAndEndsAtTheFarthestCorner()
        {
            var tiles = HarvestProbeScanner.OrderedTiles(60, 15, 80, 65);

            Assert.Equal((60, 15), tiles[0]);
            Assert.Equal((0, 64), tiles[tiles.Count - 1]);
        }

        [Fact]
        public void OrderedTiles_ReturnsEmptyForAnEmptyGrid()
        {
            Assert.Empty(HarvestProbeScanner.OrderedTiles(10, 10, 0, 0));
        }

        [Property(MaxTest = 100)]
        public Property OrderedTiles_PreservesEveryTileInBounds()
        {
            var inputs = Arb.From(
                from width in Gen.Choose(1, 80)
                from height in Gen.Choose(1, 65)
                from baseX in Gen.Choose(-500, 500)
                from baseY in Gen.Choose(-500, 500)
                select (width, height, baseX, baseY));

            return Prop.ForAll(inputs, input =>
            {
                var tiles = HarvestProbeScanner.OrderedTiles(input.baseX, input.baseY, input.width, input.height);
                var expected = new HashSet<(int X, int Y)>();
                for (int y = 0; y < input.height; y++)
                {
                    for (int x = 0; x < input.width; x++)
                    {
                        expected.Add((x, y));
                    }
                }

                return tiles.Count == expected.Count
                    && tiles.Distinct().Count() == expected.Count
                    && tiles.All(expected.Contains);
            });
        }

        [Property(MaxTest = 100)]
        public Property OrderedTiles_IsNearestFirst()
        {
            var inputs = Arb.From(
                from width in Gen.Choose(1, 80)
                from height in Gen.Choose(1, 65)
                from baseX in Gen.Choose(-500, 500)
                from baseY in Gen.Choose(-500, 500)
                select (width, height, baseX, baseY));

            return Prop.ForAll(inputs, input =>
            {
                var tiles = HarvestProbeScanner.OrderedTiles(input.baseX, input.baseY, input.width, input.height);
                long previousDistance = -1;
                foreach (var tile in tiles)
                {
                    long dx = tile.X - input.baseX;
                    long dy = tile.Y - input.baseY;
                    long distance = (dx * dx) + (dy * dy);
                    if (distance < previousDistance)
                    {
                        return false;
                    }

                    previousDistance = distance;
                }

                return true;
            });
        }

        [Property(MaxTest = 100)]
        public Property OrderedTiles_IsDeterministic()
        {
            var inputs = Arb.From(
                from width in Gen.Choose(1, 80)
                from height in Gen.Choose(1, 65)
                from baseX in Gen.Choose(-500, 500)
                from baseY in Gen.Choose(-500, 500)
                select (width, height, baseX, baseY));

            return Prop.ForAll(inputs, input =>
            {
                var first = HarvestProbeScanner.OrderedTiles(input.baseX, input.baseY, input.width, input.height);
                var second = HarvestProbeScanner.OrderedTiles(input.baseX, input.baseY, input.width, input.height);
                return first.SequenceEqual(second);
            });
        }
    }
}
