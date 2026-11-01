# Feature Specification: Soil Health → Crop Quality (Epic: Tier Outcomes)

**Feature Branch**: `014-soil-health-crop-quality`

**Created**: 2026-11-01

**Status**: Draft — promoted from assessment `soil-health-crop-quality` (verdict: go, all 10 clarification questions resolved and accepted 2026-11-01)

**Input**: User description: "Soil Health → Crop Quality — a segunda metade da promessa central (Living Soil 71–100% finalmente paga na colheita). Reusa persistência + overlay existentes; fecha o loop build → protect → harvest." Briefing: `specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md`. Assessment: `.specify/assessments/soil-health-crop-quality/` (intake, research, problem, concept, decision — all Go). Constitution: `.specify/memory/constitution.md` v2.0.0.

---

## Clarifications

### Session 2026-11-01 (assessment clarification — all 10 blocking questions resolved)

- **Q1 Band scale**: Switch to ROADMAP 0-30/31-70/71-100 (Cornell Soil Health Assessment Training Manual 2nd ed. 2009), implemented as half-open intervals with edges at 31 and 71: Poor `[0,31)`, Moderate `[31,71)`, Healthy `[71,100]`. Migration required (constants, `HealthCategory`, `ColorInterpolationService`, `ModController`, tests, Gate 3 thresholds, docs).
- **Q2 Hook mechanism**: Prefer public-field manipulation (proven for growth via `Crop.newDay`/`dayOfCurrentPhase`/`phaseDays`); Harmony only as fallback for harvest-quality interception if public fields are insufficient. Verify member names (`Crop.harvest`, `Crop.dayUpdate`, `HoeDirt.dayUpdate`, `Object.Quality`) against the 1.6.3/1.6.14 binary with a `GamePath` build before writing hooks.
- **Q3 Poor "no stars"**: Force `Object.Quality = 0` (none) at harvest time, but only when soil health < Poor threshold (31). Medium and Living tiers never touch `Quality` — guaranteed vanilla by omission. Compose with (not replace) the vanilla quality roll.
- **Q4 CollectCompost bug**: Fix the latent partial-success duplication bug (`CompostingBinService.CollectCompost`) before any output increase — two-phase commit with dry-run capacity check + compensating rollback + idempotency flag (`OutputGranted`). Prerequisite, separate task.
- **Q5 CVD channel**: Transient feedback uses sprite shape + floating text + SFX (event-driven via `TemporaryAnimatedSprite` + `Display.Rendered`), immune to the 1000-tile degradation threshold. Non-chromatic; does not rely on color alone (GAG 1.4.1 / XAG 103).
- **Q6 Numerical balance**: Keep spec 005 first-pass values as tunable constants: `SoilGrowthPoorMultiplier = 1.15`, `SoilGrowthHealthyMultiplier = 0.85`, `SoilQualityHealthyBoost = 0.08` (additive on vanilla quality roll `p`), `SoilExtraHarvestHealthyBoost = 0.10` (additive on `Data/Crops.ExtraHarvestChance`). All labeled "first-pass, tunable" in `ModConstants`.
- **Q7 Pests scope**: Pest system (aphids on Poor soil) is a **separate spec**, shipped after crop quality. The only coupling: pest infestation clamps quality by `PestQualityPenalty` at harvest (read of pest state by quality hook, not a merge).
- **Q8 "New interactions"**: Deferred to its own spec. ROADMAP/GDD wording softened to "enables additional agroecological practices (see separate spec)" — docs-only change.
- **Q9 Persistent marker**: Transient feedback only. No persistent per-tile tier marker (would inherit 1000-tile degradation and 16.67 ms/frame budget; redundant with existing overlay).
- **Q10 Gate 3 run**: Run the in-game SMAPI session (~30 min, rebuild stale dll) to capture the 3 signals and validate runtime hooks against the real binary.

### Session 2026-11-01 (clarify — autonomous resolutions from codebase + research)

