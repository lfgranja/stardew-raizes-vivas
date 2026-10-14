# Implementation Plan: Playable Compost Loop

**Branch**: `004-playable-compost-loop` | **Date**: 2026-10-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-playable-compost-loop/spec.md`

## Summary

Deliver the complete, playable composting and soil health loop in Stardew Valley 1.6:
1. Programmatically register Compost item (`LivingRoots.Compost`), Composting Bin machine (`LivingRoots.CompostingBin`), and recipe (`LivingRoots.CompostingBinRecipe`) via SMAPI `AssetRequested` against `Data/Objects`, `Data/BigCraftables`, `Data/CraftingRecipes`, and `Data/Machines`.
2. Support native machine behaviors and machine state lifecycle (Empty → Processing → Ready) driven by 2 in-game days maturation (`ModConstants.MaturationDays = 2`), single compost batch yield (retiring 1-5 multiplier per FR-005a), and progress retention across machine removal/pickup into inventory `Item.modData` (FR-005c) with tolerant reader fault isolation (FR-012, FR-015).
3. Connect compost application (consuming 1 item, adding +15 health clamped to 100 on tilled `HoeDirt` with or without active crop) with localized native refusal alerts (`Game1.showRedMessage` via SMAPI `ITranslationHelper` in `i18n/default.json`).
4. Re-render visual soil health overlays across the 3-tier RGB spectrum with neutral dirt for 0 health, responding immediately (≤16.67ms) to `ISoilHealthService.OnTileHealthChanged` reactive events.
5. Reconcile public documentation in `README.md` (`(Planned)` labels on unbuilt features per FR-016) and update stale multiplayer scope statements in the 8 prior spec/doc files per FR-017 and Principle VI.

## Technical Context

**Language/Version**: .NET 6 with C# latest language version

**Primary Dependencies**: SMAPI (Stardew Modding API 4.x), Pathoschild.Stardew.ModBuildConfig 4.4.0, Microsoft.Xna.Framework (MonoGame), FsCheck / xUnit for property-based and unit testing

**Storage**: Stardew Valley save system via `IModDataService` (`composting_bins_{saveId}`, `soil_health_data_{saveId}`), `Item.modData` for inventory item progress serialization

**Testing**: xUnit, Moq, FsCheck (property-based testing for invariants), `ThreadSafeGameLoopEventsStub` for SMAPI event loops and concurrent action simulations

**Target Platform**: Stardew Valley 1.6 mod on SMAPI (cross-platform desktop: Linux, macOS, Windows)

**Project Type**: SMAPI mod (desktop game plugin)

**Performance Goals**: 60 FPS (16.67ms per frame) overlay invalidation and rendering; zero polling lag on tile health mutation; <5ms day-start machine batch and decay updates

**Constraints**:
- Multiplayer as first-class requirement (Principle VI, FR-018, FR-019): host-authoritative atomic state locking per tile/machine instance; first-wins semantics.
- Async-only for I/O; synchronous snapshot-diff and atomic synchronization for game-tick render/state frames.
- Security-first: sanitize save inputs, no raw exception messages in logs (`ex.GetType().FullName` + `HResult` at `LogLevel.Trace` only).
- Bounds: soil health clamped to 0–100 (`ModConstants.MinSoilHealth` and `MaxSoilHealth`).
- Zero orphaned bin state: tolerant reader with per-entry isolation and orphaned entity pruning.

**Scale/Scope**: Farm and Greenhouse maps, up to 10,000 tilled tiles, multiple composting bins operating concurrently.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-Driven Design | PASS | Domain models (`CompostingBinStateModel`, `CompostingBinData`, `SoilHealthData`) separated from `Services/` and `Controllers/`. Ubiquitous language preserved. |
| II. Security-First Data Handling | PASS | Tolerant reader quarantines corrupted records per entity boundary (FR-012, FR-014, FR-015); exception details sanitized; bounds checking on 0–100 health. |
| III. Async-Only Concurrency | PASS | I/O-bound persistence uses async/await; game tick events and thread safety use atomic snapshot-diff / locks; no `.Result` or `.Wait()`. |
| IV. Test-Driven Development | PASS | Test tasks written and blocking implementation; property-based invariant tests with FsCheck / xUnit; unit & concurrency stubs. |
| V. Simplicity and YAGNI | PASS | Native machine UI and Stardew `Data/Machines` / `Game1.showRedMessage` utilized; no bespoke UI components or text drawing systems. |
| VI. Multiplayer As A First-Class Requirement | CONDITIONAL PASS | Design rules (host-authoritative, first-wins, farm-keyed) implemented + stubs; Gate 3 live 2-player **deferred to D1** (debt C1). Single-player is a supported degenerate case; multi-live is not verified here. |

**Gate Result**: PASS with one documented conditional — Principle VI (multiplayer) live verification is deferred to roadmap D1 (debt); no MUST violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/004-playable-compost-loop/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan
├── checklists/          # Requirements and quality checklists
└── tasks.md             # Execution task list
```

### Source Code (repository root)

```text
LivingRoots/
├── ModConstants.cs                             # ModConstants: IDs, maturation, recipes, colors
├── ModEntry.cs                                 # Composition root & AssetRequested handler (Data/Objects, Data/BigCraftables, Data/CraftingRecipes, Data/Machines)
├── i18n/
│   └── default.json                            # Translation keys for player refusal messages (FR-008a)
├── Domain/
│   ├── Interfaces/
│   │   ├── ICompostingBinService.cs            # Bin lifecycle, pickup progress, and batch processing contracts
│   │   ├── ICompostApplicationService.cs       # Application contracts with crop tolerance & refusal reasons
│   │   └── ISoilHealthService.cs               # Soil health contracts + OnTileHealthChanged event
│   └── Models/
│       ├── CompostingBinStateModel.cs          # Runtime machine state (Empty→Processing→Ready, modData serialization)
│       └── CompostingBinData.cs                # Persistence DTO (save/load, tolerant reader)
├── Services/
│   ├── CompostingBinService.cs                 # Machine lifecycle, 1-out yield, progress preservation, tolerant reader
│   ├── CompostApplicationService.cs            # Application execution, validation, inventory decrement
│   ├── SoilHealthService.cs                    # Reactive OnTileHealthChanged notifications, bounds clamp
│   └── Visualization/
│       └── VisualizationService.cs             # Reactive cache invalidation, tier interpolation, neutral baseline
└── Controllers/
    └── ModController.cs                        # Input, right-click, placement, dislodge hooks, refusal HUD alerts

LivingRoots.Tests/
├── Fixtures/                                   # Real game objects & test factories
├── Stubs/                                      # Event & provider stubs
├── Invariants/                                 # Property-based tests (FsCheck); shared fixture
├── CompostingBinDataTests.cs                   # Tolerant-reader persistence DTO tests (RED T000-R1)
├── CompostingBinStateModelTests.cs             # State-machine & lifecycle tests (RED T000-R2)
├── SoilHealthServiceTests.cs                   # Bounds / clamp / reactive event tests (RED T000-R3)
├── CompostApplicationServiceTests.cs           # Application, consumption & crop coexistence tests (RED T000-R4)
├── MultiplayerConcurrencyTests.cs              # Simulated concurrent action stubs (RED T000-R5)
├── VisualizationServiceTests.cs                # Overlay render-safety & tier tests (RED T000-R6)
└── DailyDecayTests.cs                          # Once-per-day decay & seasonal rules (RED T000-R7)
```
