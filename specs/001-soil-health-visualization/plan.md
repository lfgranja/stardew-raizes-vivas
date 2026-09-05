# Implementation Plan: Soil Health Visualization

**Branch**: `001-soil-health-visualization` | **Date**: 2026-09-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-soil-health-visualization/spec.md`

## Summary

Implement a soil health visualization system that renders color-coded overlays on tilled soil tiles, displays hover tooltips with health percentages, and provides hoe action feedback. The system uses MonoGame's SpriteBatch for rendering, integrates with the existing soil health save/load system, and supports configurable visualization settings persisted as JSON.

## Technical Context

**Language/Version**: .NET 6 with C# latest language version

**Primary Dependencies**: SMAPI (Stardew Modding API), MonoGame/XNA (SpriteBatch rendering), Microsoft.Xna.Framework (Vector2, Color)

**Storage**: JSON file for visualization configuration (via IModDataService), existing soil health save system for tile data

**Testing**: xUnit with Moq, ThreadSafeGameLoopEventsStub for async event testing

**Target Platform**: Stardew Valley mod (Windows, Linux, macOS via SMAPI)

**Project Type**: Game mod (desktop application extension)

**Performance Goals**: 60 FPS (16.67ms per frame) with up to 1,000 visible tilled tiles

**Constraints**: Viewport culling required, cache invalidation on game state changes, async-only concurrency pattern, thread-safe rendering

**Scale/Scope**: Visualization overlay system for tilled soil tiles across all game locations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | Visualization domain models (VisualizationConfiguration, ColorMapping, TileOverlay) placed in Domain/. Services implement interfaces in Services/. Controller handles game events. |
| II. Security-First Data Handling | PASS | Configuration file path validated via existing FileNameSanitizationService. JSON deserialization uses bounded types. No user-input paths without sanitization. |
| III. Async-Only Concurrency | PASS | I/O-bound operations (config file load, save/load data persistence) use async/await. Game-loop-thread coordination (render state snapshots, atomic config swaps, pause/resume flags) uses lock-free atomic synchronization (Interlocked, Volatile.Read) — async/await is not applicable to same-thread frame coordination. No .Result or .Wait() calls on I/O paths. |
| IV. Test-Driven Development | PASS | Tests will be written before implementation. xUnit + Moq for unit tests. ThreadSafeGameLoopEventsStub for event testing. |
| V. Simplicity and YAGNI | PASS | No premature abstractions. Only interfaces needed: IVisualizationService, IVisualizationConfigurationService, IColorInterpolationService. |

**Gate Result**: PASS — No violations detected.

### Post-Design Re-evaluation (Phase 1 Complete)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | All domain models (VisualizationConfiguration, ColorMapping, TileOverlay, HoeFeedback, HealthCategory) defined in Domain/Visualization/. Interfaces (IVisualizationService, IVisualizationConfigurationService, IColorInterpolationService) in Domain/. |
| II. Security-First Data Handling | PASS | Configuration loaded/saved via IModDataService with existing sanitization. No new file paths introduced. |
| III. Async-Only Concurrency | PASS | I/O-bound operations (config file load, save/load data persistence) use async/await. Game-loop-thread coordination uses lock-free atomic synchronization (Interlocked, Volatile.Read). Event handlers follow existing sync pattern for game-loop coordination; async/await used only for true I/O. |
| IV. Test-Driven Development | PASS | Tests to be written before implementation per project convention. |
| V. Simplicity and YAGNI | PASS | Only three interfaces created, matching exact feature requirements. No speculative abstractions. |

**Re-evaluation Result**: PASS — Design aligns with all constitutional principles.

## Project Structure

### Documentation (this feature)

```text
specs/001-soil-health-visualization/
├── plan.md              # This file ($speckit-plan command output)
├── research.md          # Phase 0 output ($speckit-plan command)
├── data-model.md        # Phase 1 output ($speckit-plan command)
├── quickstart.md        # Phase 1 output ($speckit-plan command)
├── contracts/           # Phase 1 output ($speckit-plan command)
└── tasks.md             # Phase 2 output ($speckit-tasks command - NOT created by $speckit-plan)
```

### Source Code (repository root)

```text
LivingRoots/
├── Domain/
│   ├── Visualization/
│   │   ├── VisualizationConfiguration.cs    # Configuration model
│   │   ├── ColorMapping.cs                   # Health-to-color mapping
│   │   ├── TileOverlay.cs                    # Overlay render data
│   │   ├── HoeFeedback.cs                   # Transient feedback state
│   │   └── HealthCategory.cs                # Poor/Moderate/Healthy enum
│   ├── IVisualizationService.cs             # Visualization render interface
│   ├── IVisualizationConfigurationService.cs # Config load/save interface
│   └── IColorInterpolationService.cs        # Color interpolation interface
├── Services/
│   ├── Visualization/
│   │   ├── VisualizationService.cs          # Main render orchestrator
│   │   ├── VisualizationConfigurationService.cs # JSON config persistence
│   │   └── ColorInterpolationService.cs     # Linear RGB interpolation
│   └── (existing services unchanged)
├── Controllers/
│   └── (ModController extended with visualization event handlers)
└── Constants.php                             # Extended with visualization defaults

LivingRoots.Tests/
├── Visualization/
│   ├── VisualizationServiceTests.cs
│   ├── VisualizationConfigurationServiceTests.cs
│   ├── ColorInterpolationServiceTests.cs
│   └── ModControllerVisualizationTests.cs
└── (existing tests unchanged)
```

**Structure Decision**: Follows existing DDD layered architecture pattern. Domain models and interfaces in Domain/, implementations in Services/, event handling in Controllers/. New Visualization subdirectories keep feature code organized.

## Complexity Tracking

No constitution violations to track.
