using System;
using System.Collections.Generic;
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
        /// Initializes the service with the white texture used for drawing
        /// rectangles. Must be called before the first draw call, once the
        /// shared <c>VisualizationTextures.WhiteTexture</c> reference exists.
        /// </summary>
        /// <param name="whiteTexture">White 1x1 texture for rectangle draws.</param>
        void Initialize(Texture2D whiteTexture);

        /// <summary>
        /// Sets the tile health data used for overlay rendering.
        /// Called by the game event handler before each render frame.
        /// </summary>
        /// <param name="tileHealthData">Tile coordinates mapped to soil health values.</param>
        void SetTileHealthData(Dictionary<Point, float> tileHealthData);

        /// <summary>
        /// Renders soil health overlays for all visible tilled tiles.
        /// Called after game world rendering completes.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="viewport">Current viewport bounds in tile coordinates</param>
        /// <param name="gameTime">Current game time for animation</param>
        /// <param name="mapBounds">
        /// Optional map bounds in tile units. When supplied, the
        /// visible tile range is clamped to them (FR-018); when
        /// omitted, only the map origin is enforced.
        /// </param>
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
        /// Renders the hoe-feedback floating text on the
        /// screen-space render event (FR-024, DC-3): the
        /// floating text draws above all vanilla content
        /// (the HUD, menus, and cursor), while the flash
        /// stays on the world-space event through
        /// <see cref="RenderHoeFeedback"/>. The screen-space
        /// handler invokes it with the same sprite batch and
        /// frame game time.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for the screen-space draw.</param>
        /// <param name="gameTime">Current frame game time.</param>
        void RenderHoeFeedbackFloatingText(SpriteBatch spriteBatch, GameTime gameTime);

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
        /// Resets the tooltip throttle state so the FR-015
        /// leading-edge rule applies fresh to the next hover.
        /// Called when the last open menu closes (FR-025):
        /// the throttle window must not survive a menu
        /// open/close cycle.
        /// </summary>
        void ResetTooltipThrottle();
    }
}
