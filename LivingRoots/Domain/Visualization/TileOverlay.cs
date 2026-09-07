using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Render data for a single tile overlay.
    /// </summary>
    public class TileOverlay
    {
        public Point TilePosition { get; set; }
        public Color Color { get; set; }
        public PatternType PatternType { get; set; }
        public float HealthValue { get; set; }
        public HealthCategory Category { get; set; }

        public TileOverlay()
        {
        }

        public TileOverlay(Point tilePosition, Color color, PatternType patternType, float healthValue, HealthCategory category)
        {
            TilePosition = tilePosition;
            Color = color;
            PatternType = patternType;
            HealthValue = healthValue;
            Category = category;
        }
    }
}
