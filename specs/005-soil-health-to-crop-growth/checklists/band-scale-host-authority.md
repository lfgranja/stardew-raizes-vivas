# Band Reconciliation, Scale Consistency & Host-Authoritative Multiplayer Requirements Quality Checklist: Soil Health → Crop Growth

**Feature**: `005-soil-health-to-crop-growth` — Soil Health → Crop Growth (growth, quality, extra-yield, band reconciliation, multiplayer authority)
**Checklist created**: 2026-10-16
**Spec under test**: [`spec.md`](../spec.md) (161 lines; branch `feat/005-soil-health-to-crop-growth`; Status: Draft — `needs-clarification`; 8 of 9 resolved, 1 deferred — Q5 / `SC-008`)
**Evidence sources**: `spec.md` §Clarifications (2026-10-15 Q1–Q4/Q6/Q7; 2026-10-16 Q1/Q2/Q3/Q4/Q6/Q7), §Requirements FR-001–FR-009, §Assumptions A1–A5, §Non-Goals (#7 / #10), §Open Questions (Q1–Q7), §Success Criteria SC-001–SC-008, §Dependencies / §Assumes 145; `ROADMAP.md` (lines 36–38 / 74); `spec-001`; `spec-002` (`:113`/`:176`); `spec-004` (`SC-008`); `LivingRoots/Domain/Visualization/` (`ColorDTO.cs`, `VisualizationConfiguration.cs`, `TooltipData.cs`, `HealthCategory.cs`); `ModConstants` reference targets.

---

## Purpose (what this checklist evaluates — and does not)

This checklist tests **REQUIREMENTS QUALITY** of band classification (`FR-004` / `ROADMAP.md` 36–38 / Q2), constant alignment (`ModConstants` / `ColorDTO` / tooltips / assertions), composition rules (`A2` / `FR-001` / Q3 / Q6), scale consistency (`HealthCategory` 0–30 / 31–70 / 71–100), visualization compatibility (`SC-006` / `A3` / `FR-004` / `A4` / `Non-Goals` #10), and multiplayer-authority documentation (`FR-006` / `A5` / Q1 / Q6 / `HostAtomicLockService`) — **NOT whether multiplayer sync code exists, whether `Crop.newDay` is hooked, whether `Harmony` is applied, or whether `ModConstants` numeric edits are completed**. It verifies that rules are unambiguous, consistent across all `spec.md` sections, evidence-backed, and free of hidden contradictions (e.g., `US1` lines 42–43 using `spec-001` half-open while `FR-004` / `A3` / Q2 lock `ROADMAP.md`) — pre-code, not feature-change.

**Out of scope**: sync-message design, Harmony syntax, `Crop.newDay` call patterns, `ModConstants` value assignments, `ColorDTO` edits, chunk-guard implementation, harvest-intercept hook. Those belong to `plan.md` / `tasks.md` after this checklist passes.

---

## 1. FR-004 locks scale to `ROADMAP.md`; reconciliation is pre-code, not feature change
- [x] `FR-004` 77 / `A3` 88 / `Clarifications` Q2 28 / `ROADMAP.md` 36–38 / `Non-Goals` #9 129: scale locked to 0-30 / 31-70 / 71-100; `spec-001` half-open reconciled as artifact — pre-code, not feature change.
- [x] Constant/color/tooltip/assertion surfaces (`ModConstants`, `ColorDTO`, `TooltipData`, `VisualizationConfiguration`, assertions) must reference ROADMAP tiers — no 34/67 leakage; `Anti-patterns` (no hardcoded) applies.

> `FR-004` 77 / `A3` 88; `Anti-patterns` (no hardcoded); `ModConstants` reference targets (`SoilGrowthPoorMultiplier` 1.15, `SoilGrowthHealthyMultiplier` 0.85, `SoilQualityHealthyBoost` 0.08, `SoilExtraHarvestHealthyBoost` 0.10 — `Clarifications` Q3 29 / Q4 30); `ColorDTO.cs`; `VisualizationConfiguration.cs` 23–29 (`PoorColor` / `ModerateColor` / `HealthyColor`); `TooltipData.cs`; `FR-008` 81 (`no pixel assertions`).

- [x] `ModConstants` names in `FR-001`–`FR-003` map to ROADMAP tiers (not to 34/67 splits); future plan must assign 1.15 / 0.85 / 0.08 / 0.10 with ROADMAP mapping — no embedded numbers in requirements.
- [x] `ColorDTO` / `VisualizationConfiguration` colors (`Poor` / `Moderate` / `Healthy`) must align to [0,30] / [31,70] / [71,100] — no fourth tier for `spec-001` split.
- [x] Tooltip texts (`TooltipData.cs`; `A4` 89 reuses `floating-text` / `restoration-text`) describe tiers at ROADMAP thresholds — not 34 / 67.
- [x] Test assertions (`FR-008` 81) must assert on logic / band / multiplier — never on color / pixel / blend / overlay; `SC-006` (113) relies on updated tier rendering, not new overlay design.
- [x] `Anti-pattern` (`AGENTS.md` / `spec.md` Assumptions): no hardcoded values — all new values live in `ModConstants`; checklist verifies naming consistency, not numeric edits.

---

## 2. Every constant / color / tooltip / assertion reference must use the ROADMAP scale
- [x] `ModConstants` names (`SoilGrowthPoorMultiplier` 1.15 / `SoilGrowthHealthyMultiplier` 0.85 / `SoilQualityHealthyBoost` 0.08 / `SoilExtraHarvestHealthyBoost` 0.10 — `Clarifications` Q3 29 / Q4 30) mapped to ROADMAP tiers; no embedded numbers in `FR-001`–`FR-003`.
- [x] `ColorDTO` / `VisualizationConfiguration` 23–29 (`Poor`/`Moderate`/`Healthy`) + `TooltipData` at [0,30]/[31,70]/[71,100]; `FR-008` 81 prevents false visual claims.
- [x] Tooltip texts (`TooltipData.cs`; `A4` 89 reuses `floating-text` / `restoration-text`) describe tiers at ROADMAP thresholds — not 34 / 67.
- [x] Test assertions (`FR-008` 81) must assert on logic / band / multiplier — never on color / pixel / blend / overlay; `SC-006` (113) relies on updated tier rendering, not new overlay design.

---

## 3. `HealthCategory` bands defined consistently — Poor / Moderate / Healthy, precise
- [x] `Poor`=[0,30] / `Moderate`=[31,70] / `Healthy`=[71,100] inclusive (integer; `FR-007` 80 clamped); `Clarifications` Q2 28 / Q3 29 / Q4 30 / `A3` 88 agree; `HealthCategory.cs` exactly 3 tiers.
- [x] `SC-003` 109 (Medium parity) + `SC-001` 107 / `SC-002` 108 verify at 30/31 and 70/71 borders; no overlap / no gap.

> `FR-001` 74; `A2` 87; `Clarifications` Q3 29; `Clarifications` Q6 32; `SC-001` 107 / `SC-002` 108 / `SC-003` 109; `FR-006` 79; `Dependencies` 139 (`spec-002` `:113`/`:176`); `Clarifications` Q7 34.

## 4. Growth multipliers align to bands and apply to base `phaseDays` — before vanilla modifiers

- [x] **Poor** = `1.15×` (`SoilGrowthPoorMultiplier`); **Moderate** = `1.00×` (vanilla parity, `SC-003`); **Healthy** = `0.85×` (`SoilGrowthHealthyMultiplier`) — `Clarifications` Q3 29.
- [x] Multipliers are **multiplicative on phase duration** (not fixed-day offsets) — applied to base `phaseDays` at `DayStarted` (`FR-001` 74; `Clarifications` Q6 32).
- [x] **Before vanilla modifiers** (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`) — never after, never fixed-offset (`FR-001` 74; `A2` 87 embedded in Q6 32).
- [x] `SC-001` 107 (Poor > Moderate), `SC-002` 108 (Healthy < Moderate), `SC-003` 109 (Medium = vanilla) — all require the three multipliers at ROADMAP boundaries.
- [x] Performance: linear scan ≤5 ms @ 40,000 tiles (`FR-001` 74; `Dependencies` 139); contingency chunked spread (~1000/chunk, ~2 ms/chunk, prime-modulus stride) — documented (`Clarifications` Q7 34), not claimed implemented; `ExecuteWithConcurrencyGuard` at `OnDayStarted` (`ModController` 1719–1778) required.
- [x] `FR-006` 79 requires host-only execution at `DayStarted`; `FR-007` 80 requires vanilla fallback if data missing / NaN / ±Inf / out-of-bounds.

---

## 5. Composition A2: multiplicative with vanilla — compound, do not replace
- [x] `A2` 87 explicit: `Speed-Gro`+Healthy > either alone; Medium = exactly vanilla; multiplicative chain (base×tier×vanilla) (`FR-001` 74 / Q6 32) — not override; brief risk #4 acknowledged.
- [x] `SC-003` 109 cross-check (`A2`+`A3`); `Clarifications` Q3 29 (values)+Q6 32 (ordering) agree; growth never overrides (only `FR-002` quality force-0 permitted).

## 6. Quality roll — additive +0.08 (Healthy) / force 0 (Poor); never replace vanilla `p = 0.2·L/10 + ...`

> `FR-002` 75; `Clarifications` Q4 30; `A2` consistency; `SC-004` 110; `FR-006` 79 (client-local from shared `SoilHealthService`); `Clarifications` Q1 22 / Q3 33 (floating-text feedback).

- [x] `FR-002` (75): Poor = force quality 0 (override permitted); Moderate = vanilla roll; Healthy = `SoilQualityHealthyBoost` = +0.08 **additive** to roll `p` — never replacement.
- [x] Vanilla formula (`FR-002` 75; `US2` 50): `p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` — base preserved; tier adds to `p`.
- [x] `Clarifications` Q4 (30): +0.08 additive (`SoilQualityHealthyBoost`); Poor force 0; both compositional — `A2` consistency maintained (growth multiplicative, quality additive).
- [x] `FR-006` 79: harvest-quality rolls are **client-local** from shared `SoilHealthService` — no new sync message; clients observe via `ISoilHealthService.OnTileHealthChanged`; harvest results through normal harvest sync.
- [x] `Clarifications` Q1 (22) confirms client-local from shared tile health (`ISoilHealthService.cs:15`, `ModController.cs:1719`, `HostAtomicLockService:2779`); Q3 (33) confirms floating-text when tier modifies outcome (reuse `floating-text` / `restoration-text` per `A4` 89 — no new `i18n` key needed if using existing generic keys).
- [x] `SC-004` (110): Poor → 0; Moderate → vanilla roll; Living → enhanced — matches `FR-002`.

---

## 7. Extra harvest — multiplicative on base +0.10 — per-tile, not global
- [x] `FR-003` 76: Healthy ≥71 per-tile above `Data/Crops.ExtraHarvestChance` (`SoilExtraHarvestHealthyBoost`=+0.10, Q4 30); at harvest — **per-tile**, never global (`US3` 58: `Data/Crops` global per seed must not provide).
- [x] `SC-005` 111; local (`FR-006` 79 / `A5` 90); floating-text (`Clarifications` Q3 33 / `A4` 89); +0.10 in `ModConstants` only.

## 8. `FR-006` — host-authoritative for persistent growth-phase; `FR-001` applied at `DayStarted` with `HostAtomicLockService`

> `FR-006` 79; `A5` 90; `Clarifications` Q6 32; `Clarifications` Q1 22; `ModController` (`HostAtomicLockService` 7–60; `TryApplyCompostWithLock` 2779–2853; `TryAcquireLock` 2835; `OnDayStarted` 1719–1778; `HoeDirt` filter 1483–1492); `FR-001` 74.

- [x] `FR-006` (79): growth-phase modifications (`FR-001` / `Crop.phaseDays` multipliers at `DayStarted`) execute **host only** (`IsMainPlayer` / `Context.IsMainPlayer`; `HostAtomicLockService`; `TryApplyCompostWithLock` pattern from spec 004).
- [x] Same `FR-006` (79): propagate to clients via **crop-state in location sync** (`GameLocation.terrainFeatures`) — consistent with SMAPI sync; **no new multiplayer message**.
- [x] `Clarifications` Q6 (32): guard with same host-authoritative lock (`HostAtomicLockService` 7–60 / `TryAcquireLock` 2835); apply base `phaseDays` multiplicatively at `DayStarted` before vanilla modifiers — not after, not at harvest/compost-time.
- [ ] `FR-001` 74 confirms `DayStarted` scan via public `Crop.newDay` fields (Option A, `A1` 86); `A1` 86 notes `GamePath` spike gate required before plan — ordering with vanilla growth must be verified; B (`HoeDirt.GetFertilizerSpeedBoost` Harmony) only if ordering fails; [CLARIFICATION — filter scope: FR-001 does not explicitly restate `HoeDirt` filter condition; reference `ModController` 1483–1492 / Q6 agent evidence needed]
- [x] `Clarifications` Q1 (22) confirms host-authoritative applies to persistent growth-phase (`FR-001`); harvest rolls (`FR-002` / `FR-003`) are not host-authoritative — separation is explicit.
- [x] No invented sync-message class, no `NetMessage`, no `Harmony.Patch` syntax — checklist verifies absence of design claims.

---

## 9. Harvest rolls (`FR-002` / `FR-003`) client-local from shared `SoilHealthService` — observe via `ISoilHealthService.OnTileHealthChanged`

> `FR-006` 79; `A5` 90; `Clarifications` Q1 22; `SC-004` 110 / `SC-005` 111; `FR-002` 75 / `FR-003` 76; `ISoilHealthService.cs:15` (Q1 evidence).

- [x] `FR-006` (79) separates harvest rolls (local, shared tile health) from growth-phase (host, crop-state sync) — both dimensions explicit.
- [x] `Clarifications` Q1 (22) answer (three-part): tile-level modifier shared across players; harvest is local action; host-authority applies only to persistent growth-phase (`FR-001`); **no new sync message needed**.
- [x] Observation mechanism for clients: `ISoilHealthService.OnTileHealthChanged` — existing event; harvest results observed through normal harvest action sync (SMAPI native).
- [x] `A5` (90): "No additional sync mechanism needed for quality/extra." — documented; checklist verifies statement present, not absence of code.
- [x] `SC-004` / `SC-005` verification relies on local observation — consistent with local-roll design.
- [x] No `FR-*` proposes new sync message; `Non-Goals` #7 (127) excludes transport change; checklist verifies absence, not missing feature.

---

## 10. Growth-phase propagates via crop-state in `GameLocation.terrainFeatures` — SMAPI-consistent
- [x] `FR-006` 79 + `A5` 90: modified `phaseDays` carries in `Crop.phaseDays` inside `GameLocation.terrainFeatures`; clients via normal sync (`SC-007` 113 save/reload).
- [x] `Clarifications` Q6 32 (`HostAtomicLockService` guard); `ModController` 1719/1483–1492; no new wrapper / sync field claimed — checklist verifies absence.

> `Assumes` 145 (line 145); `Clarifications` Q5 31; `A5` 90; `HostAtomicLockService` / `Principle VI`; `FR-005` 78 (`SC-008` prerequisite); `FR-006` 79.

## 11. First-wins per-tile host-authoritative (`ReGen Valley`) — never last-write-wins; inventory intact; second mutation re-evaluated

- [x] `Assumes` 145 (145): first-wins per-tile (`HostAtomicLockService` / Principle VI); second mutation re-evaluated against new state, refused with message if invalid; **inventory intact**; never last-write-wins — stated, not solved; documentation only; gate deferred to SC-008 (clear prerequisite, not ambiguity).
- [x] `Clarifications` Q5 (31): `ReGen Valley`: first-wins; second mutation re-evaluated / refused; both rollback (`SC-008`) and `ReGen Valley` answered; rollback requires `SC-008` (manual + automated `StillConsumesBin`) before any yield activates (`FR-005` 78; `Dependencies` 136); clear gate, unambiguous.
- [x] `FR-005` (78): `CompostingBinService.CollectCompost` rollback / full-inventory / duplication fix must pass `SC-008` before any yield feature activates — verification gate, explicit; not ambiguity.
- [x] `A5` (90): validation gate (`SC-008`) achievable with `GamePath`; manual play-through confirms growth + harvest feedback survive save/reload + multiplayer observation; gate defined, sound.
- [x] `FR-006` 79 authority mechanism (`IsMainPlayer` / lock) supports first-wins — second mutation evaluated after first updates `SoilHealthService` via `OnTileHealthChanged`; refusal keeps inventory intact (`FR-005b` / `FR-005c` from spec 004); mechanism documented and sound.
- [x] No new `ReGenValleySync` / `ConcurrentCropLock` message invented — only existing `HostAtomicLockService` pattern applies.

---

## 12. No new multiplayer transport / sync message required — documented, not missing
- [x] `Non-Goals` #7 127 (no transport; 2-player D1 owed from 004) + `A5` 90 (positive: none needed) + `FR-006` 79 + `Clarifications` Q1 22 / Q6 32 — all concur.
- [x] `FR-002`/`FR-003` local from shared health needs no sync; `Dependencies` 136 (`spec-004` required) + `SC-008` gate; checklist verifies concurrence, not omission.

## 13. Scale consistency across visualization (`spec-001` / overlay / tooltip / `ColorDTO`) — existing overlay renders updated tier; no new overlay needed

> `SC-006` 113; `A3` 88; `FR-004` 77; `A4` 89; `Non-Goals` #10 130; `FR-008` 81; `spec-001` delivered (`SC-006` baseline "Works").

- [x] `SC-006` (113): overlay/tooltip reflect updated health; existing overlay renders updated tier after compost; **no new overlay needed** — `spec-001` delivered.
- [x] `A3` (88) + `FR-004` (77): visualization references (`ColorDTO`, tooltip texts, overlay tiers) must use ROADMAP scale — included in scale reconciliation (section 1–2 / 14).
- [x] `A4` (89): zero new asset / `Data/Crops` / `Data/Objects` / `Data/Recipe` / `i18n` — overlay uses existing `floating-text` / `restoration-text` (`CompostApplicationService.cs` pattern).
- [x] `Non-Goals` #10 (130): "No redesign of visualization (overlay/tooltip/hoe) — only that it continues to reflect updated health." — confirms no new overlay design.
- [x] `FR-008` (81): assertions on logic / band / multiplier — never on color / pixel / blend / overlay appearance — prevents false consistency from visual checks.
- [x] `ColorDTO` mapping (3 colors → 3 ROADMAP tiers) verified by `VisualizationConfiguration.cs` 23–29 (`PoorColor` / `ModerateColor` / `HealthyColor`) — no fourth color for `spec-001` split.

---

## 14. All assertions / constants / tooltip texts / overlay colors updated to ROADMAP — not `spec-001` artifact
- [x] `FR-001`–`FR-003` 74–76 reference `ModConstants` (names); `Clarifications` Q2–Q7 28–34 ROADMAP; `Anti-patterns`; `Open Questions` 152–157 resolved/deferred; `requirements.md` 34 (16/16).
- [ ] `US1` 42–43 contradiction flagged (`spec-001` tiers vs `ROADMAP.md`); correction required in plan/task; checklist records, not suppresses; [CLARIFICATION — contradiction confirmed: update US1 to ROADMAP [0,30]/[31,70]/[71,100] in plan/task]

> `A2` 87; `A3` 88; `Clarifications` Q3 29; Q4 30; Q5 31; Q6 32; Q7 34; `FR-001` 74 / `FR-002` 75 / `FR-003` 76; `SC-003` 109 / `SC-004` 110 / `SC-005` 111; `US1` 42–43 inconsistency noted.

## 15. No contradiction between `A2` (composition) and `A3` (scale) — both verified per Clarifications (29 / 30 / 31 / 32 / 33 / 34)

- [x] `A2` (how: multiplicative-compositional) and `A3` (what: ROADMAP bands / values) are independent — no logical conflict; checklist verifies both present and unconflicting.
- [x] Sequential clarification answers cover all dimensions: Q3 (growth multipliers + A3), Q4 (quality/extra + A3), Q5 (rollback/`ReGen Valley` + authority), Q6 (application order + A2), Q7 (performance + contingency) — all consistent with `FR-001`–`FR-006`, `A1`–`A5`.
- [x] `SC-003` (109) is the cross-check: `A2` + `A3` → Medium = vanilla; `SC-001` 107 (Poor > Medium) + `SC-002` 108 (Healthy < Medium) — both hold with 1.00× at 31–70.
- [x] Growth never replaces vanilla (`FR-001` 74; `A2` 87); quality never replaces vanilla except allowed Poor force-0 (`FR-002` 75); extra never replaces base (`FR-003` 76) — all compositional.
- [ ] `US1` 42–43 uses `spec-001` tiers — inconsistency noted (section 14); correction required for executable verification; checklist does not claim `US1` is already correct; [CLARIFICATION — correct US1 to ROADMAP tiers before executable verification]
- [x] `FR-006` 79 separates growth-phase (A2+A3+host) from harvest (client-local) — authority division does not alter composition.

---

## End verification note (mandatory closing — all requirements from prompt met)

- [x] **5 open questions** (`spec.md` 150–157): Q1 RESOLVED / Q2 RESOLVED / Q3 RESOLVED / Q4 RESOLVED / **Q5 DEFERRED (`SC-008` verification gate)** / Q6 RESOLVED / Q7 RESOLVED — 7 resolved, 1 deferred, 0 unresolved.
- [x] Scale reconciliation (`FR-004` 77 / `A3` 88 / `Q2` 28 / `ROADMAP.md` 36–38 / `Non-Goals` 129) is **pre-code, not feature change** — no spec change required.
- [x] Multiplayer authority (`FR-006` 79 / `A5` 90 / `Q1` 22 / `Q6` 32 / `Assumes` 145 / `HostAtomicLockService`) is **documented rule**, not design change — existing pattern, no sync message, no Harmony.
- [ ] **No false consistency**: `US1` 42–43 uses `spec-001` tiers — contradiction with `FR-004`/`A3`/Q2 flagged; correction belongs in plan/task (update to [0,30]/[31,70]/[71,100]); [CLARIFICATION — decision needed: correct US1 now or defer to plan? Recommended: correct in plan before 005 activation]
- [x] **No invented mechanisms**: no new sync message, no `Harmony.Patch`, no `ModConstants` assignment, no `ColorDTO` mapping — checklist verifies absence of invention.
- [x] Evidence-backed (line refs to `spec.md`, `ROADMAP.md`, `LivingRoots/Domain/Visualization/`) + `requirements.md` 34 (16/16) — deferred `SC-008` is `spec-004` prerequisite, not ambiguity.
---

*Subagent output — 2026-10-16. Purpose: requirements-quality verification for spec 005 (`spec.md`, 161 lines). Not a plan, not tasks, not code. Tests whether scale reconciliation (`Q2` / `FR-004` / `A3` / `ROADMAP.md`), composition (`A2` / `FR-001` / `Q3` / `Q6`), authoritiy (`FR-006` / `A5` / `Q1` / `Q6`), visualization consistency (`SC-006` / `A3` / `FR-004` / `A4` / `Non-Goals` #10), and declaration of no-new-transport (`Non-Goals` #7 / `A5` / `Q1`) are clearly specified, consistent, and evidence-backed — not whether sync code, `Crop.newDay` hook, `ModConstants` edits, or `ColorDTO` updates exist. All recommendations are verification-of-consistency; all design claims are either already in `spec.md` or explicitly noted as absent (e.g., no new sync message, no Harmony patch). The `US1` verification contradiction (`spec-001` tiers in manual test description vs. `ROADMAP.md` scale in `FR-004`) is recorded, not hidden.*
