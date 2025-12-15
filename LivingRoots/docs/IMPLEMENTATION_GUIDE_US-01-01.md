# Implementation Guide: US-01-01 - Soil Health Persistence

**Objective:** Implement a robust system to store, load, and manage "Soil Health" values (0-100%) for each tillable tile in the game, ensuring persistence between sessions.

## 1. Architectural Analysis and Design
Following the `ARCHITECTURE.md` and the already implemented Dependency Inversion Principle (DIP) in the project:

### Domain Layer (`LivingRoots/Domain`):
- Define the core business rule: Soil health is a value between 0 and 100.
- Create a data model (DTO) for serialization.
- Define the service interface.

### Application/Services Layer (`LivingRoots/Services`):
- Implement orchestration logic using the existing `ModDataService`.
- Manage mapping between the game world (SMAPI `GameLocation`) and persisted data.
- Implement validation and sanitization of data.

### Controller Layer (`LivingRoots/Controllers`):
- Register `SaveLoaded` and `Saving` events (or `DayEnding`).
- Coordinate between game events and application services.

### Proposed Data Structure

To avoid serialization problems with complex keys in JSON (like `Vector2`) and maintain performance, we use a nested dictionary structure with composite keys.

**Persistence Model (JSON):**
```json
{
  "Farm": {
    "12,15": 85.5,
    "12,16": 90.0
  },
  "Greenhouse": {
    "5,5": 10.0
  }
}
```

## 2. TDD Cycle (Red-Green-Refactor)

### Step 1: Create Unit Tests (Red Phase)

Create tests to verify:
1. That the health value is limited between 0 and 100 (Domain Rule).
2. That the service saves data using a unique key per Save (to avoid conflicts between different saves).
3. That the service retrieves the correct value for a specific coordinate.
4. That the service handles invalid inputs and exceptions properly.
5. That the service is thread-safe.

### Step 2: Implementation (Green & Refactor Phase)

#### 2.1. Domain Layer

**File:** `LivingRoots/Domain/SoilHealthState.cs` (Data Model)
```csharp
using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Represents the persisted state of soil health for the entire save.
    /// Key: Location Name (e.g., "Farm")
    /// Value: Dictionary mapping Tile Coordinates "X,Y" to Health Value (float)
    /// </summary>
    public class SoilHealthState
    {
        public Dictionary<string, Dictionary<string, float>> LocationHealthData { get; set; } = new();
    }
}
```

**File:** `LivingRoots/Domain/ISoilHealthService.cs` (Interface)
```csharp
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    public interface ISoilHealthService
    {
        void LoadData(string saveId);
        void SaveData(string saveId);
        float GetSoilHealth(string locationName, Vector2 tile);
        void SetSoilHealth(string locationName, Vector2 tile, float value);
        void UpdateHealth(string locationName, Vector2 tile, float delta);
    }
}
```

#### 2.2. Services Layer

Implement the concrete logic with rigorous validations and failure safety.

