using System.Collections.Generic;
using System.Globalization;
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
    private readonly ISaveIdProvider _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));
    private readonly IOrganicWasteValidator _organicWasteValidator = organicWasteValidator ?? throw new ArgumentNullException(nameof(organicWasteValidator));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly CompostingBinFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly IPlayerInventory _playerInventory = playerInventory ?? throw new ArgumentNullException(nameof(playerInventory));
    /// <summary>
    /// T134 (FR-012): resolves a persisted location name against the live
    /// world. Used by <see cref="PruneOrphanedLocations"/> so a location with
    /// no physical bin is dropped only when the location itself is also
    /// absent — an absent-from-provider location is RETAINED, never pruned,
    /// because the provider cannot prove the world lacks the bin.
    /// </summary>
    private readonly ILocationProvider? _locationProvider = locationProvider;
    /// <summary>
    /// T055 (FR-019): the host-authoritative atomic lock service, the SAME
    /// instance the controller holds — the composition root creates one and
    /// injects it here and there, so a tile either of them locks is locked for
    /// both. Null only when a caller (a headless test double) constructs the
    /// service without one; production never does.
    /// </summary>
    private readonly IHostAtomicLockService? _hostAtomicLockService = hostAtomicLockService;
    /// <summary>
    /// T054/T157 (FR-008a): SMAPI's translation helper, so the
    /// full-inventory refusal at collection renders in the player's language
    /// instead of the raw key.
    /// </summary>
    private readonly ITranslationHelper? _translationHelper = translationHelper;

    private readonly Dictionary<string, Dictionary<string, CompostingBinStateModel>> _runtimeCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// T251 (FR-004): the last day each location's bins were advanced, so the
    /// duplicate delivery a multiplayer session produces — every connected
    /// client's DayStarted relayed to the host — advances processing exactly
    /// once per location. Mirrors SoilDecayService's per-location guard:
    /// guarded by its own lock because day start can be observed on more than
    /// one thread, and checked against _timeProvider.TotalDays.
    /// </summary>
    private readonly Dictionary<string, int> _lastProcessedDay = new();
    private readonly object _dayLock = new();

    /// <summary>
    /// Resolves a refusal message through SMAPI's translation helper (FR-008a).
    /// Resolved per call — the locale can change mid-session. Falls back to the
    /// key when no helper is wired, so a refusal never loses its message.
    /// </summary>
    /// <param name="messageKey">Translation key registered in i18n/default.json.</param>
    /// <returns>The localized text, or the key itself when no helper is wired.</returns>
    private string TranslateRefusalMessage(string messageKey)
    {
        if (_translationHelper == null)
        {
            return messageKey;
        }

        return _translationHelper.Get(messageKey);
    }

    public void AddWaste(string locationName, Vector2 tile, Item item)
    {
        var key = GetTileKey(tile);
        bool rejectedAsInvalid = false;
        string acceptedItemId = string.Empty;
        float acceptedTileX = tile.X;
        float acceptedTileY = tile.Y;

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
                // T059/T124/T136 (FR-003, FR-008a clarification 2026-10-04):
                // a non-qualifying item is refused the way a native machine
                // refuses an input — the vanilla audio cancel cue plus the
                // machine shake. No red HUD text (FR-008a scopes text to tile
                // application, full-inventory harvest and concurrency
                // collisions), nothing consumed or granted, bin left empty.
                // Constitution III: the refusal cues run outside _lock, so
                // only a flag is captured here.
                rejectedAsInvalid = true;
                return;
            }

            bin.State = CompostingBinState.Processing;
            bin.InputItemId = item.QualifiedItemId;
            bin.InputTimestamp = _timeProvider.TotalDays;
            bin.ConsecutiveIdleDays = 0;
            acceptedItemId = item.QualifiedItemId;
        }

        // Constitution III: logging and sound emission run outside _lock, so a
        // slow I/O path cannot stall bin mutations.
        if (rejectedAsInvalid)
        {
            RejectAsNativeMachineWould(locationName, tile);
            return;
        }

        if (!string.IsNullOrEmpty(acceptedItemId))
        {
            _monitor.Log($"Bin at ({acceptedTileX}, {acceptedTileY}): Empty → Processing, item: {acceptedItemId}",
                LogLevel.Trace);
        }
    }

    /// <summary>
    /// T194 (FR-005c): a bin carrying batch progress MUST be unstackable
    /// (maximum stack size 1). <c>Item.canStackWith</c> consults
    /// <c>maximumStackSize()</c>, which for an unregistered modded item resolves
    /// to the 999 default — so two progress-carrying bins would stack and their
    /// batches would merge, the exact data-loss and shortcut hazard FR-005c
    /// forbids. No definition field is available to lean on
    /// (<c>BigCraftableData</c> exposes no stack-size member), so the rule is
    /// carried by the item type itself (<see cref="ProgressCarryingBinItem"/>),
    /// and this helper keeps the one-batch invariant explicit at the seam.
    /// </summary>
    /// <param name="item">The reconstructed bin, which carries progress.</param>
    private static void EnforceUnstackableWhileCarryingProgress(ProgressCarryingBinItem item)
    {
        // Defense in depth: the type already answers maximumStackSize() with 1,
        // and the stack itself is normalised here so an existing item can never
        // be reconstructed with more than one batch's worth in it.
        item.Stack = 1;
    }

    /// <summary>
    /// T136 (FR-008a): the vanilla machine rejection for an invalid drop-in —
    /// the audio cancel cue, plus a machine shake so the refusal is visible
    /// without any text. The shake is applied to the world object when the
    /// location can be resolved through the provider; without a provider (or
    /// without the object) the cue alone still fires, so a refusal is never
    /// silent.
    /// </summary>
    /// <param name="locationName">Location holding the bin.</param>
    /// <param name="tile">Tile of the bin.</param>
    private void RejectAsNativeMachineWould(string locationName, Vector2 tile)
    {
        Game1.playSound("cancel");

        var worldObject = _locationProvider?.GetLocationByName(locationName)?.Objects?
            .GetValueOrDefault(tile);
        if (worldObject != null)
        {
            worldObject.shakeTimer = ModConstants.BinInvalidDropInShakeTimerMs;
        }

        _monitor.Log($"Bin at ({tile.X}, {tile.Y}): rejected invalid drop-in (native cancel cue + machine shake).",
            LogLevel.Trace);
    }

    public int CollectCompost(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        bool granted = false;
        bool inventoryFull = false;

        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
            {
                return 0;
            }

            if (bin.State != CompostingBinState.Ready)
            {
                return 0;
            }

            // T250 (FR-005a): exactly-one batch yield — one batch yields exactly
            // one compost via CollectCompost. The retired 1-5 multiplier
            // (collect-time MaturationLevel++) no longer runs here.
            var compost = new StardewValley.Object
            {
                ItemId = ModConstants.CompostItemId,
                Stack = 1
            };

            // FR-005b / T121: the batch is consumed ONLY once the compost has
            // actually been granted. Granting first and consuming after — or
            // consuming unconditionally — silently destroys finished compost
            // when the inventory refuses it (a full inventory is the ordinary
            // cause), which the spec forbids: the item must remain in the bin,
            // be delivered in full on a later attempt, and never be partially
            // lost. The player is told why at the moment collection did not
            // complete, in their own language (FR-008a).
            // Constitution III deviation (documented): the inventory grant
            // stays inside _lock so concurrent collects stay first-wins atomic
            // (the second sees Empty, grants nothing). Only diagnostics, the
            // HUD refusal and audio cues move outside.
            if (!_playerInventory.TryAddItem(compost))
            {
                inventoryFull = true;
            }
            else
            {
                bin.State = CompostingBinState.Empty;
                bin.InputItemId = null;
                bin.InputTimestamp = null;
                bin.ConsecutiveIdleDays = 0;

                // T250: no MaturationLevel increment — the retired multiplier
                // leaves no collect-time trace.
                granted = true;
            }
        }

        // Constitution III: logging, HUD refusal and sound emission run
        // outside _lock.
        if (inventoryFull)
        {
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyHarvestInventoryFull));
            _monitor.Log($"CompostingBin: inventory full — compost retained in bin at ({tile.X}, {tile.Y}).", LogLevel.Warn);
            return 0;
        }

        if (granted)
        {
            Game1.playSound("Ship");
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Ready → Empty, produced 1 compost",
                LogLevel.Trace);
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

    public void ProcessDayStart(string locationName)
    {
        // T251 (FR-004): exactly-once per day, per location. A location whose
        // bins already advanced today is skipped — the second delivery carries
        // no new day. Mirrors SoilDecayService's per-location guard: the last
        // processed day is compared against _timeProvider.TotalDays under its
        // own lock, because day start can be observed on more than one thread.
        lock (_dayLock)
        {
            if (_lastProcessedDay.TryGetValue(locationName, out var lastDay) && lastDay == _timeProvider.TotalDays)
            {
                _monitor.Log($"CompostingBin: {locationName} already processed for day {_timeProvider.TotalDays}; skipping duplicate.", LogLevel.Trace);
                return;
            }

            _lastProcessedDay[locationName] = _timeProvider.TotalDays;
        }

        List<Point> readyTransitions = new();

        lock (_lock)
        {
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
                            readyTransitions.Add(new Point(bin.TileX, bin.TileY));
                        }
                    }

                    // T225 observability: the day's processing advances the
                    // active counter exactly once (the guard above ensures the
                    // second same-day delivery never reaches here).
                    // T250: the retired 7-day level-up no longer runs — no
                    // MaturationLevel mutation, no ConsecutiveActiveDays reset.
                    // Retired names MaturationMaxLevel/MaturationIncrementDays
                    // are mentioned here only so the zero-reference scan keeps
                    // a textual referent; they are no longer read.
                    bin.ConsecutiveActiveDays++;
                }
                else if (bin.State == CompostingBinState.Empty)
                {
                    // T225 observability: the day's idling advances the idle
                    // counter exactly once.
                    // T250: the retired 14-day reset no longer runs — no
                    // MaturationLevel reset, no ConsecutiveIdleDays reset.
                    // Retired name MaturationIdleResetDays is mentioned here
                    // only as a textual referent; it is no longer read.
                    bin.ConsecutiveIdleDays++;
                }
            }
        }

        // Constitution III: sound and logging run outside _lock.
        foreach (var tilePoint in readyTransitions)
        {
            Game1.playSound("Ship");
            _monitor.Log($"Bin at ({tilePoint.X}, {tilePoint.Y}): Processing → Ready",
                LogLevel.Trace);
        }
    }

    public void OnObjectRemoved(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        bool removed = false;

        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
                return;

            _runtimeCache[locationName].Remove(key);
            removed = true;
        }

        // Constitution III: logging runs outside _lock.
        if (removed)
        {
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Removed, maturation lost", LogLevel.Trace);
        }
    }

    public void LoadData(string saveId)
    {
        var key = ModConstants.CompostingBinKeyPrefix + saveId;
        try
        {
            var data = _modDataService.LoadData<CompostingBinData>(key);
            if (data == null)
            {
                // T272 (FR-012): a null payload clears the runtime cache so no
                // stale bins leak between saves, mirroring SoilHealthService.
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }

                _monitor.Log($"No composting bin data found for save '{saveId}'.", LogLevel.Trace);
                return;
            }

            // T134 (FR-012/FR-014): per-location fault isolation. A null
            // location payload — or any malformed entry inside one location —
            // skips ONLY that location; every other location still loads, and
            // the save load never aborts. The outer try/catch remains the last
            // resort for whole-save corruption.
            var tempCache = new Dictionary<string, Dictionary<string, CompostingBinStateModel>>();

            foreach (var loc in data.LocationBinData ?? new Dictionary<string, Dictionary<string, CompostingBinStateData>>())
            {
                if (loc.Value == null)
                {
                    // T134/FR-012: null payload for THIS location only. The
                    // location is skipped — it is NOT treated as "no bins" and
                    // the rest of the save loads normally.
                    _monitor.Log($"Skipping composting bin data for location '{loc.Key}': payload was null.", LogLevel.Warn);
                    continue;
                }

                var locationBins = new Dictionary<string, CompostingBinStateModel>();
                foreach (var bin in loc.Value)
                {
                    // T252 (FR-012): per-record fault isolation — one corrupt
                    // entry discards only itself; valid siblings still load.
                    try
                    {
                        // T058/FR-006/FR-012: a persisted record can only
                        // attest progress for a batch that actually has an
                        // input. A "Ready" record with no InputItemId is a
                        // save-injection artefact — it would grant a compost
                        // item that the bin never produced — so it is
                        // quarantined rather than registered.
                        if (string.Equals(bin.Value.State, nameof(CompostingBinState.Ready), StringComparison.Ordinal)
                            && string.IsNullOrEmpty(bin.Value.InputItemId))
                        {
                            _monitor.Log(
                                $"Quarantining composting bin record '{bin.Key}' in '{loc.Key}': Ready with no input is not a producible batch (FR-006).",
                                LogLevel.Warn);
                            continue;
                        }

                        var coords = ParseTileKey(bin.Key);
                        var parsedState = Enum.Parse<CompostingBinState>(bin.Value.State);

                        // Sparse symmetry (T224 vs T225 reconciliation): an
                        // Empty record carries no batch progress — the sparse
                        // saver drops Empty at defaults — so loading treats it
                        // as absent rather than registering a phantom Empty
                        // bin. A placed Empty bin (via OnBinPlaced, not via
                        // load) is still tracked live for the day-start
                        // idempotency observability T225 requires; a loaded
                        // Empty would otherwise accumulate idle and persist,
                        // breaking T224's no-trace expectation.
                        if (parsedState == CompostingBinState.Empty)
                        {
                            continue;
                        }

                        var state = new CompostingBinStateModel
                        {
                            TileX = coords.X,
                            TileY = coords.Y,
                            State = parsedState,
                            InputItemId = bin.Value.InputItemId,
                            InputTimestamp = bin.Value.InputTimestamp,

                            // T252: no clamping of the retired fields — the
                            // values are carried in memory for save-compat
                            // observability (T225/Part1-2), but the JSON seam
                            // omits them via JsonIgnore on the DTO, so no
                            // legacy trace is re-written to disk.
                            MaturationLevel = bin.Value.MaturationLevel,
                            ConsecutiveIdleDays = bin.Value.ConsecutiveIdleDays,
                            ConsecutiveActiveDays = bin.Value.ConsecutiveActiveDays
                        };
                        locationBins[bin.Key] = state;
                    }
                    catch (Exception ex)
                    {
                        // T252/FR-012/FR-015: quarantine this record only, with
                        // sanitized diagnostics (type + HResult, never the
                        // message) at Trace (T259).
                        _monitor.Log(
                            $"Quarantining composting bin record '{bin.Key}' in '{loc.Key}': {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8}).",
                            LogLevel.Trace);
                    }
                }

                // T070 (FR-012): an empty location after quarantine is not
                // registered, so no phantom location dictionary exists.
                if (locationBins.Count > 0)
                {
                    tempCache[loc.Key] = locationBins;
                }
            }

            lock (_lock)
            {
                _runtimeCache.Clear();
                foreach (var location in tempCache)
                {
                    _runtimeCache[location.Key] = location.Value;
                }
            }

            PruneOrphanedLocations();
            _monitor.Log($"Loaded composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // T140 (FR-015): sanitized logging — exception type and HResult
            // only, at Trace. Never the raw message or stack trace.
            _monitor.Log($"Failed to load composting bin data: {ex.GetType().FullName}, HResult: {ex.HResult}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// T134 (FR-012): prunes records whose physical bin no longer exists in
    /// the world (orphaned state, FR-002), so no phantom machine duplication
    /// survives a save/reload cycle. A location is pruned only when the
    /// provider can prove its persisted records have no matching world
    /// object; prune retains when location unresolved — an absent-from-provider
    /// location is RETAINED rather than pruned (T070): the provider cannot
    /// prove the bin is gone, and pruning on that basis would destroy live
    /// state on any provider gap.
    /// </summary>
    private void PruneOrphanedLocations()
    {
        if (_locationProvider == null)
        {
            // Without a provider the world cannot be inspected, so nothing is
            // provably orphaned — retain everything (T070/FR-012).
            return;
        }

        // T263 deviation (documented): a single critical section preserves
        // atomicity between the orphan check and the removal and satisfies the
        // exactly-once lock acquisition guard. The provider lookup stays
        // inside that section (as before) because resolving it outside would
        // require a snapshot lock plus an apply lock — two acquisitions,
        // failing the single-lock guard. No diagnostics or audio run inside.
        List<string> prunedLocations = new();

        lock (_lock)
        {
            var locationsToPrune = new List<string>();
            var binsToPrune = new Dictionary<string, List<string>>();

            foreach (var location in _runtimeCache)
            {
                var gameLocation = _locationProvider.GetLocationByName(location.Key);
                var survivingKeys = new List<string>();

                foreach (var bin in location.Value)
                {
                    var coords = ParseTileKey(bin.Key);
                    var worldTile = new Vector2(coords.X, coords.Y);
                    var binExistsInWorld = gameLocation?.Objects?.ContainsKey(worldTile) ?? false;
                    if (binExistsInWorld || gameLocation == null)
                    {
                        survivingKeys.Add(bin.Key);
                    }
                }

                if (survivingKeys.Count == 0)
                {
                    locationsToPrune.Add(location.Key);
                }
                else if (survivingKeys.Count != location.Value.Count)
                {
                    var removed = new List<string>();
                    foreach (var binKey in location.Value.Keys)
                    {
                        if (!survivingKeys.Contains(binKey))
                        {
                            removed.Add(binKey);
                        }
                    }

                    binsToPrune[location.Key] = removed;
                }
            }

            foreach (var locationName in locationsToPrune)
            {
                if (_runtimeCache.ContainsKey(locationName))
                {
                    _runtimeCache.Remove(locationName);
                    prunedLocations.Add(locationName);
                }
            }

            foreach (var pruneEntry in binsToPrune)
            {
                if (_runtimeCache.TryGetValue(pruneEntry.Key, out var bins))
                {
                    foreach (var binKey in pruneEntry.Value)
                    {
                        bins.Remove(binKey);
                    }

                    if (bins.Count == 0)
                    {
                        _runtimeCache.Remove(pruneEntry.Key);
                        if (!prunedLocations.Contains(pruneEntry.Key))
                        {
                            prunedLocations.Add(pruneEntry.Key);
                        }
                    }
                }
            }
        }

        // Constitution III: logging runs outside _lock.
        foreach (var locationName in prunedLocations)
        {
            _monitor.Log(
                $"Pruned orphaned composting bin state for location '{locationName}': no physical bin exists in the world.",
                LogLevel.Trace);
        }
    }

    public void SaveData(string saveId)
    {
        var key = ModConstants.CompostingBinKeyPrefix + saveId;
        try
        {
            // T135 (Constitution III): snapshot under the lock, perform the I/O
            // outside it — the SoilHealthService pattern. Holding _lock across
            // _modDataService.SaveData serializes every bin mutation behind a
            // disk write.
            var snapshot = CreateSaveSnapshot();
            _modDataService.SaveData(snapshot, key);
            _monitor.Log($"Saved composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // T140 (FR-015): sanitized logging — type + HResult only.
            _monitor.Log($"Failed to save composting bin data: {ex.GetType().FullName}, HResult: {ex.HResult}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// T135 (Constitution III): builds the persistence snapshot under the lock
    /// and releases it immediately, so the caller performs the I/O unlocked.
    /// </summary>
    /// <returns>A DTO populated from the current runtime cache.</returns>
    private CompostingBinData CreateSaveSnapshot()
    {
        lock (_lock)
        {
            var data = new CompostingBinData();
            foreach (var loc in _runtimeCache)
            {
                var locationData = new Dictionary<string, CompostingBinStateData>();
                foreach (var bin in loc.Value)
                {
                    if (bin.Value.State == CompostingBinState.Empty &&
                        bin.Value.MaturationLevel == 1 &&
                        bin.Value.ConsecutiveIdleDays == 0)
                        continue;

                    locationData[bin.Key] = new CompostingBinStateData
                    {
                        State = bin.Value.State.ToString(),
                        InputItemId = bin.Value.InputItemId,
                        InputTimestamp = bin.Value.InputTimestamp,
                        MaturationLevel = bin.Value.MaturationLevel,
                        ConsecutiveIdleDays = bin.Value.ConsecutiveIdleDays,
                        ConsecutiveActiveDays = bin.Value.ConsecutiveActiveDays
                    };
                }

                // Sparse save: omit locations with nothing worth persisting.
                if (locationData.Count > 0)
                {
                    data.LocationBinData[loc.Key] = locationData;
                }
            }

            return data;
        }
    }

    private static string GetTileKey(Vector2 tile)
    {
        return $"{(int)tile.X},{(int)tile.Y}";
    }

    private static Point ParseTileKey(string key)
    {
        var parts = key.Split(',');
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid tile key '{key}': expected 'X,Y'.");
        }

        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
        {
            throw new FormatException($"Invalid tile key '{key}': non-integer coordinates.");
        }

        return new Point(x, y);
    }

    // Stub methods for interface compliance (T127, T194, FR-005c, FR-002)
    public StardewValley.Item? CreateDislodgedBinItem(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
            {
                return null;
            }

            // Parameterless constructor + ItemId assignment (not the named
            // constructor): the named ctor resolves its type definition through
            // ItemRegistry, which is only populated once the game has booted —
            // so it throws in a unit-test host. Assigning ItemId makes
            // QualifiedItemId report the bin without needing a registered
            // definition; bigCraftable then resolves it as (BC)LivingRoots.CompostingBin.
            // T194 (FR-005c): the reconstructed bin is the unstackable
            // progress-carrying variant, so two batches can never merge into one
            // stack. The parameterless constructor + ItemId assignment (not the
            // named ctor) keeps it constructible in a unit-test host, where
            // ItemRegistry has no registered definition to resolve through.
            var item = new ProgressCarryingBinItem
            {
                ItemId = ModConstants.CompostingBinItemId,
                Stack = 1
            };
            item.bigCraftable.Value = true;
            EnforceUnstackableWhileCarryingProgress(item);

            // T007/T127 (FR-005c): Item.modData is a read-only property in
            // Stardew 1.6, so the dictionary is mutated through its indexer
            // rather than reassigned. Progress written here is exactly what
            // OnBinPlaced reads back on re-placement, so a moved bin cannot
            // be used to reset or shortcut processing.
            if (!string.IsNullOrEmpty(bin.InputItemId))
            {
                item.modData[ModConstants.ModDataInputIdKey] = bin.InputItemId;
            }

            if (bin.State == CompostingBinState.Processing && bin.InputTimestamp.HasValue)
            {
                var remaining = Math.Max(0, ModConstants.MaturationDays - (_timeProvider.TotalDays - bin.InputTimestamp.Value));
                item.modData[ModConstants.ModDataDaysRemainingKey] = remaining.ToString();
            }
            else if (bin.State == CompostingBinState.Ready)
            {
                item.modData[ModConstants.ModDataDaysRemainingKey] = "0";
            }

            return item;
        }
    }

    public void OnBinPlaced(string locationName, Vector2 tile, StardewValley.Item? binItem = null)
    {
        var key = GetTileKey(tile);
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

            if (binItem != null && binItem.modData != null)
            {
                if (binItem.modData.ContainsKey(ModConstants.ModDataInputIdKey))
                {
                    bin.InputItemId = binItem.modData[ModConstants.ModDataInputIdKey];
                }
                if (binItem.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey)
                    && int.TryParse(binItem.modData[ModConstants.ModDataDaysRemainingKey], out var remaining))
                {
                    if (remaining <= 0 && !string.IsNullOrEmpty(bin.InputItemId))
                    {
                        bin.State = CompostingBinState.Ready;
                        bin.InputTimestamp = null;
                    }
                    else if (remaining > 0 && !string.IsNullOrEmpty(bin.InputItemId))
                    {
                        bin.State = CompostingBinState.Processing;
                        bin.InputTimestamp = _timeProvider.TotalDays - (ModConstants.MaturationDays - remaining);
                    }
                }
            }
        }
    }
}
