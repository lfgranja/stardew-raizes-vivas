using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// The world→screen position derivation every visualization
    /// draw path shares (T183, FR-003, FR-018, DC-5): a tile's
    /// world position (<c>tile * tileSize</c>) localized with
    /// <c>Game1.GlobalToLocal</c> — which subtracts the
    /// pixel-space <c>Game1.viewport</c> origin — and scaled
    /// with <c>Utility.ModifyCoordinatesForUIScale</c>, which
    /// folds in the zoom level (<c>Game1.options.zoomLevel</c>)
    /// and the UI scale (<c>Game1.options.uiScale</c>), so the
    /// flash, the floating text and the tile overlays land on
    /// the targeted tile at any camera offset and any zoom / UI
    /// scale — the same derivation the game's own coordinate
    /// pipeline applies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Headless-host boundary: <c>Game1.options</c> routes
    /// through the game instance (<c>Game1.game1.instanceOptions</c>)
    /// and is unavailable before any game is booted — the state
    /// the unit-test host runs in — where reading it raises
    /// NullReferenceException inside
    /// <c>Utility.ModifyCoordinatesForUIScale</c>, ahead of the
    /// draw-stage guards the render paths carry (the
    /// uninitialized-texture guard, the sprite-batch guard).
    /// When the game state is unavailable the derivation
    /// resolves to its legacy tile-space form: the UI-scale
    /// conversion is the identity at the default zoom (100%)
    /// and default UI scale (100%), so the localized position
    /// IS the derivation's default case, and the camera-relative
    /// tile-space behavior the pre-T183 path pinned is preserved
    /// (the T160 anchor expectations hold at unity zoom / UI
    /// scale). In-game — and under a test-installed options
    /// stand-in — the real game functions run, so the full
    /// derivation holds at every zoom level and UI scale
    /// (the VisualizationZoomAnchorTests GlobalToLocal oracle).
    /// </para>
    /// </remarks>
    internal static class ScreenPositionDerivation
    {
        /// <summary>
        /// Resolves the screen-space position of a world-space
        /// position through the game's own world→screen
        /// derivation, falling back to the legacy tile-space
        /// position when the game state is unavailable (the
        /// headless unit-test host).
        /// </summary>
        /// <param name="worldPosition">The world-space position to localize.</param>
        /// <returns>The screen-space position.</returns>
        public static Vector2 ToScreenPosition(Vector2 worldPosition)
        {
            var localPosition = Game1.GlobalToLocal(worldPosition);
            return ApplyUiScale(localPosition);
        }

        /// <summary>
        /// Applies the game's UI-scale conversion — the zoom
        /// level and the UI scale — when the game state is
        /// available, and the identity (the legacy tile-space
        /// position) when it is not: <c>Game1.options</c>
        /// routes through the game instance, and reading it
        /// before any game is booted raises
        /// NullReferenceException.
        /// </summary>
        /// <param name="localPosition">The localized (viewport-relative) position.</param>
        /// <returns>The UI-scaled screen position.</returns>
        private static Vector2 ApplyUiScale(Vector2 localPosition)
        {
            var game = Game1.game1;
            if (game == null || game.instanceOptions == null)
            {
                return localPosition;
            }

            return Utility.ModifyCoordinatesForUIScale(localPosition);
        }
    }
}