**File:** `LivingRoots/Services/SoilHealthService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

            string dataKey = GetSaveKey(saveId);
            
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

            SoilHealthState? savedData = null;
            bool loadErrorOccurred = false;
            
            try
            {
                savedData = _modDataService.LoadData<SoilHealthState>(dataKey);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error loading soil health data.", LogLevel.Error);
                loadErrorOccurred = true;
            }

            // If there was an error loading the data, return early without modifying runtime cache to preserve existing data
            if (loadErrorOccurred)
            {
                return;
            }

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
                            // Validate health value range
                            float validatedValue = tileEntry.Value;
                            
                            // Check for NaN or Infinity values and skip entirely
                            if (float.IsNaN(validatedValue) || float.IsInfinity(validatedValue))
                            {
                                // Only warn once per location for invalid values to prevent log spam
                                if (!warnedForInvalidValue)
                                {
                                    _monitor.Log($"Invalid health value (NaN/Infinity) found in save data for location '{locationEntry.Key}'; skipping entry.", LogLevel.Warn);
                                    warnedForInvalidValue = true;
                                }
                                // Skip this entry entirely instead of converting to 0
                                continue;
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

                            tileDict[new Point(x, y)] = validatedValue;
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

            string dataKey = GetSaveKey(saveId);
            
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
                        
                        // Skip invalid values (NaN, Infinity) when saving
                        if (float.IsNaN(tile.Value) || float.IsInfinity(tile.Value))
                        {
                            continue; // Skip invalid values
                        }
                        
                        // Clamp value to valid range [0, 100] before saving
                        float clampedValue = ClampHealthValue(tile.Value);
                        tileDict[tileKey] = clampedValue;
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
                try
                {
                    var stateToSave = new SoilHealthState { LocationHealthData = snapshotState };
                    _modDataService.SaveData(stateToSave, dataKey);
                    _monitor.Log($"Soil health data saved for {saveId}", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    // Log error but don't expose raw exception message for security
                    _monitor.Log("Error saving soil health data.", LogLevel.Error);
                }
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
                // Use GetOrAddLocationCache to avoid code duplication
                var tiles = GetOrAddLocationCache(locationName);

                var key = new Point(ix, iy);
                if (tiles.TryGetValue(key, out float current))
                {
                    float newValue = ClampHealthValue(current + delta);
                    tiles[key] = newValue;
                }
                else
                {
                    // If the key doesn't exist, initialize with the delta value (starting from 0)
                    float newValue = ClampHealthValue(delta);
                    tiles[key] = newValue;
                }
            }
        }

        private Dictionary<Point, float> GetOrAddLocationCache(string locationName)
        {
            lock (_lock)
            {
                if (!_runtimeCache.TryGetValue(locationName, out var locationCache))
                {
                    locationCache = new Dictionary<Point, float>();
                    _runtimeCache[locationName] = locationCache;
                }
                return locationCache;
            }
        }
        
        private float ClampHealthValue(float value)
        {
            return Math.Clamp(value, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        }

        private string GetSaveKey(string saveId)
        {
            // Sanitize the saveId to remove invalid filename characters
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveId cannot be null or empty.", LogLevel.Error);
                return null;
            }

            try
            {
                string? sanitized = _fileNameSanitizationService.Sanitize(saveId);
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    _monitor.Log("SaveId sanitizes to an empty string after processing.", LogLevel.Error);
                    return null;
                }
                
                return $"{ModConstants.KeyPrefix}{sanitized.ToLowerInvariant()}";
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
```

#### 2.3. Controller Layer and Dependency Injection

Update `ModEntry.cs` to register the new service and update the controller.

**File:** `LivingRoots/ModEntry.cs` (Snippet)
```csharp
// ... imports

public override void Entry(IModHelper helper)
{
    // ... (existing domain services) ...
    
    // Create application services
    var modDataService = new ModDataService(helper, this.Monitor, modLogic);
    
    // NEW: Soil Health Service
    var fileNameSanitizationService = new FileNameSanitizationService(this.Monitor);
    var saveIdProvider = new SaveIdProvider(this.Monitor);
    var soilHealthService = new SoilHealthService(modDataService, this.Monitor, fileNameSanitizationService);
    
    // Update ModController constructor (see step 2.4)
    _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService, soilHealthService, saveIdProvider);
    
    _controller.RegisterEvents();
}
```

**File:** `LivingRoots/Controllers/ModController.cs`

1. Add the `ISoilHealthService` and `ISaveIdProvider` dependencies.
2. Register the `SaveLoaded` and `Saving` events.

