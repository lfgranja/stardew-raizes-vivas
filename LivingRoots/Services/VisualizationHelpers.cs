using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Utility class providing helper methods for visualization operations.
    /// Provides shared functionality for health level text, tile visibility checks, and viewport operations.
    /// </summary>
    public static class VisualizationHelpers
    {
        // Rendering constants
        private const int TileSize = 64; // Stardew Valley tile size in pixels

        /// <summary>
        /// Gets or creates a simple texture for rendering.
        /// </summary>
        /// <returns>A 1x1 white texture that can be scaled</returns>
        private static Texture2D? _overlayTexture;
        public static Texture2D GetOrCreateOverlayTexture()
        {
            if (_overlayTexture != null)
            {
                return _overlayTexture;
            }

            // Try to get existing texture from game
            if (Game1.staminaRect != null)
            {
                _overlayTexture = Game1.staminaRect;
            }
            else
            {
                try
                {
                    _overlayTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                    _overlayTexture.SetData(new[] { Color.White });
                }
                catch (Exception ex)
                {
                    // Log error via IMonitor if available, or rethrow
                    throw new InvalidOperationException("Failed to create overlay texture", ex);
                }
            }
            return _overlayTexture;
        }


        /// <summary>
        /// Gets health level text for a given health value.
        /// </summary>
        /// <param name="health">The soil health value (0-100)</param>
        /// <returns>Health level text: "Poor", "Moderate", or "Healthy"</returns>
        public static string GetHealthLevelText(float health)
        {
            // Clamp health to valid range
            health = Math.Clamp(health, 0f, 100f);

            // Determine health category based on thresholds
            if (health <= ModConstants.PoorHealthThreshold)
            {
                return "Poor";
            }
            else if (health <= ModConstants.ModerateHealthThreshold)
            {
                return "Moderate";
            }
            else
            {
                return "Healthy";
            }
        }

        /// <summary>
        /// Checks if a tile is visible within current viewport.
        /// </summary>
        /// <param name="tile">The tile coordinates to check</param>
        /// <returns>True if tile is visible in viewport, false otherwise</returns>
        public static bool IsTileVisible(Vector2 tile)
        {
            try
            {
                // Get viewport bounds
                Rectangle viewport = GetViewportBounds();

                // Calculate tile screen position
                Vector2 screenPosition = GetTileScreenPosition(tile);

                // Check if tile intersects with viewport
                return viewport.Intersects(new Rectangle(
                    (int)screenPosition.X,
                    (int)screenPosition.Y,
                    TileSize,
                    TileSize
                ));
            }
            catch
            {
                // If we can't determine visibility, assume visible to avoid missing overlays
                return true;
            }
        }

        /// <summary>
        /// Checks if a tile is visible within a specific viewport.
        /// </summary>
        /// <param name="tile">The tile coordinates to check</param>
        /// <param name="viewport">The viewport rectangle to check against</param>
        /// <returns>True if tile is visible in viewport, false otherwise</returns>
        public static bool IsTileVisible(Vector2 tile, Rectangle viewport)
        {
            try
            {
                // Calculate tile screen position
                Vector2 screenPosition = GetTileScreenPosition(tile);

                // Check if tile intersects with viewport
                return viewport.Intersects(new Rectangle(
                    (int)screenPosition.X,
                    (int)screenPosition.Y,
                    TileSize,
                    TileSize
                ));
            }
            catch
            {
                // If we can't determine visibility, assume visible to avoid missing overlays
                return true;
            }
        }

        /// <summary>
        /// Gets current viewport bounds.
        /// </summary>
        /// <returns>The viewport rectangle</returns>
        public static Rectangle GetViewportBounds()
        {
            // Return default viewport if Game1.viewport is not available
            if (Game1.viewport.Width <= 0 || Game1.viewport.Height <= 0)
            {
                return new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
            }

            return new Rectangle(
                Game1.viewport.X,
                Game1.viewport.Y,
                Game1.viewport.Width,
                Game1.viewport.Height
            );
        }

        /// <summary>
        /// Calculates screen position for a tile.
        /// </summary>
        /// <param name="tile">The tile coordinates</param>
        /// <returns>The screen position in pixels</returns>
        public static Vector2 GetTileScreenPosition(Vector2 tile)
        {
            return new Vector2(tile.X * TileSize, tile.Y * TileSize);
        }

        /// <summary>
        /// Validates tile coordinates.
        /// </summary>
        /// <param name="tile">The tile coordinates to validate</param>
        /// <returns>True if tile coordinates are valid, false otherwise</returns>
        public static bool IsValidTile(Vector2 tile)
        {
            // Check for NaN or Infinity
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) ||
                float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                return false;
            }

            // Check for extreme values (security constraint)
            if (Math.Abs(tile.X) > ModConstants.MaxAbsoluteTileCoordinate ||
                Math.Abs(tile.Y) > ModConstants.MaxAbsoluteTileCoordinate)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clamps a health value to valid range [0, 100].
        /// </summary>
        /// <param name="health">The health value to clamp</param>
        /// <returns>The clamped health value</returns>
        public static float ClampHealth(float health)
        {
            return Math.Clamp(health, 0f, 100f);
        }

        /// <summary>
        /// Calculates tile coordinates from a screen position.
        /// </summary>
        /// <param name="screenPosition">The screen position in pixels</param>
        /// <returns>The tile coordinates</returns>
        public static Vector2 GetTileFromScreenPosition(Vector2 screenPosition)
        {
            return new Vector2(
                (float)Math.Floor(screenPosition.X / TileSize),
                (float)Math.Floor(screenPosition.Y / TileSize)
            );
        }

        /// <summary>
        /// Applies opacity to a color.
        /// </summary>
        /// <param name="color">The base color</param>
        /// <param name="opacity">The opacity value (0.0 to 1.0)</param>
        /// <returns>The color with applied opacity</returns>
        public static Color ApplyOpacity(Color color, float opacity)
        {
            // Clamp opacity to valid range
            opacity = Math.Clamp(opacity, 0.0f, 1.0f);

            return new Color(color.R, color.G, color.B, (byte)(color.A * opacity));
        }

        /// <summary>
        /// Gets health category for a given health value.
        /// </summary>
        /// <param name="health">The soil health value (0-100)</param>
        /// <returns>The health category</returns>
        public static HealthCategory GetHealthCategory(float health)
        {
            // Clamp health to valid range
            health = Math.Clamp(health, 0f, 100f);

            // Determine health category based on thresholds
            if (health <= ModConstants.PoorHealthThreshold)
            {
                return HealthCategory.Poor;
            }
            else if (health <= ModConstants.ModerateHealthThreshold)
            {
                return HealthCategory.Moderate;
            }
            else
            {
                return HealthCategory.Healthy;
            }
        }
    }

    /// <summary>
    /// Represents health category of soil.
    /// </summary>
    public enum HealthCategory
    {
        /// <summary>Poor soil health (0-33)</summary>
        Poor,

        /// <summary>Moderate soil health (34-66)</summary>
        Moderate,

        /// <summary>Healthy soil health (67-100)</summary>
        Healthy
    }
}
