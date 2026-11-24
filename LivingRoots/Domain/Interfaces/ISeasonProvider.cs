namespace LivingRoots.Domain;

/// <summary>
/// Provides the current season name.
/// Extracted from Game1.currentSeason for testability.
/// </summary>
public interface ISeasonProvider
{
    /// <summary>Gets the current season name (e.g., "spring", "summer", "fall", "winter").</summary>
    string CurrentSeason { get; }
}
