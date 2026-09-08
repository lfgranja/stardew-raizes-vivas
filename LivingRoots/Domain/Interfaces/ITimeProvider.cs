namespace LivingRoots.Domain
{
    /// <summary>
    /// Provides access to the current in-game total day count.
    /// Wraps <see cref="Game1.Date.TotalDays"/> for testability.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the total number of days elapsed in the current save.
        /// </summary>
        int TotalDays { get; }
    }
}
