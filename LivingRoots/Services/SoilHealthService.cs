using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    public class SoilHealthService : ISoilHealthService
    {
        private readonly IModDataService _modDataService;
        private readonly IMonitor _monitor;
        
        // Runtime cache using Vector2 directly as key for better performance
        // Dictionary<LocationName, Dictionary<TileCoordinates, HealthValue>>
        private readonly Dictionary<string, Dictionary<Vector2, float>> _runtimeCache = new(); // Tornar readonly
        private const string KeyPrefix = "soil_health_data_";
        
        // Lock object for thread safety
        private readonly object _lock = new object();

        public SoilHealthService(IModDataService modDataService, IMonitor monitor)
        {
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public void LoadData(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("LoadData aborted: invalid saveId.", LogLevel.Warn);
                return;
            }
            
            lock (_lock)
            {
                string dataKey = GetSaveKey(saveId);
                var savedData = _modDataService.LoadData<SoilHealthState>(dataKey);

                // Convert from disk format (string keys) to runtime format (Vector2 keys)
                _runtimeCache.Clear();
                if (savedData != null)
                {
                    foreach (var locationEntry in savedData.LocationHealthData)
                    {
                        // Skip if the value is null to prevent NullReferenceException
                        if (locationEntry.Value == null) continue;
                        
                        var tileDict = new Dictionary<Vector2, float>();
                        bool warnedForLocation = false; // Only warn once per location
                        foreach (var tileEntry in locationEntry.Value)
                        {
                            // Parse "X,Y" string back to Vector2
                            string[] parts = tileEntry.Key.Split(',');
                            if (parts.Length == 2 && 
                                float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                            {
                                tileDict[new Vector2(x, y)] = tileEntry.Value;
                            }
                            else
                            {
                                // Warn about malformed keys to help diagnose corrupted save data
                                if (!warnedForLocation)
                                {
                                    _monitor.Log($"Skipped malformed soil health tile key(s) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                    warnedForLocation = true;
                                }
                            }
                        }
                        _runtimeCache[locationEntry.Key] = tileDict;
                    }
                }
                else
                {
                    _monitor.Log($"No existing Soil Health data found for save {saveId}. Starting fresh.", LogLevel.Info);
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
            
            lock (_lock)
            {
                // Convert from runtime format (Vector2 keys) to disk format (string keys)
                var stateToSave = new SoilHealthState();
                foreach (var locationEntry in _runtimeCache)
                {
                    var stringDict = new Dictionary<string, float>();
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        string key = $"{tileEntry.Key.X.ToString(CultureInfo.InvariantCulture)},{tileEntry.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        stringDict[key] = tileEntry.Value;
                    }
                    stateToSave.LocationHealthData[locationEntry.Key] = stringDict;
                }

                string saveKey = GetSaveKey(saveId);
                _modDataService.SaveData(stateToSave, saveKey);
                _monitor.Log($"Soil Health data saved for {saveId}", LogLevel.Trace);
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            if (string.IsNullOrWhiteSpace(locationName)) 
                return 0f; // Return default (Poor Soil) if location is invalid

            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    // Direct lookup using Vector2 key - no string allocation or parsing
                    if (tiles.TryGetValue(tile, out float health))
                    {
                        return health;
                    }
                }
                return 0f; // Return default (Poor Soil) if no data exists
            }
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            if (string.IsNullOrWhiteSpace(locationName)) 
                return; // Skip if location is invalid
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100
                float clampedValue = Math.Clamp(value, 0f, 100f);

                // Use TryGetValue to perform a single lookup instead of ContainsKey + indexer
                if (!_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    tiles = new Dictionary<Vector2, float>();
                    _runtimeCache[locationName] = tiles;
                }
                tiles[tile] = clampedValue;
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            if (string.IsNullOrWhiteSpace(locationName)) 
                return; // Skip if location is invalid
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            lock (_lock)
            {
                // Perform the update operation in a single lock to avoid reentrant calls
                if (!_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    tiles = new Dictionary<Vector2, float>();
                    _runtimeCache[locationName] = tiles;
                }

                // Get current value (0 if tile doesn't exist) and calculate new value
                tiles.TryGetValue(tile, out float currentHealth);
                float newHealth = Math.Clamp(currentHealth + delta, 0f, 100f);
                tiles[tile] = newHealth;
            }
        }

        private string GetSaveKey(string saveId)
        {
            // Basic key sanitization is handled by ModDataService, 
            // but we ensure the save ID is part of the key to separate files.
            return $"{KeyPrefix}{saveId}";
        }
    }
}