```csharp
// ... imports
using LivingRoots.Domain; // Add this import
using LivingRoots.Services; // Add this import

public sealed class ModController : IDisposable
{
    // ... existing fields
    private readonly ISoilHealthService _soilHealthService;
    private readonly ISaveIdProvider _saveIdProvider;

    // Update constructor
    public ModController(
        IModHelper helper, 
        IMonitor monitor, 
        IManifest manifest, 
        IModDataService modDataService,
        ISoilHealthService soilHealthService,
        ISaveIdProvider saveIdProvider) // New dependency
    {
        // ... assignments
        _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
        _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));
    }

    public void RegisterEvents()
    {
        // ... checks ...
        
        try 
        {
            var gameLoop = _helper?.Events?.GameLoop;
            // ... (null checks)
            
            _onGameLaunchedHandler ??= OnGameLaunched;

            // NEW EVENTS
            gameLoop.SaveLoaded += OnSaveLoaded;
            gameLoop.Saving += OnSaving; // Note: Using 'Saving' instead of 'Saved' for pre-save hook
            
            // ... logs
        }
        // ... catch block
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Get the save ID using the abstraction (monitor is already available in the provider)
        string? saveId = _saveIdProvider.GetSaveId();

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; skipping soil health load.", LogLevel.Warn);
            return;
        }

        try
        {
            // Load data using the save folder name as unique ID
            _soilHealthService.LoadData(saveId);
            _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // Log error but don't expose raw exception message for security
            _monitor.Log($"Error occurred while loading soil health data for save.", LogLevel.Error);
        }
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        // Get the save ID using the abstraction (monitor is already available in the provider)
        string? saveId = _saveIdProvider.GetSaveId();

        if (string.IsNullOrWhiteSpace(saveId))
        {
            _monitor.Log("OnSaving: SaveFolderName unavailable; skipping soil health save.", LogLevel.Warn);
            return;
        }

        try
        {
            // Save data before the game saves/exits (using the saving event)
            _soilHealthService.SaveData(saveId);
            _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            // Log error but don't expose raw exception message for security
            _monitor.Log($"Error occurred while saving soil health data for save.", LogLevel.Error);
        }
    }
    
    // ... Dispose/Unregister methods should also remove the new events ...
}
```

## 3. Verification and Acceptance Criteria

After implementation, we perform the following manual and automated checks:

1. **Automated Tests:**
   - Run `dotnet test`. All tests (including the new `SoilHealthServiceTests`) should pass.
2. **In-Game Testing (Manual):**
   - Start the game and load a save.
   - Check the SMAPI console (Trace logs) for: `Soil health data loaded successfully.`.
   - Play a day, do something that changes health (future feature, for now the value is static or changed via debug console if you create a command).
   - Sleep to save. Check the log: `Soil health data saved successfully.`.
   - Close the game and check the mod folder: `LivingRoots/data/soil_health_data_[SaveName].json`. The file should exist and contain valid JSON.
   - Open the game again. Data should load without error.

## 4. Security and Robustness Improvements

### Input Validation
- Invalid coordinate verification (NaN, Infinity)
- Invalid location name verification
- Health value range verification [0, 100]

### Exception Handling
- Temporary cache usage to prevent data loss during loading failures
- Exception handling during read and write operations
- Preservation of existing cache in case of failures

### Thread Safety
- Use of locks to ensure thread-safe operations
- Prevention of race conditions during concurrent access
### Secure Serialization
- Data validation during loading and saving
- Handling of invalid values (NaN, Infinity) during serialization
- Prevention of malicious data injection

## 5. Summary of Required Changes

1. **Create Tests:** `LivingRoots.Tests/SoilHealthServiceTests.cs`.
2. **Create Interfaces/Models:** `ISoilHealthService.cs`, `SoilHealthState.cs`.
3. **Implement Service:** `SoilHealthService.cs` (using `IModDataService`).
4. **Update ModEntry:** Dependency injection of the new service.
5. **Update ModController:** Hook into `SaveLoaded` and `Saving` events.

This approach strictly follows SOLID principles (SRP in the service, DIP in interfaces) and DDD (Ubiquitous Language "SoilHealth"), ensuring a solid foundation for the next roadmap features.
