namespace LivingRoots.Domain.Services;

/// <summary>
/// Provides season-based decay multipliers for soil health decay calculations.
/// Higher multiplier in summer (heat stress), lower in spring/fall (mild weather),
/// zero in winter (soil frozen).
/// </summary>
public class SeasonalDecayMultiplier
{
    /// <summary>
    /// Gets the decay multiplier for the specified season.
    /// </summary>
    /// <param name="season">Season name (case-insensitive). Expected: "spring", "summer", "fall", "winter".</param>
    /// <returns>
    /// Seasonal multiplier: Spring = 0.5x, Summer = 1.5x, Fall = 0.5x, Winter = 0.0x.
    /// Defaults to 0.5x for unrecognized seasons.
    /// </returns>
    public float GetMultiplier(string season)
    {
        return season.ToLowerInvariant() switch
        {
            "spring" => 0.5f,
            "summer" => 1.5f,
            "fall" => 0.5f,
            "winter" => 0.0f,
            _ => 0.5f
        };
    }
}
