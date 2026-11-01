# Feature Specification: Add Lib.Harmony and Harmony Patch for Crop.harvest (HarvestQualityService Seam)

**Feature Branch**: `015-harmony-harvest-seam`
**Created**: 2026-11-01
**Status**: Draft — promoted from assessment `harmony-harvest-quality-seam` (verdict: go, 2026-11-01)
**Input**: User description: "Adicionar Lib.Harmony (0Harmony) como dependência e escrever um patch Harmony Prefix/Postfix em Crop.harvest que delega a um seam testável HarvestQualityService. O seam contém toda a lógica de tier (testável via xUnit/FsCheck); o patch em si é verificado apenas no Gate 3 in-game. Segue o caminho de fallback da Q2, ativado pelo spike. Conforme SMAPI: code API (não annotations), postfix preferido, try/catch com fallback ao original, EnableHarmony=true."

---

## Clarifications

### Session 2026-11-01 (assessment handoff)

- **Hook mechanism**: Harmony is required (fallback activated) — T007 spike confirmed `Crop.harvest` is a `public virtual` method, so public-field manipulation cannot intercept it. `Object.Quality` is a public get/set property (direct assignment). See `specs/014-soil-health-crop-quality/spike-t007-gamepath.md`.
- **SMAPI guidance**: Use `<EnableHarmony>true</EnableHarmony>` (SMAPI-bundled Harmony 2.2.x); code API (not annotations/`PatchAll`); prefer postfix over prefix; avoid transpilers; static patch methods; try/catch with fallback to original code; log errors; default to original game logic.
- **Testability**: All tier logic lives in a `HarvestQualityService` seam (pure functions, testable via xUnit/FsCheck without a game runtime); the patch itself is verified only by the Gate 3 in-game session.
- **Host authority**: Replicated deterministic roll (spec 014 A8) — soil health synced host→client on `DayStarted`; modifier applied locally on triggering client; seam must be pure/deterministic given inputs.
- **Dependency posture**: This is the first Harmony use in the project. Growth modifier (spec 005) remains public-field (no Harmony). Harmony is used only where public fields are insufficient (harvest method interception).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Harvest-Quality Interception Works at Runtime (Priority: P1)

As a maintainer, I want the `Crop.harvest` method to be intercepted by a Harmony patch that delegates to a testable `HarvestQualityService` seam, so that the tier-based quality effects (Poor → no stars, Living → silver/gold/iridium + extra harvest, Medium → vanilla) actually execute during real gameplay.

**Why this priority**: Without this, spec 014's harvest-quality rules exist only on paper — no runtime code applies them. This feature is the implementation enabler for the harvest half of the central promise.

**Independent Test**: With the mod loaded in a SMAPI session, harvest a crop on a Poor tile and a Living tile; confirm the Poor item is normal quality (0 stars) and the Living item may be silver/gold/iridium with occasional extra harvest, while Medium is byte-identical to vanilla. Confirm the SMAPI log shows no Harmony errors and the mod loads with the stability warning (expected for Harmony mods).

**Acceptance Scenarios**:

1. **Given** the mod is loaded with `<EnableHarmony>true</EnableHarmony>`, **When** the game starts, **Then** SMAPI registers the Harmony patch on `Crop.harvest` via the code API (no annotations/`PatchAll`), and logs the expected stability warning (acceptable for Harmony mods).
2. **Given** a tile with soil health < 31 (Poor), **When** the player harvests a crop, **Then** the harvested item's `Quality` is forced to 0, and a transient "Soil too poor for quality crops" feedback is shown (per spec 014 FR-001/FR-006).
3. **Given** a tile with soil health ≥ 71 (Living), **When** the player harvests a crop, **Then** the primary item's quality roll receives `+0.08` (may yield silver/gold/iridium) and there is a `+0.10` chance of an extra harvest, with transient "Living Soil"/"Extra harvest!" feedback as specified.
4. **Given** a tile with soil health in [31, 71) (Moderate), **When** the player harvests, **Then** the quality roll is byte-identical to vanilla (no modifier, no feedback).
5. **Given** the patch throws an exception, **When** harvest occurs, **Then** the patch logs the error and falls back to the original game logic (no crash, no data corruption).

### User Story 2 - HarvestQualityService Seam Is Fully Testable (Priority: P1)

As a developer, I want all tier logic extracted into a pure `HarvestQualityService` seam (xUnit/FsCheck-testable without a game runtime), so that TDD (Principle IV) is satisfied for the logic even though the Harmony patch itself can only be verified in-game.