- **A1 Inventory full at harvest**: Resolved to **debris fallback** for the extra-harvest item (vanilla behavior): the main harvested item follows vanilla (blocked with red message when inventory is full, crop stays in ground); any extra-harvest item produced by `ExtraHarvestChance` is dropped as ground debris via `Game1.createItemDebris` (vanilla pattern), never silently destroyed, never duplicated. No retryable state. Rationale: vanilla `Crop.harvest` blocks the main item on full inventory but always spawns extras as debris; mod's "Nada se perde" principle (GDD) + dislodgement/cooking-waste debris pattern. Compost collection's retain-state contract is a different (collection, not harvest) path and is unaffected.
- **A2 Extra-harvest item quality**: Resolved to **normal quality (0)**, inheriting vanilla behavior. Decompiled `Crop.harvest` (1.6.15) creates extra items as fresh `Object` instances with default `Quality = 0`; they do not inherit the main item's rolled quality and there is no per-extra re-roll. The mod's `+0.08` boost applies to the main item's roll only (FR-003 compose, don't replace; FR-004 no data edits). Keeps SC-001 (quality) and SC-005 (extra count) orthogonal and avoids partial-delivery invasiveness.
- **A3 NaN/Infinity (Unknown) harvest behavior**: Resolved to **no modification (vanilla roll, no forced 0, no +0.08, no feedback)**, matching the growth modifier's skip-non-finite behavior (`ModController` `continue` on NaN/Infinity) and the overlay's Unknown (gray) treatment. Finite out-of-range values still clamp to the boundary category (below 0 → Poor → force 0; above 100 → Healthy → +0.08). Measured `0` (sparse-cache floor) remains Poor → force 0. Rationale: NaN is the absence-of-measurement sentinel, distinct from a real 0% reading; penalizing/boosting unmeasured tiles would make outcomes depend on sampling history.
- **A4 Harvest feedback text localization**: Resolved as **literal text** (reusing the existing `TemporaryAnimatedSprite.text` floating-text pattern, no new i18n keys), consistent with compost/mulch (`CompostApplicationService`, `MulchApplicationService`) and spec 005 A4 (zero new i18n keys). Refusal messages (red HUD) keep using existing i18n keys (`message.harvest.inventory-full`). No change to spec text required beyond this note.
- **A5 Pest clamp integration**: Resolved as an **optional, null-safe read**: the quality system reads pest state through an optional `IPestStateProvider` resolved in `ModEntry` composition; when the pest system is not installed the provider is null and no clamp applies. The interface contract is finalized in the pest spec when it ships; this spec requires only that the hook call site tolerates absence (no hard dependency, no new constant beyond `PestQualityPenalty`).

### Session 2026-11-01 (clarify — batch questions resolved)

- **B1 Feedback-text conditionality**: "Living Soil" shows only when the tier changes the observable outcome (enhanced quality silver/gold/iridium and/or extra harvest); a Living harvest yielding normal quality with no extra item produces no feedback (silence = vanilla), preserving the "silence = vanilla" contract. (Option A accepted.)
- **B2 Feedback-text co-occurrence**: when both enhanced quality and an extra harvest occur on the same Living harvest, both "Living Soil" and "Extra harvest!" show as two separate sprites broadcast together via `Game1.multiplayer.broadcastSprites(location, sprite1, sprite2)`; each with standard alphaFade/motion. (Option A accepted.)

### Session 2026-11-01 (clarify — autonomous resolutions from codebase + research, round 2)

