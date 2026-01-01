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
            // Validate save ID and get sanitized key
            string? dataKey = ValidateSaveId(saveId);
            if (dataKey == null)
            {
                return; // LoadData already logged and cleared cache for invalid saveId
            }

            // Load data from storage
            SoilHealthState? savedData = LoadFromStorage(dataKey);
            if (savedData == null)
            {
                return; // LoadFromStorage already logged and cleared cache for errors
            }

            // Process and rebuild runtime cache
            RebuildRuntimeCache(savedData);
        }

        private string? ValidateSaveId(string saveId)
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
                return null; // Return early without modifying the cache
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
                return null; // Return early without modifying the cache
            }

            return dataKey;
        }

        private SoilHealthState? LoadFromStorage(string dataKey)
        {
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
                return null;
            }

            return savedData;
        }

        private void RebuildRuntimeCache(SoilHealthState savedData)
        {
            // Use temporary cache to prevent data loss if parsing fails partway through
            var tempCache = new Dictionary<string, Dictionary<Point, float>>();

            // Track total tile entries processed across all locations to prevent DoS attacks
            // Declared outside the if block to be accessible for the final limit check
            int totalTileEntriesProcessed = 0;

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

                    // Process location entry with validation
                    var processResult = ProcessLocationEntry(locationEntry, ref totalTileEntriesProcessed);
                    if (processResult.IsSuccess && processResult.TileDict.Count > 0)
                    {
                        tempCache[locationEntry.Key] = processResult.TileDict;
                    }
                    else if (!processResult.IsSuccess)
                    {
                        // If processing failed due to DoS protection, return early
                        return;
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

        private ProcessLocationResult ProcessLocationEntry(KeyValuePair<string, Dictionary<string, float>> locationEntry, ref int totalTileEntriesProcessed)
        {
            // Validate location name
            if (!IsValidLocationName(locationEntry.Key))
            {
                return ProcessLocationResult.SuccessEmpty();
            }

            // Skip if value is null to prevent NullReferenceException
            if (locationEntry.Value == null)
            {
                return ProcessLocationResult.SuccessEmpty();
            }

            var tileDict = new Dictionary<Point, float>();
            int tileCount = 0; // Track number of tiles loaded for this location
            bool warnedForInvalidValue = false; // Only warn once per location for invalid values
            bool warnedForMalformedKey = false; // Only warn once per location for malformed keys

            foreach (var tileEntry in locationEntry.Value)
            {
                // ENHANCEMENT: Move tile count increment BEFORE validation to prevent DoS attacks
                // Increment tile counter for ALL entries (even if they end up being invalid/skipped)
                tileCount++;

                // CRITICAL DOS PROTECTION: Check if we've exceeded the tile limit for this location
                // When per-location tile limit is exceeded, we must abort the entire load operation
                // to prevent data loss and maintain cache consistency. This prevents partial loading
                // which could result in inconsistent state where some data is loaded while other data is lost.
                if (tileCount > ModConstants.MaxTilesPerLocation)
                {
                    _monitor.Log(
                        $"LoadData aborted: Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{TruncateForLogging(locationEntry.Key)}'. Cache cleared to prevent inconsistent state.",
                        LogLevel.Alert);

                    lock (_lock)
                    {
                        _runtimeCache.Clear();
                    }
                    return ProcessLocationResult.Failure(); // Abort entire operation to maintain data consistency
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
                    return ProcessLocationResult.Failure();
                }

                // Process tile entry using the helper method
                var processingResult = ProcessTileEntry(tileEntry);

                // Handle valid tile entries
                if (processingResult.IsSuccess &&
                    processingResult.TilePoint.HasValue &&
                    processingResult.HealthValue.HasValue &&
                    Math.Abs(processingResult.HealthValue.Value) > 0.0001f) // Using epsilon comparison for floating point
                {
                    tileDict[processingResult.TilePoint.Value] = processingResult.HealthValue.Value;
                }
                // Handle invalid values with logging
                else if (processingResult.Status == TileValidationStatus.InvalidValue && !warnedForInvalidValue)
                {
                    // Only warn once per location for invalid values to prevent log spam
                    string truncatedLocationName = TruncateForLogging(locationEntry.Key);
                    _monitor.Log($"Invalid health value found in save data for location '{truncatedLocationName}'; clamping to valid range [{ModConstants.MinSoilHealth}, {ModConstants.MaxSoilHealth}].", LogLevel.Warn);
                    warnedForInvalidValue = true;
                }
                // Handle malformed keys or extreme coordinates with logging
                else if ((processingResult.Status == TileValidationStatus.MalformedKey ||
                          processingResult.Status == TileValidationStatus.ExtremeCoordinates) &&
                         !warnedForMalformedKey)
                {
                    // Only warn once per location for malformed keys to prevent log spam
                    string truncatedLocationName = TruncateForLogging(locationEntry.Key);
                    _monitor.Log($"Malformed tile key found in save data for location '{truncatedLocationName}'; skipping entry.", LogLevel.Warn);
                    warnedForMalformedKey = true;
                }
            }

            return ProcessLocationResult.SuccessWithTiles(tileDict);
        }

        /// <summary>
        /// Validates if the location name is valid according to business rules
        /// </summary>
        /// <param name="locationName">The location name to validate</param>
        /// <returns>True if the location name is valid, false otherwise</returns>
        private bool IsValidLocationName(string locationName)
        {
            // Skip if location name is null or empty to prevent NullReferenceException during length check
            if (string.IsNullOrWhiteSpace(locationName))
            {
                _monitor.Log("Skipped soil health data with null or empty location name.", LogLevel.Warn);
                return false;
            }

            // ADD LOCATION NAME LENGTH BOUNDING: Check location name length to prevent potential security issues
            if (locationName.Length > ModConstants.MaxLocationNameLength)
            {
                // Use helper method to truncate location name for logging
                string truncatedLocationName = TruncateForLogging(locationName);
                _monitor.Log($"Location name exceeds maximum length of {ModConstants.MaxLocationNameLength} characters; skipping location '{truncatedLocationName}'.", LogLevel.Warn);
                return false;
            }

            return true;
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
            var saveState = CreateSaveSnapshot();
            if (saveState.saveAbortedForLimits)
            {
                _monitor.Log("SaveData aborted: runtime cache exceeds configured limits; refusing to write potentially huge/partial state.", LogLevel.Error);

                // Overwrite the stored state with an empty state to prevent a persistent failure loop
                try
                {
                    _modDataService.SaveData(new SoilHealthState { LocationHealthData = new Dictionary<string, Dictionary<string, float>>() }, dataKey);
                }
                catch (Exception ex)
                {
                    _monitor.Log("Error clearing soil health data after SaveData limit abort.", LogLevel.Error);
                    _monitor.Log($"SaveData(clear) exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
#if DEBUG
                    _monitor.Log(ex.StackTrace ?? "SaveData(clear) stack trace unavailable.", LogLevel.Trace);
#endif
                }

                return;
            }

            // Persist the data
            PersistData(dataKey, saveState.snapshotState, saveId);
        }

        private (Dictionary<string, Dictionary<string, float>> snapshotState, bool saveAbortedForLimits) CreateSaveSnapshot()
        {
            // Create a snapshot of the current cache to avoid holding the lock during I/O
            // This implements the snapshot pattern to move I/O operations outside the lock
            // PREVENT NULLABLE STATE SERIALIZATION: Initialize as non-nullable dictionary
            var snapshotState = new Dictionary<string, Dictionary<string, float>>();
            int totalTilesToSave = 0;
            int locationsToSave = 0;
            bool saveAbortedForLimits = false;

            lock (_lock)
            {
                foreach (var location in _runtimeCache)
                {
                    // Process location for saving with validation
                    var processResult = ProcessLocationForSave(location, ref totalTilesToSave, ref locationsToSave);
                    if (processResult.IsSuccess && processResult.TileDict.Count > 0)
                    {
                        snapshotState[location.Key] = processResult.TileDict;
                    }
                    else if (processResult.IsAborted)
                    {
                        // If processing was aborted due to limits, return early
                        return (snapshotState, true);
                    }
                }
            }

            return (snapshotState, saveAbortedForLimits);
        }

        private static ProcessSaveLocationResult ProcessLocationForSave(KeyValuePair<string, Dictionary<Point, float>> location,
            ref int totalTilesToSave, ref int locationsToSave)
        {
            // Defensive: skip invalid location names to avoid crashing during Saving.
            if (string.IsNullOrWhiteSpace(location.Key))
                return ProcessSaveLocationResult.SuccessEmpty();

            // ADD LOCATION NAME LENGTH CHECK: Check location name length during save to ensure consistency with load logic
            if (location.Key.Length > ModConstants.MaxLocationNameLength)
                return ProcessSaveLocationResult.SuccessEmpty();

            if (location.Value == null)
                return ProcessSaveLocationResult.SuccessEmpty();

            locationsToSave++;
            if (locationsToSave > ModConstants.MaxLocationsPerSave)
            {
                return ProcessSaveLocationResult.Aborted();
            }

            var tileDict = new Dictionary<string, float>();
            int tilesInLocation = 0;

            foreach (var tile in location.Value)
            {
                tilesInLocation++;
                if (tilesInLocation > ModConstants.MaxTilesPerLocation)
                {
                    return ProcessSaveLocationResult.Aborted();
                }

                totalTilesToSave++;
                if (totalTilesToSave > ModConstants.MaxTilesPerSave)
                {
                    return ProcessSaveLocationResult.Aborted();
                }

                string tileKey = $"{tile.Key.X.ToString(CultureInfo.InvariantCulture)},{tile.Key.Y.ToString(CultureInfo.InvariantCulture)}";

                // Clamp value to valid range [0, 100] before saving - ClampHealthValue handles NaN/Infinity
                float clampedValue = ClampHealthValue(tile.Value);

                // Only save non-zero values to prevent bloating the save file with default values
                if (Math.Abs(clampedValue) > 0.0001f) // Using epsilon comparison for floating point
                {
                    tileDict[tileKey] = clampedValue;
                }
            }

            return ProcessSaveLocationResult.SuccessWithTiles(tileDict);
        }

        private void PersistData(string dataKey, Dictionary<string, Dictionary<string, float>> snapshotState, string originalSaveId)
        {
            // Always save the current state (even if empty) to clear any previously saved data
            // This ensures that if the cache becomes empty, the on-disk data is also cleared
            // Move the I/O operation completely outside the lock for better performance
            var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };

            // According to code review feedback, wrap the SaveData call in try-catch to handle exceptions gracefully
            try
            {
                _modDataService.SaveData(stateToSave, dataKey);
                _monitor.Log($"Soil health data saved for '{TruncateForLogging(originalSaveId)}'", LogLevel.Trace);
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
                if (_runtimeCache.TryGetValue(locationName, out var tiles) && tiles.TryGetValue(tilePoint, out float health))
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
            // Use the validation helper to check for a valid tile
            if (!IsValidTile(locationName, tile, out Point tilePoint))
            {
                return; // Skip if location is invalid
            }

            // Variables to track if we need to log warnings (set inside the lock, used outside)
            var operationResult = SoilHealthOperationResult.Success;

            lock (_lock)
            {
                // Use the internal helper method to set the health value
                operationResult = SetHealthInternal(locationName, tilePoint, value);
            }

            // Log messages outside the lock to avoid blocking other threads
            LogOperationResult(operationResult, locationName);
        }

        public void UpdateHealth(string locationName, Vector2 tile, float delta)
        {
            // Validation for the delta value to prevent invalid updates.
            if (float.IsNaN(delta))
            {
                return; // Ignore invalid delta values.
            }

            // Validate the tile using the validation helper
            if (!IsValidTile(locationName, tile, out Point tilePoint))
            {
                return; // Skip if location or tile is invalid
            }

            // Variables to track if we need to log warnings (set inside the lock, used outside)
            var operationResult = SoilHealthOperationResult.Success;

            lock (_lock)
            {
                // Read the current health value from the cache
                float currentHealth = 0f;
                if (_runtimeCache.TryGetValue(locationName, out var tiles) && tiles.TryGetValue(tilePoint, out float health))
                {
                    currentHealth = health;
                }

                // Calculate the new health value
                float newHealth = currentHealth + delta;

                // Use the internal helper method to set the health value
                operationResult = SetHealthInternal(locationName, tilePoint, newHealth);
            }

            // Log messages outside the lock to avoid blocking other threads
            LogOperationResult(operationResult, locationName);
        }

        private void LogOperationResult(SoilHealthOperationResult operationResult, string locationName)
        {
            switch (operationResult)
            {
                case SoilHealthOperationResult.LocationLimitExceeded:
                    _monitor.Log($"Location count limit ({ModConstants.MaxLocationsPerSave}) reached in runtime cache; refusing to add new location to prevent memory growth.", LogLevel.Warn);
                    break;
                case SoilHealthOperationResult.TileLimitExceeded:
                    _monitor.Log($"Tile count limit ({ModConstants.MaxTilesPerLocation}) exceeded for location '{TruncateForLogging(locationName)}'; refusing to add new tile to prevent memory growth.", LogLevel.Warn);
                    break;
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
        private SoilHealthOperationResult SetHealthInternal(string locationName, Point tilePoint, float value)
        {
            // Domain Rule: Clamp between 0 and 100 (aligning with documentation and MaxSoilHealth constant)
            float clampedValue = ClampHealthValue(value);

            // Don't store default values; keep the cache sparse to prevent unbounded growth.
            if (Math.Abs(clampedValue) < 0.0001f) // Using epsilon comparison for floating point
            {
                if (_runtimeCache.TryGetValue(locationName, out var existingTiles) && existingTiles.Remove(tilePoint))
                {
                    if (existingTiles.Count == 0)
                        _runtimeCache.Remove(locationName);
                }
                return SoilHealthOperationResult.Success;
            }

            // Only allocate location storage if we actually need to store a non-default value.
            if (!_runtimeCache.TryGetValue(locationName, out var tiles))
            {
                // Check location count limit before creating a new location
                if (_runtimeCache.Count >= ModConstants.MaxLocationsPerSave)
                {
                    return SoilHealthOperationResult.LocationLimitExceeded; // Refuse to add new location if we're over the limit
                }

                tiles = new Dictionary<Point, float>();
                _runtimeCache[locationName] = tiles;
            }

            // Check if this is a new tile (not an update) and apply the tile limit only to new tiles
            bool isExistingTile = tiles.ContainsKey(tilePoint);
            if (!isExistingTile && tiles.Count >= ModConstants.MaxTilesPerLocation)
            {
                return SoilHealthOperationResult.TileLimitExceeded; // Refuse to add new tiles if we're over the limit for this location, but allow updates
            }

            tiles[tilePoint] = clampedValue;
            return SoilHealthOperationResult.Success;
        }

        private static float ClampHealthValue(float value)
        {
            // Handle NaN and Infinity values before clamping
            if (float.IsPositiveInfinity(value))
            {
                return ModConstants.MaxSoilHealth;
            }

            if (float.IsNegativeInfinity(value))
            {
                return ModConstants.MinSoilHealth;
            }

            if (float.IsNaN(value))
            {
                return 0f;
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
        /// <returns>Result containing the processed tile point, value, and validation status</returns>
        private static TileProcessingResult ProcessTileEntry(KeyValuePair<string, float> tileEntry)
        {
            // Add null/whitespace check for tile keys to prevent crashes with corrupted save files
            if (string.IsNullOrWhiteSpace(tileEntry.Key))
            {
                return new TileProcessingResult(false, null, null, TileValidationStatus.MalformedKey);
            }

            // Parse "X,Y" string back to Point (using integers for tile coordinates)
            // Use ReadOnlySpan<char> to avoid string.Split allocation for better performance
            ReadOnlySpan<char> keySpan = tileEntry.Key.AsSpan().Trim();
            int commaIndex = keySpan.IndexOf(',');

            // If no comma found or comma is at the start/end, return malformed
            if (commaIndex <= 0 || commaIndex >= keySpan.Length - 1)
            {
                return new TileProcessingResult(false, null, null, TileValidationStatus.MalformedKey);
            }

            // Extract X and Y parts and parse them
            var xPart = keySpan.Slice(0, commaIndex).Trim();
            var yPart = keySpan.Slice(commaIndex + 1).Trim();

            if (!int.TryParse(xPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                !int.TryParse(yPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
            {
                return new TileProcessingResult(false, null, null, TileValidationStatus.MalformedKey);
            }

            // ADDITION: Check for extreme coordinates to prevent potential issues with malicious save files
            // Using direct boundary checks instead of Math.Abs to prevent overflow when dealing with int.MinValue
            if (x < -ModConstants.MaxAbsoluteTileCoordinate || x > ModConstants.MaxAbsoluteTileCoordinate ||
                y < -ModConstants.MaxAbsoluteTileCoordinate || y > ModConstants.MaxAbsoluteTileCoordinate)
            {
                return new TileProcessingResult(false, null, null, TileValidationStatus.ExtremeCoordinates);
            }

            float validatedValue = tileEntry.Value;

            if (float.IsNaN(validatedValue) || validatedValue < ModConstants.MinSoilHealth || validatedValue > ModConstants.MaxSoilHealth)
            {
                validatedValue = ClampHealthValue(validatedValue);
                return new TileProcessingResult(true, new Point(x, y), validatedValue, TileValidationStatus.InvalidValue);
            }

            return new TileProcessingResult(true, new Point(x, y), validatedValue, TileValidationStatus.Valid);
        }

        /// <summary>
        /// Validates location name and tile coordinates, and returns the floored tile point if valid.
        /// </summary>
        /// <param name="locationName">The location name to validate</param>
        /// <param name="tile">The tile coordinates to validate</param>
        /// <param name="tilePoint">The floored tile point if validation passes</param>
        /// <returns>True if both location name and tile coordinates are valid, otherwise false</returns>
        private static bool IsValidTile(string locationName, Vector2 tile, out Point tilePoint)
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

    /// <summary>
    /// Represents the result of processing a location entry during load
    /// </summary>
    internal readonly struct ProcessLocationResult
    {
        public bool IsSuccess { get; }
        public Dictionary<Point, float> TileDict { get; }
        public bool IsFailure { get; }

        public ProcessLocationResult(bool isSuccess, Dictionary<Point, float> tileDict, bool isFailure)
        {
            IsSuccess = isSuccess;
            TileDict = tileDict;
            IsFailure = isFailure;
        }

        public static ProcessLocationResult SuccessWithTiles(Dictionary<Point, float> tileDict)
        {
            return new ProcessLocationResult(true, tileDict, false);
        }

        public static ProcessLocationResult SuccessEmpty()
        {
            return new ProcessLocationResult(true, new Dictionary<Point, float>(), false);
        }

        public static ProcessLocationResult Failure()
        {
            return new ProcessLocationResult(false, new Dictionary<Point, float>(), true);
        }
    }

    /// <summary>
    /// Represents the result of processing a location for save
    /// </summary>
    internal readonly struct ProcessSaveLocationResult
    {
        public bool IsSuccess { get; }
        public Dictionary<string, float> TileDict { get; }
        public bool IsAborted { get; }

        public ProcessSaveLocationResult(bool isSuccess, Dictionary<string, float> tileDict, bool isAborted)
        {
            IsSuccess = isSuccess;
            TileDict = tileDict;
            IsAborted = isAborted;
        }

        public static ProcessSaveLocationResult SuccessWithTiles(Dictionary<string, float> tileDict)
        {
            return new ProcessSaveLocationResult(true, tileDict, false);
        }

        public static ProcessSaveLocationResult SuccessEmpty()
        {
            return new ProcessSaveLocationResult(true, new Dictionary<string, float>(), false);
        }

        public static ProcessSaveLocationResult Aborted()
        {
            return new ProcessSaveLocationResult(false, new Dictionary<string, float>(), true);
        }
    }

    /// <summary>
    /// Represents the result of processing a tile entry from save data
    /// </summary>
    internal readonly struct TileProcessingResult
    {
        public bool IsSuccess { get; }
        public Point? TilePoint { get; }
        public float? HealthValue { get; }
        public TileValidationStatus Status { get; }

        public TileProcessingResult(bool isSuccess, Point? tilePoint, float? healthValue, TileValidationStatus status)
        {
            IsSuccess = isSuccess;
            TilePoint = tilePoint;
            HealthValue = healthValue;
            Status = status;
        }
    }

    /// <summary>
    /// Enum representing the validation status of a tile entry
    /// </summary>
    internal enum TileValidationStatus
    {
        Valid,
        MalformedKey,
        InvalidValue,
        ExtremeCoordinates
    }

    /// <summary>
    /// Enum representing the result of a soil health operation
    /// </summary>
    internal enum SoilHealthOperationResult
    {
        Success,
        LocationLimitExceeded,
        TileLimitExceeded
    }
}
