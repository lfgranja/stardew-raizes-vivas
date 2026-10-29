# Implementation Plan: End-to-End In-Game Verification (Gate 3)

**Branch**: `009-end-to-end-game-verification` | **Date**: 2026-10-25 | **Spec**: `/specs/009-end-to-end-game-verification/spec.md`

**Input**: Feature specification from `/specs/009-end-to-end-game-verification/spec.md`

## Summary

This feature addresses the Gate 3 verification gap: the mod builds and passes tests but has never been confirmed to produce gameplay effects in a live SMAPI session (`In-game runs: 0`). The scope includes: (1) formally documenting the manual verification protocol (SC-009) with 3-signal triangulation; (2) creating a save comparator as the sole automation component; (3) correcting the growth modifier from inverted ±1 logic to multiplicative `phaseDays` approach, moving execution from `DayStarted` to `DayEnding`, adding host guard and idempotency, and expanding from `currentLocation`-only to all farm locations; and (4) updating `test.md` from `partial` to `verified` only after real SMAPI session with 3 signals. The `verified` status is scoped as "maintainer-verified with multi-signal evidence" (1 session + 3 signals, no replication required).

## Technical Context

**Language/Version**: C# 12 / .NET 6 (nullable enabled, `CheckForOverflowUnderflow=true`)
**Primary Dependencies**: SMAPI 4.5+ (`Pathoschild.Stardew.ModBuildConfig`), xUnit + Moq (test), Stardew Valley 1.6+ assemblies (proprietary, in `.gamepath`)
**Storage**: Stardew Valley save system only — `modData/soil_health_data_{saveId}.json` and `modData/composting_bins_{saveId}.json` via SMAPI `IModHelper.Data`
**Testing**: xUnit + Moq; one test file per subject (`{Subject}Tests.cs`); `ThreadSafeGameLoopEventsStub` for concurrent event simulation; manual `GamePath` play-through is the dominant verification path
**Target Platform**: Linux dev (Steam/Proton Stardew Valley), SMAPI runtime
**Project Type**: Single SMAPI mod project (`LivingRoots/` + `LivingRoots.Tests/`)
**Performance Goals**: Growth modifier must scan all farm-location tiles within game tick budget (no chunking needed for typical farm sizes < 40k tiles)
**Constraints**: Proprietary DLLs in `.git` (`.gamepath`); CI `Build and Test` dormant; no headless SMAPI game runner; `STARDEW_GAME_PATH` required for execution
**Scale/Scope**: 1 feature (Phase 2 hook correction + verification protocol); 3 of 4 automation components deferred (YAGNI); 19 roadmap features depend on this verification

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. DDD (NON-NEGOTIABLE) | ✅ PASS | Growth modifier correction stays in `ModController` (controller layer); domain logic in `SoilHealthService` (service layer). Save comparator is infrastructure, not domain. No new abstractions beyond spec scope. |
| II. Security-First | ✅ PASS | Save comparator reads existing `ModDataService`-sanitized files. No new input paths. Path sanitization already in `ModDataService.SanitizePathSegments()`. |
| III. Async-Only (NON-NEGOTIABLE) | ✅ PASS | Growth modifier is synchronous game-loop code (no I/O). Save comparator is read-only comparison of already-persisted JSON. No `.Result`/`.Wait()`. |
| IV. TDD | ✅ PASS | Red-phase tests exist (`T114_T126_RedPhaseTests.cs`). Must be rewritten to pass with corrected hook. New tests for growth modifier correction (multiplicative, DayEnding, host guard, idempotency, all locations) must be written before implementation. |
| V. Simplicity/YAGNI | ✅ PASS | Only save comparator in scope. Reflector, structured logger, scripted runner deferred until 2+ features reuse (Regra dos Três). No premature abstractions. |
| VI. Multiplayer (NON-NEGOTIABLE) | ✅ PASS | Host guard (`if (!Context.IsMainPlayer) return;`) at top of `OnDayEnding`. Farm state is host-authoritative. Single-player is degenerate case. Farmhands must not execute growth modifier. |

**Gate result: PASS** — no violations. All principles aligned.

## Project Structure

### Documentation (this feature)

