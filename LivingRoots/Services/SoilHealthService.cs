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
                    _monitor.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn);
                    _runtimeCache.Clear(); // ensure no stale state remains
                    return;
                }
                
                string dataKey = GetSaveKey(saveId);
                
                try
                {
                    var savedData = _modDataService.LoadData<SoilHealthState>(dataKey);

                    // Use temporary cache to prevent data loss if parsing fails partway through
                    var tempCache = new Dictionary<string, Dictionary<Point, float>>();
                    
                    if (savedData != null)
                    {
                        // Guard against null LocationHealthData to prevent NullReferenceException during deserialization
                        var locations = savedData.LocationHealthData ?? new Dictionary<string, Dictionary<string, float>>();
                        
                        foreach (var locationEntry in locations)
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
                            bool warnedForInvalidValue = false; // Only warn once per location for invalid values
                            bool warnedForMalformedKey = false; // Only warn once per location for malformed keys
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
                                        if (!warnedForInvalidValue)
                                        {
                                            _monitor.Log($"Skipped invalid soil health value (NaN/Infinity) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                            warnedForInvalidValue = true;
                                        }
                                        continue;
                                    }
                                    
                                    float clamped = Math.Clamp(rawValue, 0f, 100f);
                                    // Resolve duplicates deterministically: last-write-wins
                                    var point = new Point(x, y);
                                    tileDict[point] = clamped;
                                }
                                else
                                {
                                    // Warn about malformed keys to help diagnose corrupted save data
                                    if (!warnedForMalformedKey)
                                    {
                                        _monitor.Log($"Skipped malformed soil health tile key(s) in location '{locationEntry.Key}'.", LogLevel.Warn);
                                        warnedForMalformedKey = true;
                                    }
                                }
                            }
                            // Only add location if at least one valid tile exists
                            if (tileDict.Count > 0)
                            {
                                tempCache[locationEntry.Key] = tileDict;
                            }
                        }
                    }
                    else
                    {
                        _monitor.Log("No existing Soil Health data found. Starting fresh.", LogLevel.Info);
                    }
                    
                    // Swap caches only after successful parsing/validation
                    _runtimeCache.Clear();
                    foreach (var kv in tempCache)
                    {
                        _runtimeCache[kv.Key] = kv.Value;
                    }
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Error occurred while loading soil health data: {ex.Message}. Cache preserved.", LogLevel.Error);
                    // Keep existing cache; don't clear it on error to prevent data loss
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
            
            // Create snapshot of data to write outside the lock for better performance
            SoilHealthState snapshotState;
            List<string> postLockWarnings = new List<string>(); // List to store deferred logs
            List<bool> isWarning = new List<bool>(); // Parallel list to know if it's Warn or Trace

            lock (_lock)
            {
                // Validate and convert from runtime format (Point keys) to disk format (string keys)
                var stateToSave = new SoilHealthState();
                foreach (var locationEntry in _runtimeCache)
                {
                    // Skip invalid location names to prevent corrupt entries
                    if (string.IsNullOrWhiteSpace(locationEntry.Key))
                    {
                        // Defer logging until after lock
                        postLockWarnings.Add("Skipped saving soil health for null or empty location name.");
                        isWarning.Add(true); // true = LogLevel.Warn
                        continue;
                    }
                    
                    var stringDict = new Dictionary<string, float>(locationEntry.Value.Count);
                    int invalidCount = 0; // Count invalid entries to aggregate warnings
                    
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        var val = tileEntry.Value;
                        if (float.IsNaN(val) || float.IsInfinity(val))
                        {
                            invalidCount++;
                            continue;
                        }
                        
                        float clamped = Math.Clamp(val, 0f, 100f);
                        string key = $"{tileEntry.Key.X.ToString(CultureInfo.InvariantCulture)},{tileEntry.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        stringDict[key] = clamped;
                    }
                    
                    if (invalidCount > 0)
                    {
                        postLockWarnings.Add($"Skipped {invalidCount} invalid soil health entr(ies) in location '{locationEntry.Key}' during save.");
                        isWarning.Add(true); // true = LogLevel.Warn
                    }
                    
                    // Only add location if it has valid tiles
                    if (stringDict.Count > 0)
                    {
                        stateToSave.LocationHealthData[locationEntry.Key] = stringDict;
                    }
                }

                // Prevent saving empty data which could overwrite existing data
                if (stateToSave.LocationHealthData.Count == 0)
                {
                    // Defer this trace log too
                    postLockWarnings.Add("No valid soil health data to save; skipping persistence.");
                    isWarning.Add(false); // false = LogLevel.Trace
                    snapshotState = null!;
                }
                else
                {
                    // Capture snapshot to write outside the lock
                    snapshotState = stateToSave;
                }
            }

            // Emit deferred logs after releasing the lock
            for (int i = 0; i < postLockWarnings.Count; i++)
            {
                _monitor.Log(postLockWarnings[i], isWarning[i] ? LogLevel.Warn : LogLevel.Trace);
            }

            if (snapshotState == null) return;

            string saveKey = GetSaveKey(saveId);
            
            try
            {
                _modDataService.SaveData(snapshotState, saveKey);
                _monitor.Log("Soil Health data saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error occurred while persisting soil health data: {ex.Message}", LogLevel.Error);
                // Intentionally do not rethrow; keep runtime cache intact so the game can continue.
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Validate input to prevent potential exceptions
            if (string.IsNullOrWhiteSpace(locationName)) 
            {
                _monitor.Log("GetSoilHealth skipped: invalid location name.", LogLevel.Trace);
                return 0f; // Return default (Poor Soil) if location is invalid
            }

            // Guard against invalid coordinates to prevent misleading lookups
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("GetSoilHealth skipped: invalid tile coordinates.", LogLevel.Trace);
                return 0f;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("GetSoilHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return 0f;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            float result;
            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles) &&
                    tiles.TryGetValue(new Point(ix, iy), out float health))
                {
                    result = health;
                }
                else
                {
                    result = 0f; // Return default (Poor Soil) if no data exists
                }
            }
            return result;
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName)) 
            {
                _monitor.Log("SetSoilHealth skipped: invalid location name.", LogLevel.Warn);
                return; // Skip if location is invalid
            }
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("SetSoilHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

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
            {
                _monitor.Log("UpdateHealth skipped: invalid location name.", LogLevel.Warn);
                return; // Skip if location is invalid
            }
                
            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                _monitor.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn);
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                _monitor.Log("UpdateHealth skipped: coordinates out of integer range.", LogLevel.Trace);
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

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