- **A6 Crop scope**: Resolved to **regular tilled crops only** — the quality hook fires inside `Crop.harvest`, which is invoked only by `HoeDirt.performUseAction`, `HoeDirt.performToolAction`, and `JunimoHarvester.update`. Fruit trees (`FruitTree`), giant crops (`GiantCrop`/`ResourceClump`), tea bushes, and ground-picked forage are **out of scope** (separate terrain features, separate harvest paths; no `Crop.harvest` call). Forage crops (`crop.forageCrop == true`) are also excluded: their quality is rolled from the **Foraging** skill (not Farming), and forcing `Quality = 0` on Poor would override the Botanist profession's iridium guarantee. Fibre seeds and Qi Fruit are always normal quality and are skipped. Flowers, multi-harvest crops (blueberry, cranberry, coffee), paddy crops (rice, taro), and regrowing crops are in scope. Poor → force `Quality = 0` and Healthy → `+0.08`/`+0.10` apply **only when `!crop.forageCrop`**; forage crops keep vanilla quality on all tiers (no mod effect, no feedback). Rationale: mechanism mismatch (Foraging vs Farming), Botanist conflict, and FR-003's compose list names only Farming modifiers. Implementation guard: inside the `Crop.harvest` hook, return early when `crop.forageCrop` is true (and when the crop is fibre/Qi Fruit for clarity).
- **A7 Sparse-cache 0 vs. unmeasured (NaN)**: Resolved to keep the existing T166 adjudication — measured `0` is **Poor** (force `Quality = 0` in harvest; `SoilGrowthPoorMultiplier` in growth), unmeasured `NaN` is **neutral/vanilla** (no modification, gray "Unknown" overlay). The spec 014 edge case "Health = 0 (sparse cache): treated as unrendered neutral for overlay, but for harvest quality, health 0 is Poor" stands. The only required change is consistency: `ApplySoilGrowthModifier` currently skips `health <= 0f` (treating measured `0` as vanilla), which contradicts T166/`HealthCategory`/harvest quality. Fix the growth modifier to apply `SoilGrowthPoorMultiplier` to measured `0` (skip only non-finite and negative values), so growth, harvest, and category all agree that `0 = Poor` and `NaN = neutral`. Also amend the spec 014 edge-case note that "a never-touched tile yields no-star harvests" — unmeasured (`NaN`) tiles are vanilla for growth and harvest (per A3); only measured Poor (1–33, and measured `0`) incur the penalty. Rationale: measured `0` ≠ unmeasured (T166; NRCS/Cornell "baseline required before judgement"; "unmeasured is never drawn as zero"); onboarding is already protected by the `NaN` default (Hodent). Scientific/agronomic basis: NRCS TN 470-16, Cornell CASH, Lunch Special "unmeasured ≠ zero". Implementation tasks: (1) fix growth modifier `health <= 0f` skip; (2) correct the stale T166/T182 comment in `SoilHealthService.SetHealthInternal` (explicit `0` is runtime-only and does not survive save/load, per `SaveData_ZeroValue_NotPersisted`).
- **A8 Multiplayer roll mechanism (C1)**: Resolved to **replicated deterministic roll** (Option A). Soil health is synced to farmhand clients on `DayStarted` (and on compost application) via `helper.Multiplayer.SendMessage`; the harvest modifier is applied locally on whichever client triggers the harvest through the `Crop.harvest` Harmony patch, reading the synced soil health. SV's single-harvest guarantee ensures the extra-harvest roll fires once. No proxying of the synchronous harvest path. Host takes the same local path (no message round-trip when `Context.IsMainPlayer`). Divergences are transient and re-sync on next `DayStarted`. FR-009 is updated to reflect "replicated deterministic roll" and soil-health sync is added as a prerequisite task. Rationale: gives farmhands tier effects without proxying a synchronous harvest, matches SV's deterministic-RNG reality (shared DaysPlayed/uniqueID/tile/fertilizer net field), avoids high-risk async inventory mutation. If strict auditability is later required, post-harvest attestation (host records the roll outcome in a net field) can be layered on without changing the roll path. Constitution Principle VI alignment: shared farm state (soil health) is host-authoritative and synced; per-player harvest execution is client-local but deterministic, which is acceptable because the harvested item lives in the acting player's inventory and SV already propagates inventories.
## User Scenarios & Testing *(mandatory)*

### User Story 1 - Living Soil Pays Off at Harvest (Priority: P1)

As a farmer with Living Soil (71–100%), I want my harvests to be visibly better (silver/gold/iridium quality + chance of extra harvest), so that investing in soil health has a tangible, observable payoff at harvest time.

**Why this priority**: This is the second half of the mod's central promise. Without it, soil health only affects growth speed — the player has no reason to push soil into the Living tier beyond speed.

**Independent Test**: Plant the same seed on a Poor tile (0%) and a Living tile (≥71%); harvest both; confirm the Living tile produces higher-quality items and occasionally an extra harvest, while the Poor tile produces no-star items.

**Acceptance Scenarios**:

