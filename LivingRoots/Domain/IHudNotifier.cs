namespace LivingRoots.Domain
{
    /// <summary>
    /// Shows a transient, player-facing notification in the game's HUD.
    /// Exists so services that must surface a message to the player can be tested without
    /// a booted game, matching the extraction pattern already used for
    /// <see cref="ITimeProvider"/>, <see cref="ISeasonProvider"/>, <see cref="IPlayerProvider"/>
    /// and <see cref="ILocationProvider"/>.
    /// </summary>
    public interface IHudNotifier
    {
        /// <summary>
        /// Shows a notification to the player.
        /// </summary>
        /// <param name="message">The message text to display to the player.</param>
        void ShowNotification(string message);
    }
}
