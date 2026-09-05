using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Represents the translation from a numeric health value to a rendered color.
    /// </summary>
    public class ColorMapping
    {
        public HealthCategory Category { get; set; } = HealthCategory.Unknown;
        public Color BaseColor { get; set; }
        public Color InterpolatedColor { get; set; }
        public float HealthValue { get; set; }
    }
}
