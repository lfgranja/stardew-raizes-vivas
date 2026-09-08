namespace LivingRoots.Domain;

/// <summary>
/// Provides the current game time in total days.
/// Extracted from Game1.Date.TotalDays for testability.
/// </summary>
public interface ITimeProvider
{
    /// <summary>Gets the current total days since game start.</summary>
    int TotalDays { get; }
}
