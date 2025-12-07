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
        
        // Runtime cache using Point directly as key for better performance and precision
        // Dictionary<LocationName, Dictionary<TileCoordinates, HealthValue>>
        private readonly Dictionary<string, Dictionary<Point, float>> _runtimeCache = new();
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
            lock (_lock)
            {
                // Clear the cache if saveId is invalid to prevent stale data from persisting across different game saves
                if (string.IsNullOrWhiteSpace(saveId))
                {
                    _runtimeCache.Clear(); // ensure no stale state remains
                    _monitor.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn);
                    return;
                }
                
                string dataKey = GetSaveKey(saveId);
                
                try
                {
                    var savedData = _modDataService.LoadData<SoilHealthState>(dataKey);

                    // Convert from disk format (string keys) to runtime format (Point keys)
                    _runtimeCache.Clear();
                    if (savedData != null)
                    {
                        foreach (var locationEntry in savedData.LocationHealthData)
                        {
                            // Skip if the location name is null or empty to prevent invalid entries in the cache
                            if (string.IsNullOrWhiteSpace(locationEntry.Key))
                            {
                                _monitor.Log("Skipped soil health data with null or empty location name.", LogLevel.Warn);
                                continue;
                            }
                            
                            // Skip if the value is null to prevent NullReferenceException
                            if (locationEntry.Value == null) continue;
                            
                            var tileDict = new Dictionary<Point, float>();
                            bool warnedForLocation = false; // Only warn once per location
                            foreach (var tileEntry in locationEntry.Value)
                            {
                                // Parse "X,Y" string back to Point (using integers for tile coordinates)
                                // Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
                                ReadOnlySpan<char> keySpan = tileEntry.Key;
                                int commaIndex = keySpan.IndexOf(',');
                                if (commaIndex > 0 && commaIndex < keySpan.Length - 1 &&
                                    int.TryParse(keySpan.Slice(0, commaIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
                                    int.TryParse(keySpan.Slice(commaIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                                {
                                    // Validate the loaded value by checking for NaN/Infinity and clamping to [0, 100] range
                                    var rawValue = tileEntry.Value;
                                    if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
                                    {
                                        if (!warnedForLocation)
                                        {
                                            _monitor.Log($"Skipped invalid soil health value (NaN/Infinity) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                            warnedForLocation = true;
                                        }
                                        continue;
                                    }
                                    
                                    float clamped = Math.Clamp(rawValue, 0f, 100f);
                                    tileDict[new Point(x, y)] = clamped;
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
                            // Only add location if at least one valid tile exists
                            if (tileDict.Count > 0)
                            {
                                _runtimeCache[locationEntry.Key] = tileDict;
                            }
                        }
                    }
                    else
                    {
                        _monitor.Log("No existing Soil Health data found. Starting fresh.", LogLevel.Info);
                    }
                }
                catch (Exception)
                {
                    _monitor.Log("Error occurred while loading soil health data.", LogLevel.Error);
                    // Keep cache cleared; don't throw to avoid breaking game load
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
                // Validate and convert from runtime format (Point keys) to disk format (string keys)
                var stateToSave = new SoilHealthState();
                foreach (var locationEntry in _runtimeCache)
                {
                    // Skip invalid location names to prevent corrupt entries
                    if (string.IsNullOrWhiteSpace(locationEntry.Key))
                    {
                        _monitor.Log("Skipped saving soil health for null or empty location name.", LogLevel.Warn);
                        continue;
                    }
                    
                    var stringDict = new Dictionary<string, float>();
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        var val = tileEntry.Value;
                        if (float.IsNaN(val) || float.IsInfinity(val))
                        {
                            _monitor.Log("Skipped saving soil health value (NaN/Infinity) for a tile due to invalid state.", LogLevel.Warn);
                            continue;
                        }
                        
                        float clamped = Math.Clamp(val, 0f, 100f);
                        string key = $"{tileEntry.Key.X.ToString(CultureInfo.InvariantCulture)},{tileEntry.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        stringDict[key] = clamped;
                    }
                    // Only add location if it has valid tiles
                    if (stringDict.Count > 0)
                    {
                        stateToSave.LocationHealthData[locationEntry.Key] = stringDict;
                    }
                }

                string saveKey = GetSaveKey(saveId);
                
                try
                {
                    _modDataService.SaveData(stateToSave, saveKey);
                    _monitor.Log("Soil Health data saved successfully.", LogLevel.Trace);
                }
                catch (Exception)
                {
                    _monitor.Log("Error occurred while persisting soil health data.", LogLevel.Error);
                    // Intentionally do not rethrow; keep runtime cache intact so the game can continue.
                }
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Validate input to prevent potential exceptions
            if (string.IsNullOrWhiteSpace(locationName)) 
                return 0f; // Return default (Poor Soil) if location is invalid

            // Guard against invalid coordinates to prevent misleading lookups
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
                return 0f;

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)MathF.Floor(tile.X);
            int iy = (int)MathF.Floor(tile.Y);

            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    var key = new Point(ix, iy);
                    
                    if (tiles.TryGetValue(key, out float health))
                    {
                        return health;
                    }
                }
                return 0f; // Return default (Poor Soil) if no data exists
            }
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName)) 
                return; // Skip if location is invalid
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)MathF.Floor(tile.X);
            int iy = (int)MathF.Floor(tile.Y);

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100
                float clampedValue = Math.Clamp(value, 0f, 100f);

                // Use GetOrAddLocationCache to avoid code duplication
                var tiles = GetOrAddLocationCache(locationName);
                
                var key = new Point(ix, iy);
                tiles[key] = clampedValue;
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName)) 
                return; // Skip if location is invalid
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)MathF.Floor(tile.X);
            int iy = (int)MathF.Floor(tile.Y);

            lock (_lock)
            {
                // Perform the update operation in a single lock to avoid reentrant calls
                var tiles = GetOrAddLocationCache(locationName);

                // Convert Vector2 to Point for lookup (using integer coordinates)
                var key = new Point(ix, iy);
                
                // Get current value (0 if tile doesn't exist) and calculate new value
                tiles.TryGetValue(key, out float currentHealth);
                float newHealth = Math.Clamp(currentHealth + delta, 0f, 100f);
                tiles[key] = newHealth;
            }
        }

        /// <summary>
        /// Gets or creates the tile dictionary for a given location.
        /// This method reduces code duplication between SetSoilHealth and UpdateHealth methods.
        /// </summary>
        /// <param name="locationName">The name of the location</param>
        /// <returns>The tile dictionary for the location</returns>
        private Dictionary<Point, float> GetOrAddLocationCache(string locationName)
        {
            if (!_runtimeCache.TryGetValue(locationName, out var tiles))
            {
                tiles = new Dictionary<Point, float>();
                _runtimeCache[locationName] = tiles;
            }
            return tiles;
        }

        private string GetSaveKey(string saveId)
        {
            // Basic key sanitization is handled by ModDataService, 
            // but we ensure the save ID is part of the key to separate files.
            return $"{KeyPrefix}{saveId}";
        }
    }
}