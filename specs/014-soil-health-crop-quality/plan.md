# Implementation Plan: Soil Health → Crop Quality (Epic: Tier Outcomes)

**Branch**: `014-soil-health-crop-quality` | **Date**: 2026-11-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/014-soil-health-crop-quality/spec.md`

---

## Summary

Close the second half of the mod's central promise: Living Soil (71–100%) pays off at harvest (silver/gold/iridium quality + chance of extra harvest), Poor soil (0–30%) yields no stars, and Medium (31–70%) stays byte-identical to vanilla. Reuses existing persistence (`SoilHealthState`, farm-keyed, sparse, host-authoritative) and overlay (no new per-frame pass). Harvest-time interception of `Crop.harvest` applies a tier-based quality modifier (+0.08 additive on the vanilla quality probability roll for Healthy; force `Quality = 0` for Poor) and an extra-harvest probability boost (+0.10 additive on `Data/Crops.ExtraHarvestChance` for Healthy), plus transient floating-text feedback. Also migrates the band scale from 34/67 to 31/71 (Cornell CSHA) and fixes the `CollectCompost` partial-success duplication bug as a prerequisite.

Key deliverables (design-locked — zero `NEEDS CLARIFICATION` markers remain; execution deferred to tasks):
1. **Band scale migration (FR-013)**: `PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`, `HealthCategory`, `ColorInterpolationService`, `ModController` growth modifier, tests, Gate 3 thresholds, docs. All code/tests use 31/71 consistently; no 34/67 references remain (except migration history).
2. **Harvest quality (FR-001/FR-003)**: at `Crop.harvest`, force `Quality = 0` for Poor (only tier with override); Moderate = vanilla (byte-identical); Healthy adds `+0.08` to the vanilla quality probability roll `p`. Compose with (not replace) vanilla modifiers (Farming level, fertilizer num2 1/2/3, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`). Extra-harvest items remain normal quality (0) (vanilla behavior) — only the primary item receives the boosted roll.
3. **Extra harvest (FR-002)**: Healthy `[71,100]` → additive `+0.10` on `Data/Crops.ExtraHarvestChance`; Poor/Moderate → no change. Per-tile at harvest time; never globally per seed. Host-authoritative roll (`Context.IsMainPlayer`).
4. **Transient feedback (FR-006/FR-007/FR-008)**: event-driven via `TemporaryAnimatedSprite` + `Display.Rendered`; Poor → `marker-poor` + "Soil too poor for quality crops" + `cancel` SFX; Living → `sparkle-living` + "Living Soil" (only when enhanced quality) and/or "Extra harvest!" (when extra item produced), two sprites broadcast together when both hold; Moderate → no feedback (silence = vanilla). Non-chromatic (shape + text + SFX). Immune to 1000-tile degradation and 16.67 ms/frame budget. No persistent per-tile marker.
5. **CollectCompost fix (Q4)**: two-phase commit — dry-run capacity check + compensating rollback + idempotency flag (`OutputGranted`) — prerequisite, separate task, before any output increase.
6. **Host authority (FR-009)**: roll executes once on host; result persists via SV save; client never rolls; client re-syncs on next `DayStarted`. Divergence rule: host state wins.
7. **Multiplayer feedback**: visual/local only; does not sync; host rolls once.

