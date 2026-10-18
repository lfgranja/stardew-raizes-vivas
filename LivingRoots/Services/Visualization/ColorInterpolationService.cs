using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes color mappings from health values using linear RGB interpolation.
    /// Uses category thresholds with ROADMAP.md intervals: Poor [0, 30], Moderate [31, 70], Healthy [71, 100].
    /// </summary>
    public class ColorInterpolationService : IColorInterpolationService
    {
        private Color _poorColor = ModConstants.PoorColor;
        private Color _moderateColor = ModConstants.ModerateColor;
        private Color _healthyColor = ModConstants.HealthyColor;
        private Color _unknownColor = ModConstants.UnknownColor;
        private readonly Dictionary<(int health, int opacity), Color> _cache = new();
        private readonly object _cacheLock = new();

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue)
        {
            return GetColorForHealth(healthValue, 1.0f);
        }

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue, float opacity)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                var unknown = _unknownColor;
                unknown.A = (byte)(opacity * 255f);
                return unknown;
            }

            var clamped = Math.Clamp(healthValue, 0f, 100f);
            var key = ((int)(clamped * 100), (int)(opacity * 100));

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var cached))
                    return cached;

                var color = InterpolateColor(clamped, opacity);
                _cache[key] = color;
                return color;
            }
        }

        /// <inheritdoc />
        public HealthCategory GetCategoryForHealth(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                return HealthCategory.Unknown;

            return healthValue switch
            {
                >= 0 and <= 30 => HealthCategory.Poor,
                >= 31 and <= 70 => HealthCategory.Moderate,
                >= 71 and <= 100 => HealthCategory.Healthy,
                _ => HealthCategory.Unknown
            };
        }

        /// <inheritdoc />
        public void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor, Color unknownColor)
        {
            lock (_cacheLock)
            {
                _poorColor = poorColor;
                _moderateColor = moderateColor;
                _healthyColor = healthyColor;
                // T165 (FR-003/FR-014): the configured unknown color reaches
                // the interpolator too, so an unmeasurable tile renders in the
                // player's chosen unknown color rather than the default.
                _unknownColor = unknownColor;
            }

            InvalidateCache();
        }

        /// <inheritdoc />
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
            }
        }

        private Color InterpolateColor(float healthValue, float opacity)
        {
            Color baseColor;
            float t;

            if (healthValue <= 30f)
            {
                baseColor = _poorColor;
                t = healthValue / 30f;
                return LerpColor(baseColor, _moderateColor, t, opacity);
            }
            else if (healthValue <= 70f)
            {
                baseColor = _moderateColor;
                t = (healthValue - 31f) / 39f;
                return LerpColor(baseColor, _healthyColor, t, opacity);
            }
            else
            {
                // T253 (FR-010, F7): the Healthy tier holds the healthy
                // color flat. Re-lerping moderate-to-healthy here renders
                // 66 near-green then snaps back to moderate at 67, a
                // backwards step; the flat hold keeps every channel the
                // tier-2 endpoint reached, so the palette is monotone
                // non-decreasing across the whole 0-100 range.
                var healthy = _healthyColor;
                healthy.A = (byte)(opacity * 255f);
                return healthy;
            }
        }

        private static Color LerpColor(Color from, Color to, float t, float opacity)
        {
            var r = (byte)(from.R + (to.R - from.R) * t);
            var g = (byte)(from.G + (to.G - from.G) * t);
            var b = (byte)(from.B + (to.B - from.B) * t);
            var a = (byte)(opacity * 255f);
            return new Color(r, g, b, a);
        }
    }
}
