---

description: "Task list for feature 005 — Soil Health → Crop Growth (SMAPI .NET 6 mod)"
---

# Tasks: Soil Health → Crop Growth

**Feature**: `feat/005-soil-health-to-crop-growth` | **Branch**: `feat/005-soil-health-to-crop-growth`
**Spec**: [`specs/005-soil-health-to-crop-growth/`](./spec.md) | **Plan**: [`plan.md`](./plan.md) | **Contracts**: [`contracts/i-soil-health-service.md`](./contracts/i-soil-health-service.md)

**Input Documents**: `plan.md`, `spec.md`, `data-model.md`, `contracts/i-soil-health-service.md`, `quickstart.md`, `research.md`, `checklists/` (5 files: `band-scale-host-authority.md`, `growth-performance.md`, `harvest-quality-extra.md`, `requirements.md`, `verification-pipeline.md`), `.specify/memory/constitution.md` (v2.0.0 — Principle VI multiplayer first-class).

**Prerequisites (blocking — must complete before any user story)**:
- `spec-004` (`CompostingBinService.CollectCompost`) rollback verification (`SC-008`) — **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent `B` selection); design/implementation permitted concurrently; must pass (`manual GamePath` + automated `StillConsumesBin` — `CompostingBinServiceTests.cs:339`) before any yield feature activates (`FR-005`; `Dependencies` 136-137).
- `GamePath` spike (`A1` 86; `Dependencies` 141) — verify `Crop.newDay` public-field ordering with vanilla growth before `FR-001` activation.
- `ROADMAP.md` band reconciliation (`FR-004` / `A3`) — scale locked: `Poor` [0,30], `Moderate` [31,70], `Healthy` [71,100] (`data-model.md` 16-17; `spec.md` A3).

**Language / Framework**: .NET 6 / C# latest / SMAPI 4.x (`Pathoschild.Stardew.ModBuildConfig` 4.4.0) / xUnit + Moq / FsCheck (property-based) / `ThreadSafeGameLoopEventsStub` / `ModConstants` single source of truth.

**Tests**: **MANDATORY — TDD REQUIRED** (user directive: red-green-refactor; property-based wherever applicable; invariant tests; BLOCKING). Tests must be written FIRST and must FAIL (RED PHASE) before any implementation begins. Each user story's tests are independently testable.

**Organization**: Organized by user story (P1 → P2 → P3 → P3 verification). Setup → Foundational → RED PHASE (all story tests) → Implementation (US1 → US2 → US3) → Verification / Integration (US4 + SC-008 + chunk) → Polish.

**Path Conventions**: All file paths are absolute relative to repo root (`LivingRoots/`, `LivingRoots.Tests/`, `specs/005-soil-health-to-crop-growth/`). Adjust based on `plan.md` structure.

---

## Phase 1: Setup (Shared Infrastructure) [P]

**Purpose**: Project initialization; verify existing contracts, data model, constants, and prerequisites are aligned before any implementation.

- [ ] T001 Verify feature branch exists (`feat/005-soil-health-to-crop-growth`) and `plan.md` / `spec.md` / `data-model.md` / `contracts/` / `quickstart.md` are present and consistent (`checklists/requirements.md` 16/16 passing)
- [ ] T002 [P] Read `.specify/extensions.yml` — confirm no `before_tasks` hooks registered (`after_analyze` only: `atomic-commit-hook`); skip silently; note `after_tasks` has none
- [ ] T003 [P] Verify `.specify/memory/constitution.md` v2.0.0 compliance (Principle I DDD, II Security-First, III Async-Only, IV TDD, V YAGNI, VI Multiplayer First-Class) for feature design
- [ ] T004 [P] Verify `contracts/i-soil-health-service.md` matches `Domain/Interfaces/ISoilHealthService.cs` and `Domain/Interfaces/ICompostApplicationService.cs` (existing interfaces; zero premature abstractions per `Principle V`)
- [ ] T005 [P] Verify `data-model.md` entities (`SoilHealth`, `HealthCategory`, `Crop` / `HoeDirt`) align with `Domain/Models/SoilHealthState.cs`, `Domain/Visualization/HealthCategory.cs`, `ModConstants.cs` references
- [ ] T006 [P] Confirm `SC-008` rollback verification (`spec-004`) concurrent prerequisite status (`CompostingBinServiceTests.cs` 339 — `StillConsumesBin`) — does NOT block design/implementation but MUST pass before yield activation (`FR-005` 78)
- [ ] T007 [P] Confirm `GamePath` spike (`A1` — `Crop.newDay` public-field scan; no `Harmony`) required before `FR-001` activation (`plan.md` §Phase 2 Note; `Dependencies` 141)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core constants, contracts, data-model alignment, chunk mechanism design, and infrastructure updates that must complete before ANY user story implementation. **No user story work begins until this phase is complete.**

**Critical**: `SC-008` rollback verification is **absolute blocker for yield activation** (`FR-002`/`FR-003`; clarification 2026-10-16 overrides previous concurrent `B`) — design/implementation permitted concurrently; activation blocked until `SC-008` green. `GamePath` spike (`A1`) is absolute gate before `FR-001` activation.

- [ ] T008 Verify `ModConstants.cs` exists and is the single source of truth (`AGENTS.md` conventions) — add placeholders for `SoilGrowthPoorMultiplier`, `SoilGrowthHealthyMultiplier`, `SoilQualityHealthyBoost`, `SoilExtraHarvestHealthyBoost`
- [ ] T009 [P] Update `LivingRoots/ModConstants.cs` with soil-growth constants (quote constraints verbatim from `spec.md` / `data-model.md`): `SoilGrowthPoorMultiplier = 1.15`; `SoilGrowthHealthyMultiplier = 0.85`; `SoilQualityHealthyBoost = 0.08`; `SoilExtraHarvestHealthyBoost = 0.10` (`spec.md` Q3/Q4; `data-model.md` 21-25)
- [ ] T010 [P] Verify `HealthCategory` enum (`Domain/Visualization/HealthCategory.cs`) uses `ROADMAP.md` scale (`Poor` [0,30]; `Moderate` [31,70]; `Healthy` [71,100]) — reconcile `spec-001` half-open as artifact only (`FR-004` 77)
- [ ] T011 [P] Verify `SoilHealthService.cs` (`Services/`) applies `HealthValue_ClampedToMinMax` with `NaN`/`±Inf` guards (`FR-007` 80) and sparse persistence (`AGENTS.md` anti-pattern: sparse save)
- [ ] T012 [P] Confirm `HostAtomicLockService` interface (`Domain/Interfaces/IHostAtomicLockService.cs`) and `Services/HostAtomicLockService.cs` available for per-tile guard (`FR-006` 79; `Principle VI`)
- [ ] T013 [P] Confirm `VisualizationService.cs` (`Services/Visualization/VisualizationService.cs` 247-285) floating-text / `TemporaryAnimatedSprite` pattern exists for harvest feedback reuse (`A4` 89; `FR-002`/`FR-003`)
- [ ] T014 Design chunk mechanism specification (`FR-001` contingency — `subagent-response-q7.md`; `spec.md` Q7 34): combined chunk (`~1000` tiles/chunk; `~2ms` budget; prime-modulus stride `61`; `ExecuteWithConcurrencyGuard` at `OnDayStarted` — `ModController.cs` 2100 pattern exists for `OnSaveLoaded`/`OnSaving`; must add at `OnDayStarted`). Design locked (`Clarifications` Q7 34); implementation deferred to tasks (`plan.md` 58; `subagent-response-q7.md`: zero chunk mechanism exists in code today — only linear scan at `SoilDecayService.cs` 86-105).
- [ ] T015 [P] Design `Crop.harvest` intercept hook (`A4` / harvest feedback): reuse existing `VisualizationService` floating-text (`CompostApplicationService.cs` 178-191); zero new `Assets/*.png`; zero new `i18n` key (`FR-008` / `Non-Goals` 122-134); no `ContentPatcher`; no `Data/Crops` edit (`spec.md` 122-134)

