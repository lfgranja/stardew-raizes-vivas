# Phase 0 Research: Soil Health → Crop Growth (Feature 005)

**Feature**: Soil Health to Crop Growth (feature 005 — `feat/005-soil-health-to-crop-growth`)
**Date**: 2026-10-16
**Status**: Clarification loop completed — 2 interactive questions integrated; 1 deferred prerequisite (`SC-008` rollback verification — concurrent per user selection B, not absolute blocker); harvest feedback (`Q3/A4`) resolved by agents (Option A — floating-text reuse, new `Crop.harvest` hook deferred to plan/tasks).

---

## Clarification Results (Integrated into Spec)

### Q1: Hook Mechanism — Confirmed Option A (Public `Crop.newDay` via `DayStarted`, no Harmony)
- **Decision**: Option A — `DayStarted` scan via public `Crop.newDay` fields (`phaseDays`), no `Lib.Harmony` dependency.
- **Evidence**: Zero Harmony footprint in repo (`AGENTS.md` anti-pattern); `LuckSkill` proves direct field access; `ModController.cs` 1719-1778 (`OnDayStarted`) handles event loop; `Crop.cs` decompilation confirms public fields.
- **Gate**: `GamePath` spike required before plan to verify ordering with vanilla growth (`A1` 86; `Dependencies` 141).
- **Lock pattern**: Must align with `HostAtomicLockService` / `TryApplyCompostWithLock` 2779-2853 (`FR-006` 79).

### Q2: HealthCategory Scale — Confirmed ROADMAP Scale (0-30 / 31-70 / 71-100)
- **Decision**: `ROADMAP.md` 0-30/31-70/71-100 (`FR-004` 77; `A3` 88).
- **Reconciliation**: `spec-001` half-open `[0,34)/[34,67)/[67,100]` reconciled as implementation artifact only; all `ModConstants`, `ColorDTO`, tooltip texts, assertions must use ROADMAP scale.

