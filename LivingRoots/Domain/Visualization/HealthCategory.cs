namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Enumeration of soil health categories with half-open threshold intervals:
    /// Poor [0, 34), Moderate [34, 67), Healthy [67, 100]. Out-of-range,
    /// NaN, and Infinity values resolve to Unknown.
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
        /// Poor for [0, 34), Moderate for [34, 67), Healthy for [67, 100],
        /// Unknown for NaN, Infinity, or values outside [0, 100].
        /// </returns>
        public static HealthCategory FromHealthValue(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return HealthCategory.Unknown;
            }

            return healthValue switch
            {
                >= 0 and < 34 => HealthCategory.Poor,
                >= 34 and < 67 => HealthCategory.Moderate,
                >= 67 and <= 100 => HealthCategory.Healthy,
                _ => HealthCategory.Unknown
            };
        }
    }
}
