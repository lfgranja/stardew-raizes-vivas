# Feature Specification: Soil Health → Crop Growth

**Feature Branch**: `feat/005-soil-health-to-crop-growth` (recommended — derived from `soil-health-to-crop-growth` assessment slug; branch naming is independent of spec directory per `.specify/` conventions)

**Created**: 2026-10-15
**Status**: Draft — `needs-clarification` from `/speckit.assess.decide` unresolved (5 clarification questions carried; validation gate not yet passed)

**Input**: Handoff from `.specify/assessments/soil-health-to-crop-growth/` (intake.md + research.md + problem.md + concept.md + decision.md). The problem is defined: per-tile soil health (0–100) is measured, persisted, and displayed, but never affects crop growth, harvest quality, or extra harvest. All 18 unimplemented roadmap mechanics depend on this connection.

---

## Problem

The mod's soil-health system measures and displays health per tile (0–100%) but produces zero observable gameplay change when a tile's health improves or declines. Applying compost increases soil health by exactly 15 points (clamped to 100) — yet harvest quality, growth speed, and extra-yield rolls remain identical to untreated tiles. The overlay renders correctly; the mechanism behind it is absent.

This is not a visualization gap (spec 001) or a persistence gap (spec 002 / 004). It is the gap between measurement and decision. Without this feature, the 19 unimplemented roadmap progression items (mulch, rotation, polyculture, green manure, animal management, pest control, bio-construction, ICLF, greywater, CSA, collective kitchen, food sovereignty, and all intermediate items) have no mechanism to affect play — they modify a number the player can see but not act upon differently.

## Clarifications

### Session 2026-10-15

- Q: Which hook mechanism applies the soil-health growth modifier — Option A (public `Crop.newDay` via `DayStarted`, no Harmony) or Option B (`HoeDirt.GetFertilizerSpeedBoost()` Harmony patch)? → A: Option A — public `Crop.newDay` scan through existing `ModController.OnDayStarted` (§ModController.cs 1719–1778), no `Lib.Harmony` dependency, with `GamePath` spike gate to verify ordering with vanilla growth; B only if ordering fails (spec.md A1, concept.md 91–92; agent evidence: zero Harmony footprint, `LuckSkill` proves direct access; research: multiplicative composition aligns with agronomic standard). All `HealthCategory`/constant updates must align with the chosen hook's host-authoritative lock pattern (`HostAtomicLockService` / `TryApplyCompostWithLock` 2779–2853).
- Q: Which `HealthCategory` scale defines bands — `ROADMAP.md` 0-30/31-70/71-100 or `spec-001` half-open `[0,34)/[34,67)/[67,100]`? → A: `ROADMAP.md` 0-30/31-70/71-100 (spec.md A3 CONFIRMED; `spec-001` half-open reconciled as implementation artifact only; all `ModConstants`, `ColorDTO`, tooltip texts, and assertions must use the ROADMAP scale).
- Q: What exact multiplicative growth-speed adjustments per `HealthCategory` band, and how applied — multipliers on phase duration or fixed-day offsets, with what `ModConstants` values? → A: Multiplicative phase-duration multipliers (not fixed-day offsets): Poor 1.15× slower, Moderate 1.00× (vanilla parity, SC-003), Healthy 0.85× faster (`ModConstants`: `SoilGrowthPoorMultiplier` = 1.15, `SoilGrowthHealthyMultiplier` = 0.85). Compositional with vanilla modifiers (A2); applied at `DayStarted` via `Crop.newDay` public-field scan (Q1 Option A). Research (Lal 2020 meta-analysis; Cornell Soil Health Assessment; extension.umn.edu) supports nonlinear tiered growth response with multiplicative composition; no universal % exists, calibrated to observable parity.
- Q: What exact quality-roll and extra-harvest probability adjustments per tier, additive or multiplicative, with what `ModConstants` values? → A: Quality: Poor force 0; Moderate vanilla; Healthy +0.08 additive to vanilla roll `p` (`SoilQualityHealthyBoost` = 0.08, FR-002 / SC-004). Extra harvest: Healthy base + 0.10 (`SoilExtraHarvestHealthyBoost` = 0.10, FR-003 / SC-005). Both compositional (not replacement) with vanilla; calibrated for observable manual verification.
- Q: `CompostingBinService` rollback status and `ReGen Valley` rule — confirmation method and interaction rule? → A: Rollback: verify via `SC-008` (spec 004 manual play-through + save/reload) before activating any yield feature; do not proceed to plan until green (`FR-005`; `Dependencies` verification gate). `ReGen Valley`: first-wins host-authoritative per-tile (`HostAtomicLockService` / Principle VI); second mutation re-evaluated, refused with message if invalid; documented in Dependencies/Assumes only — no design change (`Clarifications`).

