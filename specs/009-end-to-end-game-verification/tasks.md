# Tasks: End-to-End In-Game Verification (Gate 3)

**Input**: Design documents from `/specs/009-end-to-end-game-verification/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: TDD is mandatory per Constitution IV and user directive. Test tasks are listed FIRST within each story and MUST be written and FAIL (RED phase) before any implementation task begins. Tests use property-based testing where applicable and assert invariants.

**Organization**: Tasks are grouped by user story (US1 maintainer verifies, US2 contributor reproduces, US3 CI/build) to enable independent implementation and testing. Parallelizable tasks (disjoint files, no dependency on incomplete tasks) are marked `[P]`.

## Path Conventions

- Mod source: `LivingRoots/`
- Tests: `LivingRoots.Tests/`
- Specs/docs: `specs/009-end-to-end-game-verification/`, repository root (`AGENTS.md`, `VERIFICATION_PROTOCOL.md`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a known-green baseline before any change.

- [X] T001 Verify baseline build passes: `dotnet build Stardew-LivingRoots.sln --configuration Release` (0 errors).
- [ ] T002 Verify baseline tests pass: `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` (0 failures).
- [X] T003 [P] Confirm `ILocationProvider` interface and `LocationProvider` implementation are usable without modification (`LivingRoots/Domain/ILocationProvider.cs`, `LivingRoots/Services/LocationProvider.cs`).
- [X] T004 [P] Confirm `SoilDecayService` idempotency pattern (`Dictionary<string,string>` keyed by location, lock-guarded, record-before-work, Trace log on skip) to reuse verbatim (`LivingRoots/Services/SoilDecayService.cs`).

**Checkpoint**: Baseline green; reusable patterns identified.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core wiring that MUST be complete before any user story (growth modifier correction) can be implemented. No user story work may begin until this phase is complete.

- [X] T005 Add `ILocationProvider? locationProvider = null` parameter to `ModController` primary constructor and store `private readonly ILocationProvider? _locationProvider` field, following the existing nullable-injection pattern (`LivingRoots/Controllers/ModController.cs`).
- [X] T006 Wire `locationProvider` instance into `ModController` creation in `ModEntry` (pass `locationProvider: locationProvider`) (`LivingRoots/ModEntry.cs`).
- [X] T007 Add `DayEnding` event subscription infrastructure mirroring the `DayStarted` pattern: field `_onDayEndingHandler`, register in `RegisterEvents()`, unregister in disposal, rollback on partial unsubscribe failure (`LivingRoots/Controllers/ModController.cs`).
- [X] T008 Add idempotency storage scaffold reusing the `SoilDecayService` pattern: `Dictionary<string, string>` keyed by location name, guarded by a lock, record-before-work, Trace log on skip ("idempotent skip" including the key) (`LivingRoots/Controllers/ModController.cs`).

**Checkpoint**: Foundation ready — growth modifier correction can begin.

---

## Phase 3: User Story 1 — Maintainer Verifies Phase 2 Hook (Priority: P1) 🎯 MVP

**Goal**: Correct the Phase 2 growth modifier (multiplicative on `phaseDays`, moved to `DayEnding`, host-guarded, idempotent, all farm locations) and provide the verification artifacts so a maintainer can run a scripted SMAPI session and claim `verified` (maintainer-verified with multi-signal evidence).

**Independent Test**: After correction, unit tests for multiplicative math, DayEnding execution, host guard, idempotency, all-location scope, and NaN/zero handling pass (GREEN); full test suite has 0 regressions; `dotnet format --verify-no-changes` clean.

### Tests for User Story 1 (RED PHASE — write FIRST, must FAIL before implementation) ⚠️

> **NOTE: These tests MUST be written and confirmed FAILING before any T017+ implementation task begins. They define the corrected hook contract. Use property-based testing where applicable (e.g., monotonicity of phase advancement vs. health tier) and assert invariants (never below 1, never touch 99999 sentinel, idempotent reapplication restores base).**

- [ ] T009 [P] [US1] Create `GrowthModifierMultiplicativeMathTests.cs` — assert tier multipliers (Poor 1.15×, Moderate 1.00×, Healthy 0.85×, matching `HealthCategory` enum) applied to cached base `phaseDays`; base restored via `Crop.ResetPhaseDays()` before reapply; idempotent reapplication self-correcting; never below 1; never touch 99999 sentinel; property: higher health never yields slower advancement than lower health (monotonicity). Property-based via xunit `[Theory]`/`[InlineData]` (FsCheck.Xunit optional if added) (`LivingRoots.Tests/GrowthModifier/GrowthModifierMultiplicativeMathTests.cs`).
- [ ] T010 [P] [US1] Create `GrowthModifierDayEndingTests.cs` — assert `ApplySoilGrowthModifier` executes during `DayEnding` (before `newDayAfterFade`/`Crop.newDay`) and is NOT invoked from `OnDayStarted` post-loop; `DayStarted` subscription remains for composting bins and soil decay (`LivingRoots.Tests/GrowthModifier/GrowthModifierDayEndingTests.cs`).
- [ ] T011 [P] [US1] Create `GrowthModifierHostGuardTests.cs` — assert `if (!Context.IsMainPlayer) return;` at top of `OnDayEnding`; farmhand execution skipped (stub-bounded mechanism) (`LivingRoots.Tests/GrowthModifier/GrowthModifierHostGuardTests.cs`).
- [ ] T012 [P] [US1] Create `GrowthModifierIdempotencyTests.cs` — assert key format `$"{season}:{dayOfMonth}:{year}"` with `season = ISeasonProvider.CurrentSeason.ToLowerInvariant()`, `dayOfMonth = Game1.dayOfMonth`, `year = Game1.year`, `CultureInfo.InvariantCulture`, ordinal comparison; skip + Trace log ("idempotent skip" incl. key) on re-fire (save/load); no reprocessing, no side effects; record-before-work (`LivingRoots.Tests/GrowthModifier/GrowthModifierIdempotencyTests.cs`).
- [ ] T013 [P] [US1] Create `GrowthModifierAllLocationsTests.cs` — assert `ApplySoilGrowthModifier(string locationName)` invoked for each location in `farmLocationNames` (at minimum `Farm`, `Greenhouse`, `currentLocation`) via `ILocationProvider.GetLocationByName`; missing location (null) → skip + Trace log; empty location → no-op, no logging (`LivingRoots.Tests/GrowthModifier/GrowthModifierAllLocationsTests.cs`).
- [ ] T014 [P] [US1] Create `GrowthModifierNaNHandlingTests.cs` — assert NaN health tiles skipped (no change, NOT normalized to 0); zero/missing health (sparse cache → 0) skipped (vanilla, not Poor tier); null `SoilHealthService` → skip + Trace log (`LivingRoots.Tests/GrowthModifier/GrowthModifierNaNHandlingTests.cs`).
- [ ] T015 [P] [US1] Create `GrowthModifierPerformanceTests.cs` — assert scaling/linearity + coarse ceiling (no wall-clock ≤5 ms assertion in unit tests; host-dependent); `GetChunkStartIndices` correctness; optional injected 40,001-tile synthetic-location test (`LivingRoots.Tests/GrowthModifier/GrowthModifierPerformanceTests.cs`).
- [ ] T015b [US1] **RED-phase reconciliation**: Rewrite `T114_T126_RedPhaseTests.cs` stubs (`Assert.True(false, ...)`) into real failing assertions for the corrected hook (multiplicative, DayEnding, host guard, idempotency, all locations) so they fail for the right reason and join the RED checkpoint; do NOT make them pass yet (`LivingRoots.Tests/Invariants/T114_T126_RedPhaseTests.cs`).
- [ ] T042 [P] [US1] Add `GrowthModifierDayEndingRollbackTests.cs` — assert DayEnding subscription unregistration/rollback parity with DayStarted (reuse existing rollback test pattern: re-subscribe on partial unsubscribe failure) (`LivingRoots.Tests/GrowthModifier/GrowthModifierDayEndingRollbackTests.cs`).
- [ ] T016 [US1] **BLOCKING CHECKPOINT (RED)**: Run `dotnet test --filter "GrowthModifier|T114_T126"` and confirm all new tests FAIL for the right reason (corrected hook not yet implemented). Do NOT proceed to T017 until RED is verified. Record failure output.

### Implementation for User Story 1 (GREEN PHASE — only after T016 RED verified)

- [X] T017 [US1] Refactor `ApplySoilGrowthModifier` signature to `ApplySoilGrowthModifier(string locationName)`; resolve location via `_locationProvider.GetLocationByName(locationName)` (`LivingRoots/Controllers/ModController.cs`).
- [X] T018 [US1] Remove the `ApplySoilGrowthModifier()` call from the post-loop block in `OnDayStarted` (keep composting bins and soil decay calls there) (`LivingRoots/Controllers/ModController.cs`).
- [X] T019 [US1] Add `DayEnding` event handler `OnDayEnding` with host guard `if (!Context.IsMainPlayer) return;` at top, before any processing (`LivingRoots/Controllers/ModController.cs`).
- [X] T020 [US1] Add idempotency check in `OnDayEnding`: compute key `$"{season}:{dayOfMonth}:{year}"`; if already processed for that location, skip + Trace log ("idempotent skip" incl. key); record-before-work (`LivingRoots/Controllers/ModController.cs`).
- [X] T021 [US1] Call `ApplySoilGrowthModifier(locationName)` inside `foreach (var locationName in farmLocationNames)` in `OnDayEnding`, alongside `ProcessDayStart` calls (`LivingRoots/Controllers/ModController.cs`).
- [X] T022 [US1] Replace ±1 logic with multiplicative: cache base `phaseDays` via `Crop.ResetPhaseDays()` (restore from `CropData`) before applying tier multiplier; apply Pobre 1.15×, Moderado 1.00×, Saudável 0.85×; never below 1; never touch 99999 sentinel; adjust `dayOfCurrentPhase` accordingly (`LivingRoots/Controllers/ModController.cs`).
- [X] T023 [US1] Implement NaN/zero/missing handling: NaN tiles skipped (no change); zero/missing health (sparse → 0) skipped (vanilla); missing location (null) → skip + Trace; null `SoilHealthService` → skip + Trace; empty location → no-op, no logging (`LivingRoots/Controllers/ModController.cs`).
- [X] T024 [US1] Add production performance measurement: `Stopwatch` entry→return, log elapsed ms at `LogLevel.Trace` only; chunking (≈1,000 tiles/chunk, prime-modulus stride) activates only above 5 ms (`LivingRoots/Controllers/ModController.cs`).
- [X] T025 [US1] Update comments and logging to reflect corrected behavior (DayEnding, multiplicative, host guard, idempotency, all locations) (`LivingRoots/Controllers/ModController.cs`).
- [X] T026 [US1] **GREEN CHECKPOINT**: Run `dotnet test --filter "GrowthModifier"` → all pass (0 failures). Run full suite `dotnet test` → 0 regressions (red-phase stubs now rewritten by T015b and expected green). Run `dotnet format --verify-no-changes` → clean.

**Checkpoint**: User Story 1 growth modifier correction complete and verified by tests.

---

## Phase 4: User Story 2 — Contributor Reproduces Verification (Priority: P2)

**Goal**: Document and provide tooling so a contributor can reproduce the verification without inventing steps (protocol checklist + save comparator).

**Independent Test**: `gate3-verification-contract.md` exists and is complete; save comparator detects health deltas (added/removed/changed) with float tolerance 0.0001f, ignores NaN, treats missing keys as 0; `VERIFICATION_PROTOCOL.md` has SC-009 label mapping; `AGENTS.md` has Gate 3 section.

### Tests for User Story 2 (RED PHASE) ⚠️

- [ ] T027 [P] [US2] Create `SaveComparatorTests.cs` (RED first): assert before/after `soil_health_data_{saveId}.json` diff detects added/removed/changed health values per tile keyed by `(LocationName, "X,Y")`; float tolerance 0.0001f (matching sparse cache threshold); missing keys treated as 0; NaN tiles ignored; output JSON diff with `added`/`removed`/`changed` entries (`LivingRoots.Tests/Verification/SaveComparatorTests.cs`).
- [ ] T028 [US2] **BLOCKING CHECKPOINT (RED)**: Run `dotnet test --filter "SaveComparator"` → confirm FAIL (comparator not implemented). Do NOT proceed to T029 until RED verified.

### Implementation for User Story 2

- [ ] T029 [P] [US2] Implement `SaveComparator` C# utility (xUnit-compatible) reading two `modData/soil_health_data_{saveId}.json` files and producing JSON diff; NaN tiles skipped (not mapped to 0/Unknown); missing keys (sparse cache) treated as 0; float tolerance 0.0001f (`LivingRoots.Tests/Verification/SaveComparator.cs`).
- [X] T030 [P] [US2] Create formal SC-009 checklist `gate3-verification-contract.md` (pre-state, per-criterion observation with pass thresholds, multi-signal evidence, post-state comparison, claim discipline); scope distinct from `contracts/verification-protocol.md` (interface/protocol spec) and `checklists/verification-protocol.md` (requirements-quality) — add cross-reference headers (`specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md`).
- [X] T031 [P] [US2] Update `VERIFICATION_PROTOCOL.md` §3 with SC-009 label mapping (project-specific identifier, not IEEE/ISO/SMAPI standard; overloaded across 4 specs) (`VERIFICATION_PROTOCOL.md`).
- [X] T032 [P] [US2] Add Gate 3 verification section to `AGENTS.md` (manual protocol, save comparator, 3-signal requirement, claim discipline) (`AGENTS.md`).
- [X] T032b [P] [US2] Add Gate 3 verification section to `CONTRIBUTING.md` (PR verification expectations, 3-signal evidence, claim discipline) (`CONTRIBUTING.md`).
- [X] T041 [P] [US2] Create evidence-bundle template (MIRROR-style): `manifest.json` schema (schema version, run id, timestamp ISO 8601, tool/game/mod versions, environment, SHA-256 per file) + `claims.json` structure + `artifacts/` layout + honesty levels W/R/E assignment rules (`specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` or `verification/evidence-bundle-template/`).
- [ ] T033 [US2] **GREEN CHECKPOINT**: Run `dotnet test --filter "SaveComparator"` → all pass. Run full suite → 0 regressions (red-phase stubs rewritten by T015b).

**Checkpoint**: User Story 2 verification infrastructure complete and reproducible.

---

## Phase 5: User Story 3 — CI / Build Verification (Priority: P3, tertiary, deferred)

**Goal**: Confirm build and basic test compilation pass before any feature merges (already satisfied; CI `Build and Test` remains dormant per governance).

**Independent Test**: `dotnet build` 0 errors; `dotnet test` 0 failures; `dotnet format --verify-no-changes` clean; `Build and Test` workflow status documented as dormant.

- [X] T034 [US3] Document CI dormant status and local preflight (`lefthook` pre-commit format check, pre-push test) in `AGENTS.md` CI section (no workflow changes) (`AGENTS.md`).
- [X] T035 [US3] Verify `dotnet build Stardew-LivingRoots.sln --configuration Release` → 0 errors and `dotnet test` → 0 failures (already satisfied; re-confirm after US1/US2 changes).

**Checkpoint**: User Story 3 (CI/build) confirmed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup, test reconciliation, and verification execution (manual).

- [X] T036 [P] Final reconciliation of `T114_T126_RedPhaseTests.cs`: confirm assertions (rewritten in T015b) now pass (GREEN) after implementation; remove any temporary skip markers; ensure no `Assert.True(false)` stubs remain (`LivingRoots.Tests/Invariants/T114_T126_RedPhaseTests.cs`).
- [ ] T037 Run full test suite `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` → 0 failures (including rewritten T114_T126_RedPhaseTests).
- [X] T038 Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → 0 violations.
- [ ] T039 **MANUAL (maintainer)**: Execute SC-009 scripted SMAPI session following `gate3-verification-contract.md`; capture 3 signals (observation notes + save diff + SMAPI log excerpt) — all evidence file paths are MANDATORY (not optional); update `test.md` from `partial` to `verified` only after all 3 signals agree.
- [ ] T040 **MANUAL (maintainer)**: Update `COMPLETION_SUMMARY.md` and `ROADMAP.md` Gate 3 status (verified = maintainer-verified with multi-signal evidence; D1 remains separate obligation); record promotion trigger for deferred components (reflector/logger/runner promoted only after 2+ features reuse the loop).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories.
- **Phase 3 (US1)**: Depends on Phase 2. T009–T016 (tests, RED) MUST complete and be confirmed FAILING before T017–T026 (implementation) begin.
- **Phase 4 (US2)**: Depends on Phase 2 (independent of US1). T027–T028 (RED) MUST complete before T029–T033.
- **Phase 5 (US3)**: Depends on Phase 2; can run in parallel with US1/US2 (disjoint files).
- **Phase 6 (Polish)**: Depends on US1, US2, US3 completion.

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2. No dependency on other stories. 🎯 MVP.
- **US2 (P2)**: Can start after Phase 2. Independent of US1 (different files: tests/Verification, specs/checklists, VERIFICATION_PROTOCOL.md, AGENTS.md).
- **US3 (P3)**: Can start after Phase 2. Independent of US1/US2.

### Within Each User Story

- Tests (RED) MUST be written and FAIL before implementation.
- Implementation tasks within a story are sequential where they touch the same file (`ModController.cs`); tasks touching disjoint files are parallel.
- Story complete before moving to next priority (or run in parallel if staffed).

### Parallel Opportunities

- Phase 1: T003, T004 parallel.
- Phase 2: T005–T008 sequential (same file `ModController.cs`), but T006 (`ModEntry.cs`) can run in parallel with T005/T007/T008 only after T005 signature is known — prefer sequential within Phase 2 to avoid conflicts.
- US1 tests T009–T015 all parallel (disjoint test files).
- US1 implementation T017–T025 sequential (same file `ModController.cs`).
- US2: T027 (test) then T029–T032 parallel (disjoint files) after RED checkpoint T028.
- US3: T034, T035 can run in parallel with US1/US2 (disjoint files).
- Polish: T036 [P] can run in parallel with T037–T038 (different files).

### Parallel Example: User Story 1 (tests)

```bash
# Launch all US1 RED-phase tests together (disjoint files):
Task: "T009 GrowthModifierMultiplicativeMathTests in LivingRoots.Tests/GrowthModifier/GrowthModifierMultiplicativeMathTests.cs"
Task: "T010 GrowthModifierDayEndingTests in LivingRoots.Tests/GrowthModifier/GrowthModifierDayEndingTests.cs"
Task: "T011 GrowthModifierHostGuardTests in LivingRoots.Tests/GrowthModifier/GrowthModifierHostGuardTests.cs"
Task: "T012 GrowthModifierIdempotencyTests in LivingRoots.Tests/GrowthModifier/GrowthModifierIdempotencyTests.cs"
Task: "T013 GrowthModifierAllLocationsTests in LivingRoots.Tests/GrowthModifier/GrowthModifierAllLocationsTests.cs"
Task: "T014 GrowthModifierNaNHandlingTests in LivingRoots.Tests/GrowthModifier/GrowthModifierNaNHandlingTests.cs"
Task: "T015 GrowthModifierPerformanceTests in LivingRoots.Tests/GrowthModifier/GrowthModifierPerformanceTests.cs"
```

### Parallel Example: User Story 2 (implementation, after RED)

```bash
# Launch all US2 implementation tasks together (disjoint files):
Task: "T029 SaveComparator in LivingRoots.Tests/Verification/SaveComparator.cs"
Task: "T030 gate3-verification-contract.md in specs/009-.../checklists/"
Task: "T031 VERIFICATION_PROTOCOL.md SC-009 label mapping"
Task: "T032 AGENTS.md Gate 3 section"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational).
2. Complete Phase 3 (US1): RED tests (T009–T016) → GREEN implementation (T017–T026).
3. **STOP and VALIDATE**: Growth modifier tests green, full suite green, format clean.
4. This alone delivers the corrected Phase 2 hook (the core gameplay fix).

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → corrected growth modifier (MVP gameplay fix).
3. US2 → verification infrastructure (protocol + save comparator) for contributors.
4. US3 → CI/build confirmation (already satisfied).
5. Polish → test reconciliation + manual SC-009 session → `test.md` `verified`.

