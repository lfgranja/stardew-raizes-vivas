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
        /// <summary>
        /// Translation key for the accessibility-degradation notice, shipped in
        /// <c>LivingRoots/i18n/default.json</c> with per-locale translations.
        /// </summary>
        public const string AccessibilityPatternsDegradedKey = "message.accessibility.patterns-hidden";

        private readonly ITranslationHelper? _translationHelper;

        /// <summary>
        /// Creates the notifier.
        /// </summary>
        /// <param name="translationHelper">SMAPI's translation helper, so the notice renders in the player's language (FR-008a). Null only when a caller constructs the notifier without it; the notice then falls back to the English source text so it is never dropped.</param>
        public HudNotifierImpl(ITranslationHelper? translationHelper = null)
        {
            _translationHelper = translationHelper;
        }

        /// <inheritdoc />
        public void NotifyAccessibilityPatternsDegraded(int visibleTileCount)
        {
            // FR-008a: no bespoke text component — the game's own on-screen
            // message channel carries the notice in the player's language.
            string message = _translationHelper != null
                ? _translationHelper.Get(AccessibilityPatternsDegradedKey)
                : $"Accessibility patterns hidden: too many tiles visible ({visibleTileCount}).";
            Game1.showRedMessage(message);
        }
    }
}
