using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Verification.Evidence;
using LivingRoots.Verification.Queue;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Verification
{
    /// <summary>
    /// Dev/CI-only consumer for the headless verification queue. The runner
    /// starts the real game with stdin closed, so this mod is the only channel
    /// that can drive the world: it loads the isolated milestone save, consumes
    /// executable commands from <c>RUNNER_INJECTION_DIR</c>, captures the three
    /// Gate 3 signals, and acknowledges each queue generation.
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private const string ApiUniqueId = "lfgranja.LivingRoots";
        private const string LongPhaseSeedId = "LivingRoots.Verification.LongPhaseCrop";
        private const string DefaultLocation = "Farm";
        private const int SessionPollTicks = 30;
        private const int QueuePollTicks = 15;
        private const int SaveLoadTimeoutSeconds = 120;

        private string? _injectionDir;
        private string? _dumpDir;
        private string? _saveId;
        private ILivingRootsModApi? _api;

        private int _nextGeneration = 1;
        private int _generationInFlight;
        private readonly List<QueueCommand> _pending = new();
        private readonly List<Task<bool>> _pendingWrites = new();

        private bool _sessionLoaded;
        private bool _loadRequested;
        private bool _saveReady;
        private bool _stopped;
        private DateTime _loadStartedUtc;

        private bool _advanceInProgress;
        private bool _startNextDayPending;
        private int _daysRemaining;

        private bool _quitRequested;
        private int _sessionPollCounter;
        private int _queuePollCounter;

        public override void Entry(IModHelper helper)
        {
            _injectionDir = Environment.GetEnvironmentVariable("RUNNER_INJECTION_DIR");
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            if (string.IsNullOrWhiteSpace(_injectionDir))
            {
                Monitor.Log("Verification queue disabled: RUNNER_INJECTION_DIR is not set.", LogLevel.Trace);
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            RegisterLongPhaseCrop();
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _saveReady = true;
            _api = Helper.ModRegistry.GetApi<ILivingRootsModApi>(ApiUniqueId);
            if (_api == null || !_api.IsVerificationReady)
            {
                StopWithError(_nextGeneration, "api-unavailable");
            }

            try
            {
                var farm = Game1.getFarm();
                var layer = farm?.Map?.Layers != null && farm.Map.Layers.Count > 0 ? farm.Map.Layers[0] : null;
                if (layer != null)
                {
                    Monitor.Log($"verification: Farm map {layer.LayerWidth}x{layer.LayerHeight}.", LogLevel.Info);
                }
            }
            catch
            {
                // Diagnostic only; the queue still fails closed on a bad tile.
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!_advanceInProgress || _stopped)
            {
                return;
            }

            _daysRemaining--;
            if (_daysRemaining > 0)
            {
                _startNextDayPending = true;
            }
            else
            {
                _advanceInProgress = false;
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_stopped || string.IsNullOrWhiteSpace(_injectionDir))
            {
                return;
            }

            if (!_sessionLoaded)
            {
                _sessionPollCounter++;
                if (_sessionPollCounter >= SessionPollTicks)
                {
                    _sessionPollCounter = 0;
                    TryLoadSession();
                }

                return;
            }

            if (!_saveReady)
            {
                if (_loadRequested && (DateTime.UtcNow - _loadStartedUtc).TotalSeconds > SaveLoadTimeoutSeconds)
                {
                    StopWithError(_nextGeneration, "save-load-timeout");
                }

                return;
            }

            if (_api == null || !_api.IsVerificationReady)
            {
                StopWithError(_nextGeneration, "api-unavailable");
                return;
            }

            if (_advanceInProgress)
            {
                if (_startNextDayPending)
                {
                    BeginNextDay();
                }

                return;
            }

            if (_generationInFlight == 0)
            {
                _queuePollCounter++;
                if (_queuePollCounter >= QueuePollTicks)
                {
                    _queuePollCounter = 0;
                    PollQueue();
                }

                return;
            }

            if (_pending.Count > 0)
            {
                ExecuteNextCommand();
                return;
            }

            TryFinishGeneration();
        }

        private void TryLoadSession()
        {
            var sessionPath = Path.Combine(_injectionDir!, "session.json");
            if (!File.Exists(sessionPath))
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(sessionPath));
                var root = document.RootElement;
                _saveId = root.TryGetProperty("saveId", out var saveId) ? saveId.GetString() : null;
                _dumpDir = root.TryGetProperty("dumpDir", out var dumpDir) ? dumpDir.GetString() : null;
            }
            catch
            {
                StopWithError(_nextGeneration, "session-invalid");
                return;
            }

            if (string.IsNullOrWhiteSpace(_saveId) || string.IsNullOrWhiteSpace(_dumpDir))
            {
                StopWithError(_nextGeneration, "session-invalid");
                return;
            }

            try
            {
                Directory.CreateDirectory(_dumpDir);
            }
            catch
            {
                StopWithError(_nextGeneration, "dump-dir-unavailable");
                return;
            }

            _sessionLoaded = true;
            _loadRequested = true;
            _loadStartedUtc = DateTime.UtcNow;
            try
            {
                // Same path as LoadGameMenu.SaveFileSlot.Activate(): load the
                // isolated save directly, then drop the title menu.
                SaveGame.Load(_saveId);
                Game1.exitActiveMenu();
            }
            catch
            {
                StopWithError(_nextGeneration, "save-load-failed");
            }
        }

        private void PollQueue()
        {
            if (_generationInFlight != 0 || _stopped)
            {
                return;
            }

            var queuePath = Path.Combine(_injectionDir!, $"queue-{_nextGeneration}.txt");
            if (!File.Exists(queuePath))
            {
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(queuePath);
            }
            catch
            {
                StopWithError(_nextGeneration, "queue-read-failed");
                return;
            }

            var commands = new List<QueueCommand>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!QueueCommand.TryParse(trimmed, out var command, out var error) || command == null)
                {
                    StopWithError(_nextGeneration, error);
                    return;
                }

                commands.Add(command);
            }

            _generationInFlight = _nextGeneration;
            _pending.Clear();
            _pending.AddRange(commands);
        }

        private void ExecuteNextCommand()
        {
            var command = _pending[0];
            _pending.RemoveAt(0);
            var api = _api!;

            switch (command.Name)
            {
                case "lr_set_soil_health":
                    {
                        var location = command.Arguments.Count >= 4 ? command.GetString(3) : DefaultLocation;
                        api.SetSoilHealth(location, command.GetInt(0), command.GetInt(1), command.GetFloat(2));
                        break;
                    }

                case "lr_place_bin":
                    {
                        var location = command.Arguments.Count >= 3 ? command.GetString(2) : DefaultLocation;
                        if (!api.PlaceBin(location, command.GetInt(0), command.GetInt(1)))
                        {
                            FailGeneration("place-bin-failed");
                        }

                        break;
                    }

                case "lr_insert_waste":
                    {
                        var location = command.Arguments.Count >= 5 ? command.GetString(4) : DefaultLocation;
                        if (!api.InsertWaste(location, command.GetInt(0), command.GetInt(1), command.GetString(2), command.GetInt(3)))
                        {
                            FailGeneration("insert-waste-failed");
                        }

                        break;
                    }

                case "lr_plant_crop":
                    {
                        var location = command.Arguments.Count >= 4 ? command.GetString(3) : DefaultLocation;
                        if (!PlantCrop(location, command.GetInt(0), command.GetInt(1), command.GetString(2)))
                        {
                            FailGeneration("plant-crop-failed");
                        }

                        break;
                    }

                case "lr_advance_days":
                    {
                        var days = command.GetInt(0);
                        if (days > 0)
                        {
                            _daysRemaining = days;
                            _advanceInProgress = true;
                            _startNextDayPending = true;
                        }

                        break;
                    }

                case "lr_dump_crop_phase":
                    {
                        var dump = BuildCropPhaseDump();
                        _pendingWrites.Add(CropPhaseDumpWriter.WriteAsync(_dumpDir!, command.GetString(0), dump));
                        break;
                    }

                case "lr_dump_soil_health":
                    {
                        _pendingWrites.Add(WriteSoilHealthDumpAsync(command.GetString(0)));
                        break;
                    }

                case "lr_quit":
                    {
                        _quitRequested = true;
                        break;
                    }

                default:
                    FailGeneration("unknown-command");
                    break;
            }
        }

        private void TryFinishGeneration()
        {
            if (_generationInFlight == 0)
            {
                return;
            }

            if (!AllWritesComplete(out var writeOk))
            {
                return;
            }

            if (!writeOk)
            {
                FailGeneration("dump-write-failed");
                return;
            }

            WriteAck(_generationInFlight);
            _generationInFlight = 0;
            _nextGeneration++;

            if (_quitRequested)
            {
                _quitRequested = false;
                _stopped = true;
                // S2696: quitting is intentionally a static game field write;
                // this instance method owns the queue lifecycle.
#pragma warning disable S2696
                Game1.quit = true;
#pragma warning restore S2696
            }
        }

        private bool AllWritesComplete(out bool allSucceeded)
        {
            allSucceeded = true;
            foreach (var task in _pendingWrites)
            {
                if (!task.IsCompleted)
                {
                    allSucceeded = false;
                    return false;
                }

                if (task.Status != TaskStatus.RanToCompletion || !task.Result)
                {
                    allSucceeded = false;
                }
            }

            _pendingWrites.RemoveAll(task => task.IsCompleted);
            return true;
        }

        private void FailGeneration(string error)
        {
            if (_generationInFlight == 0)
            {
                StopWithError(_nextGeneration, error);
                return;
            }

            WriteError(_generationInFlight, error);
            _pending.Clear();
            _pendingWrites.Clear();
            _generationInFlight = 0;
            _nextGeneration++;
        }

        private void StopWithError(int generation, string error)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            WriteError(generation, error);
        }

        private void BeginNextDay()
        {
            _startNextDayPending = false;
            try
            {
                Game1.player.isInBed.Value = true;
                Game1.NewDay(0f);
            }
            catch
            {
                _advanceInProgress = false;
                FailGeneration("advance-days-failed");
            }
        }

        private bool PlantCrop(string locationName, int tileX, int tileY, string seedId)
        {
            GameLocation? location;
            try
            {
                location = Game1.getLocationFromName(locationName);
            }
            catch
            {
                return false;
            }

            if (location == null)
            {
                return false;
            }

            var tile = new Vector2(tileX, tileY);
            // GameLocation.DayUpdate removes terrain features that are not on
            // the map, so an off-map tile would lose the crop on the first
            // night and the scenario would silently measure nothing.
            if (!location.isTileOnMap(tile) || location.isWaterTile(tileX, tileY))
            {
                return false;
            }

            if (location.buildings != null && location.buildings.Any(building => building != null && building.occupiesTile(tile)))
            {
                return false;
            }

            if (location.Objects.ContainsKey(tile))
            {
                location.Objects.Remove(tile);
            }

            if (location.terrainFeatures.TryGetValue(tile, out var existing)
                && existing is HoeDirt existingDirt
                && existingDirt.crop != null)
            {
                return false;
            }

            EnsureCropData(seedId);
            var crop = new Crop(seedId, tileX, tileY, location);
            var dirt = new HoeDirt(0, location)
            {
                crop = crop,
            };
            location.terrainFeatures[tile] = dirt;
            return true;
        }

        private void EnsureCropData(string seedId)
        {
            if (seedId != LongPhaseSeedId)
            {
                return;
            }

            RegisterLongPhaseCrop();
        }

        private void RegisterLongPhaseCrop()
        {
            try
            {
                if (Game1.cropData == null)
                {
                    return;
                }

                if (Game1.cropData.ContainsKey(LongPhaseSeedId))
                {
                    return;
                }

                Game1.cropData[LongPhaseSeedId] = new CropData
                {
                    Seasons = new List<Season>
                    {
                        Season.Spring,
                        Season.Summer,
                        Season.Fall,
                        Season.Winter,
                    },
                    DaysInPhase = new List<int> { 4, 4, 4, 4 },
                    RegrowDays = -1,
                    IsRaised = false,
                    IsPaddyCrop = false,
                    NeedsWatering = true,
                    HarvestItemId = "24",
                    HarvestMinStack = 1,
                    HarvestMaxStack = 1,
                    HarvestMethod = HarvestMethod.Grab,
                    Texture = "TileSheets\\crops",
                    SpriteIndex = 0,
                    CountForMonoculture = false,
                    CountForPolyculture = false,
                };
            }
            catch
            {
                Monitor.Log("Verification crop registration failed.", LogLevel.Trace);
            }
        }

        private CropPhaseDump BuildCropPhaseDump()
        {
            var stopwatch = Stopwatch.StartNew();
            var dump = new CropPhaseDump
            {
                SchemaVersion = 1,
                DayKey = $"{Game1.currentSeason}_{Game1.dayOfMonth}",
                CapturedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                IsMainPlayer = Context.IsMainPlayer,
                Done = true,
            };

            var locations = GetDumpLocations();
            dump.LocationsProcessed = locations.Count;

            foreach (var location in locations)
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is not HoeDirt dirt || dirt.crop == null)
                    {
                        continue;
                    }

                    var crop = dirt.crop;
                    int harvestIndex = 0;
                    int.TryParse(crop.indexOfHarvest.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out harvestIndex);
                    var entry = new CropPhaseEntry
                    {
                        Location = location.Name,
                        TileX = (int)pair.Key.X,
                        TileY = (int)pair.Key.Y,
                        TileKey = $"{pair.Key.X},{pair.Key.Y}",
                        SeedId = crop.forageCrop.Value ? crop.whichForageCrop.Value : crop.netSeedIndex.Value,
                        IsForageCrop = crop.forageCrop.Value,
                        IndexOfHarvest = harvestIndex,
                        CurrentPhase = crop.currentPhase.Value,
                        DayOfCurrentPhase = crop.dayOfCurrentPhase.Value,
                        PhaseDays = new List<int>(crop.phaseDays),
                        BasePhaseDays = GetBasePhaseDays(crop),
                        FullyGrown = crop.fullyGrown.Value,
                        Dead = crop.dead.Value,
                        IsSentinelPhase = crop.currentPhase.Value == -1,
                    };

                    var api = _api;
                    if (api != null && api.TryGetSoilHealth(location.Name, entry.TileX, entry.TileY, out float health))
                    {
                        if (float.IsNaN(health))
                        {
                            entry.SoilHealth = null;
                            entry.SoilHealthState = "nan";
                        }
                        else
                        {
                            entry.SoilHealth = health;
                            entry.SoilHealthState = "measured";
                        }
                    }
                    else
                    {
                        entry.SoilHealth = null;
                        entry.SoilHealthState = "missing";
                    }

                    entry.HealthTier = HealthTierClassifier.Classify(entry.SoilHealth, entry.SoilHealthState);
                    dump.Crops.Add(entry);
                }
            }

            stopwatch.Stop();
            dump.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return dump;
        }

        private static List<int> GetBasePhaseDays(Crop crop)
        {
            try
            {
                var data = crop.GetData();
                if (data?.DaysInPhase != null)
                {
                    return new List<int>(data.DaysInPhase);
                }
            }
            catch
            {
                // Fall through to an empty baseline: the dump stays honest
                // about the missing data instead of fabricating a value.
            }

            return new List<int>();
        }

        private static List<GameLocation> GetDumpLocations()
        {
            var locations = new List<GameLocation>();

            void Add(GameLocation? location)
            {
                if (location == null)
                {
                    return;
                }

                if (locations.Any(existing => existing.Name == location.Name))
                {
                    return;
                }

                locations.Add(location);
            }

            Add(Game1.currentLocation);
            try
            {
                Add(Game1.getLocationFromName("Greenhouse"));
            }
            catch
            {
                // The location does not exist in this save.
            }

            try
            {
                Add(Game1.getLocationFromName("Farm"));
            }
            catch
            {
                // The location does not exist in this save.
            }

            return locations;
        }

        private Task<bool> WriteSoilHealthDumpAsync(string label)
        {
            var dumpDir = _dumpDir!;
            IReadOnlyList<SoilHealthSnapshotEntry> snapshot = _api != null
                ? _api.SnapshotSoilHealth()
                : Array.Empty<SoilHealthSnapshotEntry>();
            return Task.Run(() =>
            {
                try
                {
                    var map = new SortedDictionary<string, Dictionary<string, double?>>(StringComparer.Ordinal);
                    foreach (var entry in snapshot)
                    {
                        if (!map.TryGetValue(entry.LocationName, out var tiles))
                        {
                            tiles = new Dictionary<string, double?>(StringComparer.Ordinal);
                            map[entry.LocationName] = tiles;
                        }

                        double? value = float.IsNaN(entry.Health) ? null : entry.Health;
                        tiles[$"{entry.TileX},{entry.TileY}"] = value;
                    }

                    var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                    var path = Path.Combine(dumpDir, $"soil_health_{label}.json");
                    var tmp = path + ".tmp";
                    File.WriteAllText(tmp, json);
                    File.Move(tmp, path, overwrite: true);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private void WriteAck(int generation)
        {
            WriteJsonAtomic(Path.Combine(_injectionDir!, $"ack-{generation}.json"), new
            {
                ok = true,
                generation,
            });
        }

        private void WriteError(int generation, string error)
        {
            WriteJsonAtomic(Path.Combine(_injectionDir!, $"error-{generation}.json"), new
            {
                ok = false,
                generation,
                error,
            });
        }

        private static void WriteJsonAtomic(string path, object payload)
        {
            try
            {
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(tmp, path, overwrite: true);
            }
            catch
            {
                // The runner observes the missing acknowledgement and fails
                // closed; a logging failure here must not crash the game.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stopped = true;
                Helper.Events.GameLoop.GameLaunched -= OnGameLaunched;
                Helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;
                Helper.Events.GameLoop.DayStarted -= OnDayStarted;
                Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            }

            base.Dispose(disposing);
        }
    }
}