```text
specs/009-end-to-end-game-verification/
├── plan.md              # This file
├── research.md          # Phase 0 output (consolidated from assessment artifacts)
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (verification protocol contract)
└── tasks.md             # Phase 2 output (NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
LivingRoots/
├── Controllers/
│   └── ModController.cs          # Growth modifier correction: DayStarted→DayEnding, ±1→multiplicative, host guard, idempotency, all farm locations
├── Domain/
│   ├── Interfaces/
│   │   └── ISoilHealthService.cs # Already exists; no changes
│   └── Models/
│       └── SoilHealthState.cs    # Already exists; no changes
├── Services/
│   ├── SoilHealthService.cs      # Already exists; no changes
│   └── ModDataService.cs         # Already exists; save comparator reads from this
└── ModEntry.cs                   # Inject ILocationProvider into ModController (if not already)

LivingRoots.Tests/
├── Invariants/
│   └── T114_T126_RedPhaseTests.cs # Rewrite for corrected hook behavior
└── (new test files as needed)

VERIFICATION_PROTOCOL.md           # Already exists; formalize SC-009 label mapping
specs/009-.../checklists/
└── gate3-verification-contract.md # NEW: formal SC-009 checklist (pending creation)
```

**Structure Decision**: Single SMAPI mod project. No new projects or packages. All changes within existing `LivingRoots/` and `LivingRoots.Tests/` directories. Verification protocol documents go in `specs/009-.../checklists/` and `VERIFICATION_PROTOCOL.md`.

## Complexity Tracking

> No constitution violations. No complexity justification needed.

---

## Phase 0: Research (Consolidated)

Research has been completed across multiple prior sessions (assessments, clarify rounds 1-6). Key findings consolidated here:

### R1: Growth Modifier Event — DayEnding vs DayStarted

- **Decision**: Move `ApplySoilGrowthModifier()` from `DayStarted` to `DayEnding`.
- **Rationale**: SMAPI `SCore.cs:1405` calls `EventManager.DayEnding.RaiseEmpty()` before `action()` (vanilla `newDayAfterFade`/`Crop.newDay`). `DayStarted` fires after save/load, post-growth. Using `DayStarted` means the modifier composes with (not replaces) vanilla growth, producing incorrect net effects.
- **Alternatives considered**: `DayStarted` (current, stale) — rejected because it fires after vanilla growth has already been applied, causing composition instead of replacement.
- **Codebase evidence**: `ModController.cs:1767` currently uses `OnDayStarted` handler. Refactoring to `OnDayEnding` (or adding DayEnding subscription) required.

### R2: Growth Modifier Math — Multiplicative vs ±1

- **Decision**: Multiplicative approach on cached base `phaseDays` (Pobre 1.15×, Moderado 1.00×, Saudável 0.85×).
- **Rationale**: The current ±1 is semantically inverted (poor +1 accelerates growth, healthy −1 delays it). The multiplicative approach is the vanilla-blessed path (Speed-Gro uses `phaseDays` multipliers). Red-phase tests (`GrowthModifierIntegrationTests`) already encode multiplicative expectations (base-phase-days cache, 1.15/0.85, idempotency).
- **Alternatives considered**: ±1 additive (current) — rejected because it's semantically inverted and indistinguishable from vanilla growth. Multiplicative on `phaseDays` — accepted because it's the vanilla pattern and produces observable differential phase advancement.
- **Implementation**: Cache base `phaseDays` per `Crop` instance; apply multiplier; restore base before reapplying (idempotent).

### R3: Host Guard & Idempotency

- **Decision**: Add `if (!Context.IsMainPlayer) return;` at top of `OnDayEnding` (where the growth modifier runs); add idempotency key (`season:dayOfMonth:year`, invariant culture) to prevent reprocessing on save/load. `OnDayStarted` subscription remains for composting bins and soil decay (no host guard change there beyond existing).
- **Rationale**: `DayStarted`/`DayEnding` fires on farmhands too. Without host guard, farmhands would execute the modifier, violating Constitution VI (host-authoritative). Without idempotency, save/load (which re-fires events) would double-apply the modifier.
- **Alternatives considered**: No guard (current) — rejected because it violates multiplayer-first principle. Host-only execution — accepted as the correct pattern.

### R4: Location Scope — All Farm Locations

