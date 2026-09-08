using System.Collections.Generic;
using LivingRoots.Domain;
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
    CompostingBinFactory factory) : ICompostingBinService
{
    private readonly IModDataService _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
    private readonly ISaveIdProvider _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));
    private readonly IOrganicWasteValidator _organicWasteValidator = organicWasteValidator ?? throw new ArgumentNullException(nameof(organicWasteValidator));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly CompostingBinFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    private readonly Dictionary<string, Dictionary<string, CompostingBinStateModel>> _runtimeCache = new();
    private readonly object _lock = new();

    public void AddWaste(string locationName, Vector2 tile, Item item)
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

            if (bin.State != CompostingBinState.Empty) return;
            if (!_organicWasteValidator.IsValidOrganicWaste(item)) return;

            bin.State = CompostingBinState.Processing;
            bin.InputItemId = item.QualifiedItemId;
            bin.InputTimestamp = _timeProvider.TotalDays;
            bin.ConsecutiveIdleDays = 0;

            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Empty → Processing, item: {item.QualifiedItemId}",
                LogLevel.Trace);
        }
    }

    public int CollectCompost(string locationName, Vector2 tile, Farmer player)
    {
        var key = GetTileKey(tile);
        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
                return 0;

            if (bin.State != CompostingBinState.Ready) return 0;

            int outputCount = bin.MaturationLevel;

            for (int i = 0; i < outputCount; i++)
            {
                var compost = new StardewValley.Object(ModConstants.CompostItemId, 1);
                player.addItemToInventoryBool(compost);
            }

            bin.State = CompostingBinState.Empty;
            bin.InputItemId = null;
            bin.InputTimestamp = null;
            bin.ConsecutiveIdleDays = 0;
            bin.ConsecutiveActiveDays = 0;

            if (bin.MaturationLevel < ModConstants.MaturationMaxLevel)
            {
                bin.MaturationLevel++;
            }

            Game1.playSound("Ship");
            _monitor.Log($"Bin at ({tile.X}, {tile.Y}): Ready → Empty, produced {outputCount} compost",
                LogLevel.Trace);
            return outputCount;
        }
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
        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName)) return;

            foreach (var bin in _runtimeCache[locationName].Values)
            {
                if (bin.State == CompostingBinState.Processing)
                {
                    if (bin.InputTimestamp.HasValue)
                    {
                        var currentDay = _timeProvider.TotalDays;
                        var recordedDay = bin.InputTimestamp.Value;
                        if (currentDay - recordedDay >= ModConstants.MaturationDays)
                        {
                            bin.State = CompostingBinState.Ready;
                            Game1.playSound("Ship");
                            _monitor.Log($"Bin at ({bin.TileX}, {bin.TileY}): Processing → Ready",
                                LogLevel.Trace);
                        }
                    }

                    bin.ConsecutiveActiveDays++;
                    if (bin.ConsecutiveActiveDays >= ModConstants.MaturationIncrementDays &&
                        bin.MaturationLevel < ModConstants.MaturationMaxLevel)
                    {
                        bin.MaturationLevel++;
                        bin.ConsecutiveActiveDays = 0;
                        _monitor.Log($"Bin at ({bin.TileX}, {bin.TileY}): Maturation level increased to {bin.MaturationLevel}",
                            LogLevel.Trace);
                    }
                }
                else if (bin.State == CompostingBinState.Empty)
                {
                    bin.ConsecutiveIdleDays++;
                    if (bin.ConsecutiveIdleDays >= ModConstants.MaturationIdleResetDays)
                    {
                        bin.MaturationLevel = 1;
                        bin.ConsecutiveIdleDays = 0;
                        _monitor.Log($"Bin at ({bin.TileX}, {bin.TileY}): Maturation reset to 1 after {ModConstants.MaturationIdleResetDays} idle days",
                            LogLevel.Trace);
                    }
                }
            }
        }
    }

    public void OnObjectRemoved(string locationName, Vector2 tile)
    {
        var key = GetTileKey(tile);
        lock (_lock)
        {
            if (!_runtimeCache.ContainsKey(locationName) ||
                !_runtimeCache[locationName].TryGetValue(key, out var bin))
                return;

            _runtimeCache[locationName].Remove(key);
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
                _monitor.Log($"No composting bin data found for save '{saveId}'.", LogLevel.Trace);
                return;
            }

            lock (_lock)
            {
                _runtimeCache.Clear();
                foreach (var loc in data.LocationBinData)
                {
                    _runtimeCache[loc.Key] = new Dictionary<string, CompostingBinStateModel>();
                    foreach (var bin in loc.Value)
                    {
                        var coords = ParseTileKey(bin.Key);
                        var state = new CompostingBinStateModel
                        {
                            TileX = coords.X,
                            TileY = coords.Y,
                            State = Enum.Parse<CompostingBinState>(bin.Value.State),
                            InputItemId = bin.Value.InputItemId,
                            InputTimestamp = bin.Value.InputTimestamp,
                            MaturationLevel = Math.Clamp(bin.Value.MaturationLevel, 1, ModConstants.MaturationMaxLevel),
                            ConsecutiveIdleDays = Math.Clamp(bin.Value.ConsecutiveIdleDays, 0, ModConstants.MaturationIdleResetDays),
                            ConsecutiveActiveDays = Math.Clamp(bin.Value.ConsecutiveActiveDays, 0, ModConstants.MaturationIncrementDays)
                        };
                        _runtimeCache[loc.Key][bin.Key] = state;
                    }
                }
            }
            _monitor.Log($"Loaded composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to load composting bin data: {ex.GetType().Name}", LogLevel.Warn);
        }
    }

    public void SaveData(string saveId)
    {
        var key = ModConstants.CompostingBinKeyPrefix + saveId;
        try
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
                            bin.Value.ConsecutiveIdleDays == 0 &&
                            bin.Value.ConsecutiveActiveDays == 0)
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
                _modDataService.SaveData(data, key);
            }
            _monitor.Log($"Saved composting bin data for save '{saveId}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to save composting bin data: {ex.GetType().Name}", LogLevel.Warn);
        }
    }

    private static string GetTileKey(Vector2 tile) => $"{(int)tile.X},{(int)tile.Y}";

    private static Point ParseTileKey(string key)
    {
        var parts = key.Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
