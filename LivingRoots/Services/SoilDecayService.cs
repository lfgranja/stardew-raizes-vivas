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
    ISeasonProvider seasonProvider,
    ILocationProvider locationProvider,
    ITimeProvider? timeProvider = null) : ISoilDecayService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly SeasonalDecayMultiplier _seasonalDecayMultiplier = seasonalDecayMultiplier ?? throw new ArgumentNullException(nameof(seasonalDecayMultiplier));
    private readonly ISeasonProvider _seasonProvider = seasonProvider ?? throw new ArgumentNullException(nameof(seasonProvider));
    private readonly ILocationProvider _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
    private readonly ITimeProvider? _timeProvider = timeProvider;

    /// <summary>
    /// T068 (FR-013): the last day each location's decay was applied, so the
    /// duplicate delivery a multiplayer session produces — every connected
    /// client's DayStarted relayed to the host — applies the day's decay
    /// exactly once per location. Guarded by a lock because day start can be
    /// observed on more than one thread.
    /// </summary>
    private readonly Dictionary<string, int> _lastProcessedDay = new();
    private readonly object _dayLock = new();

    public void ProcessDayStart(string locationName)
    {
        if (locationName == "Greenhouse")
        {
            _monitor.Log("SoilDecay: Greenhouse exempt from decay.", LogLevel.Trace);
            return;
        }

        // T068 (FR-013): exactly-once per day, per location. A location whose
        // decay already ran today is skipped — the second delivery carries no
        // new day. Without a time provider the guard cannot know the day, so a
        // caller that did not inject one keeps the unguarded (single-player
        // event path) behaviour; production always injects one.
        if (_timeProvider != null)
        {
            lock (_dayLock)
            {
                if (_lastProcessedDay.TryGetValue(locationName, out var lastDay) && lastDay == _timeProvider.TotalDays)
                {
                    _monitor.Log($"SoilDecay: {locationName} already processed for day {_timeProvider.TotalDays}; skipping duplicate.", LogLevel.Trace);
                    return;
                }

                _lastProcessedDay[locationName] = _timeProvider.TotalDays;
            }
        }

        var location = _locationProvider.GetLocationByName(locationName);
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
}
