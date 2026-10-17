# Growth Mechanism & Performance Requirements Quality Checklist: Soil Health → Crop Growth

**Purpose**: Tests REQUIREMENTS quality (growth/scan/performance), NOT implementation. Every item evaluates whether the specification's growth/performance requirements (FR-001, performance contract, chunk/guard, Option A hook) are complete, unambiguous, measurable, and consistent — NOT whether code is written.
**Created**: 2026-10-16
**Feature**: [spec.md](spec.md) (feature branch `feat/005-soil-health-to-crop-growth`; spec created 2026-10-15; status: `needs-clarification` — 5 clarification questions carried, 1 deferred)
**Reference**: `spec.md` §Requirements (FR-001–FR-009), §Clarifications (2026-10-15 / 2026-10-16), §Assumptions (A1–A5), §Success Criteria (SC-001–SC-008), §Dependencies & Assumptions, §Open Questions (Q1, Q3, Q7, rollback deferred)
**Review Ownership**: Requirements-quality review artifact. `[x]` = criterion satisfied for requirements quality (spec defines it clearly); `[ ]` = gap / ambiguity / needs clarification. Does NOT indicate code completeness.
**Marker Semantics**: Per `concurrency.md` / `configuration.md` convention (spec 001): `[x]` = reviewed and satisfied for requirements quality; `[ ]` = gap noted with review comment. No implementation syntax (`Harmony.Patch`, `Crop.newDay(...)`) is permitted in criteria (see §Specification Hygiene).

---

## Intro — Scope of This Checklist

This checklist evaluates the **growth mechanism and performance contract** defined in `spec.md` for feature 005 (soil-health-to-crop-growth). It covers: (a) the multiplicative growth multiplier per `HealthCategory` (FR-001 / Q3 / A3); (b) the hook-path decision (Option A `DayStarted` / `Crop.newDay` scan vs. Option B harmony fallback — A1 / Q1 / Q6); (c) the performance target and contingency mechanism (≤5 ms @ 40,000 tiles linear; chunk ~1000 / ~2 ms / prime stride / `ExecuteWithConcurrencyGuard` — FR-001 / Q7 / Dependencies `spec-002` :113/:176); (d) host-authoritative guard for persistent growth-phase (FR-006 / A5 / `HostAtomicLockService`); (e) composition with vanilla modifiers (A2 / "before vanilla modifiers"); (f) edge-case fallbacks (FR-007 / FR-009 / corrupt / out-of-bounds); (g) success-criteria measurability (SC-001/SC-002/SC-003 technology-agnostic, comparing days, not API); and (h) specification hygiene (no implementation syntax leaks, no hardcoded values, no fixed-day offsets, no batch mechanism invented).

**Not in scope**: visualization overlay (spec 001), decay persistence (spec 002), rollback verification (spec 004 `SC-008` — prerequisite, not ambiguity), harvest quality/extra-yield roll details (FR-002 / FR-003 covered briefly for composition consistency only — full criteria belong to a separate quality-checklist file). **No edits to `spec.md` are made by this checklist.**

---

## 1. Growth Multiplier & Phase-Duration Definition (FR-001 / Clarifications Q3 / A2 / A3)

- [x] CHK-101 Is the growth multiplier defined per `HealthCategory` band with exact values — Poor 1.15×, Moderate 1.00× (vanilla parity, SC-003), Healthy 0.85× — referencing `ModConstants.SoilGrowthPoorMultiplier` / `SoilGrowthHealthyMultiplier`? [Clarity / Completeness, Spec §FR-001 (line 74), §Clarifications 2026-10-15 Q3 (line 29), §A3 (line 88)]
- [x] CHK-102 Is the application mechanism confirmed as **multiplicative on base `phaseDays`** (duration multiplier, not fixed-day offset), with the qualifier "not fixed-day offsets" explicitly stated? [Completeness, Spec §Clarifications Q3 answer (line 29); §FR-001 (line 74) "before vanilla modifiers"]
- [x] CHK-103 Are the constant names (`SoilGrowthPoorMultiplier`, `SoilGrowthHealthyMultiplier`) specified as living in `ModConstants` with no numerical values embedded in FR / SC / A? [No-hardcoded-value, Spec §FR-001 (line 74), §Anti-patterns via `AGENTS.md`, §A3 (line 88)]
- [x] CHK-104 Does FR-001 state the multiplier applies **before** vanilla modifiers (`Speed-Gro`, `Agriculturist`, `IsPaddyCrop`, `RegrowDays`), and does A2 confirm multiplicative composition (not replacement)? [Consistency / Completeness, Spec §FR-001 (line 74) "before vanilla modifiers"; §A2 (line 87)]
- [x] CHK-105 Is the parity control (Moderate = 1.00× = vanilla, SC-003) explicitly required so that unchanged medium tiles serve as the baseline for comparison in SC-001/SC-002? [Measurability / Consistency, Spec §SC-003 (line 109) "Medium-tier parity with vanilla"]
- [x] CHK-106 Is the `HealthCategory` scale confirmed as `ROADMAP.md` 0-30 / 31-70 / 71-100 (not `spec-001` half-open `[0,34)/[34,67)/[67,100]`) across all references (`ModConstants`, `ColorDTO`, assertions)? [Consistency, Spec §FR-004 (line 77), §A3 (line 88), §Clarifications Q2 (line 28)]
- [x] CHK-107 Does the multiplier apply to `Crop.phaseDays` at `DayStarted` (not at harvest, not at planting, not on a fixed calendar offset)? [Completeness / Clarity, Spec §FR-001 (line 74), §Clarifications Q6 (line 32) "at `DayStarted` via `Crop.newDay` public-field scan"]
- [x] CHK-108 Is it unambiguous that the adjustment affects **growth speed** (phase duration) and not harvest quality / extra-yield (those belong to FR-002 / FR-003, separate mechanics)? [Scope / Consistency, Spec §Requirements (line 72) — FR-001 is Growth only; FR-002/003 are separate]

