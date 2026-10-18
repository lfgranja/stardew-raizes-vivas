using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Orchestrates rendering of soil health visualization overlays, tooltips, and hoe feedback.
    /// Called from game loop render events.
    /// </summary>
    public interface IVisualizationService
    {
        /// <summary>
        /// Renders soil health overlays for all visible tilled tiles.
        /// Called after game world rendering completes.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="viewport">Current viewport bounds in tile coordinates</param>
        /// <param name="gameTime">Current game time for animation</param>
        /// <param name="mapBounds">Active map extent in tile units, or null when unavailable; clamps culling to the map.</param>
        void RenderOverlays(SpriteBatch spriteBatch, Rectangle viewport, GameTime gameTime, Rectangle? mapBounds = null);

        /// <summary>
        /// Renders hover tooltip if cursor is over a tilled soil tile.
        /// Called after overlay rendering.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="cursorPosition">Current cursor position in screen coordinates</param>
        /// <param name="gameTime">Current game time</param>
        void RenderTooltip(SpriteBatch spriteBatch, Vector2 cursorPosition, GameTime gameTime);

        /// <summary>
        /// Renders hoe action feedback effects.
        /// Called after tooltip rendering.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="gameTime">Current game time for animation</param>
        void RenderHoeFeedback(SpriteBatch spriteBatch, GameTime gameTime);

        /// <summary>
        /// Triggers hoe feedback for a specific tile.
        /// Called when hoe action is detected.
        /// </summary>
        /// <param name="tilePosition">Tile coordinates targeted by hoe</param>
        /// <param name="healthValue">Current health value of the tile</param>
        void TriggerHoeFeedback(Point tilePosition, float healthValue);

        /// <summary>
        /// Invalidates cached overlay data, forcing recalculation on next render.
        /// Called when soil health data changes.
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// Pauses rendering to prevent state corruption during save operations.
        /// Called before game save begins.
        /// </summary>
        void PauseRendering();

        /// <summary>
        /// Resumes rendering after save/load completes.
        /// Called after game load finishes.
        /// </summary>
        void ResumeRendering();

        /// <summary>
        /// Updates the cursor tile position for tooltip tracking.
        /// Called on input events.
        /// </summary>
        /// <param name="tilePosition">Current cursor tile position.</param>
        void UpdateCursorTile(Point tilePosition);

        /// <summary>
        /// Clears the tracked cursor tile so the next hover is treated as a new tile.
        /// Called when the cursor leaves the world or a location change occurs.
        /// </summary>
        void ClearCursorTile();

        /// <summary>
        /// Installs the shared white texture the draw calls run through. Called
        /// from the render frame once a SpriteBatch exists.
        /// </summary>
        /// <param name="whiteTexture">1x1 white texture used for rectangle drawing.</param>
        void Initialize(Texture2D whiteTexture);

        /// <summary>
        /// Supplies the soil-health snapshot the overlay pipeline renders from.
        /// </summary>
        /// <param name="tileHealthData">Tile position to health value map.</param>
        void SetTileHealthData(Dictionary<Point, float> tileHealthData);

        /// <summary>
        /// Resets the FR-015 tooltip throttle window, so the leading-edge rule
        /// applies fresh after a menu open/close cycle.
        /// </summary>
        void ResetTooltipThrottle();

        /// <summary>
        /// Renders the screen-space half of hoe feedback: the floating text that
        /// must draw above all vanilla content. The flash half stays world-space
        /// on <see cref="RenderHoeFeedback"/>.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing.</param>
        /// <param name="gameTime">Current game time for animation.</param>
        void RenderHoeFeedbackFloatingText(SpriteBatch spriteBatch, GameTime gameTime);

        /// <summary>
        /// Spec 005 (A4): emits a floating-text message when a soil-health tier
        /// modifies the harvest outcome (quality boost or extra harvest). Reuses
        /// the existing floating-text render path — no new assets or i18n keys.
        /// </summary>
        /// <param name="tilePosition">Tile where the harvest occurred.</param>
        /// <param name="message">Feedback message to display.</param>
        void TriggerHarvestFeedback(Microsoft.Xna.Framework.Point tilePosition, string message);
    }
}
