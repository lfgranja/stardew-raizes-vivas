using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Computes tooltip data for soil health visualization based on cursor position.
    /// Implements cursor-to-tile mapping (FR-002), tooltip formatting per spec,
    /// and update throttling per FR-015: rapid cursor movement suppresses updates,
    /// minimum 50ms between tooltip updates, leading-edge update on new tile entry.
    /// </summary>
    public class TooltipRenderer
    {
        private readonly IVisualizationConfigurationService _configService;
        private readonly IMonitor _monitor;
        private readonly IColorInterpolationService _colorService;

        /// <summary>
        /// Standard Stardew Valley tile size in pixels (1x zoom).
        /// </summary>
        private const int TileSize = 64;

        /// <summary>
        /// Minimum interval between tooltip updates per FR-015.
        /// </summary>
        private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Sentinel tile value indicating no tile has been visited yet.
        /// </summary>
        private static readonly Point UninitializedTile = new Point(int.MinValue, int.MinValue);

        /// <summary>
        /// Tracks the tile for which the tooltip was last updated.
        /// Used to detect new tile entry for leading-edge updates.
        /// </summary>
        private Point _lastTooltipTile = UninitializedTile;

        /// <summary>
        /// Tracks the game time of the last tooltip update.
        /// Used for throttle enforcement per FR-015.
        /// </summary>
        private TimeSpan _lastUpdateTime = TimeSpan.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipRenderer"/> class.
        /// </summary>
        /// <param name="configService">Visualization configuration service.</param>
        /// <param name="monitor">Logger for diagnostic output.</param>
        /// <param name="colorService">Color interpolation service for category/color resolution.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public TooltipRenderer(
            IVisualizationConfigurationService configService,
            IMonitor monitor,
            IColorInterpolationService colorService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        }

        /// <summary>
        /// Gets the tooltip data for the current cursor position, or null if no tooltip should be shown.
        /// Returns null when tooltips are disabled, cursor is outside tile bounds,
        /// no health data exists for the tile, or the update is throttled per FR-015.
        /// </summary>
        /// <param name="cursorPosition">Cursor position in screen coordinates.</param>
        /// <param name="tileHealthData">Dictionary mapping tile coordinates to health values.</param>
        /// <param name="gameTime">Current game time for throttle tracking.</param>
        /// <returns>TooltipData for rendering, or null.</returns>
        public TooltipData? GetTooltip(Vector2 cursorPosition, Dictionary<Point, float> tileHealthData, GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(tileHealthData);
            ArgumentNullException.ThrowIfNull(gameTime);

            if (!_configService.GetConfiguration().TooltipsEnabled)
            {
                return null;
            }

            var tile = CursorToTile(cursorPosition);

            // Reset throttle state when leaving all tiles
            if (!tileHealthData.ContainsKey(tile))
            {
                _lastTooltipTile = UninitializedTile;
                return null;
            }

            // Throttle logic per FR-015
            var elapsed = gameTime.TotalGameTime - _lastUpdateTime;
            var isNewTile = tile != _lastTooltipTile;

            if (!isNewTile && elapsed < MinUpdateInterval)
            {
                // Same tile, within throttle window — suppress update
                return null;
            }

            // Update throttle state (leading-edge on new tile, or 50ms elapsed on same tile)
            _lastTooltipTile = tile;
            _lastUpdateTime = gameTime.TotalGameTime;

            var healthValue = tileHealthData[tile];
            var category = _colorService.GetCategoryForHealth(healthValue);
            var percentage = Math.Clamp(healthValue, 0f, 100f);
            var color = _colorService.GetColorForHealth(healthValue);

            var tooltip = new TooltipData
            {
                Text = $"Soil Health: {percentage:F0}% ({category})",
                Position = cursorPosition,
                BackgroundColor = color,
                TextColor = Color.White
            };

            _monitor.Log($"Tooltip generated for tile ({tile.X}, {tile.Y}): {tooltip.Text}", LogLevel.Trace);

            return tooltip;
        }

        /// <summary>
        /// Determines whether the cursor is positioned over a valid tile.
        /// Returns false for negative coordinates (outside tile bounds).
        /// </summary>
        /// <param name="cursorPosition">Cursor position in screen coordinates.</param>
        /// <returns>True if cursor maps to a non-negative tile coordinate.</returns>
        public bool ShouldShowTooltip(Vector2 cursorPosition)
        {
            var tile = CursorToTile(cursorPosition);
            return tile.X >= 0 && tile.Y >= 0;
        }

        /// <summary>
        /// Converts screen-space cursor coordinates to tile coordinates.
        /// Uses integer truncation (floor for positive values) to map
        /// pixel position to tile index.
        /// </summary>
        /// <param name="cursorPosition">Cursor position in screen coordinates.</param>
        /// <returns>Tile coordinate as a Point.</returns>
        private static Point CursorToTile(Vector2 cursorPosition)
        {
            var tileX = (int)(cursorPosition.X / TileSize);
            var tileY = (int)(cursorPosition.Y / TileSize);
            return new Point(tileX, tileY);
        }
    }
}