**Why this priority**: Without the seam, the logic is untestable (Harmony patches can't run in the headless host). The seam is the testability anchor for the entire feature.

**Independent Test**: Run `dotnet test` on `HarvestQualityServiceTests` — all tier-mapping, probability-boost, inventory-full, and edge-case assertions pass without a game runtime.

**Acceptance Scenarios**:

1. **Given** a `HarvestQualityService` instance, **When** called with health < 31, **Then** it returns "force Quality = 0" and "Poor feedback" regardless of Farming level/fertilizer.
2. **Given** health in [31, 71), **When** called, **Then** it returns "no modification" and "no feedback".
3. **Given** health ≥ 71, **When** called, **Then** it returns "+0.08 boost" and "+0.10 extra-harvest" flags, plus feedback flags per conditionality (B1/B2).
4. **Given** NaN/Infinity health, **When** called, **Then** it returns "no modification" (Unknown → vanilla).
5. **Given** forage crops, **When** called, **Then** it returns "no modification" (early return, A6).
6. **Given** measured health 0, **When** called, **Then** it returns "force Quality = 0" (Poor, A7).

### User Story 3 - EnableHarmony and Patch Registration (Priority: P2)

As a maintainer, I want `<EnableHarmony>true</EnableHarmony>` in the `.csproj` and patch registration via the code API in `ModEntry`, so that the Harmony patch is applied at game launch using SMAPI-bundled Harmony 2.2.x.

**Why this priority**: Without `EnableHarmony`, SMAPI won't load Harmony and the patch won't apply. Code API (not annotations) is required for SMAPI rewriting compatibility.

**Independent Test**: Build succeeds with `EnableHarmony=true`; SMAPI log at launch shows the patch registered; no `PatchAll`/annotation usage in code.

**Acceptance Scenarios**:

1. **Given** `<EnableHarmony>true</EnableHarmony>` in `LivingRoots.csproj`, **When** building, **Then** build succeeds and uses SMAPI-bundled Harmony.
2. **Given** `ModEntry.Entry`, **When** the game launches, **Then** the patch is registered via `new Harmony(ModManifest.UniqueID).Patch(...)` (code API, not annotations).
3. **Given** the patch class, **When** inspected, **Then** all patch methods are `static`, wrapped in try/catch, log errors, and default to original code on failure.

### Edge Cases

- **Patch failure**: if the patch throws, log the error and fall back to original game logic (no crash, no data corruption). SMAPI shows the stability warning (expected for Harmony mods).
- **Inlining**: `Crop.harvest` is large enough not to be inlined by the JIT; verify via Gate 3.
- **Android/cross-platform**: Harmony is higher risk on Android; SMAPI rewriting may not cover all patch shapes. Warn and accept untested (consistent with existing policy).
- **Crop scope (A6)**: early return for forage crops (`crop.forageCrop == true`), fibre seeds, Qi Fruit; fruit trees, giant crops, tea bushes, ground forage are not `Crop` instances (out of scope).
- **NaN/Infinity (Unknown)**: no modification (vanilla) — seam returns "no modification."
- **Measured 0 (sparse cache)**: Poor → force 0 (A7).
- **Inventory full**: main item vanilla-blocked (red message, crop stays); extra item dropped as debris (vanilla semantics, no mod override).
- **Multiplayer**: replicated deterministic roll (A8) — seam must be pure/deterministic given inputs; host-authoritative soil health synced on `DayStarted`.
- **Feedback conditionality (B1/B2)**: seam reports feedback flags; patch drives `VisualizationService` accordingly (broadcast shared outcomes; local-only input).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add `<EnableHarmony>true</EnableHarmony>` to `LivingRoots/LivingRoots.csproj` in the first `<PropertyGroup>`, using SMAPI-bundled Harmony (no separate package version).
- **FR-002**: System MUST register the Harmony patch in `ModEntry.Entry` via the code API (`new Harmony(ModManifest.UniqueID).Patch(...)`), NOT via annotations or `PatchAll`.
- **FR-003**: System MUST implement a `HarvestQualityService` seam (in `LivingRoots/Domain/Services/HarvestQualityService.cs`, interface `IHarvestQualityService` in `LivingRoots/Domain/IHarvestQualityService.cs`, types in `LivingRoots/Domain/HarvestQualityTypes.cs`) containing all tier logic as pure functions testable via xUnit/FsCheck without a game runtime. The seam MUST expose: `Decide(float soilHealth, HarvestQualityInputs inputs)` → `HarvestQualityDecision`; `ResolveFeedback(HarvestQualityDecision, bool enhancedQualityOccurred, bool extraHarvestOccurred)` → `HarvestFeedbackFlags`; `BoostQualityProbability(float vanillaProbability, HarvestQualityDecision)`; `BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision)`. Inputs/outputs follow the recommended signatures in `research-seam` (internal-seam.md §5): `HarvestQualityInputs` (FarmingLevel, FertilizerTier 0–3, IsPaddyCrop, RegrowDays, HarvestMaxIncreasePerFarmingLevel, IsForageCrop, IsFibreSeed, IsQiFruit, PestQualityPenalty int?, IsInventoryFull); `HarvestQualityDecision` (Tier, IsInScope, ForceQualityZero, QualityBoost, ExtraHarvestBoost, PestQualityPenalty, MainItemBlocked, ExtraItemDroppedAsDebris); `HarvestTier` enum (Unknown/Poor/Moderate/Healthy/OutOfScope); `HarvestFeedbackFlags` flags (None/Poor/LivingQuality/ExtraHarvest). The seam MUST accept primitives and return decisions; no SMAPI/game dependencies (testable in headless host). The seam MUST reference `ModConstants.SoilQualityHealthyBoost` and `ModConstants.SoilExtraHarvestHealthyBoost` (satisfies `ProductionSourceGuardTests.NoUnreferencedConstantsSurvive`).
- **FR-004**: System MUST implement a Harmony patch on `StardewValley.Crop.harvest` (`public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest)`) via the **code API** (not annotations/`PatchAll`). Patch style: a **non-suppressing two-phase patch** — (a) a `Prefix` that returns `true` (original still runs) only to apply the crop-scope guard early and capture inputs/state; (b) a `Postfix` that forces `Quality = 0` for Poor, applies the Healthy quality boost as a conditional upgrade of the already-rolled `Quality`, drives `VisualizationService` feedback, and handles inventory-full (main item vanilla-blocked; extra item debris). A **suppressing prefix (return false) is never acceptable** (it would skip the entire harvest pipeline and force re-implementing vanilla harvest, violating FR-003 "compose, don't replace"). If exact in-method roll modification is later required, a **Transpiler** is the documented fallback (last resort per SMAPI guidance), not the default.
- **FR-005**: The patch MUST be a `static` method wrapped in `try/catch` that logs errors via `IMonitor` and defaults to original game logic on failure (return `true` from prefix / skip modification in postfix). Patch registered in `ModEntry.Entry` via `new Harmony(ModManifest.UniqueID).Patch(...)`.
- **FR-006**: Prefer postfix over prefix when possible; avoid transpilers unless there is no alternative. Postfix cannot retroactively change the in-method probability `p` (already rolled) or the extra-harvest count (already consumed) — those are inputs decided by the seam and applied post-hoc (Poor: overwrite `Quality`; Healthy: conditional upgrade; extra count: manual spawn only if needed, otherwise accept vanilla's count). This is a documented deviation from strict "+0.08 on p" injection; Medium remains byte-identical because the patch is a no-op for Moderate/Unknown.
- **FR-007**: The patch MUST handle crop scope: early return (no modification, no feedback) for forage crops (`crop.forageCrop == true`), fibre seeds, and Qi Fruit (A6).
- **FR-008**: The patch MUST handle inventory-full: main item vanilla-blocked (red message, crop stays); extra item dropped as debris via `Game1.createItemDebris` (vanilla semantics).
- **FR-009**: The patch MUST handle NaN/Infinity (Unknown → no modification, vanilla).
- **FR-010**: The patch MUST handle measured 0 (sparse cache → Poor → force 0).
- **FR-011**: The seam MUST be pure/deterministic given inputs (replicated deterministic roll, A8) so both clients agree.
- **FR-012**: System MUST NOT break the `ProductionSourceGuardTests.NoUnreferencedConstantsSurvive` guard — every new constant has ≥1 production reference.
- **FR-013**: System MUST NOT modify `README.md` strings asserted by `FeatureStatusTests` without updating the test.
- **FR-014**: System MUST follow TDD: RED-phase tests (property-based where applicable) written and failing before implementation; one test file per subject.

### Key Entities

- **HarvestQualityService (new seam)**: pure service in `LivingRoots.Domain.Services`, interface `IHarvestQualityService` in `LivingRoots.Domain`, types in `LivingRoots.Domain.HarvestQualityTypes`. Holds all tier logic. Inputs: `HarvestQualityInputs` (soil health, Farming level, fertilizer tier, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, crop-scope flags, pest state int?, inventory-full bool). Outputs: `HarvestQualityDecision` (Tier, IsInScope, ForceQualityZero, QualityBoost, ExtraHarvestBoost, PestQualityPenalty, MainItemBlocked, ExtraItemDroppedAsDebris) + `HarvestFeedbackFlags` (None/Poor/LivingQuality/ExtraHarvest) via `ResolveFeedback`. Methods: `Decide`, `ResolveFeedback`, `BoostQualityProbability`, `BoostExtraHarvestChance`. No SMAPI/game dependencies (testable in headless host). Wired in `ModEntry` as `new HarvestQualityService()`.
- **Harmony patch (new)**: static class registered via code API (`new Harmony(ModManifest.UniqueID).Patch(...)`). Delegates to `HarvestQualityService`. Handles crop scope, inventory-full, multiplayer. Verified only by Gate 3 in-game. Uses `EnableHarmony=true` (SMAPI-bundled 2.2.x).
- **Lib.Harmony dependency (new)**: enabled via `<EnableHarmony>true</EnableHarmony>` in `LivingRoots.csproj` (SMAPI-managed, no separate version pin).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `HarvestQualityService` unit tests (xUnit/FsCheck) pass with 0 failures, covering tier mapping, probability boost, inventory-full, NaN/Unknown, crop scope, and feedback conditionality/co-occurrence — all without a game runtime.
- **SC-002**: `dotnet build -c Release` succeeds with `EnableHarmony=true` (0 errors).
- **SC-003**: SMAPI log at launch shows the patch registered and the expected stability warning (acceptable for Harmony mods); no Harmony errors during harvest.
- **SC-004**: In-game Gate 3 session captures the 3 signals (observation + save diff + SMAPI log) for the growth+quality loop, with a second player for multiplayer verification (D1) — this is the only attestation of "verified" status.
- **SC-005**: `dotnet format --verify-no-changes` clean.
- **SC-006**: No annotations or `PatchAll` used (code API only); all patch methods static; try/catch with fallback to original.

## Assumptions

- Maintainer approves adding `Lib.Harmony` (0Harmony) as a dependency (first Harmony use in the project). Approved via assessment verdict "go" (2026-11-01) and the Q2 fallback path.
- `Crop.harvest` member names confirmed by T007 spike (no change needed): `public virtual bool harvest(int, int, HoeDirt, JunimoHarvester, bool)`; `Object.Quality` get/set; `HoeDirt.dayUpdate`; `Crop.dayOfCurrentPhase`/`newDay`/`phaseDays`.
- `HarvestQualityService` seam API shape resolved to the recommended design (internal-seam.md §5): `IHarvestQualityService` with `Decide`, `ResolveFeedback`, `BoostQualityProbability`, `BoostExtraHarvestChance`; types `HarvestTier`, `HarvestFeedbackFlags`, `HarvestQualityInputs`, `HarvestQualityDecision` in `LivingRoots.Domain`; implementation in `LivingRoots.Domain.Services.HarvestQualityService`. Wired in `ModEntry` as `new HarvestQualityService()`.
- Patch style resolved: **non-suppressing two-phase patch** (Prefix returns `true` for state capture + crop-scope guard + extra-harvest probability adjustment; Postfix forces `Quality = 0` for Poor, applies Healthy boost as conditional upgrade, drives feedback, handles inventory-full). Suppressing prefix (return false) is never acceptable. Transpiler is the documented fallback (last resort) if exact in-method roll modification is later required.
- `SC-008` rollback verification (spec 014) is an absolute blocker for yield activation (not for design/tests) — design/implementation of the seam and patch may proceed concurrently.
- Android/cross-platform: Harmony is higher risk; accepted as "untested on Android" with documentation (README, Nexus page: do not tick "Android Compatible"; optional runtime guard; best-effort fallback to vanilla on patch failure). Consistent with existing desktop-only policy (GDD: "Sem console/mobile no v1").
- An ADR (`docs/adr/001-harmony-for-harvest-interception.md`) will be created to formally document the decision (recommended, not blocking).
- Host-authority contract: replicated deterministic roll (spec 014 A8) — soil health synced host→client on `DayStarted`; seam pure/deterministic given inputs so both clients agree.
- This spec is the implementation enabler for the harvest-hook portion of spec 014 (tasks T044–T054, T007, T017); it does not duplicate spec 014's functional requirements but adds the Harmony-specific implementation contract.

## Out of Scope (Non-Goals)

- Replacing the public-field growth modifier (spec 005) with Harmony.
- Pest system and "new interactions" (separate specs).
- Numerical balance tuning beyond first-pass values.
- Live 2-player verification beyond D1 roadmap obligation.
- Formal ADR (recommended but not blocking).

---

*Promoted from assessment `harmony-harvest-quality-seam` (verdict: go, 2026-11-01). Assessment artifacts: `.specify/assessments/harmony-harvest-quality-seam/` (intake, problem, concept, decision). Spike: `specs/014-soil-health-crop-quality/spike-t007-gamepath.md`. Constitution: `.specify/memory/constitution.md` v2.0.0.*