## User Scenarios & Testing (mandatory)

### User Story 1 — Soil health improves crop growth (P1)

As a player who has applied compost to a tilled tile, I see the crop on that tile grow faster than a neighboring untreated tile with the same crop type and watering conditions, so that the compost's effect is visible.

**Verification** (manual, required — see Success Criteria SC-001 / SC-008): Launch mod with `GamePath`; arar 3 tiles; plantar mesma semente; aplicar compost em 1 (ou 3× para subir de banda); comparar dias até colheita. Tier M (34–66) deve ser idêntico ao vanilla (paridade); tier P ([0,34)) deve ser mais lento; tier L ([67,100]) deve ser mais rápido. Nenhum dos 3 deve divergir da base vanilla na banda M.

---

### User Story 2 — Soil health affects harvest quality (P2)

As a player harvesting from a tile whose soil health is high, I receive higher-quality produce (silver / gold / iridium) more often than on a poor tile, so the soil state rewards sustained care.

**Verification**: Harvest from tier P / M / L tiles (mesma semente, mesma estação, mesma condição de água) — roll de qualidade deve refletir a banda de saúde, nunca substituir o roll vanilla (compor, não substituir). Baseline: vanilla roll (fórmula `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01`). O mod soma o efeito da banda (não substitui).

---

### User Story 3 — Extra harvest on living soil (P3)

As a player harvesting from a high-health tile, I occasionally receive an extra harvest item, so the investment in soil health yields tangible return.

**Verification**: Per-tile roll (não global) — o `Data/Crops` não pode fornecer isso por ser global por semente. A verificação deve ser manual: observar múltiplas colheitas de tiles L vs. M e confirmar que a taxa de extra é maior em L, sem duplicar itens.

---

### User Story 4 — Trust the pipeline (P3 — verification of verification)

As the maintainer, I can confirm that claims about soil-health effects are backed by evidence in the repo — not by a green test count alone — so that future decisions rest on verified signals.