- **Decision**: Refactor `ApplySoilGrowthModifier()` to accept `locationName` parameter; resolve via `ILocationProvider.GetLocationByName`; call within the `foreach (var locationName in farmLocationNames)` loop alongside `ProcessDayStart`.
- **Rationale**: Current code only scans `Game1.currentLocation`, so crops in `Farm`/`Greenhouse` (where the host is not standing) are never processed. The day-start loop already iterates over farm locations for composting bins and soil decay.
- **Alternatives considered**: Scan only current location (current) — rejected because it silently starves all non-occupied farm locations. Full location iteration — accepted as the correct pattern (already used by composting/decay).

### R5: SC-009 Formalization

- **Decision**: SC-009 is the per-criterion manual verification protocol (`VERIFICATION_PROTOCOL.md` §3). Formally defined in spec 009 Success Criteria. Distinct from scripted pipeline (save comparator).
- **Rationale**: SC-009 was referenced 70+ times but never formally defined. It is a project-specific label (not IEEE/ISO/SMAPI standard). Research confirms no external standard uses this label. Formalizing resolves the overloaded-label ambiguity (4 different meanings in 4 specs).
- **Alternatives considered**: Keep as informal shorthand — rejected because it creates implementation ambiguity. Formalize as success criterion — accepted because it provides clear acceptance criteria.

### R6: Verification Signal — 3-Signal Triangulation

- **Decision**: Retain 3-signal triangulation (observation + artifact + log) for `verified` status. No reduction or extension.
- **Rationale**: Research (BetterEvaluation triangulation, Huang/Meyer/Oriol 2026, ITEA/NIST) confirms that no single signal is sufficient. Removing any signal opens the false-positive gap. Adding a 4th signal is YAGNI until automation framework.
- **Alternatives considered**: 2-signal (save + log) — rejected because it can't confirm gameplay responded. 4-signal (add temporal/repeatability) — deferred until automation framework (2+ features reuse).

### R7: Automation Scope — Save Comparator Only

- **Decision**: Only save comparator in scope. Reflector, structured logger, scripted runner deferred until 2+ features reuse the loop (Regra dos Três).
- **Rationale**: YAGNI. The cost/benefit of full automation for a single feature is not justified. The manual protocol (SC-009) is the primary path. Save comparator is the sole automation component because it's the cheapest to implement and provides structural evidence.
- **Alternatives considered**: Full automation (COMPLETA) — rejected as premature. Manual-only — rejected because save comparator is low-cost and provides structural evidence. Save comparator only — accepted as the YAGNI-compliant middle ground.

### R8: "verified" Terminology

- **Decision**: "verified" = "maintainer-verified with multi-signal evidence" (1 session + 3 signals). "independently verified" = future goal (≥2 sessions + third-party review) for automation framework.
- **Rationale**: Honest scoping. Evidence multi-signal with documented protocol exceeds community mod practice. Requiring replication now would block Phase 3/4/5 without proportional gain.
- **Alternatives considered**: Require replication (IEEE/ACM/NIST standard) — rejected because it blocks progress without proportional gain for a small mod team.

---

## Phase 1: Design & Contracts

### Data Model

The feature does not introduce new domain entities. It corrects existing behavior and formalizes verification artifacts.

**Entities involved (existing, no schema changes):**

| Entity | Location | Role in this feature |
|--------|----------|---------------------|
| `SoilHealthState` | `Domain/SoilHealthState.cs` | Persistence DTO: `Dictionary<LocationName, Dictionary<TileKey, HealthValue>>`. Save comparator reads before/after snapshots. No schema changes. |
| `Crop` (vanilla) | Stardew Valley assembly | `currentPhase`, `dayOfCurrentPhase`, `phaseDays` fields. Growth modifier reads/writes these. No schema changes. |
| `CompostingBinData` | `Domain/CompostingBinData.cs` | Not directly involved in this feature (composting bin verification is in `test.md` scope, not spec 009 scope). |

**New artifacts (not domain entities):**

| Artifact | Location | Description |
|----------|----------|-------------|
| `gate3-verification-contract.md` | `specs/009-.../checklists/` | Formal SC-009 checklist with pre-state, per-criterion observation, 3-signal evidence, post-state comparison |
| Save comparator | `LivingRoots.Tests/` (or `scripts/`) | Before/after `SoilHealthState` JSON diff tool. Reads `modData/soil_health_data_{saveId}.json` files. |
| `research.md` | `specs/009-.../` | Phase 0 output (consolidated from assessment artifacts) |

### Interface Contracts

**Verification Protocol Contract** (`contracts/verification-protocol.md`):

