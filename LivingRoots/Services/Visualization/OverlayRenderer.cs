using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes and caches tile overlays for visible tiles within a viewport.
    /// Implements viewport culling (FR-009), overlay list caching (FR-010),
    /// graceful degradation when tile count exceeds thresholds (FR-009),
    /// and zero-tile no-op behavior (FR-021).
    /// </summary>
    public class OverlayRenderer(
        IColorInterpolationService colorService,
        IVisualizationConfigurationService configService,
        IMonitor monitor)
    {
        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        // Threshold at which graceful degradation activates (FR-009)
        private const int DegradationTileThreshold = 1000;

        // Overlay list cache (FR-010)
        private List<TileOverlay>? _cachedOverlays;
        private Rectangle _cachedViewport;
        private readonly object _cacheLock = new();

        // Tracks whether degradation notification has been sent (one-time per degradation event)
        private bool _degradationNotificationSent;

        /// <summary>
        /// Gets overlays for tiles visible in the specified viewport.
        /// Performs viewport culling to only include tiles within viewport bounds (FR-009),
        /// applies configuration opacity, and implements graceful degradation when
        /// visible tile count exceeds 1,000 in auto mode (FR-009).
        /// Returns an empty list when <paramref name="tileHealthData"/> is empty (FR-021).
        /// </summary>
        /// <param name="viewport">Current viewport bounds in tile coordinates.</param>
        /// <param name="tileHealthData">Dictionary mapping tile positions to health values.</param>
        /// <returns>List of tile overlays for visible tiles, or empty list if no tiles.</returns>
        public List<TileOverlay> GetOverlays(Rectangle viewport, Dictionary<Point, float> tileHealthData)
        {
            // FR-021: zero tiles = no-op with minimal resource usage, no cache allocation
            if (tileHealthData == null || tileHealthData.Count == 0)
            {
                return new List<TileOverlay>();
            }

            // Check cache (FR-010): return cached result if viewport unchanged
            lock (_cacheLock)
            {
                if (_cachedOverlays != null && _cachedViewport == viewport)
                {
                    return _cachedOverlays;
                }
            }

            // Viewport culling (FR-009): filter to tiles within viewport bounds
            var visibleTiles = CullToViewport(tileHealthData, viewport);
            var visibleCount = visibleTiles.Count;

            // Read configuration for opacity and pattern settings
            var config = _configService.GetConfiguration();

            // Graceful degradation (FR-009): disable patterns when >1000 tiles in auto mode
            var degradationMode = config.AccessibilityDegradation ?? "auto";
            var showPatterns = config.ShowPatterns;

            if (visibleCount > DegradationTileThreshold &&
                string.Equals(degradationMode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                if (!_degradationNotificationSent)
                {
                    _monitor.Log(
                        $"Performance degradation activated: {visibleCount} visible tiles exceeds {DegradationTileThreshold} threshold. " +
                        "Disabling accessibility patterns per FR-009.",
                        LogLevel.Warn);
                    _degradationNotificationSent = true;
                }
                showPatterns = false;
            }
            else if (_degradationNotificationSent && visibleCount <= DegradationTileThreshold)
            {
                // Reset notification flag when tile count drops back within threshold
                _degradationNotificationSent = false;
                _monitor.Log("Performance degradation deactivated: visible tile count within acceptable range.", LogLevel.Trace);
            }

            // Build overlay list from visible tiles
            var overlays = BuildOverlays(visibleTiles, config.Opacity, showPatterns);

            // Update cache (FR-010)
            lock (_cacheLock)
            {
                _cachedOverlays = overlays;
                _cachedViewport = viewport;
            }

            return overlays;
        }

        /// <summary>
        /// Invalidates the overlay cache, forcing recalculation on the next <see cref="GetOverlays"/> call.
        /// Called when soil health data changes (FR-010).
        /// </summary>
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedOverlays = null;
                _cachedViewport = Rectangle.Empty;
            }
            _monitor.Log("Overlay renderer cache invalidated.", LogLevel.Trace);
        }

        /// <summary>
        /// Filters tile health data to only include tiles within the viewport bounds.
        /// Implements viewport culling per FR-009.
        /// </summary>
        private static List<KeyValuePair<Point, float>> CullToViewport(
            Dictionary<Point, float> tileHealthData, Rectangle viewport)
        {
            var result = new List<KeyValuePair<Point, float>>();
            foreach (var kvp in tileHealthData)
            {
                if (viewport.Contains(kvp.Key))
                {
                    result.Add(kvp);
                }
            }
            return result;
        }

        /// <summary>
        /// Builds tile overlays from visible tile data, applying color interpolation,
        /// opacity, and pattern assignment per configuration.
        /// </summary>
        private List<TileOverlay> BuildOverlays(
            List<KeyValuePair<Point, float>> visibleTiles, float opacity, bool showPatterns)
        {
            var overlays = new List<TileOverlay>(visibleTiles.Count);

            foreach (var kvp in visibleTiles)
            {
                var tilePos = kvp.Key;
                var healthValue = kvp.Value;

                // FR-014: render Unknown color when health data is unavailable
                if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                {
                    var unknownColor = new Color(
                        ModConstants.UnknownColor.R,
                        ModConstants.UnknownColor.G,
                        ModConstants.UnknownColor.B,
                        (byte)(opacity * 255f));
                    overlays.Add(new TileOverlay(tilePos, unknownColor, PatternType.None, healthValue, HealthCategory.Unknown));
                    continue;
                }

                var category = _colorService.GetCategoryForHealth(healthValue);
                var color = _colorService.GetColorForHealth(healthValue, opacity);
                var pattern = showPatterns ? GetPatternForCategory(category) : PatternType.None;

                overlays.Add(new TileOverlay(tilePos, color, pattern, healthValue, category));
            }

            return overlays;
        }

        /// <summary>
        /// Maps a health category to its accessibility pattern type per FR-001.
        /// Stripes for Poor, Dots for Moderate, Solid for Healthy, None for Unknown.
        /// </summary>
        private static PatternType GetPatternForCategory(HealthCategory category)
        {
            return category switch
            {
                HealthCategory.Poor => PatternType.Stripes,
                HealthCategory.Moderate => PatternType.Dots,
                HealthCategory.Healthy => PatternType.Solid,
                _ => PatternType.None
            };
        }
    }
}
