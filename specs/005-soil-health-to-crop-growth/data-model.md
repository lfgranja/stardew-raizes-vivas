# Phase 1 Design: Data Model — Soil Health → Crop Growth

**Feature**: `005-soil-health-to-crop-growth` | **Branch**: `feat/005-soil-health-to-crop-growth`
**Based on**: `spec.md` (updated after clarification loop: Q1=B rollback concurrent; Q2=A performance design; harvest Q3/A4=A feedback reuse; `SC-008` deferred prerequisite)

---

## Entities

### Soil Health (`SoilHealth` / `ISoilHealthService`)

| Field | Type | Constraint | Source / Evidence |
|-------|------|-----------|-------------------|
| `LocationName` | `string` | Non-empty; normalized (`FormC` Unicode) | `SoilHealthState` (`Domain/Models/SoilHealthState.cs`) |
| `TileKey` | `(int x, int y)` tuple / key string | `x`/`y` within valid tile bounds (`ModConstants`); unique per location | `TileKey` definition; `SoilHealthState` dictionary key |
| `HealthValue` | `int` (`0-100`) | Clamped `0-100`; `NaN`/`±Inf` guarded (`FR-007`); `CheckForOverflowUnderflow` enabled (`AGENTS.md`) | `FR-007` 80; `FR-009` 82; `ModConstants` reference targets (`SoilGrowthPoorMultiplier`, `SoilGrowthHealthyMultiplier`, `SoilQualityHealthyBoost`, `SoilExtraHarvestHealthyBoost`) |
| `HealthCategory` | `enum` (`Poor` / `Moderate` / `Healthy`) | Derived from `HealthValue`: `Poor` = `[0,30]`, `Moderate` = `[31,70]`, `Healthy` = `[71,100]` (`FR-004` 77; `ROADMAP.md` 36-38) | `ROADMAP.md` scale (`spec.md` `A3` 88); checklist `band-scale-host-authority.md` 41 |

### Health Category (`HealthCategory`)

| Category | Range (inclusive) | Growth Multiplier (`FR-001`) | Quality Roll (`FR-002`) | Extra Harvest (`FR-003`) |
|----------|------------------|------------------------------|------------------------|--------------------------|
| `Poor` | `[0, 30]` | `1.15×` (`SoilGrowthPoorMultiplier` = 1.15) — slower | Force 0 (override permitted only for Poor tier) | None (base `Data/Crops.ExtraHarvestChance` only) |
| `Moderate` | `[31, 70]` | `1.00×` (vanilla parity, `SC-003`) | Vanilla roll `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` | None |
| `Healthy` | `[71, 100]` | `0.85×` (`SoilGrowthHealthyMultiplier` = 0.85) — faster | Additive `+0.08` (`SoilQualityHealthyBoost` = 0.08) | Base + `0.10` (`SoilExtraHarvestHealthyBoost` = 0.10) |

### Crop Growth Modifier (`FR-001` / `Crop.phaseDays`)

