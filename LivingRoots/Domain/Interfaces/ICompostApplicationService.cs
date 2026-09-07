using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Service for applying compost items to tilled soil to restore health.
    /// Handles validation, consumption, floating text feedback, and audio cues.
    /// </summary>
    public interface ICompostApplicationService
    {
        /// <summary>
        /// Attempts to apply compost to a tile in the specified location.
        /// Validates tile is tilled, on farm/Greenhouse, and not at max health.
        /// On success: increases health, consumes compost, shows floating text.
        /// On failure: plays cancel sound, no visual feedback.
        /// </summary>
        /// <param name="location">The game location containing the tile.</param>
        /// <param name="tile">The tile coordinates to apply compost to.</param>
        /// <returns>True if compost was successfully applied, false otherwise.</returns>
        bool TryApplyCompost(StardewValley.GameLocation location, Vector2 tile);
    }
}
