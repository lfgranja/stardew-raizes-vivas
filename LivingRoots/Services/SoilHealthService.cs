using System;
using System.Collections.Generic;
using System.Globalization;
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
        private Dictionary<string, Dictionary<Vector2, float>> _runtimeCache = new();
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
                        var tileDict = new Dictionary<Vector2, float>();
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

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100
                float clampedValue = Math.Clamp(value, 0f, 100f);

                if (!_runtimeCache.ContainsKey(locationName))
                {
                    _runtimeCache[locationName] = new Dictionary<Vector2, float>();
                }
                _runtimeCache[locationName][tile] = clampedValue;
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            lock (_lock)
            {
                float current = GetSoilHealth(locationName, tile);
                SetSoilHealth(locationName, tile, current + delta);
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