1. **Given** a tile with soil health ≥ 71 (Living), **When** the player harvests a crop, **Then** the harvested item's quality is rolled with an additive +0.08 boost to the vanilla quality probability, and there is an additional +0.10 chance of an extra harvest item.
2. **Given** a tile with soil health ≥ 71, **When** harvest quality is rolled and yields an enhanced quality (silver/gold/iridium) or an extra harvest, **Then** the result may be silver, gold, or iridium (per the boosted roll), and the player sees the corresponding transient floating-text feedback ("Living Soil" for enhanced quality, "Extra harvest!" for an extra item) with a reward SFX. A Living harvest yielding normal quality with no extra item shows no feedback.
3. **Given** a tile with soil health ≥ 71, **When** an extra harvest roll succeeds, **Then** one additional item is produced and a transient "Extra harvest!" floating text is shown.
4. **Given** a tile with soil health ≥ 71 and a pest infestation (when pest system ships), **When** harvest occurs, **Then** quality is clamped by `PestQualityPenalty` (pest system reads soil state; quality system reads pest state — one-directional).
5. **Given** a multiplayer session where a farmhand harvests a Living or Poor tile, **When** the harvest occurs, **Then** the modifier is applied locally on the triggering client reading synced soil health; both host and farmhand observe the same quality outcome and extra-harvest result; transient feedback visibility follows the hybrid rule (shared farm-state outcomes broadcast; triggering player's own input feedback local-only).

### User Story 2 - Poor Soil Yields No Stars (Priority: P1)

As a farmer with Poor Soil (0–30%), I want harvests to be lower quality (no stars), so that neglecting soil health has a visible, tangible consequence.

**Why this priority**: This is the "cost of inaction" half of the promise. Without it, Poor soil is just "slow growth" — the player never sees a quality penalty.

**Independent Test**: Plant the same seed on a Poor tile (0%) and a Medium tile (50%); harvest both; confirm the Poor tile's item is always normal quality (0 stars), while the Medium tile's quality follows the vanilla roll.

**Acceptance Scenarios**:

1. **Given** a tile with soil health < 31 (Poor), **When** the player harvests a crop, **Then** the harvested item's `Quality` is forced to 0 (none) regardless of Farming level or fertilizer.
2. **Given** a Poor tile harvest, **When** quality is forced to 0, **Then** a transient floating-text feedback ("Soil too poor for quality crops") is shown with the `cancel` SFX.
3. **Given** a Poor tile, **When** the vanilla quality roll would have produced silver/gold/iridium, **Then** the result is still normal quality (override, not penalty).

### User Story 3 - Medium Soil Is Vanilla (Priority: P1)

As a farmer with Medium Soil (31–70%), I want harvests to be identical to the base game, so that the mod does not alter the vanilla experience for adequately-maintained soil.

**Why this priority**: This is the regression control (SC-003). If Medium soil differs from vanilla, the mod breaks the baseline and players cannot attribute outcomes to soil health.

**Independent Test**: Plant the same seed on a Medium tile (50%) and an unmodded tile; harvest both; confirm quality distributions are statistically indistinguishable (within ±5% relative on quality frequency, or ±0.01 absolute) for the same seed/day/fertilizer.

**Acceptance Scenarios**:

1. **Given** a tile with soil health in [31, 71) (Moderate), **When** the player harvests a crop, **Then** the quality roll is identical to vanilla (no modifier applied).
2. **Given** a Moderate tile, **When** `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`, or `quality_fertilizer_item` are in play, **Then** all vanilla modifiers apply unchanged.
3. **Given** a Moderate tile harvest, **When** no modifier is applied, **Then** no transient feedback is shown (silence = vanilla).

### User Story 4 - Transient Tier Feedback at Harvest (Priority: P2)

As a player, I want to see a brief, non-intrusive visual feedback when a tier affects my harvest, so that I can attribute the outcome to soil health without a persistent overlay.

**Why this priority**: Observability (M1, M4). Without feedback, the player cannot tell whether soil health mattered.

**Independent Test**: Harvest a crop on a Poor tile and a Living tile; confirm transient floating text + SFX appear on both, and disappear within ~1 second; confirm no persistent per-tile marker is added.

**Acceptance Scenarios**:

1. **Given** a harvest on a Poor tile, **When** quality is forced to 0, **Then** a transient `TemporaryAnimatedSprite` (16×16 procedural `marker-poor`) + floating text ("Soil too poor for quality crops") + `cancel` SFX appears and fades within ≤1000 ms.
2. **Given** a harvest on a Living tile, **When** quality is boosted (silver/gold/iridium) and/or an extra harvest occurs, **Then** a transient `sparkle-living` sprite + floating text ("Living Soil" for enhanced quality, "Extra harvest!" for an extra item) + reward SFX appears and fades; when both conditions hold, both messages show as two separate sprites broadcast together. When neither condition holds (normal quality, no extra), no feedback is shown.
3. **Given** a harvest on a Moderate tile, **When** no modifier applies, **Then** no transient feedback is shown (silence = vanilla).
4. **Given** the feedback is event-driven, **When** > 1000 tiles are visible, **Then** the feedback still appears (immune to degradation threshold).

### Edge Cases

- **Health = 0 (sparse cache)**: measured `0` is Poor → force `Quality = 0` for harvest and `SoilGrowthPoorMultiplier` for growth (consistent with T166/`HealthCategory`); unmeasured tiles return `NaN` and remain vanilla for growth and harvest (A3/A7). Overlay treats measured `0` as unrendered neutral (vanilla dirt) per spec 004/014; `NaN` renders gray "Unknown". Note: unmeasured (`NaN`) tiles yield vanilla harvests — the "cost of inaction" applies only to tiles that have actually decayed to measured Poor (1–33, or measured `0`).
- **Crop scope (A6)**: the quality hook fires inside `Crop.harvest`, which is invoked only by `HoeDirt.performUseAction`, `HoeDirt.performToolAction`, and `JunimoHarvester.update`. Fruit trees, giant crops, tea bushes, and ground-picked forage are **not** `Crop` instances and are out of scope (separate terrain features / harvest paths). Forage crops (`crop.forageCrop == true`) are excluded to avoid overriding the Botanist profession's iridium guarantee and because their quality rolls from the Foraging skill, not Farming. Fibre seeds and Qi Fruit are always normal quality and are skipped. Flowers, multi-harvest crops, paddy crops, and regrowing crops are in scope.
- **NaN/Infinity health**: resolves to `HealthCategory.Unknown` (not a tier); harvest quality applies **no modification** (vanilla roll, no forced `Quality = 0`, no `+0.08`, no feedback), matching the growth modifier's skip-non-finite behavior and the overlay's Unknown treatment. Finite out-of-range values clamp to the nearest boundary category (below 0 → Poor → force 0; above 100 → Healthy → +0.08) per FR-008/SC-007.
- **Regrowing crops**: quality re-rolls on each harvest; soil tier applies on each harvest independently.
- **Persistence failure**: harvest outcome is transient and is never persisted. On load failure or corrupt/partial save, soil health re-queries as the NaN unknown sentinel → `HealthCategory.Unknown` → vanilla harvest (no forced 0, no +0.08, no feedback, no crash). No partial state is written by the harvest path.
- **Harvest does not mutate soil health**: soil health is read-only at harvest; regrowing crops re-roll quality on each harvest and the tier applies independently per harvest.
- **Garden Pots**: soil health applies when the tile has a tracked soil health record; same rules (Poor/Moderate/Healthy/Unknown).
- **Greenhouse**: soil health applies; same rules.
- **Multiplayer**: replicated deterministic roll (A8). Soil health is synced host→client on `DayStarted`; the harvest modifier runs locally on the triggering client (host or farmhand) reading synced soil health; SV's single-harvest guarantee ensures the extra-harvest roll fires once. Host state wins; client re-syncs soil health on next `DayStarted`. No client-side divergence in observable results when inputs match.- **Inventory full at harvest**: the main harvested item follows vanilla (blocked with a red message when inventory is full; crop stays in ground). Any extra-harvest item produced via `ExtraHarvestChance` is dropped as ground debris via `Game1.createItemDebris` (vanilla behavior): debris stays on the ground until `addItemToInventoryBool` succeeds on pickup, persists across save/load via the location's normal debris sync, stacks per vanilla rules, and is never silently destroyed, never duplicated, and creates no retryable state. No mod override of debris lifetime or stacking. No persistent marker or debris-sync beyond vanilla.
- **Pest infestation (future)**: quality clamped by `PestQualityPenalty`; if pest system not installed, no clamp.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST apply a harvest-quality effect based on the tile's soil health tier: Poor `[0,31)` → force `Quality = 0`; Moderate `[31,71)` → no change (vanilla); Healthy `[71,100]` → additive +0.08 to the vanilla quality probability roll.
- **FR-002**: System MUST apply an extra-harvest effect based on tier: Healthy `[71,100]` → additive +0.10 to `Data/Crops.ExtraHarvestChance`; Poor and Moderate → no change. Extra-harvest items are normal quality (0) (vanilla behavior); only the primary item receives the boosted quality roll (FR-001).
- **FR-003**: System MUST compose with (not replace) vanilla quality modifiers: Farming level, fertilizer (`quality_fertilizer_item` → num2 1/2/3), `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`. Baseline vanilla quality probability roll: `p = 0.2·(FarmingLevel/10) + 0.2·fertTier·((FarmingLevel+2)/12) + 0.01`; silver cap `min(0.75, p·2)`; iridium `p/2` (only if `fertTier ≥ 3`, checked first). Medium tier must be byte-identical to vanilla. The mod's `SoilQualityHealthyBoost = 0.08` is additive on `p` (Healthy only); Poor forces `Quality = 0` after the roll.
- **FR-004**: System MUST NOT use data-entry edits (`Data/Crops`, `Data/Objects`) to implement tier outcomes — all effects are runtime (Harmony or public-field manipulation), because `DaysInPhase`, `ExtraHarvestChance`, and `HarvestMin/MaxQuality` are global per seed/crop.
- **FR-005**: System MUST NOT create new PNG assets — feedback sprites are procedural (`new Texture2D(device, 16, 16)` + `SetData`, singleton pattern per `VisualizationTextures`).
- **FR-006**: System MUST show transient feedback at harvest, **only when the tier changes the observable outcome**: Poor → `marker-poor` sprite + "Soil too poor for quality crops" text + `cancel` SFX (quality forced to 0); Living → `sparkle-living` sprite + "Living Soil" text (when the roll yields an enhanced quality: silver/gold/iridium) and/or "Extra harvest!" text (when an extra item is produced) + reward SFX, shown as two separate sprites broadcast together when both conditions hold; Moderate → no feedback (silence = vanilla). A Living harvest yielding normal quality with no extra item produces no feedback.
- **FR-007**: System MUST use non-chromatic cues (sprite shape + floating text + SFX) for tier feedback — color alone must not convey essential information (GAG 1.4.1 / XAG 103). Default palette must not change.
- **FR-008**: System MUST be event-driven (transient sprites on `TemporarySprites` + floating text on `Display.Rendered`) — immune to the 1000-tile degradation threshold and the 16.67 ms/frame budget.
- **FR-009**: System MUST use a **replicated deterministic roll** for harvest quality and extra harvest: the modifier is applied on whichever client triggers the harvest, reading soil health synced from the host (synced on `DayStarted` and on compost application via `helper.Multiplayer.SendMessage`); SV's single-harvest guarantee ensures the extra-harvest roll fires once; results are deterministic across clients given shared inputs (DaysPlayed, uniqueID, tile, fertilizer net field). The host takes the same local path (no message round-trip when `Context.IsMainPlayer`). Shared farm state (soil health) remains host-authoritative; per-player harvest execution is client-local. Divergence rule: host state wins; client re-syncs soil health on next `DayStarted`. (Strict host-only proxying is explicitly rejected — see clarification A8; post-harvest attestation may be layered on later without changing the roll path.)
- **FR-011**: System MUST NOT produce a persistent per-tile tier marker (no new per-frame draw call).
- **FR-012**: System MUST add 4 new constants to `ModConstants`: `SoilQualityHealthyBoost = 0.08f`, `SoilExtraHarvestHealthyBoost = 0.10f` (plus existing `SoilGrowthPoorMultiplier = 1.15f`, `SoilGrowthHealthyMultiplier = 0.85f`), all labeled "first-pass, tunable."
- **FR-013**: System MUST migrate the band scale from implemented 34/67 to ROADMAP 31/71: update `PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`, `HealthCategory`, `ColorInterpolationService`, `ModController` growth modifier, tests, Gate 3 thresholds, and docs.
- **FR-014**: System MUST follow TDD: RED-phase tests (property-based where applicable, testing invariants) written and failing before implementation. One test file per subject.
- **FR-015**: System MUST NOT modify `README.md` strings asserted by `FeatureStatusTests` (3 `"(Planned)"` strings) without updating the test.
- **FR-016**: System MUST NOT break the `ProductionSourceGuardTests.NoUnreferencedConstantsSurvive` guard — every new constant must have ≥1 production reference.
- **FR-017**: System MUST scope the harvest-quality effect to `Crop` instances reached via `Crop.harvest` (`HoeDirt.performUseAction`, `HoeDirt.performToolAction`, `JunimoHarvester.update`) with `forageCrop == false`. Fruit trees, giant crops, tea bushes, ground-picked forage, fibre seeds, and Qi Fruit are out of scope (no `Crop.harvest` invocation, or always-normal quality). Forage crops keep vanilla quality on all tiers (no forced `0`, no `+0.08`/`+0.10`, no feedback) to preserve the Botanist profession's iridium guarantee.

### Key Entities

- **SoilHealth (existing)**: 0–100 per tile, farm-keyed, sparse (0 omitted), persisted in `soil_health_data_{saveId}.json`. Governing tier bands (post-migration): Poor `[0,31)`, Moderate `[31,71)`, Healthy `[71,100]`.
- **HealthCategory (existing)**: enum `Poor | Moderate | Healthy | Unknown`, derived from soil health via `FromHealthValue`.
- **Harvest outcome (new)**: quality (0–3: none/silver/gold/iridium) and extra-harvest count, determined at harvest time by tier modifier + vanilla roll. Extra-harvest items are **normal quality (0)**, inheriting vanilla behavior (decompiled `Crop.harvest` creates extras as fresh `Object` instances with default `Quality = 0`); only the primary item receives the boosted quality roll. Scope per FR-017: non-forage crops only. Not persisted — derived from soil health at harvest.
- **Tier feedback (new, transient)**: `TemporaryAnimatedSprite` (16×16 procedural) + floating text + SFX, event-driven, not persisted.

### Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fresh save with 3 tiles at 0%, ~50%, and ~90% soil health, planted with the same seed in the same season, yields measurably different quality distributions at harvest — Poor always 0 stars, Living produces silver/gold/iridium more often than Medium.
- **SC-002**: Unit tests assert `PoorTier_HarvestQualityIsAlwaysZero` (any value in `[0,31)` → `Quality == 0`) and `ModerateTier_QualityMatchesVanilla` (distribution statistically indistinguishable from vanilla for same seed/day/fertilizer).
- **SC-003**: Medium soil harvests are byte-identical to vanilla (regression control) — verified by test comparing quality roll output with and without the mod on Moderate tiles (within ±5% relative on quality frequency, or ±0.01 absolute).
- **SC-004**: Host-authoritative roll tests pass with a simulated second player (no client-side roll occurs).
- **SC-005**: Transient feedback (sprites + text + SFX) appears on Poor and Living harvests, disappears within ≤1000 ms, and does not trigger the 1000-tile degradation path or exceed 16.67 ms/frame.
- **SC-006**: Band scale migration complete: all code, tests, Gate 3 thresholds, and docs use 31/71 consistently; no 34/67 references remain (except in migration history).
- **SC-007**: `dotnet build -c Release` and `dotnet test --no-build -c Release` pass with 0 failures; `dotnet format --verify-no-changes` clean.
- **SC-008**: Gate 3 in-game session captures 3 signals (observation + save diff + SMAPI log) for the growth+quality loop, with a second player for multiplayer verification (D1).
- **SC-009**: Extra-harvest count on Living tiles is measurably greater than Medium (host-authoritative roll; per-tile; `ExtraHarvestChance + 0.10`), verified by statistical test over N harvests.
- **SC-010**: Client re-syncs on next `DayStarted` after a divergence; observable convergence verified by test (divergence then convergence).

### Assumptions

- The `Crop.harvest` method name and `Object.Quality` property are verified against the 1.6.3/1.6.14 binary via a `GamePath` build (Q2). If names differ, the spec's hook strategy (public-field vs. Harmony) is adjusted accordingly.
- The band scale migration (Q1) is executed as part of this feature, not as a separate spec.
- `CollectCompost` partial-success bug (Q4) is fixed as a prerequisite task within this feature's scope.
- The `cvdSafe` palette (Q5) is deferred to a follow-up; only the non-chromatic channel (shape + text + SFX) ships now.
- Numerical balance (Q6) uses first-pass values; final tuning happens after Gate 3 playtest.
- Pest system (Q7) and "new interactions" (Q8) are out of scope — separate specs.
- Pest clamp is read through an optional `IPestStateProvider` (null when pest system not installed → no clamp); interface contract finalized in the pest spec.
- Harvest feedback text is literal (reuses the existing `TemporaryAnimatedSprite.text` floating-text pattern; no new i18n keys), consistent with compost/mulch and spec 005 A4; refusal messages keep using existing i18n keys (`message.harvest.inventory-full`).
- Living reward SFX name is deferred to implementation as a tunable constant (cosmetic asset choice, not a spec-level decision); the non-chromatic requirement is satisfied by sprite shape + text + SFX regardless of the specific asset.
- Non-chromatic requirement is verified via grayscale/CVD simulation (no color-only essential information); color-blind safety (complementary tones, pattern opacity ≥0.7) and high-contrast (7:1) are inherited from the existing visualization domain and are not re-specified here.
- Photosensitivity/flicker (GAG thresholds: no >3 flashes/s over >25% screen; no sequence >5s), text scaling (XAG 101), timing adjustability (WCAG 2.2.1), and per-element color choice are deferred to the `cvdSafe` follow-up (briefing `color-schemes-cvd.md` must be created first), consistent with the existing `cvdSafe` palette deferral.
- Feedback placement is specified as `Display.Rendered` (above HUD) per existing rendering-events research; screen-space above HUD is the established pattern.
- Feedback duration is fixed at ≤1000 ms (not user-adjustable in this feature); adjustability deferred to the `cvdSafe` follow-up.
- Test names for feedback conditionality/co-occurrence are promoted to plan/tasks: `LivingTier_EnhancedQuality_ShowsLivingSoilText`, `LivingTier_ExtraHarvest_ShowsExtraHarvestText`, `LivingTier_Both_ShowsBothTexts`, `LivingTier_NormalQualityNoExtra_NoFeedback` (internal-c2 §6).
- Disconnection, host-disconnected, and concurrent-harvest (one player mid-harvest) scenarios are explicitly deferred to roadmap D1 (multiplayer verification obligation); safety invariants (host authority per datum, single source of truth, first-wins, inventory atomicity, no crash on stale data) are specified now and constrain implementation.
- Junimo-harvester and scythe-harvest paths are out of scope (only standard hoe-harvest via `Crop.harvest`); deferred to a follow-up.
- Feedback-text conditionality and co-occurrence resolved (clarify 2026-11-01): "Living Soil" shows only when the roll yields an enhanced quality (silver/gold/iridium) and/or an extra harvest occurs; when both hold, both messages show as two separate sprites broadcast together; a Living harvest yielding normal quality with no extra item produces no feedback (silence = vanilla).
- Pest clamp is read through an optional `IPestStateProvider` (null when pest system not installed → no clamp); interface contract finalized in the pest spec.
- Harvest feedback text is literal (reuses the existing `TemporaryAnimatedSprite.text` floating-text pattern; no new i18n keys), consistent with compost/mulch and spec 005 A4; refusal messages keep using existing i18n keys (`message.harvest.inventory-full`).
- Crop scope (A6): quality effect applies only to non-forage crops reached via `Crop.harvest` (FR-017); forage crops, fruit trees, giant crops, tea bushes, and ground-picked forage are excluded.
- Multiplayer roll mechanism (A8): replicated deterministic roll. Soil health is synced to farmhand clients on `DayStarted` (and on compost application) via `helper.Multiplayer.SendMessage` as a prerequisite for tier effects on farmhand harvests; the harvest modifier runs locally on the triggering client. No new message request/response correlation is required for the roll itself.- Sparse-cache semantics (A7): measured `0` is Poor for growth and harvest; unmeasured `NaN` is neutral/vanilla. Growth modifier must be fixed to apply `SoilGrowthPoorMultiplier` to measured `0` (currently skips it), aligning with T166/`HealthCategory`/harvest quality. The stale T166/T182 comment in `SoilHealthService.SetHealthInternal` must be corrected (explicit `0` is runtime-only and does not survive save/load per `SaveData_ZeroValue_NotPersisted`).
- Feedback-text conditionality and co-occurrence resolved (clarify 2026-11-01): "Living Soil" shows only when the roll yields an enhanced quality (silver/gold/iridium) and/or an extra harvest occurs; when both hold, both messages show as two separate sprites broadcast together; a Living harvest yielding normal quality with no extra item produces no feedback (silence = vanilla).
- No persistent tier marker (Q9) — transient feedback only.
- Gate 3 in-game run (Q10) is executed during implementation, not before spec.

### Out of Scope (Non-Goals)

- Pest system for Poor soil (`briefings/pest-system-mvp.md`) — separate spec, shipped after crop quality.
- "New interactions" unlocked by Living soil — vague promise; separate spec. ROADMAP/GDD wording softened.
- Persistent per-tile tier marker (would inherit 1000-tile degradation + per-frame budget).
- `cvdSafe` color palette (opt-in scheme) — follow-up; briefing `color-schemes-cvd.md` must be created first.
- Numerical balance tuning beyond first-pass values — deferred to playtest.
- Mid-game and End-game systems (Elena, feira, CSA, cozinha, ILPF, greywater, etc.).
- Heirloom pipeline, Joja Fertilizer, Three Sisters, Green Manure, Rotation Journal.
- Multiplayer transport-level verification beyond host-authoritative rolls (D1 — separate obligation).
- Disconnection, host-disconnected, and concurrent-harvest (one player mid-harvest) scenarios — deferred to roadmap D1; safety invariants (host authority per datum, single source of truth, first-wins, inventory atomicity, no crash on stale data) are specified now.
- Junimo-harvester and scythe-harvest paths — out of scope (only standard hoe-harvest via `Crop.harvest`); deferred to a follow-up.
- Photosensitivity/flicker beyond GAG thresholds, high-contrast (7:1), text scaling (XAG 101), timing adjustability (WCAG 2.2.1), and per-element color choice — deferred to the `cvdSafe` follow-up.
- Console/mobile, localization beyond PT/EN, romance Elena.

---

*Promoted from assessment `soil-health-crop-quality` (verdict: go, 2026-11-01). Assessment artifacts: `.specify/assessments/soil-health-crop-quality/` (intake, research, problem, concept, decision). Briefing: `specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md`. Constitution: `.specify/memory/constitution.md` v2.0.0.*