**Checkpoint**: Foundation ready — `SC-008` concurrent prerequisite acknowledged; `GamePath` spike prerequisite acknowledged; chunk mechanism designed but deferred; contracts verified; constants updated; `HealthCategory` scale verified. User story implementation can now begin (implementation blocked behind RED PHASE in Phase 3).

---

## Phase 3: RED PHASE — All User Story Tests (Property-Based + Invariants) [P]

**Purpose**: TDD — ALL tests written FIRST; must FAIL (RED PHASE) before ANY implementation. **BLOCKING**: No implementation task (Phase 4+) may begin until ALL tasks in this phase are complete and the resulting tests are confirmed failing. This phase uses property-based testing (`FsCheck` — `FsCheck.dll` / `FsCheck.Xunit.dll` present in `LivingRoots.Tests/bin/`) wherever invariants apply.

**Independent Test Criteria per Story**:
- **US1** (`SC-001`/`SC-002`/`SC-003`): Poor tile growth > Medium > Healthy; `Moderate` = vanilla parity; multiplicative on base `phaseDays` before vanilla modifiers (`A2` compositional).
- **US2** (`SC-004`): Poor → 0 stars (override permitted only for Poor tier — `A2` exception documented); Moderate → vanilla roll; Healthy → enhanced (`+0.08`); floating-text confirmation (`A4`).
- **US3** (`SC-005`): Healthy (`≥71`) per-tile extra harvest probability > base (`base + 0.10`); floating-text confirmation (`A4`); no duplication (`US3` 58).
- **US4** (`SC-006`/`SC-007`/`SC-008`): Overlay preserved (`SC-006`); save/reload consistent (`SC-007`); verification pipeline includes `SC-008` rollback (`manual GamePath` + `StillConsumesBin`).

### User Story 1 — Growth Modifier Tests (`FR-001`) [US1] [P]

- [ ] T016 [P] [US1] Contract test: `ISoilHealthService` load/query/update contract (`contracts/i-soil-health-service.md` §`ISoilHealthService`) — `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs`
- [ ] T017 [P] [US1] Property-based invariant test (`FsCheck`): `HealthCategory` derivation from `HealthValue` — `Poor` = [0,30], `Moderate` = [31,70], `Healthy` = [71,100] (`FR-004` 77; `ROADMAP.md` 36-38; `data-model.md` 16-17) — `LivingRoots.Tests/Invariants/SoilHealthCategoryInvariantsTests.cs`
- [ ] T018 [P] [US1] Property-based invariant test: Growth multiplier constraints (`SoilGrowthPoorMultiplier` = 1.15 > 1.0; `SoilGrowthHealthyMultiplier` = 0.85 < 1.0; `Moderate` = 1.00; all `float`/`double` within `[0.5, 2.0]`) (`spec.md` Q3; `FR-001` 74) — `LivingRoots.Tests/Invariants/GrowthMultiplierInvariantsTests.cs`
- [ ] T019 [P] [US1] Integration test: `DayStarted` scan applies multiplicative `phaseDays` modifier BEFORE vanilla modifiers (`A2`; `FR-001` 74; `data-model.md` 30-35) — must FAIL until `T028` (implementation) completes — `LivingRoots.Tests/GrowthModifierIntegrationTests.cs`
- [ ] T020 [P] [US1] Property-based invariant test: Chunk mechanism stride (`prime-modulus stride 61`) produces disjoint ranges (no tile processed twice; no tile missed) — `LivingRoots.Tests/Invariants/ChunkStrideInvariantsTests.cs`
- [ ] T021 [P] [US1] Security/path sanitization invariant (`Principle II`): `LocationName` (`FormC`) and `TileKey` (`SanitizePathSegments`) always sanitized before any `SoilHealthService` query/update — `LivingRoots.Tests/Invariants/SecuritySanitizationInvariantsTests.cs`
- [ ] T022 [P] [US1] Async-only invariant (`Principle III`): No `.Result` or `.Wait()` in `DayStarted` handler; `volatile`/`Interlocked` permitted (`AGENTS.md` anti-pattern) — `LivingRoots.Tests/ConcurrencyInvariantsTests.cs`
- [ ] T023 [P] [US1] Performance contract test (`spec-002` contract `:113`/`:176`): Linear scan ≤ `<5ms` at 40,000 tiles (`SoilDecayService.cs` 86-105); chunk budget `~2ms` per chunk (`FR-001` 74; `Clarifications` Q7 34; `subagent-response-q7.md`) — `LivingRoots.Tests/GrowthPerformanceTests.cs`

### User Story 2 — Harvest Quality Tests (`FR-002`) [US2] [P]

- [ ] T024 [P] [US2] Contract test: Harvest quality roll (`FR-002` 75) — `Crop.harvest` event interaction contract (`contracts/i-soil-health-service.md` §External Integration) — `LivingRoots.Tests/ContractTests/HarvestQualityContractTests.cs`
- [ ] T025 [P] [US2] Property-based invariant test: Quality roll formula (`p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01`) preserved; `Poor` forces 0 (only tier allowing override — `A2` exception documented explicitly at `spec.md` 89); `Moderate` = vanilla; `Healthy` adds `+0.08` (`SoilQualityHealthyBoost`) — `LivingRoots.Tests/Invariants/HarvestQualityInvariantsTests.cs`
- [ ] T026 [P] [US2] Integration test: Floating-text message shown when tier modifies harvest outcome (`A4` 89; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191; `Clarifications` Q3/A4 33-34) — `LivingRoots.Tests/HarvestFeedbackTests.cs`
- [ ] T027 [P] [US2] Property-based invariant test: No pixel assertions (`FR-008` 81) — tests assert logic (roll value, tier classification) never pixel/color/alpha — `LivingRoots.Tests/Invariants/NoPixelAssertionInvariantsTests.cs`

### User Story 3 — Extra Harvest Tests (`FR-003`) [US3] [P]

- [ ] T028 [P] [US3] Contract test: Extra harvest probability (`FR-003` 76) — per-tile (not global per seed); `extraProb = base + 0.10` (`SoilExtraHarvestHealthyBoost`) for `Healthy` (`≥71`) — `LivingRoots.Tests/ContractTests/ExtraHarvestContractTests.cs`
- [ ] T029 [P] [US3] Property-based invariant test: Per-tile probability (`base + 0.10` for `Healthy`; `base` only for `Moderate`/`Poor`) — `LivingRoots.Tests/Invariants/ExtraHarvestInvariantsTests.cs`
- [ ] T030 [P] [US3] Integration test: No duplication (`US3` 58 — "sem duplicar itens"; `FR-005` rollback concurrent protects but requires verification) — `LivingRoots.Tests/ExtraHarvestDuplicationTests.cs`
- [ ] T031 [P] [US3] Property-based invariant test: `Data/Crops` (vanilla) must NOT provide global per-seed extra — feature must only modify per-tile (`FR-003` 76; `Non-Goals` 122) — `LivingRoots.Tests/Invariants/NoGlobalExtraInvariantsTests.cs`

### User Story 4 — Verification Pipeline Tests (`FR-009` / `SC-006`–`SC-008`) [US4] [P]

- [ ] T032 [P] [US4] Integration test: `SC-006` — `Overlay` reflects updated health (`VisualizationService` continues to work; `SC-006` 113) — `LivingRoots.Tests/OverlayHealthReflectionTests.cs`
- [ ] T033 [P] [US4] Integration test: `SC-007` — Save/reload preserves growth effect (`SoilHealthState` persistent data consistent with `Crop.phaseDays` sync via `GameLocation.terrainFeatures`) — `LivingRoots.Tests/SaveReloadConsistencyTests.cs`
- [ ] T034 [P] [US4] Contract test: `SC-008` rollback verification (`manual GamePath` + automated `StillConsumesBin` — `CompostingBinServiceTests.cs`:339) — must pass before ANY yield activation (`FR-005` 78; `Dependencies` 136-137) — `LivingRoots.Tests/VerificationPipelineTests.cs`
- [ ] T035 [P] [US4] Property-based invariant test: Sparse save (`AGENTS.md` anti-pattern) — only non-default `HealthValue` stored (`SoilHealthState` dictionary); default `0` never persisted (`data-model.md` 24) — `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs`
- [ ] T036 [P] [US4] Property-based invariant test: `ReGen Valley` interaction (`Assumes` 145) — first-wins host-authoritative (`HostAtomicLockService`); second concurrent mutation refused with message (inventory intact); never last-write-wins — `LivingRoots.Tests/Invariants/ReGenValleyInvariantsTests.cs`

