using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    public class TooltipData
    {
        public string Text { get; set; } = string.Empty;
        public Vector2 Position { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
    }
}
