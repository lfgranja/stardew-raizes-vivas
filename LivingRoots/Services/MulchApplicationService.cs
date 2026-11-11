using System;
using System.Collections.Generic;
using System.Linq;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Models;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services;

public class MulchApplicationService(
    IModDataService modDataService,
    IPlayerProvider playerProvider,
    IMonitor monitor) : IMulchApplicationService
{
    private const string CancelSound = "cancel";
    private readonly IModDataService _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
    private readonly IPlayerProvider _playerProvider = playerProvider ?? throw new ArgumentNullException(nameof(playerProvider));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

    // Runtime cache: Dictionary<LocationName, Dictionary<Tile, MulchTileState>>
    private readonly Dictionary<string, Dictionary<Point, MulchTileEntry>> _runtimeCache = new();
    private readonly object _lock = new();
    private volatile bool _loadAbortedForLimits;
    private int _mulchVersion;

    public void LoadData(string saveId)
    {
        _loadAbortedForLimits = false;
        ClearRuntimeCache();

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("LoadData aborted: invalid saveId.", LogLevel.Warn);
            _loadAbortedForLimits = true;
            return;
        }

        var savedData = TryLoadMulchState(saveId);
        if (savedData == null)
        {
            return;
        }

        PopulateRuntimeCache(savedData);
    }

    private void ClearRuntimeCache()
    {
        lock (_lock)
        {
            _runtimeCache.Clear();
        }
    }

    private MulchState? TryLoadMulchState(string saveId)
    {
        var dataKey = $"mulch_state_{saveId}";
        try
        {
            return _modDataService.LoadData<MulchState>(dataKey);
        }
        catch (Exception ex)
        {
            _monitor.Log($"LoadData exception: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            return null;
        }
    }

    private void PopulateRuntimeCache(MulchState savedData)
    {
        lock (_lock)
        {
            foreach (var locEntry in savedData.LocationMulchData)
            {
                var tileDict = BuildTileDict(locEntry.Value);
                if (tileDict.Count > 0)
                {
                    _runtimeCache[locEntry.Key] = tileDict;
                }
            }
        }
    }

    private static bool TryParseTileKey(string key, out Point point)
    {
        point = default;
        var parts = key.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var x))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var y))
        {
            return false;
        }

        point = new Point(x, y);
        return true;
    }

    private static Dictionary<Point, MulchTileEntry> BuildTileDict(Dictionary<string, MulchTileEntry> savedTileDict)
    {
        var tileDict = new Dictionary<Point, MulchTileEntry>();
        foreach (var tileEntry in savedTileDict)
        {
            if (!TryParseTileKey(tileEntry.Key, out var point))
            {
                continue;
            }

            if (tileEntry.Value.HasMulch)
            {
                tileDict[point] = tileEntry.Value;
            }
        }
        return tileDict;
    }

    public void SaveData(string saveId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("SaveData aborted: invalid saveId.", LogLevel.Warn);
            return;
        }

        if (_loadAbortedForLimits)
        {
            _monitor.Log("SaveData aborted: load was aborted for limits.", LogLevel.Warn);
            return;
        }

        var dataKey = $"mulch_state_{saveId}";
        var state = new MulchState();

        lock (_lock)
        {
            foreach (var locEntry in _runtimeCache)
            {
                var tileDict = new Dictionary<string, MulchTileEntry>();
                foreach (var tileEntry in locEntry.Value)
                {
                    if (tileEntry.Value.HasMulch)
                    {
                        tileDict[$"{tileEntry.Key.X},{tileEntry.Key.Y}"] = new MulchTileEntry
                        {
                            HasMulch = tileEntry.Value.HasMulch,
                            DaysRemaining = tileEntry.Value.DaysRemaining
                        };
                    }
                }
                if (tileDict.Count > 0)
                {
                    state.LocationMulchData[locEntry.Key] = tileDict;
                }
            }
        }

        try
        {
            _modDataService.SaveData(state, dataKey);
        }
        catch (Exception ex)
        {
            _monitor.Log($"SaveData exception: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
        }
    }

    public int MulchVersion
    {
        get { lock (_lock) { return _mulchVersion; } }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _runtimeCache.Clear();
            _mulchVersion = 0;
        }
        _loadAbortedForLimits = false;
    }

    private bool TryGetMulchTileEntryNoLock(string locationName, Vector2 tile, out MulchTileEntry? state)
    {
        if (_runtimeCache.TryGetValue(locationName, out var tileDict))
        {
            var point = new Point((int)tile.X, (int)tile.Y);
            if (tileDict.TryGetValue(point, out state))
            {
                return true;
            }
        }
        state = null;
        return false;
    }

    private bool TryGetMulchTileEntry(string locationName, Vector2 tile, out MulchTileEntry? state)
    {
        lock (_lock)
        {
            return TryGetMulchTileEntryNoLock(locationName, tile, out state);
        }
    }

    public bool IsMulched(string locationName, Vector2 tile)
    {
        return TryGetMulchTileEntry(locationName, tile, out var state) && state != null && state.HasMulch;
    }

    public void TickMulch(string locationName, Vector2 tile)
    {
        lock (_lock)
        {
            if (TryGetMulchTileEntryNoLock(locationName, tile, out var state) && state != null && state.HasMulch)
            {
                state.DaysRemaining--;
                _mulchVersion++;
                if (state.DaysRemaining <= 0)
                {
                    state.HasMulch = false;
                    state.DaysRemaining = 0;
                    if (_runtimeCache.TryGetValue(locationName, out var tileDict))
                    {
                        tileDict.Remove(new Point((int)tile.X, (int)tile.Y));
                    }
                }
            }
        }
    }

    public bool TryApplyMulch(Farmer who, GameLocation location, Vector2 tile, out string messageKey)
    {
        messageKey = string.Empty;
        var locationName = location.Name ?? "Unknown";

        if (!location.IsFarm && locationName != "Greenhouse")
        {
            _monitor.Log($"Mulch rejected: {locationName} is not farm or Greenhouse.", LogLevel.Trace);
            Game1.playSound(CancelSound);
            messageKey = "message.mulch.invalid-tile";
            return false;
        }

        if (!location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
            || terrainFeature is not HoeDirt)
        {
            _monitor.Log("Mulch rejected: target tile is not tilled soil.", LogLevel.Trace);
            Game1.playSound(CancelSound);
            messageKey = "message.mulch.invalid-tile";
            return false;
        }

        // Already mulched?
        if (IsMulched(locationName, tile))
        {
            _monitor.Log("Mulch rejected: tile already mulched.", LogLevel.Trace);
            Game1.playSound(CancelSound);
            messageKey = "message.mulch.already";
            return false;
        }

        // Held item check
        var heldItem = _playerProvider.CurrentItem;
        if (heldItem == null ||
            (heldItem.QualifiedItemId != ModConstants.MulchItemId &&
             heldItem.QualifiedItemId != ModConstants.QualifiedMulchItemId &&
             heldItem.ItemId != ModConstants.MulchItemId))
        {
            _monitor.Log("Mulch rejected: player not holding mulch.", LogLevel.Trace);
            Game1.playSound(CancelSound);
            return false;
        }

        // Apply mulch
        lock (_lock)
        {
            if (!_runtimeCache.TryGetValue(locationName, out var tileDict))
            {
                tileDict = new Dictionary<Point, MulchTileEntry>();
                _runtimeCache[locationName] = tileDict;
            }
            tileDict[new Point((int)tile.X, (int)tile.Y)] = new MulchTileEntry
            {
                HasMulch = true,
                DaysRemaining = ModConstants.MulchDurationDays
            };
            _mulchVersion++;
        }

        heldItem.Stack--;

        var sprite = FeedbackSpriteFactory.CreateTileSprite(tile, "Mulch", ModConstants.MulchColor);
        location.TemporarySprites.Add(sprite);

        Game1.playSound("hoeHit");

        _monitor.Log($"Mulch applied at ({tile.X}, {tile.Y}) in {locationName}: {ModConstants.MulchDurationDays} days", LogLevel.Trace);
        return true;
    }

    // TranslateRefusalMessage removed — unused (refusal messages use raw keys).
}