**RED PHASE BLOCKING STATUS**: Confirm ALL `T016`–`T036` tasks complete and ALL resulting tests FAIL before proceeding to Phase 4. If any test does not exist or does not fail, Phase 4 is BLOCKED. Document RED status in session state file.

**Checkpoint**: RED PHASE complete — all 21 test tasks (T016–T036) written; all tests confirmed failing; property-based invariants cover `HealthCategory`, growth multipliers, chunk stride, harvest quality roll, extra harvest probability, sparse save, `ReGen Valley` interaction, security sanitization, async-only, no pixel assertions, and `SC-008` rollback prerequisite. Implementation may proceed.

---

## Phase 4: Implementation — User Story 1 (`SC-001` / `SC-002` / `SC-003`) [US1]

**Goal**: Soil-health growth modifier applies multiplicatively to `Crop.phaseDays` at `DayStarted` (`ModController.OnDayStarted` 1719-1778; `Option A` — public `Crop.newDay` field scan; no `Harmony`). Host-only (`IsMainPlayer` / `Context.IsMainPlayer`; `HostAtomicLockService` per-tile guard; `FR-001` / `FR-006` 79).

**Independent Test** (before activation): Confirm `SC-003` (`Moderate` = vanilla parity) using manual observation or automated growth-time comparison (`SC-001`: `Poor` > `Medium`; `SC-002`: `Healthy` < `Medium`).

