using System;
using System.Collections.Generic;

namespace LivingRoots.Verification.Evidence
{
    /// <summary>
    /// Pure reproduction of the harvest rolls used by the production Harmony
    /// patch. The verification mod must know the outcome before it plants a
    /// probe crop, so the formulas live here and are pinned by tests.
    /// </summary>
    public static class HarvestRollCalculator
    {
        public static double QualityRoll(int tileX, int tileY, int totalDays, int uniqueId, int fertilizerTier)
        {
            int hash = (tileX * 397) ^ (tileY * 17) ^ totalDays ^ uniqueId ^ (fertilizerTier * 31);
            return Math.Abs((long)hash % 10000L) / 10000.0;
        }

        public static double ExtraRoll(int tileX, int tileY, int totalDays, int uniqueId)
        {
            int hash = (tileX * 397) ^ (tileY * 17) ^ totalDays ^ uniqueId ^ 0x5A827999;
            return Math.Abs((long)hash % 10000L) / 10000.0;
        }

        public static double VanillaQualityProbability(int farmingLevel, int fertilizerTier)
        {
            return 0.2 * (farmingLevel / 10.0)
                + 0.2 * fertilizerTier * ((farmingLevel + 2) / 12.0)
                + 0.01;
        }

        public static bool IsQualityBoosted(double roll, double qualityProbability, double qualityBoost)
        {
            return roll >= qualityProbability && roll < qualityProbability + qualityBoost;
        }

        public static bool IsExtraTriggered(double roll, double extraChance)
        {
            return roll >= 0.0 && roll < extraChance;
        }
    }

    public sealed class HarvestProbeCandidate
    {
        public HarvestProbeCandidate(int tileX, int tileY, double qualityRoll, double extraRoll, int vanillaQuality)
        {
            TileX = tileX;
            TileY = tileY;
            QualityRoll = qualityRoll;
            ExtraRoll = extraRoll;
            VanillaQuality = vanillaQuality;
        }

        public int TileX { get; }

        public int TileY { get; }

        public double QualityRoll { get; }

        public double ExtraRoll { get; }

        public int VanillaQuality { get; }
    }

    public sealed class HarvestProbe
    {
        public HarvestProbe(string name, HarvestProbeCandidate candidate)
        {
            Name = name;
            Candidate = candidate;
        }

        public string Name { get; }

        public HarvestProbeCandidate Candidate { get; }
    }

    /// <summary>
    /// Selects one distinct tile for each cell of the four-cell harvest
    /// matrix. Selection is greedy in a fixed probe order; a partial matrix is
    /// never returned, because a missing probe would make the scenario's
    /// evidence ambiguous.
    /// </summary>
    public static class HarvestMatrixPlanner
    {
        public const string PoorProbe = "poor";
        public const string ModerateProbe = "moderate";
        public const string HealthyQualityProbe = "healthy-quality";
        public const string HealthyExtraProbe = "healthy-extra";

        public static bool TrySelect(
            IReadOnlyList<HarvestProbeCandidate> candidates,
            double qualityProbability,
            double qualityBoost,
            double extraChance,
            out IReadOnlyList<HarvestProbe> probes)
        {
            probes = Array.Empty<HarvestProbe>();
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            var used = new HashSet<string>(StringComparer.Ordinal);
            var selected = new List<HarvestProbe>(4);

            if (!TryAdd(PoorProbe, candidates, used, selected, candidate =>
                candidate.VanillaQuality == 1
                && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, extraChance)))
            {
                return false;
            }

            if (!TryAdd(ModerateProbe, candidates, used, selected, candidate =>
                candidate.VanillaQuality == 1
                && HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, qualityProbability, qualityBoost)
                && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, extraChance)))
            {
                return false;
            }

            if (!TryAdd(HealthyQualityProbe, candidates, used, selected, candidate =>
                candidate.VanillaQuality == 0
                && HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, qualityProbability, qualityBoost)
                && !HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, extraChance)))
            {
                return false;
            }

            if (!TryAdd(HealthyExtraProbe, candidates, used, selected, candidate =>
                candidate.VanillaQuality == 0
                && !HarvestRollCalculator.IsQualityBoosted(candidate.QualityRoll, qualityProbability, qualityBoost)
                && HarvestRollCalculator.IsExtraTriggered(candidate.ExtraRoll, extraChance)))
            {
                return false;
            }

            probes = selected;
            return true;
        }

        private static bool TryAdd(
            string name,
            IReadOnlyList<HarvestProbeCandidate> candidates,
            ISet<string> used,
            IList<HarvestProbe> selected,
            Func<HarvestProbeCandidate, bool> matches)
        {
            foreach (var candidate in candidates)
            {
                string key = candidate.TileX + "," + candidate.TileY;
                if (used.Contains(key) || !matches(candidate))
                {
                    continue;
                }

                used.Add(key);
                selected.Add(new HarvestProbe(name, candidate));
                return true;
            }

            return false;
        }
    }
}
