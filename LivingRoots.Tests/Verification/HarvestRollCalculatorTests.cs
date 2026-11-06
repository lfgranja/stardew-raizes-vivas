using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class HarvestRollCalculatorTests
    {
        [Fact]
        public void QualityRoll_MatchesProductionHashForKnownTile()
        {
            var roll = HarvestRollCalculator.QualityRoll(60, 15, 5, 123, 0);
            Assert.Equal(0.3949, roll, 10);
        }

        [Fact]
        public void ExtraRoll_MatchesProductionHashForKnownTile()
        {
            var roll = HarvestRollCalculator.ExtraRoll(60, 15, 5, 123);
            Assert.Equal(0.8356, roll, 10);
        }

        [Fact]
        public void VanillaQualityProbability_UsesTheProductionFormula()
        {
            Assert.Equal(0.01, HarvestRollCalculator.VanillaQualityProbability(0, 0), 10);
            Assert.Equal(0.21, HarvestRollCalculator.VanillaQualityProbability(10, 0), 10);
            Assert.Equal(0.81, HarvestRollCalculator.VanillaQualityProbability(10, 3), 10);
        }

        [Fact]
        public void IsQualityBoosted_IsHalfOpenAtTheBoostedEdge()
        {
            Assert.False(HarvestRollCalculator.IsQualityBoosted(0.20, 0.21, 0.08));
            Assert.True(HarvestRollCalculator.IsQualityBoosted(0.21, 0.21, 0.08));
            Assert.True(HarvestRollCalculator.IsQualityBoosted(0.2899, 0.21, 0.08));
            Assert.False(HarvestRollCalculator.IsQualityBoosted(0.29, 0.21, 0.08));
        }

        [Fact]
        public void IsExtraTriggered_IsStrictAtTheBoostedEdge()
        {
            Assert.True(HarvestRollCalculator.IsExtraTriggered(0.0, 0.10));
            Assert.True(HarvestRollCalculator.IsExtraTriggered(0.0999, 0.10));
            Assert.False(HarvestRollCalculator.IsExtraTriggered(0.10, 0.10));
        }

        [Property(MaxTest = 200)]
        public Property QualityRoll_IsDeterministicAndInUnitInterval()
        {
            var inputs = Arb.From(
                from x in Gen.Choose(-1000, 1000)
                from y in Gen.Choose(-1000, 1000)
                from totalDays in Gen.Choose(0, 100000)
                from uniqueId in Gen.Choose(0, 1000000)
                from fertilizerTier in Gen.Choose(0, 3)
                select (x, y, totalDays, uniqueId, fertilizerTier));

            return Prop.ForAll(inputs, input =>
            {
                var first = HarvestRollCalculator.QualityRoll(input.x, input.y, input.totalDays, input.uniqueId, input.fertilizerTier);
                var second = HarvestRollCalculator.QualityRoll(input.x, input.y, input.totalDays, input.uniqueId, input.fertilizerTier);
                return first == second && first >= 0.0 && first < 1.0;
            });
        }

        [Property(MaxTest = 200)]
        public Property ExtraRoll_IsDeterministicAndInUnitInterval()
        {
            var inputs = Arb.From(
                from x in Gen.Choose(-1000, 1000)
                from y in Gen.Choose(-1000, 1000)
                from totalDays in Gen.Choose(0, 100000)
                from uniqueId in Gen.Choose(0, 1000000)
                select (x, y, totalDays, uniqueId));

            return Prop.ForAll(inputs, input =>
            {
                var first = HarvestRollCalculator.ExtraRoll(input.x, input.y, input.totalDays, input.uniqueId);
                var second = HarvestRollCalculator.ExtraRoll(input.x, input.y, input.totalDays, input.uniqueId);
                return first == second && first >= 0.0 && first < 1.0;
            });
        }

        [Fact]
        public void TrySelect_ChoosesOneDistinctTilePerProbe()
        {
            var candidates = new List<HarvestProbeCandidate>
            {
                new HarvestProbeCandidate(1, 1, 0.50, 0.50, 1),
                new HarvestProbeCandidate(2, 2, 0.25, 0.50, 1),
                new HarvestProbeCandidate(3, 3, 0.25, 0.50, 0),
                new HarvestProbeCandidate(4, 4, 0.50, 0.05, 0),
            };

            var selected = HarvestMatrixPlanner.TrySelect(candidates, 0.21, 0.08, 0.10, out var probes);

            Assert.True(selected);
            Assert.Equal(
                new[] { HarvestMatrixPlanner.PoorProbe, HarvestMatrixPlanner.ModerateProbe, HarvestMatrixPlanner.HealthyQualityProbe, HarvestMatrixPlanner.HealthyExtraProbe },
                probes.Select(probe => probe.Name).ToArray());
            Assert.Equal(
                new[] { "1,1", "2,2", "3,3", "4,4" },
                probes.Select(probe => probe.Candidate!.TileX + "," + probe.Candidate.TileY).ToArray());
        }

        [Fact]
        public void TrySelect_MissingHealthyExtra_ReturnsFalseWithEmptySelection()
        {
            var candidates = new List<HarvestProbeCandidate>
            {
                new HarvestProbeCandidate(1, 1, 0.50, 0.50, 1),
                new HarvestProbeCandidate(2, 2, 0.25, 0.50, 1),
                new HarvestProbeCandidate(3, 3, 0.25, 0.50, 0),
            };

            var selected = HarvestMatrixPlanner.TrySelect(candidates, 0.21, 0.08, 0.10, out var probes);

            Assert.False(selected);
            Assert.Empty(probes);
        }

        [Fact]
        public void TrySelect_DoesNotReuseOnePoorCandidateForModerate()
        {
            var candidates = new List<HarvestProbeCandidate>
            {
                new HarvestProbeCandidate(1, 1, 0.25, 0.50, 1),
                new HarvestProbeCandidate(2, 2, 0.25, 0.50, 0),
                new HarvestProbeCandidate(3, 3, 0.50, 0.05, 0),
            };

            var selected = HarvestMatrixPlanner.TrySelect(candidates, 0.21, 0.08, 0.10, out var probes);

            Assert.False(selected);
            Assert.Empty(probes);
        }

        [Property(MaxTest = 100)]
        public Property TrySelect_NeverReusesATileAndHonoursProbeRules()
        {
            return Prop.ForAll(
                Arb.From(Gen.ListOf(Gen.Choose(0, 2))),
                vanillaQualities =>
                {
                    var candidates = vanillaQualities
                        .Select((vanilla, index) => new HarvestProbeCandidate(
                            index,
                            index * 2,
                            ((index * 397) % 10000) / 10000.0,
                            ((index * 811) % 10000) / 10000.0,
                            vanilla))
                        .ToList();
                    var selected = HarvestMatrixPlanner.TrySelect(candidates, 0.21, 0.08, 0.10, out var probes);
                    if (!selected)
                    {
                        return true;
                    }

                    var tiles = probes.Select(probe => probe.Candidate!.TileX + "," + probe.Candidate.TileY).ToArray();
                    var distinct = tiles.Distinct(StringComparer.Ordinal).Count() == tiles.Length;
                    var valid = probes.Count == 4;
                    foreach (var probe in probes)
                    {
                        var candidate = probe.Candidate!;
                        if (probe.Name == HarvestMatrixPlanner.PoorProbe)
                        {
                            valid &= candidate.VanillaQuality == 1 && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, 0.10);
                        }
                        else if (probe.Name == HarvestMatrixPlanner.ModerateProbe)
                        {
                            valid &= candidate.VanillaQuality == 1
                                && HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, 0.21, 0.08)
                                && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, 0.10);
                        }
                        else if (probe.Name == HarvestMatrixPlanner.HealthyQualityProbe)
                        {
                            valid &= candidate.VanillaQuality == 0
                                && HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, 0.21, 0.08)
                                && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, 0.10);
                        }
                        else if (probe.Name == HarvestMatrixPlanner.HealthyExtraProbe)
                        {
                            valid &= candidate.VanillaQuality == 0
                                && !HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, 0.21, 0.08)
                                && HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, 0.10);
                        }
                        else
                        {
                            valid = false;
                        }
                    }

                    return distinct && valid;
                });
        }
    }
}
