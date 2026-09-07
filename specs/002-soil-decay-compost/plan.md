# Implementation Plan: Soil Health Decay + Compost Restoration

**Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-soil-decay-compost/spec.md`

## Summary

This feature implements the core agroecology gameplay loop: tilled soil decays when left bare (the "problem") and players restore it using compost produced in a composting Bin machine (the "solution"). The decay system processes all tilled tiles at day start with seasonal multipliers. The composting bin is a keg-style crafting machine that converts organic waste into compost over 2 days, with a maturation mechanic that rewards continuous use. All player-facing strings use full I18n localization from v1.

## Technical Context

**Language/Version**: .NET 6, C# latest language version

**Primary Dependencies**: SMAPI (Stardew Modding API), Pathoschild.Stardew.ModBuildConfig, Stardew Valley game API

**Storage**: IModDataService for JSON save data (soil health tiles, composting bin state, maturation levels)

**Testing**: xUnit with Moq, ThreadSafeGameLoopEventsStub for async event testing

**Target Platform**: Stardew Valley via SMAPI (Windows/Linux/macOS, single-player)

**Project Type**: Stardew Valley mod (desktop game mod, DLL plugin)

**Performance Goals**: Process up to 10,000 tilled tiles in <5ms per day start event. No spatial partitioning required.

**Constraints**:
- Async-only concurrency (no `.Result` or `.Wait()`)
- All constants from ModConstants (no hardcoded values)
- Single-player only (no multiplayer sync)
- Bounds checking: soil health clamped 0-100
- Security-first: tile ownership validation, input sanitization

**Scale/Scope**: Up to 10,000 tilled tiles per farm, multiple composting bins per farm operating independently

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | Layered architecture preserved: Domain (interfaces, models) → Services (implementations) → Controllers (event handlers). Ubiquitous language used throughout. |
| II. Security-First Data Handling | PASS | Tile ownership validation (farm + Greenhouse only), bounds checking on health values, CheckForOverflowUnderflow enabled. |
| III. Async-Only Concurrency | PASS | All I/O via async/await. Day start event handler uses SMAPI's async event pattern. No blocking calls. |
| IV. Test-Driven Development | PASS | Tests written before implementation. One test file per subject: `{Subject}Tests.cs`. |
| V. Simplicity and YAGNI | PASS | Uses existing SMAPI/Stardew APIs. Single waste processing per bin (no queue). Automate compatibility via standard patterns (no custom code). |

## Project Structure

### Documentation (this feature)

```text
specs/002-soil-decay-compost/
├── plan.md              # This file ($speckit-plan command output)
├── research.md          # Phase 0 output ($speckit-plan command)
├── data-model.md        # Phase 1 output ($speckit-plan command)
├── quickstart.md        # Phase 1 output ($speckit-plan command)
├── contracts/           # Phase 1 output ($speckit-plan command)
├── assets/              # Phase 1 output (asset briefs for sprites, textures, audio)
└── tasks.md             # Phase 2 output ($speckit-tasks command - NOT created by $speckit-plan)
```

### Source Code (repository root)

```text
LivingRoots/
├── Domain/
│   ├── Interfaces/
│   │   ├── ISoilDecayService.cs        # Decay calculation interface
│   │   ├── ICompostingBinService.cs    # Composting bin operations interface
│   │   └── ICompostApplicationService.cs # Compost application interface
│   ├── Models/
│   │   ├── CompostingBinState.cs       # Bin state entity (Empty, Processing, Ready)
│   │   ├── CompostingBinMaturation.cs  # Maturation tracking entity
│   │   └── OrganicWasteDefinition.cs   # Valid waste categories/tags
│   └── Services/
│       ├── OrganicWasteValidator.cs    # Validates items against categories + tags
│       └── SeasonalDecayMultiplier.cs  # Season-based multiplier lookup
├── Services/
│   ├── SoilDecayService.cs             # Day start decay calculation implementation
│   ├── CompostingBinService.cs         # Machine logic, maturation, persistence
│   ├── CompostApplicationService.cs    # Right-click compost application
│   └── CompostingBinFactory.cs         # Creates bin instances, manages IDs
├── Controllers/
│   └── CompostingBinController.cs      # Handles machine interaction events
└── Constants.cs                        # Extended with decay/compost constants