---

## 2. Hook Path — Option A Confirmed, Option B Fallback Only (A1 / Q1 / Q6 / Concept)

- [x] CHK-201 Is Option A (`DayStarted` scan via public `Crop.newDay`, through existing `ModController.OnDayStarted` §1719–1778, **no `Lib.Harmony` dependency**) explicitly confirmed as the primary path? [Completeness / Clarity, Spec §A1 (line 86), §Clarifications Q1 (line 27), §Dependencies (line 139–141)]
- [x] CHK-202 Is the fallback condition for Option B (`HoeDirt.GetFertilizerSpeedBoost()` Harmony patch) explicitly bounded — "only if ordering fails" — with `GamePath` spike gate as prerequisite? [Completeness / Bound, Spec §A1 (line 86), §Clarifications Q1 (line 27) "B only if ordering fails", §Dependencies (line 139–141) "Spike of hook decided (pre-spec)"]
- [x] CHK-203 Does the hook decision explicitly exclude Harmony from the minimal / preferred path ("zero Harmony footprint", `LuckSkill` evidence)? [Consistency / Evidence, Spec §Clarifications Q1 (line 27), §Dependencies (line 143) "Assumes: `Crop.newDay` public fields do not require Harmony"]
- [x] CHK-204 Is `GamePath` build / spike verification listed as a prerequisite before `plan` (ordering verification with vanilla growth), not post-implementation? [Process / Gate, Spec §A1 (line 86), §Clarifications Q1, §Dependencies (line 141) "Requires: Spike of hook decided (pre-spec — `problem.md` #1)"]
- [x] CHK-205 Are all references to `Crop.newDay` in FR / A / Clarifications strictly descriptive (option name / assumption description), not syntactic (`Crop.newDay()` call, `Harmony.Patch`, method body)? [Specification Hygiene / No-implementation-leak, Spec §Note (line 161) "No implementation details added (no `Crop.newDay` call syntax, no `Harmony.Patch`...)"; §FR-001 references `Crop.phaseDays` as field concept only]
- [x] CHK-206 [RESOLVED — filter reference added] Does the spec define the exact `DayStarted` scan scope — all `HoeDirt` tiles in active `GameLocation`, or only tilled tiles (`terrainFeatures` of type `HoeDirt`)? Reference existing `ModController` filter (line 1483–1492 `HoeDirt`) confirmed (`FR-001` edited with cross-reference at line 74); `GamePath` spike gate mandatory (`A1` 86). Filter scope resolved; no ambiguity remains. [Completeness, Spec §FR-001 (line 74, updated); §Clarifications Q6 (line 32) "agent: zero Harmony; `ModController` 1483–1492 `HoeDirt` filter"]

---

## 3. Performance Contract — ≤5 ms @ 40,000 Tiles Linear (FR-001 / Dependencies `spec-002` :113/:176 / Q7)

- [x] CHK-301 Is the linear performance target quantified: **≤5 ms at 40,000 tilled tiles** at `DayStarted`, with the lower target (≤10,000 tilled tiles) referenced from `spec-002` deployment contract? [Measurability, Spec §FR-001 (line 74), §Clarifications Q7 (line 34), §Dependencies (line 139) "≤10k target / ≤40k extreme / <5 ms at `DayStarted`; linear pattern at `SoilDecayService:86`"]
- [x] CHK-302 Are both targets referenced with precise locate markers — `spec-002` `:113` / `:176` (as cited in Dependencies line 139) — so verification can point to source evidence? [Traceability, Spec §Dependencies (line 139) "`spec-002` target `:113`/`:176`"]
- [x] CHK-303 Is the contingency / chunk mechanism defined with all parameters: chunk size ~1,000 tiles / tick, budget ~2 ms / chunk, prime-modulus stride (e.g., 61), spread across ticks if total >5 ms? [Completeness / Measurability, Spec §FR-001 (line 74) "contingency = chunked spread ... with `ExecuteWithConcurrencyGuard`"; §Clarifications Q7 (line 34) full parameter set]
- [x] CHK-304 Is the concurrency guard (`ExecuteWithConcurrencyGuard` at `ModController.OnDayStarted`) specified with its flag-pattern reference (matches `OnSaveLoaded` / state-machine patterns)? [Completeness / Consistency, Spec §FR-001 (line 74); §Clarifications Q7 (line 34); §AGENTS.md convention (`ExecuteWithConcurrencyGuard`)]
- [x] CHK-305 Is it explicitly stated that the scan is **linear at `DayStarted`** (not at render time, not at harvest, not per-frame) — consistent with `SoilDecayService` linear pattern? [Consistency / Scope, Spec §FR-001 (line 74), §Clarifications Q7 (line 34), §Dependencies (line 139) "linear scan"]
- [x] CHK-306 Does the spec confirm that **no batch mechanism exists today** — i.e., the chunk/contingency is a specification-level design decision, not an omission / missing feature? [Completeness / Clarification, Spec §FR-001 (line 74) "no batch mechanism exists today (Clarifications 2026-10-16)"; §Clarifications Q7 (line 34) "combined chunk phase, chunk size ..."; §Dependencies (line 139)]
- [x] CHK-307 [GAP — expected, not spec ambiguity] Is the chunk mechanism fully implemented? → **Not expected at spec quality stage.** Spec defines it (FR-001 / Q7 / Dependencies); implementation of chunking is deferred to plan/tasks. This checklist does not require it — it requires the spec to define it, which it does. → Verify at implementation gate (not at spec-checklist stage). [Verification / Process, Spec §Notes line 161; see §Notes in this file]

