---
description: "Task list for feature 014 — Soil Health → Crop Quality (SMAPI .NET 6 mod)"
---

# Tasks: Soil Health → Crop Quality (Epic: Tier Outcomes)

**Feature**: `014-soil-health-crop-quality` | **Branch**: `014-soil-health-crop-quality`
**Spec**: [`specs/014-soil-health-crop-quality/spec.md`](./spec.md) | **Plan**: [`plan.md`](./plan.md) | **Contracts**: [`contracts/i-harvest-quality.md`](./contracts/i-harvest-quality.md) | **Data model**: [`data-model.md`](./data-model.md) | **Quickstart**: [`quickstart.md`](./quickstart.md) | **Research**: [`research.md`](./research.md)

**Input Documents**: `plan.md`, `spec.md`, `data-model.md`, `contracts/i-harvest-quality.md`, `quickstart.md`, `research.md`, `checklists/` (5 files, all 163 items checked), `.specify/memory/constitution.md` (v2.0.0 — Principle VI multiplayer first-class).

**Prerequisites (blocking — must complete before yield activation)**:
- `SC-008` rollback verification (`spec-004` `CompostingBinService.CollectCompost` — hidden duplication bug at `outputCount > 1`) — **absolute blocker for yield activation** (`FR-002`/`FR-003`; `Dependencies` 136-137; `FR-005` 78; clarification 2026-11-01). Design/implementation permitted concurrently; activation blocked until green (`manual GamePath` + automated `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`).
- `GamePath` spike (Q2) — verify `Crop.harvest` / `Object.Quality` member names against the 1.6.3/1.6.14 binary before writing hooks. No hook code written until this gate passes.
- Gate 3 in-game SMAPI session (Q10) — capture 3 signals (observation + save diff + SMAPI log) with a second player for multiplayer verification (D1).

**Language / Framework**: .NET 6 / C# latest / SMAPI 4.x (`Pathoschild.Stardew.ModBuildConfig` 4.4.0) / xUnit + Moq / FsCheck (property-based) / `ThreadSafeGameLoopEventsStub` / `ModConstants` single source of truth.

**Tests**: **MANDATORY — TDD REQUIRED** (user directive: red-green-refactor; property-based wherever applicable; invariant tests; BLOCKING). Tests must be written FIRST and must FAIL (RED PHASE) before any implementation begins. Each user story's tests are independently testable. TDD test tasks (Phase 3) BLOCK all implementation tasks (Phase 4+).

**Organization**: Setup → Foundational → RED PHASE (all story tests, BLOCKING) → Implementation (band migration → CollectCompost fix → US1 → US2 → US3 → US4) → Verification / Integration (quickstart scenarios + Gate 3) → Polish. Tasks marked `[P]` are parallelizable on disjoint files; tasks sharing context reuse the same subagent. Verification tasks are the ONLY attestation of success.

**Path Conventions**: All file paths relative to repo root. Adjust based on `plan.md` structure.

