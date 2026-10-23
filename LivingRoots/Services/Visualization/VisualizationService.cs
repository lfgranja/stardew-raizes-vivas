using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Main render orchestrator for soil health visualization.
    /// Coordinates overlay rendering, tooltips, and hoe feedback effects per FR-001 through FR-021.
    /// Implements <see cref="IVisualizationService"/> and is called from game loop render events.
    /// </summary>
    public class VisualizationService(
        IColorInterpolationService colorService,
        IVisualizationConfigurationService configService,
        IMonitor monitor,
        IHudNotifier hudNotifier) : IVisualizationService
    {
        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        // White texture for drawing rectangles (set via Initialize)
        private Texture2D? _whiteTexture;

        // Pause state for save/load
        private volatile bool _isPaused;

        // Overlays-disabled observation from the previous render frame
        // (FR-010, T134), per local player (T163, FR-026, DC-6).
        // The disabled early-exit cannot discard the
        // overlay-list cache itself — FR-023 keeps the disabled frame a
        // no-op — so it records the disabled state here and the first
        // enabled frame after it invalidates the cache. That is what
        // makes the first frame after re-enable rebuild the overlay list
        // from the current tile-health data (consulting the color service
        // with the current values) instead of serving the cached list the
        // last enabled frame before the disable populated. Each local
        // player tracks their own disable observation: one player's
        // disabled frame never invalidates another player's cache.
        private readonly PerScreen<bool> _overlaysWereDisabled = new();

        // Cursor tile position for tooltip tracking, per local
        // player (T163, FR-026, DC-6): the cursor is resolved
        // per player against that player's Game1 state (DC-5),
        // so each player's tooltip follows their own cursor.
        private readonly PerScreen<Point> _cursorTile = new();

        /// <summary>
        /// Initializes the service with a texture for drawing rectangles.
        /// Called after SpriteBatch is available.
        /// </summary>
        public void Initialize(Texture2D whiteTexture)
        {
            _whiteTexture = whiteTexture ?? throw new ArgumentNullException(nameof(whiteTexture));
        }

        private Texture2D GetTexture()
        {
            return _whiteTexture ?? throw new InvalidOperationException("VisualizationService not initialized. Call Initialize() first.");
        }

        // Tooltip renderer, one per local player (T163,
        // FR-026, DC-6): the renderer owns the FR-015
        // per-tile throttle map, which must never be shared
        // across screens — one player's hover throttles that
        // player's tooltip updates only. The container is
        // readonly; per-player values flow through its Value,
        // and ResetTooltipThrottle replaces the current
        // screen's renderer with a fresh instance (a fresh
        // empty throttle map).
        private readonly PerScreen<TooltipRenderer> _tooltipRenderer = new(() => new TooltipRenderer(configService, monitor, colorService));

        // Overlay renderer for computing visible tile overlays
        private readonly OverlayRenderer _overlayRenderer = new(colorService, configService, monitor, hudNotifier);

        // Hoe feedback renderer for the flash and floating-text
        // effects (T129): the live path draws both effects through
        // it, resolving the HoeFeedbackRenderer duplication — the
        // service no longer reimplements the flash draw or the
        // floating-text position function. T140 disposition
        // (retained): the "unused renderer" review item predates
        // T129's wiring — the renderer is the live path today,
        // so removal is not on the table; the duplication it
        // resolved is the justification for keeping it.
        private readonly HoeFeedbackRenderer _hoeFeedbackRenderer = new(configService, monitor, colorService);

        // Tile health data snapshot for current frame
        private Dictionary<Point, float> _tileHealthData = new();
        private readonly object _dataLock = new();

        // Hoe feedback state — one list per local player (FR-026, DC-6):
        // a single handler serves every local-player dispatch with the
        // Game1 state already swapped to that player, so the feedback
        // list must never be shared across screens.
        private readonly PerScreen<List<HoeFeedback>> _activeFeedbacks = new(() => new List<HoeFeedback>());
        private readonly object _feedbackLock = new();

        /// <summary>
        /// Sets the tile health data used for overlay rendering.
        /// Called by the game event handler before each render frame.
        /// </summary>
        /// <param name="tileHealthData">Dictionary mapping tile positions to health values.</param>
        public void SetTileHealthData(Dictionary<Point, float> tileHealthData)
        {
            lock (_dataLock)
            {
                _tileHealthData = tileHealthData ?? new Dictionary<Point, float>();
            }
        }

        /// <inheritdoc />
        public void PauseRendering()
        {
            _isPaused = true;

            // FR-017: in-flight hoe feedback is discarded when
            // the pause begins — the flash and the floating text
            // are dropped, neither completed nor preserved — so
            // no half-finished effect survives the pause window
            // into the first resumed frame.
            DiscardHoeFeedback();
        }

        /// <inheritdoc />
        public void ResumeRendering()
        {
            _isPaused = false;
        }

        /// <inheritdoc />
        public void UpdateCursorTile(Point tilePosition)
        {
            _cursorTile.Value = tilePosition;
        }

        /// <inheritdoc />
        public void ClearCursorTile()
        {
            _cursorTile.Value = new Point(-1, -1);
        }

        /// <summary>
        /// Resets the tooltip throttle state (FR-015, FR-025):
        /// the per-tile throttle window must not survive a
        /// menu open/close cycle, so closing the last menu
        /// re-establishes the leading-edge rule for the next
        /// hover. The throttle map belongs to the
        /// <see cref="TooltipRenderer"/>, which owns it and
        /// exposes no reset, so the renderer is rebuilt with
        /// the same dependencies — a fresh instance carries an
        /// empty throttle map. The rebuild is a reference
        /// assignment, so a concurrent frame either sees the
        /// old renderer (still valid, only its throttle map is
        /// stale) or the new one; no half-state is observable.
        /// </summary>
        public void ResetTooltipThrottle()
        {
            _tooltipRenderer.Value = new TooltipRenderer(_configService, _monitor, _colorService);
        }

        /// <inheritdoc />
        public void RenderOverlays(SpriteBatch spriteBatch, Rectangle viewport, GameTime gameTime, Rectangle? mapBounds = null)
        {
            if (_isPaused)
                return;

            var config = _configService.GetConfiguration();

            // No draw calls when overlays disabled (FR-007)
            if (!config.OverlaysEnabled)
            {
                // FR-010 / FR-023: the disabled frame stays an
                // early-exit no-op — no tile-health snapshot copy, no
                // overlay-list rebuild, no color-service consultation,
                // no draw calls, zero allocations — but it records that
                // overlays are disabled so the next enabled frame can
                // discard the overlay-list cache: FR-010 requires the
                // list to be rebuilt on the first frame after re-enable,
                // not served from the cache the enabled frames before
                // the disable populated (the disabled-era list).
                _overlaysWereDisabled.Value = true;
                return;
            }

            // FR-010 / T134: cache maintenance applies when overlays
            // are enabled, so the re-enable transition is where the
            // overlay-list cache is discarded. The first frame after
            // re-enable misses the cache and rebuilds from the current
            // tile-health data; frames after that (no state change) are
            // legitimately served from the cache again.
            if (_overlaysWereDisabled.Value)
            {
                _overlaysWereDisabled.Value = false;
                _overlayRenderer.InvalidateCache();
            }

            // Snapshot tile health data under lock for thread safety (FR-013, FR-020)
            Dictionary<Point, float> dataSnapshot;
            lock (_dataLock)
            {
                dataSnapshot = _tileHealthData;
            }

            // Get overlays with viewport culling and caching (FR-009, FR-010),
            // clamped to the map bounds when the caller supplies them (FR-018)
            var overlays = _overlayRenderer.GetOverlays(viewport, dataSnapshot, mapBounds);

            if (overlays.Count == 0)
            {
                return;
            }

            // Render each overlay with configured opacity and patterns
            foreach (var overlay in overlays)
            {
                DrawOverlay(spriteBatch, overlay, config, viewport);
            }
        }

        /// <inheritdoc />
        public void RenderTooltip(SpriteBatch spriteBatch, Vector2 cursorPosition, GameTime gameTime)
        {
            var config = _configService.GetConfiguration();
            if (!config.TooltipsEnabled)
                return;

            // Snapshot tile health data under lock for thread safety
            Dictionary<Point, float> snapshot;
            lock (_dataLock)
            {
                snapshot = new Dictionary<Point, float>(_tileHealthData);
            }

            // Delegate rendering computation to TooltipRenderer (FR-002, FR-015)
            // Pass SMAPI cursor tile (from ModController via UpdateCursorTile / GrabTile) instead of raw division.
            var tooltipData = _tooltipRenderer.Value.GetTooltip(cursorPosition, _cursorTile.Value, snapshot, gameTime);

            // TooltipRenderer handles unknown-state path (missing/NaN/Unknown) per FR-014.
            if (tooltipData == null)
                return;

            DrawTooltip(spriteBatch, tooltipData);
        }

        /// <inheritdoc />
        public void RenderHoeFeedback(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_isPaused)
                return;

            var currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;

            var activeFeedbacks = PruneActiveFeedbacks(currentTime);

            var config = _configService.GetConfiguration();

            if (!config.HoeFeedbackEnabled)
            {
                return;
            }

            if (activeFeedbacks.Count == 0)
            {
                return;
            }

            // Render each active feedback effect
            foreach (var feedback in activeFeedbacks)
            {
                DrawHoeFeedback(spriteBatch, feedback, gameTime);
            }
        }

        /// <summary>
        /// Renders the hoe-feedback floating text on the screen-space
        /// render event (DC-3, FR-024): the floating text is a
        /// screen-space feature — it draws above all vanilla content
        /// (the HUD, menus, and cursor) — while the flash stays on the
        /// world-space event through <see cref="RenderHoeFeedback"/>.
        /// This is the <c>Display.Rendered</c> counterpart of
        /// <see cref="RenderHoeFeedback"/>: the screen-space handler
        /// invokes it with the same sprite batch and frame game time,
        /// and it walks the same pruned, enabled-gated feedback list
        /// the world-space half renders the flash from.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for the screen-space draw.</param>
        /// <param name="gameTime">Current frame game time.</param>
        public void RenderHoeFeedbackFloatingText(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_isPaused)
                return;

            var currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;

            var activeFeedbacks = PruneActiveFeedbacks(currentTime);

            var config = _configService.GetConfiguration();

            if (!config.HoeFeedbackEnabled || activeFeedbacks.Count == 0)
            {
                return;
            }

            // FR-003 / FR-024: the floating text only — the flash is a
            // world-space effect and never redraws on the screen-space
            // event.
            foreach (var feedback in activeFeedbacks)
            {
                _hoeFeedbackRenderer.RenderFloatingText(spriteBatch, feedback, gameTime);
            }
        }

        /// <summary>
        /// Prunes the expired hoe-feedback entries from the current
        /// screen's list and returns a snapshot of the survivors.
        /// Runs on every render frame — including frames with hoe
        /// feedback disabled — because the toggle gates drawing, not
        /// list maintenance (T096): pruning only on enabled frames
        /// would let <see cref="TriggerHoeFeedback"/> accumulate
        /// unbounded while the feature is off.
        /// </summary>
        /// <param name="currentTimeMs">Current game-clock milliseconds.</param>
        /// <returns>A snapshot of the non-expired feedback entries.</returns>
        private List<HoeFeedback> PruneActiveFeedbacks(long currentTimeMs)
        {
            // Prune expired feedbacks on every frame, including disabled
            // ones: the toggle gates drawing, not list maintenance.
            // Pruning only on enabled frames would let TriggerHoeFeedback
            // accumulate unbounded while hoe feedback is off.
            lock (_feedbackLock)
            {
                var screenFeedbacks = _activeFeedbacks.Value;
                screenFeedbacks.RemoveAll(f => f.IsExpired(currentTimeMs));
                return new List<HoeFeedback>(screenFeedbacks);
            }
        }

        /// <summary>
        /// Discards every in-flight hoe feedback on the current
        /// screen (FR-017): the flash and the floating text are
        /// dropped, neither completed nor preserved. Called when
        /// the render pause begins — <see cref="PauseRendering"/>
        /// invokes it, so a save/load pause (bounded by the
        /// controller: the <c>OnSaving</c> pause runs to its
        /// <c>Saved</c> resume, and the load window from
        /// <c>SaveLoaded</c> until the refreshed data is loaded)
        /// never carries a half-finished effect into the first
        /// resumed frame — and available to any caller that needs
        /// to drop the in-flight effects without pausing rendering.
        /// </summary>
        public void DiscardHoeFeedback()
        {
            lock (_feedbackLock)
            {
                _activeFeedbacks.Value.Clear();
            }
        }

        /// <inheritdoc />
        public void TriggerHoeFeedback(Point tilePosition, float healthValue)
        {
            var config = _configService.GetConfiguration();

            // FR-007 (Q4): a disabled toggle suppresses this feature's own
            // derived work end-to-end as a zero-allocation no-op. When every
            // visualization toggle is off the pipeline as a whole is off, so
            // the trigger path returns before the color consult, the health
            // text, the HoeFeedback allocation, the list entry, and the log.
            // The gate deliberately keys on all three toggles rather than
            // HoeFeedbackEnabled alone: per T097 the hoe-feedback toggle gates
            // drawing, not list maintenance, so TriggerHoeFeedback keeps
            // recording state while any other visualization feature is live
            // (FeedbackBoundedTests pins that contract) — only a fully
            // disabled pipeline suppresses state creation. The producer is
            // untouched (FR-022 read-only projection), and re-enabling takes
            // effect on the next frame (SC-003) because the configuration is
            // read fresh on every call.
            if (config is not null
                && !config.OverlaysEnabled
                && !config.TooltipsEnabled
                && !config.HoeFeedbackEnabled)
            {
                return;
            }

            var category = _colorService.GetCategoryForHealth(healthValue);
            var healthText = FormatHealthText(healthValue, category);

            var feedback = new HoeFeedback
            {
                TilePosition = tilePosition,
                StartTime = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds,
                FlashDuration = ModConstants.FlashDurationMs,
                TextDuration = ModConstants.TextDurationMs,
                HealthValue = healthValue,
                Category = category,
                HealthText = healthText
            };

            lock (_feedbackLock)
            {
                // FR-003: a new action on a tile with active feedback
                // replaces that tile's entry and restarts both timers
                // (flash 300ms, floating text 1000ms) from the new
                // action's start time — at most one entry per tile,
                // last action wins, and no queue of pending feedback
                // is maintained. The removal is unconditional: an
                // entry the render path has not yet pruned (expired,
                // but still in the list) is replaced too, so
                // successive actions on a tile never leave more than
                // one entry. Actions on different tiles are
                // independent (per-tile entries).
                //
                // The scan is a hand-rolled reverse loop rather
                // than RemoveAll with a lambda: a lambda that
                // captures the tile-position parameter hoists its
                // closure to method entry, which would allocate on
                // every call — including the fully-disabled
                // pipeline's zero-allocation no-op path (FR-007,
                // T095) — so the replacement must not capture the
                // parameter.
                var screenFeedbacks = _activeFeedbacks.Value;
                for (var index = screenFeedbacks.Count - 1; index >= 0; index--)
                {
                    if (screenFeedbacks[index].TilePosition == tilePosition)
                    {
                        screenFeedbacks.RemoveAt(index);
                    }
                }

                screenFeedbacks.Add(feedback);
            }

            _monitor.Log($"Hoe feedback triggered for tile ({tilePosition.X}, {tilePosition.Y}): {healthText}", LogLevel.Trace);
        }

        /// <inheritdoc />
        public void InvalidateCache()
        {
            _overlayRenderer.InvalidateCache();
        }

        /// <summary>
        /// Draws a single tile overlay with color, opacity, and optional pattern.
        /// The destination origin follows the game's own world→screen
        /// derivation — the tile's world position
        /// (<c>tile * tileSize</c>) localized with
        /// <c>Game1.GlobalToLocal</c> (which subtracts the
        /// pixel-space <c>Game1.viewport</c> origin) and scaled
        /// with <c>Utility.ModifyCoordinatesForUIScale</c> (which
        /// folds in the zoom level and the UI scale), through the
        /// shared <see cref="ScreenPositionDerivation"/> (which
        /// resolves the legacy tile-space position when the game
        /// state is unavailable — the headless unit-test host, the
        /// identity at the default zoom and UI scale) — the same
        /// derivation the hoe-feedback anchors follow, so the
        /// overlay lands on the targeted tile at any camera
        /// offset and any zoom / UI scale (FR-003, FR-018, DC-5,
        /// T183). The <paramref name="viewport"/> stays the
        /// tile-space culling window the caller resolved the
        /// overlay in (the visible-tile rectangle
        /// <see cref="RenderOverlays"/> culled against); it no
        /// longer enters the position math — the viewport origin
        /// enters the derivation through <c>Game1.viewport</c>,
        /// which the localization reads.
        /// </summary>
        private void DrawOverlay(SpriteBatch spriteBatch, TileOverlay overlay, VisualizationConfiguration config, Rectangle viewport)
        {
            var worldPosition = new Vector2(
                overlay.TilePosition.X * ModConstants.TileSize,
                overlay.TilePosition.Y * ModConstants.TileSize);
            var screenPosition = ScreenPositionDerivation.ToScreenPosition(worldPosition);

            var destRect = new Rectangle(
                (int)screenPosition.X,
                (int)screenPosition.Y,
                ModConstants.TileSize,
                ModConstants.TileSize);

            // T183 (FR-003/FR-018, DC-5): the demanded draw
            // observable — the destination the draw consumes is
            // emitted before the draw, because SpriteBatch.Draw
            // is non-virtual and needs a GraphicsDevice the
            // headless unit-test host lacks, so this trace log
            // is the frame-level observable of where the overlay
            // drew (the RenderFloatingText seam pattern).
            _monitor.Log($"Overlay drawn: ({screenPosition.X}, {screenPosition.Y})", LogLevel.Trace);

            // T091 / T101 (FR-010): the drawn tint is
            // the unlit tint blended under the game's
            // ambient lighting pass, so the overlay
            // darkens with the world instead of rendering
            // at full strength over the darkened world
            // (night, rain, winter). The blended tint is
            // emitted before the draw as the demanded
            // ambient-tint draw observable — the tint
            // counterpart of the destination log above:
            // the tint is folded into the Color passed to
            // the non-virtual SpriteBatch.Draw, which the
            // headless unit-test host cannot perform, so
            // this trace log is the frame-level observable
            // of the tint the draw consumed.
            var drawnTint = BlendUnderAmbientLight(overlay.Color);
            _monitor.Log($"Overlay tint drawn: RGBA({drawnTint.R}, {drawnTint.G}, {drawnTint.B}, {drawnTint.A})", LogLevel.Trace);

            // Draw the base color overlay with the ambient-blended tint
            spriteBatch.Draw(GetTexture(), destRect, drawnTint);

            // FR-001: draw pattern overlay when ShowPatterns is enabled
            if (overlay.PatternType != PatternType.None && config.ShowPatterns)
            {
                DrawPatternOverlay(spriteBatch, destRect, overlay.PatternType);
            }
        }

        /// <summary>
        /// Blends an unlit overlay tint under the game's
        /// ambient lighting pass (FR-010, T091/T101): the
        /// overlay is part of the world, so the lighting
        /// pass that darkens the world at night, in rain,
        /// and in winter darkens the overlay with it — the
        /// drawn tint is the unlit tint multiplied by the
        /// ambient light, never a full-strength tint pasted
        /// over the darkened world (the full-opacity-over-
        /// darkened-world defect this replaces). Full
        /// daylight (white ambient) is the identity, so the
        /// tint is unchanged; night darkens every channel
        /// by the ambient channel, so the overlay stays
        /// readable — darker than unlit, never blacked out,
        /// still visible through its alpha. Only the base
        /// fill tint is blended: the pinned FR-001 pattern
        /// tones stay opaque so at least one tone always
        /// meets the 3:1 non-text contrast minimum against
        /// the fill composite — blending them away would
        /// trade the accessibility guarantee the patterns
        /// exist for. Game1.ambientLight is the universal
        /// ambient pass (the outdoor light is the
        /// outdoor-specific component of the same pass and
        /// tracks it); it is read at draw time so the blend
        /// follows the live day cycle, weather, and season.
        /// </summary>
        /// <param name="unlitTint">The overlay tint the color service resolved — the category color at the configured opacity.</param>
        /// <returns>The tint to draw: the unlit tint under the ambient lighting pass.</returns>
        private static Color BlendUnderAmbientLight(Color unlitTint)
        {
            // The byte-channel scale the blend normalizes
            // against — a Color channel's domain maximum
            // (the blend multiplies the unlit and ambient
            // channels, both bytes, and divides by this to
            // stay in the byte domain). Method-local constant
            // alongside the pattern construction's pinned
            // values (a class-level const would read as a
            // static literal field to the PerScreen
            // architecture scans, which only admit init-only
            // statics; the mod-wide magic numbers live in
            // ModConstants).
            const float channelScale = 255f;

            var ambientLight = Game1.ambientLight;
            return new Color(
                (byte)(unlitTint.R * ambientLight.R / channelScale),
                (byte)(unlitTint.G * ambientLight.G / channelScale),
                (byte)(unlitTint.B * ambientLight.B / channelScale),
                (byte)(unlitTint.A * ambientLight.A / channelScale));
        }

        // FR-001 (T131/T139): the pattern construction's
        // pinned alpha and its opaque white/black tones
        // live in ModConstants (PatternAlphaPinned,
        // PatternWhiteTone, PatternBlackTone) — the single
        // source of truth for the FR-001 two-tone
        // accessibility construction.

        /// <summary>
        /// Draws an accessibility pattern overlay on top of the base
        /// color fill (FR-001): the pattern elements are opaque
        /// black and opaque white two-tone — never tinted in the
        /// category color — at the pinned pattern alpha 1.0,
        /// independent of the configured overlay opacity, so at
        /// least one tone always meets the 3:1 non-text contrast
        /// minimum against the fill composite. Only the pattern
        /// type and the destination rectangle enter the
        /// construction: the tones carry no category information.
        /// </summary>
        private void DrawPatternOverlay(SpriteBatch spriteBatch, Rectangle destRect, PatternType patternType)
        {
            switch (patternType)
            {
                case PatternType.Stripes:
                    DrawStripesPattern(spriteBatch, destRect);
                    break;
                case PatternType.Dots:
                    DrawDotsPattern(spriteBatch, destRect);
                    break;
                case PatternType.Solid:
                    DrawCheckerboardPattern(spriteBatch, destRect);
                    break;
            }
        }

        /// <summary>
        /// Draws the stripe pattern for Poor category tiles
        /// (FR-001): alternating opaque white and opaque black
        /// stripes, each 8px wide, repeating on a 16px period —
        /// the 8px stripe width is at least the 4px non-text
        /// minimum feature and still renders at 6 screen pixels
        /// at the 75% minimum zoom.
        /// </summary>
        private void DrawStripesPattern(SpriteBatch spriteBatch, Rectangle rect)
        {
            const int stripeWidth = 8;
            for (var x = rect.Left; x < rect.Right; x += stripeWidth * 2)
            {
                var whiteStripe = new Rectangle(x, rect.Top, stripeWidth, rect.Height);
                var blackStripe = new Rectangle(x + stripeWidth, rect.Top, stripeWidth, rect.Height);
                spriteBatch.Draw(GetTexture(), whiteStripe, ModConstants.PatternWhiteTone);
                spriteBatch.Draw(GetTexture(), blackStripe, ModConstants.PatternBlackTone);
            }
        }

        /// <summary>
        /// Draws the dot pattern for Moderate category tiles
        /// (FR-001): opaque 4px dots on an 8px pitch — the 4px
        /// dot is the non-text minimum feature and leaves 4px of
        /// spacing, completing the 8px pitch — with the tones
        /// alternating in a checkerboard (a dot's tone is white
        /// when its pitch indices sum to even, black when they
        /// sum to odd), so the dot field is a two-tone texture
        /// that tiles seam-free on the 16px pattern texture.
        /// </summary>
        private void DrawDotsPattern(SpriteBatch spriteBatch, Rectangle rect)
        {
            const int dotPitch = 8;
            const int dotSize = 4;
            for (var x = rect.Left; x + dotSize <= rect.Right; x += dotPitch)
            {
                for (var y = rect.Top; y + dotSize <= rect.Bottom; y += dotPitch)
                {
                    var pitchIndex = (x - rect.Left) / dotPitch + (y - rect.Top) / dotPitch;
                    var tone = pitchIndex % 2 == 0 ? ModConstants.PatternWhiteTone : ModConstants.PatternBlackTone;
                    var dotRect = new Rectangle(x, y, dotSize, dotSize);
                    spriteBatch.Draw(GetTexture(), dotRect, tone);
                }
            }
        }

        /// <summary>
        /// Draws the Healthy solid pattern as a real two-tone
        /// checkerboard (FR-001): 8px cells alternating opaque
        /// white and opaque black across the tile — 8 cells per
        /// tile axis, integer tiling that is seam-free on the
        /// 16px pattern texture (2x2 cells per texture tile).
        /// The 8px cell is at least the 4px non-text minimum
        /// feature and still renders at 6 screen pixels at the
        /// 75% minimum zoom.
        /// </summary>
        private void DrawCheckerboardPattern(SpriteBatch spriteBatch, Rectangle rect)
        {
            const int cellSize = 8;
            for (var x = rect.Left; x < rect.Right; x += cellSize)
            {
                for (var y = rect.Top; y < rect.Bottom; y += cellSize)
                {
                    var cellIndex = (x - rect.Left) / cellSize + (y - rect.Top) / cellSize;
                    var tone = cellIndex % 2 == 0 ? ModConstants.PatternWhiteTone : ModConstants.PatternBlackTone;
                    var cellRect = new Rectangle(x, y, cellSize, cellSize);
                    spriteBatch.Draw(GetTexture(), cellRect, tone);
                }
            }
        }

        /// <summary>
        /// Draws a tooltip at the position computed for the
        /// cursor anchor using the game's native tooltip renderer.
        /// Measures the string, draws a background rectangle with padding,
        /// draws text with a 1px drop shadow, and handles edge cases
        /// where the tooltip would go off-screen.
        /// </summary>
        private void DrawTooltip(SpriteBatch spriteBatch, TooltipData tooltipData)
        {
            var text = tooltipData.Text;
            var position = tooltipData.Position;
            var bgColor = tooltipData.BackgroundColor;

            var textSize = Game1.smallFont.MeasureString(text);

            var paddingX = 8;
            var paddingY = 4;

            var bgWidth = (int)textSize.X + paddingX * 2;
            var bgHeight = (int)textSize.Y + paddingY * 2;

            // FR-002 placement (T087): the panel renders
            // cursor-adjacent, offset by the UI-scaled
            // tooltip offset and clamped within the
            // viewport so it never renders off-screen,
            // through the shared computation the
            // TooltipRenderer owns — never an inline
            // offset-and-clamp copy.
            var panelPosition = TooltipRenderer.ComputeTooltipPosition(
                position,
                new Vector2(bgWidth, bgHeight));
            var tooltipX = (int)panelPosition.X;
            var tooltipY = (int)panelPosition.Y;

            var tooltipRect = new Rectangle(tooltipX, tooltipY, bgWidth, bgHeight);

            // Draw background using tooltip color (gray for unknown per FR-014)
            spriteBatch.Draw(GetTexture(), tooltipRect, bgColor);

            // Draw text with 1px drop shadow using tooltip text color
            var shadowColor = ModConstants.TooltipShadowColor;
            var textColor = tooltipData.TextColor;
            var textPosX = tooltipX + paddingX;
            var textPosY = tooltipY + paddingY;
            spriteBatch.DrawString(Game1.smallFont, text, new Vector2(textPosX + 1, textPosY + 1), shadowColor);
            spriteBatch.DrawString(Game1.smallFont, text, new Vector2(textPosX, textPosY), textColor);
        }

        /// <summary>
        /// Draws hoe feedback effects (flash and floating text) for a
        /// single feedback entry, through the wired
        /// <see cref="HoeFeedbackRenderer"/> (T129): the renderer owns
        /// the flash fade, the floating-text anchor (the tile's
        /// horizontal center, above the tile, drifting upward over the
        /// text window), and the white text color, and logs the anchor
        /// on every in-window frame.
        /// </summary>
        private void DrawHoeFeedback(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime)
        {
            // FR-003: the flash and the floating text both draw through
            // the renderer; each effect no-ops on its own once its
            // duration has elapsed.
            _hoeFeedbackRenderer.RenderFlash(spriteBatch, feedback, gameTime);
            _hoeFeedbackRenderer.RenderFloatingText(spriteBatch, feedback, gameTime);
        }

        /// <summary>
        /// Formats tooltip text per spec: "Soil Health: {percentage}% ({category})".
        /// Finite out-of-range health clamps to the [0, 100] soil-health
        /// domain before formatting (data-model.md ColorMapping validation
        /// rules); unknown, NaN and Infinity health emit the constant
        /// "Soil Health: Unknown" with no percentage and no category
        /// (FR-002, FR-014).
        /// </summary>
        private static string FormatTooltipText(float healthValue, HealthCategory category)
        {
            if (category == HealthCategory.Unknown || float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return "Soil Health: Unknown";
            }

            var clampedHealth = Math.Clamp(healthValue, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
            return $"Soil Health: {clampedHealth:F0}% ({category})";
        }

        /// <summary>
        /// Formats health text for hoe feedback display: the same
        /// canonical render text as the tooltip (FR-003 — the
        /// floating text and the tooltip carry the same content).
        /// Finite out-of-range health clamps to the [0, 100]
        /// soil-health domain before formatting (data-model.md
        /// ColorMapping validation rules); unknown, NaN and
        /// Infinity health emit the constant "Soil Health:
        /// Unknown" with no percentage and no category (FR-014).
        /// </summary>
        private static string FormatHealthText(float healthValue, HealthCategory category)
        {
            if (category == HealthCategory.Unknown || float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return "Soil Health: Unknown";
            }

            var clampedHealth = Math.Clamp(healthValue, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
            return $"Soil Health: {clampedHealth:F0}% ({category})";
        }
    }
}
