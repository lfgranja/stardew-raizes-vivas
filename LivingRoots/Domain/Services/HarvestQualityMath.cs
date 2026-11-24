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
            float p = 0.2f * (farmingLevel / 10.0f)
                      + 0.2f * fertilizerTier * ((farmingLevel + 2) / 12.0f)
                      + 0.01f;
            if (isPaddyCrop)
            {
                p += 0.1f;
            }

            if (regrowDays > 0)
            {
                p += 0.1f;
            }

            return p;
        }

        public static float ComputeSilverProbability(float baselineProbability)
        {
            return Math.Min(0.75f, 2.0f * baselineProbability);
        }

        public static float ComputeIridiumProbability(float baselineProbability, int fertilizerTier)
        {
            if (fertilizerTier < 3)
            {
                return 0.0f;
            }

            return baselineProbability / 2.0f;
        }
    }
}
