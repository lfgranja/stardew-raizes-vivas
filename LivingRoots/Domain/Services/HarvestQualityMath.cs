using System;

namespace LivingRoots.Domain.Services
{
    /// <summary>
    /// Pure vanilla harvest-quality probability math (FR-003) extracted from the
    /// Harmony postfix so the composition rules can be tested without a booted game.
    /// </summary>
    public static class HarvestQualityMath
    {
        /// <summary>
        /// Baseline vanilla quality probability before the Living Soil boost:
        /// <c>0.2 * (FarmingLevel / 10) + 0.2 * FertTier * ((FarmingLevel + 2) / 12) + 0.01</c>,
        /// plus the vanilla paddy and regrow modifiers.
        /// </summary>
        public static float ComputeBaselineProbability(
            int farmingLevel,
            int fertilizerTier,
            bool isPaddyCrop,
            int regrowDays)
        {
            throw new NotImplementedException("T122 implements the vanilla quality math.");
        }

        /// <summary>Vanilla silver chance: <c>min(0.75, 2p)</c>.</summary>
        public static float ComputeSilverProbability(float baselineProbability)
        {
            throw new NotImplementedException("T122 implements the vanilla quality math.");
        }

        /// <summary>Vanilla iridium chance: <c>p / 2</c>, only when fertilizer tier is 3 (deluxe).</summary>
        public static float ComputeIridiumProbability(float baselineProbability, int fertilizerTier)
        {
            throw new NotImplementedException("T122 implements the vanilla quality math.");
        }
    }
}
