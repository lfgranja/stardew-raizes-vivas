# Contract: IColorInterpolationService

**Domain**: LivingRoots.Domain
**Implementation**: LivingRoots.Services.Visualization.ColorInterpolationService

## Interface Definition

```csharp
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
        /// Invalidates the color cache.
        /// Called when category colors change.
        /// </summary>
        void InvalidateCache();
    }
}
```

## Preconditions
- Health values outside [0, 100] are clamped per ModConstants
- Category colors must be set before GetColorForHealth is called
- Opacity values outside [0.0, 1.0] are clamped

## Postconditions
- Returned colors have valid RGBA values (0-255 each channel)
- Interpolation follows linear RGB space (not HSL/HSV)
- Cache invalidation clears all cached colors

## Interpolation Rules

### Category Boundaries
| Range | Category A | Category B | Interpolation |
|-------|------------|------------|---------------|
| [0, 34) | Poor (0) | Moderate (34) | Lerp(Poor, Moderate, t) where t = value / 34 |
| [34, 67) | Moderate (34) | Healthy (67) | Lerp(Moderate, Healthy, t) where t = (value - 34) / 33 |
| [67, 100] | Healthy | Healthy | Solid Healthy color |

### Unknown Values
- NaN, Infinity, or missing data returns Unknown color (gray)

## Performance Constraints
- Color lookup should be O(1) with caching
- Cache size bounded to prevent memory growth