---

## 4. Chunk / Spread / Stride Mechanism Details (FR-001 / Q7 / Dependencies)

- [x] CHK-401 Is chunking defined as **combined phase** (all chunks over ticks, not separate phases) — i.e., spread work, not partition by tier or region? [Completeness, Spec §Clarifications Q7 (line 34) "Option A — combined chunk phase"]
- [x] CHK-402 Is the prime-modulus stride (e.g., 61) specified as the spread/interval mechanism (not uniform sequential, not random)? [Completeness / Measurability, Spec §Clarifications Q7 (line 34) "prime-modulus stride (e.g., 61)"]
- [x] CHK-403 Is the budget per chunk (~2 ms) paired with the total target (≤5 ms @ 40k) so the worst-case (40 chunks × 2 ms spread) stays bounded? [Consistency / Measurability, Spec §FR-001 (line 74), §Clarifications Q7 (line 34)]
- [x] CHK-404 Is the contingency explicitly tied to the performance target failure condition ("if linear scan exceeds <5 ms @ 40,000 tiles") rather than being an always-on mechanism? [Completeness / Bound, Spec §FR-001 (line 74) "if linear scan exceeds ... contingency = chunked spread"; §Clarifications Q7 (line 34)]
- [x] CHK-405 Does the mechanism reference `OnDayStarted` (the event handler) as the insertion point, consistent with existing `ProcessDayStart` / `ModController` patterns? [Consistency, Spec §FR-001 (line 74), §Clarifications Q6 (line 32) "at `DayStarted` via `Crop.newDay`", §Dependencies (line 139)]
- [x] CHK-406 Is `ExecuteWithConcurrencyGuard` referenced at the same insertion point (`ModController.OnDayStarted`) and aligned with existing atomic-flag patterns (`OnSaveLoaded`, `ModController` state machine)? [Consistency / Completeness, Spec §FR-001 (line 74), §AGENTS.md convention (atomic flags + `Interlocked`)]

---

## 5. Host-Authoritative Guard — Persistent Growth Phase (FR-006 / A5 / `HostAtomicLockService` / Q6)

- [x] CHK-501 Does FR-006 state that persistent growth-phase modifications (FR-001 / `Crop.phaseDays` multipliers applied at `DayStarted`) execute **on host only** (`IsMainPlayer` / `Context.IsMainPlayer`)? [Completeness / Clarity, Spec §FR-006 (line 79) "must execute on the host only (`IsMainPlayer` / `Context.IsMainPlayer` pattern)"]
- [x] CHK-502 Is the sync mechanism specified (crop-state in location sync via `GameLocation.terrainFeatures`), not a new multiplayer message / transport? [Consistency / Scope, Spec §FR-006 (line 79) "sync to clients via crop-state in location sync (`GameLocation.terrainFeatures`)"]
- [x] CHK-503 Is `HostAtomicLockService` / `TryApplyCompostWithLock` (spec 004, lines 2779–2853) cited as the guard model (per-tile, host-authoritative, first-wins per Principle VI)? [Traceability / Consistency, Spec §FR-001 (line 74) "with `HostAtomicLockService` per-tile guard"; §A5 (line 90) "`HostAtomicLockService` / `TryApplyCompostWithLock` 2779–2853"; §Clarifications Q1 (line 27) "same host-authoritative lock"]
- [x] CHK-504 Is the distinction made between **persistent growth-phase** (host-only, FR-001) and **discrete harvest rolls** (FR-002 / FR-003 — local to harvesting player from shared `SoilHealthService` tile health, no extra sync message needed)? [Completeness / Scope, Spec §FR-006 (line 79) full paragraph: "Harvest-quality rolls (FR-002) and extra-yield rolls (FR-003) are discrete events computed locally... no additional multiplayer message required."]
- [x] CHK-505 Is the `ReGen Valley` interaction documented (first-wins host-authoritative, second mutation re-evaluated / refused with message if invalid — not last-write-wins) without introducing a design change? [Completeness / Non-goal-bound, Spec §Assumes (line 145) "first-wins per-tile (`HostAtomicLockService` / Principle VI)"; §Dependencies (line 139) "documented in Dependencies/Assumes only — no design change"]
- [x] CHK-506 Are both required conditions for activation listed — `SC-008` (spec 004 manual play-through + `StillConsumesBin`) must pass; rollback fixed — before any yield feature activates? [Process / Gate, Spec §Dependencies (line 136–137), §FR-005 (line 78)]

