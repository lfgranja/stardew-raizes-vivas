using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

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
        /// Null until the first update, so the first hover is never throttled
        /// and never subtracts from <see cref="TimeSpan.MinValue"/>.
        /// </summary>
        private TimeSpan? _lastUpdateTime;

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
        /// <param name="cursorTile">Tile under cursor from SMAPI cursor-to-tile API (GrabTile).</param>
        /// <param name="tileHealthData">Dictionary mapping tile coordinates to health values.</param>
        /// <param name="gameTime">Current game time for throttle tracking.</param>
        /// <returns>TooltipData for rendering, or null.</returns>
        public TooltipData? GetTooltip(Vector2 cursorPosition, Point cursorTile, Dictionary<Point, float> tileHealthData, GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(tileHealthData);
            ArgumentNullException.ThrowIfNull(gameTime);

            if (!_configService.GetConfiguration().TooltipsEnabled)
            {
                return null;
            }

            if (cursorTile.X < 0 || cursorTile.Y < 0)
            {
                return null;
            }

            var isNewTile = cursorTile != _lastTooltipTile;

            // TimeSpan subtraction is checked in .NET 6, so the first call must not
            // compute a delta against a sentinel. Only an existing timestamp can throttle.
            if (!isNewTile && _lastUpdateTime.HasValue)
            {
                var elapsed = gameTime.TotalGameTime - _lastUpdateTime.Value;
                if (elapsed < MinUpdateInterval)
                {
                    return null;
                }
            }

            _lastTooltipTile = cursorTile;
            _lastUpdateTime = gameTime.TotalGameTime;

            float healthValue = tileHealthData.ContainsKey(cursorTile) ? tileHealthData[cursorTile] : float.NaN;

            if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
            {
                return CreateUnknownTooltip(cursorPosition, cursorTile);
            }

            var category = _colorService.GetCategoryForHealth(healthValue);
            if (category == HealthCategory.Unknown)
            {
                return CreateUnknownTooltip(cursorPosition, cursorTile);
            }

            var percentage = Math.Clamp(healthValue, 0f, 100f);
            var color = _colorService.GetColorForHealth(healthValue);

            var tooltip = new TooltipData
            {
                Text = $"Soil Health: {percentage:F0}% ({category})",
                Position = cursorPosition,
                BackgroundColor = color,
                TextColor = Color.White
            };

            _monitor.Log($"Tooltip generated for tile ({cursorTile.X}, {cursorTile.Y}): {tooltip.Text}", LogLevel.Trace);

            return tooltip;
        }

        /// <summary>
        /// Builds the "Soil Health: Unknown" tooltip used when no health data is
        /// available for the hovered tile (FR-014).
        /// </summary>
        /// <param name="cursorPosition">Tooltip anchor in screen coordinates.</param>
        /// <param name="cursorTile">Tile under cursor, used for the trace log.</param>
        /// <returns>TooltipData rendering the unknown state without a percentage.</returns>
        private TooltipData CreateUnknownTooltip(Vector2 cursorPosition, Point cursorTile)
        {
            var unknownColor = new Color(
                ModConstants.UnknownColor.R,
                ModConstants.UnknownColor.G,
                ModConstants.UnknownColor.B);

            _monitor.Log($"Tooltip unknown for tile ({cursorTile.X}, {cursorTile.Y})", LogLevel.Trace);

            return new TooltipData
            {
                Text = "Soil Health: Unknown",
                Position = cursorPosition,
                BackgroundColor = unknownColor,
                TextColor = Color.White
            };
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
            var scaled = Utility.ModifyCoordinatesForUIScale(cursorPosition);
            var tileX = (int)(scaled.X / Game1.tileSize);
            var tileY = (int)(scaled.Y / Game1.tileSize);
            return new Point(tileX, tileY);
        }
    }
}
