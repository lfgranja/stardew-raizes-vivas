namespace LivingRoots.Domain
{
    /// <summary>
    /// Provides access to the current in-game season.
    /// Wraps <see cref="StardewValley.Game1.currentSeason"/> for testability.
    /// </summary>
    public interface ISeasonProvider
    {
        /// <summary>
        /// Gets the current season name (e.g. "spring", "summer", "fall", "winter").
        /// </summary>
        string CurrentSeason { get; }
    }
}
