#pragma warning disable CS9113 // Parameter is unread: saveIdProvider/hostAtomicLockService retained for API/test compatibility
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Services;

public class CompostingBinService(
    IModDataService modDataService,
    ISaveIdProvider saveIdProvider,
    IOrganicWasteValidator organicWasteValidator,
    IMonitor monitor,
    ITimeProvider timeProvider,
    CompostingBinFactory factory,
    IPlayerInventory playerInventory,
    ILocationProvider? locationProvider = null,
    IHostAtomicLockService? hostAtomicLockService = null,
    ITranslationHelper? translationHelper = null) : ICompostingBinService
{
    private readonly IModDataService _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
    private readonly IOrganicWasteValidator _organicWasteValidator = organicWasteValidator ?? throw new ArgumentNullException(nameof(organicWasteValidator));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly CompostingBinFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly IPlayerInventory _playerInventory = playerInventory ?? throw new ArgumentNullException(nameof(playerInventory));
    private readonly ILocationProvider? _locationProvider = locationProvider;

    // T054 (FR-008a): SMAPI translation helper for the
    // inventory-full refusal on harvest. Optional so headless
    // test doubles keep constructing the service; a null
    // helper falls back to the raw key.
    private readonly ITranslationHelper? _translationHelper = translationHelper;

    private readonly Dictionary<string, Dictionary<string, CompostingBinStateModel>> _runtimeCache = new();
    private readonly object _lock = new();

    public void AddWaste(string locationName, Vector2 tile, Item item)
    {
        var key = GetTileKey(tile);
        bool accepted = false;
        bool rejected = false;
        string rejectedItemId = "null";

        // Constitution III: the accepted/rejected outcome is
        // decided under the lock, but the log write, the audio
        // cue and the machine shake happen after it — a lock
        // never spans game or monitor I/O.
        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName))
                _runtimeCache[locationName] = new Dictionary<string, CompostingBinStateModel>();

            var bins = _runtimeCache[locationName];
            if (!bins.TryGetValue(key, out var bin))
            {
                bin = _factory.CreateBin((int)tile.X, (int)tile.Y);
                bins[key] = bin;
            }

            if (bin.State != CompostingBinState.Empty) return;
            if (!_organicWasteValidator.IsValidOrganicWaste(item))
            {
                rejected = true;
                rejectedItemId = item == null ? "null" : item.QualifiedItemId;
            }
            else
            {
                bin.State = CompostingBinState.Processing;
                bin.InputItemId = item.QualifiedItemId;
                bin.InputTimestamp = _timeProvider.TotalDays;
                bin.ConsecutiveIdleDays = 0;
                accepted = true;
            }
        }

        if (rejected)
        {
            // T060 (FR-003, FR-008a clarification
            // 2026-10-04, T059): a non-qualifying
            // item is refused the way a native
            // machine refuses — the game's own
            // audio cancel cue and machine shake.
            // FR-008a: vanilla audio cancel cue + machine shake,
            // no HUD text.
            Game1.playSound("cancel");
            // Machine shake (FR-008a): shake the composting bin
            // like vanilla machines do on invalid item drop-in.
            var location = _locationProvider?.GetLocationByName(locationName);
            if (location != null &&
                location.Objects.TryGetValue(tile, out var machine) &&
                machine != null)
            {
                machine.shakeTimer = ModConstants.BinInvalidDropInShakeTimerMs;
            }
            // T181 (domain purity): OrganicWasteValidator is a pure rule with
            // no SMAPI dependency, so the rejection trace moved here — the
            // application layer that owns IMonitor.
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): item {rejectedItemId} rejected invalid drop-in as organic waste; bin remains Empty.", LogLevel.Trace);
            return;
        }

        if (accepted)
        {
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Empty → Processing, item: {item!.QualifiedItemId}",
                LogLevel.Trace);
        }
    }

    public int CollectCompost(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        bool harvested = false;
        bool inventoryRefused = false;

        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
                return 0;

            if (bin.State != CompostingBinState.Ready) return 0;

            // The bin now yields exactly one compost item regardless of maturation level.
            int outputCount = 1;

            for (int i = 0; i < outputCount; i++)
            {
                var compost = new StardewValley.Object(ModConstants.CompostItemId, 1);
                if (!_playerInventory.TryAddItem(compost))
                {
                    inventoryRefused = true;
                    break;
                }
            }

            if (!inventoryRefused)
            {
                // Successful harvest – reset the bin.
                bin.State = CompostingBinState.Empty;
                bin.InputItemId = null;
                bin.InputTimestamp = null;
                bin.ConsecutiveIdleDays = 0;
                bin.HasPaidThisBatch = true;

                if (bin.MaturationLevel < ModConstants.MaturationMaxLevel)
                {
                    bin.MaturationLevel++;
                }

                harvested = true;
            }
        }

        if (inventoryRefused)
        {
            // Inventory is full – keep the bin in Ready state and inform the player.
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyHarvestInventoryFull));
            _monitor.Log($"CompostingBin: inventory full when harvesting at ({tile.X}, {tile.Y}); bin remains Ready.", LogLevel.Warn);
            return 0; // No items harvested.
        }

        if (harvested)
        {
            Game1.playSound("Ship");
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Ready → Empty, produced 1 compost", LogLevel.Trace);
            return 1;
        }

        return 0;
    }

    public CompostingBinState GetBinState(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        lock (_lock)
        {
            if (_runtimeCache.ContainsKey(locationName) &&
                _runtimeCache[locationName].TryGetValue(key, out var bin))
                return bin.State;
            return CompostingBinState.Empty;
        }
    }

    // T056 (FR-004): the exactly-once-per-day guard
    // is per location — the Greenhouse matures on
    // the same day the farm does, and neither
    // location's bins wait behind the other's
    // processing tick. Guarded inside _lock: a
    // dictionary is not safe for concurrent writes,
    // where the single day field it replaced was.
    private readonly Dictionary<string, int> _lastProcessedDayByLocation = new();

    public void ProcessDayStart(string locationName)
    {
        // Ensure processing occurs only once per in‑game
        // day, per location.
        var currentDay = _timeProvider.TotalDays;
        bool alreadyProcessed = false;
        var transitionLogs = new List<string>();
        int maturedBins = 0;

        lock (_lock)
        {
            if (_lastProcessedDayByLocation.TryGetValue(locationName, out var lastProcessedDay) &&
                currentDay <= lastProcessedDay)
            {
                alreadyProcessed = true;
            }
            else
            {
                _lastProcessedDayByLocation[locationName] = currentDay;

                if (!_runtimeCache.ContainsKey(locationName)) return;

                foreach (var bin in _runtimeCache[locationName].Values)
                {
                    if (bin.State == CompostingBinState.Processing)
                    {
                        if (bin.InputTimestamp.HasValue)
                        {
                            var elapsed = _timeProvider.TotalDays - bin.InputTimestamp.Value;
                            if (elapsed >= ModConstants.MaturationDays)
                            {
                                bin.State = CompostingBinState.Ready;
                                maturedBins++;
                                transitionLogs.Add($"Bin at ({bin.TileX}, {bin.TileY}): Processing → Ready");
                            }
                        }

                        bin.ConsecutiveActiveDays++;
                        if (bin.ConsecutiveActiveDays >= ModConstants.MaturationIncrementDays &&
                            bin.MaturationLevel < ModConstants.MaturationMaxLevel)
                        {
                            bin.MaturationLevel++;
                            bin.ConsecutiveActiveDays = 0;
                            transitionLogs.Add($"Bin at ({bin.TileX}, {bin.TileY}): Maturation level increased to {bin.MaturationLevel}");
                        }
                    }
                    else if (bin.State == CompostingBinState.Empty)
                    {
                        bin.ConsecutiveIdleDays++;
                        if (bin.ConsecutiveIdleDays >= ModConstants.MaturationIdleResetDays)
                        {
                            bin.MaturationLevel = 1;
                            bin.ConsecutiveIdleDays = 0;
                            transitionLogs.Add($"Bin at ({bin.TileX}, {bin.TileY}): Maturation reset to 1 after {ModConstants.MaturationIdleResetDays} idle days");
                        }
                    }
                }
            }
        }

        if (alreadyProcessed)
        {
            _monitor.Log($"Bin day start for {locationName} already processed for day {currentDay}; skipping.", LogLevel.Trace);
            return;
        }

        for (int i = 0; i < maturedBins; i++)
        {
            Game1.playSound("Ship");
        }

        foreach (var transitionLog in transitionLogs)
        {
            _monitor.Log(transitionLog, LogLevel.Trace);
        }
    }

    public void OnObjectRemoved(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        bool removed = false;

        lock (_lock)
        {
            if (_runtimeCache.ContainsKey(locationName) &&
                _runtimeCache[locationName].TryGetValue(key, out _))
            {
                _runtimeCache[locationName].Remove(key);
                removed = true;
            }
        }

        if (removed)
        {
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Removed, maturation lost", LogLevel.Trace);
        }
    }

    public void OnBinPlaced(string locationName, Vector2 tile, StardewValley.Item? binItem)
    {
        if (binItem == null) return;
        var key = GetTileKey(tile);
        string? restoredLog = null;

        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName))
                _runtimeCache[locationName] = new Dictionary<string, CompostingBinStateModel>();

            var bins = _runtimeCache[locationName];
            if (!bins.TryGetValue(key, out var bin))
            {
                bin = _factory.CreateBin((int)tile.X, (int)tile.Y);
                bins[key] = bin;
            }

            if (binItem.modData != null &&
                binItem.modData.TryGetValue(ModConstants.ModDataInputIdKey, out string inputId) &&
                binItem.modData.TryGetValue(ModConstants.ModDataDaysRemainingKey, out string daysRemainingStr) &&
                int.TryParse(daysRemainingStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int daysRemaining))
            {
                // T056 (FR-005c, SC-004): the
                // carried batch restores the
                // state it had. Zero remaining
                // days is a finished batch —
                // the same DaysUntilReady = 0
                // semantics the domain
                // CompostingBinStateModel.
                // PopulateFromModData applies —
                // so a bin picked up while
                // holding finished compost
                // restores as Ready instead of
                // silently restarting the
                // batch.
                bin.State = daysRemaining <= 0
                    ? CompostingBinState.Ready
                    : CompostingBinState.Processing;
                bin.InputItemId = inputId;
                bin.InputTimestamp = _timeProvider.TotalDays - ModConstants.MaturationDays + daysRemaining;
                restoredLog = $"Bin placed at ({tile.X}, {tile.Y}) restored state: {bin.State} {inputId}, {daysRemaining} days remaining";
            }
        }

        if (restoredLog != null)
        {
            _monitor.Log(restoredLog, LogLevel.Trace);
        }
    }

    public StardewValley.Item CreateDislodgedBinItem(string locationName, Vector2 tile)
    {
        var item = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
        // T076 (FR-005c): the bin is a big craftable —
        // the same (BC) item the crafting recipe yields
        // (recipe segment 3 is `true`) and the world
        // placement creates. bigCraftable.Value flips
        // QualifiedItemId from (O) to (BC) (verified
        // against the game assembly), so a dislodged
        // bin resolves as the machine it was instead
        // of a nonexistent (O) object.
        item.bigCraftable.Value = true;
        var key = GetTileKey(tile);

        lock (_lock)
        {
            if (_runtimeCache.ContainsKey(locationName) &&
                _runtimeCache[locationName].TryGetValue(key, out var bin) &&
                (bin.State == CompostingBinState.Processing || bin.State == CompostingBinState.Ready) &&
                bin.InputItemId != null && bin.InputTimestamp.HasValue)
            {
                // T056 (FR-005c, SC-004): a bin picked
                // up while its compost is ready retains
                // the batch — the finished state travels
                // in the same modData as a running
                // batch, with zero days remaining.
                item.modData[ModConstants.ModDataInputIdKey] = bin.InputItemId;
                var elapsed = _timeProvider.TotalDays - bin.InputTimestamp.Value;
                var remaining = Math.Max(0, ModConstants.MaturationDays - elapsed);
                item.modData[ModConstants.ModDataDaysRemainingKey] = remaining.ToString(CultureInfo.InvariantCulture);
            }
        }
        return item;
    }

    public int GetMaxStackSize(StardewValley.Item binItem)
    {
        if (binItem != null && binItem.modData != null &&
            (binItem.modData.ContainsKey(ModConstants.ModDataInputIdKey) ||
             binItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey)))
        {
            return 1;
        }
        return ModConstants.CompostStackMax;
    }

    public void LoadData(string saveId)
    {
        var key = ModConstants.CompostingBinKeyPrefix + saveId;
        try
        {
            var data = _modDataService.LoadData<CompostingBinData>(key);
            if (data == null)
            {
                _monitor.Log($"No composting bin data found for save '{saveId}'.", LogLevel.Trace);
                return;
            }

            // Build the temp cache outside the lock — the record
            // parsing reads the world through ILocationProvider and
            // emits diagnostics, neither of which belongs inside a
            // critical section (Constitution III). The swap is a
            // single assignment under the lock.
            var loaded = new Dictionary<string, Dictionary<string, CompostingBinStateModel>>();
            foreach (var loc in data.LocationBinData)
            {
                var location = _locationProvider?.GetLocationByName(loc.Key);

                // (T134 FR-012/FR-014) Guard null payload: skip only that location
                // rather than throwing and aborting the entire load via the outer catch.
                if (loc.Value == null)
                {
                    _monitor.Log($"Null bin data for location {loc.Key}; skipping.", LogLevel.Trace);
                    continue;
                }

                var binDict = new Dictionary<string, CompostingBinStateModel>();
                foreach (var bin in loc.Value)
                {
                    try
                    {
                        var coords = ParseTileKey(bin.Key);
                        var tileVec = new Vector2(coords.X, coords.Y);
                        var recordState = Enum.Parse<CompostingBinState>(bin.Value.State);

                        // T060 (FR-006, FR-012, FR-015):
                        // a Ready record with no input
                        // provenance is forged. The state
                        // machine only reaches Ready from
                        // Processing, and AddWaste always
                        // records the input item when it
                        // enters Processing, so no
                        // legitimate save can hold a Ready
                        // record without an InputItemId —
                        // such a record can only come from
                        // a hand-edited save. Quarantine
                        // it: skip it from runtime
                        // registration with a sanitized
                        // diagnostic (the tile key and the
                        // rule it violated, never raw save
                        // content), so an injected
                        // "finished batch" yields no
                        // compost (FR-006) while the load
                        // of valid bins continues
                        // (FR-012, FR-014).
                        if (recordState == CompostingBinState.Ready &&
                            string.IsNullOrWhiteSpace(bin.Value.InputItemId))
                        {
                            _monitor.Log($"Quarantined composting bin record at {loc.Key}:{bin.Key}: Ready record without input provenance.", LogLevel.Trace);
                            continue;
                        }

                        // T079 (FR-012): a location the provider
                        // omits — a Greenhouse only lazily
                        // loaded at load time — still holds
                        // real bins. A null provider result
                        // is an omission, not evidence of
                        // absence: retain those entries.
                        // Prune only entries positively
                        // contradicted by world state — the
                        // location is loaded and its tile
                        // holds something other than a
                        // composting bin.
                        bool worldStateContradictsRecord = _locationProvider != null &&
                            location != null &&
                            (!location.Objects.TryGetValue(tileVec, out var worldObj) ||
                             worldObj.ItemId != ModConstants.CompostingBinItemId);

                        if (!worldStateContradictsRecord)
                        {
                            var state = new CompostingBinStateModel
                            {
                                TileX = coords.X,
                                TileY = coords.Y,
                                State = recordState,
                                InputItemId = bin.Value.InputItemId,
                                InputTimestamp = bin.Value.InputTimestamp,
                                MaturationLevel = Math.Clamp(bin.Value.MaturationLevel, 1, ModConstants.MaturationMaxLevel),
                                ConsecutiveIdleDays = Math.Clamp(bin.Value.ConsecutiveIdleDays, 0, ModConstants.MaturationIdleResetDays),
                                ConsecutiveActiveDays = bin.Value.ConsecutiveActiveDays
                            };
                            binDict[bin.Key] = state;
                        }
                        else
                        {
                            // Orphan record – no matching world object.
                            _monitor.Log($"Pruned orphan bin record at {loc.Key}:{bin.Key} (no matching world object).", LogLevel.Trace);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log and skip malformed record.
                        _monitor.Log($"Failed to load composting bin record {loc.Key}:{bin.Key}: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    }
                }
                if (binDict.Count > 0)
                {
                    loaded[loc.Key] = binDict;
                }
            }

            lock (_lock)
            {
                _runtimeCache.Clear();
                foreach (var locationEntry in loaded)
                {
                    _runtimeCache[locationEntry.Key] = locationEntry.Value;
                }
            }

            // After loading all locations, prune any orphaned bins that may have been missed.
            PruneOrphanedLocations();
            _monitor.Log($"Loaded composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        // (T134) Narrowed outer catch: only catches truly unhandled exceptions,
        // no longer aborted by null loc.Value or per-record failures.
        catch (Exception ex)
        {
            _monitor.Log($"Failed to load composting bin data: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
        }
    }

    public void SaveData(string saveId)
    {
        var key = ModConstants.CompostingBinKeyPrefix + saveId;
        try
        {
            // (T135) Snapshot the data before locking, then perform I/O outside the lock
            // Model after SoilHealthService pattern: snapshot → I/O outside lock →
            var snapshotData = CreateSaveSnapshot();
            if (snapshotData == null)
            {
                _monitor.Log($"Saved composting bin data for save '{saveId}'.", LogLevel.Trace);
                return;
            }

            // Perform SaveData outside the lock to avoid blocking other threads
            _modDataService.SaveData(snapshotData, key);
            _monitor.Log($"Saved composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to save composting bin data: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
        }
    }

    private CompostingBinData CreateSaveSnapshot()
    {
        lock (_lock)
        {
            var data = new CompostingBinData();
            foreach (var loc in _runtimeCache)
            {
                data.LocationBinData[loc.Key] = new Dictionary<string, CompostingBinStateData>();
                foreach (var bin in loc.Value)
                {
                    if (bin.Value.State == CompostingBinState.Empty &&
                        bin.Value.MaturationLevel == 1 &&
                        bin.Value.ConsecutiveIdleDays == 0)
                        continue;

                    data.LocationBinData[loc.Key][bin.Key] = new CompostingBinStateData
                    {
                        State = bin.Value.State.ToString(),
                        InputItemId = bin.Value.InputItemId,
                        InputTimestamp = bin.Value.InputTimestamp,
                        MaturationLevel = bin.Value.MaturationLevel,
                        ConsecutiveIdleDays = bin.Value.ConsecutiveIdleDays,
                        ConsecutiveActiveDays = bin.Value.ConsecutiveActiveDays
                    };
                }
            }
            return data;
        }
    }

    private static string GetTileKey(Vector2 tile)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0},{1}", (int)tile.X, (int)tile.Y);
    }

    private static Point ParseTileKey(string key)
    {
        var parts = key.Split(',');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
        {
            throw new FormatException("Malformed tile key.");
        }

        return new Point(x, y);
    }

    // Removes any bin entries that no longer have a matching world object, and removes empty locations.
    private void PruneOrphanedLocations()
    {
        if (_locationProvider == null) return;

        var logs = new List<string>();

        // Exactly one acquisition: the world-state resolution and
        // the log collection both happen under the same lock, and
        // the collected diagnostics are emitted after release.
        lock (_lock)
        {
            var locationsToRemove = new List<string>();
            foreach (var locEntry in _runtimeCache)
            {
                var location = _locationProvider.GetLocationByName(locEntry.Key);
                // T079 (FR-012): a location the provider
                // omits is retained, not orphaned — a
                // lazily-loaded Greenhouse at load time
                // still holds real bins. A null provider
                // result is an omission, not evidence of
                // absence: prune retains when location
                // unresolved. Only positively
                // contradicted tiles (a loaded
                // location whose tile holds something
                // other than a composting bin) are
                // orphans.
                if (location == null)
                {
                    continue;
                }

                var binsToRemove = new List<string>();
#pragma warning disable S3267 // Loop contains try/catch and conditional pruning logic
                foreach (var binEntry in locEntry.Value)
                {
                    try
                    {
                        var coords = ParseTileKey(binEntry.Key);
                        var tileVec = new Vector2(coords.X, coords.Y);
                        if (!location.Objects.TryGetValue(tileVec, out var obj) || obj.ItemId != ModConstants.CompostingBinItemId)
                        {
                            binsToRemove.Add(binEntry.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        binsToRemove.Add(binEntry.Key);
                        logs.Add($"Pruned malformed bin key {locEntry.Key}:{binEntry.Key}: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})");
                    }
                }
#pragma warning restore S3267

                foreach (var binKey in binsToRemove)
                {
                    locEntry.Value.Remove(binKey);
                    logs.Add($"Pruned orphan bin record at {locEntry.Key}:{binKey} (no matching world object).");
                }

                if (locEntry.Value.Count == 0)
                {
                    locationsToRemove.Add(locEntry.Key);
                }
            }

            foreach (var locKey in locationsToRemove)
            {
                _runtimeCache.Remove(locKey);
                logs.Add($"Pruned orphan location entry {locKey} (no bins).");
            }
        }

        foreach (var log in logs)
        {
            _monitor.Log(log, LogLevel.Trace);
        }
    }

    /// <summary>
    /// Resolves a refusal translation key (T054, FR-008a) through the
    /// injected <see cref="ITranslationHelper"/> so the red HUD message
    /// renders in the player's language. Falls back to the raw key when
    /// no helper was injected (headless test doubles), which preserves
    /// the pre-T054 behaviour for those hosts. Resolved per call, never
    /// cached — the locale can change mid-session.
    /// </summary>
    private string TranslateRefusalMessage(string messageKey)
    {
        if (_translationHelper == null)
        {
            // Fallback must not leak raw translation keys to the player
            if (messageKey == ModConstants.TranslationKeyHarvestInventoryFull)
            {
                return "Your inventory is full!";
            }
            return messageKey;
        }

        var translated = _translationHelper.Get(messageKey);
        // If translation returns the key itself (untranslated), use fallback
        if (string.Equals(translated, messageKey, StringComparison.Ordinal) &&
            messageKey == ModConstants.TranslationKeyHarvestInventoryFull)
        {
            return "Your inventory is full!";
        }
        return translated;
    }
}
