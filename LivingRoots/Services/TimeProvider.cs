using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of ITimeProvider.
/// Wraps Game1.Date.TotalDays for whole-day granularity.
/// </summary>
public class TimeProvider : ITimeProvider
{
    /// <summary>Gets the current total days from the game clock.</summary>
    public int TotalDays => Game1.Date.TotalDays;
}
