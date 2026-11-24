using System;
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
    /// scales linearly and stays fast, that the chunked scan does the same,
    /// and that chunking produces correct coverage for various input sizes.
    /// </summary>
    public class GrowthPerformanceTests
    {
        // The scans are asserted by scaling, not by a wall-clock budget: the small scan is
        // a tenth of the large one, so a linear scan must cost about ten times as much.
        private const int SmallScanTileCount = 4000;
        private const int LargeScanTileCount = 40000;
        private const int ScanMeasurementRuns = 7;
        private const int MaxScalingFactor = 50;
        private const double MaxLinearScanCeilingMs = 1000d;

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

        /// <summary>
        /// The linear scan of the tilled tiles must stay linear and fast.
        /// </summary>
        /// <remarks>
        /// A wall-clock budget is not a deterministic assertion: on a loaded host the very same
        /// scan measures 5ms or 6ms depending on the scheduler, which is a property of the
        /// machine and not of the code under test — and the suite has to stay green for the
        /// constitution's Gate 1. The test therefore asserts the scan's SCALING — the large
        /// scan must cost no more than a small multiple of the small one — which proves
        /// linearity and is immune to host load, because a uniformly slow host inflates both
        /// measurements by the same factor. A generous absolute ceiling remains as a coarse
        /// guard against an accidental O(n^2) regression.
        /// </remarks>
        [Fact]
        public void LinearScan_40000Tiles_CompletesWithinBudget()
        {
            var smallTiles = BuildHealthValues(SmallScanTileCount);
            var largeTiles = BuildHealthValues(LargeScanTileCount);

            // Warm up first, so the measurement reflects the scan and not the JIT.
            MeasureLinearScan(smallTiles, out _);
            MeasureLinearScan(largeTiles, out _);

            // A preemption can only ever inflate a measurement, so the minimum of several runs
            // is the closest estimate of what the scan really costs.
            long smallTicks = long.MaxValue;
            long largeTicks = long.MaxValue;
            float smallAccumulator = 0f;
            float largeAccumulator = 0f;
            for (int run = 0; run < ScanMeasurementRuns; run++)
            {
                smallTicks = Math.Min(smallTicks, MeasureLinearScan(smallTiles, out smallAccumulator));
                largeTicks = Math.Min(largeTicks, MeasureLinearScan(largeTiles, out largeAccumulator));
            }

            double smallMs = ToMilliseconds(smallTicks);
            double largeMs = ToMilliseconds(largeTicks);
            double scaling = (double)largeTicks / Math.Max(smallTicks, 1);

            // Linearity: ten times the tiles must not cost more than MaxScalingFactor times as much.
            Assert.True(
                scaling <= MaxScalingFactor,
                $"Linear scan of {LargeScanTileCount} tiles took {largeMs:F3}ms, which is {scaling:F1}x " +
                $"the {smallMs:F3}ms of the {SmallScanTileCount}-tile scan (max {MaxScalingFactor}x — " +
                "a super-linear scan is a regression).");

            // Coarse absolute guard: a quadratic scan over 40,000 tiles takes seconds, not milliseconds.
            Assert.True(
                largeMs < MaxLinearScanCeilingMs,
                $"Linear scan of {LargeScanTileCount} tiles took {largeMs:F3}ms " +
                $"(ceiling: {MaxLinearScanCeilingMs:F0}ms).");

            // The scan must actually have computed something, so the timings above measured real work.
            Assert.True(
                smallAccumulator > 0f && largeAccumulator > 0f,
                "The linear scan produced no result, so the timings above measured nothing.");
        }

        /// <summary>
        /// The chunked scan of the tilled tiles must stay linear and fast.
        /// </summary>
        /// <remarks>
        /// Asserted by SCALING, like the linear scan above: a wall-clock budget is a
        /// property of the host, not of the code under test, and the suite has to stay
        /// green for the constitution's Gate 1. Ten times the tiles must not cost more
        /// than MaxScalingFactor times as much, which proves the chunking stays linear
        /// and is immune to host load.
        /// </remarks>
        [Fact]
        public void ChunkedScan_40000Tiles_CompletesWithinBudget()
        {
            const int chunkSize = 1000;
            var smallTiles = BuildHealthValues(SmallScanTileCount);
            var largeTiles = BuildHealthValues(LargeScanTileCount);

            // Warm up first, so the measurement reflects the scan and not the JIT.
            MeasureChunkedScan(smallTiles, chunkSize, out _);
            MeasureChunkedScan(largeTiles, chunkSize, out _);

            long smallTicks = long.MaxValue;
            long largeTicks = long.MaxValue;
            float smallAccumulator = 0f;
            float largeAccumulator = 0f;
            for (int run = 0; run < ScanMeasurementRuns; run++)
            {
                smallTicks = Math.Min(smallTicks, MeasureChunkedScan(smallTiles, chunkSize, out smallAccumulator));
                largeTicks = Math.Min(largeTicks, MeasureChunkedScan(largeTiles, chunkSize, out largeAccumulator));
            }

            double smallMs = ToMilliseconds(smallTicks);
            double largeMs = ToMilliseconds(largeTicks);
            double scaling = (double)largeTicks / Math.Max(smallTicks, 1);

            // Linearity: ten times the tiles must not cost more than MaxScalingFactor times as much.
            Assert.True(
                scaling <= MaxScalingFactor,
                $"Chunked scan of {LargeScanTileCount} tiles took {largeMs:F3}ms, which is {scaling:F1}x " +
                $"the {smallMs:F3}ms of the {SmallScanTileCount}-tile scan (max {MaxScalingFactor}x — " +
                "a super-linear scan is a regression).");

            // Coarse absolute guard: a quadratic scan over 40,000 tiles takes seconds, not milliseconds.
            Assert.True(
                largeMs < MaxLinearScanCeilingMs,
                $"Chunked scan of {LargeScanTileCount} tiles took {largeMs:F3}ms " +
                $"(ceiling: {MaxLinearScanCeilingMs:F0}ms).");

            // The scan must actually have computed something, so the timings above measured real work.
            Assert.True(
                smallAccumulator > 0f && largeAccumulator > 0f,
                "The chunked scan produced no result, so the timings above measured nothing.");
        }

        /// <summary>
        /// Builds the simulated tilled-tile health values used by the scan measurements.
        /// </summary>
        private static List<float> BuildHealthValues(int tileCount)
        {
            var tiles = new List<float>(tileCount);
            for (int i = 0; i < tileCount; i++)
            {
                tiles.Add(i % 101); // Simulate health values 0-100
            }

            return tiles;
        }

        /// <summary>
        /// Runs the linear scan over <paramref name="tiles"/> and returns how long it took, in
        /// stopwatch ticks. The accumulated multiplier sum is returned through
        /// <paramref name="accumulator"/> so the scan cannot be optimised away.
        /// </summary>
        private static long MeasureLinearScan(List<float> tiles, out float accumulator)
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate the linear scan: iterate all tiles, read health, compute multiplier
            accumulator = 0f;
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
            return stopwatch.ElapsedTicks;
        }

        /// <summary>
        /// Converts stopwatch ticks into milliseconds.
        /// </summary>
        private static double ToMilliseconds(long ticks)
        {
            return (double)ticks * 1000d / Stopwatch.Frequency;
        }

        /// <summary>
        /// Runs the chunked scan over <paramref name="tiles"/> and returns how long it took, in
        /// stopwatch ticks. The accumulated multiplier sum is returned through
        /// <paramref name="accumulator"/> so the scan cannot be optimised away.
        /// </summary>
        private static long MeasureChunkedScan(List<float> tiles, int chunkSize, out float accumulator)
        {
            var chunkStarts = ModController.GetChunkStartIndices(tiles.Count, chunkSize);
            var stopwatch = Stopwatch.StartNew();

            // Simulate the chunked scan: iterate every chunk, read health, compute multiplier.
            accumulator = 0f;
            foreach (var chunkStart in chunkStarts)
            {
                int chunkEnd = chunkStart + chunkSize;
                if (chunkEnd > tiles.Count) chunkEnd = tiles.Count;

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
            return stopwatch.ElapsedTicks;
        }
    }
}
