using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes and caches tile overlays for visible tiles within a viewport.
    /// Implements viewport culling (FR-009), the 1-tile viewport margin and
    /// map-bounds clamping (FR-018), overlay list caching keyed on the
    /// viewport, the map bounds, the tile-health generation, the category
    /// palette generation, and the configuration values the list is built
    /// from (FR-010, T134, T164), graceful degradation when tile count
    /// exceeds thresholds (FR-009, decided once per render frame —
    /// T099 / Q2, FR-019, FR-020) with asymmetric hysteresis (degrade
    /// eagerly, recover lazily — T100 / Q6 / FR-009), the "notify"
    /// degradation mode's one-time toast plus one-time Warn log per
    /// degradation event (T136, FR-009), health 0 renders no
    /// overlay at all — the neutral vanilla-dirt baseline (T072,
    /// FR-009, FR-010), the configured unknown
    /// color for absent-data tiles (T165, FR-003, FR-014), and
    /// zero-tile no-op behavior (FR-021).
    ///
    /// Per-player render state — the overlay-list cache, the frame
    /// degradation decision, the degradation notification latch,
    /// the degradation frame epoch, and the call-site decision
    /// store — lives in SMAPI
    /// <c>PerScreen&lt;T&gt;</c> readonly fields (T127, T163, FR-026,
    /// DC-6), so every local player in split-screen keeps their own
    /// culling, degradation, and cache state: only the player whose
    /// viewport exceeds the threshold degrades, and no render state
    /// is shared across screens through a static.
    /// </summary>
    public class OverlayRenderer(
        IColorInterpolationService colorService,
        IVisualizationConfigurationService configService,
        IMonitor monitor,
        IHudNotifier hudNotifier)
    {
        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        // Player-facing HUD toast sink (FR-009). Required since T084:
        // a missing notifier is a compile-time error, so the FR-009
        // degradation notification can never silently revert to a
        // monitor-only log, which is invisible to the player
        // (WCAG 4.1.3 Status Messages).
        private readonly IHudNotifier _hudNotifier = hudNotifier ?? throw new ArgumentNullException(nameof(hudNotifier));

        // Overlay list cache (FR-010), one per local player (T127,
        // FR-026, DC-6). The cache entry is keyed on the viewport,
        // the map bounds, the tile-health generation, the category
        // palette generation, and the configuration values the list
        // is built from (T134, T164, FR-010): the generations are
        // what make the cache self-invalidating, so a tile-health
        // mutation, a palette change, or a configuration change
        // at an unchanged viewport rebuilds the list on the next
        // frame without manual refresh and without moving the
        // camera.
        private readonly PerScreen<List<TileOverlay>?> _cachedOverlays = new();
        private readonly PerScreen<Rectangle> _cachedViewport = new();
        private readonly PerScreen<Rectangle?> _cachedMapBounds = new();
        private readonly PerScreen<List<KeyValuePair<Point, float>>?> _cachedVisibleTiles = new();
        private readonly PerScreen<long> _cachedPaletteGeneration = new();
        private readonly PerScreen<float> _cachedOpacity = new();
        private readonly PerScreen<bool> _cachedShowPatterns = new();
        private readonly PerScreen<string?> _cachedDegradationMode = new();
        // T072 (FR-010): the configured unknown-tile color is
        // part of the cache key — a colors-only change of the
        // unknown color must invalidate the cached overlay list
        // so the next frame serves the new color without a
        // camera move or manual refresh.
        private readonly PerScreen<ColorDTO> _cachedUnknownColor = new();

        // Guards the compound cache check/store above. Held only around
        // the in-memory PerScreen reads and writes — never across I/O.
        private readonly object _cacheLock = new();

        // Tracks whether degradation notification has been sent (one-time
        // per degradation event; re-arms only at the lower hysteresis
        // band edge, not at the degrade threshold — T100 / Q6 / FR-009),
        // per local player (T127, FR-026, DC-6). The latch is shared by
        // "auto" and "notify" so both modes have identical notification
        // frequency by construction (T136, FR-009).
        private readonly PerScreen<bool> _degradationNotificationSent = new();

        // Once-per-render-frame degradation decision (T099 / Q2 /
        // FR-009, FR-019, FR-020), per local player (T127, FR-026,
        // DC-6). Under the production snapshot shape every
        // GetOverlays call IS a render frame boundary (T162 / F2):
        // ModController builds a NEW Dictionary<Point, float> on
        // every UpdateTicked tick and VisualizationService snapshots
        // that reference, so the same dictionary reference is never
        // re-presented twice — a per-GetOverlays-call epoch is the
        // only frame model that stays live in production. Every call
        // re-evaluates the decision from the presented snapshot's
        // culled visible count, so a threshold crossing takes effect
        // on the frame that presents it and the hysteresis latch
        // never freezes at a session's frame-1 values. The decision
        // carries asymmetric hysteresis (T100 / Q6 / FR-009): once
        // degraded it stays degraded until the visible count falls
        // to the lower band edge (ModConstants.DegradationRecovery-
        // Threshold), so the degraded state survives frames inside
        // the hysteresis band.
        private readonly PerScreen<bool> _frameDegraded = new();

        // The degradation mode the current frame STARTED under (T117,
        // FR-019, FR-020). The frame-start mode governs the frame's
        // degradation consequence — "auto" disables accessibility
        // patterns for the whole degraded frame, "notify" keeps every
        // pattern rendered and instead announces the event once (T136,
        // FR-009) — so a mid-frame mode reconfiguration never tears
        // the frame; the new mode applies at the next frame boundary.
        private readonly PerScreen<string?> _frameDegradationMode = new();

        // Frame-scoped degradation decisions for direct CheckDegradation
        // call sites (T099 / Q2 / FR-009), per local player (T127,
        // FR-026, DC-6, T163). Keyed by call site; each entry carries
        // the frame epoch its decision was established at, so each
        // caller observes the decision its first invocation between
        // render frames established, and a re-invocation with a
        // fresher count cannot tear the frame. The frame epoch
        // advances (making every stored decision stale at once)
        // whenever a render frame begins — every GetOverlays call
        // under the per-call frame model (T162) — so call sites
        // re-establish their decision on their next invocation.
        // The epoch advances per screen and the store is a lock-free
        // concurrent dictionary per screen: per-player isolation comes
        // from PerScreen<T> (no cross-screen leakage), while the short
        // shared _cacheLock critical section serializes cache access.
        private readonly PerScreen<ConcurrentDictionary<string, FrameDecision>> _callSiteFrameDecisions = new(() => new ConcurrentDictionary<string, FrameDecision>());
        private readonly PerScreen<long> _degradationFrameEpoch = new();

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
                    var cachedOverlays = _cachedOverlays.Value;
                    return cachedOverlays == null || cachedOverlays.Count == 0;
                }
            }
        }

        /// <summary>
        /// Determines whether accessibility patterns should be suppressed for
        /// the given visible tile count and degradation mode (FR-009).
        /// The decision is frame-scoped (T099 / Q2 / FR-009, FR-019,
        /// FR-020): the first invocation from a call site between render
        /// frames establishes the decision, and re-invocations from the
        /// same call site observe it, so re-evaluating with a fresher
        /// count cannot tear the frame with a mixed-state decision. The
        /// per-screen frame epoch advances whenever this
        /// <see cref="OverlayRenderer"/> begins a new render frame (every
        /// <see cref="GetOverlays"/> call under the per-call frame model,
        /// T162), so the decision is re-established on the call site's
        /// next invocation. The decision state is per local player
        /// (T163, FR-026, DC-6): one player's frame epoch never retires
        /// another player's stored decision. "auto" and "notify" both
        /// evaluate (they share the degradation state machine — T136,
        /// FR-009); "never" and unrecognized modes never degrade.
        /// </summary>
        /// <param name="visibleCount">Number of tiles visible in the viewport.</param>
        /// <param name="mode">Configured degradation mode ("auto", "never", or "notify").</param>
        /// <param name="callSite">Frame-scoped decision key; supplied by the compiler.</param>
        /// <returns>True when the visible count exceeds the degradation threshold.</returns>
        public bool CheckDegradation(
            int visibleCount,
            string? mode,
            [CallerMemberName] string? callSite = null)
        {
            if (!IsDegradationEvaluatedMode(mode))
            {
                return false;
            }

            var site = callSite ?? string.Empty;

            // Lock-free single decision path (T099 / Q2 / FR-009, FR-019,
            // FR-020): a call site observes the decision established for the
            // current per-screen frame epoch. A re-invocation between render
            // frames observes that established decision instead of tearing
            // the frame with the fresher count; the epoch advance at every
            // frame boundary makes every stored decision stale at once, so
            // each call site re-establishes its decision on its next
            // invocation.
            var frameEpoch = _degradationFrameEpoch.Value;
            if (_callSiteFrameDecisions.Value.TryGetValue(site, out var frameDecision)
                && frameDecision.FrameEpoch == frameEpoch)
            {
                return frameDecision.Decision;
            }

            var decision = visibleCount > ModConstants.DegradationTileThreshold;
            _callSiteFrameDecisions.Value[site] = new FrameDecision(frameEpoch, decision);
            return decision;
        }

        /// <summary>
        /// Advances the per-screen degradation frame epoch: begins a new
        /// render frame. Every call-site decision stored for the previous
        /// epoch becomes stale at once, so each direct
        /// <see cref="CheckDegradation"/> call site re-establishes its
        /// decision on its next invocation (T099 / Q2 / FR-009). The epoch
        /// is per local player (T163, FR-026, DC-6) and advances without a
        /// lock, so concurrent frame boundaries from split-screen renderers
        /// never serialize on one another's state (T127).
        /// </summary>
        private void BeginDegradationFrame()
        {
            _degradationFrameEpoch.Value = _degradationFrameEpoch.Value + 1;
        }

        /// <summary>
        /// Determines whether the degradation mode participates in the
        /// degradation state machine (T136, FR-009): "auto" degrades and
        /// disables accessibility patterns, "notify" shares the state
        /// machine so both modes notify at identical frequency while every
        /// pattern stays rendered, and "never" — or any unrecognized mode —
        /// is the pure escape hatch that never degrades.
        /// </summary>
        /// <param name="mode">Configured degradation mode.</param>
        /// <returns>True when the mode evaluates degradation.</returns>
        private static bool IsDegradationEvaluatedMode(string? mode)
        {
            return string.Equals(mode, "auto", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "notify", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Discards the cached overlay list without logging. Alias for
        /// <see cref="InvalidateCache"/> used when callers know the cache is already warm.
        /// </summary>
        public void ForceRefresh()
        {
            lock (_cacheLock)
            {
                _cachedOverlays.Value = null;
                _cachedViewport.Value = Rectangle.Empty;
                _cachedMapBounds.Value = null;
                _cachedVisibleTiles.Value = null;
            }
        }

        /// <summary>
        /// Gets overlays for tiles visible in the specified viewport.
        /// Performs viewport culling to only include tiles within viewport bounds
        /// extended by the 1-tile margin and clamped to the map bounds
        /// (FR-009, FR-018), applies configuration opacity, and implements
        /// graceful degradation when visible tile count exceeds the
        /// threshold in auto mode (FR-009), with asymmetric hysteresis:
        /// degrade eagerly above the threshold, recover lazily at the
        /// lower band edge (T100 / Q6 / FR-009). The degradation decision
        /// is evaluated once per render frame — every call presents a
        /// render frame under the production snapshot shape, where each
        /// frame hands the renderer a fresh tile-health dictionary
        /// (T162 / F2, T099 / Q2 / FR-009, FR-019, FR-020) — and is
        /// shared by every consumer of that frame; in "notify" mode the
        /// same decision instead emits the one-time player-facing toast
        /// plus the one-time Warn log per degradation event while every
        /// accessibility pattern stays rendered (T136, FR-009). The
        /// overlay list is cached keyed on the viewport, the map bounds,
        /// the tile-health generation, the category-palette generation,
        /// and the configuration values the list is built from (T134,
        /// T164, FR-010), so a health, palette, or configuration change
        /// at an unchanged viewport rebuilds the list on the next frame.
        /// Returns an empty list when
        /// <paramref name="tileHealthData"/> is empty (FR-021).
        /// </summary>
        /// <param name="viewport">Current viewport bounds in tile coordinates.</param>
        /// <param name="tileHealthData">Dictionary mapping tile positions to health values.</param>
        /// <param name="mapBounds">
        /// Optional map bounds in tile units (Map.DisplayWidth/Height divided by
        /// ModConstants.TileSize). When supplied, the visible tile range is
        /// clamped to it; when omitted, only the map origin (0, 0) is enforced
        /// (FR-018).
        /// </param>
        /// <returns>List of tile overlays for visible tiles, or empty list if no tiles.</returns>
        public List<TileOverlay> GetOverlays(
            Rectangle viewport,
            Dictionary<Point, float> tileHealthData,
            Rectangle? mapBounds = null)
        {
            // FR-021: zero tiles = no-op with minimal resource usage, no cache allocation
            if (tileHealthData == null || tileHealthData.Count == 0)
            {
                return new List<TileOverlay>();
            }

            // Read configuration for opacity, pattern settings, and the
            // degradation mode. The configuration values the overlay list
            // is built from are part of the cache key (T134, FR-010): a
            // configuration change at an unchanged viewport invalidates
            // the cached list, so opacity and pattern edits are visible
            // without moving the camera.
            var config = _configService.GetConfiguration();
            var opacity = config.Opacity;

            // FR-006 / FR-008 (T118): an invalid configured opacity is
            // replaced by the default before it reaches the cache key or
            // the color service — the same fallback the configuration
            // service applies when it loads a persisted NaN opacity.
            // Math.Clamp passes NaN through (every NaN comparison is
            // false), so a raw NaN would overflow the color service's
            // checked cache-key cast, and — because NaN != NaN — miss
            // the cache-key comparison below on every frame: a perpetual
            // rebuild of the very redundant calculation the FR-010 cache
            // exists to avoid.
            if (float.IsNaN(opacity))
            {
                opacity = ModConstants.DefaultOpacity;
            }

            // Accessibility floor reference: ModConstants.PatternMinOpacity (0.7).
            // (The configured opacity is clamped by the color service; this
            // constant documents the minimum pattern opacity for FR-001.)

            var showPatterns = config.ShowPatterns;
            var degradationMode = config.AccessibilityDegradation ?? "auto";

            // The CONFIGURED pattern toggle is the cache-key
            // component (T134, T164, T175, FR-010): the
            // graceful-degradation override below rewrites the
            // working value for the frame's build, but the
            // override is a frame-scoped consequence of the
            // visible count and the degradation mode — both
            // already keyed (the count leads the tile-health
            // generation, the mode is keyed directly) — so
            // keying on the overridden value would miss the
            // cache on every frame of a degraded session: the
            // frame that populated the cache stored the
            // overridden value, and every later frame compares
            // the configured value against it. Equal keys still
            // imply equal built lists: the visible count that
            // produces the override fingerprints into the
            // generation, so a cache hit returns exactly the
            // list a rebuild would produce (still degraded,
            // patterns off — the T175 cache-hit frame shape).
            var configuredShowPatterns = showPatterns;

            // Category-palette generation (T164, FR-010): the
            // palette half of the cache key. The color
            // interpolation service advances its generation
            // whenever SetCategoryColors swaps the palette, so a
            // colors-only configuration change at an unchanged
            // viewport, opacity, and degradation mode
            // invalidates the cached overlay list and the next
            // frame serves the new palette's colors — without a
            // camera move, a health mutation, or a manual
            // refresh.
            var paletteGeneration = _colorService.PaletteGeneration;

            // FR-003 / FR-014 (T165): the unknown overlay
            // renders the CONFIGURED unknown color — the
            // configuration service defaults it to the
            // ModConstants neutral gray — not the constant
            // directly, so a custom unknown color reaches
            // the absent-data tiles.
            var unknownColor = config.UnknownColor;

            // Viewport culling (FR-009, FR-018): filter to tiles within the viewport
            // bounds extended by the 1-tile margin and clamped to the map bounds
            var cullingBounds = ComputeCullingBounds(viewport, mapBounds);
            var visibleTiles = CullToViewport(tileHealthData, cullingBounds);
            var visibleCount = visibleTiles.Count;

            // Tile-health snapshot (T134, FR-010, S1244): the data
            // half of the cache key is the visible tile-health
            // content, compared with the shared float tolerance so
            // sub-epsilon jitter (the same frame's float noise) is a
            // cache hit while a real mutation (a measurable change,
            // an added/removed tile, or a finite/non-finite flip)
            // invalidates the cached list. Exact equality and hash
            // fingerprints are both forbidden/unreliable here:
            // approximate comparison needs the epoch values, not a
            // hash of them.

            // Degradation frame boundary (T099 / Q2 / FR-009,
            // T162 / F2, T175): under the per-call frame model
            // every GetOverlays call IS a render frame boundary —
            // including the cache-hit frame, the common frame in
            // production, where the visible set and count carry
            // over and the content-based tile-health generation
            // (T134) fingerprints identically, so the overlay-list
            // cache (FR-010) serves the hit. The per-screen epoch
            // therefore advances BEFORE the cache early-return:
            // the advance is an O(1) counter bump that retires
            // every stored call-site decision at once, so each
            // direct CheckDegradation call site re-establishes its
            // decision on its next invocation — without forcing a
            // rebuild, because the cached overlay list was built
            // under the same visible set and count this frame
            // presents, so the degradation consequence it carries
            // is still this frame's decision.
            BeginDegradationFrame();

            // Check cache (FR-010): return the cached result when the
            // viewport, the map bounds, the tile-health content, the
            // palette generation, and the configuration values the
            // list was built from are all unchanged.
            lock (_cacheLock)
            {
                if (_cachedOverlays.Value is { } cachedOverlays
                    && _cachedViewport.Value == viewport
                    && _cachedMapBounds.Value == mapBounds
                    && VisibleTilesEqual(_cachedVisibleTiles.Value, visibleTiles)
                    && _cachedPaletteGeneration.Value == paletteGeneration
                    && ApproximatelyEqual(_cachedOpacity.Value, opacity)
                    && _cachedShowPatterns.Value == configuredShowPatterns
                    && string.Equals(_cachedDegradationMode.Value, degradationMode, StringComparison.Ordinal)
                    && _cachedUnknownColor.Value == unknownColor)
                {
                    return cachedOverlays;
                }
            }

            // Per-render-frame degradation decision (T099 / Q2 /
            // FR-009, FR-019, FR-020, T162 / F2): under the
            // production snapshot shape every GetOverlays call IS a
            // render frame boundary — ModController builds a NEW
            // Dictionary<Point, float> on every UpdateTicked tick
            // and VisualizationService snapshots that reference, so
            // the same dictionary reference is never re-presented
            // twice. The decision is therefore evaluated exactly
            // once per call, from the presented snapshot's culled
            // visible count and the degradation mode, routed through
            // the single decision path; a fresh snapshot reference
            // is the next frame, not a mid-frame refresh, so a
            // visible-count change takes effect on the frame that
            // presents it and the decision never freezes at a
            // session's frame-1 values. Culling stays per-call:
            // every consumer still culls the data it is handed.

            // Asymmetric hysteresis (T100 / Q6 / FR-009): degrade
            // eagerly — the frame degrades as soon as the culled
            // visible count exceeds the threshold, routed through
            // the single decision path (CheckDegradation) — but
            // recover lazily: once degraded, the frame stays
            // degraded until the count falls to the lower band
            // edge (ModConstants.DegradationRecoveryThreshold, an
            // extrapolated-not-standard judgement value — see
            // Constants.cs). A count inside the band
            // (edge < count <= threshold) keeps the degraded
            // state across frame boundaries, so panning across
            // the threshold (e.g. 999 -> 1001 -> 999) neither
            // recovers the frame nor re-spams the one-time
            // degradation toast. The carry applies to the modes
            // that evaluate degradation: "never" is an escape
            // hatch that never degrades and clears a stale
            // degraded state immediately.
            var degradesEagerly = CheckDegradation(visibleCount, degradationMode);
            var staysDegraded = _frameDegraded.Value
                && IsDegradationEvaluatedMode(degradationMode)
                && visibleCount > ModConstants.DegradationRecoveryThreshold;
            _frameDegraded.Value = degradesEagerly || staysDegraded;

            // The frame-start mode governs the frame's degradation
            // consequence (FR-019, FR-020): the mode read for this
            // frame governs the whole frame — the frame completes
            // with the mode it started under.
            _frameDegradationMode.Value = degradationMode;

            // Graceful degradation (FR-009): the single frame-start
            // decision governs every in-frame consumer. "auto" disables
            // accessibility patterns for the whole degraded frame, while
            // "notify" keeps every pattern rendered and instead announces
            // the degradation event once through both the player-facing
            // toast and the Warn log (T136, FR-009) — the polite
            // performance warning, visible without stealing focus or
            // pausing gameplay (WCAG 4.1.3, role=status).
            if (_frameDegraded.Value)
            {
                if (!_degradationNotificationSent.Value)
                {
                    if (string.Equals(_frameDegradationMode.Value, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        _monitor.Log(
                            $"Performance degradation activated: {visibleCount} visible tiles exceeds {ModConstants.DegradationTileThreshold} threshold. " +
                            "Disabling accessibility patterns per FR-009.",
                            LogLevel.Warn);

                        // FR-009: a monitor log alone is invisible to the
                        // player (WCAG 1.4.1), so emit one player-facing
                        // toast per degradation event. The shared latch
                        // above guarantees this fires exactly once until
                        // the count recovers to the hysteresis band edge
                        // and the latch resets (T100 / Q6).
                        _hudNotifier.ShowNotification(
                            "Accessibility patterns disabled for performance: too many tiles are visible at once. " +
                            "Re-enable patterns in the mod config.");
                    }
                    else
                    {
                        // "notify" (T136, FR-009): the degradation event
                        // is announced once through both channels while
                        // every accessibility pattern stays rendered —
                        // the mode's defining difference from "auto".
                        _monitor.Log(
                            $"Performance degradation activated: {visibleCount} visible tiles exceeds {ModConstants.DegradationTileThreshold} threshold. " +
                            "Accessibility patterns remain enabled per FR-009 (notify mode).",
                            LogLevel.Warn);

                        _hudNotifier.ShowNotification(
                            "Performance degraded: too many tiles are visible at once. " +
                            "Accessibility patterns remain enabled; tune degradation in the mod config.");
                    }

                    _degradationNotificationSent.Value = true;
                }

                // Only "auto" suppresses patterns; "notify" keeps them.
                if (string.Equals(_frameDegradationMode.Value, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    showPatterns = false;
                }
            }
            else if (_degradationNotificationSent.Value && visibleCount <= ModConstants.DegradationRecoveryThreshold)
            {
                // Reset the notification latch at the lower hysteresis
                // band edge (T100 / Q6 / FR-009), not at the degrade
                // threshold: a count that drops back below the
                // threshold but stays above the band edge keeps the
                // latch armed (the frame stays degraded), so panning
                // across the threshold (e.g. 999 -> 1001 -> 999)
                // cannot re-spam the one-time degradation toast. The
                // reset stays per-call count-based.
                _degradationNotificationSent.Value = false;
                _monitor.Log("Performance degradation deactivated: visible tile count within acceptable range.", LogLevel.Trace);
            }

            // Build overlay list from visible tiles
            var overlays = BuildOverlays(visibleTiles, opacity, showPatterns, unknownColor);

            // Update cache (FR-010)
            lock (_cacheLock)
            {
                _cachedOverlays.Value = overlays;
                _cachedViewport.Value = viewport;
                _cachedMapBounds.Value = mapBounds;
                _cachedVisibleTiles.Value = visibleTiles;
                _cachedPaletteGeneration.Value = paletteGeneration;
                _cachedOpacity.Value = opacity;
                _cachedShowPatterns.Value = configuredShowPatterns;
                _cachedDegradationMode.Value = degradationMode;
                _cachedUnknownColor.Value = unknownColor;
            }

            return overlays;
        }

        /// <summary>
        /// Invalidates the overlay cache, forcing recalculation on the next <see cref="GetOverlays"/> call.
        /// Called when soil health data changes, on save/load, and on configuration change (FR-010, T134).
        /// </summary>
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedOverlays.Value = null;
                _cachedViewport.Value = Rectangle.Empty;
                _cachedMapBounds.Value = null;
                _cachedVisibleTiles.Value = null;
                _cachedUnknownColor.Value = default;
            }
            _monitor.Log("Overlay renderer cache invalidated.", LogLevel.Trace);
        }

        /// <summary>
        /// Computes the tile rectangle used for culling: the viewport extended
        /// by the 1-tile margin on every side (FR-018), then clamped to the
        /// map bounds when supplied. When no map bounds are supplied, only the
        /// map origin (0, 0) is enforced, so the margin cannot leak into
        /// negative tile coordinates.
        /// </summary>
        /// <param name="viewport">Viewport bounds in tile units.</param>
        /// <param name="mapBounds">Map bounds in tile units, or null when unknown.</param>
        /// <returns>The clamped culling bounds in tile units.</returns>
        private static Rectangle ComputeCullingBounds(Rectangle viewport, Rectangle? mapBounds)
        {
            // FR-018: extend the visible tile range by ±1 tile beyond the
            // viewport so edge overlays render without pop-in
            var expanded = new Rectangle(
                viewport.X - ModConstants.ViewportMarginTiles,
                viewport.Y - ModConstants.ViewportMarginTiles,
                viewport.Width + (2 * ModConstants.ViewportMarginTiles),
                viewport.Height + (2 * ModConstants.ViewportMarginTiles));

            // Clamp to the map bounds (Map.DisplayWidth/Height / tileSize, in
            // tile units). The map origin always applies; the map maximum only
            // applies when the caller supplies the map bounds.
            var minX = Math.Max(expanded.Left, 0);
            var minY = Math.Max(expanded.Top, 0);
            var maxX = expanded.Right;
            var maxY = expanded.Bottom;

            if (mapBounds.HasValue)
            {
                var bounds = mapBounds.Value;
                minX = Math.Max(minX, bounds.Left);
                minY = Math.Max(minY, bounds.Top);
                maxX = Math.Min(maxX, bounds.Right);
                maxY = Math.Min(maxY, bounds.Bottom);
            }

            return new Rectangle(
                minX,
                minY,
                Math.Max(0, maxX - minX),
                Math.Max(0, maxY - minY));
        }

        /// <summary>
        /// Filters tile health data to only include tiles within the culling
        /// bounds (viewport plus 1-tile margin, clamped to the map bounds).
        /// Implements viewport culling per FR-009 and FR-018.
        /// </summary>
        private static List<KeyValuePair<Point, float>> CullToViewport(
            Dictionary<Point, float> tileHealthData, Rectangle cullingBounds)
        {
            return tileHealthData.Where(kvp => cullingBounds.Contains(kvp.Key)).ToList();
        }

        /// <summary>
        /// Compares the visible tile-health snapshot the cache was built from
        /// against the one just received (T134, FR-010, S1244). Two frames with
        /// the same viewport but different tile data are NOT cache hits — the
        /// second frame would otherwise redraw the first frame's values, which
        /// is exactly the fabricated/stale report FR-011 forbids. Values are
        /// compared with <see cref="ModConstants.FloatComparisonEpsilon"/> so
        /// sub-epsilon float jitter still serves the cache, while the
        /// non-finite unknown sentinel only matches itself.
        /// </summary>
        /// <param name="cached">The visible tiles the cache was built from.</param>
        /// <param name="incoming">The visible tiles just received.</param>
        /// <returns>True when both snapshots carry the same tiles and values.</returns>
        private static bool VisibleTilesEqual(
            List<KeyValuePair<Point, float>>? cached,
            List<KeyValuePair<Point, float>> incoming)
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

            // Culling preserves the incoming dictionary's order, so a
            // position-by-position comparison is valid; the count check above
            // makes the pairing complete.
            for (var index = 0; index < cached.Count; index++)
            {
                if (cached[index].Key != incoming[index].Key
                    || !ApproximatelyEqual(cached[index].Value, incoming[index].Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// S1244: approximate float equality with explicit non-finite handling.
        /// NaN matches NaN, each infinity matches itself, and finite values match
        /// within <see cref="ModConstants.FloatComparisonEpsilon"/>.
        /// </summary>
        /// <param name="left">First value.</param>
        /// <param name="right">Second value.</param>
        /// <returns>True when the values are the same for cache-key purposes.</returns>
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
        /// Builds tile overlays from visible tile data, applying color interpolation,
        /// opacity, the configured unknown color, and pattern assignment per
        /// configuration.
        /// </summary>
        private List<TileOverlay> BuildOverlays(
            List<KeyValuePair<Point, float>> visibleTiles, float opacity, bool showPatterns, ColorDTO unknownColor)
        {
            var overlays = new List<TileOverlay>(visibleTiles.Count);

            foreach (var kvp in visibleTiles)
            {
                var tilePos = kvp.Key;
                var healthValue = kvp.Value;

                // FR-014: render Unknown color when health data is unavailable
                // (the producer supplies the non-finite sentinel for absent /
                // not-yet-loaded records — T135, FR-014). FR-003 / FR-014
                // (T165): that color is the CONFIGURED unknown color with the
                // configured opacity applied — the FR-014 neutral gray is what
                // the configuration resolves, not the ModConstants constant
                // (which is only the default the configuration service falls
                // back to).
                if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
                {
                    var unknownOverlayColor = new Color(
                        unknownColor.R,
                        unknownColor.G,
                        unknownColor.B,
                        (byte)(opacity * 255f));
                    overlays.Add(new TileOverlay(tilePos, unknownOverlayColor, PatternType.None, healthValue, HealthCategory.Unknown));
                    continue;
                }

                // T072 / FR-009 / FR-010: health 0 is the
                // neutral vanilla-dirt baseline — unaugmented
                // soil — and renders zero overlays, so the
                // tile keeps the game's own dirt appearance
                // instead of a colored overlay. This supersedes
                // the T135 pin that rendered the Poor overlay
                // at health 0: the measured range the renderer
                // colors starts at 1 (the Poor interval floor,
                // ColorInterpolationService), and 0 is the
                // unrendered state — distinct from a tile with
                // no record (the non-finite sentinel, which
                // still renders the Unknown gray overlay above
                // per FR-014).
                if (healthValue == 0f)
                {
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

        /// <summary>
        /// A direct-call-site degradation decision and the frame epoch it was
        /// established at (T099 / Q2 / FR-009). A decision is observable only
        /// while its epoch is current; the epoch advance at every frame
        /// boundary retires every stored decision at once.
        /// </summary>
        private readonly struct FrameDecision
        {
            public FrameDecision(long frameEpoch, bool decision)
            {
                FrameEpoch = frameEpoch;
                Decision = decision;
            }

            public long FrameEpoch { get; }

            public bool Decision { get; }
        }
    }
}
