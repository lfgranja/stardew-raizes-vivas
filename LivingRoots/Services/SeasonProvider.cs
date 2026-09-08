using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of <see cref="ISeasonProvider"/> that wraps <see cref="Game1.currentSeason"/>.
/// </summary>
public class SeasonProvider : ISeasonProvider
{
    /// <summary>
    /// Gets the current season name from the game state.
    /// </summary>
    public string CurrentSeason => Game1.currentSeason;
}