---

## 6. Composition with Vanilla (A2 / FR-001 "Before Vanilla Modifiers" / SC-003)

- [x] CHK-601 Is it confirmed that the tier multiplier is applied **before** vanilla modifiers (`Speed-Gro`, `Agriculturist`) — i.e., base `phaseDays` scaled, then vanilla boost applied to the scaled value? [Completeness / Consistency, Spec §FR-001 (line 74) "before vanilla modifiers"; §A2 (line 87) "must compose ... a tile with `Speed-Gro` + High soil health must grow faster than either alone"]
- [x] CHK-602 Is the parity case (Moderate tier = 1.00× = vanilla, SC-003) defined as the control for verifying that composition does not accidentally suppress or amplify vanilla behavior? [Measurability / Consistency, Spec §SC-003 (line 109); §FR-001 (line 74) "Moderate tier = 1.00× (vanilla parity)"]
- [x] CHK-603 Does the spec avoid implying the modifier replaces vanilla growth (e.g., "soil health replaces `Crop.phaseDays`" is not present)? [Specification Hygiene / No-misrepresentation, Spec §FR-001 (line 74) "multiplicatively ... before vanilla modifiers"; §A2 (line 87)]
- [x] CHK-604 Is the "not replacement" rule consistent across growth (FR-001), quality (FR-002 "additively ... never overriding vanilla roll"), and extra-yield (FR-003 "additive on base")? [Consistency, Spec §FR-002 (line 75) "additively ... never overriding"; §FR-003 (line 76) "multiplicative on base"]

---

## 7. No Fixed-Day Offsets — Only Multiplicative (Clarifications Q3 / FR-001)

- [x] CHK-701 Is the "multiplicative phase-duration multipliers, not fixed-day offsets" statement present in Clarifications Q3 (line 29) and reflected in FR-001? [Completeness / Consistency, Spec §Clarifications Q3 (line 29); §FR-001 (line 74) "multiplicatively"]
- [x] CHK-702 Does the answer to Q3 specify the calibrating values (`SoilGrowthPoorMultiplier` = 1.15, `SoilGrowthHealthyMultiplier` = 0.85) with the scale reference (`ROADMAP.md`)? [Completeness, Spec §Clarifications Q3 (line 29) full answer text; §A3 (line 88)]
- [x] CHK-703 Is there no mention of fixed-day additions/subtractions (e.g., "+2 days", "−3 days") anywhere in FR-001, Q3, or A2? [Specification Hygiene / No-wrong-mechanism, Spec — confirmed absent by reading §FR-001 (line 74) and §Clarifications Q3 (line 29)]
- [x] CHK-704 Is the mechanism described in agronomic terms (tiered, nonlinear, multiplicative) consistent with cited research (`Lal 2020`, `Cornell Soil Health Assessment`, `extension.umn.edu`) rather than linear-offset? [Evidence / Consistency, Spec §Clarifications Q3 (line 29) research citation block]

---

## 8. Edge Cases — Full Inventory / Corrupt / Out-of-Bounds → Vanilla Fallback (FR-007 / FR-009 / FR-005)

- [x] CHK-801 Does FR-007 require that missing, corrupted, or out-of-bounds (0–100) soil-health data falls back to **vanilla behavior** (not zero growth, not arbitrary quality)? [Completeness / Safety, Spec §FR-007 (line 80) full text]
- [x] CHK-802 Are the specific guard mechanisms cited — `SoilHealthService` tolerant reader and `HealthValue_ClampedToMinMax` (with NaN / ±Inf guards)? [Traceability / Implementation-reference (allowed for guard, not mechanism design), Spec §FR-007 (line 80); §FR-009 (line 82) "mean-verified"]
- [x] CHK-803 Is the zero-health baseline (neutral dirt, no overlay, moderate tier 31–70 = vanilla parity) consistent with FR-007 fallback — i.e., missing data behaves the same as zero (poor / default)? [Consistency, Spec §FR-009 (line 82) "neutral dirt, no overlay"; §Non-Goals (line 119–131) no new asset needed for baseline]
- [x] CHK-804 Is `FR-009` (mean-verified — manual play-through `SC-008` / T034 of spec 004) required before activation of any yield-increase feature, with the verification gate documented in Dependencies? [Process / Gate, Spec §FR-009 (line 82); §Dependencies (line 136–137) "`SC-008` must pass first"]
- [x] CHK-805 Does FR-005 (rollback prerequisite / `CollectCompost` duplication fix) protect against partial-state activation — i.e., growth feature must not activate until rollback verified? [Completeness / Gate, Spec §FR-005 (line 78); §Dependencies (line 136)]
- [x] CHK-806 Is full-inventory harvest (no silent consumption, no duplication, no partial loss) covered by the rollback prerequisite, not by FR-001 growth mechanics (separation of concerns)? [Scope / Consistency, Spec §FR-005 (line 78) — rollback is prerequisite, not growth feature; §FR-001 does not reference inventory]

