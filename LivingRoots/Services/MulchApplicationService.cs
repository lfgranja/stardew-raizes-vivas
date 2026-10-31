using System;
using System.Collections.Generic;
using System.Linq;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Models;
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
        lock (_lock)
        {
            _runtimeCache.Clear();
        }

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("LoadData aborted: invalid saveId.", LogLevel.Warn);
            _loadAbortedForLimits = true;
            return;
        }

        var dataKey = $"mulch_state_{saveId}";
        MulchState? savedData;
        try
        {
            savedData = _modDataService.LoadData<MulchState>(dataKey);
        }
        catch (Exception ex)
        {
            _monitor.Log($"LoadData exception: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            savedData = null;
        }

        if (savedData == null)
        {
            return;
        }

        lock (_lock)
        {
            foreach (var locEntry in savedData.LocationMulchData)
            {
                var tileDict = new Dictionary<Point, MulchTileEntry>();
                foreach (var tileEntry in locEntry.Value)
                {
                    var parts = tileEntry.Key.Split(',');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out var x) &&
                        int.TryParse(parts[1], out var y))
                    {
                        var state = tileEntry.Value;
                        if (state.HasMulch) // sparse: only store non-default
                        {
                            tileDict[new Point(x, y)] = state;
                        }
                    }
                }
                if (tileDict.Count > 0)
                {
                    _runtimeCache[locEntry.Key] = tileDict;
                }
            }
        }
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

    public bool IsMulched(string locationName, Vector2 tile)
    {
        lock (_lock)
        {
            if (_runtimeCache.TryGetValue(locationName, out var tileDict))
            {
                var point = new Point((int)tile.X, (int)tile.Y);
                if (tileDict.TryGetValue(point, out var state))
                {
                    return state.HasMulch;
                }
            }
            return false;
        }
    }

    public void TickMulch(string locationName, Vector2 tile)
    {
        lock (_lock)
        {
            if (_runtimeCache.TryGetValue(locationName, out var tileDict))
            {
                var point = new Point((int)tile.X, (int)tile.Y);
                if (tileDict.TryGetValue(point, out var state) && state.HasMulch)
                {
                    state.DaysRemaining--;
                    _mulchVersion++;
                    if (state.DaysRemaining <= 0)
                    {
                        state.HasMulch = false;
                        state.DaysRemaining = 0;
                        tileDict.Remove(point);
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
            Game1.playSound("cancel");
            messageKey = "message.mulch.invalid-tile";
            return false;
        }

        if (!location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
            || terrainFeature is not HoeDirt)
        {
            _monitor.Log("Mulch rejected: target tile is not tilled soil.", LogLevel.Trace);
            Game1.playSound("cancel");
            messageKey = "message.mulch.invalid-tile";
            return false;
        }

        // Already mulched?
        if (IsMulched(locationName, tile))
        {
            _monitor.Log("Mulch rejected: tile already mulched.", LogLevel.Trace);
            Game1.playSound("cancel");
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
            Game1.playSound("cancel");
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

        // Visual feedback
        var worldPosition = new Vector2(tile.X * (float)ModConstants.TileSize + 32f, tile.Y * (float)ModConstants.TileSize);
        var sprite = new TemporaryAnimatedSprite(
            null,
            Rectangle.Empty,
            1000f,
            1,
            1,
            worldPosition,
            false,
            false);
        sprite.text = "Mulch";
        sprite.color = ModConstants.MulchColor;
        sprite.alphaFade = 0.02f;
        sprite.motion = new Vector2(0, -0.5f);
        location.TemporarySprites.Add(sprite);

        Game1.playSound("hoeHit");

        _monitor.Log($"Mulch applied at ({tile.X}, {tile.Y}) in {locationName}: {ModConstants.MulchDurationDays} days", LogLevel.Trace);
        return true;
    }

    // TranslateRefusalMessage removed — unused (refusal messages use raw keys).
}
