using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes color mappings from health values using linear RGB interpolation.
    /// Uses category thresholds with half-open intervals: Poor [0, 34), Moderate [34, 67), Healthy [67, 100].
    /// </summary>
    public class ColorInterpolationService : IColorInterpolationService
    {
        private Color _poorColor = ModConstants.PoorColor;
        private Color _moderateColor = ModConstants.ModerateColor;
        private Color _healthyColor = ModConstants.HealthyColor;
        private readonly Dictionary<int, Color> _cache = new();

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
                var unknown = ModConstants.UnknownColor;
                unknown.A = (byte)(opacity * 255f);
                return unknown;
            }

            var clamped = Math.Clamp(healthValue, 0f, 100f);
            var key = ((int)(clamped * 100)) | ((int)(opacity * 100) << 17);

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var color = InterpolateColor(clamped, opacity);
            _cache[key] = color;
            return color;
        }

        /// <inheritdoc />
        public HealthCategory GetCategoryForHealth(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                return HealthCategory.Unknown;

            return healthValue switch
            {
                >= 0 and < 34 => HealthCategory.Poor,
                >= 34 and < 67 => HealthCategory.Moderate,
                >= 67 and <= 100 => HealthCategory.Healthy,
                _ => HealthCategory.Unknown
            };
        }

        /// <inheritdoc />
        public void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor)
        {
            _poorColor = poorColor;
            _moderateColor = moderateColor;
            _healthyColor = healthyColor;
            InvalidateCache();
        }

        /// <inheritdoc />
        public void InvalidateCache()
        {
            _cache.Clear();
        }

        private Color InterpolateColor(float healthValue, float opacity)
        {
            Color baseColor;
            float t;

            if (healthValue < 34f)
            {
                baseColor = _poorColor;
                t = healthValue / 34f;
                return LerpColor(baseColor, _moderateColor, t, opacity);
            }
            else if (healthValue < 67f)
            {
                baseColor = _moderateColor;
                t = (healthValue - 34f) / 33f;
                return LerpColor(baseColor, _healthyColor, t, opacity);
            }
            else
            {
                baseColor = _healthyColor;
                t = (healthValue - 67f) / 33f;
                return LerpColor(_moderateColor, baseColor, t, opacity);
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