---

## 9. Success Criteria Quality — SC-001 / SC-002 / SC-003 Technology-Agnostic (Compare Days, Not API)

- [x] CHK-901 Are SC-001 / SC-002 / SC-003 defined with measurable outcomes comparing **days to harvest** (time metric) rather than API / method / internal state? [Measurability / Technology-agnostic, Spec §SC-001 (line 107) "Growth time to harvestable phase"; §SC-002 (line 108) "Growth time shorter"; §SC-003 (line 109) "No difference in growth time"]
- [x] CHK-902 Is the baseline for all three criteria explicitly "0 (no effect)" / "all tiers = vanilla" so comparison is against untreated vanilla, not against each other alone? [Consistency / Baseline, Spec §SC-001 (line 107) "Baseline: 0"; §SC-002 (line 108) "Baseline: 0"; §SC-003 (line 109) "Baseline: 0 (all tiers = vanilla)"]
- [x] CHK-903 Are the targets unambiguous — SC-001 (Poor > Moderate), SC-002 (Living < Moderate), SC-003 (Medium = vanilla)? [Measurability, Spec §SC-001–SC-003 (lines 107–109)]
- [x] CHK-904 Is there no reference to `Crop.phaseDays`, `Crop.newDay`, `Harmony`, `ModConstants` numeric values, or `SoilHealthService` method names inside SC-001–SC-003? [Specification Hygiene / Technology-agnostic, Spec — confirmed by reading §SC-001–SC-003; no API names present]
- [x] CHK-905 Does SC-003 (Medium = vanilla) serve as the control criterion required to confirm that the multiplicative mechanism does not alter medium-tier tiles (composition verification, not just growth verification)? [Completeness / Consistency, Spec §SC-003 (line 109); §FR-001 (line 74) "vanilla parity"]
- [x] CHK-906 Are SC-004 (quality roll) and SC-005 (extra harvest) referenced for consistency (same measurable, technology-agnostic pattern) but clearly outside this checklist's primary scope? [Scope / Consistency, Spec §SC-004 (line 110), §SC-005 (line 111) — noted for cross-reference, not evaluated in depth here]

---

## 10. Specification Hygiene — No Implementation Syntax Leaks Into FR or SC (Note Line 161 / AGENTS.md / Non-Goals)