- [ ] T037 [US1] Implement `Crop.newDay` public-field scan (`DayStarted`) in `LivingRoots/Controllers/ModController.cs` — extend existing `OnDayStarted` handler (1719-1778) or add new hook; apply multiplicative modifier (`SoilGrowthPoorMultiplier` / `SoilGrowthHealthyMultiplier`) to `phaseDays` BEFORE vanilla modifiers (`A2`; `FR-001` 74; `data-model.md` 30-35)
- [ ] T038 [US1] Add `HostAtomicLockService` per-tile guard (`IsMainPlayer` / `Context.IsMainPlayer`) for `FR-001` host-only persistent phase (`FR-006` 79; `HostAtomicLockService.cs` 7-60; `ModController` 1483-1492 `HoeDirt` filter pattern)
- [ ] T039 [US1] Update `ModConstants.cs` — confirm `SoilGrowthPoorMultiplier` (`1.15`) and `SoilGrowthHealthyMultiplier` (`0.85`) applied; no hardcoded values (`AGENTS.md` anti-pattern)
- [ ] T040 [P] [US1] Implement chunk mechanism (`FR-001` contingency — `Clarifications` Q7 34): combined chunk (`~1000` tiles/chunk; `~2ms` budget; prime-modulus stride `61`); spread across ticks when linear exceeds `<5ms` at 40,000 tiles; add `ExecuteWithConcurrencyGuard` at `OnDayStarted` (`ModController` 2100 pattern exists for `OnSaveLoaded`/`OnSaving`; must add at `OnDayStarted`) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/SoilDecayService.cs`
- [ ] T041 [US1] Verify `A2` compositional consistency: `Moderate` (`31-70`) must stay exactly vanilla (`1.00×`); `Poor` (`0-30`) slower (`1.15×`); `Healthy` (`71-100`) faster (`0.85×`); modifier applied to base `phaseDays` before `Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays` (`FR-001` 74; `spec.md` A2 87)
- [ ] T042 [P] [US1] Verify `GamePath` spike (`A1` 86) — confirm `Crop.newDay` public-field ordering with vanilla growth (`plan.md` §Phase 0 / `Dependencies` 141); if ordering fails, document `B` fallback (`HoeDirt.GetFertilizerSpeedBoost()` `Harmony` patch) — but `B` is NOT implemented (zero `Harmony` footprint required; `AGENTS.md` anti-pattern; `A1` stands)
- [ ] T043 [US1] Confirm `FR-007`: Fallback to vanilla (`1.00×`) if health data missing/corrupted/out-of-bounds (`0-100`); `SoilHealthService` tolerant reader and `HealthValue_ClampedToMinMax` with `NaN`/`±Inf` guards (`FR-007` 80)
- [ ] T044 [US1] Confirm `SC-009` (`Manual observation`) — `GamePath` play-through verifies growth differences (`SC-001`/`SC-002`) and parity (`SC-003`) before activation (`FR-009` 82)

**Checkpoint**: US1 complete — growth modifier applied; `GamePath` verified; `SC-001` (`Poor` > `Medium`), `SC-002` (`Healthy` < `Medium`), `SC-003` (`Moderate` = vanilla) observable; chunk mechanism implemented or verified; `HostAtomicLockService` guard active; `FR-001` activation complete.

---

## Phase 5: Implementation — User Story 2 (`SC-004`) [US2]

**Goal**: Harvest quality roll incorporates tile health additively (`A2` compositional; never overriding vanilla `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01`): `Poor` forces 0 (only tier with override exception — `spec.md` 89); `Moderate` = vanilla; `Healthy` `+0.08` (`FR-002` 75 / `SC-004` 110). Observable: floating-text message when tier modifies outcome (`A4` 89).

- [ ] T045 [US2] Implement harvest-intercept hook at `Crop.harvest` in `LivingRoots/Controllers/ModController.cs` — new hook required (zero harvest hooks exist today; `ModController` has zero `Crop.harvest` references beyond `message.harvest.inventory-full` at `Constants.cs` 56; `Clarifications` Q3/A4 33-34)
- [ ] T046 [US2] Implement quality roll (`FR-002` 75): Poor tier → force 0 (override permitted only for Poor — `A2` exception); Moderate → vanilla roll; Healthy → `vanilla + 0.08` (`SoilQualityHealthyBoost`) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/SoilHealthService.cs`
- [ ] T047 [US2] Confirm `A2` compositional consistency (`FR-002` 75; `A2` 87): roll is composed (not replaced); `Poor` exception is tier-conditional only — `LivingRoots/Controllers/ModController.cs`
- [ ] T048 [P] [US2] Reuse `VisualizationService` floating-text (`TemporaryAnimatedSprite`) for harvest feedback (`A4` 89; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/Visualization/VisualizationService.cs`
- [ ] T049 [US2] Update `ModConstants.cs` — confirm `SoilQualityHealthyBoost` (`0.08`); no new `i18n` key; reuse existing `floating-text` / `restoration-text` patterns (`Non-Goals` 122-134)
- [ ] T050 [US2] Confirm `FR-003` (`Extra harvest`) interaction: `US2` quality roll does NOT conflict with `US3` extra-harvest probability (`FR-002` additive quality; `FR-003` additive extra; independent mechanisms) — `LivingRoots/Controllers/ModController.cs`
- [ ] T051 [US2] Confirm zero `Harmony` footprint (`AGENTS.md` anti-pattern) — `FR-002` uses `Crop.harvest` public-field scan or direct event hook (no `Lib.Harmony` patch)

**Checkpoint**: US2 complete — harvest quality reflects tier (`SC-004`); floating-text confirmation (`A4`); `A2` exception documented (`spec.md` 89); no new assets; `SC-004` observable via manual `GamePath`.

---

## Phase 6: Implementation — User Story 3 (`SC-005`) [US3]

**Goal**: Per-tile extra harvest on `Healthy` tier (`≥71`) — `extraProb = base + 0.10` (`SoilExtraHarvestHealthyBoost`). Not globally per seed (`FR-003` 76; `US3` 58). Observable: floating-text confirmation (`A4`) when extra item produced; no duplication (`US3` 58; `FR-005` rollback protects but verification required).

- [ ] T052 [US3] Implement per-tile extra harvest probability (`FR-003` 76) — `extraProb = baseDataCropsExtraHarvestChance + ModConstants.SoilExtraHarvestHealthyBoost` (`+0.10`) — applied only when `HealthCategory.Healthy` (`≥71`) — `LivingRoots/Controllers/ModController.cs`
- [ ] T053 [US3] Confirm no global per-seed extra (`US3` 58; `FR-003` 76; `Non-Goals` 122) — feature must NOT edit `Data/Crops` globally; only modify per-tile at harvest time — `LivingRoots/Controllers/ModController.cs`
- [ ] T054 [P] [US3] Confirm `SC-005`: `Healthy` tile shows extra item rate > `Moderate` tile (`base + 0.10` > `base`); manual `GamePath` observation required (`SC-009` / `FR-009` 82) — `LivingRoots.Tests/ExtraHarvestDuplicationTests.cs`
- [ ] T055 [US3] Confirm `FR-005` rollback (`SC-008`) interaction — duplication hazard (`outputCount > 1`) must not affect `US3` (extra harvest never duplicates; rollback protects inventory-full case only; `SC-008` concurrent prerequisite) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/CompostingBinService.cs`
- [ ] T056 [P] [US3] Reuse floating-text (`VisualizationService`) for extra-item confirmation (`A4` 89; `Clarifications` Q3/A4 33-34) — `LivingRoots/Controllers/ModController.cs`
- [ ] T057 [US3] Confirm zero duplication (`US3` 58 — "sem duplicar itens") — `LivingRoots/Controllers/ModController.cs`

**Checkpoint**: US3 complete — `SC-005` observable (`Healthy` > `base`); floating-text confirmation; no duplication; per-tile only; `FR-003` activation complete.

---

## Phase 7: Verification — User Story 4 (`SC-006` / `SC-007` / `SC-008`) [US4]

**Goal**: Trust / verification pipeline (`US4` / `FR-009` 82; `SC-008` / `SC-009`). Confirm `SC-006` (`Overlay` preserved), `SC-007` (save/reload consistent), `SC-008` (rollback verification + `GamePath` spike) pass; `SC-009` (manual observation) confirms full loop. This phase also includes the `GamePath` spike (`A1`) verification gate (`Dependencies` 141) and `SC-008` activation gate (`FR-005` 78; `Dependencies` 136-137).

- [ ] T058 [US4] Verify `SC-006` — `Overlay` reflects updated `HealthCategory` (`VisualizationService` continues to work; `SC-006` 113) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/Visualization/VisualizationService.cs`
- [ ] T059 [P] [US4] Verify `SC-007` — Save/reload consistency (`SoilHealthState` persistent data + `Crop.phaseDays` sync through `GameLocation.terrainFeatures`; `data-model.md` 91-92; `FR-007` 80) — `LivingRoots.Tests/SaveReloadConsistencyTests.cs`
- [ ] T060 [US4] Confirm `SC-008` rollback verification (`manual GamePath` play-through + automated `StillConsumesBin` — `CompostingBinServiceTests.cs`:339) passes BEFORE ANY yield activation (`FR-005` 78; `Dependencies` 136-137; user `B` concurrent selection — design/implementation permitted concurrently; activation blocked) — `LivingRoots.Tests/VerificationPipelineTests.cs`
- [ ] T061 [US4] Confirm `SC-009` (`Manual observation`) — `GamePath` play-through verifies full loop: apply compost → observe faster/slower growth (`SC-001`/`SC-002`) and parity (`SC-003`) → harvest quality (`SC-004`) → extra harvest (`SC-005`) → save/reload (`SC-007`) — `LivingRoots/Controllers/ModController.cs` / `quickstart.md` §Scenarios
- [ ] T062 [US4] Confirm `A1` (`GamePath` spike) — verify `Crop.newDay` public-field ordering with vanilla growth (`plan.md` §Phase 0; `Dependencies` 141) — `LivingRoots/Controllers/ModController.cs`
- [ ] T063 [US4] Confirm `A5` (`ReGen Valley` interaction — `Assumes` 145): first-wins host-authoritative (`HostAtomicLockService`); second concurrent mutation refused with message (inventory intact); never last-write-wins (`Principle VI`; `Clarifications` Q5 31-32) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/HostAtomicLockService.cs`
- [ ] T064 [P] [US4] Confirm `FR-009` (`Mean-verified`) — `ROADMAP.md` reconciliation (`FR-004` 77; `A3` 88); `ModConstants` values (`SoilGrowthPoorMultiplier` = 1.15, `SoilGrowthHealthyMultiplier` = 0.85, `SoilQualityHealthyBoost` = 0.08, `SoilExtraHarvestHealthyBoost` = 0.10); no contradiction (`quickstart.md` §Scenarios; `checklists/verification-pipeline.md` CHK-101–CHK-208)
- [ ] T065 [US4] Confirm `SC-003` (`Moderate` = vanilla parity) — `Moderate` tile (`[31,70]`) must grow at exactly vanilla rate (`1.00×`) with same modifiers (`Speed-Gro`, `Agriculturist`, etc.) — `LivingRoots/Controllers/ModController.cs` / `quickstart.md` §Scenario 1
- [ ] T066 [US4] Confirm `SC-004` (`Harvest quality`) — manual `GamePath`: harvest from `Poor` (`[0,30]`) → 0 stars; `Moderate` → vanilla roll; `Healthy` (`[71,100]`) → enhanced (`+0.08`) — `quickstart.md` §Scenario 2; `LivingRoots.Tests/HarvestQualityTests.cs`
- [ ] T067 [US4] Confirm `SC-005` (`Extra harvest`) — manual `GamePath`: multiple harvests from `Healthy` vs. `Moderate`; confirm higher rate (`base + 0.10`) and no duplication — `quickstart.md` §Scenario 3; `LivingRoots.Tests/ExtraHarvestTests.cs`
- [ ] T068 [P] [US4] Confirm no `Harmony` footprint (`A1` — `AGENTS.md` anti-pattern; `spec.md` A1 86; zero `Lib.Harmony` dependency) — `LivingRoots/Controllers/ModController.cs`
- [ ] T069 [P] [US4] Confirm `FR-008` (`No pixel assertions`) — all tests assert logic (`phaseDays` multiplier, quality roll value, tier classification) — never color, pixel, blend alpha, or overlay appearance (`SC-006` visual preservation only) — `LivingRoots.Tests/HarvestQualityTests.cs` / `LivingRoots.Tests/GrowthModifierTests.cs`

**Checkpoint**: US4 / Verification complete — `SC-008` rollback verification passes (`manual` + `StillConsumesBin`); `SC-006` preserved; `SC-007` consistent; `SC-009` manual observation confirms full loop; `SC-003` parity verified; `SC-004` quality verified; `SC-005` extra harvest verified; `GamePath` spike verified; `ReGen Valley` interaction documented; `FR-009` mean-verified; `FR-008` no pixel assertions confirmed. **This is the ONLY phase that fully attests feature success and completeness (per user instruction 3).**

---

## Phase 8: Implementation — Chunk Mechanism (Performance Contingency) [P]

**Purpose**: `FR-001` contingency (`Clarifications` Q7 34; `subagent-response-q7.md`): if linear scan exceeds `<5ms` at 40,000 tiles, activate combined chunk (`~1000` tiles/chunk; `~2ms` budget; stride `61`); spread across ticks; `ExecuteWithConcurrencyGuard` at `OnDayStarted`. Design locked (`plan.md` 58); zero chunk mechanism exists today (`SoilDecayService.cs` 86-105 linear only).

- [ ] T070 [P] Implement chunk mechanism (`~1000` tiles/chunk; `~2ms` budget; prime-modulus stride `61`) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/SoilDecayService.cs`
- [ ] T071 [P] Confirm chunk mechanism activates only when linear exceeds `<5ms` at 40,000 tiles (`spec-002` contract `:113`/`:176`; `FR-001` 74)
- [ ] T072 [P] Confirm `ExecuteWithConcurrencyGuard` (`Interlocked.CompareExchange`) protects `OnDayStarted` (`ModController` 2100 pattern) — `LivingRoots/Controllers/ModController.cs`
- [ ] T073 Confirm chunk mechanism does not break `SC-007` (save/reload consistency) or `SC-003` (parity) — `LivingRoots/Controllers/ModController.cs`

---

## Phase 9: Polish & Cross-Cutting Concerns [P]

**Purpose**: Improvements affecting multiple stories; final verification; security hardening; documentation reconciliation.

- [ ] T074 [P] Confirm `SC-006` (`Overlay` preserved) — `VisualizationService.cs` continues to reflect updated `HealthCategory`; no regression in `OverlayRenderer`, `TooltipRenderer`, `HoeFeedbackRenderer` — `LivingRoots/Services/Visualization/`
- [ ] T075 [P] Security hardening (`Principle II`): verify `SanitizePathSegments` (`FormC`) applied to all new input paths (`LocationName`, `TileKey`) — `LivingRoots/Services/ModDataService.cs` / `LivingRoots/Domain/FileNameSanitizationService.cs`
- [ ] T076 [P] Confirm no new `Assets/*.png` (`A4` 89; `FR-008` / `Non-Goals` 122-123) — `LivingRoots/Assets/` unchanged; `Data/Crops` not edited; `i18n/default.json` / `i18n/en.json` / `i18n/pt.json` unchanged (reuse existing `floating-text` / `restoration-text` keys only)
- [ ] T077 [P] Confirm `SC-007` (`Save/reload`) — `SoilHealthState` persistence (`ModDataService`) + `Crop.phaseDays` sync through `GameLocation.terrainFeatures` — `LivingRoots/Services/ModDataService.cs` / `LivingRoots/Services/SoilHealthService.cs`
- [ ] T078 [P] Confirm `FR-006` (`Host-authoritative`) — `FR-001` (`DayStarted`) executes host-only (`IsMainPlayer` / `Context.IsMainPlayer`); `FR-002`/`FR-003` computed locally from shared `SoilHealthService`; no new multiplayer transport (`Principle VI`; `A5` 90) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/HostAtomicLockService.cs`
- [ ] T079 [P] Confirm `SC-009` (`Manual play-through`) — full loop verified (`quickstart.md` §Scenarios 1–4) — `specs/005-soil-health-to-crop-growth/quickstart.md`
- [ ] T080 [P] Confirm `FR-009` (`Mean-verified`) — `ROADMAP.md` reconciliation (`FR-004` 77); `ModConstants` values documented; `COMPLETION_SUMMARY.md` reconciled; `.specify/assessments/` artifacts show evidence-backed status; `research.md` cited (`research/2026-10-roadmap-feasibility-and-agronomy.md`) — `specs/005-soil-health-to-crop-growth/checklists/verification-pipeline.md`
- [ ] T081 [P] Confirm design deferred items (`plan.md` §Phase 2 Note) — `Crop.newDay` scan (`FR-001`), chunk mechanism (`FR-001` contingency), harvest-intercept hook (`A4`), `SC-008` verification, `GamePath` spike — all either completed or deferred with clear prerequisites — `specs/005-soil-health-to-crop-growth/plan.md`
- [ ] T082 [P] Confirm `SC-008` concurrent prerequisite (`user B`) — design/implementation permitted concurrently; yield activation (`FR-005`) blocked until `SC-008` green (`manual GamePath` + `StillConsumesBin`) — `LivingRoots/Services/CompostingBinService.cs` / `LivingRoots.Tests/CompostingBinServiceTests.cs`:339
- [ ] T083 [P] Confirm zero `Harmony` footprint (`AGENTS.md` anti-pattern) — `Lib.Harmony` not referenced; direct `Crop.newDay` public-field scan (`A1` 86) — `LivingRoots/Controllers/ModController.cs`
- [ ] T084 [P] Confirm no premature abstractions (`Principle V`) — `Crop.harvest` hook reuses `VisualizationService` floating-text; no new abstract base for harvest intercept (`A4` 89) — `LivingRoots/Controllers/ModController.cs`
- [ ] T085 [P] Confirm `SC-003` (`Moderate` parity) — `Moderate` (`[31,70]`) must stay exactly `1.00×` with same vanilla modifiers — `LivingRoots/Controllers/ModController.cs` / `LivingRoots.Tests/GrowthModifierIntegrationTests.cs`
- [ ] T086 [P] Confirm `SC-004` (`Harvest quality`) — `Poor` (`[0,30]`) → 0 stars (`FR-002` 75; `A2` exception); `Moderate` (`[31,70]`) → vanilla roll; `Healthy` (`[71,100]`) → enhanced (`+0.08`) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots.Tests/HarvestQualityTests.cs`
- [ ] T087 [P] Confirm `SC-005` (`Extra harvest`) — `Healthy` (`≥71`) per-tile probability > base (`+0.10`); floating-text confirmation; no duplication (`US3` 58) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots.Tests/ExtraHarvestTests.cs`

---

## Phase 10: Final Verification (ONLY Verification Phase Attests Success) [P]

**Purpose**: Per user instruction: verification tasks performed at the end. **ONLY these verification tasks attest to the success and completeness of the implementation — NEVER the agent itself.** This phase includes the `SC-008` activation gate, `GamePath` final verification, and cross-story integration.

**Critical**: This phase must complete BEFORE declaring feature complete. If `SC-008` rollback (`manual GamePath` + `StillConsumesBin`) is not green, `FR-005` activation (`FR-002`/`FR-003` yield activation) is BLOCKED — regardless of any other green tests or agent claims.

- [ ] T088 [US4] [P] **VERIFICATION TASK**: Confirm `SC-008` rollback verification (`CompostingBinServiceTests.cs`:339 — `StillConsumesBin`) passes (`manual GamePath` + automated) — **MANDATORY GATE** before yield activation (`FR-005` 78; `Dependencies` 136-137) — `LivingRoots.Tests/VerificationPipelineTests.cs`
- [ ] T089 [US4] [P] **VERIFICATION TASK**: Confirm `SC-001` / `SC-002` / `SC-003` growth differences observable via `GamePath` (`manual` — `FR-009` 82; `SC-009`) — `quickstart.md` §Scenario 1; `LivingRoots/Controllers/ModController.cs`
- [ ] T090 [US4] [P] **VERIFICATION TASK**: Confirm `SC-004` harvest quality observable (`manual` — poor = 0 stars; healthy enhanced; floating-text) — `quickstart.md` §Scenario 2; `LivingRoots/Controllers/ModController.cs`
- [ ] T091 [US4] [P] **VERIFICATION TASK**: Confirm `SC-005` extra harvest observable (`manual` — healthy rate > base; no duplication; floating-text) — `quickstart.md` §Scenario 3; `LivingRoots/Controllers/ModController.cs`
- [ ] T092 [US4] [P] **VERIFICATION TASK**: Confirm `SC-007` save/reload consistency — `SoilHealthState` + `Crop.phaseDays` sync preserved after reload — `LivingRoots.Tests/SaveReloadConsistencyTests.cs`
- [ ] T093 [US4] [P] **VERIFICATION TASK**: Confirm `SC-006` overlay preserved — `VisualizationService` reflects updated `HealthCategory` with no regression — `LivingRoots/Services/Visualization/VisualizationService.cs`
- [ ] T094 [US4] [P] **VERIFICATION TASK**: Confirm `SC-003` (`Moderate` = vanilla parity) — `Moderate` tile (`[31,70]`) grows at exactly `1.00×` with same modifiers — `quickstart.md` §Scenario 1; `LivingRoots/Controllers/ModController.cs`
- [ ] T095 [P] **VERIFICATION TASK**: Confirm chunk mechanism (`~1000` tiles; `~2ms` budget; stride `61`) activates only above `<5ms` @ 40k tiles; does not break parity (`SC-003`) or save consistency (`SC-007`) — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/SoilDecayService.cs`
- [ ] T096 [P] **VERIFICATION TASK**: Confirm `A1` (`GamePath` spike) — `Crop.newDay` public-field ordering verified; zero `Harmony` footprint; `FR-001` activation safe — `plan.md` §Phase 0; `spec.md` A1; `LivingRoots/Controllers/ModController.cs`
- [ ] T097 [P] **VERIFICATION TASK**: Confirm zero new assets / `i18n` (`FR-008` / `A4`) — `Assets/` unchanged; `i18n/default.json` unchanged; `Data/Crops` not edited; `ContentPatcher` not used; `config.json` / GMCM not added — `LivingRoots/Assets/` / `LivingRoots/i18n/` / `spec.md` 122-134
- [ ] T098 [P] **VERIFICATION TASK**: Confirm `FR-009` (`Mean-verified`) — `ROADMAP.md` reconciliation (`FR-004` 77); `ModConstants` values verified; `.specify/assessments/` evidence-backed; no `spec-001` half-open leakage; `research.md` cited (`2026-10-roadmap-feasibility-and-agronomy.md`) — `specs/005-soil-health-to-crop-growth/checklists/verification-pipeline.md`
- [ ] T099 [P] **VERIFICATION TASK**: Confirm no false-green risk — `SC-008` rollback verification (`manual` + `StillConsumesBin`) executed (not just documented); manual observation (`SC-009`) executed (not theoretical); `SC-003` parity measured; `SC-004` quality measured; `SC-005` extra measured; no pixel assertions (`FR-008`); automation gap (`FR-009`) acknowledged — `checklists/verification-pipeline.md` CHK-101–CHK-208; `spec.md` §Open Questions 156
- [ ] T100 [P] **VERIFICATION TASK**: Confirm `Principle VI` (Multiplayer First-Class) — host-authoritative (`IsMainPlayer` / `Context.IsMainPlayer`); `HostAtomicLockService`; first-wins; inventory intact on refusal; single-player = host with no clients; no second source of truth — `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/HostAtomicLockService.cs` / `.specify/memory/constitution.md` v2.0.0

**Completion Criteria (ONLY verification phase attests success — NEVER agent)**:
- [ ] `SC-008` rollback (`manual GamePath` + `StillConsumesBin` — `CompostingBinServiceTests.cs`:339) verified GREEN (`FR-005` 78; `Dependencies` 136-137)
- [ ] `SC-001` (`Poor` > `Medium`) verified
- [ ] `SC-002` (`Healthy` < `Medium`) verified
- [ ] `SC-003` (`Moderate` = vanilla) verified
- [ ] `SC-004` (harvest quality: Poor=0, Moderate=vanilla, Healthy enhanced) verified
- [ ] `SC-005` (extra harvest: Healthy > base; no duplication) verified
- [ ] `SC-006` (overlay preserved) verified
- [ ] `SC-007` (save/reload consistent) verified
- [ ] `SC-009` (`Manual observation`) verified
- [ ] `FR-009` (`Mean-verified`) verified (`ROADMAP.md` reconciliation; `ModConstants` values; `.specify/assessments/` evidence)
- [ ] `A1` (`GamePath` spike) verified (zero `Harmony`; direct `Crop.newDay` scan)
- [ ] `FR-001` activation complete; `SC-008` gate passed; `GamePath` verified

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — starts immediately. Must verify contracts (`i-soil-health-service.md`), data model (`data-model.md`), prerequisites (`SC-008` concurrent; `GamePath` spike).
- **Phase 2 (Foundational)**: Depends on Phase 1. Updates `ModConstants.cs`, verifies `HealthCategory` scale, confirms `HostAtomicLockService`, designs chunk mechanism (deferred to tasks; design locked). **Blocks all user stories.**
- **Phase 3 (RED PHASE — All Tests)**: Depends on Phase 2. **BLOCKING**: No Phase 4 (US1 implementation) may begin until ALL `T016`–`T036` complete and ALL resulting tests FAIL (RED PHASE). Property-based invariants (`FsCheck`) cover `HealthCategory`, growth multipliers, chunk stride, harvest quality roll, extra harvest probability, sparse save, `ReGen Valley` interaction, security sanitization, async-only, no pixel assertions.
- **Phase 4 (US1 — Growth)**: Depends on Phase 3 RED PHASE complete. Independent of US2/US3 (story-level independence). `SC-003` parity is the critical verification.
- **Phase 5 (US2 — Quality)**: Depends on Phase 3 RED PHASE complete. Independent of US1/US3. `SC-004` (poor=0, healthy enhanced) is critical.
- **Phase 6 (US3 — Extra)**: Depends on Phase 3 RED PHASE complete. Independent of US1/US2. `SC-005` (healthy > base, no duplication) is critical.
- **Phase 7 (US4 — Verification)**: Depends on Phase 6 (all stories implemented). Includes `SC-008` activation gate (`FR-005`), `SC-009` manual observation, `GamePath` spike verification (`A1`), `SC-007` save/reload, `SC-006` overlay, `FR-009` mean-verified.
- **Phase 8 (Chunk)**: Parallel `P` tasks; can run with Phase 4–6 but must complete before `SC-009` manual verification of performance.
- **Phase 9 (Polish)**: Depends on Phase 7. Cross-cutting: overlay preservation, security hardening, documentation reconciliation (`plan.md`, `quickstart.md`, `data-model.md`), zero assets verification (`FR-008`), multiplayer (`Principle VI`), no premature abstractions (`Principle V`).
- **Phase 10 (VERIFICATION — ONLY attests success)**: Final verification gate. `SC-008` rollback (`manual GamePath` + `StillConsumesBin`) must be GREEN (`FR-005`). `SC-001`–`SC-005` observed. `SC-009` executed. `FR-009` verified. **Agent NEVER attests — only these tasks.**

### User Story Dependencies

- **US1 (Growth)**: No dependencies on US2/US3 (independent gameplay mechanism — `DayStarted` persistent phase). `SC-008` concurrent prerequisite (`B`) does not block US1 design/implementation.
- **US2 (Quality)**: No dependencies on US1/US3. Independent discrete event (`Crop.harvest` — client-local computation from shared tile health; `FR-006` 79).
- **US3 (Extra)**: No dependencies on US1/US2. Independent per-tile probability (`FR-003` 76).
- **US4 (Verification)**: Depends on US1–US3 (must observe effects) and `SC-008` rollback (activation gate). `SC-009` requires `GamePath` (`A1` 86) and manual observation (`FR-009` 82).

### Within Each User Story (TDD Flow — Mandatory)

- Tests (`Phase 3`) MUST be written FIRST and MUST FAIL (`RED PHASE`) before any implementation (`Phase 4+`).
- Models (`ModConstants.cs` updates) before services (`SoilHealthService` / `CompostingBinService` modifications).
- Services before controllers (`ModController` event handlers: `OnDayStarted`, `Crop.harvest` hook).
- Core implementation (`DayStarted` scan / `Crop.harvest` hook) before chunk mechanism / floating-text integration.
- Integration (`SC-007` save/reload; `SC-009` manual) before final verification (`Phase 10`).

---

## Parallel Execution Examples

```bash
# Wave 1 (Setup + Foundational) — Subagent A (shared context: infrastructure/contracts):
Task: "T001 Verify feature branch / design docs present"
Task: "T004 Read contracts / data-model / quickstart"
Task: "T009 Update ModConstants.cs with soil-growth constants"
Task: "T012 Confirm HostAtomicLockService / VisualizationService available"

# Wave 2 (RED PHASE — All Story Tests) — Subagent B (shared context: property-based / invariants; reuse same subagent for testing patterns):
Task: "T016 [US1] Contract test: ISoilHealthService growth query"
Task: "T024 [US2] Contract test: Harvest quality roll"
Task: "T032 [US4] Integration test: SC-007 save/reload consistency"
Task: "T017 [US1] Property-based: HealthCategory derivation invariant"
Task: "T025 [US2] Property-based: Quality roll invariant"
Task: "T035 [US4] Property-based: Sparse save invariant"

# Wave 3 (Implementation — US1 Growth) — Subagent C (shared context: US1 / ModController / DayStarted / chunk mechanism):
Task: "T037 [US1] Implement Crop.newDay public-field scan (DayStarted)"
Task: "T040 [US1] Implement chunk mechanism (~1000 tiles; stride 61)"
Task: "T041 [US1] Verify A2 compositional consistency (Moderate = 1.00x)"

# Wave 4 (Implementation — US2 Quality) — Subagent D (shared context: US2 / harvest hook / floating-text):
Task: "T045 [US2] Implement harvest-intercept hook at Crop.harvest"
Task: "T048 [US2] Reuse VisualizationService floating-text (A4)"
Task: "T047 [US2] Confirm A2 exception documented (Poor override only)"

# Wave 5 (Implementation — US3 Extra) — Subagent E (shared context: US3 / extra harvest / per-tile):
Task: "T052 [US3] Implement extra harvest probability (Healthy >= 71)"
Task: "T054 [US3] Confirm SC-005 (per-tile > base; no duplication)"
Task: "T056 [US3] Reuse floating-text for extra-item confirmation"

# Wave 6 (Verification + Integration — US4 + Chunk) — Subagent F (shared context: verification / SC-008 / manual / integration):
Task: "T058 [US4] Verify SC-006 (Overlay preserved)"
Task: "T060 [US4] Confirm SC-008 rollback verification (manual + StillConsumesBin) GREEN"
Task: "T090 [US4] Confirm SC-001 / SC-002 / SC-003 observed (manual GamePath)"
Task: "T070 [P] Implement chunk mechanism spread (contingency)"

# Wave 7 (Polish) — Subagent G (shared context: cross-cutting / security / docs / zero assets / multiplayer):
Task: "T074 [P] Confirm overlay preserved (SC-006)"
Task: "T075 [P] Security hardening (SanitizePathSegments / FormC)"
Task: "T077 [P] Confirm SC-007 save/reload consistent"
Task: "T082 [P] Confirm SC-008 concurrent prerequisite status"
Task: "T097 [P] Confirm zero new assets / i18n (FR-008 / A4)"

# Wave 8 (VERIFICATION — ONLY attests success — NEVER agent) — Subagent H (shared context: final verification / proof):
Task: "T088 [US4] VERIFICATION: SC-008 rollback verified GREEN"
Task: "T089 [US4] VERIFICATION: SC-001 / SC-002 / SC-003 observed"
Task: "T091 [US4] VERIFICATION: SC-005 observed (manual)"
Task: "T099 [US4] VERIFICATION: No false-green risk documented"
Task: "T100 [US4] VERIFICATION: Principle VI multiplayer verified"
```

---

## Implementation Strategy

### MVP First (US1 Only — `SC-001`/`SC-002`/`SC-003`)

1. Complete Phase 1 (`T001`–`T007`) — Setup + Foundational.
2. Complete Phase 2 (`T008`–`T015`) — Foundational prerequisites (`ModConstants`, contracts, `HealthCategory`, chunk design).
3. Complete Phase 3 (`T016`–`T036`) — RED PHASE (all tests FAIL).
4. Complete Phase 4 (`T037`–`T044`) — US1 Growth (`FR-001` / `SC-001`–`SC-003`).
5. **STOP and VALIDATE (US4 / Phase 7 verification)**: Confirm `SC-003` (`Moderate` = vanilla); `SC-001` (`Poor` > `Moderate`); `SC-002` (`Healthy` < `Moderate`); `SC-009` (`Manual observation`); `SC-008` rollback concurrent acknowledged; `A1` (`GamePath` spike) verified.
6. If any verification fails (`SC-003` not parity; `SC-001`/`SC-002` not observable; `SC-009` manual not done; `SC-008` rollback not verified), STOP — do not activate yield (`FR-002`/`FR-003`).

### Incremental Delivery (Per User Story — TDD Enforced)

1. Phase 1 + 2 → Foundation ready.
2. Phase 3 (`RED PHASE`) → All tests written and FAILING (`T016`–`T036`).
3. Phase 4 (`US1`) → Growth modifier (`FR-001`); `SC-003` parity is the critical verification; `A1` (`GamePath`) required before activation; `SC-008` concurrent prerequisite acknowledged.
4. Phase 5 (`US2`) → Harvest quality (`FR-002`); `SC-004` observable; `A2` exception (`Poor` override only) documented; floating-text (`A4`) reused.
5. Phase 6 (`US3`) → Extra harvest (`FR-003`); `SC-005` observable; per-tile only (`US3` 58); no duplication; floating-text (`A4`) reused.
6. Phase 7 (`US4`) → `SC-006` (`Overlay` preserved); `SC-007` (save/reload); `SC-008` rollback (`manual GamePath` + `StillConsumesBin`); `SC-009` (`Manual`); `FR-009` (`Mean-verified`); `A1` (`GamePath` spike); `A5` (`ReGen Valley`); `SC-003` (parity); `SC-004` (quality); `SC-005` (extra); `SC-008` (rollback); `SC-006` (overlay); `SC-007` (save/reload); `SC-009` (manual); no false-green (`FR-009` 82).
7. Phase 8 (`Chunk`) → Performance contingency (`FR-001`); activates only when linear exceeds `<5ms` @ 40,000 tiles (`spec-002` `:113`/`:176`); `SC-003` preserved; `SC-007` preserved.
8. Phase 9 (`Polish`) → Cross-cutting (`SC-006` preserved; `SC-007` verified; `SC-008` concurrent; `SC-009` manual; `FR-008` no pixel assertions; `FR-009` mean-verified; `Principle VI` multiplayer; `Principle V` no premature abstractions; `Principle II` security; `A4` zero assets; zero `Harmony`).
9. Phase 10 (`VERIFICATION`) — ONLY attests success: `SC-008` rollback GREEN; `SC-001`–`SC-005` observed; `SC-006`–`SC-007` verified; `SC-009` executed; `FR-009` verified; `A1` verified; `Principle VI` verified; `FR-008` no pixel assertions; `FR-005` yield activation blocked until `SC-008` green; agent NEVER claims success.

---

## Notes

- **[P]** = parallelizable (different files; no dependency on incomplete tasks). Tasks within `Phase 3` (RED PHASE) can run in parallel across stories (`US1`/`US2`/`US3`/`US4`). Tasks within `Phase 4`–`Phase 6` can run sequentially per story but independently across stories (US1/US2/US3 parallel if staffed).
- **[Story]** = maps task to user story (`US1`, `US2`, `US3`, `US4`). Setup (`Phase 1`) and Foundational (`Phase 2`) have NO story label. RED PHASE (`Phase 3`) has story labels for test tasks. Implementation (`Phase 4`–`Phase 6`) has story labels (`US1`–`US4`). Verification (`Phase 10`) has `US4` label (verification is the story that confirms all others).
- **RED PHASE BLOCKING** (`Phase 3`): All `T016`–`T036` must be complete; all resulting tests must FAIL; property-based invariants (`FsCheck`) cover `HealthCategory` derivation (`T017`), growth multipliers (`T018`), chunk stride disjoint ranges (`T020`), harvest quality roll (`T025`), extra harvest probability (`T029`), sparse save (`T035`), `ReGen Valley` interaction (`T036`), security sanitization (`T021`), async-only (`T022`), no pixel assertions (`T027`), `SC-008` rollback (`T034`). Implementation (`Phase 4+`) BLOCKED until RED PHASE complete.
- **TDD Enforcement**: Each user story's tests (`Phase 3`) must fail before its implementation (`Phase 4`/`Phase 5`/`Phase 6`). Implementation tasks include file paths (`ModController.cs`, `ModConstants.cs`, `SoilDecayService.cs`, `VisualizationService.cs`, `CompostingBinService.cs`, `HostAtomicLockService.cs`, `ModDataService.cs`).
- **Property-Based Testing**: `FsCheck.dll` / `FsCheck.Xunit.dll` present in `LivingRoots.Tests/bin/`. Invariant tests (`Invariants/` folder pattern: `SoilHealthInvariantsTests.cs`, `CompostConservationInvariantsTests.cs`) used for property-based verification. Each property task (`T017`, `T018`, `T020`, `T025`, `T029`, `T035`, `T036`) uses `FsCheck` property patterns.
- **Verification Phase (`Phase 10`) ONLY attests success** (`T088`–`T100`). Per user instruction 3: ONLY these verification tasks attest to the success and completeness of the implementation. `SC-008` rollback (`manual GamePath` + `StillConsumesBin`) is the mandatory activation gate (`FR-005` 78; `Dependencies` 136-137). Agent claims are NEVER sufficient.
- **SC-008 Concurrent Prerequisite (`user B`)**: Design/implementation permitted concurrently (`plan.md` §Phase 1; `Clarifications` 2026-10-16); yield activation (`FR-002`/`FR-003`) BLOCKED until `SC-008` green. Documented in `T060`, `T082`, `T088`.
- **GamePath Spike (`A1`)**: `Crop.newDay` public-field scan requires `GamePath` build verification (`plan.md` §Phase 0; `Dependencies` 141). Documented in `T042`, `T062`, `T096`. Zero `Harmony` footprint (`AGENTS.md` anti-pattern; `spec.md` A1 86).
- **Chunk Mechanism (`FR-001` contingency)**: Design locked (`plan.md` 58; `subagent-response-q7.md` — zero chunk mechanism exists today; only linear `SoilDecayService.cs` 86-105). Implementation deferred to `Phase 8` (`T070`–`T073`) but design verified in `Phase 2` (`T014`).
- **Zero Assets / Zero i18n (`A4`)**: `FR-008` (`No pixel assertions`) enforced; `A4` (`No new Assets/*.png`; `No new Data/Crops`; `No new i18n`) enforced (`T097`). `VisualizationService` floating-text reused (`CompostApplicationService.cs` 178-191 pattern; `VisualizationService.cs` 247-285).
- **No Harmony (`A1`)**: Zero `Lib.Harmony` footprint (`AGENTS.md` anti-pattern). `Crop.newDay` direct public-field scan (`Option A` — `spec.md` A1 86; `Clarifications` Q1 27). `B` (`HoeDirt.GetFertilizerSpeedBoost()` `Harmony` patch) is NOT implemented.
- **Multiplayer (`Principle VI`)**: Host-authoritative (`IsMainPlayer` / `Context.IsMainPlayer`); `HostAtomicLockService`; first-wins; inventory intact on refusal; single-player = degenerate case (host with no clients). Documented in `T103`, `T068`.
- **Atomic Conventional Commit (`Process #6`)**: After each file edit, `git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"`. Enforced by `.specify/scripts/atomic-commit-hook.sh` (`.specify/extensions.yml` `after_analyze` — `optional: false`). No manual commits without hook.
- **No Premature Abstractions (`Principle V`)**: `Crop.harvest` hook deferred to tasks (`T045`) — not invented prematurely; `VisualizationService` floating-text reused; no new abstract base (`A4` 89).
- **Security-First (`Principle II`)**: `SanitizePathSegments` (`FormC`) enforced (`T021`, `T075`); `0-100` bounds (`ModConstants`); `CheckForOverflowUnderflow=true` (`AGENTS.md`); no raw exception messages to logs (`AGENTS.md` anti-patterns; `spec.md` `FR-007` 80).
- **Async-Only (`Principle III`)**: `DayStarted` non-blocking; chunk spread allows concurrent tick processing (`~2ms`/chunk); `.Result`/`.Wait()` forbidden (`AGENTS.md` anti-pattern); `ModEntry.Dispose()` uses lock + `_disposed` (`AGENTS.md` conventions); event handlers unregistered on disposal (`ModController.cs` rollback patterns 566-598; `T084`).
- **DDD Layered (`Principle I`)**: Contracts (`Domain/Interfaces/`) separated from implementations (`Services/`) and controllers (`Controllers/`). `ISoilHealthService` (`Domain/Interfaces/ISoilHealthService.cs`) + `SoilHealthService` (`Services/SoilHealthService.cs`). `ModController` (`Controllers/ModController.cs`) orchestrates events. `HealthCategory` (`Domain/Visualization/HealthCategory.cs`) derived from `SoilHealthState` (`Domain/Models/SoilHealthState.cs`).
- **Template / Scaffold (`Requirement 0`)**: All code generable by template/scaffold (LSP code actions: interface contracts, model DTOs, test stubs, `ModConstants` constants) must be written by scaffold BEFORE hand-writing. Tasks `T002` (`contracts/` verification), `T009` (`ModConstants.cs` constants from spec/data-model), `T010` (`HealthCategory.cs` scale verification from `ROADMAP.md`), `T012` (`HostAtomicLockService` interface verification), `T015` (`VisualizationService` floating-text reuse verification) enforce this — scaffold/template code verified before `T037` (hand-written `DayStarted` scan) / `T045` (hand-written `Crop.harvest` hook) / `T052` (hand-written extra harvest).
- **Verification Tasks (`Phase 10`) — ONLY attest success**: `T088`–`T100`. Per user instruction 3: ONLY these verification tasks attest to success. Agent never claims feature complete; only `Phase 10` tasks (with `SC-008` rollback verified GREEN, `SC-001`–`SC-005` observed, `SC-009` executed, `FR-009` mean-verified, `A1` spike verified, `Principle VI` multiplayer verified, `FR-008` no pixel assertions confirmed) can declare feature complete.

---

## Format Validation

- [ ] All tasks use checklist format (`- [ ]`)
- [ ] All tasks have sequential IDs (`T001`–`T100`)
- [ ] All tasks include file paths (e.g., `LivingRoots/Controllers/ModController.cs`, `LivingRoots.Tests/Invariants/SoilHealthCategoryInvariantsTests.cs`)
- [ ] User story phases (`US1`, `US2`, `US3`, `US4`) have `[USn]` story labels
- [ ] Setup (`Phase 1`) and Foundational (`Phase 2`) have NO story label
- [ ] RED PHASE (`Phase 3`) tests have `[USn]` story labels (`US1`–`US4`)
- [ ] Implementation phases (`Phase 4`–`Phase 6`) have `[USn]` story labels (`US1`, `US2`, `US3`)
- [ ] Verification phase (`Phase 7`, `Phase 10`) uses `[US4]` (verification story) and `[P]` where parallel
- [ ] `[P]` included ONLY for tasks that can run in parallel (different files / independent stories / property-based tests across stories)
- [ ] Task IDs sequential (`T001` → `T100`)
- [ ] Phase structure: Setup (`Phase 1`) → Foundational (`Phase 2`) → RED PHASE Tests (`Phase 3`) → US1 Implementation (`Phase 4`) → US2 Implementation (`Phase 5`) → US3 Implementation (`Phase 6`) → US4 Verification (`Phase 7`) → Chunk (`Phase 8`) → Polish (`Phase 9`) → Final Verification (`Phase 10`)
- [ ] Dependencies section shows phase order and user story independence
- [ ] Parallel examples show subagent waves (`Subagent A` through `Subagent H`) with shared context reuse (`US1` growth mechanism; `US2` harvest hook; `US3` extra harvest; `US4` verification; cross-cutting security/docs; RED PHASE property-based; final verification proof)
