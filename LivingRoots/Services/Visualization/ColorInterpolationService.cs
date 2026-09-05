using System.Globalization;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes color mappings from health values using linear RGB interpolation.
    /// Supports caching for performance. Category thresholds use half-open intervals:
    /// Poor [0, 34), Moderate [34, 67), Healthy [67, 100].
    /// </summary>
    public class ColorInterpolationService(IMonitor monitor) : IColorInterpolationService
    {
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        // Category boundary constants (half-open intervals)
        private const float PoorBoundary = 0f;
        private const float ModerateBoundary = 34f;
        private const float HealthyBoundary = 67f;
        private const float MaxBoundary = 100f;

        // Rounding precision for cache keys (reduces variations)
        private const int CacheRoundingDigits = 2;

        // Base colors for each category boundary
        private Color _poorColor = new Color(255, 0, 0, 255);
        private Color _moderateColor = new Color(255, 255, 0, 255);
        private Color _healthyColor = new Color(0, 255, 0, 255);
        private Color _unknownColor = new Color(128, 128, 128, 255);

        // Cache keyed by rounded health value
        private readonly Dictionary<float, Color> _colorCache = new();

        // Lock for thread-safe cache access
        private readonly object _cacheLock = new();

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue)
        {
            // Handle NaN/Infinity gracefully
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                _monitor.Log($"Invalid health value ({healthValue}) encountered; returning Unknown category color.", LogLevel.Trace);
                return _unknownColor;
            }

            // Clamp to valid range
            var clamped = Math.Clamp(healthValue, PoorBoundary, MaxBoundary);

            // Round for cache key
            var cacheKey = RoundForCache(clamped);

            // Check cache
            lock (_cacheLock)
            {
                if (_colorCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }
            }

            // Compute interpolated color
            var color = InterpolateColor(clamped);

            // Store in cache
            lock (_cacheLock)
            {
                _colorCache[cacheKey] = color;
            }

            return color;
        }

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue, float opacity)
        {
            var color = GetColorForHealth(healthValue);

            // If unknown color was returned, don't modify opacity
            if (color == _unknownColor)
            {
                return color;
            }

            // Clamp opacity to valid range
            var clampedOpacity = Math.Clamp(opacity, 0f, 1f);
            color.A = (byte)(clampedOpacity * 255f);
            return color;
        }

        /// <inheritdoc />
        public HealthCategory GetCategoryForHealth(float healthValue)
        {
            // Handle NaN/Infinity gracefully
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return HealthCategory.Unknown;
            }

            // Clamp to valid range
            var clamped = Math.Clamp(healthValue, PoorBoundary, MaxBoundary);

            // Half-open intervals: Poor [0, 34), Moderate [34, 67), Healthy [67, 100]
            if (clamped < ModerateBoundary)
            {
                return HealthCategory.Poor;
            }

            if (clamped < HealthyBoundary)
            {
                return HealthCategory.Moderate;
            }

            return HealthCategory.Healthy;
        }

        /// <inheritdoc />
        public void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor)
        {
            _poorColor = poorColor;
            _moderateColor = moderateColor;
            _healthyColor = healthyColor;

            // Invalidate cache since base colors changed
            InvalidateCache();

            _monitor.Log("Category colors updated and cache invalidated.", LogLevel.Trace);
        }

        /// <inheritdoc />
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _colorCache.Clear();
            }

            _monitor.Log("Color interpolation cache invalidated.", LogLevel.Trace);
        }

        /// <summary>
        /// Performs linear RGB interpolation based on health value within category ranges.
        /// At boundaries: health=0 returns PoorColor, health=100 returns HealthyColor.
        /// </summary>
        private Color InterpolateColor(float health)
        {
            // At exact upper boundary return HealthyColor
            if (health >= MaxBoundary)
            {
                return _healthyColor;
            }

            // At exact lower boundary return PoorColor
            if (health <= PoorBoundary)
            {
                return _poorColor;
            }

            Color fromColor;
            Color toColor;
            float rangeStart;
            float rangeEnd;

            if (health < ModerateBoundary)
            {
                // Poor range [0, 34): interpolate from Poor to Moderate
                fromColor = _poorColor;
                toColor = _moderateColor;
                rangeStart = PoorBoundary;
                rangeEnd = ModerateBoundary;
            }
            else if (health < HealthyBoundary)
            {
                // Moderate range [34, 67): interpolate from Moderate to Healthy
                fromColor = _moderateColor;
                toColor = _healthyColor;
                rangeStart = ModerateBoundary;
                rangeEnd = HealthyBoundary;
            }
            else
            {
                // Healthy range [67, 100): interpolate from Healthy to a brighter variant
                // At 100, returns HealthyColor directly (handled above)
                fromColor = _healthyColor;
                toColor = _healthyColor;
                rangeStart = HealthyBoundary;
                rangeEnd = MaxBoundary;
            }

            // Calculate interpolation factor (0.0 to 1.0)
            var range = rangeEnd - rangeStart;
            var t = range > 0 ? (health - rangeStart) / range : 0f;

            // Linear RGB interpolation
            var r = Lerp(fromColor.R, toColor.R, t);
            var g = Lerp(fromColor.G, toColor.G, t);
            var b = Lerp(fromColor.B, toColor.B, t);
            var a = Lerp(fromColor.A, toColor.A, t);

            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Linear interpolation between two byte values.
        /// </summary>
        private static byte Lerp(byte from, byte to, float t)
        {
            return (byte)Math.Clamp(from + (to - from) * t, 0f, 255f);
        }

        /// <summary>
        /// Rounds a float value for use as a cache key to reduce variations.
        /// </summary>
        private static float RoundForCache(float value)
        {
            return (float)Math.Round(value, CacheRoundingDigits, MidpointRounding.AwayFromZero);
        }
    }
}
