using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LivingRoots.Domain;
using LivingRoots;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using System.Threading;

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
            // If saveId is invalid, clear cache to prevent data leakage between saves
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

            // Track total tile entries processed across all locations to prevent DoS attacks
            // Declared outside the if block to be accessible for the final limit check
            int totalTileEntriesProcessed = 0;

            // LoadData implementation is designed to handle exceptions internally and return null on failure
            SoilHealthState? savedData = null;
            try
            {
                savedData = _modDataService.LoadData<SoilHealthState>(dataKey);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error occurred while loading soil health data from storage.", LogLevel.Error);
                
                // Add trace-level exception details for debugging without leaking message content
                _monitor.Log($"LoadData exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? "LoadData stack trace unavailable.", LogLevel.Trace);
                #endif
                
                // Ensure we don't leak/serve stale state after a failed load.
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return;
            }

            if (savedData != null)
            {
                // Guard against null LocationHealthData to prevent NullReferenceException during deserialization
                var locations = savedData.LocationHealthData ?? new Dictionary<string, Dictionary<string, float>>();

                int locationCount = 0;

                foreach (var locationEntry in locations)
                {
                    locationCount++;
                    if (locationCount > ModConstants.MaxLocationsPerSave)
                    {
                        _monitor.Log($"Location count limit ({ModConstants.MaxLocationsPerSave}) exceeded; stopping location processing to prevent DoS.", LogLevel.Warn);
                        break;
                    }

                    // Skip if location name is null or empty to prevent NullReferenceException during length check
                    if (string.IsNullOrWhiteSpace(locationEntry.Key))
                    {
                        _monitor.Log("Skipped soil health data with null or empty location name.", LogLevel.Warn);
                        continue;
                    }

                    // ADD LOCATION NAME LENGTH BOUNDING: Check location name length to prevent potential security issues
                    if (locationEntry.Key.Length > ModConstants.MaxLocationNameLength)
                    {
                        // Use helper method to truncate location name for logging
                        string truncatedLocationName = TruncateForLogging(locationEntry.Key);
                        _monitor.Log($"Location name exceeds maximum length of {ModConstants.MaxLocationNameLength} characters; skipping location '{truncatedLocationName}'.", LogLevel.Warn);
                        continue;
                    }

                    // Skip if value is null to prevent NullReferenceException
                    if (locationEntry.Value == null) continue;

                    var tileDict = new Dictionary<Point, float>();
                    int tileCount = 0; // Track number of tiles loaded for this location
                    bool warnedForInvalidValue = false; // Only warn once per location for invalid values
                    bool warnedForMalformedKey = false; // Only warn once per location for malformed keys
                    bool limitExceededLogged = false; // Only log limit exceeded warning once per location
                    
                    foreach (var tileEntry in locationEntry.Value)
                    {
                        // ENHANCEMENT: Move tile count increment BEFORE validation to prevent DoS attacks
                        // Increment tile counter for ALL entries (even if they end up being invalid/skipped)
                        tileCount++;
                        
                        // Check if we've exceeded the tile limit for this location
                        if (tileCount > ModConstants.MaxTilesPerLocation)
                        {
                            // Only log once per location when the limit is reached
                            if (!limitExceededLogged)
                            {
                                // Use helper method to truncate the location name for logging
                                string truncatedLocationName = TruncateForLogging(locationEntry.Key);
                                _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{truncatedLocationName}'; stopping tile processing for this location.", LogLevel.Warn);
                                limitExceededLogged = true;
                            }
                            // For high severity fix: abort the entire load operation if per-location limit is exceeded
                            _monitor.Log($"LoadData aborted: Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{TruncateForLogging(locationEntry.Key)}'. Cache cleared to prevent data loss.", LogLevel.Alert);
                            lock (_lock)
                            {
                                _runtimeCache.Clear();
                            }
                            return;
                        }
                        
                        // GLOBAL DOS PROTECTION: Increment total tile entries across all locations
                        totalTileEntriesProcessed++;
                        if (totalTileEntriesProcessed > ModConstants.MaxTilesPerSave)
                        {
                            _monitor.Log($"Total tile entry limit ({ModConstants.MaxTilesPerSave}) exceeded; stopping load to prevent DoS.", LogLevel.Warn);
                            lock (_lock)
                            {
                                _runtimeCache.Clear();
                            }
                            return;
                        }
                        
                        // Process tile entry using the helper method
                        if (ProcessTileEntry(tileEntry, locationEntry.Key, ref warnedForMalformedKey, ref warnedForInvalidValue, out Point? processedTilePoint, out float? processedValue))
                        {
                            // Only add non-zero values to prevent bloating the cache with default values
                            if (processedTilePoint.HasValue && processedValue.HasValue && processedValue.Value != 0f)
                            {
                                tileDict[processedTilePoint.Value] = processedValue.Value;
                            }
                        }
                    }
                    if (tileDict.Count > 0) // Only add location if it has valid tiles
                    {
                        tempCache[locationEntry.Key] = tileDict;
                    }
                    
                    // GLOBAL DOS PROTECTION: Check if total tile limit has been exceeded after processing this location
                    if (totalTileEntriesProcessed > ModConstants.MaxTilesPerSave)
                        break;
                }
            }

            // If we hit the global limit, treat it as a load failure to avoid silently committing partial/truncated state.
            if (totalTileEntriesProcessed > ModConstants.MaxTilesPerSave)
            {
                _monitor.Log($"LoadData failed: Total tile entry limit ({ModConstants.MaxTilesPerSave}) exceeded. Cache cleared to prevent silent data loss.", LogLevel.Error);
                lock (_lock)
                {
                    _runtimeCache.Clear();
                }
                return;
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
            // PREVENT NULLABLE STATE SERIALIZATION: Initialize as non-nullable dictionary
            var snapshotState = new Dictionary<string, Dictionary<string, float>>();
            
            lock (_lock)
            {
                foreach (var location in _runtimeCache)
                {
                    // Defensive: skip invalid location names to avoid crashing during Saving.
                    if (string.IsNullOrWhiteSpace(location.Key))
                        continue;
                    
                    // ADD LOCATION NAME LENGTH CHECK: Check location name length during save to ensure consistency with load logic
                    if (location.Key.Length > ModConstants.MaxLocationNameLength)
                    {
                        continue; // Skip locations with names that are too long
                    }
                    
                    var tileDict = new Dictionary<string, float>();
                    foreach (var tile in location.Value)
                    {
                        // Convert Point back to "X,Y" string format using invariant culture for consistency
                        string tileKey = $"{tile.Key.X.ToString(CultureInfo.InvariantCulture)},{tile.Key.Y.ToString(CultureInfo.InvariantCulture)}";
                        
                        // Clamp value to valid range [0, 100] before saving - ClampHealthValue handles NaN/Infinity
                        float clampedValue = ClampHealthValue(tile.Value);
                        
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

            // Always save the current state (even if empty) to clear any previously saved data
            // This ensures that if the cache becomes empty, the on-disk data is also cleared
            // Move the I/O operation completely outside the lock for better performance
            var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };
            
            // According to code review feedback, wrap the SaveData call in try-catch to handle exceptions gracefully
            try
            {
                _modDataService.SaveData(stateToSave, dataKey);
                _monitor.Log($"Soil health data saved for '{TruncateForLogging(saveId)}'", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error saving soil health data.", LogLevel.Error);
                
                // Add trace-level exception details for debugging without leaking message content
                _monitor.Log($"SaveData exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? "SaveData stack trace unavailable.", LogLevel.Trace);
                #endif
            }
        }

        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            // Use the validation helper to check for a valid tile
            if (!IsValidTile(locationName, tile, out Point tilePoint))
            {
                return 0f;
            }

            float result;
            lock (_lock)
            {
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    if (tiles.TryGetValue(tilePoint, out float health))
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
            // Use the validation helper to check for a valid tile
            if (!IsValidTile(locationName, tile, out Point tilePoint))
            {
                return; // Skip if location is invalid
            }

            // Variables to track if we need to log warnings (set inside the lock, used outside)
            bool logLocationLimitExceeded = false;
            bool logTileLimitExceeded = false;
            string truncatedLocationNameForTileLog = "";

            lock (_lock)
            {
                // Use the internal helper method to set the health value
                SetHealthInternal(locationName, tilePoint, value, ref logLocationLimitExceeded,
                    ref logTileLimitExceeded, ref truncatedLocationNameForTileLog);
            }
            
            // Log messages outside the lock to avoid blocking other threads
            if (logLocationLimitExceeded)
            {
                _monitor.Log($"Location count limit ({ModConstants.MaxLocationsPerSave}) reached in runtime cache; refusing to add new location to prevent memory growth.", LogLevel.Warn);
            }
            else if (logTileLimitExceeded)
            {
                _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{truncatedLocationNameForTileLog}'; refusing to add new tile to prevent memory growth.", LogLevel.Warn);
            }
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            // Validate the tile using the validation helper
            if (!IsValidTile(locationName, tile, out Point tilePoint))
            {
                return; // Skip if location or tile is invalid
            }

            // Add validation for the delta value to prevent invalid updates.
            if (float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return; // Ignore invalid delta values.
            }

            // Variables to track if we need to log warnings (set inside the lock, used outside)
            bool logLocationLimitExceeded = false;
            bool logTileLimitExceeded = false;
            string truncatedLocationNameForTileLog = "";

            lock (_lock)
            {
                // Read the current health value from the cache
                float currentHealth = 0f;
                if (_runtimeCache.TryGetValue(locationName, out var tiles))
                {
                    if (tiles.TryGetValue(tilePoint, out float health))
                    {
                        currentHealth = health;
                    }
                }

                // Calculate the new health value
                float newHealth = currentHealth + delta;

                // Use the internal helper method to set the health value
                SetHealthInternal(locationName, tilePoint, newHealth, ref logLocationLimitExceeded,
                    ref logTileLimitExceeded, ref truncatedLocationNameForTileLog);
            }
            
            // Log messages outside the lock to avoid blocking other threads
            if (logLocationLimitExceeded)
            {
                _monitor.Log($"Location count limit ({ModConstants.MaxLocationsPerSave}) reached in runtime cache; refusing to add new location to prevent memory growth.", LogLevel.Warn);
            }
            else if (logTileLimitExceeded)
            {
                _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{truncatedLocationNameForTileLog}'; refusing to add new tile to prevent memory growth.", LogLevel.Warn);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _runtimeCache.Clear();
            }
        }

        /// <summary>
        /// Internal helper method to set soil health value in the cache.
        /// This method must be called within a lock to ensure thread safety.
        /// </summary>
        /// <param name="locationName">The name of the location</param>
        /// <param name="tilePoint">The tile coordinates as a Point</param>
        /// <param name="value">The health value to set</param>
        /// <param name="logLocationLimitExceeded">Reference to flag indicating if location limit was exceeded</param>
        /// <param name="logTileLimitExceeded">Reference to flag indicating if tile limit was exceeded</param>
        /// <param name="truncatedLocationNameForTileLog">Reference to truncated location name for tile limit logging</param>
        private void SetHealthInternal(string locationName, Point tilePoint, float value,
            ref bool logLocationLimitExceeded, ref bool logTileLimitExceeded, ref string truncatedLocationNameForTileLog)
        {
            // Domain Rule: Clamp between 0 and 100 (aligning with documentation and MaxSoilHealth constant)
            float clampedValue = ClampHealthValue(value);

            // Don't store default values; keep the cache sparse to prevent unbounded growth.
            if (clampedValue == 0f)
            {
                if (_runtimeCache.TryGetValue(locationName, out var existingTiles) && existingTiles.Remove(tilePoint))
                {
                    if (existingTiles.Count == 0)
                        _runtimeCache.Remove(locationName);
                }
                return;
            }

            // Only allocate location storage if we actually need to store a non-default value.
            if (!_runtimeCache.TryGetValue(locationName, out var tiles))
            {
                // Check location count limit before creating a new location
                if (_runtimeCache.Count >= ModConstants.MaxLocationsPerSave)
                {
                    logLocationLimitExceeded = true;
                    return; // Refuse to add new location if we're over the limit
                }
                
                tiles = new Dictionary<Point, float>();
                _runtimeCache[locationName] = tiles;
            }
            
            // Check if this is a new tile (not an update) and apply the tile limit only to new tiles
            bool isExistingTile = tiles.ContainsKey(tilePoint);
            if (!isExistingTile && tiles.Count >= ModConstants.MaxTilesPerLocation)
            {
                truncatedLocationNameForTileLog = TruncateForLogging(locationName);
                logTileLimitExceeded = true;
                return; // Refuse to add new tiles if we're over the limit for this location, but allow updates
            }
            
            tiles[tilePoint] = clampedValue;
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
            
            // Add length validation to prevent overlong filenames using the constant
            if (saveId.Length > ModConstants.MaxSaveIdLength) // Reasonable limit for filename
            {
                _monitor.Log($"SaveId exceeds maximum length of {ModConstants.MaxSaveIdLength} characters.", LogLevel.Error);
                return null;
            }

            try
            {
                // Apply Unicode normalization using FormC (Canonical Decomposition followed by Canonical Composition)
                string normalizedSaveId = saveId.Normalize(NormalizationForm.FormC);
                
                // Re-check length after normalization in case normalization expands the string
                if (normalizedSaveId.Length > ModConstants.MaxSaveIdLength)
                {
                    _monitor.Log($"SaveId exceeds maximum length of {ModConstants.MaxSaveIdLength} characters after normalization.", LogLevel.Error);
                    return null;
                }

                string? sanitized = _fileNameSanitizationService.Sanitize(normalizedSaveId);
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    _monitor.Log("SaveId sanitizes to an empty string after processing.", LogLevel.Error);
                    return null;
                }
                
                // Calculate the maximum allowed length for the sanitized part (after accounting for prefix)
                int maxSanitizedLength = Math.Max(0, ModConstants.MaxDataKeyLength - ModConstants.KeyPrefix.Length);
                
                // Ensure the final key (prefix + sanitized) stays within the configured bound
                if (sanitized.Length > maxSanitizedLength)
                {
                    _monitor.Log(
                        $"SaveId exceeds maximum sanitized length of {maxSanitizedLength} characters (max key length {ModConstants.MaxDataKeyLength} incl. prefix).",
                        LogLevel.Error);
                    return null;
                }
                
                // Remove ToLowerInvariant to prevent data corruption on case-sensitive file systems
                // where MyFarm and myfarm would be treated as different saves but mapped to the same file
                return $"{ModConstants.KeyPrefix}{sanitized}";
            }
            catch (ArgumentException)
            {
                // Log error without exposing raw exception message for security
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
        
        /// <summary>
        /// Truncates a string for safe logging to prevent potentially malicious long names from being logged.
        /// </summary>
        /// <param name="value">The string value to truncate</param>
        /// <param name="maxLength">The maximum length before truncation (default 50)</param>
        /// <returns>The original string if within length, otherwise a truncated version with "..." appended</returns>
        private static string TruncateForLogging(string value, int maxLength = 50)
        {
            if (value.Length <= maxLength)
                return value;
            return string.Concat(value.AsSpan(0, maxLength), "...");
        }
        
        /// <summary>
        /// Processes a single tile entry from the save data, handling validation and conversion.
        /// </summary>
        /// <param name="tileEntry">The tile entry from the saved data</param>
        /// <param name="locationName">The name of the location being processed</param>
        /// <param name="warnedForMalformedKey">Reference to a flag that tracks if a warning has already been logged for malformed keys in this location</param>
        /// <param name="warnedForInvalidValue">Reference to a flag that tracks if a warning has already been logged for invalid values in this location</param>
        /// <param name="tilePoint">Output parameter for the parsed tile coordinates, if successful</param>
        /// <param name="value">Output parameter for the validated health value, if successful</param>
        /// <returns>True if the tile entry was processed successfully, false if it should be skipped</returns>
        private bool ProcessTileEntry(KeyValuePair<string, float> tileEntry, string locationName, 
            ref bool warnedForMalformedKey, ref bool warnedForInvalidValue, 
            out Point? tilePoint, out float? value)
        {
            tilePoint = null;
            value = null;

            // Add null/whitespace check for tile keys to prevent crashes with corrupted save files
            if (string.IsNullOrWhiteSpace(tileEntry.Key))
            {
                // Only warn once per location for null/whitespace keys to prevent log spam
                if (!warnedForMalformedKey)
                {
                    // Use the helper method to truncate the location name for logging
                    string truncatedLocationName = TruncateForLogging(locationName);
                    _monitor.Log($"Null or whitespace tile key found in save data for location '{truncatedLocationName}'; skipping entry.", LogLevel.Warn);
                    warnedForMalformedKey = true;
                }
                return false; // Skip this entry
            }

            // Parse "X,Y" string back to Point (using integers for tile coordinates)
            // Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
            ReadOnlySpan<char> keySpan = tileEntry.Key.AsSpan().Trim();
            int commaIndex = keySpan.IndexOf(',');
            if (commaIndex > 0 && commaIndex < keySpan.Length - 1 &&
                int.TryParse(keySpan.Slice(0, commaIndex).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
                int.TryParse(keySpan.Slice(commaIndex + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
            {
                // ADDITION: Check for extreme coordinates to prevent potential issues with malicious save files
                // Using direct boundary checks instead of Math.Abs to prevent overflow when dealing with int.MinValue
                if (x < -ModConstants.MaxAbsoluteTileCoordinate || x > ModConstants.MaxAbsoluteTileCoordinate || 
                    y < -ModConstants.MaxAbsoluteTileCoordinate || y > ModConstants.MaxAbsoluteTileCoordinate)
                {
                    if (!warnedForMalformedKey)
                    {
                        // Use the helper method to truncate the location name for logging
                        string truncatedLocationName = TruncateForLogging(locationName);
                        _monitor.Log($"Extreme tile coordinates found in save data for location '{truncatedLocationName}'; skipping entry.", LogLevel.Warn);
                        warnedForMalformedKey = true;
                    }
                    return false; // Skip this entry
                }

                // Simplified validation logic: check for NaN/Infinity first, then range
                float validatedValue = tileEntry.Value;
                
                // Check for NaN or Infinity values and convert to 0 instead of skipping
                if (float.IsNaN(validatedValue) || float.IsInfinity(validatedValue))
                {
                    // Only warn once per location for invalid values to prevent log spam
                    if (!warnedForInvalidValue)
                    {
                        // Use the helper method to truncate the location name for logging
                        string truncatedLocationName = TruncateForLogging(locationName);
                        _monitor.Log($"Invalid health value (NaN/Infinity) found in save data for location '{truncatedLocationName}'; converting to 0.", LogLevel.Warn);
                        warnedForInvalidValue = true;
                    }
                    validatedValue = 0f; // Convert to 0 instead of skipping
                }
                else if (validatedValue < ModConstants.MinSoilHealth || validatedValue > ModConstants.MaxSoilHealth)
                {
                    // Only warn once per location for out-of-range values to prevent log spam
                    if (!warnedForInvalidValue)
                    {
                        // Use the helper method to truncate the location name for logging
                        string truncatedLocationName = TruncateForLogging(locationName);
                        _monitor.Log($"Invalid health value found in save data for location '{truncatedLocationName}'; clamping to valid range [{ModConstants.MinSoilHealth}, {ModConstants.MaxSoilHealth}].", LogLevel.Warn);
                        warnedForInvalidValue = true;
                    }
                    validatedValue = ClampHealthValue(validatedValue);
                }

                tilePoint = new Point(x, y);
                value = validatedValue;
                return true; // Successfully processed
            }
            else
            {
                // Only warn once per location for malformed keys to prevent log spam
                if (!warnedForMalformedKey)
                {
                    // Use the helper method to truncate the location name for logging
                    string truncatedLocationName = TruncateForLogging(locationName);
                    _monitor.Log($"Malformed tile key found in save data for location '{truncatedLocationName}'; skipping entry.", LogLevel.Warn);
                    warnedForMalformedKey = true;
                }
                return false; // Skip this entry
            }
        }
        
        /// <summary>
        /// Validates location name and tile coordinates, and returns the floored tile point if valid.
        /// </summary>
        /// <param name="locationName">The location name to validate</param>
        /// <param name="tile">The tile coordinates to validate</param>
        /// <param name="tilePoint">The floored tile point if validation passes</param>
        /// <returns>True if both location name and tile coordinates are valid, otherwise false</returns>
        private bool IsValidTile(string locationName, Vector2 tile, out Point tilePoint)
        {
            tilePoint = default;

            if (string.IsNullOrWhiteSpace(locationName) || locationName.Length > ModConstants.MaxLocationNameLength)
            {
                return false;
            }

            if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.X) || float.IsInfinity(tile.Y))
            {
                return false;
            }

            float fx = MathF.Floor(tile.X);
            float fy = MathF.Floor(tile.Y);
            if (fx > int.MaxValue || fx < int.MinValue || fy > int.MaxValue || fy < int.MinValue)
            {
                return false;
            }

            // ADD COORDINATE BOUNDS CHECK: Check coordinate bounds to prevent potential issues with extreme tile coordinates
            int ix = (int)fx;
            int iy = (int)fy;
            if (ix < -ModConstants.MaxAbsoluteTileCoordinate || ix > ModConstants.MaxAbsoluteTileCoordinate || 
                iy < -ModConstants.MaxAbsoluteTileCoordinate || iy > ModConstants.MaxAbsoluteTileCoordinate)
            {
                return false;
            }

            tilePoint = new Point(ix, iy);
            return true;
        }
    }
}
