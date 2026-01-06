using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of tile overlay renderer for soil health visualization.
    /// Renders colored rectangles over tiles based on soil health values with viewport culling.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TileOverlayRenderer.
    /// </remarks>
    /// <param name="monitor">Monitor for logging</param>
    /// <param name="config">Visualization configuration</param>
    /// <param name="colorMapper">Color mapper for health values</param>
    /// <param name="soilHealthService">Service for retrieving soil health data</param>
    public class TileOverlayRenderer(
        IMonitor monitor,
        IVisualizationConfig config,
        IColorMapper colorMapper,
        ISoilHealthService soilHealthService) : ITileOverlayRenderer
    {
        // Dependencies
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        private readonly IVisualizationConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IColorMapper _colorMapper = colorMapper ?? throw new ArgumentNullException(nameof(colorMapper));
        private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));

        // Rendering constants
        private const int TileSize = 64; // Stardew Valley tile size in pixels

        // Performance: Health cache
        private readonly Dictionary<(string Location, Vector2 Tile), float> _healthCache = new Dictionary<(string, Vector2), float>();
        private readonly int _cacheSize = ModConstants.TileHealthCacheSize;
        private DateTime _lastCacheClear = DateTime.UtcNow;
        private readonly TimeSpan _cacheClearInterval = TimeSpan.FromSeconds(ModConstants.CacheClearIntervalSeconds);

        /// <inheritdoc/>
        public void RenderTileOverlay(SpriteBatch spriteBatch, GameLocation location, Vector2 tile, float health)
        {
            try
            {
                // Validate parameters
                if (spriteBatch == null)
                {
                    _monitor.Log("SpriteBatch is null, cannot render tile overlay.", LogLevel.Trace);
                    return;
                }

                if (location == null)
                {
                    _monitor.Log("GameLocation is null, cannot render tile overlay.", LogLevel.Trace);
                    return;
                }

                if (!IsValidTile(tile))
                {
                    _monitor.Log($"Invalid tile coordinates {tile}, skipping overlay.", LogLevel.Trace);
                    return;
                }

                // Check if tile overlays are enabled
                if (!_config.ShowTileOverlays)
                {
                    _monitor.Log("Tile overlays are disabled, skipping overlay rendering.", LogLevel.Trace);
                    return;
                }

                // Check if tile is visible in viewport
                if (!IsTileVisible(tile))
                {
                    return;
                }

                // Check for missing or invalid soil health data
                if (health <= 0.0001f)
                {
                    _monitor.Log($"Missing soil health data for tile {tile}, skipping overlay.", LogLevel.Trace);
                    return;
                }

                // Get color for health value
                Color baseColor = _colorMapper.GetHealthColor(health);
                Color overlayColor = ApplyOpacity(baseColor, _config.OverlayOpacity);

                // Calculate screen position
                Vector2 screenPosition = GetTileScreenPosition(tile);

                // Render overlay
                RenderColoredRectangle(spriteBatch, screenPosition, overlayColor);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering tile overlay for tile {tile}: {ex.Message}", LogLevel.Error);
                _monitor.Log($"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <inheritdoc/>
        public void RenderAllVisibleOverlays(SpriteBatch spriteBatch, GameLocation location)
        {
            try
            {
                // Validate parameters
                if (!ValidateRenderingParameters(spriteBatch, location))
                {
                    return;
                }

                // Check if tile overlays are enabled
                if (!_config.ShowTileOverlays)
                {
                    _monitor.Log("Tile overlays are disabled, skipping overlay rendering.", LogLevel.Trace);
                    return;
                }

                // Clear cache if needed
                ClearCacheIfNeeded();

                // Get viewport bounds and tile range
                var tileRange = GetViewportTileRange();
                if (tileRange == null)
                {
                    return;
                }

                // Render overlays for visible tiles with performance limit
                var tilesRenderedThisFrame = 0;
                var renderResult = RenderOverlaysForTileRange(
                    spriteBatch, location, tileRange.Value, ref tilesRenderedThisFrame);

                LogRenderingResult(renderResult.OverlayCount, renderResult.SkippedTiles, tilesRenderedThisFrame);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering all visible overlays: {ex.Message}", LogLevel.Error);
                _monitor.Log($"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Validates rendering parameters.
        /// </summary>
        private bool ValidateRenderingParameters(SpriteBatch spriteBatch, GameLocation location)
        {
            if (spriteBatch == null)
            {
                _monitor.Log("SpriteBatch is null, cannot render overlays.", LogLevel.Trace);
                return false;
            }

            if (location == null)
            {
                _monitor.Log("GameLocation is null, cannot render overlays.", LogLevel.Trace);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the tile range for the current viewport.
        /// </summary>
        private (int startX, int startY, int endX, int endY)? GetViewportTileRange()
        {
            Rectangle viewport = GetViewportBounds();

            // Calculate tile range for viewport
            var startTileX = (int)Math.Floor(viewport.X / (double)TileSize);
            var startTileY = (int)Math.Floor(viewport.Y / (double)TileSize);
            var endTileX = (int)Math.Ceiling((viewport.X + viewport.Width) / (double)TileSize);
            var endTileY = (int)Math.Ceiling((viewport.Y + viewport.Height) / (double)TileSize);

            // Validate tile range
            if (startTileX > endTileX || startTileY > endTileY)
            {
                _monitor.Log($"Invalid tile range: X[{startTileX}, {endTileX}], Y[{startTileY}, {endTileY}], skipping overlay rendering.", LogLevel.Trace);
                return null;
            }

            return (startTileX, startTileY, endTileX, endTileY);
        }

        /// <summary>
        /// Renders overlays for a given tile range.
        /// </summary>
        private (int OverlayCount, int SkippedTiles) RenderOverlaysForTileRange(
            SpriteBatch spriteBatch, GameLocation location, (int startX, int startY, int endX, int endY) tileRange, ref int tilesRenderedThisFrame)
        {
            var overlayCount = 0;
            var skippedTiles = 0;

            for (var tileX = tileRange.startX; tileX <= tileRange.endX; tileX++)
            {
                for (var tileY = tileRange.startY; tileY <= tileRange.endY; tileY++)
                {
                    // Performance: Check tile limit per frame
                    if (tilesRenderedThisFrame >= ModConstants.MaxTilesPerFrame)
                    {
                        _monitor.Log($"Reached tile rendering limit ({ModConstants.MaxTilesPerFrame}) for this frame.", LogLevel.Trace);
                        return (overlayCount, skippedTiles);
                    }

                    Vector2 tile = new Vector2(tileX, tileY);

                    // Skip invalid tiles
                    if (!IsValidTile(tile))
                    {
                        skippedTiles++;
                        continue;
                    }

                    // Get soil health for this tile using cache
                    var health = GetCachedHealth(location.NameOrUniqueName, tile,
                        t => _soilHealthService.GetSoilHealth(location.NameOrUniqueName, t));

                    // Skip if no health data (health is 0 or negative)
                    if (health <= 0.0001f)
                    {
                        skippedTiles++;
                        continue;
                    }

                    // Render overlay for this tile
                    RenderTileOverlay(spriteBatch, location, tile, health);
                    overlayCount++;
                    tilesRenderedThisFrame++;
                }
            }

            return (overlayCount, skippedTiles);
        }

        /// <summary>
        /// Logs the rendering result.
        /// </summary>
        private void LogRenderingResult(int overlayCount, int skippedTiles, int tilesRendered)
        {
            if (overlayCount > 0)
            {
                _monitor.Log($"Rendered {overlayCount} tile overlays ({tilesRendered} tiles processed), skipped {skippedTiles} tiles.", LogLevel.Trace);
            }
            else if (skippedTiles > 0)
            {
                _monitor.Log($"No overlays rendered ({tilesRendered} tiles processed), skipped {skippedTiles} tiles.", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Validates tile coordinates.
        /// </summary>
        /// <param name="tile">The tile coordinates to validate</param>
        /// <returns>True if tile coordinates are valid, false otherwise</returns>
        private static bool IsValidTile(Vector2 tile)
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
        /// Checks if a tile is visible in current viewport.
        /// </summary>
        /// <param name="tile">The tile coordinates to check</param>
        /// <returns>True if tile is visible, false otherwise</returns>
        private static bool IsTileVisible(Vector2 tile)
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
        /// Gets current viewport bounds.
        /// </summary>
        /// <returns>The viewport rectangle</returns>
        private static Rectangle GetViewportBounds()
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
        private static Vector2 GetTileScreenPosition(Vector2 tile)
        {
            return new Vector2(tile.X * TileSize, tile.Y * TileSize);
        }

        /// <summary>
        /// Applies opacity to a color.
        /// </summary>
        /// <param name="color">The base color</param>
        /// <param name="opacity">The opacity value (0.0 to 1.0)</param>
        /// <returns>The color with applied opacity</returns>
        private static Color ApplyOpacity(Color color, float opacity)
        {
            // Clamp opacity to valid range
            opacity = Math.Clamp(opacity, 0.0f, 1.0f);

            return new Color(color.R, color.G, color.B, (byte)(color.A * opacity));
        }

        /// <summary>
        /// Renders a colored rectangle at specified position.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="position">The screen position</param>
        /// <param name="color">The color to render</param>
        private static void RenderColoredRectangle(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            // Create a simple texture for rendering
            Texture2D texture = VisualizationHelpers.GetOrCreateOverlayTexture();

            if (texture != null)
            {
                spriteBatch.Draw(
                    texture,
                    new Rectangle((int)position.X, (int)position.Y, TileSize, TileSize),
                    color
                );
            }
        }

        /// <summary>
        /// Clears the health cache if the configured interval has passed.
        /// </summary>
        private void ClearCacheIfNeeded()
        {
            if (DateTime.UtcNow - _lastCacheClear > _cacheClearInterval)
            {
                _healthCache.Clear();
                _lastCacheClear = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets cached health value for a tile, or retrieves and caches it if not present.
        /// </summary>
        /// <param name="locationName">The name of the game location</param>
        /// <param name="tile">The tile coordinates</param>
        /// <param name="getHealth">Function to retrieve health value if not cached</param>
        /// <returns>The cached or retrieved health value</returns>
        private float GetCachedHealth(string locationName, Vector2 tile, Func<Vector2, float> getHealth)
        {
            try
            {
                var cacheKey = (locationName, tile);

                // Check cache first
                if (_healthCache.TryGetValue(cacheKey, out var cachedHealth))
                {
                    return cachedHealth;
                }

                // Retrieve health value
                var health = getHealth(tile);

                // Clear cache if size limit reached
                if (_healthCache.Count >= _cacheSize)
                {
                    _healthCache.Clear();
                }

                // Cache the health value
                _healthCache[cacheKey] = health;
                return health;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error getting cached health for tile {tile}: {ex.Message}", LogLevel.Trace);
                // Return 0 on error to avoid exceptions
                return 0f;
            }
        }
    }
}