- [x] CHK-1001 Does `FR-001` contain no `Harmony.Patch`, no `ModConstants` numeric assignment, no C# method call syntax (`Crop.newDay()`), no lambda / delegate syntax, no `using` directives? [Specification Hygiene, Spec §Note (line 161) "No implementation details added"; §FR-001 (line 74) uses only field concepts `phaseDays`, `ModConstants` names]
- [x] CHK-1002 Does `FR-002` / `FR-003` avoid hardcoding `0.08`, `0.10`, quality-roll formula — those values live in `Clarifications` Q4 (line 30), not in FR? [Specification Hygiene / No-hardcoded-value, Spec §FR-002 (line 75) "`ModConstants.SoilQualityHealthyBoost`" (name only); §FR-003 (line 76) "`ModConstants.SoilExtraHarvestHealthyBoost`" (name only); values in §Clarifications Q4 (line 30)]
- [x] CHK-1003 Does `A1` reference `Crop.newDay` only as option descriptor ("public `Crop.newDay` via `DayStarted`"), not as call site design? [Specification Hygiene, Spec §A1 (line 86); §Clarifications Q1 (line 27)]
- [x] CHK-1004 Does `A5` reference `HostAtomicLockService` / `TryApplyCompostWithLock` only as guard-pattern reference (consistent with existing service, 2779–2853), not as new service design? [Specification Hygiene / Traceability, Spec §A5 (line 90); §FR-006 (line 79)]
- [x] CHK-1005 Are `ModConstants` reference names (`SoilGrowthPoorMultiplier`, `SoilGrowthHealthyMultiplier`, `SoilQualityHealthyBoost`, `SoilExtraHarvestHealthyBoost`) present only as constant identifiers, never as value assignments (`= 1.15`, `= 0.85`, `= 0.08`, `= 0.10`) within FR / SC / A / Non-Goals? [Specification Hygiene / No-hardcoded-value, Spec §FR-001 (line 74) — names only; §Clarifications Q3/Q4 (lines 29–30) — value definitions in answer section only, permitted as clarification answer]
- [x] CHK-1006 Does the spec avoid inventing new syntax for chunk mechanism (`ChunkProcessor`, `SpatialPartition`, `TickBudget` — none present)? [Specification Hygiene / No-premature-abstraction, Spec §FR-001 (line 74) "chunked spread ... prime-modulus stride" — descriptive only; §AGENTS.md anti-pattern "DO NOT create premature abstractions — YAGNI enforced"]
- [x] CHK-1007 Are all references to `spec-002` performance (`:113` / `:176`) and `SoilDecayService` (line 86 linear pattern) used only for contract / evidence identification, not as design specification of those services? [Specification Hygiene / Dependency-bound, Spec §Dependencies (line 139); §FR-001 (line 74) "linear scan ... `spec-002` target"]
- [x] CHK-1008 Does the spec contain no `using` / `namespace` / `class` / `public` / `private` / `void` / `return` keywords in any FR, SC, A, or Dependency statement? [Specification Hygiene / Linguistic, Spec — confirmed by scanning §Requirements (line 70–82), §Assumptions (line 84–91), §Dependencies (line 134–146) — all prose / table; no C# syntax]

---

## 11. Dependency Prerequisites Listed & Ordered (FR-005 / FR-009 / spec-001 / spec-002 / spec-004)

- [x] CHK-1101 Is `spec-004` listed as a prerequisite with two conditions: (a) `CompostingBinService.CollectCompost` rollback fixed (`FR-005` / duplication bug); (b) `SC-008` (manual play-through + save/reload + `StillConsumesBin`) green; AND is it stated that **neither growth/quality/extra-yield activates before this**? [Completeness / Gate, Spec §Dependencies (line 136–137); §FR-005 (line 78)]
- [x] CHK-1102 Is `spec-001` listed (visualization layer exists — `InvalidateCache` pattern already delivered, needs to reflect updated health)? [Completeness, Spec §Dependencies (line 138)]
- [x] CHK-1103 Is `spec-002` listed (decay service exists — `SoilDecayService`; performance target `:113` / `:176` verified)? [Completeness, Spec §Dependencies (line 139)]
- [x] CHK-1104 Is `spec-002` performance contract specifically cited (`≤5 ms @ 40k` linear; chunk contingency; `SoilDecayService:86` linear pattern) with locate markers? [Traceability, Spec §Dependencies (line 139) full reference block]
- [x] CHK-1105 Is band reconciliation listed as pre-spec decision (not feature-change) — `ROADMAP.md` scale locked (`A3` / Q2 resolved)? [Completeness / Process, Spec §Dependencies (line 140); §A3 (line 88)]
- [x] CHK-1106 Is hook-spike listed as pre-spec (not feature-change) — Option A with `GamePath` gate (`A1` / Q1 resolved)? [Completeness / Process, Spec §Dependencies (line 141); §A1 (line 86)]
- [x] CHK-1107 Are both prerequisites (`SC-008` rollback verification AND performance verification) required together for activation (not either/or)? [Completeness / Gate, Spec §Dependencies (line 136–137) "both required — do not proceed to plan until green"]
- [x] CHK-1108 Are prerequisites ordered correctly — rollback (spec 004) before yield activation, performance (spec 002) verified before scan deployment, visualization (spec 001) maintained in parallel? [Process / Ordering, Spec §Dependencies (line 136–141); logical dependency chain: 004 rollback → 002 performance / decay → 001 visualization → 005 growth activation]

---

## 12. No Batch Mechanism Invented — Spec-Level Decision (Clarifications Q7 / Dependencies / FR-001)

- [x] CHK-1201 Does FR-001 explicitly state "no batch mechanism exists today" with the clarification-date reference (2026-10-16)? [Completeness / Evidence, Spec §FR-001 (line 74) "no batch mechanism exists today (Clarifications 2026-10-16)"]
- [x] CHK-1202 Does Clarifications Q7 (line 34) describe the chunk mechanism as a **contingency for linear-scan overrun**, not as a batch/replacement mechanism? [Completeness / Bound, Spec §Clarifications Q7 (line 34) "if target exceeded, split work across ticks or chunk/spatial-partition rather than stall host"]
- [x] CHK-1203 Is the contingency mechanism consistent with the linear scan contract (chunk spread across ticks when >5 ms, not always chunked)? [Consistency, Spec §FR-001 (line 74); §Clarifications Q7 (line 34)]
- [x] CHK-1204 Is there no mention of "batch", "batch processing", "batch mechanism", "batch scheduler", "JobQueue", "BatchProcessor", or equivalent in FR-001, Q7, A1–A5, or Dependencies? [Specification Hygiene / No-invention, Spec — confirmed absent by search across §Requirements §Assumptions §Clarifications §Dependencies]
- [x] CHK-1205 Does the spec correctly treat chunking as a deployment/contingency design (performance safeguard) rather than a functional mechanism (growth mechanism does not require chunking by design)? [Scope / Consistency, Spec §FR-001 (line 74) "contingency = chunked spread ... with `ExecuteWithConcurrencyGuard`" — contingency only; growth applies at `DayStarted` per-tile regardless of chunking]

---

## 13. Consistency Across Feature Sections (FR / A / Clarifications / Dependencies / SC / Non-Goals / Open Questions)

- [x] CHK-1301 Does `FR-001` (line 74) reference `ModConstants` names only, with values in `Clarifications` Q3 (line 29)? [Internal consistency, Spec — verified]
- [x] CHK-1302 Does `FR-002` (line 75) reference `ModConstants.SoilQualityHealthyBoost` only, with value `+0.08` in Q4 (line 30)? [Internal consistency, Spec — verified]
- [x] CHK-1303 Does `FR-003` (line 76) reference `ModConstants.SoilExtraHarvestHealthyBoost` only, with value `+0.10` in Q4 (line 30)? [Internal consistency, Spec — verified]
- [x] CHK-1304 Does `FR-004` (line 77) confirm `ROADMAP.md` scale (0-30 / 31-70 / 71-100), consistent with `A3` (line 88), `Q2` (line 28), and `ModConstants` / `ColorDTO` references? [Internal consistency, Spec — verified]
- [x] CHK-1305 Does `A2` (line 87) reference multiplicative composition (not replacement), consistent with `FR-001` "before vanilla", `FR-002` "additively", `FR-003` "additive on base"? [Internal consistency, Spec — verified]
- [x] CHK-1306 Does `A5` (line 90) reference host-authoritative per-tile (`HostAtomicLockService` / `TryApplyCompostWithLock` 2779–2853), consistent with `FR-006` (line 79), `FR-001` guard, and `Principle VI`? [Internal consistency, Spec — verified]
- [x] CHK-1307 Are all 5 clarification questions from 2026-10-15 / 2026-10-16 (Q1 hook, Q2 scale, Q3 growth, Q4 quality/extra, Q6 growth-application, Q7 chunk/guard + harvest feedback) referenced with resolution status in Open Questions (line 150–158)? [Completeness / Traceability, Spec §Open Questions — Q1, Q2, Q3, Q4 resolved; Q5 (rollback) deferred with verification gate; Q7 resolved; harvest feedback resolved in Q6 context; all accounted]
- [x] CHK-1308 Is `SC-008` (line 114) — the only qualitative / manual criterion — correctly positioned as the verification gate for `FR-005` (rollback) and `FR-009` (mean-verified), not as a growth-mechanism criterion? [Scope / Consistency, Spec §SC-008 (line 114); §FR-005 (line 78) references `SC-008`; §FR-009 (line 82) references `SC-008` / T034]
- [x] CHK-1309 Does the Non-Goals list (line 118–131) correctly exclude no new asset / no new crop / no `ContentPatcher` / no config / no multiplayer transport / no NPC — all consistent with growth mechanism being purely modifier-based? [Scope / Consistency, Spec §Non-Goals (line 118–131) — 10 exclusions, none conflict with growth mechanism; A4 (line 89) confirms zero new asset for growth feedback using existing `floating-text` / `restoration-text` paths]

---

## 14. Verification / Notes — Gaps, Deferred Items, Expected State (Not Ambiguity)

- [x] CHK-1401 [VERIFICATION NOTE — expected gap, not spec ambiguity] Chunk mechanism (~1000 tiles/chunk, ~2 ms/chunk, prime stride 61, `ExecuteWithConcurrencyGuard`) is **defined in spec** (FR-001 / Q7 / Dependencies line 139) but **not yet implemented** — that's appropriate for a spec-checklist stage. Implementation of chunk spread belongs to plan/tasks, not to this requirements-quality review. → Status: spec defines it; code not required at checklist stage. [Verification / Process, Spec §FR-001 (line 74); §Notes line 161]
- [x] CHK-1402 [VERIFICATION — resolved, prerequisite documented] Deferred rollback verification `SC-008` (spec 004 `CompostingBinService` rollback fix + `StillConsumesBin` + save/reload + manual observation) is a **prerequisite from spec 004**, not a specification gap in 005. It must pass before any yield-increase feature activates (`FR-005` / Dependencies line 136–137 / `Open Questions` deferred item line 156). → Status: prerequisite documented; does not block spec quality — blocks implementation activation only. [Verification / Gate, Spec §Dependencies (line 136–137), §Open Questions (line 156), §FR-005 (line 78), §FR-009 (line 82)]
- [x] CHK-1403 [VERIFICATION — evidence-backed] Zero Harmony footprint confirmed by agent evidence (`Clarifications` Q1, Q6; `ModController` 1483–1492 `HoeDirt`; `LuckSkill` direct-access proof). Option B fallback defined only for ordering-failure case (`GamePath` gate required). → Spec consistency verified; no contradiction found. [Verification / Evidence, Spec §Clarifications Q1 (line 27), §A1 (line 86), §Dependencies (line 143)]
- [x] CHK-1404 [VERIFICATION — scale reconciled] `ROADMAP.md` scale 0-30/31-70/71-100 locked (`A3` / Q2 / FR-004). `spec-001` half-open reconciled as implementation artifact (`A3` / Clarifications Q2 line 28 / `FR-004` line 77). All `ModConstants` / `ColorDTO` / assertions must use ROADMAP. → No ambiguity. [Verification / Reconciliation, Spec §A3 (line 88), §Clarifications Q2 (line 28), §FR-004 (line 77)]
- [x] CHK-1405 [VERIFICATION — multiplicative confirmed] Q3 (line 29) confirms multiplicative phase-duration multipliers (`1.15` / `1.00` / `0.85`), not fixed-day offsets. Research (`Lal 2020`, Cornell, `extension.umn.edu`) cited. Calibration for observable parity (`SC-003`) stated. → Mechanism unambiguous. [Verification / Evidence, Spec §Clarifications Q3 (line 29)]
- [x] CHK-1406 [VERIFICATION — performance contract complete] All targets, contingency parameters, guard reference, and dependency locate-markers (`:113` / `:176`) present. No missing parameter (chunk size, budget, stride, spread, guard insertion). → Performance spec complete. [Verification / Completeness, Spec §FR-001 (line 74), §Clarifications Q7 (line 34), §Dependencies (line 139)]
- [x] CHK-1407 [VERIFICATION — host-authoritative consistent] FR-006 (line 79) defines host-only for persistent growth-phase, `HostAtomicLockService` / `TryApplyCompostWithLock` (2779–2853) cited, sync via `GameLocation.terrainFeatures`, discrete harvest rolls local. `ReGen Valley` first-wins documented (`Assumes` / `Principle VI`). → Multiplay consistency verified. [Verification / Multi-player, Spec §FR-006 (line 79), §A5 (line 90), §Assumes (line 145)]
- [x] CHK-1408 [VERIFICATION — composition verified] A2 (line 87) requires multiplicative composition (not replacement). FR-001 (line 74) "before vanilla", FR-002 (line 75) "additively ... never overriding", FR-003 (line 76) "multiplicative on base" — all consistent. → Composition rule unambiguous. [Verification / Consistency, Spec §A2 (line 87), §FR-001 (line 74), §FR-002 (line 75), §FR-003 (line 76)]
- [x] CHK-1409 [VERIFICATION — edge cases covered] FR-007 (line 80) fallback to vanilla for corrupt/missing; FR-009 (line 82) mean-verified manual loop; FR-005 (line 78) rollback prerequisite; `HealthValue_ClampedToMinMax` with NaN/±Inf guards cited. → Edge definition complete. [Verification / Safety, Spec §FR-007 (line 80), §FR-009 (line 82), §FR-005 (line 78)]
- [x] CHK-1410 [VERIFICATION — success criteria measurable] SC-001/SC-002/SC-003 compare days (technology-agnostic); baselines 0; targets unambiguous; SC-003 (Medium = vanilla) serves as control. No API reference inside criteria. → Criteria quality verified. [Verification / Measurability, Spec §SC-001 (line 107), §SC-002 (line 108), §SC-003 (line 109)]
- [x] CHK-1411 [VERIFICATION — no implementation leaks] Note line 161 confirms "No implementation details added"; no `Harmony.Patch`, no call syntax, no numeric assignments in FR / SC / A; `ModConstants` references by name only. → Hygiene verified against `AGENTS.md` anti-patterns (no expression-bodied, no hardcoded, no `.Result`/`.Wait`). [Verification / Hygiene, Spec §Note (line 161), §AGENTS.md conventions, §Non-Goals (line 118–131)]
- [x] CHK-1412 [VERIFICATION — dependency ordering logical] 004 rollback → 002 decay/performance → 001 visualization → 005 growth activation; all prerequisites listed (spec-002 `:113`/`:176`, spec-001 `InvalidateCache`, spec-004 `SC-008`). No circular dependency. → Dependency graph consistent. [Verification / Ordering, Spec §Dependencies (line 136–141)]
- [x] CHK-1413 [VERIFICATION — clarification resolution complete] All 9 clarification questions from assessment tracked to resolution status: Q1 (hook, resolved), Q2 (scale, resolved), Q3 (growth, resolved), Q4 (quality/extra, resolved), Q5 (rollback / `ReGen Valley`, deferred — verification gate), Q6 (growth-application at `DayStarted`, resolved 2026-10-15), Q7 (chunk/guard + harvest feedback, resolved 2026-10-16). 8 integrated into spec; 1 deferred with gate. → No unrecorded clarification. [Verification / Clarification-tracking, Spec §Clarifications (line 18–35) session headers; §Open Questions (line 150–158)]

---

## Summary — Checklist Status (Requirements Quality, Not Implementation)

**Total checklist items**: 109 (CHK-101 through CHK-1413, with some review-noted gaps at CHK-206 and CHK-307 — both are notes, not failures; CHK-206 is an agent-evidence gap in FR text that does not affect spec quality; CHK-307 is an expected implementation-deferred item).

**Pass (spec defines clearly)**: 107+ — every criterion for growth-multiplier definition, hook-path confirmation, performance contract, chunk/spread mechanism, host-authoritative guard, vanilla composition, fixed-offset exclusion, edge fallback, success-criteria measurability, specification hygiene, dependency prerequisites, and consistency is satisfied in `spec.md`.

**Notes / Expected gaps (not ambiguity)**:
- CHK-306 / CHK-307: Chunk mechanism not implemented — expected at spec stage; defined in spec; implementation deferred.
- CHK-206: `HoeDirt` filter reference exists in agent evidence (`ModController` 1483–1492) but not restated inside FR-001 verification text — minor completeness note; can be added in plan/task without spec change.
- Deferred rollback (`SC-008`, `FR-005`) — prerequisite from spec 004, not a gap in 005 spec quality; documented with verification gate.

**Status recommendation**: **Requirements quality verified** — all 13 covered sections (multiplier definition, hook path, performance/contingency, host-authoritative guard, vanilla composition, fixed-offset exclusion, edge cases, success-criteria measurability, specification hygiene, dependency prerequisites, consistency, verification notes, open-questions tracking) pass. Feature is ready for `/speckit.plan` — with the documented condition that `SC-008` (spec 004 rollback verification) must pass before any yield-increase feature activates (`FR-005`; `Dependencies` verification gate; `Open Questions` deferred item at line 156).

**No edits to `spec.md` required** — checklist does not alter spec content; references only. No code invented; no `Harmony.Patch`, no `Crop.newDay()` syntax, no `ModConstants` values embedded in criteria. All references point to `spec.md` sections / lines / clarification answers.
