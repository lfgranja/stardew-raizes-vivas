namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Enumeration of soil health categories with ROADMAP.md threshold intervals:
    /// Poor [0, 30], Moderate [31, 70], Healthy [71, 100]. Out-of-range,
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
        /// Maps a health value to its category using ROADMAP.md intervals.
        /// </summary>
        /// <param name="healthValue">Health value (0-100).</param>
        /// <returns>
        /// Poor for [0, 30], Moderate for [31, 70], Healthy for [71, 100],
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
                >= 0 and <= 30 => HealthCategory.Poor,
                >= 31 and <= 70 => HealthCategory.Moderate,
                >= 71 and <= 100 => HealthCategory.Healthy,
                _ => HealthCategory.Unknown
            };
        }
    }
}
