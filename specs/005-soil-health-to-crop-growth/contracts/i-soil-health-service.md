# Contracts — Soil Health → Crop Growth (Feature 005)

**Feature**: `005-soil-health-to-crop-growth` | **Project Type**: SMAPI mod (desktop game plugin — internal only, no external interfaces exposed to users or other systems)

**Note**: This feature is purely internal. No public APIs, command schemas, endpoint contracts, or grammars are exposed externally. The contracts documented here are the internal domain/service interface contracts used by the feature.

---

## Domain Interface Contracts

### `ISoilHealthService` (`Domain/Interfaces/ISoilHealthService.cs`)

**Purpose**: Per-tile soil health query and persistence layer. Shared by all players (multiplayer — host-authoritative per tile; SMAPI location sync ensures clients observe same value).

**Operations** (contract basis — implementation details belong in plan/tasks):
- Load soil health for a location (`LocationName`) → `SoilHealthState` (sparse dictionary)
- Query health value for a tile (`TileKey`) → `HealthValue` (`int`, clamped `0-100`)
- Update health value (`HealthValue` → `HealthCategory` derived) — with `HostAtomicLockService` per-tile guard (`FR-006` 79; `A5` 90)
- Observe health changes (`OnTileHealthChanged` event) — clients observe updates via SMAPI native sync (`FR-006` 79; `Clarifications` Q1 22)

**Constraints** (from spec / clarification loop):
- `HealthValue` clamped `0-100`; `NaN`/`±Inf` guarded (`FR-007` 80)
- `TileKey` (`x`, `y`) must be valid within `LocationName` bounds; `LocationName` sanitized (`FormC` normalization, `SanitizePathSegments` — `Principle II` security-first)
- Sparse persistence: only non-default health values stored (`AGENTS.md` anti-pattern: sparse save; `SoilHealthService` existing pattern from spec 002)
- Async-only for I/O (`Principle III`); `.Result`/`.Wait()` forbidden

**Multiplayer contract** (`Principle VI` — non-negotiable):
- Persistent growth-phase (`FR-001` — `DayStarted` modifier) executes host-only (`IsMainPlayer` / `Context.IsMainPlayer`)
- Harvest-quality rolls (`FR-002`) and extra-yield rolls (`FR-003`) are discrete events computed locally by harvesting player from shared `SoilHealthService` tile health — no new transport message (`A5` 90; `FR-006` 79)
- `ReGen Valley` interaction (`Assumes` 145; user B — concurrent prerequisite): first-wins host-authoritative per tile; second mutation re-evaluated against new state; refused with message (inventory intact) if no longer valid

### `ICompostApplicationService` (`Domain/Interfaces/ICompostApplicationService.cs`)

**Purpose**: Compost application to tilled tiles. Existing interface from spec 004 — rollback verification (`SC-008`) is an **absolute blocker for yield activation** (`FR-002`/`FR-003`); clarification 2026-10-16 (Option A) overrides previous concurrent (`B`) selection — design/implementation permitted concurrently; activation blocked until `SC-008` passes (`manual GamePath` + `StillConsumesBin`).

**Verification prerequisite** (`SC-008` — absolute blocker for yield activation; concurrent design/implementation permitted; clarification 2026-10-16 overrides previous `B` selection):
- `CompostingBinService.CollectCompost` rollback must pass (`manual GamePath` play-through + automated `StillConsumesBin` — `CompostingBinServiceTests.cs` 339)
- Hidden duplication bug (`outputCount > 1` with partial-delivery loop at `CompostingBinService.cs` 189-264) must be fixed or verified before final yield activation (`FR-005` 78)
- Clarification 2026-10-16 Option A: design/implementation concurrent (`user B`); **yield activation blocked until `SC-008` passes (`manual GamePath` + `StillConsumesBin`)** — absolute blocker for `FR-002`/`FR-003`.

**Rollback contract** (`FR-005` — concurrent design permitted; absolute blocker for activation until `SC-008` green):
- `CompostingBinService` must handle full-inventory collection correctly (`FR-005b` / `FR-005c`)
- Partial delivery (`outputCount > 1`) must not cause duplication; rollback mechanism must exist
- `ModController.cs` rollback patterns (566-598) cover event unregistration but **not** inventory-full partial delivery (`DEFERRED_SC008_EXPLANATION.md` evidence) — verification gate (`SC-008`) must confirm before activation

---

## External Integration Contracts

**None exposed.** This feature is an internal SMAPI mod plugin. Integration points (existing, not new contracts):
- SMAPI event loop (`ModController` — `OnDayStarted`, `OnButtonPressed`, `OnRenderedWorld`): event handler contracts defined by SMAPI framework (`Game1.event.subscribe` patterns)
- SMAPI persistence (`IModDataService`): contract defined by SMAPI framework (`ModDataService` existing from specs 002/004)
- SMAPI multiplayer sync (`GameLocation.terrainFeatures`): contract defined by SMAPI native sync mechanism (`FR-006` 79; `A5` 90)
- Stardew Valley vanilla objects (`Crop`, `HoeDirt`, `Data/Crops`): contracts defined by vanilla game (`Crop.phaseDays`, `Data/Crops.ExtraHarvestChance` — referenced only as field concepts, not edited in this spec; `FR-003` 76; `Non-Goals` 122-123)

---

## Contract Quality / Verification

- All contracts reference existing interfaces (`ISoilHealthService`, `ICompostApplicationService`) — no new interfaces invented (`AGENTS.md` anti-pattern: YAGNI enforced)
- Contracts reference existing patterns (`HostAtomicLockService`, `VisualizationService.cs` floating-text, `CompostApplicationService.cs` `TemporaryAnimatedSprite` construction) — no premature abstractions
- No new `Data/` edits (`Non-Goals` 122): no new `Data/Crops` entry, no new `Assets/*.png`, no new `i18n` key (`A4` 89; `Clarifications` Q3/A4 33-34)
- `SC-008` verification concurrent (`B` selected by user): contract update requires `SC-008` green before final yield activation, but design/implementation may proceed concurrently