**Deterministic hook**: After every file edit, `git add <file> && git commit` with a detailed summary (Process #6 / `.specify/scripts/atomic-commit-hook.sh`).

---

## Phase 1: Setup (Shared Infrastructure) [P]

**Purpose**: Project initialization; verify existing contracts, data model, constants, and prerequisites are aligned before any implementation. No user story work begins until this phase is complete.

- [ ] T001 Verify feature branch exists (`014-soil-health-crop-quality`) and `plan.md` / `spec.md` / `data-model.md` / `contracts/` / `quickstart.md` / `research.md` are present and consistent (`checklists/requirements.md` 16/16 passing; all 4 custom checklists 163/163 checked)
- [ ] T002 [P] Read `.specify/extensions.yml` — confirm no `before_tasks` hooks registered (`after_analyze` only: `atomic-commit-hook`); skip silently; note `after_tasks` has none
- [ ] T003 [P] Verify `.specify/memory/constitution.md` v2.0.0 compliance (Principle I DDD, II Security-First, III Async-Only, IV TDD, V YAGNI, VI Multiplayer First-Class) for feature design
- [ ] T004 [P] Verify `contracts/i-harvest-quality.md` matches `Domain/Interfaces/ISoilHealthService.cs`, `ICompostApplicationService.cs`, `IPlayerInventory.cs`, `IHostAtomicLockService.cs` (existing interfaces; zero premature abstractions per `Principle V`)
- [ ] T005 [P] Verify `data-model.md` entities (`SoilHealth`, `HealthCategory`, `HarvestOutcome`, `TierFeedback`, `CollectCompost` outcome) align with `Domain/Models/SoilHealthState.cs`, `Domain/Visualization/HealthCategory.cs`, `ModConstants.cs` references
- [ ] T006 [P] Confirm `SC-008` rollback verification (`spec-004`) concurrent prerequisite status (`CompostingBinServiceTests.cs` — `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState`) — does NOT block design/implementation but MUST pass before yield activation (`FR-005` 78; `Dependencies` 136-137)
- [x] T007 [P] Confirm `GamePath` spike (Q2) required before `FR-001`/`FR-002` activation — verify `Crop.harvest` / `Object.Quality` member names against 1.6.3/1.6.14 binary; no hook code written until gate passes (RESULT: confirmed — `Crop.harvest` is `public virtual bool harvest(int,int,HoeDirt,JunimoHarvester,bool)`; `Object.Quality` has get/set; `HoeDirt.dayUpdate`, `Crop.dayOfCurrentPhase/newDay/phaseDays` confirmed. Harmony required — see `spike-t007-gamepath.md`)
- [ ] T008 [P] Confirm `IPestStateProvider` interface location decision (`Domain/Interfaces/IPestStateProvider.cs`, optional, null-safe) — create only if pest system ships; otherwise leave as documented contract only (YAGNI, Principle V)

**Checkpoint**: Foundation verified — prerequisites acknowledged; contracts aligned; no implementation yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core constants, contracts, data-model alignment, and infrastructure updates that must complete before ANY user story implementation or RED-phase tests that depend on them. **No user story work (including tests) begins until this phase is complete.**

**Critical**: `SC-008` rollback verification is **absolute blocker for yield activation** (not for design/tests). `GamePath` spike is **absolute gate before hook code** (not before test writing).

- [ ] T009 Verify `ModConstants.cs` is the single source of truth (`AGENTS.md` conventions) — add band edges (`PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`) and new constants (`SoilQualityHealthyBoost = 0.08f`, `SoilExtraHarvestHealthyBoost = 0.10f`); verify `PestQualityPenalty` (future) is documented as a placeholder only (do not add constant until pest spec ships — YAGNI)
- [ ] T010 [P] Update `LivingRoots/ModConstants.cs` with soil-quality constants (quote constraints verbatim from `spec.md` / `data-model.md`): `SoilQualityHealthyBoost = 0.08f`; `SoilExtraHarvestHealthyBoost = 0.10f`; band edges `PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`; all labeled "first-pass, tunable" (`spec.md` Q6; `data-model.md` 21-25)
- [ ] T011 [P] Verify `HealthCategory` enum (`Domain/Visualization/HealthCategory.cs`) intervals are `Poor [0,31)`, `Moderate [31,71)`, `Healthy [71,100]`, `Unknown` (NaN/Inf); intervals documented in XML doc comments (`FR-013`)
- [ ] T012 [P] Verify `ColorInterpolationService.cs` (`Services/Visualization/`) uses 31/71 edges and returns configured unknown color for NaN/Inf (`FR-013`; `internal-c3`)
- [ ] T013 [P] Verify `SoilHealthService.cs` applies `HealthValue_ClampedToMinMax` with `NaN`/`±Inf` guards (NaN sentinel for missing records → `Unknown`); sparse persistence (no zero values stored) (`FR-007`; `AGENTS.md` anti-pattern)
- [ ] T014 [P] Verify `HostAtomicLockService` (`Domain/Interfaces/IHostAtomicLockService.cs`, `Services/HostAtomicLockService.cs`) available for per-tile guard (`FR-006`; `Principle VI`)
- [ ] T015 [P] Verify `VisualizationService.cs` (`Services/Visualization/VisualizationService.cs` 247-285) floating-text / `TemporaryAnimatedSprite` pattern exists for harvest feedback reuse (`A4`; `FR-006`)
- [ ] T016 [P] Verify `CompostApplicationService.cs` (82-97) and `MulchApplicationService.cs` (293-309) world-space sprite patterns exist as templates for harvest feedback
- [ ] T017 Design `Crop.harvest` intercept strategy (documentation only — no code): public-field preference / Harmony fallback; member names verified against 1.6.3/1.6.14 binary via `GamePath` build; reuse `VisualizationService` floating-text; zero new `Assets/*.png`; zero new `i18n` key (`FR-008`, `Non-Goals`); no `ContentPatcher`; no `Data/Crops` edit (`spec.md` 122-134; `contracts/i-harvest-quality.md` §1)
- [ ] T018 Design inventory-full debris contract (documentation only): main item vanilla-blocked (red message, crop stays); extra item dropped via `Game1.createItemDebris` (vanilla); debris stays until `addItemToInventoryBool` succeeds on pickup; persists via location's normal debris sync; stacks per vanilla rules; no mod override; no duplication, no retryable state (`research-c1`; `contracts/i-harvest-quality.md` §5)

**Checkpoint**: Foundational ready — constants updated, contracts verified, strategies documented. RED PHASE (Phase 3) may now begin. Implementation (Phase 4+) remains blocked until Phase 3 tests are written and FAILING.

---

## Phase 3: RED PHASE — Test Tasks (BLOCKING)

**Purpose**: Write all tests FIRST. They MUST FAIL (red) before any implementation. This phase BLOCKS Phase 4+ (implementation). Use property-based testing (FsCheck) wherever applicable; test invariants as much as possible. One test file per subject (`{Subject}Tests.cs`). Tests are mandatory (TDD REQUIRED per user directive and `Principle IV`).

**CRITICAL**: No implementation task in Phase 4+ may begin until every task in this phase is complete AND the tests are confirmed failing (RED). Verification (Phase 6) attests success — never the agent itself.

### 3.1 Band-scale & category tests (affects existing visualization)

- [ ] T019 [P] Update `LivingRoots.Tests/Visualization/HealthCategoryTests.cs` — intervals `[0,31)` / `[31,71)` / `[71,100]`; `Unknown` for NaN/Inf; finite out-of-range clamps to boundary category; property-based (FsCheck) over float domain including NaN, ±Inf, subnormals, negatives, >100
- [ ] T020 [P] Update `LivingRoots.Tests/Visualization/ColorInterpolationServiceTests.cs` — 31/71 edges; unknown color for NaN/Inf; finite out-of-range clamps
- [ ] T021 [P] Update `LivingRoots.Tests/Controllers/ModControllerTests.cs` — growth modifier uses 31/71 band edges; measured `0` → `SoilGrowthPoorMultiplier` (A7); NaN/Inf skip (vanilla); negative skip
- [ ] T022 [P] Update `LivingRoots.Tests/Services/SoilHealthServiceTests.cs` — band edges; NaN sentinel for missing records; `SaveData_ZeroValue_NotPersisted` (explicit `0` runtime-only, does not survive save/load per A7)

### 3.2 CollectCompost fix tests (prerequisite)

- [ ] T023 [P] Update `LivingRoots.Tests/Services/CompostingBinServiceTests.cs` — `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` (existing, alias `StillConsumesBin`); add two-phase commit tests: dry-run capacity check, compensating rollback on partial failure, `OutputGranted` idempotency flag prevents duplicate grant on retry; property-based over `outputCount` values

### 3.3 Harvest-quality tests (US1, US2, US3, US4)

- [ ] T024 [P] [US2] Create `LivingRoots.Tests/HarvestQuality/PoorTierHarvestQualityTests.cs` — `PoorTier_HarvestQualityIsAlwaysZero` (any value in `[0,31)` → `Quality == 0`), property-based over health values and Farming levels/fertilizer num2 1/2/3; override permitted only for Poor tier
- [ ] T025 [P] [US3] Create `LivingRoots.Tests/HarvestQuality/ModerateTierHarvestQualityTests.cs` — `ModerateTier_QualityMatchesVanilla` (distribution statistically indistinguishable from vanilla for same seed/day/fertilizer within ±5% relative or ±0.01 absolute); byte-identical regression control (`SC-003`)
- [ ] T026 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/HealthyTierHarvestQualityTests.cs` — enhanced quality probability (silver/gold/iridium) greater than Medium over N harvests; `+0.08` additive on `p`; extra-harvest items remain normal quality (0) (A2); property-based over health `[71,100]`
- [ ] T027 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/ExtraHarvestTests.cs` — `ExtraHarvestHealthyBoost` adds `+0.10` to `ExtraHarvestChance`; host-authoritative roll; per-tile (not global per seed); extra items normal quality (0); `SC-009`
- [ ] T028 [P] [US4] Create `LivingRoots.Tests/HarvestQuality/HarvestFeedbackTests.cs` — `LivingTier_EnhancedQuality_ShowsLivingSoilText`, `LivingTier_ExtraHarvest_ShowsExtraHarvestText`, `LivingTier_Both_ShowsBothTexts`, `LivingTier_NormalQualityNoExtra_NoFeedback`; Poor shows `marker-poor` + "Soil too poor for quality crops" + `cancel`; Moderate shows none; transient appears/fades ≤1000 ms; no 1000-tile degradation; ≤16.67 ms/frame
- [ ] T029 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/HostAuthoritativeRollTests.cs` — `SC-004`: simulated second player; no client-side roll; client re-syncs on next `DayStarted` (`SC-010`)
- [ ] T030 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/InventoryFullHarvestTests.cs` — main item vanilla-blocked on full inventory (crop stays, red message); extra item dropped as debris; no duplication; no retryable state; `createItemDebris` vanilla semantics
- [ ] T031 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/UnknownTierHarvestTests.cs` — NaN/Inf → `HealthCategory.Unknown` → no modification (vanilla roll, no forced 0, no +0.08, no feedback); finite out-of-range clamps to boundary category; measured 0 → Poor (A7)
- [ ] T032 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/CropScopeHarvestTests.cs` — forage crops excluded (`crop.forageCrop == true` → early return, no mod effect, no feedback); fibre seeds and Qi Fruit skipped; flowers, multi-harvest, paddy, regrowing in scope (A6)
- [ ] T033 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/RegrowthHarvestTests.cs` — quality re-rolls on each harvest; tier applies independently per harvest
- [ ] T034 [P] [US1] Create `LivingRoots.Tests/HarvestQuality/FeedbackVisibilityMultiplayerTests.cs` — hybrid visibility: shared farm-state outcomes broadcast via `broadcastSprites` + `location.playSound`; triggering player's own input (hoe flash/text) local-only via `SpriteBatch`; state-derived rendering (overlay/tooltip) not a sprite; single-player degenerate case

### 3.4 Multiplayer / contract tests

- [ ] T035 [P] Create `LivingRoots.Tests/Contracts/HarvestQualityContractTests.cs` — contract assertions for `i-harvest-quality.md`: tier rules, extra-harvest normal quality, inventory-full debris, feedback conditionality/co-occurrence, visibility hybrid, crop scope guard, band 31/71

### 3.5 RED PHASE VERIFICATION (BLOCKING GATE)

- [ ] T036 **BLOCKING GATE** — Run `dotnet test --filter "FullyQualifiedName~HarvestQuality|FullyQualifiedName~HealthCategory|FullyQualifiedName~ColorInterpolation|FullyQualifiedName~CompostingBinService"` and confirm ALL new/updated tests FAIL (RED). Record the failure output (test names + failure reasons) in `specs/014-soil-health-crop-quality/red-phase-evidence.md`. **No implementation task in Phase 4+ may begin until this gate passes.** If any test passes unexpectedly, investigate (likely over-specified or already-implemented code) before proceeding.

**Checkpoint (RED PHASE)**: All tests written and confirmed failing. Implementation (Phase 4+) is now unblocked. Tests are the sole specification of correctness; implementation must make them pass without weakening them.

---

## Phase 4: Implementation

**Purpose**: Make the RED-phase tests pass. Organized by dependency order: band migration first (touches existing code), CollectCompost fix (prerequisite), then user stories in priority order (US2 Poor → US1 Living → US3 Medium verification → US4 feedback). Implementation may run in parallel only on disjoint files (marked [P]); tasks sharing context reuse the same subagent.

**CRITICAL**: Implementation does NOT begin until Phase 3 (RED PHASE) is complete and verified failing.

### 4.1 Band-scale migration (touches existing files) [P-group]

- [ ] T037 Update `LivingRoots/Constants.cs` — `PoorModerateBandEdge = 31f`, `ModerateHealthyBandEdge = 71f`, `ModerateBandWidth = 40f`; add `SoilQualityHealthyBoost = 0.08f`, `SoilExtraHarvestHealthyBoost = 0.10f` (labeled "first-pass, tunable"); remove/replace any 34/67 references (keep migration history only)
- [ ] T038 [P] Update `LivingRoots/Domain/Visualization/HealthCategory.cs` — intervals `[0,31)` / `[31,71)` / `[71,100]` / `Unknown` (NaN/Inf); XML doc comments updated
- [ ] T039 [P] Update `LivingRoots/Services/Visualization/ColorInterpolationService.cs` — 31/71 edges; unknown color for NaN/Inf; finite out-of-range clamps to boundary category
- [ ] T040 Update `LivingRoots/Controllers/ModController.cs` growth modifier — 31/71 band edges; measured `0` → `SoilGrowthPoorMultiplier` (A7, fix `health <= 0f` skip → skip only non-finite and negative); NaN/Inf skip (vanilla)
- [ ] T041 [P] Update `LivingRoots.Tests/...` (already done in Phase 3) — verify no 34/67 references remain in code/tests (except migration history) (`SC-006`)

### 4.2 CollectCompost fix (prerequisite) [P-group]

- [ ] T042 Update `LivingRoots/Services/CompostingBinService.cs` `CollectCompost` — two-phase commit: dry-run capacity check + compensating rollback + idempotency flag `OutputGranted`; retain `Ready` state on inventory refusal; red message `message.harvest.inventory-full` (Q4; `FR-005`)
- [ ] T043 [P] Update `LivingRoots/Services/CompostingBinService.cs` — add `OutputGranted` idempotency flag to `CompostingBinStateModel`/data as needed (minimal change; YAGNI)

### 4.3 User Story 2 — Poor Soil Yields No Stars (P1) [US2]

**Goal**: Poor soil (0–30%) forces `Quality = 0` at harvest; visible feedback; tangible consequence of neglect.

**Independent Test**: Plant same seed on Poor (0%) and Medium (50%) tiles; harvest both; confirm Poor item always normal quality (0 stars), Medium follows vanilla roll.

- [ ] T044 [US2] Implement harvest hook (public-field preference; Harmony fallback only if needed): intercept `Crop.harvest` after vanilla quality roll; when `HealthCategory.Poor` and `!crop.forageCrop`, set `Object.Quality = 0` (`FR-001`, Q2/Q3; `contracts/i-harvest-quality.md` §1, §3)
- [ ] T045 [US2] Implement transient feedback for Poor: `marker-poor` sprite (16×16 procedural) + "Soil too poor for quality crops" text + `cancel` SFX, event-driven via `TemporaryAnimatedSprite` + `Display.Rendered` (`FR-006`, `FR-008`)
- [ ] T046 [US2] Implement host-authority guard for the force-0 (replicated deterministic: triggering client reads synced soil health; host takes same local path) (`FR-009`, A8)
- [ ] T047 [US2] Implement inventory-full handling for Poor harvests: main item vanilla-blocked (red message, crop stays); no extra-harvest boost (Poor has none) (`contracts/i-harvest-quality.md` §5)

### 4.4 User Story 1 — Living Soil Pays Off at Harvest (P1) [US1]

**Goal**: Living soil (71–100%) yields visibly better harvests (silver/gold/iridium + chance of extra harvest) plus transient feedback.

**Independent Test**: Plant same seed on Poor (0%) and Living (≥71%) tiles; harvest both; confirm Living produces higher-quality items and occasionally an extra harvest, Poor produces no-star items.

- [ ] T048 [US1] Implement harvest hook quality boost: when `HealthCategory.Healthy` and `!crop.forageCrop`, add `SoilQualityHealthyBoost = 0.08` to vanilla quality probability roll `p` for the primary item only (`FR-001`, `FR-003`; `contracts/i-harvest-quality.md` §3, §4)
- [ ] T049 [US1] Implement extra-harvest boost: when `HealthCategory.Healthy`, add `SoilExtraHarvestHealthyBoost = 0.10` to `Data/Crops.ExtraHarvestChance` (per-tile at harvest time; never globally per seed) (`FR-002`)
- [ ] T050 [US1] Implement host-authoritative extra-harvest roll (replicated deterministic; SV single-harvest guarantee ensures roll fires once) (`FR-009`, A8)
- [ ] T051 [US1] Implement inventory-full handling for Living harvests: main item vanilla-blocked on full inventory; extra item dropped as debris via `Game1.createItemDebris` (vanilla semantics) (`contracts/i-harvest-quality.md` §5; `research-c1`)
- [ ] T052 [US1] Implement crop-scope guard: early return when `crop.forageCrop` is true; skip fibre seeds and Qi Fruit (`contracts/i-harvest-quality.md` §3; A6)
- [ ] T053 [US1] Implement transient feedback for Living: `sparkle-living` sprite + "Living Soil" text (only when enhanced quality) and/or "Extra harvest!" text (when extra item produced) + reward SFX; both as two sprites broadcast together when both hold; hybrid visibility (broadcast shared outcomes; local-only input) (`FR-006`, `FR-007`, `FR-008`; B1, B2; `contracts/i-harvest-quality.md` §6)
- [ ] T054 [US1] Implement pest clamp read (optional): if `IPestStateProvider` is non-null, clamp quality by `PestQualityPenalty` at harvest (one-directional) (`contracts/i-harvest-quality.md` §7; A5) — guard with null check; no hard dependency

### 4.5 User Story 3 — Medium Soil Is Vanilla (P1) [US3]

**Goal**: Medium soil (31–70%) harvests are byte-identical to vanilla (regression control).

**Independent Test**: Plant same seed on Medium (50%) and unmodded tile; harvest both; confirm quality distributions statistically indistinguishable (within ±5% relative or ±0.01 absolute).

- [ ] T055 [US3] Verify Medium tier leaves `Quality` untouched (vanilla by omission) — no code path modifies `Quality` for Moderate; ensure no accidental modifier application (`FR-001`, `FR-003`, `SC-003`)
- [ ] T056 [US3] Verify Medium tier applies no extra-harvest boost and no feedback (silence = vanilla) (`FR-002`, `FR-006`)
- [ ] T057 [US3] Verify all vanilla modifiers (Farming level, fertilizer, `IsPaddyCrop`, `RegrowDays`, `HarvestMaxIncreasePerFarmingLevel`) apply unchanged on Moderate tiles (`FR-003`)

### 4.6 User Story 4 — Transient Tier Feedback at Harvest (P2) [US4]

**Goal**: Brief, non-intrusive visual feedback when a tier affects harvest; no persistent overlay.

**Independent Test**: Harvest on Poor and Living tiles; confirm transient floating text + SFX appear and disappear within ≤1000 ms; no persistent per-tile marker.

- [ ] T058 [US4] Implement `marker-poor` procedural sprite (16×16 `Texture2D` + `SetData`, singleton per `VisualizationTextures`) (`FR-005`, `FR-006`)
- [ ] T059 [US4] Implement `sparkle-living` procedural sprite (16×16) (`FR-005`, `FR-006`)
- [ ] T060 [US4] Implement event-driven render path: `TemporarySprites.Add` + `Display.Rendered` floating text; `alphaFade` 0.02; upward drift; ~1s duration; immune to 1000-tile degradation (`FR-008`, `SC-005`)
- [ ] T061 [US4] Implement hybrid visibility broadcast: shared farm-state outcomes via `Game1.multiplayer.broadcastSprites` + `location.playSound`; triggering player's own input (hoe flash/text) via `SpriteBatch` (local-only) (`contracts/i-harvest-quality.md` §6; clarify-feedback-visibility research)
- [ ] T062 [US4] Verify no persistent per-tile tier marker is created (no new per-frame draw call) (`FR-011`)

### 4.7 Verification of implementation (make tests pass)

- [ ] T063 Run `dotnet build -c Release` — must succeed with 0 errors
- [ ] T064 Run `dotnet test -c Release` — all RED-phase tests must now PASS (GREEN); record pass count
- [ ] T065 Run `dotnet format --verify-no-changes` — must be clean

**Checkpoint**: Implementation complete; tests green; build/format clean. Proceed to Phase 5 (verification).

---

## Phase 5: Verification & Integration

**Purpose**: Verification tasks attest to success and completeness. ONLY these tasks attest to the success and completeness of the implementation — never the agent itself. Run in order; each is a gate.

### 5.1 Quickstart scenarios (automated/manual)

- [ ] T066 Verify Scenario 1 (Poor → no stars) per `quickstart.md` §Scenario 1 — `PoorTier_HarvestQualityIsAlwaysZero` passes
- [ ] T067 Verify Scenario 2 (Living enhances quality) per `quickstart.md` §Scenario 2 — enhanced probability over N harvests; extra items normal quality
- [ ] T068 Verify Scenario 3 (Extra harvest) per `quickstart.md` §Scenario 3 — "Extra harvest!" text; both sprites when both hold; no feedback on normal-quality no-extra
- [ ] T069 Verify Scenario 4 (Inventory full) per `quickstart.md` §Scenario 4 — main blocked (red message, crop stays); extra dropped as debris
- [ ] T070 Verify Scenario 5 (Medium byte-identical to vanilla) per `quickstart.md` §Scenario 5 — `ModerateTier_QualityMatchesVanilla` passes
- [ ] T071 Verify Scenario 6 (NaN/Unknown → vanilla) per `quickstart.md` §Scenario 6 — `UnknownTier_QualityMatchesVanilla` passes
- [ ] T072 Verify Scenario 7 (Band scale 31/71) per `quickstart.md` §Scenario 7 — no 34/67 references in code/tests (except migration history)
- [ ] T073 Verify Scenario 8 (Host authority) per `quickstart.md` §Scenario 8 — `SC-004` passes with simulated second player
- [ ] T074 Verify Scenario 9 (Transient feedback immunity) per `quickstart.md` §Scenario 9 — feedback appears with >1000 visible tiles; ≤16.67 ms/frame

### 5.2 Gate 3 in-game (manual, blocking for "verified" status)

- [ ] T075 **BLOCKING GATE (manual)** — Run in-game SMAPI session (~30 min, rebuild stale dll) per `quickstart.md` §Scenario 10: capture 3 signals (observation notes + save diff `modData/soil_health_data_{saveId}.json` with float tolerance 0.0001f + SMAPI log) for the growth+quality loop; include second player for multiplayer verification (D1). Record evidence in `specs/014-soil-health-crop-quality/gate3-evidence.md`. This is the ONLY attestation of "verified" status (per `VERIFICATION_PROTOCOL.md` / Gate 3 contract). Agent itself does NOT attest success.

### 5.3 Verification of all success criteria

- [ ] T076 Verify `SC-001` — fresh save with 3 tiles at 0%, ~50%, ~90% yields measurably different quality distributions (Poor always 0 stars; Living produces silver/gold/iridium more often than Medium)
- [ ] T077 Verify `SC-002` — `PoorTier_HarvestQualityIsAlwaysZero` and `ModerateTier_QualityMatchesVanilla` pass
- [ ] T078 Verify `SC-003` — Medium byte-identical to vanilla (within ±5% relative or ±0.01 absolute)
- [ ] T079 Verify `SC-004` — host-authoritative roll tests pass with simulated second player
- [ ] T080 Verify `SC-005` — transient feedback appears/fades ≤1000 ms; no 1000-tile degradation; ≤16.67 ms/frame
- [ ] T081 Verify `SC-006` — band scale 31/71 consistent across code/tests/docs; no 34/67 (except migration history)
- [ ] T082 Verify `SC-007` — `dotnet build -c Release` + `dotnet test --no-build -c Release` pass with 0 failures; `dotnet format --verify-no-changes` clean
- [ ] T083 Verify `SC-009` — extra-harvest count on Living measurably greater than Medium over N harvests
- [ ] T084 Verify `SC-010` — client re-syncs on next `DayStarted` after divergence; observable convergence

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and final validation. No new functionality.

- [ ] T085 [P] Update `README.md` only if needed (do not modify the 3 `"(Planned)"` strings asserted by `FeatureStatusTests` without updating the test) (`FR-015`)
- [ ] T086 [P] Verify `ProductionSourceGuardTests.NoUnreferencedConstantsSurvive` — every new constant has ≥1 production reference (`FR-016`)
- [ ] T087 [P] Verify no new `Assets/*.png` created; feedback sprites procedural (`FR-005`)
- [ ] T088 [P] Verify no new `i18n` keys for feedback text (literal text reused) (`spec 005 A4`; `FR-006`)
- [ ] T089 [P] Verify no `Data/Crops`/`Data/Objects` edits (`FR-004`)
- [ ] T090 [P] Verify no persistent per-tile tier marker created (`FR-011`)
- [ ] T091 [P] Update `ROADMAP.md` / `GDD` wording if needed ("enables additional agroecological practices (see separate spec)" for "new interactions")
- [ ] T092 Final `dotnet format --verify-no-changes` + `dotnet build -c Release` + `dotnet test -c Release` (all must pass)
- [ ] T093 Commit all changes with atomic conventional commits per Process #6 (one commit per file edit, detailed summary)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (RED PHASE)**: Depends on Phase 2 — BLOCKS Phase 4+ (TDD: tests written first, must FAIL before implementation)
- **Phase 4 (Implementation)**: Depends on Phase 3 (tests written & red) — band migration + CollectCompost first, then US2 → US1 → US3 → US4
- **Phase 5 (Verification)**: Depends on Phase 4 (implementation complete, tests green)
- **Phase 6 (Polish)**: Depends on Phase 5 (verification green)

### User Story Dependencies

- **US2 (Poor no stars)**: Independent — can start after Foundational + RED PHASE
- **US1 (Living pays off)**: Independent — can start after Foundational + RED PHASE (shares harvest hook with US2; coordinate via same subagent)
- **US3 (Medium vanilla)**: Independent — verification-only phase (no new code; asserts absence of modification)
- **US4 (Transient feedback)**: Depends on US1/US2 feedback triggers (sprites/text/SFX) — implement after US1/US2 harvest hook exists

### Within Each User Story

- Tests (Phase 3) → Implementation (Phase 4) → Verification (Phase 5)
- Models → Services → Integration
- Core implementation before integration

### Parallel Opportunities

- Phase 1: T002–T008 all `[P]` (disjoint files/verification)
- Phase 2: T010–T016 all `[P]` (disjoint files)
- Phase 3: T019–T035 all `[P]` (disjoint test files) — but ALL must complete and FAIL (RED) before Phase 4
- Phase 4.1: T038, T039, T041 `[P]` (disjoint files; T037/T040 share `ModController`/`Constants` — sequential)
- Phase 4.2: T042, T043 `[P]` (disjoint)
- Phase 4.3–4.4: US2 and US1 share the harvest hook in `ModController` — use the SAME subagent (shared context); do not parallelize across different subagents
- Phase 4.5: US3 is verification-only; can run in parallel with US4 once US1/US2 hook exists
- Phase 4.6: T058–T062 `[P]` (disjoint sprite/render files)
- Phase 6: T085–T091 all `[P]`

### Parallel Example: RED PHASE (Phase 3)

```bash
# Launch all RED-phase test tasks in parallel (disjoint test files):
Task: "T019 Update HealthCategoryTests.cs — 31/71 intervals + property-based float domain"
Task: "T020 Update ColorInterpolationServiceTests.cs — 31/71 edges"
Task: "T021 Update ModControllerTests.cs — growth modifier 31/71 + measured 0"
Task: "T022 Update SoilHealthServiceTests.cs — band edges + NaN sentinel"
Task: "T023 Update CompostingBinServiceTests.cs — CollectCompost two-phase"
Task: "T024 [US2] Create PoorTierHarvestQualityTests.cs"
Task: "T025 [US3] Create ModerateTierHarvestQualityTests.cs"
Task: "T026 [US1] Create HealthyTierHarvestQualityTests.cs"
Task: "T027 [US1] Create ExtraHarvestTests.cs"
Task: "T028 [US4] Create HarvestFeedbackTests.cs"
Task: "T029 [US1] Create HostAuthoritativeRollTests.cs"
Task: "T030 [US1] Create InventoryFullHarvestTests.cs"
Task: "T031 [US1] Create UnknownTierHarvestTests.cs"
Task: "T032 [US1] Create CropScopeHarvestTests.cs"
Task: "T033 [US1] Create RegrowthHarvestTests.cs"
Task: "T034 [US1] Create FeedbackVisibilityMultiplayerTests.cs"
Task: "T035 Create HarvestQualityContractTests.cs"
# Then T036 BLOCKING GATE: run tests, confirm all FAIL (RED), record evidence
```

### Parallel Example: Implementation (Phase 4.1 band migration)

```bash
# After T037 (Constants) completes:
Task: "T038 [P] Update HealthCategory.cs — 31/71 intervals"
Task: "T039 [P] Update ColorInterpolationService.cs — 31/71 edges"
Task: "T041 [P] Verify no 34/67 references remain"
# T040 (ModController growth modifier) shares ModController with US2/US1 harvest hook — use same subagent, sequential
```

---

## Implementation Strategy

### MVP First (User Story 2 — Poor "no stars")

1. Complete Phase 1 (Setup) + Phase 2 (Foundational)
2. Complete Phase 3 (RED PHASE) — ALL tests written and failing
3. Complete Phase 4.1 (band migration) + Phase 4.2 (CollectCompost fix)
4. Complete Phase 4.3 (US2 Poor no stars) — first tangible payoff
5. **STOP and VALIDATE**: Run US2 tests; verify Poor → Quality = 0; manual smoke test
6. Deploy/demo if ready (Poor "cost of inaction" is independently valuable)

### Incremental Delivery

1. Setup + Foundational + RED PHASE → foundation ready (tests red)
2. Add US2 (Poor no stars) → Test independently → Deploy/Demo (MVP: cost of inaction visible)
3. Add US1 (Living pays off) → Test independently → Deploy/Demo (full payoff loop closed)
4. Add US3 (Medium vanilla) → Test independently → Deploy/Demo (regression control verified)
5. Add US4 (Transient feedback) → Test independently → Deploy/Demo (observability complete)
6. Verification (Phase 5) + Polish (Phase 6) → release-ready (pending Gate 3 in-game)

### Parallel Team Strategy

With multiple developers/subagents:
1. Team completes Setup + Foundational + RED PHASE together (RED PHASE is blocking — all tests must be written first)
2. Once RED PHASE is green (tests written & failing):
   - Developer A: band migration (T037–T041) + CollectCompost fix (T042–T043)
   - Developer B: US2 (T044–T047) — shares harvest hook subagent with US1
   - Developer C: US4 sprites (T058–T062) — disjoint files, parallel
   - Developer D: US3 verification (T055–T057) — verification-only, parallel after hook exists
3. Stories integrate at Phase 5 (verification) — `ModController` harvest hook is the shared integration point; coordinate via same subagent to avoid conflicts

---

## Notes

- `[P]` tasks = different files, no dependencies on incomplete tasks; safe to parallelize via subagent driven development in waves
- Tasks sharing context (especially `ModController` harvest hook) reuse the same subagent — do NOT parallelize across different subagents on shared files
- `[Story]` label maps task to specific user story for traceability (US1, US2, US3, US4)
- Each user story is independently completable and testable
- TDD is mandatory: RED PHASE (Phase 3) blocks implementation (Phase 4+); tests must FAIL before implementation
- Property-based testing (FsCheck) used wherever applicable (invariants over float domain, health values, Farming levels, fertilizer tiers)
- Verification tasks (Phase 5) are the ONLY attestation of success — never the agent itself
- Deterministic hook (Process #6): after every file edit, `git add <file> && git commit` with detailed summary
- Commit after each task or logical group; use atomic conventional commits
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same-file conflicts, cross-story dependencies that break independence
- The `GamePath` spike and `SC-008` rollback verification are external prerequisites that gate yield *activation* — they do not block test writing or implementation, only final "verified" status
- `tasks.md` is immediately executable; each task is specific enough for an LLM to complete without additional context

## Phase 7: Convergence (appended 2026-11-03)

- [ ] T094 Fix `CollectCompost` two-phase commit + idempotency (`OutputGranted`) + rollback on partial inventory refusal — `Services/CompostingBinService.cs` (partial, FR-005/Q4; `Contracts` §5)
- [x] T095 Fix growth modifier to apply `SoilGrowthPoorMultiplier` to measured `0` (skip only negative/non-finite) and update stale comment — `Controllers/ModController.cs` (contradicts, A7)
- [ ] T096 Replace heuristic `Quality++` in harvest patch with deterministic `+0.08` on vanilla `p` using `BoostQualityProbability` — `Patches/HarvestPatches.cs` (partial, FR-001/FR-003)
- [ ] T097 Set extra-harvest item `Quality = 0` (normal) instead of inheriting source quality — `Patches/HarvestPatches.cs` (partial, A2/FR-002)
- [ ] T098 Wire actual transient feedback sprites (`marker-poor`, `sparkle-living`) + floating text + SFX + broadcast via `Game1.multiplayer.broadcastSprites`, not just logs — `Patches/HarvestPatches.cs`, `Services/Visualization/` (partial, FR-006/FR-007/FR-008)
- [ ] T099 Add regrowth handling: quality re-rolls per harvest, tier applies independently — `Patches/HarvestPatches.cs` (missing, edge case)
- [ ] T100 Implement inventory-full debris handling verification (main blocked, extra dropped) — `Patches/HarvestPatches.cs` (partial, A1)
- [ ] T101 Make harvest roll host-authoritative (host-only path; farmhand reads synced soil health) — `Patches/HarvestPatches.cs`, `ModEntry.cs` (partial, FR-009/A8)
- [ ] T102 Add missing RED test files: `HostAuthoritativeRollTests`, `InventoryFullHarvestTests`, `CropScopeHarvestTests`, `RegrowthHarvestTests`, `HarvestFeedbackTests`, `FeedbackVisibilityMultiplayerTests`, `HarvestQualityContractTests` — `LivingRoots.Tests/HarvestQuality/`, `LivingRoots.Tests/Contracts/` (missing, FR-014)
- [ ] T103 Update `ModControllerTests` and `SoilHealthServiceTests` to 31/71 band edges (if not already) — `LivingRoots.Tests/` (partial, SC-006)
- [ ] T104 Run Gate 3 in-game session and record 3 signals — manual (missing, SC-008)
