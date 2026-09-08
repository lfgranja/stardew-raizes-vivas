using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of <see cref="ITimeProvider"/>
/// that wraps <see cref="Game1.Date.TotalDays"/>.
/// </summary>
public class TimeProvider : ITimeProvider
{
    /// <summary>
    /// Gets the total number of days elapsed in the current save.
    /// </summary>
    public int TotalDays => Game1.Date.TotalDays;
}
