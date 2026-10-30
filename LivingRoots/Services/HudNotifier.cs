using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of <see cref="IHudNotifier"/>.
/// Wraps the game's HUD toast (<c>Game1.addHUDMessage</c>) so services
/// can surface a transient, player-facing notification (FR-009).
/// </summary>
public class HudNotifierImpl : IHudNotifier
{
    /// <summary>Shows a transient, auto-dismissible HUD toast to the player.</summary>
    /// <param name="message">The message text to display to the player.</param>
    public void ShowNotification(string message)
    {
        Game1.addHUDMessage(new HUDMessage(message));
    }
}
