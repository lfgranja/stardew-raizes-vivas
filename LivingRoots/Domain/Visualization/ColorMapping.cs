using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    public class ColorMapping
    {
        public HealthCategory Category { get; set; }
        public Color BaseColor { get; set; }
        public Color InterpolatedColor { get; set; }
        public float HealthValue { get; set; }
    }
}
