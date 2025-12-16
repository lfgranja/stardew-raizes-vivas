using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LivingRoots.Domain;
using LivingRoots;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    public class SoilHealthService : ISoilHealthService
    {
        private readonly IModDataService _modDataService;
        private readonly IMonitor _monitor;
        private readonly IFileNameSanitizationService _fileNameSanitizationService;

        // Runtime cache using Point directly as key for better performance and precision
        // Dictionary<LocationName, Dictionary<TileCoordinates, HealthValue>>
        private readonly Dictionary<string, Dictionary<Point, float>> _runtimeCache = new();

        // Lock object for thread safety
        private readonly object _lock = new object();

        public SoilHealthService(IModDataService modDataService, IMonitor monitor, IFileNameSanitizationService fileNameSanitizationService)
        {
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _fileNameSanitizationService = fileNameSanitizationService ?? throw new ArgumentNullException(nameof(fileNameSanitizationService));
        }

        public void LoadData(string saveId)
        {
            // If saveId is invalid, clear the cache to prevent data leakage between saves
            // IMPORTANT: Clearing the cache when saveId is invalid maintains data integrity
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("LoadData aborted: invalid saveId. Runtime cache cleared to prevent data leakage.", LogLevel.Warn);
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return; // Return early without modifying the cache
            }

            string? dataKey = GetSaveKey(saveId);
            
            // If sanitization failed and we got a null key, log and return early
            if (dataKey == null)
            {
                _monitor.Log("LoadData aborted: saveId sanitization failed.", LogLevel.Error);
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return; // Return early without modifying the cache
            }

            // Use temporary cache to prevent data loss if parsing fails partway through
            var tempCache = new Dictionary<string, Dictionary<Point, float>>();

            // LoadData implementation is designed to handle exceptions internally and return null on failure
            SoilHealthState? savedData = _modDataService.LoadData<SoilHealthState>(dataKey);

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
                    int tileCount = 0; // Track the number of tiles loaded for this location
                    bool warnedForInvalidValue = false; // Only warn once per location for invalid values
                    bool warnedForMalformedKey = false; // Only warn once per location for malformed keys
                    bool limitExceededLogged = false; // Only log the limit exceeded warning once per location
                    
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        // Check if we've exceeded the tile limit for this location
                        if (tileCount >= ModConstants.MaxTilesPerLocation)
                        {
                            // Only log once per location when the limit is reached
                            if (!limitExceededLogged)
                            {
                                _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{locationEntry.Key}'; stopping tile processing for this location.", LogLevel.Warn);
                                limitExceededLogged = true;
                            }
                            break; // Stop processing tiles for this location
                        }
                        
                        // Add null/whitespace check for tile keys to prevent crashes with corrupted save files
                        if (string.IsNullOrWhiteSpace(tileEntry.Key))
                        {
                            // Only warn once per location for null/whitespace keys to prevent log spam
                            if (!warnedForMalformedKey)
                            {
                                _monitor.Log($"Null or whitespace tile key found in save data for location '{locationEntry.Key}'; skipping entry.", LogLevel.Warn);
                                warnedForMalformedKey = true;
                            }
                            continue; // Skip this entry
                        }

                        // Parse "X,Y" string back to Point (using integers for tile coordinates)
                        // Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
                        ReadOnlySpan<char> keySpan = tileEntry.Key;
                        int commaIndex = keySpan.IndexOf(',');
                        if (commaIndex > 0 && commaIndex < keySpan.Length - 1 &&
                            int.TryParse(keySpan.Slice(0, commaIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
                            int.TryParse(keySpan.Slice(commaIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                        {
                            // Simplified validation logic: check for NaN/Infinity first, then range
                            float validatedValue = tileEntry.Value;
                            
                            // Check for NaN or Infinity values and convert to 0 instead of skipping
                            if (float.IsNaN(validatedValue) || float.IsInfinity(validatedValue))
                            {
                                // Only warn once per location for invalid values to prevent log spam
                                if (!warnedForInvalidValue)
                                {
                                    _monitor.Log($"Invalid health value (NaN/Infinity) found in save data for location '{locationEntry.Key}'; converting to 0.", LogLevel.Warn);
                                    warnedForInvalidValue = true;
                                }
                                validatedValue = 0f; // Convert to 0 instead of skipping
                            }
                            else if (validatedValue < ModConstants.MinSoilHealth || validatedValue > ModConstants.MaxSoilHealth)
                            {
                                // Only warn once per location for out-of-range values to prevent log spam
                                if (!warnedForInvalidValue)
                                {
                                    _monitor.Log($"Invalid health value found in save data for location '{locationEntry.Key}'; clamping to valid range [0, 100].", LogLevel.Warn);
                                    warnedForInvalidValue = true;
                                }
                                validatedValue = ClampHealthValue(validatedValue);
                            }

                            // Only add non-zero values to prevent bloating the cache with default values
                            if (validatedValue != 0f)
                            {
                                tileDict[new Point(x, y)] = validatedValue;
                                tileCount++; // Increment the tile counter
                            }
                        }
                        else
                        {
                            // Only warn once per location for malformed keys to prevent log spam
                            if (!warnedForMalformedKey)
                            {
                                _monitor.Log($"Malformed tile key found in save data for location '{locationEntry.Key}'; skipping entry.", LogLevel.Warn);
                                warnedForMalformedKey = true;
                            }
                        }
                    }
                    if (tileDict.Count > 0) // Only add location if it has valid tiles
                    {
                        tempCache[locationEntry.Key] = tileDict;
                    }
                }
            }

            // Replace the runtime cache for this save regardless of whether we loaded valid data
            // This ensures data from one save doesn't leak into another
            lock (_lock)
            {
                _runtimeCache.Clear();
                foreach (var location in tempCache)
                {
                    _runtimeCache[location.Key] = location.Value;
                }
            }
            
            if (tempCache.Count == 0)
            {
                _monitor.Log("LoadData found no valid entries; cache has been cleared.", LogLevel.Trace);
            }
        }

        public void SaveData(string saveId)
        {
            // If saveId is invalid, skip saving to prevent using a default/fallback key
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveData aborted: invalid saveId.", LogLevel.Warn);
                return;
            }

            string? dataKey = GetSaveKey(saveId);
            
            // If sanitization failed and we got a null key, log and return early
            if (dataKey == null)
            {
                _monitor.Log("SaveData aborted: saveId sanitization failed.", LogLevel.Error);
                return;
            }

            // Create a snapshot of the current cache to avoid holding the lock during I/O
            // This implements the snapshot pattern to move I/O operations outside the lock
            Dictionary<string, Dictionary<string, float>>? snapshotState = null;
            bool hasDataToSave = false;
            
            lock (_lock)
            {
                if (_runtimeCache.Count == 0)
                {
                    // If no data to save, return early without performing I/O
                    return;
                }

                hasDataToSave = true;
                snapshotState = new Dictionary<string, Dictionary<string, float>>();
                foreach (var location in _runtimeCache)
                {
                    var tileDict = new Dictionary<string, float>();
                    foreach (var tile in location.Value)
                    {
                        // Convert Point back to "X,Y" string format using invariant culture for consistency
                        string tileKey = $"{tile.Key.X.ToString(CultureInfo.InvariantCulture)},{tile.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        
                        // Normalize invalid values (NaN, Infinity) to 0 instead of skipping them to prevent silent data loss
                        float processedValue = tile.Value;
                        if (float.IsNaN(processedValue) || float.IsInfinity(processedValue))
                        {
                            processedValue = 0f; // Convert invalid values to 0
                        }
                        
                        // Clamp value to valid range [0, 100] before saving
                        float clampedValue = ClampHealthValue(processedValue);
                        
                        // Only save non-zero values to prevent bloating the save file with default values
                        if (clampedValue != 0f)
                        {
                            tileDict[tileKey] = clampedValue;
                        }
                    }
                    
                    // Only add location if it has valid tiles
                    if (tileDict.Count > 0)
                    {
                        snapshotState[location.Key] = tileDict;
                    }
                }
            }

            // Only save if we have data to save (this prevents the test failure)
            // This moves the I/O operation completely outside the lock for better performance
            if (hasDataToSave && snapshotState != null && snapshotState.Count > 0)
            {
                var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };
                _modDataService.SaveData(stateToSave, dataKey);
                _monitor.Log($"Soil health data saved for {saveId}", LogLevel.Trace);
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Validate input to prevent potential exceptions
            if (string.IsNullOrWhiteSpace(locationName))
            {
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return 0f; // Return default (Poor Soil) if location is invalid
            }

            // Guard against invalid coordinates to prevent misleading lookups
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return 0f;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
                return 0f;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            float result;
            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    var key = new Point(ix, iy);

                    if (tiles.TryGetValue(key, out float health))
                    {
                        result = health;
                    }
                    else
                    {
                        result = 0f; // Return default (Poor Soil) if no data exists
                    }
                }
                else
                {
                    result = 0f; // Return default (Poor Soil) if location doesn't exist
                }
            }
            return result;
        }

        public void SetSoilHealth(string locationName, Vector2 tile, float value)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName))
            {
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return; // Skip if location is invalid
            }

            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Domain Rule: Clamp between 0 and 100 (not 10 as previously)
                float clampedValue = ClampHealthValue(value);

                var key = new Point(ix, iy);

                // Don't store default values; keep the cache sparse to prevent unbounded growth.
                if (clampedValue == 0f)
                {
                    if (_runtimeCache.TryGetValue(locationName, out var existingTiles) && existingTiles.Remove(key))
                    {
                        if (existingTiles.Count == 0)
                            _runtimeCache.Remove(locationName);
                    }
                    return;
                }

                // Only allocate location storage if we actually need to store a non-default value.
                var tiles = GetOrAddLocationCacheUnsafe(locationName);
                tiles[key] = clampedValue;
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            // Validate input to prevent adding entries with invalid keys
            if (string.IsNullOrWhiteSpace(locationName))
            {
                // Skip logging for invalid location name to reduce noise in frequently called methods
                return; // Skip if location is invalid
            }

            // Guard against invalid coordinates to prevent data corruption
            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                // Skip logging for invalid coordinates to reduce noise in frequently called methods
                return;
            }

            // Check for potential integer overflow before converting coordinates
            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                // Skip logging for coordinate range issues to reduce noise in frequently called methods
                return;
            }

            // Map to tile indices consistently (using MathF.Floor to handle negatives and fractions correctly)
            int ix = (int)fx;
            int iy = (int)fy;

            lock (_lock)
            {
                // Use GetOrAddLocationCacheUnsafe to avoid code duplication
                var tiles = GetOrAddLocationCacheUnsafe(locationName);

                var key = new Point(ix, iy);
                if (tiles.TryGetValue(key, out float current))
                {
                    float newValue = ClampHealthValue(current + delta);
                    
                    // Don't store default values; keep the cache sparse to prevent unbounded growth.
                    if (newValue == 0f)
                    {
                        tiles.Remove(key);
                        if (tiles.Count == 0)
                            _runtimeCache.Remove(locationName);
                    }
                    else
                    {
                        tiles[key] = newValue;
                    }
                }
                else
                {
                    // If the key doesn't exist, initialize with the delta value (starting from 0)
                    float newValue = ClampHealthValue(delta);
                    
                    // Don't store default values; keep the cache sparse to prevent unbounded growth.
                    if (newValue != 0f)
                    {
                        tiles[key] = newValue;
                    }
                }
            }
        }
        
        // Renamed from GetOrAddLocationCache to GetOrAddLocationCacheUnsafe to indicate it should only be called within a lock
        private Dictionary<Point, float> GetOrAddLocationCacheUnsafe(string locationName)
        {
            // Remove the internal lock since this method is now called within an external lock
            // This addresses the nested locking issue mentioned in the code review
            if (!_runtimeCache.TryGetValue(locationName, out var locationCache))
            {
                locationCache = new Dictionary<Point, float>();
                _runtimeCache[locationName] = locationCache;
            }
            return locationCache;
        }
        
        private float ClampHealthValue(float value)
        {
            // Handle NaN and Infinity values before clamping
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f; // Convert invalid values to 0
            }
            
            return Math.Clamp(value, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        }

        private string? GetSaveKey(string saveId)
        {
            // Sanitize the saveId to remove invalid filename characters
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveId cannot be null or empty.", LogLevel.Error);
                return null;
            }
            
            // Add length validation to prevent overlong filenames
            if (saveId.Length > 200) // Reasonable limit for filename
            {
                _monitor.Log("SaveId exceeds maximum length of 200 characters.", LogLevel.Error);
                return null;
            }

            try
            {
                // Apply Unicode normalization using FormC (Canonical Decomposition followed by Canonical Composition)
                string normalizedSaveId = saveId.Normalize(NormalizationForm.FormC);
                
                string? sanitized = _fileNameSanitizationService.Sanitize(normalizedSaveId);
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    _monitor.Log("SaveId sanitizes to an empty string after processing.", LogLevel.Error);
                    return null;
                }
                
                // Remove ToLowerInvariant to prevent data corruption on case-sensitive file systems
                // where MyFarm and myfarm would be treated as different saves but mapped to the same file
                return $"{ModConstants.KeyPrefix}{sanitized}";
            }
            catch (ArgumentException)
            {
                // Log the error without exposing raw exception message for security
                _monitor.Log("SaveId sanitization failed due to invalid characters.", LogLevel.Error);
                return null; // Fail-fast approach: return null instead of a default key
            }
            catch (Exception)
            {
                // Catch any other unexpected exceptions during sanitization
                _monitor.Log("SaveId sanitization failed due to an unexpected error.", LogLevel.Error);
                return null; // Fail-fast approach: return null instead of a default key
            }
        }
    }
}