### Q3: Growth Multipliers — Confirmed Multiplicative Phase-Duration
- **Decision**: Multiplicative on base `phaseDays`: Poor 1.15× (`SoilGrowthPoorMultiplier`=1.15), Moderate 1.00× (vanilla parity, `SC-003`), Healthy 0.85× (`SoilGrowthHealthyMultiplier`=0.85). Applied before vanilla modifiers (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`).
- **Evidence**: `Lal 2020` meta-analysis (nonlinear tiered growth response); `Cornell Soil Health Assessment`; `extension.umn.edu` (multiplicative composition aligns with agronomic standard); `Clarifications` Q3 29.

### Q4: Quality / Extra-Yield Values — Confirmed Additive / Multiplicative on Base
- **Decision**: Quality — Poor force 0; Moderate vanilla roll; Healthy +0.08 additive (`SoilQualityHealthyBoost`=0.08, `FR-002` 75, `SC-004` 110). Extra harvest — Healthy ≥71 base +0.10 (`SoilExtraHarvestHealthyBoost`=0.10, `FR-003` 76, `SC-005` 111). Both compositional (not replacement) with vanilla.
- **Evidence**: Vanilla roll formula `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` (`FR-002` 75; `US2` 50); calibrated for observable manual verification (`Clarifications` Q4 30).

### Q5: Rollback Verification / `ReGen Valley` — User Selected Concurrent (B)
- **User selection**: Option B — concurrent prerequisite (`SC-008` verification can proceed in parallel with design/implementation; duplication risk accepted during verification window; `SC-008` must still pass before final yield activation).
- **Note**: This weakens `FR-005` verification gate relative to Option A recommendation; design work permitted concurrently but yield activation carries documented duplication risk (`CompostingBinService.cs` `outputCount > 1` partial-delivery loop) until verification completes.
- **ReGen Valley interaction**: Confirmed documentation-only (`Assumes` 145; `Dependencies` 139) — first-wins host-authoritative per tile (`HostAtomicLockService` / Principle VI); second mutation re-evaluated, refused with message (inventory intact); never last-write-wins.
- **Evidence**: `CompostingBinService.cs` 189-264 (`CollectCompost` loop; `outputCount` hardcoded 1 at 211; partial-delivery loop 229-243 masks duplication hazard); `ModController.cs` rollback patterns 566-598 (don't cover partial delivery); `SC-008` requires manual `GamePath` play-through + automated `StillConsumesBin` (`CompostingBinServiceTests.cs` 339); `DEFERRED_SC008_EXPLANATION.md`.

### Q6: Growth Application (`DayStarted` / `Crop.phaseDays`) — Confirmed Before Vanilla, Multiplicative
- **Decision**: Apply base `phaseDays` multiplicatively at `DayStarted` before vanilla modifiers (not after, not fixed-offset). Guard with `HostAtomicLockService` per-tile (`FR-006` 79; `A5` 90).
- **Evidence**: Zero `Crop.newDay` references exist in code — must be added in tasks (`Clarifications` Q6 32); `ModController` 1483-1492 `HoeDirt` filter; `HostAtomicLockService` interface (`Domain/Interfaces/IHostAtomicLockService.cs` 1-46); `TryApplyCompostWithLock` 2779-2853.

### Q7: Performance / Chunk Mechanism — Confirmed Design (A); Implementation Deferred
- **User selection**: Option A — confirm defined contingency (combined chunk phase, `~1000` tiles/chunk, `~2ms` budget, prime-modulus stride `61`, spread across ticks when linear exceeds `<5ms`, `ExecuteWithConcurrencyGuard` at `OnDayStarted`).
- **Evidence**: ONLY linear scan (`SoilDecayService.cs` 86-105: plain `foreach`); NO chunk mechanism; NO `ExecuteWithConcurrencyGuard` at `OnDayStarted` (exists at 2100 for `OnSaveLoaded`/`OnSaving` only); `MaxTilesPerSave=30000` (`Constants.cs` 14) is persistence cap, not chunk mechanism (`subagent-response-q7.md`).
- **Status**: Design locked (`FR-001` 74; `Dependencies` 139; `Clarifications` Q7 34; checklist `growth-performance.md` CHK-303/304/1401). Implementation deferred to `/speckit-plan` tasks.

### Harvest Feedback (Q3/A4) — Resolved by Agents (A)
- **Status**: Option A confirmed (`Clarifications` 2026-10-16 Q3/A4 33-34) — floating-text message shown when tier modifies harvest outcome (Poor → forced 0 stars; Healthy → enhanced quality / extra harvest). Reuse existing `floating-text` / `TemporaryAnimatedSprite` (`VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191); no new `Assets/*.png`, no new `i18n` key (`A4` 89; `Non-Goals` #6 126). Contradiction with `soil-tier-outcomes-harvest-feedback.md` (proposes new tier-specific `i18n` keys) resolved by reuse of existing generic keys.
- **New dependency**: `Crop.harvest` intercept hook does not exist (`ModController` has no harvest hook; `LivingRoots/*.cs` zero `Crop.harvest` references beyond `message.harvest.inventory-full` at `Constants.cs` 56). Deferred to `/speckit-plan` tasks (`QUESTION_2.md`; `/tmp/report_harvest_feedback.txt` confirms zero harvest hooks).
- **Evidence**: Engineering (`ShowItemQuality` / `UIInfoSuite2Alt` / `CropQuality` mods confirm floating/overlay quality indicators as QoL need); HCI literature (`Nakamura & Csikszentmihalyi` flow; `INFORM` framework `Sedig & Haworth`; `CHI 2024` juicy feedback; `Pinelle` usability heuristics); agronomic observation (`FAO` crop assessment guide; `UMN` forage quality; `Oklahoma State` hay evaluation; Cameroon cocoa quality study `s40066-015-0047-z`). No direct peer-reviewed paper on floating-text harvest verification — bridge analogical: agronomic structured visual observation aligns with game-design immediate action-linked feedback (`research-harvest-feedback.md`).

---

## Key Decisions & Tradeoffs (Recorded for Plan / Tasks)

| Dimension | Decision | Evidence / Constraint |
|-----------|----------|----------------------|
| Hook path (`A1`) | Option A (`DayStarted` public-field scan, no `Harmony`); fallback B (`HoeDirt.GetFertilizerSpeedBoost()` Harmony) only if ordering fails | Zero Harmony footprint (`AGENTS.md`); `GamePath` spike gate required (`Dependencies` 141); `ModController` 1719-1778 handles `OnDayStarted` |
| Scale (`A3` / `FR-004`) | `ROADMAP.md` 0-30 / 31-70 / 71-100; `spec-001` half-open reconciled as artifact | `ModConstants`, `ColorDTO`, tooltip texts, assertions must use ROADMAP (`FR-004` 77; checklist `band-scale-host-authority.md`) |
| Composition (`A2`) | Multiplicative with vanilla (not replacement) — base `phaseDays` scaled before vanilla modifiers (`FR-001` 74; `A2` 87) | `SC-003` (Medium = vanilla) is control; `SC-001` (Poor > Medium); `SC-002` (Healthy < Medium) |
| Growth multipliers (`Q3`) | Multiplicative phase-duration: Poor 1.15× (`SoilGrowthPoorMultiplier`), Moderate 1.00×, Healthy 0.85× (`SoilGrowthHealthyMultiplier`) | Calibrated for observable parity; `SC-001`/`SC-002`/`SC-003` verify; `Clarifications` Q3 29 |
| Quality roll (`FR-002`) | Additive (`+0.08` `SoilQualityHealthyBoost`) to vanilla roll `p`; Poor forces 0 (override permitted only for Poor tier) | `SC-004` (Poor → 0; Moderate → vanilla; Living → enhanced); `FR-002` 75; `A2` consistency |
| Extra harvest (`FR-003`) | Multiplicative on base (`base + 0.10` `SoilExtraHarvestHealthyBoost`); per-tile at harvest; never global per seed | `SC-005` (Living > base); `US3` 58 (per-tile observation; `Data/Crops` must not provide); `FR-003` 76 |
| Performance (`Q7` / `FR-001`) | Design A (combined chunk, `~1000`/tick, `~2ms` budget, stride `61`, spread across ticks, `ExecuteWithConcurrencyGuard` at `OnDayStarted`) — design locked; implementation deferred to plan | `SoilDecayService.cs` 86 linear; `ModController` 1719 (no guard); 2100 (guard definition); checklist `growth-performance.md` CHK-1401 |
| Rollback verification (`Q5` / user B) | Concurrent prerequisite (`SC-008` verification proceeds in parallel; duplication risk accepted during verification window) — NOT absolute blocker (user selected B) | `Dependencies` 136-137; `FR-005` 78; `DEFERRED_SC008_EXPLANATION.md` (duplication hazard at `outputCount > 1`); `SC-008` still required before final activation |
| `ReGen Valley` (`Q5` / `A5`) | Documentation-only (`Assumes` 145; `Dependencies` 139) — first-wins host-authoritative per tile (`HostAtomicLockService` / Principle VI); second mutation re-evaluated, refused with message (inventory intact); no new mechanism needed | `Clarifications` Q5 31-32; `Assumes` 145; checklist `band-scale-host-authority.md` 110-111 |
| Multiplayer (`FR-006` / `A5` / `Q1`) | Persistent growth-phase (host-only `DayStarted`, sync via `GameLocation.terrainFeatures`); discrete harvest rolls (client-local from shared `SoilHealthService`) — no new transport message | `FR-006` 79; `A5` 90; `Clarifications` Q1 22; `Clarifications` Q6 32 |
| Harvest feedback (`Q3` / `A4`) | Option A — message shown when tier modifies outcome; reuse `floating-text` / `TemporaryAnimatedSprite`; new `Crop.harvest` hook deferred to tasks; zero new assets / `i18n` keys | `Clarifications` Q3/A4 33-34; `A4` 89; `Non-Goals` #6 126; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191 |
| Security / Sanitization (`Principle II`) | All user-influenced paths sanitized (`SanitizePathSegments`); Unicode normalized (`FormC`); bounds-checked (`0-100`); `CheckForOverflowUnderflow` enabled | `AGENTS.md` anti-patterns; `ModDataService` security references; `FR-007` 80; `FR-009` 82 |

---

## Unknowns Resolved (Phase 0 → Phase 1)

No `NEEDS CLARIFICATION` markers remain in `spec.md` (checklist `requirements.md` line 16 confirms: 8 of 9 resolved; 1 deferred — `SC-008` prerequisite, not ambiguity). All clarification questions from assessment (`Q1`-`Q7`) have been answered and integrated. No speculative tech-stack questions remain. The deferred `SC-008` item is a verification prerequisite (`spec-004`), not a specification ambiguity — it does not block plan creation, only yield activation.

---

## Design Decisions (Locked in Plan)

1. **Hook mechanism (`A1`)**: `DayStarted` public-field scan (`Crop.newDay` fields); `GamePath` spike required before implementation (`A1` 86).
2. **Growth modifier (`FR-001`)**: Multiplicative on base `phaseDays` at `DayStarted`, before vanilla modifiers (`A2` 87); `HostAtomicLockService` per-tile guard (`FR-006` 79).
3. **Performance (`FR-001` / `Q7`)**: Linear scan default (`SoilDecayService.cs` 86); contingency `A` (combined chunk, `~1000`/tick, `~2ms`, stride `61`, spread across ticks, `ExecuteWithConcurrencyGuard`) activates only when linear exceeds `<5ms` (`FR-001` 74; `Dependencies` 139).
4. **Harvest quality (`FR-002`)**: Additive (`+0.08` `SoilQualityHealthyBoost`) to vanilla roll `p`; Poor forces 0; Moderate = vanilla (`SC-004`).
5. **Extra harvest (`FR-003`)**: Multiplicative on base (`base + 0.10` `SoilExtraHarvestHealthyBoost`); Healthy (`≥71`) only; per-tile (`SC-005`).
6. **Rollback (`FR-005` / user B)**: Concurrent prerequisite (`SC-008`) — verification proceeds in parallel with design; duplication risk documented; activation requires `SC-008` green.
7. **Visualization / Feedback (`A4` / `Q3`)**: Zero new assets / `i18n`; reuse `floating-text` / `TemporaryAnimatedSprite`; new `Crop.harvest` hook deferred to tasks.
8. **Multiplayer (`FR-006` / `Principle VI`)**: Host-authoritative persistent phase (`DayStarted`); local discrete rolls (`Crop.harvest`); no new transport message.
