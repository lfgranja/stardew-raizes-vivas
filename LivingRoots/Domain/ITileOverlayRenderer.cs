using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for rendering tile overlays based on soil health.
    /// Provides methods to render colored rectangles over tiles with proper opacity and viewport culling.
    /// </summary>
    public interface ITileOverlayRenderer
    {
        /// <summary>
        /// Renders a colored overlay over a single tile based on soil health.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="location">The game location containing the tile</param>
        /// <param name="tile">The tile coordinates to render over</param>
        /// <param name="health">The soil health value (0-100) for color mapping</param>
        void RenderTileOverlay(SpriteBatch spriteBatch, GameLocation location, Vector2 tile, float health);

        /// <summary>
        /// Renders overlays for all visible tiles in the current viewport.
        /// Applies viewport culling for performance optimization.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="location">The game location containing the tiles</param>
        void RenderAllVisibleOverlays(SpriteBatch spriteBatch, GameLocation location);
    }
}