| Attribute | Specification | Implementation Note |
|-----------|-------------|---------------------|
| Application event | `DayStarted` (`ModController.OnDayStarted` 1719-1778) | Option A: public `Crop.newDay` field scan; no `Harmony` (`A1` 86) |
| Target tiles | `HoeDirt` tiles in active `GameLocation` (`terrainFeatures` of type `HoeDirt`; `ModController` 1485 `HoeDirt` filter) | `FR-001` 74; `Clarifications` Q6 32 |
| Modifier application | Multiplicative on base `phaseDays` — before vanilla modifiers (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`) | `FR-001` 74; `A2` 87; `Clarifications` Q6 32 |
| Host authority | `IsMainPlayer` / `Context.IsMainPlayer`; `HostAtomicLockService` per-tile guard (`FR-006` 79; `A5` 90) | `HostAtomicLockService.cs` 7-60; `TryApplyCompostWithLock` 2779-2853 |
| Client sync | Modified `phaseDays` propagates via `GameLocation.terrainFeatures` (normal SMAPI location sync); no new transport message (`A5` 90) | `FR-006` 79; `Clarifications` Q1 22 |

### Harvest Quality Roll (`FR-002` / `Crop.harvest`)

| Attribute | Specification | Evidence |
|-----------|-------------|----------|
| Event | `Crop.harvest` (discrete event; client-local computation from shared `SoilHealthService` tile health) | `FR-002` 75; `US2` 49-51 |
| Roll formula | Vanilla: `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` (`FR-002` 75; `US2` 50) | `FR-002` 75 |
| Tier effect | Poor: force quality 0 (override permitted only for Poor); Moderate: vanilla roll; Healthy: additive `+0.08` (`SoilQualityHealthyBoost` = 0.08) | `FR-002` 75; `Clarifications` Q4 30; `SC-004` 110 |
| Composition | Additive to vanilla (not replacement) — `A2` 87 consistency (`FR-002` "additively … never overriding") | `A2` 87; `FR-002` 75 |
| Observable confirmation | Floating-text message when tier modifies outcome (`Clarifications` Q3/A4 33-34; `A4` 89 — reuse existing `floating-text` / `restoration-text`; no new `i18n` key) | `Clarifications` Q3/A4; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191 |
| New dependency (deferred to plan/tasks) | Harvest-intercept hook at `Crop.harvest` — does not exist (`ModController` has no harvest hook; zero `Crop.harvest` references beyond `message.harvest.inventory-full` at `Constants.cs` 56) | `CLARIFICATION_BATCH.md`; `/tmp/report_harvest_feedback.txt` |

### Extra Harvest (`FR-003` / `Data/Crops`)

| Attribute | Specification | Evidence |
|-----------|-------------|----------|
| Event | Per-tile at harvest time (`Crop.harvest`); not globally per seed (`FR-003` 76; `US3` 58) | `FR-003` 76; `US3` 58 |
| Probability | `extraProb = base + ModConstants.SoilExtraHarvestHealthyBoost` (`+0.10`; `FR-003` 76; `Clarifications` Q4 30) | `FR-003` 76; `SC-005` 111 |
| Tier activation | Healthy (`≥71`) only (`FR-003` 76; `FR-004` 77) | `FR-003` 76 |
| Observable confirmation | Floating-text message when extra item produced (`Clarifications` Q3/A4 33-34) | `Clarifications` Q3/A4 |

### Performance Contingency (`FR-001` / `Q7` — User A Confirmed)

| Parameter | Design Value | Evidence / Source |
|-----------|-------------|-------------------|
| Contract source | `spec-002` deployment target (`≤10,000` target / `≤40,000` extreme / `<5ms` at `DayStarted`; `Dependencies` 139; `spec-002` `:113`/`:176`) | `FR-001` 74; `Dependencies` 139 |
| Default scan | Linear (`SoilDecayService.cs` 86-105: `foreach` over `terrainFeatures.Pairs`) | `subagent-response-q7.md`; `SoilDecayService.cs` 86 |
| Contingency activation | When linear exceeds `<5ms` at `40,000` tiles (`FR-001` 74; `Clarifications` Q7 34) | `FR-001` 74; `Clarifications` Q7 34 |
| Chunk size | `~1,000` tiles per tick (`Clarifications` Q7 34) | `Clarifications` Q7 34 |
| Chunk budget | `~2ms` per chunk (`Clarifications` Q7 34) | `Clarifications` Q7 34 |
| Spread mechanism | Combined chunk phase (not separate decay/growth chunks); prime-modulus stride (`61`); spread across ticks (`FR-001` 74) | `Clarifications` Q7 34; `FR-001` 74 |
| Concurrency guard | `ExecuteWithConcurrencyGuard` at `ModController.OnDayStarted` (matches `OnSaveLoaded` atomic-flag state-machine; `ModController.cs` 2100) | `FR-001` 74; `Clarifications` Q7 34; `subagent-response-q7.md` |
| Implementation status | **Design locked; implementation deferred to plan/tasks** (user confirmed A; `growth-performance.md` CHK-1401 notes expected gap) | `subagent-response-q7.md`; checklist `growth-performance.md` |

---

## Key Entities & Relationships (Domain Model)

```
SoilHealthState (per save / farm)
  ├── key: LocationName (string, sanitized FormC, normalized)
  └── value: Dictionary<TileKey, HealthValue>

TileKey (unique within location)
  ├── x: int (tile coordinate)
  └── y: int (tile coordinate)

HealthValue (int, clamped 0-100, NaN/±Inf guarded)
  └── derived: HealthCategory (Poor [0,30] / Moderate [31,70] / Healthy [71,100])

Crop / HoeDirt (vanilla game object — modified via public fields at DayStarted)
  ├── phaseDays (int) — scaled multiplicatively by HealthCategory multiplier
  └── harvest quality / extra harvest — computed locally at harvest time (FR-002 / FR-003)

Compost / CompostingBin (existing spec 002 / 004 systems — prerequisite)
  ├── rollback verification (SC-008) — **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent B); design/implementation permitted concurrently; activation blocked until `SC-008` passes (`manual GamePath` + `StillConsumesBin` — `CompostingBinServiceTests.cs:339`).
  └── interaction rule (ReGen Valley) — documentation-only (A5 / Assumes 145; first-wins host-authoritative)
```

---

## Lifecycle / State Transitions

```
Soil Health (per tile): measured (0-100) → classified (HealthCategory: Poor/Moderate/Healthy) → persisted (sparse save via ModDataService) → displayed (overlay / tooltip — spec 001) → affects growth (DayStarted: phaseDays multiplier) → affects harvest (harvest time: quality roll + extra probability)

