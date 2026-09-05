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
        private const int TileSize = 64;
        private const int FlashDurationMs = 300;
        private const int TextDurationMs = 1000;
        private const float FullOpacity = 1.0f;

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
                : FullOpacity;
            float opacity = FullOpacity - progress;

            Color flashColor = _colorInterpolationService.GetColorForHealth(feedback.HealthValue, opacity);
            Vector2 screenPos = TileToScreenPosition(feedback.TilePosition);

            var destination = new Rectangle(
                (int)screenPos.X,
                (int)screenPos.Y,
                TileSize,
                TileSize);

            spriteBatch.Draw(WhiteTexture, destination, flashColor);
        }

        /// <summary>
        /// Renders floating health text above the targeted tile for 1000ms with
        /// a linear fade-out animation. No-op if the text duration has elapsed.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="feedback">Feedback state containing tile position and health data.</param>
        /// <param name="gameTime">Current game time for animation progress.</param>
        public void RenderFloatingText(SpriteBatch spriteBatch, HoeFeedback feedback, GameTime gameTime)
        {
            if (spriteBatch == null || feedback == null)
                return;

            long elapsedMs = GetElapsedMilliseconds(feedback, gameTime);
            if (elapsedMs < 0 || elapsedMs >= feedback.TextDuration)
                return;

            float progress = feedback.TextDuration > 0
                ? elapsedMs / (float)feedback.TextDuration
                : FullOpacity;
            float opacity = FullOpacity - progress;

            Color textColor = _colorInterpolationService.GetColorForHealth(feedback.HealthValue, opacity);
            Vector2 screenPos = TileToScreenPosition(feedback.TilePosition);

            // Center text horizontally above the tile
            Vector2 textSize = Game1.smallFont.MeasureString(feedback.HealthText);
            Vector2 textPos = new Vector2(
                screenPos.X + (TileSize / 2) - (textSize.X / 2),
                screenPos.Y - textSize.Y - 4);

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
            int percentage = (int)Math.Clamp(Math.Round(healthValue), 0f, 100f);

            return new HoeFeedback
            {
                TilePosition = tilePosition,
                StartTime = (long)gameTime.TotalGameTime.TotalMilliseconds,
                FlashDuration = FlashDurationMs,
                TextDuration = TextDurationMs,
                HealthValue = healthValue,
                Category = category,
                HealthText = $"Soil Health: {percentage}% ({category})"
            };
        }

        /// <summary>
        /// Checks whether the feedback is still within its active rendering window.
        /// Delegates to <see cref="HoeFeedback.IsActive"/> using the game clock.
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
        /// Converts tile coordinates to screen-space pixel coordinates.
        /// </summary>
        private static Vector2 TileToScreenPosition(Point tilePosition)
        {
            return new Vector2(tilePosition.X * TileSize, tilePosition.Y * TileSize);
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
