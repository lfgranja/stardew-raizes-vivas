# LivingRoots Architecture

This document outlines the architecture of the LivingRoots mod for Stardew Valley, following Domain-Driven Design (DDD) principles and the Dependency Inversion Principle (DIP).

## Architecture Layers

The mod follows a layered architecture with clear separation of concerns:

### Domain Layer (`LivingRoots/Domain`)
- Contains domain models, interfaces, and business logic
- Defines the core concepts of the mod using domain-driven design
- Contains interfaces that abstract external dependencies (e.g., `IModDataService`)
- Implements domain services with business rules (e.g., soil health clamping to 0-100 range)

### Services Layer (`LivingRoots/Services`)
- Implements application services that orchestrate domain logic
- Contains concrete implementations of domain interfaces
- Handles cross-cutting concerns like data validation and sanitization
- Implements the soil health persistence mechanism using SMAPI's data system

### Controllers Layer (`LivingRoots/Controllers`)
- Handles SMAPI events and game lifecycle
- Coordinates between game events and application services
- Implements command pattern for in-game commands
- Acts as the boundary between the game engine and the mod's business logic

### Entry Point (`ModEntry.cs`)
- Bootstraps the application and registers services
- Implements SMAPI's `Mod` interface
- Handles dependency injection through constructor parameters

## Design Patterns

### Dependency Inversion Principle (DIP)
- High-level modules depend on abstractions, not concretions
- Domain layer defines interfaces that services layer implements
- Enables loose coupling and testability

### Service Layer Pattern
- Encapsulates application logic in service classes
- Provides a clear API for controllers to interact with business logic
- Handles data validation, transformation, and persistence

### Domain-Driven Design (DDD)
- Models the problem domain using domain entities and value objects
- Uses ubiquitous language throughout the codebase
- Separates domain logic from infrastructure concerns

## Data Flow

1. Game events are captured by controllers
2. Controllers delegate to application services
3. Services coordinate domain objects and persistence
4. Domain rules are enforced throughout the process
5. Data is persisted using SMAPI's data system

## Soil Health Implementation

The soil health feature implements the following architecture:

### SoilHealthState Model
- Represents the persisted state of soil health for the entire save
- Key: Location Name (e.g., "Farm")
- Value: Dictionary mapping Tile Coordinates "X,Y" to Health Value (float)

### ISoilHealthService Interface
- Defines the contract for soil health operations
- Located in the Domain layer to maintain separation of concerns

### SoilHealthService Implementation
- Implements the soil health business logic
- Handles data validation, sanitization, and persistence
- Uses thread-safe operations with lock mechanisms
- Implements temporary cache pattern to prevent data loss during load failures
- Validates coordinates and clamps values to [0, 100] range

### Integration with Game Events
- Hooks into SMAPI's SaveLoaded and Saving events
- Ensures data is loaded on game load and saved on game save
- Maintains data integrity across game sessions

## Security Considerations

- Input validation and sanitization at all layers
- Path traversal prevention using validation services
- File name sanitization to prevent invalid characters
- Proper encoding and normalization of user input
- Secure logging practices to prevent information disclosure

## Testing Strategy

- Unit tests for all domain services
- Integration tests for data persistence
- Mocking of external dependencies for isolated testing
- Test coverage of edge cases and error conditions

## Conventions

- Follows Stardew Valley Modding API (SMAPI) conventions
- Uses semantic versioning for releases
- Implements conventional commits for version control
- Maintains comprehensive documentation for all components