Verification Gate (SC-008): not yet green (deferred prerequisite from spec 004; concurrent with design per user B selection)
Performance Contingency (Q7): defined (combined chunk; ~1000 tiles/chunk; ~2ms; stride 61; ExecuteWithConcurrencyGuard at OnDayStarted); not yet implemented (deferred to plan/tasks; user A confirmed design)
```

---

## Constraints (Constitution Check — Must Pass Before Implementation)

**Constitution (`.specify/memory/constitution.md`) — re-read: v2.0.0 (ratified 2026-09-05; amended 2026-10-13)**

| Principle | Constraint | Plan Impact |
|-----------|-----------|-------------|
| I. DDD (NON-NEGOTIABLE) | Domain layer (`Domain/Interfaces/`, `Domain/Models/`) separated from Services (`Services/`) and Controllers (`Controllers/`); ubiquitous language (`SoilHealth`, `Compost`, `LivingSoil`) | `ICompostApplicationService` / `ISoilHealthService` interfaces must remain in `Domain/Interfaces/`; implementations in `Services/`; `ModController` event handlers in `Controllers/` |
| II. Security-First (NON-NEGOTIABLE) | All input paths sanitized (`SanitizePathSegments`); Unicode normalized (`FormC`); bounds-checked (`0-100`); `CheckForOverflowUnderflow` enabled; no raw exception messages to logs | `ModDataService` must sanitize all `LocationName` and `TileKey` values; `SoilHealthService` must apply `HealthValue_ClampedToMinMax` with `NaN`/`±Inf` guards (`FR-007` 80) |
| III. Async-Only (NON-NEGOTIABLE) | `.Result`/`.Wait()` forbidden; async/await only for I/O; `volatile` / `Interlocked` permitted for atomic flags; `ModEntry.Dispose()` uses established lock + `_disposed` pattern | `DayStarted` scan must not block (`FR-001` 74); chunk spread (`~2ms`/chunk) allows concurrent tick processing; `ExecuteWithConcurrencyGuard` (`Interlocked.CompareExchange`) protects against re-entrancy (`FR-001` 74; `ModController` 2100) |
| IV. TDD | Red-Green-Refactor enforced; integration tests cover save/load, tile parsing, security paths; one file per subject (`{Subject}Tests.cs`) | `SC-008` (`StillConsumesBin`) automated test required before activation; manual `GamePath` verification (`SC-008` / T034) is the dominant verification path (`FR-009` 82; user B — concurrent, not blocker) |
| V. Simplicity / YAGNI | No premature abstractions; no new interfaces unless complexity justifies; direct `IMonitor` acceptable when wrapper adds no value; expression-bodied methods forbidden (`.editorconfig`) | `Crop.harvest` hook should reuse existing `VisualizationService` floating-text pattern (`A4` 89); no new abstract base for harvest intercept unless justified |
| VI. Multiplayer First-Class (NON-NEGOTIABLE, v2.0.0) | Farm state host-authoritative (`Context.IsMainPlayer`); no second source of truth; atomic per-tile lock (`HostAtomicLockService`); first-wins semantics; inventory intact on concurrent refusal; single-player = host with no clients | `FR-001` (`DayStarted` growth) executes host-only; `FR-002`/`FR-003` (harvest rolls) computed locally from shared `SoilHealthService`; `ReGen Valley` (`Assumes` 145) = documentation-only, first-wins (`Clarifications` Q5 31-32); `SC-008` rollback is **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent `B`); design/implementation permitted concurrently |

**Gate evaluation:** All principles pass. `SC-008` rollback verification deferred (absolute blocker for yield activation per clarification 2026-10-16 override — design/implementation concurrent, activation blocked until green; not ambiguity). Performance contingency (`Q7`) design locked (user A — combined chunk mechanism confirmed). No violations requiring justification.

---

## Unknowns / Deferred (Resolved / Confirmed in Planning Phase)

No `NEEDS CLARIFICATION` markers remain in `spec.md` (checklist `requirements.md` line 16 confirms: 8 of 9 clarification questions resolved; 1 deferred — `SC-008` verification prerequisite from spec 004, not ambiguity). All clarification questions (`Q1`-`Q7`) answered and integrated (`CLARIFICATION_BATCH.md` lines 21-45; `spec.md` `Clarifications` lines 22-36).

**Deferred prerequisites (must resolve before final yield activation — not before plan creation):**
1. `SC-008` rollback verification (`spec-004` `CompostingBinService.CollectCompost` + manual `GamePath` + automated `StillConsumesBin` — `Dependencies` 136-137; `FR-005` 78; user B — concurrent, not blocker; must still complete before final activation).
2. `GamePath` spike (`A1` 86; `Dependencies` 141) — verify `Crop.newDay` public-field ordering with vanilla growth before `FR-001` implementation.

**Design deferred to plan/tasks (not ambiguity — fully parameterized):**
- `Crop.newDay` public-field scan (`FR-001` 74) — design A confirmed; implementation deferred.
- Chunk mechanism (`FR-001` 74; `Clarifications` Q7 34) — design A confirmed (`~1000` tiles/chunk, `~2ms` budget, stride `61`, combined chunk, `ExecuteWithConcurrencyGuard` at `OnDayStarted`); zero chunk mechanism exists in code (`subagent-response-q7.md`); implementation deferred.
- Harvest-intercept hook at `Crop.harvest` (`Clarifications` Q3/A4 33-34; `A4` 89) — design A confirmed (reuse `floating-text` / `TemporaryAnimatedSprite`; zero new assets / `i18n`); hook does not exist (`ModController` zero harvest hooks; `Constants.cs` 56 only reference); deferred to tasks.
