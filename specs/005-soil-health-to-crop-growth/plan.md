# Implementation Plan: Soil Health → Crop Growth

**Branch**: `feat/005-soil-health-to-crop-growth` | **Date**: 2026-10-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-soil-health-to-crop-growth/spec.md`

---

## Summary

Connect the existing soil-health measurement system (per-tile 0–100, persisted, visualized) to observable gameplay effects on crop growth and harvest outcomes. This is the mechanism gap that makes the 21 unimplemented roadmap mechanics (mulch, rotation, polyculture, green manure, animal management, pest control, bio-construction, ICLF, greywater, CSA, collective kitchen, food sovereignty) non-functional: without a mechanism that changes gameplay from soil state, modifying soil health produces zero player-observable change.

Key deliverables (all design-locked — i.e. specification, plan, data-model and contracts are settled with zero `NEEDS CLARIFICATION` markers and no unresolved open questions; execution deferred to implementation tasks):
1. **Growth modifier (`FR-001`)**: At `DayStarted`, apply `HealthCategory` multiplicative multiplier to `Crop.phaseDays` (base phase duration, before vanilla modifiers): Poor = 1.15× slower (`ModConstants.SoilGrowthPoorMultiplier`), Moderate = 1.00× (vanilla parity, `SC-003`), Healthy = 0.85× faster (`ModConstants.SoilGrowthHealthyMultiplier`). Host-only (`IsMainPlayer` / `Context.IsMainPlayer`) via `HostAtomicLockService` per-tile guard; no `Harmony` dependency (Option A — `Crop.newDay` public-field scan); `GamePath` spike gate required before activation.
2. **Harvest quality (`FR-002`)**: At `Crop.harvest`, incorporate tile health additively to vanilla roll `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01`: Poor forces 0 stars (override permitted only for Poor tier); Moderate = vanilla; Healthy adds `+0.08` (`SoilQualityHealthyBoost`). No replacement of vanilla roll (`A2` compositional consistency).
3. **Extra harvest (`FR-003`)**: On Healthy-tier (`≥71`) tiles, increase probability additively: `extraProb = base + 0.10` (`SoilExtraHarvestHealthyBoost`). Per-tile at harvest time; never globally per seed.
4. **Performance (`FR-001` / Q7)**: Linear scan default (`SoilDecayService.cs:86`); contingency = combined chunk mechanism (`~1000` tiles/chunk, `~2ms` budget, prime-modulus stride `61`, spread across ticks when linear exceeds `<5ms` at 40,000 tiles; `ExecuteWithConcurrencyGuard` at `OnDayStarted`). Design locked; zero chunk mechanism exists in code (`subagent-response-q7.md`); deferred to tasks.
5. **Harvest feedback (`A4` / Q3)**: Reuse existing `floating-text` / `TemporaryAnimatedSprite` path (`VisualizationService.cs:247-285`; `CompostApplicationService.cs:178-191`); no new `Assets/*.png`, no new `i18n` key. New `Crop.harvest` hook deferred to tasks (zero harvest hooks exist today; `ModController` zero harvest references beyond `message.harvest.inventory-full` at `Constants.cs:56`).
6. **Rollback gate (`SC-008` / clarification confirmed)**: **Absolute blocker for yield activation** (confirmation `spec.md`:28 — overrides previous concurrent `B` selection from 2026-10-15). `CompostingBinService.CollectCompost` rollback (`manual GamePath` play-through + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` (alias: `StillConsumesBin`) — `CompostingBinServiceTests.cs:339`) MUST pass before any yield feature activates (`FR-002`/`FR-003`; `FR-005`; `Dependencies` 136-137). Design/implementation of `Crop.newDay` / `Crop.harvest` hooks permitted concurrently; **activation** of yield effects blocked until `SC-008` green.
7. **ReGen Valley interaction (`A5` / Q5)**: Documentation-only — first-wins host-authoritative per tile (`HostAtomicLockService` / Principle VI); second concurrent mutation re-evaluated, refused with message (inventory intact); never last-write-wins (`Clarifications` 2026-10-15).

---

## Technical Context

**Language/Version**: .NET 6 with C# latest language version.

**Primary Dependencies**: SMAPI (Stardew Modding API 4.x), `Pathoschild.Stardew.ModBuildConfig` 4.4.0, `Microsoft.Xna.Framework` (MonoGame), xUnit / Moq for unit/integration testing.

**Storage**: Stardew Valley save system through `IModDataService` (`soil_health_data_{saveId}`); sparse dictionary (`SoilHealthState` — only non-default values stored); `ModDataService` sanitizes all `LocationName` and `TileKey` (`FormC` normalization, `SanitizePathSegments`). No external database.

**Testing**: xUnit + Moq; one test file per subject (`{Subject}Tests.cs`); `ThreadSafeGameLoopEventsStub` for concurrent event simulation; manual `GamePath` play-through is the dominant verification path (`FR-009` / `SC-008` / `SC-009`) — automation gap acknowledged explicitly (`SC-008` deferred prerequisite).

**Target Platform**: Stardew Valley 1.6 mod via SMAPI (cross-platform desktop: Linux, macOS, Windows).

**Project Type**: SMAPI mod (desktop game plugin — DLL); purely internal — no public APIs, command schemas, endpoint contracts, or grammars exposed externally. Internal contracts (`ISoilHealthService`, `ICompostApplicationService`) documented in `contracts/`.

**Performance Goals**: Linear scan ≤ `<5ms` at `DayStarted` for ≤ 40,000 tilled tiles (`spec-002` contract `:113`/`:176`; `SoilDecayService.cs:86` linear `foreach` over `terrainFeatures.Pairs`). Contingency activates above threshold (`Clarifications` Q7 / `subagent-response-q7.md`). Frame-rate target for overlay/render remains 60 FPS (`16.67ms`); this feature introduces no new render overhead (reuses existing `floating-text` path; `FR-008` prohibits pixel assertions).

**Constraints**:
- Multiplayer first-class (Principle VI, NON-NEGOTIABLE): host-authoritative (`Context.IsMainPlayer` / `IsMasterGame`); farm-scoped; single-player = host with no clients; no second source of truth; atomic per-tile lock (`HostAtomicLockService`); first-wins semantics.
- No `Lib.Harmony` dependency (Option A — `Crop.newDay` public-field scan; `AGENTS.md` anti-pattern: zero Harmony footprint; `LuckSkill` proves direct field access; `GamePath` spike gate verifies ordering before activation; B fallback only if ordering fails).
- Async-only (`Principle III`): `.Result` / `.Wait()` forbidden; `volatile` / `Interlocked` permitted for atomic flags; `ModEntry.Dispose()` uses lock + `_disposed` pattern; event handlers unregistered on disposal.
- Security-first (`Principle II`): all input paths sanitized (`SanitizePathSegments`); Unicode normalized (`FormC`); bounds-checked (`0-100`); `CheckForOverflowUnderflow` enabled project-wide; no raw exception messages to logs.
- Zero new assets / `i18n` (`A4`): reuse existing visual/feedback patterns; no `Data/Crops` edit; no `Assets/*.png`; no new `Data/Objects` / `Data/Recipe` / `Data/BigCraftables` / `Data/Characters` / `Data/Buildings` / `Data/Quests`.
- No new `ContentPatcher`; repo uses `AssetRequested` + `e.Edit` only; no `config.json` / GMCM page.
- Sparse save (`AGENTS.md` anti-pattern): only non-default health values stored.
- DDD layered architecture (`Principle I`): interfaces (`Domain/Interfaces/`) separated from implementations (`Services/`) and controllers (`Controllers/`); ubiquitous language (`SoilHealth`, `Compost`, `CropGrowth`, `LivingSoil`, `HealthCategory`).

**Scale/Scope**: Farm, Greenhouse, Ginger Island maps; up to 10,000 tilled tiles (target) / 40,000 extreme (`spec-002` contract); multiple concurrent composting bins (`spec-004`). Design covers only soil-health → crop-growth mechanism; broader scope (mulch, polyculture, green manure, animal management, pest control, bio-construction, ICLF, greywater, CSA, collective kitchen, food sovereignty) deferred to roadmap items that depend on this mechanism.

**Unknowns / Deferred** (resolved / confirmed in planning phase):
- No `NEEDS CLARIFICATION` markers remain in `spec.md` (checklist `requirements.md`: 8 of 9 clarification questions resolved; 1 deferred — `SC-008` rollback verification prerequisite from `spec-004`, not ambiguity; does not block plan creation, only yield activation).
- Deferred prerequisites (must resolve before final yield activation — not before design/implementation):
  1. `SC-008` rollback verification (`spec-004` `CompostingBinService.CollectCompost` — hidden duplication bug at `outputCount > 1`; `manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`); **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent `B` selection — design/implementation concurrent, activation blocked until green; `Dependencies` 136-137; `FR-005` 78).
  2. `GamePath` spike (`A1` 86; `Dependencies` 141) — verify `Crop.newDay` public-field ordering with vanilla growth before `FR-001` activation.
  3. `ReGen Valley` interaction (`Assumes` 145) — documentation-only (first-wins host-authoritative; no design change needed).
- Design deferred to plan/tasks (fully parameterized — not ambiguity): `Crop.newDay` public-field scan (`FR-001`); chunk mechanism (`FR-001` contingency — combined chunk, `~1000` tiles/chunk, `~2ms` budget, stride `61`, `ExecuteWithConcurrencyGuard` at `OnDayStarted`); harvest-intercept hook at `Crop.harvest` (reuse `floating-text`; zero new assets / `i18n`).

---

## Constitution Check (Pre-Phase 0)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution version: `2.0.0` (ratified `2026-09-05`; last amended `2026-10-13` — added `Principle VI`: Multiplayer As A First-Class Requirement; revoked v1.x single-player-only scope constraint).

| Principle | Status | Evidence / Notes |
|-----------|--------|------------------|
| I. Domain-Driven Design (NON-NEGOTIABLE) | PASS | Layered architecture preserved: `Domain/Interfaces/` (`ISoilHealthService.cs`, `ICompostApplicationService.cs`) → `Services/` (`SoilHealthService.cs`, `CompostApplicationService.cs`, `CompostingBinService.cs`) → `Controllers/` (`ModController.cs`). Ubiquitous language (`SoilHealth`, `Compost`, `CropGrowth`, `HealthCategory`, `LivingSoil`) used in interfaces, models, services, controllers, and documentation. `data-model.md` defines entities and relationships explicitly. |
| II. Security-First Data Handling (NON-NEGOTIABLE) | PASS | `ModDataService.SanitizePathSegments()` sanitizes `LocationName` and `TileKey`; `FormC` normalization enforced; health values clamped `0-100` (`ModConstants.MinSoilHealth` / `MaxSoilHealth`); `HealthValue_ClampedToMinMax` with `NaN`/`±Inf` guards (`FR-007` 80); `CheckForOverflowUnderflow=true` project-wide (cannot be suppressed); exception details only at `Trace` (`LogLevel.Trace`) — no raw exception messages to user-facing logs (`AGENTS.md` anti-patterns; `Principle II`). |
| III. Async-Only Concurrency (NON-NEGOTIABLE) | PASS | `DayStarted` growth scan uses non-blocking event handler (`ModController.OnDayStarted` 1719-1778); `ExecuteWithConcurrencyGuard` (`Interlocked.CompareExchange`) protects against re-entrancy; chunk mechanism (`~2ms`/chunk) spreads across ticks; no `.Result` or `.Wait()` on async calls (`AGENTS.md`); `ModEntry.Dispose()` uses lock + `_disposed` pattern; event handlers unregistered on disposal. |
| IV. Test-Driven Development | PASS | Red-Green-Refactor enforced: `SC-008` (`CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` automated test + manual `GamePath` play-through) is an absolute prerequisite for activation; design work permitted concurrently (`user B` selection — `Clarifications` 2026-10-16). `SC-001`–`SC-007` verification criteria defined with measurable targets (`SC-003` parity control; `SC-004` quality roll; `SC-005` extra harvest). `FR-009` requires manual verification (`SC-008`) — automation gap explicitly acknowledged (not hidden). Test stubs (`ThreadSafeGameLoopEventsStub`) simulate concurrent events. One test file per subject (`{Subject}Tests.cs`). `FR-008` prohibits pixel assertions. |
| V. Simplicity and YAGNI | PASS | Zero new interfaces invented (`A4`; contracts reference existing `ISoilHealthService` / `ICompostApplicationService`); harvest hook reuses `VisualizationService` floating-text pattern (no new abstract base); no new asset / `i18n` / `Data/` entry; `Crop.harvest` hook deferred to tasks (not invented prematurely); performance contingency (chunk mechanism) designed at specification level, deferred to implementation. Direct `IMonitor` acceptable when wrapper adds no value (`Principle V`). No expression-bodied methods (`.editorconfig` forbids). No premature abstractions (`AGENTS.md` anti-pattern). |
| VI. Multiplayer As A First-Class Requirement (NON-NEGOTIABLE, v2.0.0) | CONDITIONAL PASS | Design rules implemented + documented: host-authoritative persistent phase (`DayStarted` — `IsMainPlayer` / `Context.IsMainPlayer`; `HostAtomicLockService` per-tile guard; `FR-006` 79; `A5` 90); discrete harvest rolls (`FR-002`/`FR-003`) computed locally from shared `SoilHealthService` tile health — no new transport message; `ReGen Valley` interaction (`Assumes` 145) = documentation-only (first-wins host-authoritative; second mutation refused with message; inventory intact); `SC-008` rollback concurrent (`user B`). Live 2-player verification (`D1`) deferred to roadmap D1 (debt D1 — `Constitution` Governance §Migration Plan: amendment breaks no existing code). Single-player is the degenerate case of multiplayer (host with no clients) — no separate single-player mode. **No MUST violations**; conditional pass is documented and justified. |

**Gate Result**: PASS with one documented conditional — `Principle VI` live 2-player verification deferred to roadmap `D1` (`SC-008` concurrent verification + `GamePath` spike); no `MUST` violations detected. Design work (Phase 1) completed; yield activation blocked by `SC-008` verification gate (`FR-005`; `Dependencies` 136-137; `Clarifications` 2026-10-16) — this is an external prerequisite (`spec-004`), not a design ambiguity, and does not block plan creation.

---

## Project Structure

### Documentation (this feature — `specs/005-soil-health-to-crop-growth/`)

```text
specs/005-soil-health-to-crop-growth/
├── spec.md              # Feature specification (updated after clarification loop 2026-10-16)
├── plan.md              # This file ($speckit-plan output — design-locked; deferred prerequisites documented)
├── research.md          # Phase 0 output (all 9 clarification questions answered; 8 integrated; 1 deferred — SC-008)
├── data-model.md        # Phase 1 output (entities, relationships, validation rules, state transitions)
├── quickstart.md        # Phase 1 output (runnable validation scenarios US1–US4; SC-001–SC-008)
├── contracts/           # Phase 1 output (`i-soil-health-service.md` — internal domain/service contracts)
│   └── i-soil-health-service.md
├── checklists/          # Phase 1 verification artifacts
│   ├── band-scale-host-authority.md
│   ├── growth-performance.md
│   ├── harvest-quality-extra.md
│   ├── requirements.md
│   └── verification-pipeline.md
└── tasks.md             # Phase 2 output ($speckit-tasks — NOT created by $speckit-plan; deferred)
```

### Source Code (repository root — `LivingRoots/`)

```text
LivingRoots/
├── ModConstants.cs                  # Soil-growth multipliers (`SoilGrowthPoorMultiplier` = 1.15; `SoilGrowthHealthyMultiplier` = 0.85); quality boost (`SoilQualityHealthyBoost` = 0.08); extra harvest (`SoilExtraHarvestHealthyBoost` = 0.10) — all constants live here (`FR-001`/`FR-002`/`FR-003`).
├── ModEntry.cs                      # Composition root; event subscription/unsubscription; `AssetRequested` (no new assets needed for this feature).
├── Controllers/
│   └── ModController.cs             # `OnDayStarted` (1719–1778) — existing event handler; must add `Crop.newDay` public-field scan (`FR-001`) with `HostAtomicLockService` per-tile guard; `OnSaveLoaded` atomic-flag state machine (existing); `ExecuteWithConcurrencyGuard` must be added at `OnDayStarted` (`ModController` 2100 — guard definition exists for `OnSaveLoaded`/`OnSaving` only). Zero harvest hooks exist (`ModController` zero `Crop.harvest` references beyond `message.harvest.inventory-full` at `Constants.cs:56`) — harvest-intercept hook deferred to tasks.
├── Domain/
│   ├── Interfaces/
│   │   ├── ISoilHealthService.cs     # Per-tile health query / persistence contract (`SoilHealthState`; sparse dictionary; `TileKey`; `HealthValue` 0–100 clamped).
│   │   └── ICompostApplicationService.cs  # Existing interface from `spec-004` (prerequisite — `SC-008` verification concurrent).
│   ├── Models/
│   │   ├── SoilHealthState.cs        # Persistence DTO (`Dictionary<LocationName, Dictionary<TileKey, HealthValue>>`).
│   │   └── HealthCategory.cs         # Enum: `Poor` ([0,30]); `Moderate` ([31,70]); `Healthy` ([71,100]) — derived from `HealthValue`; `ROADMAP.md` scale confirmed (`A3` 88; `FR-004` 77; `spec.md` `Clarifications` Q2).
│   └── Services/
│       └── SoilDecayService.cs      # Linear scan (`foreach` over `terrainFeatures.Pairs`; `86-105`) — default for `FR-001` growth modifier application.
└── Services/
    ├── SoilHealthService.cs          # Existing persistence + reactive events (`OnTileHealthChanged`); `HealthValue_ClampedToMinMax`; `NaN`/`±Inf` guards (`FR-007`).
    ├── CompostApplicationService.cs  # Existing compost application; `TemporaryAnimatedSprite` floating-text pattern (`178-191`) — reused for harvest feedback (`A4` 89; `FR-002`/`FR-003`).
    ├── VisualizationService.cs       # Overlay / tooltip / hoe feedback (`247-285`) — exists; continues to reflect updated health (`SC-006` 113 — unchanged; `FR-008` 81 — no pixel assertions).
    └── CompostingBinService.cs        # `CollectCompost` rollback (`189-264`) — hidden duplication bug (`outputCount > 1` at `211`; partial-delivery loop `229-243` masks duplication hazard); must pass `SC-008` (`manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) before yield activation (`FR-005` 78; `Dependencies` 136-137).

LivingRoots.Tests/
├── CompostingBinServiceTests.cs      # `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` automated verification (`339`) — prerequisite for `SC-008`.
├── SoilHealthServiceTests.cs         # Existing persistence / clamp tests.
├── VisualizationServiceTests.cs      # Overlay render-safety (`SC-006` preservation).
└── [New tests deferred to tasks.md] # `Crop.newDay` growth modifier (`SC-001`/`SC-002`/`SC-003`); harvest quality (`SC-004`); extra harvest (`SC-005`); performance chunk (`SC-007`); `GamePath` manual verification (`SC-008`); save/reload consistency (`SC-007`); rollback (`SC-008`).
```

**Structure Decision**: Extends existing `DDD` layered architecture (`Principle I`); no new interfaces invented (`Principle V` / `A4`); contracts (`i-soil-health-service.md`) document internal interfaces (not external APIs); `contracts/` exists because this feature exposes domain-service contracts used by other systems (existing `ISoilHealthService` / `ICompostApplicationService`); quickstart validates through manual `GamePath` observation (dominant verification path per `FR-009` / `SC-008`).

---

## Phase 0: Research & Unknown Resolution

*Full details: [research.md](./research.md).*

**Status**: All 9 clarification questions (`Q1`–`Q7`) answered and integrated into `spec.md` (`Clarifications` 2026-10-15 / 2026-10-16). Zero `NEEDS CLARIFICATION` markers remain in `spec.md` (checklist `requirements.md` 16/16 passing).

### Resolved Unknowns (from `research.md`)

| Unknown (from spec.md `Open Questions` / `Clarifications`) | Resolution | Evidence / Source |
|-----------------------------------------------------------|-----------|------------------|
| `Q1` — Hook mechanism (`Crop.newDay` / `DayStarted` vs. `HoeDirt.GetFertilizerSpeedBoost()` Harmony patch) | **Option A** (`DayStarted` public-field scan via `Crop.newDay`; no `Harmony`; `GamePath` spike gate verifies ordering before activation; `B` only if ordering fails) | Zero Harmony footprint (`AGENTS.md` anti-pattern); `LuckSkill` proves direct field access; `ModController.cs` 1719–1778 (`OnDayStarted`); `Crop.cs` decompilation confirms public fields; `Clarifications` Q1 27; `subagent-response-q1.md` |
| `Q2` — HealthCategory scale (`ROADMAP.md` 0-30/31-70/71-100 vs. `spec-001` half-open) | **ROADMAP.md` scale confirmed** (`Poor` [0,30]; `Moderate` [31,70]; `Healthy` [71,100]); `spec-001` half-open reconciled as implementation artifact only; all `ModConstants`, `ColorDTO`, tooltip texts, assertions aligned | `ROADMAP.md` 36-38; `spec.md` `A3` 88; checklist `band-scale-host-authority.md`; `Clarifications` Q2 28 |
| `Q3` — Growth multipliers (fixed-day offset vs. multiplicative phase-duration) | **Multiplicative phase-duration**: Poor 1.15× (`SoilGrowthPoorMultiplier` = 1.15); Moderate 1.00× (vanilla parity, `SC-003`); Healthy 0.85× (`SoilGrowthHealthyMultiplier` = 0.85); applied before vanilla modifiers (`A2` compositional consistency) | `Lal 2020` meta-analysis; `Cornell Soil Health Assessment`; `extension.umn.edu`; `Clarifications` Q3 29; `subagent-response-q3.md` |
| `Q4` — Quality / extra-yield values (additive vs. multiplicative; descriptor correction) | **Quality**: Poor force 0; Moderate vanilla; Healthy `+0.08` (`SoilQualityHealthyBoost`); descriptor corrected to "additive on base" (`Clarifications` Q4 30). **Extra**: Healthy `≥71` base `+0.10` (`SoilExtraHarvestHealthyBoost`). Compositional (`A2`); exception: Poor tier permits quality override (force 0) as designed exception — documented explicitly (`A2` updated at `spec.md` 89) | `FR-002` 75; `FR-003` 76; `SC-004` 110; `SC-005` 111; `Clarifications` Q4 30; checklist `harvest-quality-extra.md` |
| `Q5` — Rollback verification (`SC-008`) / `ReGen Valley` interaction | **Rollback**: `SC-008` = concurrent prerequisite (`user B` — design/implementation permitted concurrently; yield activation requires `SC-008` green). Hidden duplication bug (`CompostingBinService.cs` `outputCount > 1` at `211`; partial-delivery loop `229-243`) must pass verification (`manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` (alias: `StillConsumesBin`) — `CompostingBinServiceTests.cs:339`). **ReGen Valley**: documentation-only (`Assumes` 145); first-wins host-authoritative (`HostAtomicLockService`); second mutation refused with message (inventory intact) | `DEFERRED_SC008_EXPLANATION.md`; `Dependencies` 136-137; `FR-005` 78; `Clarifications` Q5 31-32; `Assumes` 145 |
| `Q6` — `FR-001` / `Crop.phaseDays` application (`DayStarted` / before vanilla modifiers / `HostAtomicLockService` guard) | **Confirmed**: Apply base `phaseDays` multiplicatively at `DayStarted` before vanilla modifiers (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`); guard with `HostAtomicLockService`; zero `Crop.newDay` references — must be added | `ModController` 1483–1492 (`HoeDirt` filter); `HostAtomicLockService` interface; `Clarifications` Q6 32; `subagent-response-q6.md` |
| `Q7` — Performance / chunk mechanism (`<5ms` @ 40k tiles; contingency design) | **Design A confirmed** (`user A`): combined chunk (`~1000` tiles/chunk; `~2ms` budget; prime-modulus stride `61`; spread across ticks when linear exceeds `<5ms`); `ExecuteWithConcurrencyGuard` at `OnDayStarted` (`ModController` 2100 — guard exists for `OnSaveLoaded`/`OnSaving` only; must add at `OnDayStarted`). Zero chunk mechanism exists (`subagent-response-q7.md`; `SoilDecayService.cs` linear only). Implementation deferred to tasks. | `spec-002` `:113`/`:176`; `FR-001` 74; `Clarifications` Q7 34; checklist `growth-performance.md` CHK-1401; `subagent-response-q7.md` |
| Harvest feedback (`Q3` / `A4`) — floating-text message when tier modifies harvest outcome | **Option A** (`user A`): message shown when tier modifies outcome (`Poor` → forced 0 stars; `Healthy` → enhanced quality / extra harvest); reuse existing `floating-text` / `TemporaryAnimatedSprite` (`VisualizationService.cs` 247-285); zero new assets / `i18n` (`A4` 89; `Non-Goals` 126). `Crop.harvest` hook does not exist (`ModController` zero harvest hooks) — deferred to tasks. | `Clarifications` Q3/A4 33-34; `A4` 89; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191; `research-harvest-feedback.md` |

---

## Phase 1: Design & Contracts

*Prerequisites: `research.md` complete (all `NEEDS CLARIFICATION` resolved).*

### Data Model

*Full definitions: [data-model.md](./data-model.md).*

**Key entities** (extracted from `data-model.md`):

- **`SoilHealth` / `ISoilHealthService`** (`Domain/Interfaces/ISoilHealthService.cs`) — per-tile persistence (`SoilHealthState`: `Dictionary<LocationName, Dictionary<TileKey, HealthValue>>`); `HealthValue` (`int`, clamped `0-100`, `NaN`/`±Inf` guarded); `TileKey` (`(int x, int y)`, valid within `LocationName` bounds); `HealthCategory` (`Poor` [0,30]; `Moderate` [31,70]; `Healthy` [71,100]) — derived from `HealthValue`; `ROADMAP.md` scale confirmed (`FR-004` 77).
- **`HealthCategory`** — derived enum; growth multiplier (`FR-001`), quality roll (`FR-002`), extra harvest (`FR-003`) mapped per tier (`data-model.md` table at line 22-25).
- **`Crop` / `HoeDirt`** (vanilla game objects — not edited) — `phaseDays` (int) scaled multiplicatively at `DayStarted`; harvest quality (`Crop.harvest`) computed locally from shared `SoilHealthService`; no `Data/Crops` edit (`Non-Goals` 122-123).
- **`Compost` / `CompostingBin`** (`spec-004` — prerequisite) — rollback verification (`SC-008`) concurrent; interaction (`ReGen Valley`) documentation-only.

**Relationships / Constraints**:
- `SoilHealth` → `HealthCategory` (derived, not stored; derived from `HealthValue`).
- `SoilHealth` → `Crop` (`FR-001`: `DayStarted` scan modifies `phaseDays`; host-only; sync via `GameLocation.terrainFeatures`).
- `SoilHealth` → `Crop.harvest` (`FR-002` / `FR-003`: local computation from shared tile health; no new transport message; `FR-006` 79).
- `CompostApplicationService` → `SoilHealthService` (existing dependency; `FR-005` rollback gate concurrent).

**State Transitions**:
```
Soil Health (per tile):
  measured (0–100) → classified (HealthCategory) → persisted (sparse save)
  → affects growth (DayStarted: phaseDays multiplier) → affects harvest (harvest time: quality roll + extra probability)
```
- `SC-008` rollback verification (`spec-004`) = concurrent prerequisite (`user B`) — does not block design; must pass before final yield activation.
- `SC-006` (`Overlay` reflects updated health) = preserved (existing behavior; no design change needed).
- `SC-007` (`Save/reload` preserves growth effect) = preserved via `SoilHealthState` persistent data + `Crop.phaseDays` sync through `GameLocation.terrainFeatures`.

**Validation rules** (from `spec.md` `Requirements` 72-92):
- `FR-001`: Multiplicative on base `phaseDays` before vanilla modifiers (`A2` compositional); `HealthCategory` derived from `ROADMAP.md` scale; `IsMainPlayer` / `Context.IsMainPlayer` host-only; `HostAtomicLockService` per-tile guard.
- `FR-002`: Additive (`+0.08`) to vanilla roll `p`; Poor forces 0 (only tier allowing override); Moderate = vanilla; Healthy enhanced; floating-text confirmation (`A4`).
- `FR-003`: Per-tile (`≥71`); `extraProb = base + 0.10`; floating-text confirmation (`A4`).
- `FR-004`: `ROADMAP.md` scale (`Poor` [0,30]; `Moderate` [31,70]; `Healthy` [71,100]); `spec-001` half-open reconciled as artifact only.
- `FR-005`: `SC-008` rollback verification (`manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) concurrent prerequisite; hidden duplication bug (`outputCount > 1`) must pass verification before activation (`Dependencies` 136-137).
- `FR-006`: Host-only persistent phase (`IsMainPlayer` / `Context.IsMainPlayer`); discrete harvest rolls (`FR-002`/`FR-003`) computed locally from shared `SoilHealthService`; no new transport message; clients observe health via `OnTileHealthChanged`.
- `FR-007`: Fallback to vanilla if health data missing/corrupted/out-of-bounds (`0-100`); `HealthValue_ClampedToMinMax` + `NaN`/`±Inf` guards (`FR-009`).
- `FR-008`: No pixel assertions (`SC-006` visual preservation only; `SC-008` manual verification).
- `FR-009`: Manual play-through (`SC-008`) required for verification; `Roadmap.md` / `COMPLETION_SUMMARY.md` / `.specify/assessments/` reconciliation confirmed (`US4` 64-68; checklist `verification-pipeline.md`).

---

### Interface Contracts (`contracts/`)

*Full contract documentation: [contracts/i-soil-health-service.md](./contracts/i-soil-health-service.md).*

**Type**: Internal domain-service contracts (not public APIs). This feature is purely internal (`spec.md` `Project Type`: SMAPI mod — DLL plugin; no user-facing APIs, no endpoint contracts, no command schemas, no grammars, no external interfaces). Contracts are included because the feature uses existing domain interfaces (`ISoilHealthService`, `ICompostApplicationService`) that are referenced by other systems and must be documented for multi-system coordination (multiplayer sync, rollback verification, harvest feedback).

**Contracts defined** (`contracts/i-soil-health-service.md`):
1. **`ISoilHealthService`** (`Domain/Interfaces/ISoilHealthService.cs`) — load/query/update per-tile soil health; `OnTileHealthChanged` event; sparse persistence; `FormC` normalization; `0-100` bounds; `HostAtomicLockService` per-tile guard; async-only; multiplayer: host-authoritative persistent phase; local discrete harvest rolls.
2. **`ICompostApplicationService`** (`Domain/Interfaces/ICompostApplicationService.cs`) — existing `spec-004` interface; prerequisite (`SC-008`) concurrent; rollback contract (`FR-005b`/`FR-005c`); hidden duplication bug (`outputCount > 1`) must pass verification.
3. **External integration contracts** (`contracts/i-soil-health-service.md` §External Integration) — SMAPI event loop (`ModController`); SMAPI persistence (`IModDataService`); SMAPI multiplayer sync (`GameLocation.terrainFeatures`); vanilla game objects (`Crop`, `HoeDirt`, `Data/Crops`) — contracts defined by SMAPI/vanilla framework, not edited by this feature (`Non-Goals` 122-134).
4. **Contract quality / verification** (`contracts/i-soil-health-service.md` §Contract Quality) — no premature abstractions; references existing patterns (`HostAtomicLockService`; `VisualizationService` floating-text; `CompostApplicationService` `TemporaryAnimatedSprite`); `SC-008` concurrent; `SC-006` preserved; zero new assets / `i18n`.

---

### Quickstart Validation

*Full scenarios: [quickstart.md](./quickstart.md).*

**Purpose**: Document runnable validation scenarios that prove the feature works end-to-end from the player's perspective — linking to contracts (`contracts/i-soil-health-service.md`) and data model (`data-model.md`) without duplicating design details. No full implementation code, model/service/controller bodies, migrations, or complete test suites included. Implementation details belong in `tasks.md` (Phase 2, deferred to `/speckit-tasks` command — NOT created by this plan).

**Scenarios** (from `quickstart.md`):
- **US1 (`SC-001`/`SC-002`/`SC-003`) — Growth Speed Change**: Launch with `GamePath`; plant same seed on 3 tiles; apply compost to 1 (or 3× to reach `Healthy`); observe `Poor` (`[0,30]`) slower than `Moderate` (`[31,70]`); `Healthy` (`[71,100]`) faster; `Moderate` = vanilla (parity). Manual observation only (`SC-009` / `FR-009` 82).
- **US2 (`SC-004`) — Harvest Quality**: Harvest from `Poor` (`[0,30]`) → 0 stars; `Moderate` → vanilla roll; `Healthy` → enhanced probability (`+0.08`). Confirm floating-text message (`A4`). No pixel assertions (`FR-008`).
- **US3 (`SC-005`) — Extra Harvest**: Harvest multiple times from `Healthy` (`≥71`) vs. `Moderate`; confirm higher rate (`base + 0.10`). Confirm floating-text message (`A4`). Confirm no duplication (`US3` 58: "sem duplicar itens"; `FR-005` rollback concurrent protects but requires verification).
- **US4 (`SC-008` / `FR-009`) — Trust / Verification Pipeline**: Confirm `ROADMAP.md` reconciliation (`A3` 88 — scale locked); `ModConstants` values documented (`SoilGrowthPoorMultiplier` = 1.15, `SoilGrowthHealthyMultiplier` = 0.85, `SoilQualityHealthyBoost` = 0.08, `SoilExtraHarvestHealthyBoost` = 0.10); `SC-008` rollback concurrent in progress; `SC-006` (`Overlay` updated); `SC-007` (`Save/reload` consistent); no contradiction with `ROADMAP.md`; no `spec-001` half-open leakage.

**Validation prerequisites** (`quickstart.md` §Prerequisites):
- `SC-008` rollback verification (`manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) — concurrent (`user B`); must pass before final yield activation (`FR-005`; `Dependencies` 136-137).
- `GamePath` spike (`A1` 86) — verify `Crop.newDay` public-field ordering with vanilla growth before `FR-001` activation.
- `ReGen Valley` interaction (`Assumes` 145) — documented only; no design change needed.

**Validation commands** (`quickstart.md` §Validation Commands):
```bash
# Manual verification (SC-008 / FR-009 82):
# Launch with GamePath; apply compost; observe growth/harvest; save/reload;
# verify consistency; confirm SC-001/SC-002/SC-003 growth differences,
# SC-004 quality differences, SC-005 extra harvest differences.

# Automated verification (SC-008 prerequisite — concurrent):
# dotnet test LivingRoots.Tests --filter "FullyQualifiedName~CollectCompost_WhenInventoryRefuses_RetainsBinReadyState"
# (CompostingBinServiceTests.cs 339 — rollback mechanism verification)
# Note: manual verification remains dominant path (FR-009 82; SC-008 114).

# Formatting / style check (AGENTS.md conventions):
# dotnet format --verify-no-changes specs/005-soil-health-to-crop-growth/
# dotnet build specs/005-soil-health-to-crop-growth/
```

---

## Phase 1 Design Artifacts (Generated / Verified)

*All artifacts exist and are consistent with `spec.md` and `constitution.md`. No `NEEDS CLARIFICATION` markers remain in `spec.md`.*

### Generated Artifacts

| Artifact | Path | Status | Notes |
|----------|------|--------|-------|
| `plan.md` | `specs/005-soil-health-to-crop-growth/plan.md` | **Generated (this file)** | Design-locked; deferred prerequisites (`SC-008`, `GamePath` spike) documented; no `NEEDS CLARIFICATION` markers; Phase 1 complete. |
| `research.md` | `specs/005-soil-health-to-crop-growth/research.md` | **Verified (exists)** | All 9 clarification questions (`Q1`–`Q7` + harvest feedback) answered and integrated; `SC-008` deferred prerequisite documented; `subagent-response-q7.md` (performance chunk) and `subagent-response-q1.md` (hook mechanism) cited. |
| `data-model.md` | `specs/005-soil-health-to-crop-growth/data-model.md` | **Verified (exists)** | Entities (`SoilHealth`, `HealthCategory`, `Crop` / `HoeDirt`, `Compost` / `CompostingBin`); relationships; validation rules (`FR-001`–`FR-009`); state transitions; `ROADMAP.md` scale (`FR-004`); `SC-008` deferred prerequisite (`user B` — concurrent, not blocker); performance design (`Q7` — design `A` locked; deferred to tasks). |
| `contracts/i-soil-health-service.md` | `specs/005-soil-health-to-crop-growth/contracts/i-soil-health-service.md` | **Verified (exists)** | Internal domain-service contracts (`ISoilHealthService`; `ICompostApplicationService`); external integration contracts (`ModController`; `IModDataService`; `GameLocation.terrainFeatures`; `Crop` / `HoeDirt` / `Data/Crops`); `SC-008` concurrent; zero new assets / `i18n`; no premature abstractions. |
| `quickstart.md` | `specs/005-soil-health-to-crop-growth/quickstart.md` | **Verified (exists)** | 4 validation scenarios (`US1` – growth; `US2` – quality; `US3` – extra harvest; `US4` – trust / verification); prerequisites (`SC-008` concurrent; `GamePath` spike); commands (manual + automated); references contracts/data-model (no duplication); no implementation code. |
| `checklists/` (5 files) | `specs/005-soil-health-to-crop-growth/checklists/` | **Verified (exists)** | `band-scale-host-authority.md`; `growth-performance.md`; `harvest-quality-extra.md`; `requirements.md` (16/16 passing); `verification-pipeline.md` (requirements-quality — `FR-005` rollback, `SC-008` gate, `US4` trust pipeline, `SC-009` manual observation, no false-green risk). |

---

## Constitution Re-Check (Post-Phase 1 Design)

*Re-evaluated after design artifacts (`data-model.md`, `contracts/`, `quickstart.md`) generated. No new violations introduced by design. All deferred prerequisites (`SC-008`, `GamePath` spike) are external prerequisites (not design gaps).*

| Principle | Status (Post-Design) | Evidence / Update from Design Phase |
|-----------|---------------------|-------------------------------------|
| I. DDD | PASS (unchanged) | Design artifacts preserve layered architecture (`Domain/` interfaces; `Services/` implementations; `Controllers/` event handlers); no interfaces relocated; no premature abstractions (`Principle V`). Contract (`i-soil-health-service.md`) documents existing interfaces (`ISoilHealthService` / `ICompostApplicationService`) without inventing new ones. |
| II. Security-First | PASS (unchanged) | Design does not change sanitization (`SanitizePathSegments`; `FormC` normalization); does not suppress `CheckForOverflowUnderflow`; does not expose raw exception messages. `FR-007` (`NaN`/`±Inf` guards) preserved; bounds (`0-100`) preserved (`ModConstants`). No new user-influenced input paths introduced by design (`Contracts` §Security). |
| III. Async-Only | PASS (unchanged) | Design confirms `DayStarted` event handler (`ModController`) uses non-blocking scan; chunk mechanism (`~2ms`/chunk) allows concurrent tick processing; `ExecuteWithConcurrencyGuard` (`Interlocked.CompareExchange`) protects `OnDayStarted` (must be added — `ModController` 2100 exists for `OnSaveLoaded`/`OnSaving` only). No `.Result` or `.Wait()` introduced by design (`AGENTS.md`; `Principle III`). |
| IV. TDD | PASS (updated) | Design confirms `SC-008` (`CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` automated test + manual `GamePath` play-through) = absolute blocker for activation (`FR-005`; `Dependencies` 136-137); `user B` selection (`Clarifications` 2026-10-16) allows concurrent design/implementation but does not weaken the gate before activation. Design does not invent new tests; it references existing verification (`SC-001`–`SC-007`) and deferred prerequisite (`SC-008`). Test stubs (`ThreadSafeGameLoopEventsStub`) preserved. `FR-009` (manual verification) preserved — automation gap explicitly acknowledged (not hidden). |
| V. Simplicity / YAGNI | PASS (updated) | Design introduces zero new interfaces (`A4`); harvest hook (`Crop.harvest`) deferred to tasks (not invented prematurely); no new abstract base for harvest intercept; `floating-text` reuse (`VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191) — no new asset / `i18n`; no `Data/Crops` edit; no `Data/Objects` / `Data/Recipe` / `Data/BigCraftables` / `Data/Characters` / `Data/Buildings` / `Data/Quests` (`Non-Goals` 122-134). Performance contingency (chunk mechanism) designed but deferred (`subagent-response-q7.md`: zero chunk mechanism exists; must be added). `Direct IMonitor` acceptable when wrapper adds no value (`Principle V`). |
| VI. Multiplayer (NON-NEGOTIABLE, v2.0.0) | CONDITIONAL PASS (updated — clarification 2026-10-16 override confirmed) | Design confirms host-authoritative persistent phase (`DayStarted` — `IsMainPlayer` / `Context.IsMainPlayer`; `HostAtomicLockService` per-tile guard — `FR-006` 79; `A5` 90); discrete harvest rolls (`FR-002`/`FR-003`) local from shared `SoilHealthService` — no new transport message (`FR-006` 79; `Clarifications` Q1 22); `ReGen Valley` interaction (`Assumes` 145) = documentation-only (first-wins host-authoritative; second mutation refused with message — inventory intact; `HostAtomicLockService` / `Principle VI`); `SC-008` rollback is **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent `B`); design/implementation permitted concurrently. Live 2-player verification (`D1`) deferred to roadmap `D1` (`Constitution` Governance §Migration Plan — amendment breaks no existing code; single-player = host with no clients — degenerate case). **No MUST violations**; conditional pass documented and justified (`Data-Model.md` §Multiplayer Contract; `Contracts/i-soil-health-service.md` §Multiplayer Contract; `Quickstart.md` §Prerequisites; `Plan.md` §Constitution Check). |

**Post-Design Gate Result**: PASS with one documented conditional (`Principle VI` — live multi-player verification deferred to `D1`); no new `MUST` violations detected. All deferred prerequisites (`SC-008` rollback; `GamePath` spike; `ReGen Valley` documentation) are either external prerequisites (not design gaps) or fully parameterized design choices deferred to implementation (`tasks.md` / Phase 2). No `NEEDS CLARIFICATION` markers remain (`spec.md` verified; `checklist/requirements.md` 16/16 passing; `research.md` line 72 confirms zero remaining).

---

## Complexity Tracking

No Constitution Check violations require justification. All design choices (`A1` Option A hook; `A2` compositional multiplicative; `A3` `ROADMAP.md` scale; `A4` zero new assets / reuse floating-text; `A5` first-wins host-authoritative / no new mechanism; `B` concurrent prerequisite `SC-008`) are either confirmed user selections (documented in `Clarifications` 2026-10-15 / 2026-10-16) or fully parameterized deferred implementations (`chunk mechanism` — design `A` locked; `Crop.harvest` hook — design `A` locked). No premature abstractions (`Principle V`); no new interfaces invented; no new assets; no `Harmony` dependency (`AGENTS.md` anti-pattern). Performance contingency (`Q7`) is specification-level design (`FR-001` 74; `Dependencies` 139) — implementation deferred to `tasks.md` (Phase 2 — `/speckit-tasks` command; NOT created by this plan). No complexity justifications needed (`Plan Template` §Complexity Tracking: "No Constitution Check violations. No complexity justifications needed.").

---

## Phase 2 Note

`tasks.md` (execution task list) is NOT created by this plan — deferred to `/speckit-tasks` command. Implementation of `Crop.newDay` scan (`FR-001`), chunk mechanism (`Q7` contingency), harvest-intercept hook (`A4`), `GamePath` spike (`A1`), and `SC-008` verification (`FR-005`) are deferred to `tasks.md`. Design artifacts are complete; no `NEEDS CLARIFICATION` markers remain. `SC-008` verification is absolute blocker for yield activation (`FR-002`/`FR-003`; clarification 2026-10-16 override); design/implementation concurrent. Chunk mechanism (`Q7`) activates only above `<5ms` @ 40,000 tiles; does NOT block `SC-003` parity verification (`Phase 10` `T094` can verify linear scan parity manually via `SC-009`).

---

*Plan generated: 2026-10-16. Feature branch: `feat/005-soil-health-to-crop-growth`. Design artifacts: `plan.md` (this file), `research.md`, `data-model.md`, `contracts/i-soil-health-service.md`, `quickstart.md`, `checklists/` (5 files). Phase 2 (`tasks.md`) deferred to `/speckit-tasks` command. Verification gate `SC-008` (`spec-004` rollback + `manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) is **absolute blocker for yield activation** (`FR-005`; `Dependencies` 136-137; clarification 2026-10-16 overrides previous concurrent `B` selection — design permitted concurrently, activation blocked until green). `GamePath` spike (`A1` 86; `Dependencies` 141) must verify `Crop.newDay` ordering before `FR-001` activation. `ReGen Valley` interaction (`Assumes` 145) = documentation-only (first-wins host-authoritative; no design change needed).*
