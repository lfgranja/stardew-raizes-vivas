using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;

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
        /// Sets the base colors for each category, including
        /// the unknown-health color. Called when configuration changes.
        /// </summary>
        /// <param name="poorColor">Color for Poor category boundary</param>
        /// <param name="moderateColor">Color for Moderate category boundary</param>
        /// <param name="healthyColor">Color for Healthy category boundary</param>
        /// <param name="unknownColor">Color for unknown (non-finite) health</param>
        void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor, Color unknownColor);

        /// <summary>
        /// Invalidates the color cache.
        /// Called when category colors change.
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// Gets a generation counter that advances whenever the
        /// category palette changes through either
        /// <see cref="SetCategoryColors"/> overload (FR-010, T164).
        /// Consumers that cache palette-derived output — the
        /// <c>OverlayRenderer</c> overlay list — key their cache
        /// on it, so a palette-only configuration change
        /// invalidates the cached overlay list and the next frame
        /// serves the new palette's colors without a camera move,
        /// a health mutation, or a manual refresh.
        /// </summary>
        long PaletteGeneration { get; }
    }
}
