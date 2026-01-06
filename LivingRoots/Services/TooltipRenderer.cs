using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of tooltip and hoe feedback renderer for soil health visualization.
    /// Renders hover tooltips with soil health information and visual feedback for hoe actions.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TooltipRenderer.
    /// </remarks>
    /// <param name="monitor">Monitor for logging</param>
    /// <param name="config">Visualization configuration</param>
    /// <param name="colorMapper">Color mapper for health values</param>
    public class TooltipRenderer(
        IMonitor monitor,
        IVisualizationConfig config,
        IColorMapper colorMapper) : ITooltipRenderer
    {
        // Dependencies
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        private readonly IVisualizationConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IColorMapper _colorMapper = colorMapper ?? throw new ArgumentNullException(nameof(colorMapper));

        // Tooltip styling constants
        private const int TooltipPadding = 8;
        private const int TooltipBorderWidth = 2;
        private const int TooltipLineHeight = 16;
        private const int CursorOffsetX = 16;
        private const int CursorOffsetY = -32;

        // Hoe feedback constants
        private const int FloatingTextOffsetY = -20;

        /// <inheritdoc/>
        public void RenderHoverTooltip(SpriteBatch spriteBatch, Vector2 cursorPosition, float health)
        {
            try
            {
                // Validate parameters
                if (spriteBatch == null)
                {
                    _monitor.Log("SpriteBatch is null, cannot render tooltip.", LogLevel.Trace);
                    return;
                }

                // Check if hover tooltips are enabled
                if (!_config.ShowHoverTooltips)
                {
                    return;
                }

                // Validate health value
                health = SoilHealthService.ClampHealthValue(health);

                // Get health level text
                var healthLevelText = VisualizationHelpers.GetHealthLevelText(health);

                // Format tooltip text
                var tooltipLines = new string[]
                {
                    $"Soil Health: {health:F0}%",
                    $"Status: {healthLevelText}"
                };

                // Get color for health value
                Color healthColor = _colorMapper.GetHealthColor(health);

                // Calculate tooltip position
                Vector2 tooltipPosition = CalculateTooltipPosition(cursorPosition, tooltipLines);

                // Render tooltip background and border
                RenderTooltipBackground(spriteBatch, tooltipPosition, tooltipLines, healthColor);

                // Render tooltip text
                RenderTooltipText(spriteBatch, tooltipPosition, tooltipLines);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering hover tooltip: {ex.Message}", LogLevel.Error);
                _monitor.Log($"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <inheritdoc/>
        public void RenderHoeFeedback(SpriteBatch spriteBatch, Vector2 tilePosition, float health)
        {
            try
            {
                // Validate parameters
                if (spriteBatch == null)
                {
                    _monitor.Log("SpriteBatch is null, cannot render hoe feedback.", LogLevel.Trace);
                    return;
                }

                // Check if hoe feedback is enabled
                if (!_config.ShowHoeFeedback)
                {
                    return;
                }

                // Validate health value
                health = SoilHealthService.ClampHealthValue(health);

                // Get color for health value
                Color healthColor = _colorMapper.GetHealthColor(health);

                // Render visual flash
                RenderFlashEffect(spriteBatch, tilePosition, healthColor);

                // Render floating text
                RenderFloatingText(spriteBatch, tilePosition, health, healthColor);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering hoe feedback: {ex.Message}", LogLevel.Error);
                _monitor.Log($"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Calculates the tooltip position based on cursor position and text content.
        /// </summary>
        /// <param name="cursorPosition">The current cursor position</param>
        /// <param name="tooltipLines">The lines of text in the tooltip</param>
        /// <returns>The calculated tooltip position</returns>
        private static Vector2 CalculateTooltipPosition(Vector2 cursorPosition, string[] tooltipLines)
        {
            // Calculate tooltip dimensions
            var maxLineWidth = 0;
            foreach (var line in tooltipLines)
            {
                Vector2 textSize = Game1.dialogueFont.MeasureString(line);
                maxLineWidth = Math.Max(maxLineWidth, (int)textSize.X);
            }

            var tooltipWidth = maxLineWidth + (TooltipPadding * 2);
            var tooltipHeight = (tooltipLines.Length * TooltipLineHeight) + (TooltipPadding * 2);

            // Calculate position with offset from cursor
            Vector2 position = new Vector2(
                cursorPosition.X + CursorOffsetX,
                cursorPosition.Y + CursorOffsetY
            );

            // Clamp to screen bounds
            position.X = Math.Clamp(position.X, 0, Game1.uiViewport.Width - tooltipWidth);
            position.Y = Math.Clamp(position.Y, 0, Game1.uiViewport.Height - tooltipHeight);

            return position;
        }

        /// <summary>
        /// Renders the tooltip background with border.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="position">The tooltip position</param>
        /// <param name="tooltipLines">The lines of text in the tooltip</param>
        /// <param name="borderColor">The color for the border</param>
        private static void RenderTooltipBackground(SpriteBatch spriteBatch, Vector2 position, string[] tooltipLines, Color borderColor)
        {
            // Calculate tooltip dimensions
            var maxLineWidth = 0;
            foreach (var line in tooltipLines)
            {
                Vector2 textSize = Game1.dialogueFont.MeasureString(line);
                maxLineWidth = Math.Max(maxLineWidth, (int)textSize.X);
            }

            var tooltipWidth = maxLineWidth + (TooltipPadding * 2);
            var tooltipHeight = (tooltipLines.Length * TooltipLineHeight) + (TooltipPadding * 2);

            // Create background rectangle
            Rectangle backgroundRect = new Rectangle(
                (int)position.X,
                (int)position.Y,
                tooltipWidth,
                tooltipHeight
            );

            // Render semi-transparent black background
            Color backgroundColor = new Color(0, 0, 0, 204); // 0.8 alpha
            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                backgroundRect,
                backgroundColor
            );

            // Render border with health color
            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                new Rectangle(
                    backgroundRect.X,
                    backgroundRect.Y,
                    TooltipBorderWidth,
                    backgroundRect.Height
                ),
                borderColor
            ); // Left border

            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                new Rectangle(
                    backgroundRect.X + backgroundRect.Width - TooltipBorderWidth,
                    backgroundRect.Y,
                    TooltipBorderWidth,
                    backgroundRect.Height
                ),
                borderColor
            ); // Right border

            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                new Rectangle(
                    backgroundRect.X,
                    backgroundRect.Y,
                    backgroundRect.Width,
                    TooltipBorderWidth
                ),
                borderColor
            ); // Top border

            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                new Rectangle(
                    backgroundRect.X,
                    backgroundRect.Y + backgroundRect.Height - TooltipBorderWidth,
                    backgroundRect.Width,
                    TooltipBorderWidth
                ),
                borderColor
            ); // Bottom border
        }

        /// <summary>
        /// Renders the tooltip text.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="position">The tooltip position</param>
        /// <param name="tooltipLines">The lines of text to render</param>
        private static void RenderTooltipText(SpriteBatch spriteBatch, Vector2 position, string[] tooltipLines)
        {
            Color textColor = Color.White;

            for (var i = 0; i < tooltipLines.Length; i++)
            {
                Vector2 textPosition = new Vector2(
                    position.X + TooltipPadding,
                    position.Y + TooltipPadding + (i * TooltipLineHeight)
                );

                spriteBatch.DrawString(
                    Game1.dialogueFont,
                    tooltipLines[i],
                    textPosition,
                    textColor
                );
            }
        }

        /// <summary>
        /// Renders a flash effect at the tile position.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="tilePosition">The tile position</param>
        /// <param name="color">The color for the flash</param>
        private static void RenderFlashEffect(SpriteBatch spriteBatch, Vector2 tilePosition, Color color)
        {
            // Create a bright version of the color for the flash
            Color flashColor = new Color(
                Math.Min(255, color.R + 50),
                Math.Min(255, color.G + 50),
                Math.Min(255, color.B + 50),
                200
            );

            // Render flash rectangle
            spriteBatch.Draw(
                VisualizationHelpers.GetOrCreateOverlayTexture(),
                new Rectangle((int)tilePosition.X, (int)tilePosition.Y, 64, 64),
                flashColor
            );
        }

        /// <summary>
        /// Renders floating text at the tile position.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="tilePosition">The tile position</param>
        /// <param name="health">The soil health value</param>
        /// <param name="color">The color for the text</param>
        private static void RenderFloatingText(SpriteBatch spriteBatch, Vector2 tilePosition, float health, Color color)
        {
            // Format floating text
            var text = $"{health:F0}%";

            // Calculate text position (above the tile)
            Vector2 textPosition = new Vector2(
                tilePosition.X + 32 - (Game1.dialogueFont.MeasureString(text).X / 2),
                tilePosition.Y + FloatingTextOffsetY
            );

            // Render text with a slight shadow for visibility
            spriteBatch.DrawString(
                Game1.dialogueFont,
                text,
                new Vector2(textPosition.X + 1, textPosition.Y + 1),
                Color.Black
            ); // Shadow

            spriteBatch.DrawString(
                Game1.dialogueFont,
                text,
                textPosition,
                color
            ); // Main text
        }
    }
}
