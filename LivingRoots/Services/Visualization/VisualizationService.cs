using System.Collections.Generic;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
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
        IHudNotifier? hudNotifier = null) : IVisualizationService
    {
        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        /// <summary>
        /// T069: HUD seam for the accessibility degradation notice. Optional so
        /// existing three-argument test doubles keep constructing the service.
        /// </summary>
        private readonly IHudNotifier? _hudNotifier = hudNotifier;

        // White texture for drawing rectangles (set via Initialize)
        private Texture2D? _whiteTexture;

        // Pause state for save/load
        private volatile bool _isPaused;

        // Cursor tile position for tooltip tracking
        private Point _cursorTile;

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

        private readonly TooltipRenderer _tooltipRenderer = new(configService, monitor, colorService);

        // Overlay renderer for computing visible tile overlays
        private readonly OverlayRenderer _overlayRenderer = new(colorService, configService, monitor, hudNotifier);

        // Tile health data snapshot for current frame
        private Dictionary<Point, float> _tileHealthData = new();
        private readonly object _dataLock = new();

        // Hoe feedback state
        private readonly List<HoeFeedback> _activeFeedbacks = new();
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
        }

        /// <inheritdoc />
        public void ResumeRendering()
        {
            _isPaused = false;
        }

        /// <inheritdoc />
        public void UpdateCursorTile(Point tilePosition)
        {
            _cursorTile = tilePosition;
        }

        /// <inheritdoc />
        public void ClearCursorTile()
        {
            _cursorTile = new Point(-1, -1);
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
                return;
            }

            // Snapshot tile health data under lock for thread safety (FR-013, FR-020)
            Dictionary<Point, float> dataSnapshot;
            lock (_dataLock)
            {
                dataSnapshot = _tileHealthData;
            }

            // Get overlays with viewport culling and caching (FR-009, FR-010).
            // mapBounds supplies the map extent clamp (FR-018) when the active
            // location exposed it; null keeps the renderer's origin-clamp-only
            // behaviour.
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
            var tooltipData = _tooltipRenderer.GetTooltip(cursorPosition, _cursorTile, snapshot, gameTime);

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

            var config = _configService.GetConfiguration();

            if (!config.HoeFeedbackEnabled)
            {
                return;
            }

            var currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;

            // Snapshot active feedbacks under lock, removing expired ones
            List<HoeFeedback> activeFeedbacks;
            lock (_feedbackLock)
            {
                _activeFeedbacks.RemoveAll(f => f.IsExpired(currentTime));
                activeFeedbacks = new List<HoeFeedback>(_activeFeedbacks);
            }

            if (activeFeedbacks.Count == 0)
            {
                return;
            }

            // Render each active feedback effect
            foreach (var feedback in activeFeedbacks)
            {
                DrawHoeFeedback(spriteBatch, feedback, gameTime, config);
            }
        }

        /// <inheritdoc />
        public void TriggerHoeFeedback(Point tilePosition, float healthValue)
        {
            var category = _colorService.GetCategoryForHealth(healthValue);
            var healthText = FormatHealthText(healthValue, category);

            var feedback = new HoeFeedback
            {
                TilePosition = tilePosition,
                StartTime = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds,
                HealthValue = healthValue,
                Category = category,
                HealthText = healthText
            };

            lock (_feedbackLock)
            {
                _activeFeedbacks.Add(feedback);
            }

            _monitor.Log($"Hoe feedback triggered for tile ({tilePosition.X}, {tilePosition.Y}): {healthText}", LogLevel.Trace);
        }

        /// <inheritdoc />
        public void TriggerHarvestFeedback(Point tilePosition, string message)
        {
            var feedback = new HoeFeedback
            {
                TilePosition = tilePosition,
                StartTime = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds,
                HealthValue = 0,
                Category = HealthCategory.Unknown,
                HealthText = message
            };

            lock (_feedbackLock)
            {
                _activeFeedbacks.Add(feedback);
            }

            _monitor.Log($"Harvest feedback triggered for tile ({tilePosition.X}, {tilePosition.Y}): {message}", LogLevel.Trace);
        }

        /// <inheritdoc />
        public void InvalidateCache()
        {
            _overlayRenderer.InvalidateCache();
        }

        /// <inheritdoc />
        public void ResetTooltipThrottle()
        {
            _tooltipRenderer.ResetThrottle();
        }

        /// <inheritdoc />
        public void RenderHoeFeedbackFloatingText(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Screen-space half of hoe feedback (FR-024): the floating text only.
            // The flash half stays world-space on RenderHoeFeedback.
            DrawHoeFeedbackFloatingText(spriteBatch, gameTime);
        }

        /// <summary>
        /// Draws the floating-text half of each active hoe feedback, above all
        /// vanilla content (FR-024). Expired entries are reaped here exactly as
        /// on the world-space half, so an entry cannot survive one frame past
        /// its duration on either seam.
        /// </summary>
        private void DrawHoeFeedbackFloatingText(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_isPaused)
                return;

            var config = _configService.GetConfiguration();
            if (!config.HoeFeedbackEnabled)
                return;

            var currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;

            List<HoeFeedback> activeFeedbacks;
            lock (_feedbackLock)
            {
                _activeFeedbacks.RemoveAll(f => f.IsExpired(currentTime));
                activeFeedbacks = new List<HoeFeedback>(_activeFeedbacks);
            }

            foreach (var feedback in activeFeedbacks)
            {
                var elapsedMs = currentTime - feedback.StartTime;
                if (elapsedMs >= feedback.TextDuration)
                {
                    continue;
                }

                var textY = feedback.TilePosition.Y * ModConstants.TileSize - (float)elapsedMs * 0.02f;
                var textPos = new Vector2(
                    feedback.TilePosition.X * ModConstants.TileSize + ModConstants.TileSize / 2,
                    textY);

                _monitor.Log($"Hoe feedback text: {feedback.HealthText} at ({textPos.X}, {textPos.Y})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Draws a single tile overlay with color, opacity, and optional pattern.
        /// </summary>
        private void DrawOverlay(SpriteBatch spriteBatch, TileOverlay overlay, VisualizationConfiguration config, Rectangle viewport)
        {
            // Convert tile position to screen coordinates relative to viewport
            var screenX = (overlay.TilePosition.X - viewport.X) * ModConstants.TileSize;
            var screenY = (overlay.TilePosition.Y - viewport.Y) * ModConstants.TileSize;

            var destRect = new Rectangle(screenX, screenY, ModConstants.TileSize, ModConstants.TileSize);

            // T261 (FR-011/FR-010, F15): one log record per drawn frame.
            // The destination observable carries the value the draw
            // actually consumed — the measured value for a measurable
            // tile, the literal Unknown for an unmeasurable one, never
            // a fabricated percentage — so the duplicate drawn-value
            // log and the third per-overlay tint log are gone.
            _monitor.Log(
                "Overlay drawn: " + (float.IsNaN(overlay.HealthValue) || float.IsInfinity(overlay.HealthValue) ? "Unknown" : overlay.HealthValue.ToString("R", System.Globalization.CultureInfo.CurrentCulture)),
                LogLevel.Trace);

            // T101 (FR-010): the overlay blends under the ambient lighting pass
            // instead of rendering at full strength over a darkened world. The
            // resolved tint is multiplied by the game's ambient light, so the
            // overlay stays readable at night, in rain and in winter without
            // clipping or washing out. Full daylight is the identity blend
            // (white ambient), which is why the day tint is the unlit tint.
            var blendedColor = BlendUnderAmbientLight(overlay.Color);

            _monitor.Log(
                "Overlay tint drawn: RGBA(" + (int)blendedColor.R + ", " + (int)blendedColor.G + ", " + (int)blendedColor.B + ", " + (int)blendedColor.A + ")",
                LogLevel.Trace);

            // Draw the base color overlay with configured opacity
            spriteBatch.Draw(GetTexture(), destRect, blendedColor);

            // FR-001: draw pattern overlay when ShowPatterns is enabled
            if (overlay.PatternType != PatternType.None && config.ShowPatterns)
            {
                DrawPatternOverlay(spriteBatch, destRect, overlay, config);
            }
        }

        /// <summary>
        /// Draws an accessibility pattern overlay on top of the base color.
        /// Pattern opacity is clamped to minimum 0.7 per FR-001.
        /// </summary>
        private void DrawPatternOverlay(SpriteBatch spriteBatch, Rectangle destRect, TileOverlay overlay, VisualizationConfiguration config)
        {
            // FR-001: pattern opacity must remain at minimum 0.7 regardless of overlay opacity
            var patternOpacity = Math.Max(ModConstants.PatternMinOpacity, config.Opacity);
            // T253 (FR-010, F7): the pattern blends under the ambient
            // lighting pass like the base fill, instead of drawing the
            // unblended color over the darkened world.
            var patternColor = BlendUnderAmbientLight(overlay.Color);
            patternColor.A = (byte)(patternOpacity * 255f);

            switch (overlay.PatternType)
            {
                case PatternType.Stripes:
                    DrawStripesPattern(spriteBatch, destRect, patternColor);
                    break;
                case PatternType.Dots:
                    DrawDotsPattern(spriteBatch, destRect, patternColor);
                    break;
                case PatternType.Solid:
                    spriteBatch.Draw(GetTexture(), destRect, patternColor);
                    break;
            }
        }

        /// <summary>
        /// Draws diagonal stripes pattern for Poor category tiles.
        /// </summary>
        private void DrawStripesPattern(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            const int stripeWidth = 8;
            for (int x = rect.Left; x < rect.Right; x += stripeWidth * 2)
            {
                var stripeRect = new Rectangle(x, rect.Top, stripeWidth, rect.Height);
                spriteBatch.Draw(GetTexture(), stripeRect, color);
            }
        }

        /// <summary>
        /// Draws dots pattern for Moderate category tiles.
        /// </summary>
        private void DrawDotsPattern(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            const int dotSpacing = 16;
            const int dotSize = 8;
            for (int x = rect.Left + dotSpacing / 2; x < rect.Right; x += dotSpacing)
            {
                for (int y = rect.Top + dotSpacing / 2; y < rect.Bottom; y += dotSpacing)
                {
                    var dotRect = new Rectangle(x, y, dotSize, dotSize);
                    spriteBatch.Draw(GetTexture(), dotRect, color);
                }
            }
        }

        /// <summary>
        /// Draws a tooltip at the specified screen position using the game's native tooltip renderer.
        /// Measures the string, draws a background rectangle with padding, draws text with a 1px drop shadow,
        /// and handles edge cases where the tooltip would go off-screen.
        /// </summary>
        private void DrawTooltip(SpriteBatch spriteBatch, TooltipData tooltipData)
        {
            var text = tooltipData.Text;
            var position = tooltipData.Position;
            var bgColor = tooltipData.BackgroundColor;
            bgColor.A = 200; // opaque background

            var textSize = Game1.smallFont.MeasureString(text);

            var paddingX = 8;
            var paddingY = 4;

            var tooltipX = (int)position.X + 16;
            var tooltipY = (int)position.Y + 16;

            var bgWidth = (int)textSize.X + paddingX * 2;
            var bgHeight = (int)textSize.Y + paddingY * 2;

            // Handle edge cases: keep tooltip within viewport bounds
            if (tooltipX + bgWidth > Game1.viewport.Width)
            {
                tooltipX = Game1.viewport.Width - bgWidth;
            }
            if (tooltipY + bgHeight > Game1.viewport.Height)
            {
                tooltipY = Game1.viewport.Height - bgHeight;
            }
            if (tooltipX < 0)
            {
                tooltipX = 0;
            }
            if (tooltipY < 0)
            {
                tooltipY = 0;
            }

            var tooltipRect = new Rectangle(tooltipX, tooltipY, bgWidth, bgHeight);

            // Draw background using tooltip color (gray for unknown per FR-014)
            spriteBatch.Draw(GetTexture(), tooltipRect, bgColor);

            // Draw text with 1px drop shadow using tooltip text color
            var shadowColor = new Color(0, 0, 0, 128);
            var textColor = tooltipData.TextColor;
            var textPosX = tooltipX + paddingX;
            var textPosY = tooltipY + paddingY;
            spriteBatch.DrawString(Game1.smallFont, text, new Vector2(textPosX + 1, textPosY + 1), shadowColor);
            spriteBatch.DrawString(Game1.smallFont, text, new Vector2(textPosX, textPosY), textColor);
        }

        /// <summary>
        /// Draws hoe feedback effects (flash and floating text) for a single feedback entry.
        /// </summary>
        private void DrawHoeFeedback(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime, VisualizationConfiguration config)
        {
            var currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;
            var elapsedMs = currentTime - feedback.StartTime;
            var maxDuration = Math.Max(feedback.FlashDuration, feedback.TextDuration);

            if (elapsedMs >= maxDuration)
            {
                return;
            }

            // FR-003: flash effect at tile's health category color
            if (elapsedMs < feedback.FlashDuration)
            {
                var flashRect = new Rectangle(
                    feedback.TilePosition.X * ModConstants.TileSize,
                    feedback.TilePosition.Y * ModConstants.TileSize,
                    ModConstants.TileSize,
                    ModConstants.TileSize);

                var flashColor = _colorService.GetColorForHealth(feedback.HealthValue, config.Opacity);
                var alpha = (byte)(255f * (1f - (float)elapsedMs / feedback.FlashDuration));
                flashColor.A = alpha;

                spriteBatch.Draw(GetTexture(), flashRect, flashColor);
            }

            // FR-003: floating text showing health status
            if (elapsedMs < feedback.TextDuration)
            {
                var textY = feedback.TilePosition.Y * ModConstants.TileSize - (float)elapsedMs * 0.02f;
                var textPos = new Vector2(
                    feedback.TilePosition.X * ModConstants.TileSize + ModConstants.TileSize / 2,
                    textY);

                _monitor.Log($"Hoe feedback text: {feedback.HealthText} at ({textPos.X}, {textPos.Y})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// T101 (FR-010): multiplies the overlay tint by the game's ambient light
        /// so the overlay blends under the lighting pass instead of rendering at
        /// full strength over a darkened world. White ambient (full daylight) is
        /// the identity, so the day tint is the unlit tint exactly; a darkened
        /// ambient darkens the tint proportionally. Alpha is preserved: a
        /// zero-alpha overlay would be invisible in both cases.
        /// </summary>
        /// <param name="color">The unlit tint the palette resolved.</param>
        /// <returns>The ambient-blended tint.</returns>
        private static Color BlendUnderAmbientLight(Color color)
        {
            var ambient = StardewValley.Game1.ambientLight;
            if (ambient == default)
            {
                // A host without the game booted has no ambient light installed;
                // the unlit tint is then the honest answer (identity blend).
                return color;
            }

            var r = (byte)Math.Clamp((int)color.R * ambient.R / 255, 0, 255);
            var g = (byte)Math.Clamp((int)color.G * ambient.G / 255, 0, 255);
            var b = (byte)Math.Clamp((int)color.B * ambient.B / 255, 0, 255);
            return new Color(r, g, b, color.A);
        }

        /// <summary>
        /// Converts screen pixel coordinates to tile coordinates using SMAPI mapping (FR-002).
        /// </summary>
        private static Point CursorToTile(Vector2 screenPos)
        {
            var scaled = Utility.ModifyCoordinatesForUIScale(screenPos);
            var tileX = (int)(scaled.X / Game1.tileSize);
            var tileY = (int)(scaled.Y / Game1.tileSize);
            return new Point(tileX, tileY);
        }

        /// <summary>
        /// Formats tooltip text per spec: "Soil Health: {percentage}% ({category})".
        /// </summary>
        private static string FormatTooltipText(float healthValue, HealthCategory category)
        {
            // T159 / FR-011: floor Poor at 1; suppress fabricated values for NaN/Unknown
            if (category == HealthCategory.Poor && healthValue < 1f)
            {
                healthValue = 1f;
            }
            if (category == HealthCategory.Unknown || float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return "Soil Health: Unknown";
            }
            return $"Soil Health: {healthValue:F0}% ({category})";
        }

        /// <summary>
        /// Formats health text for hoe feedback display.
        /// </summary>
        private static string FormatHealthText(float healthValue, HealthCategory category)
        {
            if (category == HealthCategory.Unknown || float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return "Soil Health: Unknown";
            }
            return $"Soil Health: {healthValue:F0}% ({category})";
        }
    }
}
