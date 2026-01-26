using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of tile overlay renderer for soil health visualization.
    /// Renders colored rectangles over tiles based on soil health values with viewport culling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Attribution to DataLayers by Pathoschild:</strong>
    /// </para>
    /// <para>
    /// The tile aggregation, border detection, and batch rendering pipeline algorithms implemented in this class
    /// were adapted from the DataLayers mod by Pathoschild. DataLayers is a sophisticated Stardew Valley mod
    /// that provides comprehensive overlay visualization for various game data layers.
    /// </para>
    /// <para>
    /// <strong>DataLayers Mod Information:</strong>
    /// <list type="bullet">
    /// <item><description>Author: Pathoschild</description></item>
    /// <item><description>GitHub: https://github.com/Pathoschild/StardewMods</description></item>
    /// <item><description>Mod: DataLayers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Adapted Components:</strong>
    /// <list type="bullet">
    /// <item><description>Tile aggregation logic for grouping tiles by category</description></item>
    /// <item><description>Border detection algorithms for identifying tile edges</description></item>
    /// <item><description>Batch rendering pipeline for efficient overlay drawing</description></item>
    /// <item><description>Data structures: TileData, TileDrawData, TileGroup</description></item>
    /// <item><description>TileEdges enum for edge direction flags</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Adaptation for LivingRoots:</strong>
    /// The original DataLayers algorithms have been adapted and modified to meet LivingRoots' specific
    /// soil health visualization needs. This includes grouping tiles by soil health categories,
    /// applying health-based colors and borders, and integrating with LivingRoots' caching and
    /// configuration systems.
    /// </para>
    /// </remarks>
    /// <param name="monitor">Monitor for logging</param>
    /// <param name="config">Visualization configuration</param>
    /// <param name="colorMapper">Color mapper for health values</param>
    /// <param name="soilHealthService">Service for retrieving soil health data</param>
    public class TileOverlayRenderer(
        IMonitor monitor,
        IVisualizationConfig config,
        IColorMapper colorMapper,
        ISoilHealthService soilHealthService
    ) : ITileOverlayRenderer
    {
        // Dependencies
        private readonly IMonitor _monitor =
            monitor ?? throw new ArgumentNullException(nameof(monitor));
        private readonly IVisualizationConfig _config =
            config ?? throw new ArgumentNullException(nameof(config));
        private readonly IColorMapper _colorMapper =
            colorMapper ?? throw new ArgumentNullException(nameof(colorMapper));
        private readonly ISoilHealthService _soilHealthService =
            soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));

        // Rendering constants
        private const int TileSize = 64; // Stardew Valley tile size in pixels

        // Thread safety locks
        private readonly object _cacheLock = new();
        private readonly object _aggregationLock = new();

        // Performance: LRU Health cache
        private readonly LruCache<(string Location, Point Tile), float> _healthCache = new(
            ModConstants.TileHealthCacheSize
        );

        // Aggregated tile data for batch rendering (from DataLayers)
        private readonly Dictionary<Vector2, TileDrawData> _aggregatedTiles = [];
        private readonly List<TileGroup> _tileGroups = [];

        /// <inheritdoc/>
        public void RenderTileOverlay(
            SpriteBatch spriteBatch,
            GameLocation location,
            Vector2 tile,
            float health
        )
        {
            try
            {
                // Validate parameters
                if (
                    spriteBatch == null
                    || location == null
                    || !VisualizationHelpers.IsValidTile(tile)
                    || !_config.ShowTileOverlays
                )
                {
                    return;
                }

                // Check for missing or invalid soil health data
                // Allow health=0 to be rendered (distinguishes "set to 0" from "not set")
                if (health < 0f)
                {
                    return;
                }

                // Get color for health value
                Color baseColor = _colorMapper.GetHealthColor(health);
                Color overlayColor = VisualizationHelpers.ApplyOpacity(
                    baseColor,
                    _config.OverlayOpacity
                );

                // Calculate world position (for world-space rendering in OnRenderedWorld event)
                Vector2 worldPosition = GetTileWorldPosition(tile);

                // Render overlay at world position
                RenderColoredRectangle(spriteBatch, worldPosition, overlayColor);
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Error rendering tile overlay for tile {tile}: {ex.Message}",
                    LogLevel.Error
                );
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
                    return;
                }

                // Get viewport bounds and tile range
                var tileRange = GetViewportTileRange();
                if (tileRange == null)
                {
                    return;
                }

                // Aggregate tile data for batch rendering (from DataLayers)
                AggregateTileDataForBatchRendering(location, tileRange.Value);

                // Draw aggregated overlays
                DrawAggregatedTiles(spriteBatch);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering all visible overlays: {ex.Message}", LogLevel.Error);
            }
        }

        private void DrawAggregatedTiles(SpriteBatch spriteBatch)
        {
            Texture2D? texture = VisualizationHelpers.GetOrCreateOverlayTexture();
            if (texture == null)
            {
                _monitor.Log("Texture is null, cannot draw overlay!", LogLevel.Error);
                return;
            }

            lock (_aggregationLock)
            {
                foreach (TileDrawData data in _aggregatedTiles.Values)
                {
                    // draw fill(s)
                    foreach (Color c in data.Colors)
                    {
                        // Calculate world position with camera offset for proper rendering in OnRenderedWorld event
                        Vector2 worldPosition = GetTileWorldPosition(data.TilePosition);
                        Vector2 screenPosition = new Vector2(
                            worldPosition.X - Game1.viewport.X,
                            worldPosition.Y - Game1.viewport.Y
                        );

                        // Draw overlay without layerDepth parameter to use XNA default depth
                        // This matches DataLayers mod approach which renders overlays correctly between ground and characters
                        spriteBatch.Draw(
                            texture,
                            new Rectangle(
                                (int)screenPosition.X,
                                (int)screenPosition.Y,
                                TileSize,
                                TileSize
                            ),
                            c // color
                        );
                    }

                    // Draw borders
                    foreach (var kvp in data.BorderColors)
                    {
                        Color borderColor = kvp.Key;
                        TileEdges edges = kvp.Value;

                        // Calculate world position
                        Vector2 worldPosition = GetTileWorldPosition(data.TilePosition);
                        Vector2 screenPosition = new Vector2(
                            worldPosition.X - Game1.viewport.X,
                            worldPosition.Y - Game1.viewport.Y
                        );

                        // Draw borders for each edge
                        int borderSize = 2; // Thickness of the border

                        if (edges.HasFlag(TileEdges.Left))
                            DrawBorder(
                                spriteBatch,
                                screenPosition,
                                TileEdges.Left,
                                borderColor,
                                borderSize
                            );

                        if (edges.HasFlag(TileEdges.Right))
                            DrawBorder(
                                spriteBatch,
                                screenPosition,
                                TileEdges.Right,
                                borderColor,
                                borderSize
                            );

                        if (edges.HasFlag(TileEdges.Top))
                            DrawBorder(
                                spriteBatch,
                                screenPosition,
                                TileEdges.Top,
                                borderColor,
                                borderSize
                            );

                        if (edges.HasFlag(TileEdges.Bottom))
                            DrawBorder(
                                spriteBatch,
                                screenPosition,
                                TileEdges.Bottom,
                                borderColor,
                                borderSize
                            );
                    }
                }
            }
        }

        /// <summary>
        /// Draws a border on a specific edge of a tile.
        /// </summary>
        private void DrawBorder(
            SpriteBatch spriteBatch,
            Vector2 position,
            TileEdges edge,
            Color color,
            int width
        )
        {
            Texture2D? texture = VisualizationHelpers.GetOrCreateOverlayTexture();
            if (texture == null)
                return;

            Rectangle borderRect;

            switch (edge)
            {
                case TileEdges.Left:
                    borderRect = new Rectangle((int)position.X, (int)position.Y, width, TileSize);
                    break;
                case TileEdges.Right:
                    borderRect = new Rectangle(
                        (int)(position.X + TileSize - width),
                        (int)position.Y,
                        width,
                        TileSize
                    );
                    break;
                case TileEdges.Top:
                    borderRect = new Rectangle((int)position.X, (int)position.Y, TileSize, width);
                    break;
                case TileEdges.Bottom:
                    borderRect = new Rectangle(
                        (int)position.X,
                        (int)(position.Y + TileSize - width),
                        TileSize,
                        width
                    );
                    break;
                default:
                    return;
            }

            spriteBatch.Draw(texture, borderRect, color);
        }

        /// <summary>
        /// Gets border color for a health category.
        /// </summary>
        /// <param name="category">The health category</param>
        /// <returns>The border color for category</returns>
        private static Color GetCategoryBorderColor(SoilHealthCategory category)
        {
            return category switch
            {
                SoilHealthCategory.Poor => new Color(139, 0, 0, 255), // Dark red for poor soil
                SoilHealthCategory.Moderate => new Color(139, 139, 0, 255), // Dark yellow for moderate soil
                SoilHealthCategory.Healthy => new Color(0, 100, 0, 255), // Dark green for healthy soil
                _ => Color.Gray,
            };
        }

        /// <summary>
        /// Aggregates tile data for batch rendering using DataLayers' tile aggregation logic.
        /// This groups tiles by health category for efficient rendering and detects borders.
        /// </summary>
        /// <remarks>
        /// <strong>Adapted from DataLayers:</strong>
        /// This method implements the tile aggregation algorithm from DataLayers by Pathoschild.
        /// The original algorithm groups tiles by data category for efficient batch rendering.
        /// Adapted for LivingRoots to group tiles by soil health categories (Poor, Moderate, Healthy).
        /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
        /// </remarks>
        private void AggregateTileDataForBatchRendering(
            GameLocation location,
            (int startX, int startY, int endX, int endY) tileRange
        )
        {
            lock (_aggregationLock)
            {
                // Clear previous aggregation
                _tileGroups.Clear();
                _aggregatedTiles.Clear();

                // Group tiles by health category
                var tilesByCategory = GroupTilesByCategory(location, tileRange);

                // Create tile groups for batch rendering
                CreateTileGroups(tilesByCategory);

                // Aggregate tile data for rendering
                AggregateTileDataWithBorders();

                // Detect combined borders
                DetectCombinedBorders();
            }
        }

        /// <summary>
        /// Groups tiles by health category for efficient batch rendering.
        /// </summary>
        private Dictionary<SoilHealthCategory, List<Services.TileData>> GroupTilesByCategory(
            GameLocation location,
            (int startX, int startY, int endX, int endY) tileRange
        )
        {
            var tilesByCategory = new Dictionary<SoilHealthCategory, List<TileData>>();

            for (var tileX = tileRange.startX; tileX <= tileRange.endX; tileX++)
            {
                for (var tileY = tileRange.startY; tileY <= tileRange.endY; tileY++)
                {
                    var tile = new Vector2(tileX, tileY);

                    if (!TryProcessTile(location, tile, out var tileData))
                    {
                        continue;
                    }

                    // tileData is guaranteed to be non-null here since TryProcessTile returned true
                    var category = tileData!.Category;
                    AddTileToCategory(tilesByCategory, category, tileData!);
                }
            }

            return tilesByCategory;
        }

        /// <summary>
        /// Tries to process a single tile and returns tile data if successful.
        /// </summary>
        private bool TryProcessTile(
            GameLocation location,
            Vector2 tile,
            out Services.TileData? tileData
        )
        {
            tileData = null;

            // Skip invalid tiles
            if (!VisualizationHelpers.IsValidTile(tile))
            {
                return false;
            }

            // Check if tile has HoeDirt (tilled soil)
            if (
                location.terrainFeatures == null
                || !location.terrainFeatures.TryGetValue(tile, out var feature)
                || feature is not HoeDirt
            )
            {
                return false;
            }

            // Get soil health for this tile using cache
            var health = GetCachedHealth(
                location.NameOrUniqueName,
                tile,
                t => _soilHealthService.GetSoilHealth(location.NameOrUniqueName, t)
            );

            // Skip if no health data (health is negative)
            if (health < 0f)
            {
                return false;
            }

            // Categorize from health (not from color)
            SoilHealthCategory category;
            if (health < 40f)
            {
                category = SoilHealthCategory.Poor;
            }
            else if (health < 70f)
            {
                category = SoilHealthCategory.Moderate;
            }
            else
            {
                category = SoilHealthCategory.Healthy;
            }

            // Get color for health value
            Color baseColor = _colorMapper.GetHealthColor(health);
            Color overlayColor = VisualizationHelpers.ApplyOpacity(
                baseColor,
                _config.OverlayOpacity
            );

            // Create tile data structure
            tileData = new TileData(tile, overlayColor, category);
            return true;
        }

        /// <summary>
        /// Adds tile to the appropriate category list.
        /// </summary>
        private static void AddTileToCategory(
            Dictionary<SoilHealthCategory, List<Services.TileData>> tilesByCategory,
            SoilHealthCategory category,
            Services.TileData tileData
        )
        {
            if (!tilesByCategory.ContainsKey(category))
            {
                tilesByCategory[category] = [];
            }
            tilesByCategory[category].Add(tileData);
        }

        /// <summary>
        /// Creates tile groups from categorized tiles.
        /// </summary>
        private void CreateTileGroups(
            Dictionary<SoilHealthCategory, List<Services.TileData>> tilesByCategory
        )
        {
            foreach (var kvp in tilesByCategory)
            {
                var category = kvp.Key;
                var tiles = kvp.Value;

                // Create a tile group with category-specific border color
                var borderColor = GetCategoryBorderColor(category);

                var tileGroup = new TileGroup(tiles, borderColor, shouldExport: false);
                _tileGroups.Add(tileGroup);
            }
        }

        /// <summary>
        /// Aggregates tile data with border detection.
        /// </summary>
        private void AggregateTileDataWithBorders()
        {
            foreach (TileGroup group in _tileGroups)
            {
                var inGroupLazy = new Lazy<HashSet<Vector2>>(
                    () => [.. group.Tiles.Select(p => p.TilePosition)],
                    LazyThreadSafetyMode.ExecutionAndPublication
                );

                foreach (Services.TileData groupTile in group.Tiles)
                {
                    ProcessTileForAggregation(groupTile, group, inGroupLazy);
                }
            }
        }

        /// <summary>
        /// Processes a single tile for aggregation.
        /// </summary>
        private void ProcessTileForAggregation(
            Services.TileData groupTile,
            TileGroup group,
            Lazy<HashSet<Vector2>> inGroupLazy
        )
        {
            Vector2 position = groupTile.TilePosition;
            if (!_aggregatedTiles.TryGetValue(position, out TileDrawData? data))
                data = _aggregatedTiles[position] = new TileDrawData(position, Point.Zero);

            // Update data
            data.Colors.Add(groupTile.Color);
            if (group.OuterBorderColor.HasValue)
                data.BorderColors.TryAdd(group.OuterBorderColor.Value, TileEdges.None);

            // Detect borders
            if (group.OuterBorderColor.HasValue)
            {
                DetectTileBorders(groupTile, group.OuterBorderColor.Value, inGroupLazy, data);
            }
        }

        /// <summary>
        /// Detects borders for a tile within a group.
        /// </summary>
        private static void DetectTileBorders(
            Services.TileData groupTile,
            Color borderColor,
            Lazy<HashSet<Vector2>> inGroupLazy,
            TileDrawData data
        )
        {
            var x = (int)groupTile.TilePosition.X;
            var y = (int)groupTile.TilePosition.Y;
            HashSet<Vector2> inGroup = inGroupLazy.Value;

            TileEdges edge = data.BorderColors[borderColor];

            if (!inGroup.Contains(new Vector2(x - 1, y)))
                edge |= TileEdges.Left;
            if (!inGroup.Contains(new Vector2(x + 1, y)))
                edge |= TileEdges.Right;
            if (!inGroup.Contains(new Vector2(x, y - 1)))
                edge |= TileEdges.Top;
            if (!inGroup.Contains(new Vector2(x, y + 1)))
                edge |= TileEdges.Bottom;

            data.BorderColors[borderColor] = edge;
        }

        /// <summary>
        /// Detects combined borders by comparing adjacent tiles.
        /// </summary>
        private void DetectCombinedBorders()
        {
            foreach (Vector2 position in _aggregatedTiles.Keys.ToList())
            {
                var x = (int)position.X;
                var y = (int)position.Y;
                TileDrawData data = _aggregatedTiles[position];

                if (!data.BorderColors.Any())
                    continue;

                var neighbors = GetTileNeighbors(x, y);
                UpdateTileBordersWithNeighbors(data, neighbors);
            }
        }

        /// <summary>
        /// Gets neighboring tiles for a position.
        /// </summary>
        private (
            TileDrawData? left,
            TileDrawData? right,
            TileDrawData? top,
            TileDrawData? bottom
        ) GetTileNeighbors(int x, int y)
        {
            _aggregatedTiles.TryGetValue(new Vector2(x - 1, y), out var left);
            _aggregatedTiles.TryGetValue(new Vector2(x + 1, y), out var right);
            _aggregatedTiles.TryGetValue(new Vector2(x, y - 1), out var top);
            _aggregatedTiles.TryGetValue(new Vector2(x, y + 1), out var bottom);

            return (left, right, top, bottom);
        }

        /// <summary>
        /// Updates tile borders based on neighbor data.
        /// </summary>
        private static void UpdateTileBordersWithNeighbors(
            TileDrawData data,
            (
                TileDrawData? left,
                TileDrawData? right,
                TileDrawData? top,
                TileDrawData? bottom
            ) neighbors
        )
        {
            foreach (Color color in data.BorderColors.Keys.ToArray())
            {
                if (neighbors.left == null || !neighbors.left.BorderColors.ContainsKey(color))
                    data.BorderColors[color] |= TileEdges.Left;
                if (neighbors.right == null || !neighbors.right.BorderColors.ContainsKey(color))
                    data.BorderColors[color] |= TileEdges.Right;
                if (neighbors.top == null || !neighbors.top.BorderColors.ContainsKey(color))
                    data.BorderColors[color] |= TileEdges.Top;
                if (neighbors.bottom == null || !neighbors.bottom.BorderColors.ContainsKey(color))
                    data.BorderColors[color] |= TileEdges.Bottom;
            }
        }

        /// <summary>
        /// Validates rendering parameters.
        /// </summary>
        private static bool ValidateRenderingParameters(
            SpriteBatch spriteBatch,
            GameLocation location
        )
        {
            if (spriteBatch == null)
            {
                return false;
            }

            if (location == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets tile range for current viewport.
        /// </summary>
        private static (int startX, int startY, int endX, int endY)? GetViewportTileRange()
        {
            Rectangle viewport = VisualizationHelpers.GetViewportBounds();

            // Calculate tile range for viewport
            var startTileX = (int)Math.Floor(viewport.X / (double)TileSize);
            var startTileY = (int)Math.Floor(viewport.Y / (double)TileSize);
            var endTileX = (int)Math.Ceiling((viewport.X + viewport.Width) / (double)TileSize);
            var endTileY = (int)Math.Ceiling((viewport.Y + viewport.Height) / (double)TileSize);

            // Validate tile range
            if (startTileX > endTileX || startTileY > endTileY)
            {
                return null;
            }

            return (startTileX, startTileY, endTileX, endTileY);
        }

        /// <summary>
        /// Calculates world position for a tile (for world-space rendering).
        /// </summary>
        /// <param name="tile">The tile coordinates</param>
        /// <returns>The world position in pixels</returns>
        private static Vector2 GetTileWorldPosition(Vector2 tile)
        {
            // Convert tile to world coordinates (no viewport offset)
            return new Vector2(tile.X * TileSize, tile.Y * TileSize);
        }

        /// <summary>
        /// Renders a colored rectangle at specified position.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="position">The screen position</param>
        /// <param name="color">The color to render</param>
        private void RenderColoredRectangle(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            // Create a simple texture for rendering
            Texture2D? texture = VisualizationHelpers.GetOrCreateOverlayTexture();

            if (texture != null)
            {
                // Adjust position for camera offset when rendering in world space
                Vector2 screenPosition = new Vector2(
                    position.X - Game1.viewport.X,
                    position.Y - Game1.viewport.Y
                );

                // Draw overlay without layerDepth parameter to use XNA default depth
                // This matches DataLayers mod approach which renders overlays correctly between ground and characters
                spriteBatch.Draw(
                    texture,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, TileSize, TileSize),
                    color // color
                );
            }
            else
            {
                _monitor.Log("Texture is null, cannot draw overlay!", LogLevel.Error);
            }
        }

        /// <summary>
        /// Gets cached health value for a tile, or retrieves and caches it if not present.
        /// Uses LRU cache to prevent sudden cache flushes.
        /// </summary>
        /// <param name="locationName">The name of game location</param>
        /// <param name="tile">The tile coordinates</param>
        /// <param name="getHealth">Function to retrieve health value if not cached</param>
        /// <returns>The cached or retrieved health value</returns>
        private float GetCachedHealth(
            string locationName,
            Vector2 tile,
            Func<Vector2, float> getHealth
        )
        {
            try
            {
                var tileKey = new Point((int)tile.X, (int)tile.Y);
                var cacheKey = (locationName, tileKey);

                // Check cache first (thread-safe)
                lock (_cacheLock)
                {
                    if (_healthCache.TryGetValue(cacheKey, out var cachedHealth))
                    {
                        return cachedHealth;
                    }
                }

                // Retrieve health value
                var health = getHealth(tile);

                // Cache health value (thread-safe)
                lock (_cacheLock)
                {
                    _healthCache.Put(cacheKey, health);
                }

                return health;
            }
            catch (Exception)
            {
                // Return negative sentinel on error so callers can skip rendering
                return -1f;
            }
        }
    }

    #region DataLayers Structures (Adapted for LivingRoots)
    /// <remarks>
    /// <strong>Attribution to DataLayers:</strong>
    /// The following data structures (TileEdges, TileData, TileDrawData, TileGroup) were adapted
    /// from the DataLayers mod by Pathoschild. These structures form the foundation of the
    /// tile aggregation and batch rendering pipeline.
    /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
    /// </remarks>
    /// <summary>
    /// Soil health category for grouping tiles with similar health values.
    /// </summary>
    internal enum SoilHealthCategory
    {
        /// <summary>Poor soil health (health < 40).</summary>
        Poor = 0,

        /// <summary>Moderate soil health (health 40-69).</summary>
        Moderate = 1,

        /// <summary>Healthy soil (health >= 70).</summary>
        Healthy = 2,
    }

    /// <summary>
    /// A tile edge direction (adapted from DataLayers).
    /// </summary>
    /// <remarks>
    /// <strong>Adapted from DataLayers:</strong>
    /// This enum defines tile edge directions used for border detection.
    /// DataLayers uses these flags to efficiently track which tile edges need borders.
    /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
    /// </remarks>
    [Flags]
    internal enum TileEdges
    {
        /// <summary>No edge.</summary>
        None = 0,

        /// <summary>The top tile edge.</summary>
        Top = 1,

        /// <summary>The left tile edge.</summary>
        Left = 2,

        /// <summary>The right tile edge.</summary>
        Right = 4,

        /// <summary>The bottom tile edge.</summary>
        Bottom = 8,
    }

    /// <summary>
    /// Metadata for a tile (adapted from DataLayers).
    /// Adapted for LivingRoots without LegendEntry dependency.
    /// </summary>
    /// <remarks>
    /// <strong>Adapted from DataLayers:</strong>
    /// This class stores tile position and overlay color metadata.
    /// DataLayers uses TileData as the fundamental unit for tile aggregation.
    /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
    /// </remarks>
    /// <remarks>Construct an instance.</remarks>
    /// <param name="tile">The tile position.</param>
    /// <param name="color">The overlay color.</param>
    /// <param name="category">The soil health category (optional, for LivingRoots).</param>
    internal class TileData(
        Vector2 tile,
        Color color,
        SoilHealthCategory category = SoilHealthCategory.Healthy
    )
    {
        /// <summary>The tile position.</summary>
        public Vector2 TilePosition { get; } = tile;

        /// <summary>The overlay color.</summary>
        public Color Color { get; } = color;

        /// <summary>The pixel offset at which to draw this tile.</summary>
        public Point DrawOffset { get; } = Point.Zero;

        /// <summary>The soil health category (LivingRoots-specific).</summary>
        public SoilHealthCategory Category { get; } = category;
    }

    /// <summary>
    /// Aggregate drawing metadata for a tile (adapted from DataLayers).
    /// </summary>
    /// <remarks>
    /// <strong>Adapted from DataLayers:</strong>
    /// This class aggregates drawing metadata for efficient batch rendering.
    /// DataLayers uses TileDrawData to store multiple colors and border information per tile.
    /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
    /// </remarks>
    /// <remarks>Construct an instance.</remarks>
    /// <param name="position">The tile position.</param>
    /// <param name="drawOffset">The pixel offset at which to draw this tile.</param>
    internal class TileDrawData(Vector2 position, Point drawOffset)
    {
        /// <summary>The tile position.</summary>
        public Vector2 TilePosition { get; } = position;

        /// <summary>The overlay colors to draw.</summary>
        public HashSet<Color> Colors { get; } = [];

        /// <summary>The border colors to draw.</summary>
        public Dictionary<Color, TileEdges> BorderColors { get; } = [];

        /// <summary>The pixel offset at which to draw this tile.</summary>
        public Point DrawOffset { get; } = drawOffset;
    }

    /// <summary>
    /// A group of tiles (adapted from DataLayers).
    /// </summary>
    /// <remarks>
    /// <strong>Adapted from DataLayers:</strong>
    /// This class groups tiles with shared properties for efficient batch rendering.
    /// DataLayers uses TileGroup to organize tiles by data layer and apply shared borders.
    /// Reference: https://github.com/Pathoschild/StardewMods/tree/develop/DataLayers/Framework
    /// </remarks>
    /// <remarks>Construct an instance.</remarks>
    /// <param name="tiles">The tiles in the group.</param>
    /// <param name="outerBorderColor">A border color to draw along edges that aren't touching another tile in the group (if any).</param>
    /// <param name="shouldExport">Whether to include this tile group in data exports.</param>
    internal class TileGroup(
        IEnumerable<TileData> tiles,
        Color? outerBorderColor = null,
        bool shouldExport = true
    )
    {
        /// <summary>The tiles in the group.</summary>
        public TileData[] Tiles { get; } = tiles.ToArray();

        /// <summary>A border color to draw along edges that aren't touching another tile in the group (if any).</summary>
        public Color? OuterBorderColor { get; } = outerBorderColor;

        /// <summary>Whether to include this tile group in data exports.</summary>
        public bool ShouldExport { get; } = shouldExport;
    }

    #endregion

    #region LRU Cache Implementation

    /// <summary>
    /// A simple Least Recently Used (LRU) cache implementation.
    /// Prevents sudden cache flushes by evicting least recently used items when capacity is reached.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache</typeparam>
    /// <typeparam name="TValue">The type of values in the cache</typeparam>
    internal class LruCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<LruCacheItem>> _cacheMap;
        private readonly LinkedList<LruCacheItem> _lruList;

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        public int Count => _cacheMap.Count;

        /// <summary>
        /// Initializes a new instance of the LRUCache class.
        /// </summary>
        /// <param name="capacity">The maximum number of items to store in the cache</param>
        public LruCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

            _capacity = capacity;
            _cacheMap = [];
            _lruList = new LinkedList<LruCacheItem>();
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Adds or updates a key-value pair in the cache.
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        public void Put(TKey key, TValue value)
        {
            if (_cacheMap.TryGetValue(key, out var existingNode))
            {
                // Update existing node
                existingNode.Value.Value = value;
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
            }
            else
            {
                // Add new node
                if (_cacheMap.Count >= _capacity)
                {
                    // Remove least recently used item
                    var lruNode = _lruList.Last;
                    if (lruNode != null)
                    {
                        _cacheMap.Remove(lruNode.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                var newNode = new LinkedListNode<LruCacheItem>(new LruCacheItem(key, value));
                _lruList.AddFirst(newNode);
                _cacheMap[key] = newNode;
            }
        }

        /// <summary>
        /// Removes the value with the specified key from the cache.
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false</returns>
        public bool Remove(TKey key)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _cacheMap.Remove(key);
                _lruList.Remove(node);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all keys and values from the cache.
        /// </summary>
        public void Clear()
        {
            _cacheMap.Clear();
            _lruList.Clear();
        }

        /// <summary>
        /// Gets all keys in the cache.
        /// </summary>
        /// <returns>An enumerable of all keys in the cache</returns>
        public IEnumerable<TKey> Keys => _cacheMap.Keys;

        /// <summary>
        /// Internal class representing a cache item.
        /// </summary>
        private sealed class LruCacheItem(TKey key, TValue value)
        {
            public TKey Key { get; } = key;
            public TValue Value { get; set; } = value;
        }
    }

    #endregion
}
