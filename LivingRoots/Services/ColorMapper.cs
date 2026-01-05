using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of color mapping for soil health values.
    /// Provides color interpolation based on health levels with smooth gradients.
    /// </summary>
    public class ColorMapper : IColorMapper
    {
        // Dependencies
        private readonly IVisualizationConfig _config;

        // Default colors
        private static readonly Color DefaultPoorColor = new Color(139, 69, 19);  // SaddleBrown
        private static readonly Color DefaultModerateColor = new Color(218, 165, 32);  // GoldenRod
        private static readonly Color DefaultHealthyColor = new Color(85, 107, 47);  // DarkOliveGreen

        /// <summary>
        /// Initializes a new instance of ColorMapper.
        /// </summary>
        /// <param name="monitor">Monitor for logging</param>
        /// <param name="config">Visualization configuration</param>
        public ColorMapper(IMonitor monitor, IVisualizationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public Color GetHealthColor(float health)
        {
            return GetHealthColor(health, _config.UseCustomColors ? _config.PoorHealthColor : DefaultPoorColor,
                _config.UseCustomColors ? _config.ModerateHealthColor : DefaultModerateColor,
                _config.UseCustomColors ? _config.HealthyHealthColor : DefaultHealthyColor);
        }

        /// <inheritdoc/>
        public Color GetHealthColor(float health, Color poor, Color moderate, Color healthy)
        {
            // Clamp health to valid range [0, 100]
            health = Math.Clamp(health, 0f, 100f);

            // Determine health category and interpolate colors
            if (health <= ModConstants.PoorHealthThreshold)
            {
                // Poor health: interpolate from red to poor color
                return InterpolateColors(new Color(255, 0, 0), poor, health / ModConstants.PoorHealthThreshold);
            }
            else if (health <= ModConstants.ModerateHealthThreshold)
            {
                // Moderate health: interpolate between poor and moderate colors
                float t = (health - ModConstants.PoorHealthThreshold) /
                           (ModConstants.ModerateHealthThreshold - ModConstants.PoorHealthThreshold);
                return InterpolateColors(poor, moderate, t);
            }
            else
            {
                // Healthy health: interpolate between moderate and healthy colors
                float t = (health - ModConstants.ModerateHealthThreshold) /
                           (ModConstants.HealthyHealthThreshold - ModConstants.ModerateHealthThreshold);
                return InterpolateColors(moderate, healthy, t);
            }
        }

        /// <summary>
        /// Interpolates between two colors using linear interpolation.
        /// </summary>
        /// <param name="start">The starting color</param>
        /// <param name="end">The ending color</param>
        /// <param name="t">The interpolation factor (0.0 to 1.0)</param>
        /// <returns>Interpolated color</returns>
        private static Color InterpolateColors(Color start, Color end, float t)
        {
            // Clamp t to [0, 1] to ensure valid interpolation
            t = Math.Clamp(t, 0f, 1f);

            // Linear interpolation for each color channel
            byte r = (byte)(start.R + (end.R - start.R) * t);
            byte g = (byte)(start.G + (end.G - start.G) * t);
            byte b = (byte)(start.B + (end.B - start.B) * t);
            byte a = (byte)(start.A + (end.A - start.A) * t);

            return new Color(r, g, b, a);
        }
    }
}
