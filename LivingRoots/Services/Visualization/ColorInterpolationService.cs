using System;
using System.Threading;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes color mappings from health values using linear RGB interpolation.
    /// Uses category thresholds with half-open intervals: Poor [1, 34), Moderate [34, 67), Healthy [67, 100].
    /// </summary>
    public class ColorInterpolationService : IColorInterpolationService
    {
        private Color _poorColor = ModConstants.PoorColor;
        private Color _moderateColor = ModConstants.ModerateColor;
        private Color _healthyColor = ModConstants.HealthyColor;
        private Color _unknownColor = ModConstants.UnknownColor;
        private readonly Dictionary<(int health, int opacity), Color> _cache = new();
        private readonly object _cacheLock = new();

        // Category-palette generation (FR-010, T164): advances
        // whenever SetCategoryColors swaps the palette. The
        // OverlayRenderer keys its overlay-list cache on it, so
        // a palette-only configuration change invalidates the
        // cached overlay list — a colors-only edit is visible on
        // the next frame without a camera move or a health
        // mutation.
        private long _paletteGeneration;

        /// <inheritdoc />
        public long PaletteGeneration
        {
            get
            {
                return Volatile.Read(ref _paletteGeneration);
            }
        }

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue)
        {
            return GetColorForHealth(healthValue, 1.0f);
        }

        /// <inheritdoc />
        public Color GetColorForHealth(float healthValue, float opacity)
        {
            // Opacity clamps to the valid 0.0-1.0 range before it
            // reaches the alpha channel or the cache key (FR-008,
            // T118): an out-of-range opacity used to overflow the
            // checked alpha conversion and the cache-key cast, and
            // the clamp makes the resolution deterministic for any
            // input opacity. Math.Clamp passes NaN through (every
            // NaN comparison is false), so a NaN opacity is
            // replaced by the default first: the same FR-006
            // fallback the configuration service applies to a
            // persisted NaN opacity, applied here as well because
            // the T146 opacity property consults this method
            // directly and a raw NaN would overflow the checked
            // cache-key cast.
            if (float.IsNaN(opacity))
            {
                opacity = ModConstants.DefaultOpacity;
            }

            var clampedOpacity = Math.Clamp(opacity, 0f, 1f);

            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                // FR-003 / FR-014 (T165): the unknown-health
                // resolution is the CONFIGURED unknown color —
                // seeded from ModConstants.UnknownColor, the
                // default the configuration service falls back
                // to — with the opacity applied, so the
                // interpolation path and the overlay path
                // render absent data in the same color.
                var unknown = _unknownColor;
                unknown.A = (byte)(clampedOpacity * 255f);
                return unknown;
            }

            var clamped = Math.Clamp(healthValue, 0f, 100f);
            var key = ((int)(clamped * 100), (int)(clampedOpacity * 100));

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var cached))
                    return cached;

                var color = InterpolateColor(clamped, clampedOpacity);
                _cache[key] = color;
                return color;
            }
        }

        /// <inheritdoc />
        public HealthCategory GetCategoryForHealth(float healthValue)
        {
            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return HealthCategory.Unknown;
            }

            // Finite values clamp to the valid soil-health range
            // before the category intervals resolve (FR-008, SC-007,
            // T118): an out-of-range producer value renders in its
            // boundary category (0 → Poor, 100 → Healthy), never
            // Unknown — only non-finite values are the unknown state.
            var clamped = Math.Clamp(healthValue, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);

            // T072 / FR-010: the Poor interval is floored
            // at 1 — Poor covers [1, 34), never 0. Health
            // 0 is the unrendered neutral-dirt baseline (the
            // overlay renderer omits it, FR-009), so the
            // measured range the intervals resolve starts at
            // 1; a sub-floor reading (0 after the clamp)
            // resolves at the Poor floor rather than inside
            // the band.
            var intervalValue = Math.Max(clamped, 1f);

            if (intervalValue < ModConstants.PoorModerateBandEdge)
            {
                return HealthCategory.Poor;
            }

            if (intervalValue < ModConstants.ModerateHealthyBandEdge)
            {
                return HealthCategory.Moderate;
            }

            return HealthCategory.Healthy;
        }

        /// <inheritdoc />
        public void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor)
        {
            lock (_cacheLock)
            {
                _poorColor = poorColor;
                _moderateColor = moderateColor;
                _healthyColor = healthyColor;

                // F7 / FR-003 / FR-014 (T186): a palette-only swap
                // cannot strand a stale unknown color. The 3-arg call
                // carries no unknown color of its own, so the only
                // non-stale value it can land is the canonical
                // ModConstants fallback — the same default the
                // configuration-validation path applies (FR-006
                // ValidateColor) when no custom color is configured.
                // Leaving _unknownColor untouched here would carry a
                // custom color an earlier 4-arg application set
                // across the swap, and the unknown-health
                // interpolation would keep rendering that stale
                // value until the next 4-arg change.
                _unknownColor = ModConstants.UnknownColor;

                Interlocked.Increment(ref _paletteGeneration);
            }

            InvalidateCache();
        }

        /// <inheritdoc />
        public void SetCategoryColors(Color poorColor, Color moderateColor, Color healthyColor, Color unknownColor)
        {
            lock (_cacheLock)
            {
                _poorColor = poorColor;
                _moderateColor = moderateColor;
                _healthyColor = healthyColor;
                _unknownColor = unknownColor;
                Interlocked.Increment(ref _paletteGeneration);
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

            if (healthValue < ModConstants.PoorModerateBandEdge)
            {
                baseColor = _poorColor;
                t = healthValue / ModConstants.PoorModerateBandEdge;
                return LerpColor(baseColor, _moderateColor, t, opacity);
            }
            else if (healthValue < ModConstants.ModerateHealthyBandEdge)
            {
                baseColor = _moderateColor;
                t = (healthValue - ModConstants.PoorModerateBandEdge) / ModConstants.ModerateBandWidth;
                return LerpColor(baseColor, _healthyColor, t, opacity);
            }
            else
            {
                // The Moderate→Healthy ramp completes at
                // the 67 boundary — t = 1 at the top of the
                // Moderate band — so the Healthy band
                // [67, 100] is the solid Healthy pole
                // (FR-008, T161): health 67 renders exactly
                // the Healthy pole, and the band never
                // re-ramps back toward Moderate.
                baseColor = _healthyColor;
                return LerpColor(baseColor, baseColor, 0f, opacity);
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
