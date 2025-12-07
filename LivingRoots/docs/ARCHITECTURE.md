# ARCHITECTURE.md - Stardew Valley: Living Roots Mod Architecture

This document describes the architecture for the "Living Roots" mod, focusing on the application of software engineering principles (SOLID, TDD, DDD) to ensure clean, modular, testable, and easily maintainable and extensible code.

## Fundamental Principles

Our architecture is guided by the following principles:

*   **Separation of Concerns (SOLID - S):** Each component (class, module) will have a single, well-defined responsibility.
*   **Dependency Inversion (SOLID - D):** High-level modules will not depend on low-level modules. Both will depend on abstractions (interfaces), facilitating testability and flexibility.
*   **Domain-Driven Design (DDD):** The code will reflect the agroecology domain, using a "Ubiquitous Language" to name classes, methods, and variables (e.g., `SoilHealth`, `Composter`, `CropRotation`).
*   **Test-Driven Development (TDD):** Core business logic will be developed with unit tests first, ensuring correctness and robustness.
*   **KISS (Keep It Simple, Stupid):** Avoiding unnecessary complexity, such as wrapper interfaces when the original interface is sufficient (e.g., using `IMonitor` directly instead of creating `IMonitorWrapper`).
*   **YAGNI (You Aren't Gonna Need It):** Avoiding premature abstractions that don't provide immediate value (e.g., removing unused interfaces like `IModService`).

## Folder Structure and Components

The project is organized into a folder structure that reflects the separation of concerns and Domain-Driven Design:


LivingRoots/
├── Domain/                 # Pure Business Logic (Agroecology) - Planned
│   ├── IModLogic.cs        # Interface for mod logic - Planned
│   └── ...
├── Services/               # SMAPI Interaction and Data Persistence
│   ├── IModDataService.cs  # Interface for data persistence
│   ├── ModDataService.cs   # Implementation of data persistence
│   └── ...
├── Controllers/            # Game Event Management and Player Interaction
│   ├── ModController.cs    # Main controller for game events
│   └── ...
├── Models/                 # Data classes (POCOs) - Planned for future use
│   └── ...                   # Currently, data models are embedded within services or domain classes as needed
├── docs/                   # Documentation files
├── ModEntry.cs             # Mod Entry Point and Dependency Orchestrator
├── LivingRoots.csproj
└── manifest.json


LivingRoots.Tests/          # Unit Tests Project
├── LivingRoots.Tests.csproj
├── ModControllerTests.cs
├── ModDataServiceTests.cs
└── Usings.cs


### Component Details

1.  **`ModEntry.cs` (Entry Point and Orchestrator)**
    *   This is the main class that SMAPI loads.
    *   Its primary responsibility is to initialize the mod, register SMAPI events, and **orchestrate the creation and injection of dependencies** for other components (following the D of SOLID principle).
    *   Example:
        ```csharp
        public override void Entry(IModHelper helper)
        {
            // Create domain services - Composition Root
            var unicodeNormalizationService = new UnicodeNormalizationService();
            var reservedNameHandler = new ReservedNameHandler(unicodeNormalizationService);
            var fileNameSanitizationService = new FileNameSanitizationService(unicodeNormalizationService, reservedNameHandler);
            var pathValidationService = new PathValidationService(unicodeNormalizationService);
            var modLogic = new ModLogic(fileNameSanitizationService, pathValidationService);
            
            // Create application services
            var modDataService = new ModDataService(helper, this.Monitor, modLogic);
            
            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService);
            
            // Register events through the Controller
            _controller.RegisterEvents();
        }
```
        

2. **`Domain/` (Pure Business Logic)**
    *   Planned folder for classes that will encapsulate the mod's core logic, directly related to agroecology concepts.
    *   These classes will be **pure**, meaning they will not directly interact with the game (SMAPI) or data persistence.
    *   They will be highly testable via TDD, as they will have no complex external dependencies.
    *   Contains interfaces like `ISoilHealthService` for future domain logic implementations.

3. **`Services/` (SMAPI Interaction and Persistence)**
    *   Responsible for interacting with SMAPI APIs to:
        *   Persist and load mod data (e.g., `helper.Data.WriteJsonFile`).
        *   Access game information (locations, tiles, objects).
        *   Provide interfaces for other components to access data abstractly (following the I of SOLID principle).
    *   `ModDataService.cs` (implementation) and `IModDataService.cs` (interface) handle generic mod data saving/loading.
    *   `SoilHealthService.cs` (implementation) and `ISoilHealthService.cs` (interface) handle soil health data persistence.

4. **`Controllers/` (Event Management and Interaction)**
    *   Responsible for listening to game events (via `helper.Events`) and reacting to them, orchestrating calls to domain logic and services.
    *   `ModController.cs` is the main controller that handles game events like `GameLaunched`, `SaveLoaded`, and `Saving`.

5. **`Models/` (Data Classes)**
    *   Simple classes (POCOs - Plain Old C# Objects) that represent the structure of mod data.
    *   Used for serialization/deserialization of data and for passing information between components.
    *   This directory is planned for future use to centralize data models.
    *   Currently, data models are embedded within services or domain classes as needed.

## Data and Control Flow (Example: Soil Health)

1.  **`ModEntry`** initializes `ModController`, injecting dependencies like `IModHelper`, `IMonitor`, `IManifest`, `IModDataService`, and `ISoilHealthService`.
2.  **`ModController`** listens to the `GameLoop.SaveLoaded` event from SMAPI to load soil health data when a save is loaded.
3.  **`ModController`** listens to the `GameLoop.Saving` event from SMAPI to save soil health data before the game saves.
4.  When a save is loaded, `ModController` calls `ISoilHealthService.LoadData()` to retrieve soil health values from the save file.
5.  When the game is about to save, `ModController` calls `ISoilHealthService.SaveData()` to persist soil health values to the save file.
6.  **`SoilHealthService`** manages the runtime cache and handles conversion between disk format (string keys) and runtime format (Point keys) for optimal performance.
7.  The architecture ensures that business logic is independent of the game and UI, making it easier to test and maintain. Interaction with the game is encapsulated in services and controllers, minimizing coupling.

## Recent Improvements and Fixes

The following improvements and security fixes have been implemented to enhance the mod's robustness:

1. **Thread Safety in ModController Command Registration**: Implemented atomic state management with bit flags and thread-safe operations to prevent race conditions during command registration. Uses `Interlocked` operations to ensure only one thread can register commands or events.

2. **Static Readonly HashSet Optimization**: Optimized the blocked extensions list by making it a static readonly field to avoid rebuilding the set on every call, improving performance.

3. **Error Message Consistency**: Standardized error messages in PathValidationService to ensure consistent error reporting across all validation methods, following security best practices for not revealing specific attack vectors.

4. **Extension Detection Edge Cases**: Enhanced the extension detection algorithm to handle more complex edge cases including:
   - Proper handling of Unicode normalization to prevent homoglyph attacks
   - Robust detection of extensions in filenames with consecutive dots
   - Validation of extension content to prevent bypass attempts using control characters
   
5. **Cross-Platform Path Separator Handling**: Improved handling of both forward slashes and backslashes as path separators to ensure consistent behavior across different operating systems.

6. **UNC Path Handling**: Enhanced ReservedNameHandler to properly handle UNC paths (Universal Naming Convention paths starting with `\\` or `//`) by implementing specialized parsing logic that preserves the UNC structure while still processing the filename component.

7. **Surrogate Pair Handling**: Improved SafeSubstring method to prevent splitting Unicode surrogate pairs (used for emojis and other characters outside the Basic Multilingual Plane), ensuring character integrity.

8. **Integer Overflow Protection**: Added protection against integer overflow and underflow in path traversal depth calculations to prevent potential security vulnerabilities.

9. **Path Traversal Security**: Enhanced validation logic to distinguish between legitimate uses of `.` and `..` segments and malicious path traversal attempts, allowing safe relative paths while blocking dangerous traversal patterns.

10. **Security Requirements Validation**: Added comprehensive validation to ensure filenames meet all security requirements after processing, preventing invalid states that could lead to vulnerabilities.

11. **Soil Health Persistence Integration**: Added comprehensive soil health data persistence system with:
    - `SoilHealthState` model for data serialization
    - `ISoilHealthService` and `SoilHealthService` for managing soil health values
    - Integration with `ModController` to handle `SaveLoaded` and `Saving` events
    - Thread-safe caching and data access
    - Robust error handling for corrupted save data
