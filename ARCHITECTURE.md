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
        ``````csharp
        public override void Entry(IModHelper helper)
        {
            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest);
            
            // Register events through the controller
            _controller.RegisterEvents();
        }
        ```
        ```
        

2. **`Domain/` (Pure Business Logic)**
    *   Planned folder for classes that will encapsulate the mod's core logic, directly related to agroecology concepts.
    *   These classes will be **pure**, meaning they will not directly interact with the game (SMAPI) or data persistence.
    *   They will be highly testable via TDD, as they will have no complex external dependencies.
    *   Planned to contain `IModLogic.cs` interface for future domain logic implementations.

3.  **`Services/` (SMAPI Interaction and Persistence)**
    *   Responsible for interacting with SMAPI APIs to:
        *   Persist and load mod data (e.g., `helper.Data.WriteJsonFile`).
        *   Access game information (locations, tiles, objects).
        *   Provide interfaces for other components to access data abstractly (following the I of SOLID principle).
    *   `ModDataService.cs` (implementation) and `IModDataService.cs` (interface) handle generic mod data saving/loading.

4. **`Controllers/` (Event Management and Interaction)**
    *   Responsible for listening to game events (via `helper.Events`) and reacting to them, orchestrating calls to domain logic and services.
    *   `ModController.cs` is the main controller that handles game events like `GameLaunched`.

5. **`Models/` (Data Classes)**
    *   Simple classes (POCOs - Plain Old C# Objects) that represent the structure of mod data.
    *   Used for serialization/deserialization of data and for passing information between components.
    *   This directory is planned for future use to centralize data models.
    *   Currently, data models are embedded within services or domain classes as needed.

## Data and Control Flow (Example: Soil Health)

1.  **`ModEntry`** initializes `ModController`, injecting dependencies like `IModHelper`, `IMonitor`, and `IManifest`.
2.  **`ModController`** listens to the `GameLoop.GameLaunched` event from SMAPI.
3.  When the game launches, `ModController`:
    *   Registers console commands (e.g., `lr_version`).
    *   Logs the successful loading of the mod.
4.  **`ModDataService`** handles data persistence operations when needed by controllers or domain logic.
5.  The architecture ensures that business logic is independent of the game and UI, making it easier to test and maintain. Interaction with the game is encapsulated in services and controllers, minimizing coupling.