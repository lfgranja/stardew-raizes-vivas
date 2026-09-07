using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Data for hover tooltip rendering.
    /// </summary>
    public class TooltipData
    {
        public string? Text { get; set; }
        public Vector2 Position { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }

        /// <summary>
        /// Formats tooltip text using the pattern: "Soil Health: {percentage}% ({category})"
        /// </summary>
        public static string Format(int percentage, string category)
        {
            return $"Soil Health: {percentage}% ({category})";
        }
    }
}
