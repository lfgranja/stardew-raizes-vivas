using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

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
        IMonitor monitor) : IVisualizationService
    {
        /// <summary>Standard Stardew Valley tile size in pixels.</summary>
        public const int TileSize = 64;

        private readonly IColorInterpolationService _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        private readonly IVisualizationConfigurationService _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        // White texture for drawing rectangles (set via Initialize)
        private Texture2D? _whiteTexture;

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

        // Overlay renderer for computing visible tile overlays
        private readonly OverlayRenderer _overlayRenderer = new(colorService, configService, monitor);

        // Tile health data snapshot for current frame
        private Dictionary<Point, float> _tileHealthData = new();
        private readonly object _dataLock = new();

        // Tooltip state for throttling (FR-015)
        private string _lastTooltipText = string.Empty;
        private Point _lastTooltipTile;
        private long _lastTooltipTime;

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
        public void RenderOverlays(SpriteBatch spriteBatch, Rectangle viewport, GameTime gameTime)
        {
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

            // Get overlays with viewport culling and caching (FR-009, FR-010)
            var overlays = _overlayRenderer.GetOverlays(viewport, dataSnapshot);

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
            {
                return;
            }

            // Convert screen position to tile coordinates
            var tilePos = ScreenToTile(cursorPosition);

            // Look up health data for the tile under cursor
            float healthValue;
            lock (_dataLock)
            {
                if (!_tileHealthData.TryGetValue(tilePos, out healthValue))
                {
                    return; // No data for this tile, no tooltip
                }
            }

            // FR-015: throttle tooltip updates to minimum 50ms interval
            var currentTime = gameTime.TotalGameTime.Ticks;
            var tooltipText = FormatTooltipText(healthValue);

            if (tooltipText == _lastTooltipText && tilePos == _lastTooltipTile)
            {
                var elapsedMs = (double)(currentTime - _lastTooltipTime) / TimeSpan.TicksPerMillisecond;
                if (elapsedMs < ModConstants.TooltipDebounceMs)
                {
                    return; // Throttled
                }
            }

            _lastTooltipText = tooltipText;
            _lastTooltipTile = tilePos;
            _lastTooltipTime = currentTime;

            // Draw tooltip at cursor position
            DrawTooltip(spriteBatch, tooltipText, cursorPosition, config);
        }

        /// <inheritdoc />
        public void RenderHoeFeedback(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var config = _configService.GetConfiguration();

            if (!config.HoeFeedbackEnabled)
            {
                return;
            }

            // Snapshot active feedbacks under lock, removing expired ones
            List<HoeFeedback> activeFeedbacks;
            lock (_feedbackLock)
            {
                _activeFeedbacks.RemoveAll(f => f.IsExpired(gameTime.TotalGameTime.Ticks));
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
                StartTime = DateTime.UtcNow.Ticks,
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
        public void InvalidateCache()
        {
            _overlayRenderer.InvalidateCache();
        }

        /// <summary>
        /// Draws a single tile overlay with color, opacity, and optional pattern.
        /// </summary>
        private void DrawOverlay(SpriteBatch spriteBatch, TileOverlay overlay, VisualizationConfiguration config, Rectangle viewport)
        {
            // Convert tile position to screen coordinates relative to viewport
            var screenX = (overlay.TilePosition.X - viewport.X) * TileSize;
            var screenY = (overlay.TilePosition.Y - viewport.Y) * TileSize;

            var destRect = new Rectangle(screenX, screenY, TileSize, TileSize);

            // Draw the base color overlay with configured opacity
            spriteBatch.Draw(GetTexture(), destRect, overlay.Color);

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
            var patternColor = overlay.Color;
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
        /// Draws a tooltip at the specified screen position.
        /// </summary>
        private void DrawTooltip(SpriteBatch spriteBatch, string text, Vector2 position, VisualizationConfiguration config)
        {
            // Tooltip background
            var bgColor = new Color(0, 0, 0, 200);
            var textColor = Color.White;

            // Position tooltip offset from cursor
            var tooltipX = (int)position.X + 16;
            var tooltipY = (int)position.Y + 16;

            // Estimate text dimensions (approximate without font measurement)
            var textWidth = text.Length * 8 + 16;
            var textHeight = 24;

            var tooltipRect = new Rectangle(tooltipX, tooltipY, textWidth, textHeight);

            // Draw background
            spriteBatch.Draw(GetTexture(), tooltipRect, bgColor);

            // Note: Text rendering would use Game1.smallFont or similar in a full implementation
            // spriteBatch.DrawString(Game1.smallFont, text, new Vector2(tooltipX + 8, tooltipY + 4), textColor);
            _monitor.Log($"Tooltip rendered: {text}", LogLevel.Trace);
        }

        /// <summary>
        /// Draws hoe feedback effects (flash and floating text) for a single feedback entry.
        /// </summary>
        private void DrawHoeFeedback(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime, VisualizationConfiguration config)
        {
            var currentTime = gameTime.TotalGameTime.Ticks;
            var elapsedMs = (double)(currentTime - feedback.StartTime) / TimeSpan.TicksPerMillisecond;
            var maxDuration = Math.Max(feedback.FlashDuration, feedback.TextDuration);

            if (elapsedMs >= maxDuration)
            {
                return;
            }

            // FR-003: flash effect at tile's health category color
            if (elapsedMs < feedback.FlashDuration)
            {
                var flashRect = new Rectangle(
                    feedback.TilePosition.X * TileSize,
                    feedback.TilePosition.Y * TileSize,
                    TileSize,
                    TileSize);

                var flashColor = _colorService.GetColorForHealth(feedback.HealthValue, config.Opacity);
                var alpha = (byte)(255f * (1f - (float)elapsedMs / feedback.FlashDuration));
                flashColor.A = alpha;

                spriteBatch.Draw(GetTexture(), flashRect, flashColor);
            }

            // FR-003: floating text showing health status
            if (elapsedMs < feedback.TextDuration)
            {
                var textY = feedback.TilePosition.Y * TileSize - (float)elapsedMs * 0.02f;
                var textPos = new Vector2(
                    feedback.TilePosition.X * TileSize + TileSize / 2,
                    textY);

                // Note: Text rendering would use Game1.smallFont or similar in a full implementation
                // spriteBatch.DrawString(Game1.smallFont, feedback.HealthText, textPos, Color.White);
                _monitor.Log($"Hoe feedback text: {feedback.HealthText} at ({textPos.X}, {textPos.Y})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Converts screen pixel coordinates to tile coordinates.
        /// </summary>
        private static Point ScreenToTile(Vector2 screenPos)
        {
            var tileX = (int)(screenPos.X / TileSize);
            var tileY = (int)(screenPos.Y / TileSize);
            return new Point(tileX, tileY);
        }

        /// <summary>
        /// Formats tooltip text per spec: "Soil Health: {percentage}% ({category})".
        /// </summary>
        private static string FormatTooltipText(float healthValue)
        {
            var category = healthValue switch
            {
                >= 0 and < 34 => "Poor",
                >= 34 and < 67 => "Moderate",
                >= 67 and <= 100 => "Healthy",
                _ => "Unknown"
            };
            return $"Soil Health: {healthValue:F0}% ({category})";
        }

        /// <summary>
        /// Formats health text for hoe feedback display.
        /// </summary>
        private static string FormatHealthText(float healthValue, HealthCategory category)
        {
            return $"Soil Health: {healthValue:F0}% ({category})";
        }
    }
}
