namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Enumeration of accessibility patterns for colorblind accessibility.
    /// </summary>
    public enum PatternType
    {
        None = 0,
        Stripes = 1,
        Dots = 2,
        Solid = 3
    }

    public static class PatternTypeExtensions
    {
        /// <summary>
        /// Maps a health category to its corresponding accessibility pattern.
        /// Poor → Stripes, Moderate → Dots, Healthy → Solid, Unknown → None.
        /// </summary>
        public static PatternType FromHealthCategory(HealthCategory category)
        {
            return category switch
            {
                HealthCategory.Poor => PatternType.Stripes,
                HealthCategory.Moderate => PatternType.Dots,
                HealthCategory.Healthy => PatternType.Solid,
                HealthCategory.Unknown => PatternType.None,
                _ => PatternType.None
            };
        }
    }
}