**Verification**: `ROADMAP.md` status by item must match code (as of this spec's creation: 2 of 21 features; 4 of 13 core promises); `COMPLETION_SUMMARY.md` must be reconciled (26 claimed failures → 0 actual); `.specify/assessments/` artifacts must show evidence-backed status; `research/2026-10-roadmap-feasibility-and-agronomy.md` must be cited for agronomic claims (Three Sisters overstatement; insect hotels not supported).

---

## Requirements

### Functional Requirements

- **FR-001 (Growth)**: The soil-health value of a tilled tile (`HoeDirt`) must modify the crop's growth speed multiplicatively by `HealthCategory` at `DayStarted` (host-authoritative, per `SoilHealthService` and `SoilDecayService` patterns): Poor tier = `ModConstants.SoilGrowthPoorMultiplier` (slower); Moderate tier = 1.00× (vanilla parity, SC-003); Healthy tier = `ModConstants.SoilGrowthHealthyMultiplier` (faster). The adjustment must compose with vanilla modifiers (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`) without double-dipping; multipliers apply to base phase length, not to modifier-derived adjustments. No hardcoded numerical values — all constants live in `ModConstants`.
- **FR-002 (Quality)**: The harvest quality roll (`Crop.harvest`) must incorporate the tile's soil-health band additively (never overriding vanilla roll `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01`): Poor tier forces quality 0 (no stars, override permitted for this tier); Moderate tier = vanilla roll (no change); Healthy tier increases quality-upgrade probability by `ModConstants.SoilQualityHealthyBoost` (additive to roll). The roll is composed, not replaced (A2; FR-002). No hardcoded numerical values.
- **FR-003 (Extra harvest)**: A completed harvest on a Healthy-tier tile (≥71 per ROADMAP scale) must have an increased probability of producing an extra item above `Data/Crops.ExtraHarvestChance`, applied per-tile at harvest time (not globally per crop). The increase is multiplicative on base: `extraProb = base + ModConstants.SoilExtraHarvestHealthyBoost` (calibrated; observable within SC-005 manual verification). No hardcoded numerical values.
- **FR-004 (Band reconciliation)**: The `HealthCategory` scale is `ROADMAP.md` 0-30/31-70/71-100 (confirmed; `spec-001` half-open `[0,34)/[34,67)/[67,100]` reconciled as implementation artifact only). Constant values (`ModConstants`), overlay colors (`ColorDTO`), tooltip texts, and all test assertions must use the ROADMAP scale: Poor = [0,30], Moderate = [31,70], Healthy = [71,100].
- **FR-005 (Rollback prerequisite)**: `CompostingBinService.CollectCompost` must handle full-inventory collection correctly — no partial loss, no duplication, no silent consumption of a ready bin when inventory is full (existing `FR-005b` / `FR-005c` from spec 004 must hold; the hidden duplication bug — `outputCount > 1` with no rollback — must be fixed before any yield-increase feature activates).
- **FR-006 (Host-authoritative)**: All growth, quality, and extra-yield modifications must execute on the host only (`IsMainPlayer` / `Context.IsMainPlayer` pattern, consistent with `HostAtomicLockService` and `TryApplyCompostWithLock` from spec 004). Clients observe changes via `ISoilHealthService.OnTileHealthChanged` and the existing overlay-invalidation mechanism.
- **FR-007 (No silent failure)**: If soil-health data is missing, corrupted, or out of bounds (0–100), the growth mechanism must fall back to vanilla behavior (not to zero growth or arbitrary quality). The `SoilHealthService`'s tolerant reader and `HealthValue_ClampedToMinMax` (with NaN/±Inf guards) must apply.
- **FR-008 (No pixel assertions)**: Tests for this feature must assert on logic (growth multiplier, quality roll, band classification) — never on color, pixel coverage, blend alpha, or overlay appearance. The existing `OverlayMeasurementTests` (`T040`) are for visualization, not for growth.
- **FR-009 (Mean-verified)**: The feature's success must be verified by manual play-through (`SC-008` / T034 of spec 004 — the only automation gap that requires live observation) — applying compost to a High-health tile, confirming faster growth / better harvest / extra yield vs. untreated neighbors, and confirming the change survives save/reload.

### Assumptions

- **A1 (Hook path)**: CONFIRMED — Option A (`DayStarted` scan via public `Crop.newDay` fields, no Harmony required for minimal path). Spike must verify ordering with vanilla growth (`GamePath` build required) before `plan`; fallback to Option B (`HoeDilt.GetFertilizerSpeedBoost()`) only if ordering fails. Default (a) stands.
- **A2 (Composition)**: The multiplicative adjustment must compose with vanilla modifications rather than replace them (e.g., a tile with `Speed-Gro` + High soil health must grow faster than either alone; a Medium tile must stay exactly vanilla). The brief `soil-tier-outcomes-harvest-feedback.md` flags this as risk #4.
- **A3 (Scale)**: CONFIRMED — Adopt `ROADMAP.md` scale (0-30 / 31-70 / 71-100) per user's criteria; `spec-001` half-open reconciled as implementation artifact. All `HealthCategory`, `ModConstants`, and test assertions must align.
- **A4 (No asset)**: No new `Assets/*.png`, no new `Data/Crops` entry, no new `Data/Objects`, no new `Data/Recipe`, no new `Data/BigCraftables`, no new `i18n` key required for growth feedback (reuses existing `floating-text` / `restoration-text` patterns from `CompostApplicationService.cs`).
- **A5 (Validation)**: The feature requires one manual verification (`SC-008`) before any claim of completeness. No automated test substitutes for observing live crop-state change.

---

## Key Entities

- **Soil Health (0–100)**: Per-tile value, persisted via `ModDataService` / `SoilHealthService`, sparse save, clamped.
- **HealthCategory**: Band classification (Poor / Moderate / Healthy) — must be reconciled.
- **Crop / HoeDirt**: Vanilla game objects whose growth/quality are modified per tile.
- **Composting Bin / Compost / Soil Decay**: Existing systems (spec 004 / 002 / 001) that feed into this feature.

---

## Success Criteria

| # | Criterion | Measurable | Technology-agnostic | Baseline (today) | Target |
|---|---|---|---|---|---|
| SC-001 | Poor-tile growth is slower than vanilla Medium | Growth time to harvestable phase is longer on Poor vs. Medium, same crop/season/water | Yes — compares days, not API | 0 (no effect) | Poor > Medium |
| SC-002 | Living-tile growth is faster than vanilla Medium | Growth time shorter on Living vs. Medium | Yes | 0 | Living < Medium |
| SC-003 | Medium-tier parity with vanilla | No difference in growth time / quality roll / extra harvest vs. untreated vanilla tile | Yes — controls for all modifiers | 0 (all tiers = vanilla) | Medium = vanilla |
| SC-004 | Harvest quality reflects tier | Poor tier → quality 0; Medium → vanilla roll; Living → enhanced probability | Yes | 0 | Tier reflects in roll |
| SC-005 | Extra harvest on Living tier | Per-tile probability > base `ExtraHarvestChance` on Living tiles | Yes | 0 | Living > base |
| SC-006 | Overlay/tooltip reflect updated health | Existing overlay renders updated tier after compost; no new overlay needed | Yes (visual) | Works (spec 001) | Unchanged |
| SC-007 | Save/reload preserves growth effect | After save/reload, tile's health and crop's growth state are consistent | Yes | Not tested (no runs) | Consistent |
| SC-008 | Manual play-through confirms full loop | Player applies compost → sees crop response on treated vs. untreated tiles | Qualitative (only verification) | Unverified | Confirmed |

---

## Non-Goals

- No new NPC (Elena) — out of scope for this feature (Epic 1 / `welcome-kit.md` / `elena-npc.md`).
- No new crafting item / recipe / machine (mulch, heirloom seeds, Joja Fertilizer — all deferred to `future-roadmap/briefings/`).
- No new crop sheet / `Data/Crops` edit (no new seeds / products — this feature uses existing crops).
- No new `Data/Characters`, `Data/Buildings`, `Data/Quests` — no narrative or quest integration.
- No `ContentPatcher` — repo uses `AssetRequested` + `e.Edit` only.
- No `config.json` / GMCM page — persistence stays via existing `IModDataService` / `VisualiizationConfigurationService`.
- No new `i18n` keys for growth feedback (reuses `floating-text` / `restoration-text`).
- No multiplayer transport change (live 2-player D1 verification stays owed from spec 004).
- No teaching-material redesign (Three Sisters / insect hotels must remain with documented caveats from `research.md` — not hidden bonuses).
- No change to `HealthCategory` bands in this feature — the reconciliation (scale choice) is a pre-code decision, not a feature change.
- No redesign of visualization (overlay/tooltip/hoe) — only that it continues to reflect updated health.

---

## Dependencies & Assumptions

- **Requires**: `spec-004` completed (compost loop works — `SC-008` must pass first; `CollectCompost` rollback fixed — prerequisite from `problem.md` / `FR-005`). **Verification gate**: `SC-008` (manual play-through + save/reload of `CompostingBinService`) must be green before any growth/quality/extra-yield feature activates; do not proceed to plan/tasks until this is confirmed.
- **Requires**: `spec-001` completed (visualization layer exists — already delivered, just needs to reflect updated health — `InvalidateCache` pattern exists).
- **Requires**: `spec-002` completed (decay service exists — `SoilDecayService` — already delivered).
- **Requires**: Band reconciliation decided (pre-spec — `problem.md` §Open Questions; CONFIRMED `ROADMAP.md` scale per Q2).
- **Requires**: Spike of hook decided (pre-spec — `problem.md` #1; CONFIRMED Option A with `GamePath` gate per Q1).
- **Assumes**: The user's "zero new asset" criteria is satisfiable — yes, this spec proposes zero new asset.
- **Assumes**: `Crop.newDay` public fields do not require Harmony for the minimal path — verified by `LuckSkill` usage and `Crop.cs` decompilation; ordering with vanilla growth requires test.
- **Assumes**: The validation gate is achievable — requires `GamePath`, launch, manual observation; `problem.md` notes this is the dominant risk.
- **Assumes**: No multi-mod interaction rule needed for single-mod operation — but the `ReGen Valley` interaction rule must be documented in the spec (not designed, just stated): **first-wins per-tile host-authoritative** (`HostAtomicLockService` / Principle VI); second concurrent `Crop` mutation is re-evaluated against new state and, when no longer valid, refused with explicit message (inventory intact). Never last-write-wins.

---

## Open Questions (must resolve before plan / tasks; carried from assessment)

- [RESOLVED — Q1] **Hook path**: Option A (`Crop.newDay` public-field scan via `DayStarted`, no Harmony) confirmed with `GamePath` spike gate (`spec.md` A1 / `concept.md` 91); B is fallback only if ordering fails (`Clarifications` 2026-10-15).
- [RESOLVED — Q2] **Band scale**: `ROADMAP.md` 0-30/31-70/71-100 locked (`spec.md` A3 / FR-004); `spec-001` half-open reconciled as artifact only.
- [RESOLVED — Q3] **Growth multipliers**: Multiplicative phase-duration: Poor 1.15×, Moderate 1.00×, Healthy 0.85× (`ModConstants` `SoilGrowthPoorMultiplier` / `SoilGrowthHealthyMultiplier`; `FR-001`; `Clarifications`).
- [RESOLVED — Q4] **Quality / extra-yield values**: Quality Poor force 0 / Moderate vanilla / Healthy +0.08 additive (`SoilQualityHealthyBoost`); Extra Healthy +0.10 above base (`SoilExtraHarvestHealthyBoost`; `FR-002` / `FR-003`; `Clarifications`).
- [DEFERRED — verification required before plan] **Rollback / `ReGen Valley`**: `CollectCompost` duplication fix must be verified by `SC-008` (spec 004) passing before any yield feature activates (`FR-005`; `Dependencies` verification gate). `ReGen Valley` rule stated first-wins host-authoritative (`Assumes` / `Dependencies`), not solved — documentation only, no design change needed.

---

*Written to `specs/005-soil-health-to-crop-growth/spec.md`. Based on: `.specify/assessments/soil-health-to-crop-growth/` (intake/research/problem/concept/decision) + `ROADMAP.md` (core concept, line 7–11) + `specs/001-soil-health-visualization/` + `specs/002-soil-decay-compost/` + `specs/004-playable-compost-loop/` (FR-005/FR-009/FR-012/SC-008). No implementation details added (no `Crop.newDay` call syntax, no `Harmony.Patch`, no `ModConstants` change values — those belong to the plan/tasks after clarification).*
