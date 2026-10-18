using System.Collections.Generic;
using System.Diagnostics;
using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T023 — Performance contract tests for the growth modifier
    /// (spec 005 FR-001, Q7). Verifies that the linear scan of tilled tiles
    /// completes within the 5ms budget and that chunking produces correct
    /// coverage for various input sizes.
    /// </summary>
    public class GrowthPerformanceTests
    {
        private const int MaxScanBudgetMs = 5;

        [Fact]
        public void GetChunkStartIndices_ProducesCorrectChunkCount()
        {
            // 40,000 tiles / 1,000 per chunk = 40 chunks
            var starts = ModController.GetChunkStartIndices(40000, 1000);
            Assert.Equal(40, starts.Count);
        }

        [Fact]
        public void GetChunkStartIndices_SmallInput_SingleChunk()
        {
            var starts = ModController.GetChunkStartIndices(100, 1000);
            Assert.Single(starts);
            Assert.Equal(0, starts[0]);
        }

        [Fact]
        public void GetChunkStartIndices_ExactMultiple_NoRemainder()
        {
            // 5,000 / 1,000 = exactly 5 chunks
            var starts = ModController.GetChunkStartIndices(5000, 1000);
            Assert.Equal(5, starts.Count);
        }

        [Fact]
        public void GetChunkStartIndices_WithRemainder_IncludesPartialChunk()
        {
            // 5,500 / 1,000 = 5 full + 1 partial = 6 chunks
            var starts = ModController.GetChunkStartIndices(5500, 1000);
            Assert.Equal(6, starts.Count);
        }

        [Fact]
        public void GetChunkStartIndices_ZeroTiles_ReturnsEmpty()
        {
            var starts = ModController.GetChunkStartIndices(0, 1000);
            Assert.Empty(starts);
        }

        [Fact]
        public void GetChunkStartIndices_ZeroChunkSize_ReturnsEmpty()
        {
            var starts = ModController.GetChunkStartIndices(1000, 0);
            Assert.Empty(starts);
        }

        [Fact]
        public void GetChunkStartIndices_NoDuplicates()
        {
            var starts = ModController.GetChunkStartIndices(40000, 1000);
            var unique = new HashSet<int>(starts);
            Assert.Equal(starts.Count, unique.Count);
        }

        [Fact]
        public void GetChunkStartIndices_FullCoverage()
        {
            const int totalTiles = 40000;
            const int chunkSize = 1000;
            var starts = ModController.GetChunkStartIndices(totalTiles, chunkSize);

            var covered = new bool[totalTiles];
            foreach (var start in starts)
            {
                for (int i = start; i < start + chunkSize && i < totalTiles; i++)
                {
                    Assert.False(covered[i], $"Tile {i} covered twice.");
                    covered[i] = true;
                }
            }

            for (int i = 0; i < totalTiles; i++)
            {
                Assert.True(covered[i], $"Tile {i} not covered.");
            }
        }

        [Fact]
        public void LinearScan_40000Tiles_CompletesWithinBudget()
        {
            const int tileCount = 40000;
            var tiles = new List<float>(tileCount);
            for (int i = 0; i < tileCount; i++)
            {
                tiles.Add(i % 101); // Simulate health values 0-100
            }

            var stopwatch = Stopwatch.StartNew();

            // Simulate the linear scan: iterate all tiles, read health, compute multiplier
            float accumulator = 0f;
            for (int i = 0; i < tiles.Count; i++)
            {
                var health = tiles[i];
                if (health <= 0f || health > 100f) continue;

                float multiplier;
                if (health <= 30f) multiplier = ModConstants.SoilGrowthPoorMultiplier;
                else if (health <= 70f) multiplier = 1.0f;
                else multiplier = ModConstants.SoilGrowthHealthyMultiplier;

                if (multiplier == 1.0f) continue;

                accumulator += health * multiplier;
            }

            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxScanBudgetMs,
                $"Linear scan of {tileCount} tiles took {stopwatch.ElapsedMilliseconds}ms (budget: {MaxScanBudgetMs}ms).");
        }

        [Fact]
        public void ChunkedScan_40000Tiles_CompletesWithinBudget()
        {
            const int tileCount = 40000;
            const int chunkSize = 1000;
            var tiles = new List<float>(tileCount);
            for (int i = 0; i < tileCount; i++)
            {
                tiles.Add(i % 101);
            }

            var chunkStarts = ModController.GetChunkStartIndices(tileCount, chunkSize);
            var stopwatch = Stopwatch.StartNew();

            float accumulator = 0f;
            foreach (var chunkStart in chunkStarts)
            {
                int chunkEnd = chunkStart + chunkSize;
                if (chunkEnd > tileCount) chunkEnd = tileCount;

                for (int i = chunkStart; i < chunkEnd; i++)
                {
                    var health = tiles[i];
                    if (health <= 0f || health > 100f) continue;

                    float multiplier;
                    if (health <= 30f) multiplier = ModConstants.SoilGrowthPoorMultiplier;
                    else if (health <= 70f) multiplier = 1.0f;
                    else multiplier = ModConstants.SoilGrowthHealthyMultiplier;

                    if (multiplier == 1.0f) continue;

                    accumulator += health * multiplier;
                }
            }

            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxScanBudgetMs,
                $"Chunked scan of {tileCount} tiles took {stopwatch.ElapsedMilliseconds}ms (budget: {MaxScanBudgetMs}ms).");
        }
    }
}
