using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for rendering tooltips and hoe action feedback.
    /// Provides methods to display hover tooltips and visual feedback for hoe actions.
    /// </summary>
    public interface ITooltipRenderer
    {
        /// <summary>
        /// Renders a hover tooltip showing soil health information near the cursor.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="cursorPosition">The current cursor position in screen coordinates</param>
        /// <param name="health">The soil health value (0-100) to display</param>
        void RenderHoverTooltip(SpriteBatch spriteBatch, Vector2 cursorPosition, float health);

        /// <summary>
        /// Renders visual feedback when a hoe is used on a tile.
        /// Displays a visual flash or floating text at the tile position.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="tilePosition">The tile position in screen coordinates</param>
        /// <param name="health">The soil health value (0-100) for color mapping</param>
        void RenderHoeFeedback(SpriteBatch spriteBatch, Vector2 tilePosition, float health);
    }
}
