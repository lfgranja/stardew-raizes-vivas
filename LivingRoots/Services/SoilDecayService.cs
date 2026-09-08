using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services;

/// <summary>
/// Implementation of ISoilDecayService that processes all tilled tiles
/// in a location at day start and applies seasonal-multiplied decay to bare tiles.
/// </summary>
public class SoilDecayService(
    ISoilHealthService soilHealthService,
    IMonitor monitor,
    SeasonalDecayMultiplier seasonalDecayMultiplier,
    ISeasonProvider seasonProvider) : ISoilDecayService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly SeasonalDecayMultiplier _seasonalDecayMultiplier = seasonalDecayMultiplier ?? throw new ArgumentNullException(nameof(seasonalDecayMultiplier));
    private readonly ISeasonProvider _seasonProvider = seasonProvider ?? throw new ArgumentNullException(nameof(seasonProvider));

    public void ProcessDayStart(string locationName)
    {
        if (locationName == "Greenhouse")
        {
            _monitor.Log("SoilDecay: Greenhouse exempt from decay.", LogLevel.Trace);
            return;
        }

        var location = GetLocationByName(locationName);
        if (location == null)
        {
            _monitor.Log($"SoilDecay: Location '{locationName}' not found, skipping.", LogLevel.Trace);
            return;
        }

        var season = _seasonProvider.CurrentSeason.ToLowerInvariant();
        var multiplier = _seasonalDecayMultiplier.GetMultiplier(season);

        if (multiplier == 0f)
        {
            _monitor.Log($"SoilDecay: Winter - no decay for {locationName}.", LogLevel.Trace);
            return;
        }

        var decayAmount = ModConstants.DailyDecayRate * multiplier;
        int tilesProcessed = 0;
        float totalHealthLost = 0f;

        foreach (var kvp in location.terrainFeatures.Pairs)
        {
            if (kvp.Value is HoeDirt hoeDirt)
            {
                bool isBare = hoeDirt.crop == null;
                if (isBare)
                {
                    var tile = kvp.Key;
                    var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
                    var newHealth = Math.Max(currentHealth - decayAmount, 0f);
                    var actualDecay = currentHealth - newHealth;
                    if (actualDecay > 0f)
                    {
                        _soilHealthService.UpdateHealth(locationName, tile, -actualDecay);
                    }
                    totalHealthLost += actualDecay;
                    tilesProcessed++;
                }
            }
        }

        _monitor.Log($"SoilDecay: Processed {tilesProcessed} bare tiles in {locationName}, " +
                     $"total health lost: {totalHealthLost:F1}, season: {season}, " +
                     $"multiplier: {multiplier}, decayAmount: {decayAmount:F1}",
                     LogLevel.Trace);
    }

    private static GameLocation? GetLocationByName(string locationName)
    {
        foreach (var loc in Game1.locations)
        {
            if (loc.Name == locationName)
                return loc;
        }
        return null;
    }
}
