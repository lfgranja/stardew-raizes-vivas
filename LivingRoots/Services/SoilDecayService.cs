using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
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
    ITimeProvider timeProvider,
    IMulchApplicationService? mulchService = null) : ISoilDecayService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly SeasonalDecayMultiplier _seasonalDecayMultiplier = seasonalDecayMultiplier ?? throw new ArgumentNullException(nameof(seasonalDecayMultiplier));
    private readonly ISeasonProvider _seasonProvider = seasonProvider ?? throw new ArgumentNullException(nameof(seasonProvider));
    private readonly ILocationProvider _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly IMulchApplicationService? _mulchService = mulchService;

    // T068 (FR-013): the exactly-once-per-day guard
    // is keyed by (day, location) — the duplicate
    // DayStarted delivery a multiplayer session
    // produces (every connected player's notification
    // is relayed to the host) must not decay a
    // location twice. The day clock is
    // Game1.dayOfMonth, the in-game day the DayStarted
    // event fires for; the guard compares with equality
    // rather than ordering because dayOfMonth wraps at
    // the end of each month, so a monotonic
    // comparison would starve the first day of the
    // next month. Guarded under _lock: the dictionary
    // is not safe for concurrent writes.
    private readonly Dictionary<string, int> _lastProcessedDayByLocation = new();
    private readonly object _lock = new();

    public void ProcessDayStart(string locationName)
    {
        if (locationName == "Greenhouse")
        {
            _monitor.Log("SoilDecay: Greenhouse exempt from decay.", LogLevel.Trace);
            return;
        }

        var location = _locationProvider.GetLocationByName(locationName);
        if (location == null)
        {
            _monitor.Log($"SoilDecay: Location '{locationName}' not found, skipping.", LogLevel.Trace);
            return;
        }

        var season = _seasonProvider.CurrentSeason.ToLowerInvariant();
        var multiplier = _seasonalDecayMultiplier.GetMultiplier(season);

        // T007 (spec 013): mulch day-countdown must run every day
        // (including Winter, where multiplier == 0 and the decay
        // loop is skipped) so expiry is day-based, not season-based.
        // Tick mulch for ALL hoeDirt tiles first, then apply decay
        // only to bare, non-mulched tiles.
        var currentDay = _timeProvider.TotalDays;
        lock (_lock)
        {
            if (_lastProcessedDayByLocation.TryGetValue(locationName, out var lastProcessedDay) &&
                lastProcessedDay == currentDay)
            {
                _monitor.Log($"SoilDecay: {locationName} already processed for day {currentDay}, skipping duplicate.", LogLevel.Trace);
                return;
            }

            _lastProcessedDayByLocation[locationName] = currentDay;
        }

        // Tick mulch for all tilled tiles (including planted ones).
        if (_mulchService != null)
        {
            foreach (var kvp in location.terrainFeatures.Pairs.Where(p => p.Value is HoeDirt))
            {
                _mulchService.TickMulch(locationName, kvp.Key);
            }
        }

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

                    // T007 (spec 013): mulched tiles are protected — skip decay.
                    if (_mulchService != null && _mulchService.IsMulched(locationName, tile))
                    {
                        tilesProcessed++;
                        continue;
                    }

                    var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
                    if (float.IsNaN(currentHealth))
                    {
                        currentHealth = 0f;
                    }
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