The verification protocol defines the contract between the maintainer and the evidence system:

1. **Pre-state capture**: Save backup + mod versions + SMAPI log rotation + pre-state screenshot
2. **Execution**: Manual SMAPI session following SC-009 checklist (≤30 min)
3. **Evidence capture**: All 3 signals (observation + artifact + log) must be present
4. **Post-state comparison**: Pre/post screenshot + save diff + no unexpected side effects
5. **Claim discipline**: "verified by manual end-to-end session + multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]"

**Growth Modifier Contract** (internal, not external API):

The corrected `ApplySoilGrowthModifier` must satisfy:
- **Event**: Executes during `DayEnding` (before `newDayAfterFade`/`Crop.newDay`)
- **Scope**: All farm locations (not only `currentLocation`)
- **Guard**: `if (!Context.IsMainPlayer) return;` at entry
- **Idempotency**: Keyed by `season:dayOfMonth:year`; skip if already processed
- **Math**: Multiplicative on cached base `phaseDays` (Pobre 1.15×, Moderado 1.00×, Saudável 0.85×)
- **Idempotent reapplication**: Restore base before reapplying
- **NaN handling**: Tiles with NaN health (never sampled) are out of scope for `verified`

### Quickstart Validation Guide

See `quickstart.md` (to be generated) for runnable validation scenarios.

---

## Implementation Phases (High-Level)

### Phase A: Growth Modifier Correction (Code Changes)

1. **Inject `ILocationProvider` into `ModController`** (if not already) — `ModEntry.cs:127`
2. **Add `DayEnding` event subscription** — alongside or replacing `DayStarted` subscription in `RegisterEvents()`
3. **Move `ApplySoilGrowthModifier()` call** from post-loop in `OnDayStarted` to `OnDayEnding` handler
4. **Refactor signature**: `ApplySoilGrowthModifier(string locationName)` — resolve location via `ILocationProvider.GetLocationByName`
5. **Call within farm location loop**: inside `foreach (var locationName in farmLocationNames)` in `OnDayEnding`
6. **Add host guard**: `if (!Context.IsMainPlayer) return;` at top of `OnDayEnding`
7. **Add idempotency**: Track processed day key (`season:dayOfMonth:year`) per location; skip if already processed
8. **Replace ±1 with multiplicative**: Cache base `phaseDays` per `Crop` instance; apply tier multiplier; restore base before reapplying
9. **Update comments and logging**: Reflect new behavior

### Phase B: Verification Infrastructure

1. **Create `gate3-verification-contract.md`** in `specs/009-.../checklists/` — formal SC-009 checklist (template from research findings)
2. **Create save comparator** — C# test utility or script that diffs `modData/soil_health_data_{saveId}.json` before/after
3. **Update `VERIFICATION_PROTOCOL.md`** — add SC-009 label mapping (section 3 already has substance)
4. **Update `AGENTS.md` / `CONTRIBUTING.md`** — add Gate 3 verification section

### Phase C: Test Updates

1. **Rewrite `T114_T126_RedPhaseTests.cs`** — change `Assert.True(false, ...)` to actual assertions for corrected hook behavior
2. **Add new tests**: Growth modifier multiplicative math, DayEnding execution, host guard, idempotency, all-location scope
3. **Run full test suite** — verify no regressions

### Phase D: Verification Execution (Manual)

1. **Run SC-009 manual session** — `GamePath` play-through with 3 signals
2. **Update `test.md`** — from `partial` to `verified` with evidence
3. **Update `COMPLETION_SUMMARY.md`** and `ROADMAP.md` — Gate 3 status

---

## Re-evaluation of Constitution Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. DDD | ✅ PASS | No new domain entities. Growth modifier correction in controller layer (correct). Save comparator is test infrastructure. |
| II. Security | ✅ PASS | Save comparator reads existing sanitized files. No new input paths. |
| III. Async | ✅ PASS | All code is synchronous game-loop. No I/O in hot path. |
| IV. TDD | ✅ PASS | Red-phase tests exist; must be rewritten. New tests for correction required before implementation. |
| V. YAGNI | ✅ PASS | Only save comparator in scope. 3 components deferred. |
| VI. Multiplayer | ✅ PASS | Host guard + idempotency explicitly scoped. Farmhands excluded. |

**Post-design gate: PASS** — no new violations introduced by design.