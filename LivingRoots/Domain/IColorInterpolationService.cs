using Microsoft.Xna.Framework;
using LivingRoots.Domain.Visualization;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Computes color mappings from health values using linear RGB interpolation.
    /// Supports caching for performance.
    /// </summary>
    public interface IColorInterpolationService
    {
        /// <summary>
        /// Gets the color for a health value with interpolation.
        /// Uses category thresholds with half-open intervals: Poor [0, 34), Moderate [34, 67), Healthy [67, 100].
        /// </summary>
        /// <param name="healthValue">Health value (0-100)</param>
        /// <returns>Interpolated color for the health value</returns>
        Color GetColorForHealth(float healthValue);

        /// <summary>
        /// Gets the color for a health value with opacity applied.
        /// </summary>
        /// <param name="healthValue">Health value (0-100)</param>
        /// <param name="opacity">Opacity level (0.0-1.0)</param>
        /// <returns>Interpolated color with opacity</returns>
        Color GetColorForHealth(float healthValue, float opacity);

        /// <summary>
        /// Gets the health category for a health value.
        /// </summary>
        /// <param name="healthValue">Health value (0-100)</param>
        /// <returns>Resolved health category</returns>
        HealthCategory GetCategoryForHealth(float healthValue);

        /// <summary>
        /// Sets the base colors for each category.
        /// Called when configuration changes.
        /// </summary>
        /// <param name="poorColor">Color for Poor category boundary</param>
        /// <param name="moderateColor">Color for Moderate category boundary</param>
        /// <param name="healthyColor">Color for Healthy category boundary</param>
        void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor);

        /// <summary>
        /// Invalidates the color cache.
        /// Called when category colors change.
        /// </summary>
        void InvalidateCache();
    }
}
