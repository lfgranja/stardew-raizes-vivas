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
        IMonitor monitor,
        IHudNotifier? hudNotifier = null)
    {
        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        /// <summary>
        /// T069: seam through which the one-time accessibility degradation
        /// notice (FR-009) reaches the player. Optional so existing test
        /// doubles keep constructing the renderer with three arguments.
        /// </summary>
        private readonly IHudNotifier? _hudNotifier = hudNotifier;

        // Overlay list cache (FR-010)
        private List<TileOverlay>? _cachedOverlays;
        private Rectangle _cachedViewport;
        // T143/FR-011: the snapshot the cache was built from, so a data change
        // invalidates even when the viewport is unchanged.
        private Dictionary<Point, float>? _cachedData;
        // T253/FR-010: the configuration the cache was built from, so a
        // configuration change invalidates even when the viewport and the
        // data are unchanged.
        private float _cachedOpacity;
        private bool _cachedShowPatterns;
        private string? _cachedDegradationMode;
        private readonly object _cacheLock = new();

        // Tracks whether degradation notification has been sent (one-time per degradation event)
        private bool _degradationNotificationSent;

        /// <summary>
        /// Indicates whether the overlay cache currently holds no entries.
        /// Exposed for diagnostics and tests verifying cache invalidation (FR-010).
        /// </summary>
        public bool IsCacheEmpty
        {
            get
            {
                lock (_cacheLock)
                {
                    return _cachedOverlays == null || _cachedOverlays.Count == 0;
                }
            }
        }

        /// <summary>
        /// Determines whether accessibility patterns should be suppressed for the
        /// given visible tile count and degradation mode (FR-009).
        /// </summary>
        /// <param name="visibleCount">Number of tiles visible in the viewport.</param>
        /// <param name="mode">Configured degradation mode ("auto", "never", or "notify").</param>
        /// <returns>True when patterns must be disabled due to degradation.</returns>
        public static bool CheckDegradation(int visibleCount, string? mode)
        {
            if (!string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return visibleCount > ModConstants.DegradationTileThreshold;
        }

        /// <summary>
        /// Discards the cached overlay list without logging. Alias for
        /// <see cref="InvalidateCache"/> used when callers know the cache is already warm.
        /// </summary>
        public void ForceRefresh()
        {
            lock (_cacheLock)
            {
                _cachedOverlays = null;
                _cachedViewport = Rectangle.Empty;
                _cachedData = null;
                _cachedOpacity = 0f;
                _cachedShowPatterns = false;
                _cachedDegradationMode = null;
            }
        }

        /// <summary>
        /// Gets overlays for tiles visible in the specified viewport.
        /// Performs viewport culling to only include tiles within viewport bounds (FR-009),
        /// applies configuration opacity, and implements graceful degradation when
        /// visible tile count exceeds 1,000 in auto mode (FR-009).
        /// Returns an empty list when <paramref name="tileHealthData"/> is empty (FR-021).
        /// </summary>
        /// <param name="viewport">Current viewport bounds in tile coordinates.</param>
        /// <param name="tileHealthData">Dictionary mapping tile positions to health values.</param>
        /// <param name="mapBounds">
        /// Optional map extent in tile units (FR-018). When supplied, culling is
        /// clamped to the map so tiles outside it are never overlaid; when null,
        /// the renderer keeps its origin-clamp-only behaviour.
        /// </param>
        /// <returns>List of tile overlays for visible tiles, or empty list if no tiles.</returns>
        public List<TileOverlay> GetOverlays(Rectangle viewport, Dictionary<Point, float> tileHealthData, Rectangle? mapBounds = null)
        {
            // FR-021: zero tiles = no-op with minimal resource usage, no cache allocation
            if (tileHealthData == null || tileHealthData.Count == 0)
            {
                return new List<TileOverlay>();
            }

            // Check cache (FR-010): return cached result only when the
            // viewport, the tile-health data AND the configuration are
            // unchanged. Keying on the viewport alone serves a stale
            // overlay set after the data changes (FR-011: the display
            // must not report a value it cannot draw, and must not
            // linger on one it should no longer draw), and keying
            // without the configuration serves one after an opacity,
            // palette or pattern change. The configuration is read
            // before the check so the comparison observes it.
            var config = _configService.GetConfiguration();
            lock (_cacheLock)
            {
                if (_cachedOverlays != null
                    && _cachedViewport == viewport
                    && DataEqual(_cachedData, tileHealthData)
                    && ConfigEqual(_cachedOpacity, _cachedShowPatterns, _cachedDegradationMode, config))
                {
                    return _cachedOverlays;
                }
            }

            // Viewport culling (FR-009): filter to tiles within viewport bounds,
            // clamped to the map extent when one was supplied (FR-018).
            var cullBounds = ComputeCullingBounds(viewport, mapBounds);
            var visibleTiles = CullToViewport(tileHealthData, cullBounds);
            var visibleCount = visibleTiles.Count;

            // Graceful degradation (FR-009): disable patterns when >1000 tiles in auto mode
            var degradationMode = config.AccessibilityDegradation ?? "auto";
            var showPatterns = config.ShowPatterns;

            if (visibleCount > ModConstants.DegradationTileThreshold &&
                string.Equals(degradationMode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                if (!_degradationNotificationSent)
                {
                    _monitor.Log(
                        $"Performance degradation activated: {visibleCount} visible tiles exceeds {ModConstants.DegradationTileThreshold} threshold. " +
                        "Disabling accessibility patterns per FR-009.",
                        LogLevel.Warn);

                    // FR-009: the player gets one native notice per degradation
                    // event, through the injected seam — no bespoke UI (FR-008a).
                    _hudNotifier?.NotifyAccessibilityPatternsDegraded(visibleCount);
                    _degradationNotificationSent = true;
                }
                showPatterns = false;
            }
            else if (_degradationNotificationSent && visibleCount <= ModConstants.DegradationTileThreshold)
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
                _cachedData = tileHealthData;
                _cachedOpacity = config.Opacity;
                _cachedShowPatterns = config.ShowPatterns;
                _cachedDegradationMode = config.AccessibilityDegradation;
            }

            return overlays;
        }

        /// <summary>
        /// S1244: float equality within <see cref="ModConstants.FloatComparisonEpsilon"/>
        /// instead of by exact value. The non-finite sentinels keep their exact
        /// semantics — NaN (the unmeasurable marker) equals only NaN, and each
        /// infinity equals only itself — because the range check alone cannot
        /// express them: <c>Math.Abs(NaN - x)</c> and <c>Math.Abs(∞ - ∞)</c> are
        /// both NaN, and every NaN comparison is false, so a range-only check
        /// would treat an Unknown frame as unchanged and redraw it forever
        /// (FR-011 forbids exactly that stale report).
        /// </summary>
        private static bool ApproximatelyEqual(float left, float right)
        {
            if (!float.IsFinite(left) || !float.IsFinite(right))
            {
                return float.IsNaN(left) == float.IsNaN(right)
                    && float.IsPositiveInfinity(left) == float.IsPositiveInfinity(right)
                    && float.IsNegativeInfinity(left) == float.IsNegativeInfinity(right);
            }

            return Math.Abs(left - right) <= ModConstants.FloatComparisonEpsilon;
        }

        /// <summary>
        /// T143/FR-011: content comparison of the snapshot the cache was built
        /// from against the one just received. Two frames with the same viewport
        /// but different tile data are NOT cache hits — the second frame would
        /// otherwise redraw the first frame's values, which is exactly the
        /// fabricated/stale report FR-011 forbids.
        /// </summary>
        private static bool DataEqual(Dictionary<Point, float>? cached, Dictionary<Point, float> incoming)
        {
            if (cached == null)
            {
                return false;
            }

            if (ReferenceEquals(cached, incoming))
            {
                return true;
            }

            if (cached.Count != incoming.Count)
            {
                return false;
            }

            foreach (var kvp in cached)
            {
                if (!incoming.TryGetValue(kvp.Key, out var value)
                    || !ApproximatelyEqual(kvp.Value, value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// T253/FR-010: compares the configuration snapshot the cache was
        /// built from against the configuration just read. A frame whose
        /// opacity, pattern toggle or degradation mode changed is NOT a
        /// cache hit — it would otherwise redraw the previous
        /// configuration's tints, which is exactly the stale report
        /// FR-010 forbids.
        /// </summary>
        private static bool ConfigEqual(
            float cachedOpacity,
            bool cachedShowPatterns,
            string? cachedDegradationMode,
            VisualizationConfiguration incoming)
        {
            if (!ApproximatelyEqual(cachedOpacity, incoming.Opacity))
            {
                return false;
            }

            if (cachedShowPatterns != incoming.ShowPatterns)
            {
                return false;
            }

            return string.Equals(cachedDegradationMode, incoming.AccessibilityDegradation, StringComparison.Ordinal);
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
                _cachedData = null;
                _cachedOpacity = 0f;
                _cachedShowPatterns = false;
                _cachedDegradationMode = null;
            }
            _monitor.Log("Overlay renderer cache invalidated.", LogLevel.Trace);
        }

        /// <summary>
        /// Computes the effective culling rectangle: the viewport, one-tile
        /// margin on all four edges, origin-clamped at zero, and clamped to the
        /// map extent when one was supplied (FR-018). A null or empty map extent
        /// leaves the rectangle margin-extended only.
        /// </summary>
        private static Rectangle ComputeCullingBounds(Rectangle viewport, Rectangle? mapBounds)
        {
            // End bounds widen to long before the addition so an extreme
            // viewport cannot trip the project-wide overflow check.
            var startX = Math.Max(0, viewport.X - 1);
            var startY = Math.Max(0, viewport.Y - 1);
            var endX = Math.Min((long)viewport.X + viewport.Width + 1, mapBounds?.Right ?? long.MaxValue);
            var endY = Math.Min((long)viewport.Y + viewport.Height + 1, mapBounds?.Bottom ?? long.MaxValue);

            return new Rectangle(
                startX,
                startY,
                (int)Math.Max(0, endX - startX),
                (int)Math.Max(0, endY - startY));
        }

        /// <summary>
        /// Filters tile health data to only include tiles within the culling bounds.
        /// Implements viewport culling per FR-009.
        /// </summary>
        private static List<KeyValuePair<Point, float>> CullToViewport(
            Dictionary<Point, float> tileHealthData, Rectangle bounds)
        {
            var result = new List<KeyValuePair<Point, float>>();
            foreach (var kvp in tileHealthData)
            {
                if (bounds.Contains(kvp.Key))
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

                // FR-011 / T143: drawable-only reporting. An unmeasurable tile
                // (NaN/inf) carries no drawable value — it is reported as
                // Unknown and tinted with the unknown color, never a fabricated
                // percentage. The overlay is still produced so the draw path can
                // report the Unknown label (the value observable is what FR-011
                // governs; the tint is the unknown color, which is honest).
                if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                {
                    var unknownColor = ModConstants.UnknownColor;
                    unknownColor.A = (byte)(opacity * 255f);
                    overlays.Add(new TileOverlay(tilePos, unknownColor, PatternType.None, float.NaN, HealthCategory.Unknown));
                    continue;
                }

                // T248 (FR-009/FR-010, F2): baseline soil (health 0)
                // renders as neutral vanilla dirt with no overlay, so a
                // zero reading is skipped outright and never reaches
                // the draw path. NaN/inf still render the Unknown
                // overlay above; every other finite value draws.
                if (healthValue == 0f)
                {
                    continue;
                }

                // FR-009/T178: the Poor interval floor at 1 applies to the DRAWN
                // tint (the floor keeps the lowest drawn value non-zero),
                // never to the reported value: FR-011 forbids reporting a
                // value the draw did not carry, so a sub-floor reading is
                // reported exactly as measured.
                var drawnHealth = Math.Max(healthValue, 1f);

                var category = _colorService.GetCategoryForHealth(drawnHealth);
                var color = _colorService.GetColorForHealth(drawnHealth, opacity);
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
