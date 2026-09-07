namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Enumeration of soil health categories with threshold boundaries.
    /// Uses half-open interval rules: [0, 34), [34, 67), [67, 100].
    /// </summary>
    public enum HealthCategory
    {
        Poor = 0,
        Moderate = 1,
        Healthy = 2,
        Unknown = 3
    }

    public static class HealthCategoryExtensions
    {
        /// <summary>
        /// Determines the health category for a given health value.
        /// Uses half-open intervals: 33 → Poor, 34 → Moderate, 66 → Moderate, 67 → Healthy.
        /// </summary>
        public static HealthCategory FromHealthValue(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                return HealthCategory.Unknown;

            if (healthValue < 0f || healthValue > 100f)
                return HealthCategory.Unknown;

            if (healthValue < 34f)
                return HealthCategory.Poor;

            if (healthValue < 67f)
                return HealthCategory.Moderate;

            return HealthCategory.Healthy;
        }
    }
}
