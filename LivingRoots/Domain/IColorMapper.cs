using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for mapping soil health values to colors.
    /// Provides color interpolation based on health levels.
    /// </summary>
    public interface IColorMapper
    {
        /// <summary>
        /// Gets the color corresponding to a soil health value using default colors.
        /// </summary>
        /// <param name="health">The soil health value (0-100)</param>
        /// <returns>Color representing the health level</returns>
        Color GetHealthColor(float health);

        /// <summary>
        /// Gets the color corresponding to a soil health value using custom colors.
        /// </summary>
        /// <param name="health">The soil health value (0-100)</param>
        /// <param name="poor">Color for poor health (0-33)</param>
        /// <param name="moderate">Color for moderate health (34-66)</param>
        /// <param name="healthy">Color for healthy health (67-100)</param>
        /// <returns>Color representing the health level with smooth gradient transition</returns>
        Color GetHealthColor(float health, Color poor, Color moderate, Color healthy);
    }
}
