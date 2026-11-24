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

The `SoilHealthService` implements the core logic with the following key changes:

1. **NaN/Infinity Handling**: LoadData now converts NaN/Infinity values to 0 instead of skipping them.
2. **Cache Clearing**: Clear the cache on load failure to prevent cross-save data leakage.
3. **Key Sanitization**: Apply ToLowerInvariant only to the sanitized portion of the save key, not the entire key including prefix.

Key improvements include:
- Thread-safe operations using locks
- Proper validation of input coordinates and values
- Secure serialization with validation during loading/saving
- Error handling to prevent data corruption

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
- Clearing cache on load failure to prevent cross-save data leakage
- Preservation of existing cache in case of failures

### Thread Safety
- Use of locks to ensure thread-safe operations
- Prevention of race conditions during concurrent access

### Secure Serialization
- Data validation during loading and saving
- Handling of invalid values (NaN, Infinity) during serialization by converting them to 0
- Prevention of malicious data injection

## 5. Summary of Required Changes

1. **Create Tests:** `LivingRoots.Tests/SoilHealthServiceTests.cs`.
2. **Create Interfaces/Models:** `ISoilHealthService.cs`, `SoilHealthState.cs`.
3. **Implement Service:** `SoilHealthService.cs` (using `IModDataService`).
4. **Update ModEntry:** Dependency injection of the new service.
5. **Update ModController:** Hook into `SaveLoaded` and `Saving` events.

This approach strictly follows SOLID principles (SRP in the service, DIP in interfaces) and DDD (Ubiquitous Language "SoilHealth"), ensuring a solid foundation for the next roadmap features.
