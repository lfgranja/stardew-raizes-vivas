namespace LivingRoots.Domain
{
    /// <summary>
    /// Provides access to the current player and their held item.
    /// Wraps <see cref="StardewValley.Game1.player"/> for testability.
    /// </summary>
    public interface IPlayerProvider
    {
        /// <summary>
        /// The current player instance.
        /// </summary>
        StardewValley.Farmer CurrentPlayer { get; }

        /// <summary>
        /// The item currently held by the player, or <c>null</c> if none.
        /// </summary>
        StardewValley.Item? CurrentItem { get; }
    }
}
