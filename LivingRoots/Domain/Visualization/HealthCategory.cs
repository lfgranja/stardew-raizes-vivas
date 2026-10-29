namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Enumeration of soil health categories with half-open threshold intervals:
    /// Poor [0, 34), Moderate [34, 67), Healthy [67, 100]. NaN and Infinity
    /// resolve to Unknown; finite values outside [0, 100] are clamped to the
    /// nearest boundary category (below 0 → Poor, above 100 → Healthy) per
    /// FR-008/SC-007.
    /// </summary>
    public enum HealthCategory
    {
        Poor = 0,
        Moderate = 1,
        Healthy = 2,
        Unknown = 3
    }

    /// <summary>
    /// Resolves a numeric soil health value to its <see cref="HealthCategory"/>.
    /// </summary>
    public static class HealthCategoryExtensions
    {
        /// <summary>
        /// Maps a health value to its category using half-open intervals.
        /// </summary>
        /// <param name="healthValue">Health value (0-100).</param>
        /// <returns>
        /// Poor for [0, 34), Moderate for [34, 67), Healthy for [67, 100].
        /// Unknown for NaN or Infinity. Finite values outside [0, 100] are
        /// clamped to the nearest boundary category.
        /// </returns>
        public static HealthCategory FromHealthValue(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return HealthCategory.Unknown;
            }

            // Finite out-of-range producer values clamp to the valid
            // soil-health range before the intervals resolve (FR-008,
            // SC-007, T118) — the same contract
            // ColorInterpolationService.GetCategoryForHealth applies, so
            // every category resolver agrees on a single out-of-range rule.
            var clamped = Math.Clamp(
                healthValue,
                ModConstants.MinSoilHealth,
                ModConstants.MaxSoilHealth);

            if (clamped < ModConstants.PoorModerateBandEdge)
            {
                return HealthCategory.Poor;
            }

            if (clamped < ModConstants.ModerateHealthyBandEdge)
            {
                return HealthCategory.Moderate;
            }

            return HealthCategory.Healthy;
        }
    }
}