LivingRoots.Tests/
├── SoilDecayServiceTests.cs
├── CompostingBinServiceTests.cs
├── CompostApplicationServiceTests.cs
├── OrganicWasteValidatorTests.cs
└── CompostingBinStateTests.cs
```

**Structure Decision**: Extends the existing DDD layered architecture. New interfaces go in `Domain/Interfaces/`, new models in `Domain/Models/`, service implementations in `Services/`, and a new controller for machine interactions. This follows the established pattern from the existing `SoilHealthService` and `ModController`.

## Complexity Tracking

> No Constitution Check violations. No complexity justifications needed.

---

## Phase 0: Research & Unknowns

*See [research.md](./research.md) for detailed findings.*

### Research Outcomes

| Unknown | Resolution | Source |
|---------|------------|--------|
| Stardew Valley machine implementation pattern (Keg/Preserves Jar) | Use `Data/Machines` with `IMachineActions` behavior. Standard output production via `OutputFactory`. | Stardew Valley Wiki + existing mod patterns |
| Tilled soil detection via game API | Check `terrainFeature is HoeDirt` with `crop == null` for bare state. Dead crop residue: `crop != null && crop.dead`. | Decompiled game code via SMAPI docs |
| Seasonal multiplier timing | Apply at day start before decay calculation. Use `Game1.currentSeason` for current season. | Stardew Valley game loop docs |
| IModDataService save format | JSON serialization per save file. Key by location name + tile coordinates. | Existing SoilHealthService implementation |
| Automate mod compatibility | Standard `Data/Machines` pattern is automatically compatible. No custom code needed. | Automate mod documentation |
| Compost item creation | Use `ItemRegistry.Create<Object>` with unique mod item ID. Category: -26 (fertilizer-like). | Stardew Valley item system docs |

## Phase 1: Design & Contracts

### Data Model

*See [data-model.md](./data-model.md) for full entity definitions, fields, relationships, validation rules, and state transitions.*

### Interface Contracts

*See [contracts/](./contracts/) for machine interaction contracts and save data schemas.*

### Quickstart Validation

*See [quickstart.md](./quickstart.md) for runnable validation scenarios.*

### Asset Briefs

*See [assets/](./assets/) for sprite, texture, and audio specifications.*

---

## Phase 0-1 Research Summary

### Key Technical Findings

1. **Machine Implementation**: Stardew Valley machines use `Data/Machines` data file with behavior classes. The Composting Bin follows the Keg pattern: accepts input, processes over time, produces output. Maturation is a custom extension not present in vanilla machines.

2. **Save/Load Persistence**: Standard SMAPI pattern uses `ISpaceCore` or custom `IModDataService` with JSON serialization. Each composting bin state is keyed by location + tile position.

3. **Day Start Event**: `IDayStartedWritableAPI` fires once per day. All decay calculations happen synchronously within this event (not per-frame).

4. **Input Handling**: `IInputEvents.ButtonPressed` detects right-click. Must check player is holding compost item and target tile is valid before applying.

5. **Item System**: New items (compost, composting bin object) require unique IDs registered via `ItemRegistry`. The composting bin machine uses `Data/ObjectInformation` for its placement behavior.

### Constitution Re-Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | New interfaces in Domain/, implementations in Services/, controllers in Controllers/. |
| II. Security-First Data Handling | PASS | Tile ownership validated. Health bounds enforced. Overflow checking preserved. |
| III. Async-Only Concurrency | PASS | No blocking calls in event handlers. |
| IV. Test-Driven Development | PASS | Test files planned for all new services. |
| V. Simplicity and YAGNI | PASS | Standard machine patterns reused. No premature abstractions. |
