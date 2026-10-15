using StardewModdingAPI;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Delivers a one-time accessibility-degradation notice to the player
    /// (FR-009). Extracted as a seam so the render pipeline can run without a
    /// booted game: the production implementation routes through the game's
    /// own HUD (<see cref="StardewValley.Game1.showRedMessage"/>), which needs
    /// <c>Game1</c> allocated and therefore cannot be unit-tested.
    /// </summary>
    public interface IHudNotifier
    {
        /// <summary>
        /// Shows the player a degradation notice explaining that accessibility
        /// patterns were suppressed because too many tiles are visible.
        /// </summary>
        /// <param name="visibleTileCount">Number of tiles currently visible.</param>
        void NotifyAccessibilityPatternsDegraded(int visibleTileCount);
    }
}
