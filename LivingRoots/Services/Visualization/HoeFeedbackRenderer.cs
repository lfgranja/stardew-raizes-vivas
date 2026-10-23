using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Renders hoe feedback effects: a flash overlay (FR-003, 300ms) and floating
    /// health text (1000ms) that fades out over its duration.
    /// Per FR-011, only the targeted tile receives feedback.
    /// </summary>
    public class HoeFeedbackRenderer
    {
        private readonly IVisualizationConfigurationService _configService;
        private readonly IMonitor _monitor;
        private readonly IColorInterpolationService _colorInterpolationService;
        private Texture2D? _whiteTexture;

        /// <summary>
        /// Initializes a new instance of the <see cref="HoeFeedbackRenderer"/> class.
        /// </summary>
        /// <param name="configService">Visualization configuration service.</param>
        /// <param name="monitor">Mod monitor for logging.</param>
        /// <param name="colorInterpolationService">Service for resolving health categories and colors.</param>
        public HoeFeedbackRenderer(
            IVisualizationConfigurationService configService,
            IMonitor monitor,
            IColorInterpolationService colorInterpolationService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _colorInterpolationService = colorInterpolationService ?? throw new ArgumentNullException(nameof(colorInterpolationService));
        }

        /// <summary>
        /// Renders a colored flash overlay on the targeted tile for 300ms with
        /// a linear fade-out animation. No-op if the flash duration has elapsed.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="feedback">Feedback state containing tile position and health data.</param>
        /// <param name="gameTime">Current game time for animation progress.</param>
        public void RenderFlash(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime)
        {
            if (spriteBatch == null || feedback == null)
                return;

            long elapsedMs = GetElapsedMilliseconds(feedback, gameTime);
            if (elapsedMs < 0 || elapsedMs >= feedback.FlashDuration)
                return;

            float progress = feedback.FlashDuration > 0
                ? elapsedMs / (float)feedback.FlashDuration
                : 1.0f;
            float opacity = 1.0f - progress;

            Color flashColor = _colorInterpolationService.GetColorForHealth(feedback.HealthValue, opacity);
            Vector2 screenPos = TileToScreenPosition(feedback.TilePosition);

            var destination = new Rectangle(
                (int)screenPos.X,
                (int)screenPos.Y,
                ModConstants.TileSize,
                ModConstants.TileSize);

            spriteBatch.Draw(WhiteTexture, destination, flashColor);
        }

        /// <summary>
        /// Renders floating health text above the targeted tile for 1000ms with
        /// a linear fade-out animation. No-op if the text duration has elapsed.
        /// The text is white — a constant, never a health- or category-derived
        /// color (FR-003): the category is already conveyed by the text
        /// content and the tile overlay color, so the color interpolation
        /// service is never consulted for the text color.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="feedback">Feedback state containing tile position and health data.</param>
        /// <param name="gameTime">Current game time for animation progress.</param>
        public void RenderFloatingText(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime)
        {
            if (feedback == null)
                return;

            long elapsedMs = GetElapsedMilliseconds(feedback, gameTime);
            if (elapsedMs < 0 || elapsedMs >= feedback.TextDuration)
                return;

            var anchor = ComputeFloatingTextAnchor(feedback, elapsedMs);
            RenderFloatingText(spriteBatch, feedback, anchor, elapsedMs);
        }

        /// <summary>
        /// Renders the floating text at a precomputed anchor. The anchor is
        /// logged before the sprite-batch guard so the position function
        /// stays observable on every in-window frame even when the host
        /// cannot supply a SpriteBatch — the headless unit-test host passes
        /// null, and the trace log is the only frame-level observable of
        /// where the text draws.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="feedback">Feedback state containing the health text.</param>
        /// <param name="anchor">Precomputed floating-text anchor.</param>
        /// <param name="elapsedMs">Milliseconds into the feedback's text window.</param>
        private void RenderFloatingText(SpriteBatch spriteBatch, HoeFeedback feedback, FloatingTextAnchor anchor, long elapsedMs)
        {
            _monitor.Log($"Hoe feedback text: {feedback.HealthText} at ({anchor.Position.X}, {anchor.Position.Y})", LogLevel.Trace);

            if (spriteBatch == null)
                return;

            // FR-003: linear fade-out over the text window, applied to the
            // white text and its drop shadow together so neither outlives
            // the other. Base tones from ModConstants (T185); the alpha is
            // the window opacity scaled to the byte range — the conversion
            // idiom the category-color alpha application uses — so both
            // fade together.
            float progress = feedback.TextDuration > 0
                ? elapsedMs / (float)feedback.TextDuration
                : 1.0f;
            float opacity = 1.0f - progress;
            var textColor = ModConstants.FloatingTextColor;
            textColor.A = (byte)(opacity * 255f);
            var shadowColor = ModConstants.FloatingTextShadowColor;
            shadowColor.A = (byte)(opacity * 128f);

            // Center the measured string on the anchor and draw it with a
            // 1px drop shadow so it stays legible over any backdrop.
            Vector2 textSize = Game1.smallFont.MeasureString(feedback.HealthText);
            Vector2 textPos = new Vector2(
                anchor.Position.X - (textSize.X / 2),
                anchor.Position.Y - textSize.Y);

            spriteBatch.DrawString(Game1.smallFont, feedback.HealthText, textPos + Vector2.One, shadowColor);
            spriteBatch.DrawString(Game1.smallFont, feedback.HealthText, textPos, textColor);
        }

        /// <summary>
        /// Creates a new <see cref="HoeFeedback"/> instance for the specified tile.
        /// HealthText follows the format "Soil Health: {percentage}% ({category})".
        /// StartTime is captured from the current game clock.
        /// </summary>
        /// <param name="tilePosition">Tile coordinates targeted by the hoe.</param>
        /// <param name="healthValue">Current soil health value (0-100).</param>
        /// <returns>Initialized feedback state ready for rendering.</returns>
        public HoeFeedback CreateFeedback(Point tilePosition, float healthValue, GameTime gameTime)
        {
            HealthCategory category = _colorInterpolationService.GetCategoryForHealth(healthValue);

            if (category == HealthCategory.Unknown || float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return new HoeFeedback
                {
                    TilePosition = tilePosition,
                    StartTime = (long)gameTime.TotalGameTime.TotalMilliseconds,
                    FlashDuration = ModConstants.FlashDurationMs,
                    TextDuration = ModConstants.TextDurationMs,
                    HealthValue = healthValue,
                    Category = category,
                    HealthText = "Soil Health: Unknown"
                };
            }

            int percentage = (int)Math.Clamp(Math.Round(healthValue), 0f, 100f);

            return new HoeFeedback
            {
                TilePosition = tilePosition,
                StartTime = (long)gameTime.TotalGameTime.TotalMilliseconds,
                FlashDuration = ModConstants.FlashDurationMs,
                TextDuration = ModConstants.TextDurationMs,
                HealthValue = healthValue,
                Category = category,
                HealthText = $"Soil Health: {percentage}% ({category})"
            };
        }

        /// <summary>
        /// Checks whether the feedback is still within its active rendering window.
        /// Delegates to <see cref="HoeFeedback.IsActive(long)"/> using the game clock.
        /// </summary>
        /// <param name="feedback">Feedback state to check.</param>
        /// <param name="gameTime">Current game time.</param>
        /// <returns>True if the feedback should be rendered; otherwise, false.</returns>
        public bool IsFeedbackActive(HoeFeedback feedback, GameTime gameTime)
        {
            if (feedback == null)
                return false;

            long currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;
            return feedback.IsActive(currentTime);
        }

        /// <summary>
        /// Gets the elapsed milliseconds since the feedback was created.
        /// </summary>
        private static long GetElapsedMilliseconds(HoeFeedback feedback, GameTime gameTime)
        {
            long currentTime = (long)gameTime.TotalGameTime.TotalMilliseconds;
            return currentTime - feedback.StartTime;
        }

        /// <summary>
        /// Converts tile coordinates to screen-space pixel
        /// coordinates through the game's own world→screen
        /// derivation: the tile's world position
        /// (<c>tile * tileSize</c>) is localized with
        /// <c>Game1.GlobalToLocal</c> — which subtracts the
        /// pixel-space <c>Game1.viewport</c> origin — and
        /// scaled with <c>Utility.ModifyCoordinatesForUIScale</c>,
        /// which folds in the zoom level
        /// (<c>Game1.options.zoomLevel</c>) and the UI scale
        /// (<c>Game1.options.uiScale</c>), so the flash lands
        /// on the targeted tile at any camera offset and any
        /// zoom / UI scale (FR-003, FR-018, US3/AC4 — the
        /// same derivation the floating-text anchor and the
        /// tile-overlay destination follow, T183). The
        /// derivation runs through the shared
        /// <see cref="ScreenPositionDerivation"/>, which
        /// resolves the legacy tile-space position when the
        /// game state is unavailable (the headless unit-test
        /// host — the identity at the default zoom and UI
        /// scale, so the T160 camera-relative expectations
        /// hold at unity zoom / UI scale).
        /// </summary>
        private static Vector2 TileToScreenPosition(Point tilePosition)
        {
            var worldPosition = new Vector2(
                tilePosition.X * ModConstants.TileSize,
                tilePosition.Y * ModConstants.TileSize);
            return ScreenPositionDerivation.ToScreenPosition(worldPosition);
        }

        /// <summary>
        /// Computes the floating-text anchor for the feedback's
        /// current elapsed lifetime: horizontally centered on the
        /// targeted tile, starting above the tile's top edge and
        /// drifting upward over the text window (FR-003). The
        /// tile-center world position follows the game's own
        /// world→screen derivation —
        /// <c>Utility.ModifyCoordinatesForUIScale(
        /// Game1.GlobalToLocal(worldPosition))</c>, which folds
        /// in the viewport origin, the zoom level and the UI
        /// scale — through the shared
        /// <see cref="ScreenPositionDerivation"/>, which
        /// resolves the legacy tile-space position when the game
        /// state is unavailable (the headless unit-test host —
        /// the identity at the default zoom and UI scale) — and
        /// the drift is a screen-space rate
        /// (ModConstants.FloatingTextDriftPixelsPerMs)
        /// subtracted from that derived screen position, so the
        /// text stays above the targeted tile at any camera
        /// offset and any zoom / UI scale (FR-003, FR-018,
        /// US3/AC4 — the same derivation the flash rect and the
        /// tile-overlay destination follow, T183).
        /// </summary>
        /// <param name="feedback">Feedback state containing the tile position.</param>
        /// <param name="elapsedMs">Milliseconds into the feedback's text window.</param>
        /// <returns>The anchor the floating text draws from.</returns>
        private static FloatingTextAnchor ComputeFloatingTextAnchor(HoeFeedback feedback, long elapsedMs)
        {
            var worldCenter = new Vector2(
                feedback.TilePosition.X * ModConstants.TileSize + ModConstants.TileSize / 2,
                feedback.TilePosition.Y * ModConstants.TileSize);
            var screenPosition = ScreenPositionDerivation.ToScreenPosition(worldCenter);
            var position = new Vector2(
                screenPosition.X,
                screenPosition.Y - elapsedMs * ModConstants.FloatingTextDriftPixelsPerMs);
            return new FloatingTextAnchor(position);
        }

        /// <summary>
        /// The anchor the floating text draws from — the point the position
        /// function resolves for the feedback's current elapsed lifetime,
        /// logged as the frame-level observable of the drawn position.
        /// </summary>
        private readonly struct FloatingTextAnchor
        {
            public FloatingTextAnchor(Vector2 position)
            {
                Position = position;
            }

            public Vector2 Position { get; }
        }

        /// <summary>
        /// Lazily creates and caches a 1x1 white texture used for the flash overlay.
        /// </summary>
        private Texture2D WhiteTexture => _whiteTexture ??= CreateWhiteTexture();

        private static Texture2D CreateWhiteTexture()
        {
            var texture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            return texture;
        }
    }
}
