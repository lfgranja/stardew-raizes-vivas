using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
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
        /// Minimum interval between tooltip updates per FR-015,
        /// sourced from the single tuning seam
        /// <see cref="ModConstants.TooltipDebounceMs"/>.
        /// </summary>
        private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromMilliseconds(ModConstants.TooltipDebounceMs);

        /// <summary>
        /// Tracks the game time of the last tooltip update
        /// per tile, per local player (T163, FR-026, DC-6).
        /// The FR-015 throttle window belongs to the tile:
        /// re-entering a previously visited tile inside that
        /// tile's own window is a suppressed redraw on the
        /// same tile, independent of the cursor's path between
        /// visits (FR-002, FR-015). The map is per screen:
        /// one player's hover throttle never suppresses another
        /// player's tooltip updates.
        /// </summary>
        private readonly PerScreen<Dictionary<Point, TimeSpan>> _lastUpdateTimeByTile = new(() => new Dictionary<Point, TimeSpan>());

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

            // FR-015: the throttle window belongs to the tile.
            // A tile that has never been visited is a leading
            // edge and updates immediately; re-entering a
            // previously visited tile inside that tile's own
            // window is a suppressed redraw on the same tile,
            // independent of the cursor's path between visits.
            if (_lastUpdateTimeByTile.Value.TryGetValue(cursorTile, out var lastUpdateTime)
                && gameTime.TotalGameTime - lastUpdateTime < MinUpdateInterval)
            {
                return null;
            }

            _lastUpdateTimeByTile.Value[cursorTile] = gameTime.TotalGameTime;

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

            var tooltip = new TooltipData
            {
                Text = $"Soil Health: {percentage:F0}% ({category})",
                Position = cursorPosition,
                TextColor = ModConstants.TooltipTextColor
            };

            // BackgroundColor deliberately stays the TooltipData
            // default — the FR-002 dark panel (#000000 at alpha
            // 217). The category is already conveyed by the
            // tooltip's own text, and a category-colored panel
            // fails the FR-002 4.5:1 minimum over bright scenes,
            // so no category color may become the panel.

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
            _monitor.Log($"Tooltip unknown for tile ({cursorTile.X}, {cursorTile.Y})", LogLevel.Trace);

            // BackgroundColor deliberately stays the TooltipData
            // default — the FR-002 dark panel (#000000 at alpha
            // 217). The Unknown gray is the overlay color (FR-014),
            // not a tooltip panel: a gray panel fails the FR-002
            // 4.5:1 minimum over bright scenes.
            return new TooltipData
            {
                Text = "Soil Health: Unknown",
                Position = cursorPosition,
                TextColor = ModConstants.TooltipTextColor
            };
        }

        /// <summary>
        /// Computes the screen position at which the tooltip panel
        /// renders for a cursor anchor: cursor-adjacent, offset by
        /// the FR-002 UI-scaled offset so the panel does not occlude
        /// the cursor hotspot or the hovered tile, and clamped within
        /// the viewport so the panel never renders off-screen (FR-002).
        /// The anchor itself (<see cref="TooltipData.Position"/>) stays
        /// the raw cursor position; the offset and the clamp are
        /// applied here, in the draw path, never baked into the anchor.
        /// </summary>
        /// <param name="cursorPosition">Cursor anchor in screen coordinates.</param>
        /// <param name="tooltipSize">Measured tooltip panel size, used by the viewport clamp.</param>
        /// <returns>The clamped screen position for the tooltip panel.</returns>
        public static Vector2 ComputeTooltipPosition(Vector2 cursorPosition, Vector2 tooltipSize)
        {
            var scaledOffset = Utility.ModifyCoordinatesForUIScale(new Vector2(ModConstants.TooltipOffsetXPixels, ModConstants.TooltipOffsetYPixels));
            var position = cursorPosition + scaledOffset;

            var viewport = Game1.viewport;
            var maxX = Math.Max(0f, viewport.Width - tooltipSize.X);
            var maxY = Math.Max(0f, viewport.Height - tooltipSize.Y);

            var clampedX = Math.Clamp(position.X, 0f, maxX);
            var clampedY = Math.Clamp(position.Y, 0f, maxY);

            return new Vector2(clampedX, clampedY);
        }
    }
}
