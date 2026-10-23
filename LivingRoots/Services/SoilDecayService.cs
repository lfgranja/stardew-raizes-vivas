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
    ILocationProvider locationProvider) : ISoilDecayService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly SeasonalDecayMultiplier _seasonalDecayMultiplier = seasonalDecayMultiplier ?? throw new ArgumentNullException(nameof(seasonalDecayMultiplier));
    private readonly ISeasonProvider _seasonProvider = seasonProvider ?? throw new ArgumentNullException(nameof(seasonProvider));
    private readonly ILocationProvider _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));

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

        if (multiplier == 0f)
        {
            _monitor.Log($"SoilDecay: Winter - no decay for {locationName}.", LogLevel.Trace);
            return;
        }

        // T068 (FR-013): this location's day tick
        // already ran today — a duplicate delivery,
        // not a new day — so the decay walk (and
        // its health updates) is skipped without
        // recording a second pass. Recorded only
        // when decay work is about to run, so a
        // location that appears later on the same
        // day is still processed.
        var currentDay = Game1.dayOfMonth;
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
                    // FR-014: an absent record is the unknown sentinel (float.NaN) — "no
                    // data", not a 0% reading. Resolve it to the same 0 baseline the
                    // producer's UpdateHealth applies to an absent record, so the sentinel
                    // never reaches the arithmetic: a NaN currentHealth would otherwise flow
                    // into actualDecay and poison the health-lost accumulator — and the
                    // day-start summary that reports it.
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