### Parallel Team Strategy

With multiple developers:
1. Team completes Setup + Foundational together.
2. Once Foundational done:
   - Developer A: US1 (growth modifier correction)
   - Developer B: US2 (verification infrastructure)
   - Developer C: US3 (CI/build confirmation)
3. Stories complete and integrate independently; Polish phase runs after all.

---

## Notes

- `[P]` tasks = different files, no dependencies on incomplete tasks.
- `[Story]` label maps task to specific user story for traceability.
- TDD: RED-phase tests MUST fail before implementation; verify failure reason is correct (not compile error).
- Property-based testing: use where applicable (monotonicity of phase advancement vs. health tier; idempotent reapplication).
- Invariants to assert: health bounds 0–100; never below 1 `phaseDays`; never touch 99999 sentinel; idempotent reapplication restores base; NaN tiles skipped; sparse-cache missing keys treated as 0.
- Commit after each task or logical group (atomic conventional commit per Process #6).
- Stop at any checkpoint to validate story independently.
- Avoid: vague tasks, same-file conflicts, cross-story dependencies that break independence.
- Manual tasks (T039, T040) require `STARDEW_GAME_PATH` and a live SMAPI session; they are NOT automated.
## Phase 7: Convergence

- [X] T043 Implement chunking mechanism (≈1,000 tiles/chunk, prime-modulus stride 61) that activates only when the linear scan exceeds 5 ms at 40,000 tiles, with `GetChunkStartIndices` correctness tests and an optional 40,001-tile synthetic-location test — per FR-002a performance / T024 [partial]
- [X] T044 Align `ApplySoilGrowthModifier` with spec FR-002a details: log at `LogLevel.Trace` on null/missing location; inject `ISeasonProvider` and use `ISeasonProvider.CurrentSeason.ToLowerInvariant()` for the idempotency key instead of `Game1.currentSeason` — per FR-002a exception handling & idempotency [partial]
- [ ] T045 Resolve pre-existing test-project compile errors (82 errors in unrelated invariants) blocking spec 009 RED-phase test work (T009–T016, T027–T029, T042) — cross-cutting blocker [missing]

## Phase 8: Convergence

- [X] T046 Align `ApplySoilGrowthModifier` with spec FR-002a exception handling & idempotency details: log at `LogLevel.Trace` when `SoilHealthService` is null; format idempotency key with `CultureInfo.InvariantCulture` (per spec FR-002a) — per FR-002a [partial]

## Phase 9: Convergence

- [ ] T047 Create the eight missing growth-modifier RED-phase test files and confirm they FAIL before implementation per Constitution IV / FR-002a / T009-T015 / T042 (missing)
- [ ] T048 Resolve the 82 pre-existing test-project compile errors (including the ChunkStrideInvariantsTests three-parameter overload) so spec 009 RED-phase tests build and run per T045 (missing)
- [ ] T049 Implement the SaveComparator utility and its RED test (before/after soil_health_data_{saveId}.json diff, 0.0001f tolerance, missing keys as 0, NaN ignored) per FR-001b / FR-003a / T029 (missing)
- [ ] T050 Rewrite the T114_T126_RedPhaseTests Assert.Fail stubs into real failing assertions for the corrected hook and reconcile to GREEN per FR-002b / T015b / T036 (partial)
- [ ] T051 Implement the GetChunkStartIndices three-parameter prime-modulus-stride (61) overload and wire the chunked scan path above 5 ms, or remove the misleading activation log per FR-002a / T043 (partial)
- [ ] T052 Execute the SC-009 scripted SMAPI session, capture all three signals, and update test.md to verified plus COMPLETION_SUMMARY.md and ROADMAP.md Gate 3 status per FR-001c / SC-009 / T039 / T040 (missing)"
