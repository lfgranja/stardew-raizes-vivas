using LivingRoots.Domain.Visualization;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Production <see cref="IHudNotifier"/>: routes the accessibility
    /// degradation notice through the game's own HUD message system
    /// (FR-009). Deliberately the only seam here — this feature builds no UI
    /// of its own (FR-008a), so a notice is a native red message, in the game's
    /// font, without new artwork.
    /// </summary>
    public sealed class HudNotifierImpl : IHudNotifier
    {
        /// <inheritdoc />
        public void NotifyAccessibilityPatternsDegraded(int visibleTileCount)
        {
            // FR-008a: no bespoke text component — the game's own on-screen
            // message channel carries the notice in the player's language.
            Game1.showRedMessage($"Accessibility patterns hidden: too many tiles visible ({visibleTileCount}).");
        }
    }
}
