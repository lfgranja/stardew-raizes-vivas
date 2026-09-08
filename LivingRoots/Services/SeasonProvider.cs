using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of ISeasonProvider.
/// Wraps Game1.currentSeason for season-dependent logic.
/// </summary>
public class SeasonProvider : ISeasonProvider
{
    /// <summary>Gets the current season name from the game.</summary>
    public string CurrentSeason => Game1.currentSeason;
}