**Deferred prerequisites (must resolve before yield activation — not before design/implementation):**
1. `SC-008` rollback verification (`spec-004` `CompostingBinService.CollectCompost` — hidden duplication bug at `outputCount > 1`; manual `GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) — **absolute blocker for yield activation** (`FR-002`/`FR-003`; `Dependencies` 136–137; `FR-005` 78; clarification 2026-11-01).
2. `GamePath` spike (Q2) — verify `Crop.harvest` / `Object.Quality` member names against the 1.6.3/1.6.14 binary before writing hooks.
3. Gate 3 in-game SMAPI session (Q10) — capture 3 signals (observation + save diff + SMAPI log) for the growth+quality loop, with a second player for multiplayer verification (D1).

---

## Technical Context

**Language/Version**: .NET 6 with C# latest language version.

**Primary Dependencies**: SMAPI (Stardew Modding API 4.x), `Pathoschild.Stardew.ModBuildConfig` 4.4.0, `Microsoft.Xna.Framework` (MonoGame), xUnit / Moq for unit/integration testing. No `Lib.Harmony` unless public-field manipulation proves insufficient (Q2: Harmony only as fallback for harvest-quality interception).

**Storage**: Stardew Valley save system through `IModDataService` (`soil_health_data_{saveId}`); sparse dictionary (`SoilHealthState` — only non-default values stored); `ModDataService` sanitizes all `LocationName` and `TileKey` (`FormC` normalization, `SanitizePathSegments`). No external database. Harvest outcome is **not persisted** — derived from soil health at harvest.

**Testing**: xUnit + Moq; one test file per subject (`{Subject}Tests.cs`); `ThreadSafeGameLoopEventsStub` for concurrent event simulation; manual `GamePath` play-through is the dominant verification path (`FR-009`/`SC-008`/`SC-010`) — automation gap acknowledged explicitly. TDD Red-Green-Refactor (FR-014); property-based tests where applicable.

**Target Platform**: Stardew Valley 1.6 mod via SMAPI (cross-platform desktop: Linux, macOS, Windows).

**Project Type**: SMAPI mod (desktop game plugin — DLL); purely internal — no public APIs, command schemas, endpoint contracts, or grammars exposed externally. Internal contracts (`ISoilHealthService`, `ICompostApplicationService`, `IPlayerInventory`, `IHostAtomicLockService`) documented in `contracts/`.

**Performance Goals**: No new per-frame render pass (FR-011, FR-008); transient feedback event-driven only; must not trigger the 1000-tile degradation threshold or exceed 16.67 ms/frame (SC-005). Existing overlay/tooltip render budget unchanged.

**Constraints**:
- Multiplayer first-class (Principle VI, NON-NEGOTIABLE): host-authoritative (`Context.IsMainPlayer`/`IsMasterGame`); farm-scoped; single-player = host with no clients; no second source of truth; atomic per-tile lock (`HostAtomicLockService`); first-wins semantics; live 2-player verification owed to roadmap D1.
- Async-only (Principle III): `.Result`/`.Wait()` forbidden; `volatile`/`Interlocked` permitted for atomic flags; `ModEntry.Dispose()` uses lock + `_disposed` pattern; event handlers unregistered on disposal.
- Security-first (Principle II): all input paths sanitized (`SanitizePathSegments`); Unicode normalized (`FormC`); bounds-checked (0–100); `CheckForOverflowUnderflow` enabled project-wide; no raw exception messages to logs.
- Zero new assets / `i18n` (A4/Q5): reuse existing `TemporaryAnimatedSprite` floating-text pattern; no new `Assets/*.png`; no new `i18n` keys for feedback text (literal text); refusal messages keep existing i18n keys (`message.harvest.inventory-full`). No `Data/Crops`/`Data/Objects` edits (FR-004) — all effects runtime.
- No new `ContentPatcher`; repo uses `AssetRequested` + `e.Edit` only; no `config.json`/GMCM page.
- Sparse save (`AGENTS.md` anti-pattern): only non-default health values stored.
- DDD layered architecture (Principle I): interfaces (`Domain/Interfaces/`) separated from implementations (`Services/`) and controllers (`Controllers/`); ubiquitous language (`SoilHealth`, `Compost`, `CropQuality`, `HealthCategory`, `LivingSoil`).
- No persistent per-tile tier marker (FR-011, Q9) — transient feedback only.
- Pest system (Q7) and "new interactions" (Q8) are out of scope — separate specs; quality hook reads pest state through an optional null-safe provider (no hard dependency).

**Scale/Scope**: Farm, Greenhouse, Ginger Island maps; up to 10,000 tilled tiles; multiple concurrent composting bins. Design covers only soil-health → crop-quality mechanism (plus band-scale migration and CollectCompost fix). Broader scope (pest system, "new interactions", numerical tuning beyond first-pass, mid/end-game systems) deferred.

**Unknowns / Deferred** (resolved / confirmed in planning phase):
- No `NEEDS CLARIFICATION` markers remain in `spec.md` (checklist `requirements.md`: 16/16 passing; 7 clarification questions resolved in clarify session 2026-11-01).
- Deferred prerequisites (must resolve before final yield activation — not before design/implementation):
  1. `SC-008` rollback verification (absolute blocker for yield activation; design/implementation concurrent, activation blocked until green).
  2. `GamePath` spike (verify `Crop.harvest`/`Object.Quality` member names against 1.6.3/1.6.14 binary).
  3. Gate 3 in-game SMAPI session (capture 3 signals for growth+quality loop, with second player for multiplayer verification D1).

---

## Constitution Check (Pre-Phase 0)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution version: `2.0.0` (ratified `2026-09-05`; last amended `2026-10-13` — added Principle VI).

| Principle | Status | Evidence / Notes |
|-----------|--------|------------------|
| I. Domain-Driven Design (NON-NEGOTIABLE) | PASS | Layered architecture preserved: `Domain/Interfaces/` (`ISoilHealthService`, `ICompostApplicationService`, `IPlayerInventory`, `IHostAtomicLockService`) → `Services/` (`SoilHealthService`, `CompostingBinService`, `VisualizationService`) → `Controllers/` (`ModController`). Ubiquitous language (`SoilHealth`, `Compost`, `CropQuality`, `HealthCategory`, `LivingSoil`) used throughout. New harvest-quality logic lives in Services/ (application) and Domain/ (models); no domain logic in Controllers. |
| II. Security-First Data Handling (NON-NEGOTIABLE) | PASS | `ModDataService.SanitizePathSegments()` sanitizes `LocationName`/`TileKey`; `FormC` normalization enforced; health values clamped 0–100 (`ModConstants.MinSoilHealth`/`MaxSoilHealth`); `NaN`/`±Inf` guarded (`HealthCategory.FromHealthValue` → `Unknown`); `CheckForOverflowUnderflow` enabled project-wide; exception details only at `Trace`. No new file paths introduced (reuses `soil_health_data_{saveId}`); harvest outcome is not persisted. |
| III. Async-Only Concurrency (NON-NEGOTIABLE) | PASS | `DayStarted` growth scan uses non-blocking event handler (`ModController.OnDayStarted`); `ExecuteWithConcurrencyGuard` (`Interlocked.CompareExchange`) protects re-entrancy; no `.Result`/`.Wait()` on async calls. `ModEntry.Dispose()` uses lock + `_disposed` pattern; event handlers unregistered on disposal. Harvest interception at `Crop.harvest` is synchronous game-thread callback (no I/O) — consistent with existing compost/mulch button-press paths. |
| IV. Test-Driven Development | PASS | Red-Green-Refactor enforced: TDD (FR-014); property-based tests where applicable; one test file per subject. `SC-002`/`SC-003`/`SC-004`/`SC-005` define measurable assertions. `SC-008` (Gate 3 in-game) is the manual verification obligation; automation gap acknowledged (not hidden). CollectCompost fix prerequisite pinned by `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`. |
| V. Simplicity and YAGNI | PASS | Zero new interfaces invented beyond optional `IPestStateProvider` (null-safe, deferred contract); harvest quality reuses `VisualizationService` floating-text pattern (no new abstract base); no new assets/i18n/Data entries; `Crop.harvest` hook deferred to tasks (not invented prematurely); numerical values are first-pass tunable constants (Q6). Direct `IMonitor` acceptable. No expression-bodied methods. |
| VI. Multiplayer As A First-Class Requirement (NON-NEGOTIABLE, v2.0.0) | CONDITIONAL PASS | Design rules implemented + documented: host-authoritative shared soil health synced host→client on `DayStarted`/compost application (FR-009/A8); harvest quality/extra rolls run client-locally on the triggering client from synced deterministic inputs (no harvest-path transport proxy); host takes the same local path (`Context.IsMainPlayer`); transient feedback broadcast is host-scoped; `HostAtomicLockService` per-tile guard for mutations. Live 2-player verification (D1) deferred to roadmap D1 (debt documented). Single-player is the degenerate case of multiplayer. **No MUST violations**; conditional pass justified. |

**Gate Result**: PASS with one documented conditional — Principle VI live 2-player verification deferred to roadmap D1; no MUST violations. Design work (Phase 1) completed; yield activation blocked by `SC-008` verification gate (external prerequisite, not a design ambiguity; does not block plan creation).

---

## Project Structure

### Documentation (this feature)

```text
specs/014-soil-health-crop-quality/
├── plan.md              # This file
├── research.md          # Phase 0 output (clarify session 2026-11-01; 7 questions resolved)
├── data-model.md        # Phase 1 output (entities, relationships, validation, state transitions)
├── quickstart.md        # Phase 1 output (runnable validation scenarios)
├── contracts/           # Phase 1 output (internal service contracts)
│   └── i-harvest-quality.md
└── tasks.md             # Phase 2 output ($speckit-tasks — NOT created by $speckit-plan)
```

### Source Code (repository root — `LivingRoots/`)

```text
LivingRoots/
├── Constants.cs                          # ModConstants: band edges (PoorModerateBandEdge=31f, ModerateHealthyBandEdge=71f, ModerateBandWidth=40f), SoilQualityHealthyBoost=0.08f, SoilExtraHarvestHealthyBoost=0.10f, TranslationKeyHarvestInventoryFull (existing), PestQualityPenalty (future)
├── ModEntry.cs                           # Composition root; wires IPlayerInventory, IHostAtomicLockService, ITranslationHelper; optional IPestStateProvider when pest system installed
├── Controllers/
│   └── ModController.cs                  # OnDayStarted (growth modifier, band 31/71); NO harvest hook yet — deferred to tasks; ExecuteWithConcurrencyGuard at OnDayStarted
├── Domain/
│   ├── Interfaces/
│   │   ├── ISoilHealthService.cs         # Per-tile health query/persistence (existing)
│   │   ├── ICompostApplicationService.cs  # Existing (spec-004; SC-008 prerequisite)
│   │   ├── IPlayerInventory.cs           # TryAddItem (existing)
│   │   ├── IHostAtomicLockService.cs     # Per-tile atomic lock (existing)
│   │   └── IPestStateProvider.cs         # Optional (future pest spec); null-safe read
│   ├── Models/
│   │   └── SoilHealthState.cs            # Persistence DTO (existing; unchanged)
│   └── Visualization/
│       └── HealthCategory.cs             # Poor [0,31), Moderate [31,71), Healthy [71,100], Unknown (NaN/Inf); migration 34/67→31/71
├── Services/
│   ├── SoilHealthService.cs              # GetSoilHealth (NaN sentinel for missing); sparse cache; host-authoritative (existing)
│   ├── CompostingBinService.cs           # CollectCompost — fix partial-success duplication (Q4 prerequisite: two-phase commit, dry-run capacity, rollback, OutputGranted idempotency flag)
│   ├── CompostApplicationService.cs      # Floating-text pattern reuse (+15) — template for harvest feedback
│   ├── MulchApplicationService.cs        # Floating-text pattern reuse — template
│   ├── Visualization/
│   │   ├── VisualizationService.cs       # Floating-text / TemporaryAnimatedSprite pattern (247-285); hoe feedback (256-321); broadcastSprites for MP
│   │   └── ColorInterpolationService.cs  # 31/71 migration
│   └── (new) HarvestQualityService?      # Decision in tasks: whether to extract a domain service or keep inline in ModController harvest hook
└── (no new assets, no new i18n keys for feedback text)

LivingRoots.Tests/
├── SoilHealthServiceTests.cs             # Existing; update band edges 31/71
├── HealthCategoryTests.cs                # Existing; update intervals 31/71; NaN→Unknown
├── ColorInterpolationServiceTests.cs      # Existing; update edges
├── CompostingBinServiceTests.cs          # Existing; CollectCompost rollback tests
├── ModControllerTests.cs                 # Existing; growth modifier 31/71
└── (new) HarvestQuality*Tests.cs         # TDD red-phase: PoorTier_HarvestQualityIsAlwaysZero, ModerateTier_QualityMatchesVanilla, LivingTier_EnhancedQuality_*, LivingTier_ExtraHarvest_*, LivingTier_Both_ShowsBothTexts, LivingTier_NormalQualityNoExtra_NoFeedback, HostAuthoritativeRoll_*, InventoryFull_ExtraDroppedAsDebris
```

**Structure Decision**: Follows existing DDD layered architecture. Harvest-quality logic is an application-level concern (intercepts `Crop.harvest`, reads `SoilHealthService`, mutates `Object.Quality`, adjusts extra-harvest probability, drives `VisualizationService` feedback). No new domain interface unless tasks justify (YAGNI — Principle V). Band-scale migration touches existing files in place. CollectCompost fix is a separate prerequisite task within this feature's scope. Floating-text feedback reuses `CompostApplicationService`/`MulchApplicationService` world-space sprite pattern (`TemporaryAnimatedSprite` + `alphaFade` + `motion`, ~1s) and `VisualizationService` screen-space pattern; multi-sprite broadcast via `Game1.multiplayer.broadcastSprites` (MP-safe) when both messages hold.

---

## Complexity Tracking

No constitution violations to track. The one conditional (Principle VI live 2-player verification deferred to roadmap D1) is a documented debt, not a violation: design keeps farm state host-authoritative and farm-scoped; visualization/feedback stays local-rendered and read-only; single-player is the degenerate case of multiplayer (host with no clients); no second source of truth is introduced.

---

## Phase 0: Outline & Research

**Status**: Complete. See `research.md` (Phase 0 output) and `CLARIFICATION_COMPLETION_REPORT.md`. All 7 clarification questions resolved (5 autonomous + 2 batch) in clarify session 2026-11-01. Evidence artifacts: `internal-c1.md`, `internal-c2.md`, `internal-c3.md`, `research-c1.md`, `research-c2.md` (in `/tmp/opencode/research-014/`).

**Resolved unknowns:**
- Inventory-full at harvest → debris fallback for extra item; main item vanilla-blocked (research-c1).
- Extra-harvest item quality → normal (0), vanilla behavior (research-c2, decompiled `Crop.harvest` 1.6.15).
- NaN/Infinity (Unknown) → no modification (vanilla), consistent with growth modifier skip (internal-c3).
- Feedback text localization → literal (existing floating-text pattern; no new i18n keys).
- Pest clamp → optional null-safe provider (deferred contract to pest spec).
- Feedback conditionality → show only on enhanced outcome (batch Q1 = A).
- Feedback co-occurrence → both messages as two sprites (batch Q2 = A).

**No NEEDS CLARIFICATION markers remain** in Technical Context.

---

## Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete (yes).

1. **Entities** → `data-model.md`: `SoilHealth` (existing, unchanged), `HealthCategory` (migration 34/67→31/71; `Unknown` for NaN/Inf), `HarvestOutcome` (new, transient: quality 0–3 for primary item + extra-harvest count; not persisted), `TierFeedback` (new, transient: `TemporaryAnimatedSprite` + floating text + SFX; event-driven; not persisted). Validation rules: health clamped 0–100; NaN/Inf → Unknown → no modification; finite out-of-range → boundary category. State transitions: harvest quality re-rolls per harvest; soil tier applies independently per harvest; regrowing crops re-roll.

2. **Interface contracts** → `contracts/i-harvest-quality.md`: internal contracts `ISoilHealthService` (existing, read at harvest), `IPlayerInventory` (existing, `TryAddItem`), `IHostAtomicLockService` (existing, per-tile lock), `IVisualizationService` (existing, floating-text reuse), optional `IPestStateProvider` (future). Documents the harvest-intercept contract (public-field preference / Harmony fallback), host-authority rule, inventory-full debris rule, feedback conditionality/co-occurrence, and the "no new i18n keys" constraint. No external/public API (purely internal mod).

3. **Quickstart validation guide** → `quickstart.md`: runnable scenarios proving US1–US4 and SC-001–SC-008 (Poor always 0 stars; Moderate byte-identical to vanilla; Living enhanced quality + extra harvest; transient feedback appears/fades ≤1s; immune to 1000-tile degradation; band 31/71 consistency; build/test/format clean; Gate 3 multi-signal capture). Includes prerequisites (`GamePath` spike, SC-008 green), setup commands, test/run commands, expected outcomes. No full implementation code.

**Post-Design Re-evaluation (Phase 1 Complete)**: all six principles PASS with the same documented conditional (Principle VI live 2-player verification deferred to roadmap D1). Design aligns with constitutional principles; Phase 9 convergence implements FR-009/A8 replicated deterministic roll with host-authoritative soil-health sync (no harvest-path proxy), keeping shared farm state host-authoritative/farm-scoped.

---

## Done When (Plan)

- [ ] Plan workflow executed and design artifacts generated (`plan.md`, `research.md`, `data-model.md`, `contracts/i-harvest-quality.md`, `quickstart.md`)
- [ ] Extension hooks dispatched or skipped per Mandatory Post-Execution Hooks
- [ ] Completion reported with branch, plan path, and generated artifacts

*Implementation details (tasks, RED-phase tests, hook wiring) belong in `tasks.md` (Phase 2 — `$speckit-tasks`), not in this plan.*
