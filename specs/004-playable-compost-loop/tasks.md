# Tasks: Playable Compost Loop

**Input**: Design documents from `/specs/004-playable-compost-loop/` (`spec.md`, `plan.md`)
**Prerequisites**: `spec.md`, `plan.md`
**Tests**: YES — Strict TDD mode enforced. All test tasks (including property-based invariant tests with FsCheck / xUnit) MUST be written first and MUST BLOCK implementation tasks within each phase.

**Organization**: Tasks are grouped by foundational infrastructure and user stories to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared state or write dependencies). Parallel tasks are executed via Subagent-Driven Development in waves.
- **[Story]**: Which user story this task belongs to (`US1`, `US2`, `US3`, `US4`). Setup and Foundational tasks have no story label.
- Exact file paths and target classes/methods are included in every description.

---

## Phase 1: Setup (Shared Infrastructure & Property Testing Tooling)

**Purpose**: Initialize project dependencies and shared test infrastructure for property-based testing and concurrency stubs.

- [X] T001 [P] Add FsCheck and FsCheck.Xunit package references to `LivingRoots.Tests/LivingRoots.Tests.csproj` for property-based invariant testing
- [X] T002 [P] Create `LivingRoots.Tests/Stubs/MultiplayerFarmerStub.cs` implementing concurrent multi-farmer context stubs
- [X] T003 [P] Add refusal translation keys (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`) to `LivingRoots/i18n/default.json` and `LivingRoots/i18n/en.json`

---

## Phase 2: Foundational (Blocking Prerequisites for All Stories)

**Purpose**: Core constants, atomic locking primitives, and base model structures required before story implementation.

**⚠️ CRITICAL**: All tests and contracts in this phase MUST be authored first and fail before concrete implementation.

### Foundational Contracts & Tests (TDD Red Phase)
- [X] T004 [P] Create `LivingRoots/Domain/Interfaces/IHostAtomicLockService.cs` interface for atomic coordinate/machine action resolution
- [X] T005 [P] Unit and concurrency tests for `HostAtomicLockService` in `LivingRoots.Tests/HostAtomicLockServiceTests.cs` (verifying first-wins semantics and thread-safe lock acquisition/release)
- [X] T006 [P] Update `LivingRoots/Constants.cs` ensuring `MaturationDays = 2`, `RestorationAmount = 15f`, `MinSoilHealth = 0f`, `MaxSoilHealth = 100f`, and qualification item constants align with native machine specifications
- [X] T007 [P] Update `LivingRoots/Domain/Models/CompostingBinStateModel.cs` to support progress retention serialization (`InputId`, `DaysRemaining`) for `Item.modData`

### Foundational Implementation (TDD Green Phase)
- [X] T008 Implement host-authoritative tile and machine atomic coordinate lock helper `LivingRoots/Services/HostAtomicLockService.cs` satisfying `IHostAtomicLockService`

**Checkpoint**: Foundation ready — user story testing and implementation can now begin.

---

## Phase 3: User Story 1 - Obtain and place a composting bin (Priority: P1) 🎯 MVP

**Goal**: Enable crafting of the composting bin recipe, world placement on farm/Greenhouse, pickup retaining state, and native machine registration.

**Independent Test**: Launch mod, open crafting menu, verify composting bin recipe is listed and craftable with 50 wood / 25 stone / 15 fiber (per spec clarification 2026-10-04); place bin in world and pick back up.

### Tests for User Story 1 (TDD: Write and FAIL before implementation) ⚠️

- [X] T009 [P] [US1] Property-based invariant test for bin crafting recipe ingredient consumption in `LivingRoots.Tests/Invariants/BinCraftingInvariantsTests.cs` (verifying exact ingredient deduction for arbitrary craft counts)
- [X] T010 [P] [US1] Unit and contract test for SMAPI asset registration of `LivingRoots.CompostingBinRecipe` and `Data/Objects` entry in `LivingRoots.Tests/AssetRegistrationTests.cs`
- [X] T011 [P] [US1] Unit test for bin placement validation and world object initialization in `LivingRoots.Tests/BinPlacementTests.cs` (farm/Greenhouse valid, invalid terrain refused without ghost objects)
- [X] T012 [P] [US1] Unit test for bin pickup and dislodgement preserving unstackable `Item.modData` in `LivingRoots.Tests/BinPickupTests.cs` (inventory full debris drop, explosion dislodge)

### Implementation for User Story 1

- [X] T013 [P] [US1] Register Composting Bin machine and recipe programmatically in `LivingRoots/ModEntry.cs` via `AssetRequested` for `Data/BigCraftables` (bin machine), `Data/CraftingRecipes` (recipe), and `Data/Machines` (processing rules). Compost item (`Data/Objects`) is T022. NOTE: re-execution owned by T053 spike outcome.
- [X] T014 [US1] Implement bin placement and tool/explosion dislodgement handlers in `LivingRoots/Controllers/ModController.cs` tracking world objects, preventing orphaned state per FR-002, and ejecting floating debris (`Game1.createItemDebris`) if inventory is full per FR-005c. NOTE: placement guard re-decided in T057 (HoeDirt-only currently contradicts FR-002).
- [X] T015 [US1] Implement `Item.modData` serialization for held bin items in `LivingRoots/Services/CompostingBinService.cs` enforcing max stack size 1 and progress preservation per FR-005c

**Checkpoint**: User Story 1 is independently verifiable and functional *(pending Phase 9: T053 may re-execute T013; T055/T057 affect placement/pickup paths)*.

---

## Phase 4: User Story 2 - Turn organic waste into compost (Priority: P1)

**Goal**: Full machine processing lifecycle over 2 in-game days yielding exactly 1 compost item, safe collection handling full inventory, and tolerant save reader quarantining corrupt entries.

**Independent Test**: Place bin, deposit organic waste, advance 2 days, collect 1 compost; verify collection with full inventory keeps item in bin and displays native refusal message.

### Tests for User Story 2 (TDD: Write and FAIL before implementation) ⚠️

- [X] T016 [P] [US2] Property-based invariant test for machine state transitions in `LivingRoots.Tests/Invariants/CompostingMachineInvariantsTests.cs` (Empty + Waste → Processing → Ready after exactly 2 days; output quantity strictly == 1)
- [X] T017 [P] [US2] Unit test for single compost yield retiring 1–5 maturation multiplier in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005a
- [X] T018 [P] [US2] Unit test for collection with full inventory in `LivingRoots.Tests/CompostingBinServiceTests.cs` (zero compost lost, remains in bin, shows red refusal message per FR-005b and FR-008a)
- [X] T019 [P] [US2] Unit test for day-advance processing in multiplayer in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` (advancing day advances batch exactly once regardless of connected player count per FR-004)
- [X] T020 [P] [US2] Unit test for tolerant reader fault isolation and orphaned bin pruning in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` (skips corrupt entries, ignores obsolete fields, prunes disconnected records per FR-012, FR-015)
- [X] T021 [P] [US2] Unit test for concurrent player interactions on same bin in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` (first-wins atomic locking per FR-019)
- [X] T058 [P] [US2] Negative-path test for FR-006 in `LivingRoots.Tests/CompostingBinServiceTests.cs`: no path other than completed-batch harvest (debug spawn, console command, save injection) yields a compost item; each attempt is refused or quarantined per FR-012/FR-015. TDD red-first; on green, re-verify T023/T024 against it.
- [X] T059 [P] [US2] Unit test for non-qualifying waste refusal in `LivingRoots.Tests/CompostingBinServiceTests.cs`: arbitrary invalid items → vanilla audio cancel cue / machine shake, zero `showRedMessage` calls, zero inventory consumed, per FR-003 + FR-008a clarification 2026-10-04. Property-based over the invalid-item generator where feasible. TDD red-first alongside T058.

### Implementation for User Story 2

- [X] T022 [P] [US2] Register Compost object (`LivingRoots.Compost`, `(O)LivingRoots.Compost`) programmatically in `LivingRoots/ModEntry.cs` via `AssetRequested` against `Data/Objects`. NOTE: re-verify against T053 spike outcome together with T013.
- [X] T023 [US2] Update `LivingRoots/Services/CompostingBinService.cs` to enforce 1-out batch yield (retiring 1-5 multiplier per FR-005a) and day-start processing per FR-004. NOTE: single `_lastProcessedDay` guard is superseded by the per-location guard in T056.
- [X] T024 [US2] Implement non-destructive harvest collection with inventory-full check and localized red HUD message (`Game1.showRedMessage`) in `LivingRoots/Services/CompostingBinService.cs` and `LivingRoots/Controllers/ModController.cs` per FR-005b, FR-008a. NOTE: shows raw translation keys until T054 wires `ITranslationHelper`.
- [X] T025 [US2] Implement tolerant reader with per-record try-catch, sanitized logging (FR-015), and orphaned record pruning in `LivingRoots/Services/CompostingBinService.cs` per FR-012
- [X] T026 [US2] Integrate host-authoritative atomic lock into bin interaction handling in `LivingRoots/Controllers/ModController.cs` per FR-019
- [X] T060 [US2] Remediate any FR-006 bypass surfaced by T058 in `LivingRoots/ModEntry.cs` (registration validation) and/or `LivingRoots/Services/CompostingBinService.cs` (save quarantine per FR-012/FR-015); re-run T022–T024 after. Opens only if T058 fails; stays closed otherwise.

**Checkpoint**: User Stories 1 & 2 are complete. Composting cycle functions from waste to compost item *(code-complete; FR-006/FR-008a/FR-012 wiring attested only after T054/T056/T058–T060)*.

---

## Phase 5: User Story 3 - See soil health change on the ground (Priority: P2)

**Goal**: Apply compost to tilled dirt (with or without growing crops) adding +15 health clamped to 100, displaying localized refusal notices on invalid tiles/max health, and reactively re-rendering 3-tier RGB overlays with neutral baseline (0 health).

**Independent Test**: Hold compost, apply to tilled tile (empty and with growing crop); verify +15 health change and visual overlay updates within ≤16.67ms; verify refusal message when applying to untilled tile or tile at 100 health.

### Tests for User Story 3 (TDD: Write and FAIL before implementation) ⚠️

- [X] T027 [P] [US3] Property-based invariant test for soil health bounds and clamping in `LivingRoots.Tests/Invariants/SoilHealthInvariantsTests.cs` (for any initial health and compost applications, health is in [0, 100], exactly +15 per valid application)
- [X] T028 [P] [US3] Unit test for compost application on tilled tiles with active growing crops in `LivingRoots.Tests/CompostApplicationServiceTests.cs` (side-dressing succeeds, does not harm crop per FR-007)
- [X] T029 [P] [US3] Unit test for compost application refusals in `LivingRoots.Tests/CompostApplicationServiceTests.cs` (untilled, non-farm map, max health 100 refused with zero compost consumed and localized red HUD message per FR-008, FR-008a)
- [X] T030 [P] [US3] Unit test for reactive visual overlay invalidation in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` (subscribes to `OnTileHealthChanged`, invalidates cache immediately ≤16.67ms per FR-010)
- [X] T031 [P] [US3] Unit test for neutral dirt appearance at health 0 in `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` (no color overlay rendered at health 0 per FR-009, FR-010)
- [X] T032 [P] [US3] Unit test for concurrent compost application on same tile in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` (first-wins atomic lock, second refused with refund and alert per FR-019)
- [X] T033 [P] [US3] Unit test for daily soil health decay execution under multiplayer in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` (decay executes exactly once per day regardless of connected player count per FR-013)

### Implementation for User Story 3

- [X] T034 [P] [US3] Add `event Action<string, Vector2, float>? OnTileHealthChanged` to `LivingRoots/Domain/Interfaces/ISoilHealthService.cs` and invoke upon health mutation in `LivingRoots/Services/SoilHealthService.cs`
- [X] T035 [US3] Update `LivingRoots/Services/CompostApplicationService.cs` to allow top-dressing on active crops without damage (FR-007) and trigger localized refusal alerts via `Game1.showRedMessage` (FR-008, FR-008a). NOTE: shows raw translation keys until T054 wires `ITranslationHelper`.
- [X] T036 [US3] Update `LivingRoots/Services/Visualization/VisualizationService.cs` to subscribe to `OnTileHealthChanged`, omit overlays for health == 0 (FR-009, FR-010), and ensure 3-tier RGB interpolation under ambient lighting
- [X] T037 [US3] Integrate host-authoritative atomic lock into compost tile application in `LivingRoots/Controllers/ModController.cs` per FR-019

**Checkpoint**: User Stories 1, 2, and 3 are fully operational and visually observable *(pending T054 translation wiring — raw keys shown until then)*.

---

## Phase 6: User Story 4 - Trust what the project says about itself (Priority: P3)

**Goal**: Audit and reconcile public claims in `README.md` using `(Planned)` taxonomy, and reconcile multiplayer scope statements in the 8 prior specification and plan documents per Principle VI and FR-017.

**Independent Test**: Read `README.md` under `## Features` and assert all unbuilt items have `(Planned)`; verify all 8 spec/plan documents affirm multiplayer scope without contradiction.

### Tests for User Story 4 (TDD: Write and FAIL before implementation) ⚠️

- [X] T038 [P] [US4] Update embedded resource test in `LivingRoots.Tests/FeatureStatusTests.cs` to assert accurate `(Planned)` taxonomy in `README.md` per FR-016
- [X] T039 [P] [US4] Architectural compliance test in `LivingRoots.Tests/ArchitecturalPlanTests.cs` verifying zero multiplayer scope exclusions across the 8 specified documents per FR-017 and trailing newline integrity

### Implementation for User Story 4

- [X] T040 [P] [US4] Audit and update `README.md` under `## Features` labeling unbuilt features with `(Planned)` per FR-016
- [X] T041 [P] [US4] Reconcile multiplayer scope in `specs/001-soil-health-visualization/spec.md` and `specs/001-soil-health-visualization/plan.md` per FR-017
- [X] T042 [P] [US4] Reconcile multiplayer scope in `specs/002-soil-decay-compost/spec.md` and `specs/002-soil-decay-compost/plan.md` per FR-017
- [X] T043 [P] [US4] Reconcile multiplayer scope in `specs/003-composting-tests/spec.md` and `specs/003-composting-tests/plan.md` per FR-017
- [X] T044 [P] [US4] Reconcile multiplayer scope in `docs/design-system/REALITY-CHECK-EVALUATION.md` and `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` per FR-017

**Checkpoint**: Documentation and architectural consistency fully reconciled.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final formatting, code cleanup, and full suite validation.

- [X] T045 [P] Verify exception sanitization in logging across all modified services ensuring zero raw message leakage per FR-015
- [X] T046 Run `dotnet format Stardew-LivingRoots.sln` and verify zero style/indentation violations
- [X] T047 [P] Run end-to-end regression verification on full test suite

---

## Phase 8: Verification Gate (ONLY Verification Tasks Attest to Completeness)

**Purpose**: Final objective verification gate per user rules and Quality Gates. The agent itself NEVER attests to success; ONLY the execution and results of these verification tasks validate completion. Scope is stub-bounded for multiplayer (SC-005/SC-010): T052 live 2-player verification is excluded, and a green Phase 8 MUST NOT be read as Gate-3 multiplayer completeness.

- [X] T048 **Gate 1 (Format Check)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit code 0 — run AFTER T053–T057 (verified by T058–T060); T052 excluded (deferred to D1)
- [X] T049 **Gate 2 (Compilation & Build)**: Run `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — run AFTER T053–T057 (verified by T058–T060); T052 excluded (deferred to D1)
- [X] T050 **Gate 3 (Test Suite Execution)**: Run `GamePath=$GP dotnet test --no-build --configuration Release` and confirm 100% passing tests (including all property-based invariant and concurrency tests) — run AFTER T053–T057 (verified by T058–T060); T052 excluded (deferred to D1); prior pass was stub-bounded only; attestation is stub-bounded for SC-005/SC-010
- [X] T051 **Gate 4 (Architectural & Markdown Integrity)**: Run test filter `ArchitecturalPlanTests` and verify all specification markdown files end with trailing newline and pass structural checks — run AFTER T053–T057 (verified by T058–T060); T052 excluded (deferred to D1)

---

## Phase 9: Analyze Remediation (C1, C2, U1–U5 follow-up, all [ ] deferred)

**Purpose**: Close the wiring gaps found by `/speckit.analyze` without renumbering T001–T051. TDD red-first applies to each new test task below.

- [ ] T052 Gate 3 live 2-player debt (C1): host + farmhand full loop craft → place → load → 2 days → collect → apply → tier change with consistent values on both clients, plus mid-loop save/reload. Blocked until D1; 004 delivers stubs only (T019/T021/T032/T033). Attests SC-005/SC-010 live.
- [X] T053 [P] Spike `AssetRequested` record (U1, C2): time-boxed spike registering compost via `Data/Objects`, bin machine via `Data/BigCraftables` (`(BC)LivingRoots.CompostingBin`), recipe via `Data/CraftingRecipes`, and processing rules via `Data/Machines`; verify `(BC)` namespace resolves in-shop/in-world, `Triggers` + `DaysUntilReady = MaturationDays` fire, and `Item.modData` survives pickup/place. Output: `briefings/spike-decision.md` (native vs event-interception fallback per FR-001b; ContentPatcher out of scope). Blocks re-execution of T013/T022.
- [X] T054 [P] Wire `ITranslationHelper` (U2): inject via `ModEntry` into `CompostApplicationService`, `CompostingBinService`, and the `ModController` refusal closure (`OnButtonPressed`); replace `showRedMessage(key)` with translated text at all 6 sites per FR-008a. Test asserts translated PT/EN text, never the raw key. Covers T003+T024+T035.
- [X] T055 Wire composition root (U3): pass `playerProvider:`, `hostAtomicLockService:`, `locationProvider:` through `ModEntry` into `CompostingBinService`/`ModController`; remove silent `?? new` divergence or document the single shared instance. Wiring test: production `ModController` never has null `_playerProvider` (regresses dead hoe feedback at `ModController.cs:1708-1712`); orphan prune no longer short-circuits.
- [X] T056 Wire bin persistence (U4): call `_compostingBinService.LoadData/SaveData(saveId)` alongside soil in `OnSaveLoaded`/`OnSaving`; replace single `_lastProcessedDay` with per-location guard (Greenhouse matures). Test: round-trip matrix Empty/Processing/Ready x placed/held/debris (covers SC-004/G1).
- [X] T057 Decide + wire placement (U5): (a) native BigCraftable vanilla (removes custom `TryPlaceBin`/`Dislodge`) or (b) intercept with `Objects[tile].ItemId == Bin` guard + relax `HoeDirt`-only + consume `Stack--` on deposit (ends ghost bins + infinite waste). Test: valid/invalid placement + pickup/debris without orphans (FR-002/005c).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational completion.
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (machine requires item registration from US1).
- **User Story 3 (Phase 5)**: Depends on User Story 2 completion (requires compost item from US2).
- **User Story 4 (Phase 6)**: Can proceed after User Stories 1-3.
- **Polish (Phase 7)**: Depends on all stories being implemented.
- **Analyze Remediation (Phase 9)**: Depends on `/speckit.analyze` findings — wiring debt (T053–T057, verified by T058–T060), executed BEFORE the final gate run. T052 (live 2-player) stays deferred to roadmap D1 and is not a gate input.
- **Verification Gate (Phase 8)**: Depends on Polish completion AND Phase 9 wiring (T053–T057 verified by T058–T060) — FINAL ATTESTATION OF SUCCESS, run AFTER remediation. Attestation is stub-bounded for SC-005/SC-010; live multi-client transport remains owed to D1 via T052.

### Within Each User Story Phase

1. **TESTS FIRST (STRICT TDD BLOCKER)**: All test tasks (property-based invariants using FsCheck, unit, concurrency stubs) MUST be authored and verified failing before any implementation task in the phase is started.
2. **MODELS & CONTRACTS**: Interfaces and data transfer models.
3. **SERVICES & LIFECYCLE**: Implementation of business rules, tolerant reader, and machine logic.
4. **CONTROLLER & SMAPI INTEGRATION**: SMAPI hooks, atomic lock coordination, and localized red HUD alerts.
5. **DETERMINISTIC ATOMIC COMMIT HOOK**:
   - Immediately upon completing edits to a file or logical pair, execute an atomic conventional commit before proceeding:
     - `test(<scope>): <description>` (for test-first additions)
     - `feat(<scope>): <description>` (for implementation tasks)
     - `fix(<scope>): <description>` (for bug/tolerance adjustments)
     - `docs(<scope>): <description>` (for README and spec reconciliation)
   - Include a multi-line body summarizing the exact rationale, invariants verified, and files changed.

### Parallel Opportunities & Subagent Dispatch Plan

- Tasks marked with `[P]` have no overlapping write dependencies and MUST be delegated across parallel subagents in coordinated waves.
- Tasks sharing file context are batched to the same subagent.

- **Wave 1 (Setup Infrastructure)**:
  - Subagent A: T001 (`LivingRoots.Tests.csproj`), T002 (`MultiplayerFarmerStub.cs`)
  - Subagent B: T003 (`i18n/*.json`)
- **Wave 2 (Foundational Contracts & Invariant Tests)**:
  - Subagent C: T004 (`IHostAtomicLockService.cs`), T005 (`HostAtomicLockServiceTests.cs`)
  - Subagent D: T006 (`Constants.cs`), T007 (`CompostingBinStateModel.cs`)
  *(Followed by Serial Task T008: HostAtomicLockService.cs)*
- **Wave 3 (US1 Tests — Strict TDD Blocker for US1)**:
  - Subagent E: T009 (`BinCraftingInvariantsTests.cs`), T010 (`AssetRegistrationTests.cs`)
  - Subagent F: T011 (`BinPlacementTests.cs`), T012 (`BinPickupTests.cs`)
- **Wave 4 (US2 Tests — Strict TDD Blocker for US2)**:
  - Subagent G: T016 (`CompostingMachineInvariantsTests.cs`), T017 & T018 & T058 & T059 (`CompostingBinServiceTests.cs` — one subagent, shared file context per rule 4)
  - Subagent H: T019 & T021 (`MultiplayerConcurrencyTests.cs`), T020 (`CompostingBinPersistenceTests.cs`)
- **Wave 5 (US3 Tests — Strict TDD Blocker for US3)**:
  - Subagent I: T027 (`SoilHealthInvariantsTests.cs`), T028 & T029 (`CompostApplicationServiceTests.cs`)
  - Subagent J: T030 (`SoilHealthVisualizationTests.cs`), T031 (`OverlayRendererTests.cs`), T032 & T033 (`MultiplayerConcurrencyTests.cs`)
- **Wave 6 (US4 Tests & Documentation Reconciliation)**:
  - Subagent K: T038 (`FeatureStatusTests.cs`), T040 (`README.md`)
  - Subagent L: T039 (`ArchitecturalPlanTests.cs`), T041–T044 (8 spec/plan document reconciliations)
- **Wave 7 (Cross-Cutting Polish)**:
  - Subagent M: T045 (Exception sanitization audit across all modified services)
- **Wave 8 (Phase 9 wiring, after Wave 4 + baseline gates)**:
  - Subagent N: T053 (spike; `ModEntry.cs` asset formats + `briefings/spike-decision.md`) + T054 (`ITranslationHelper` wiring; disjoint files, parallel-safe)
  - Subagent O: T055 → T056 → T057 in order (all share `ModController.cs` context — one subagent, serial within)
  - T052: no agent (live session, deferred to D1). T060: opens only if T058 fails.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational).
2. Complete Phase 3 (User Story 1: Craft and place composting bin).
3. **STOP and VALIDATE**: Verify recipe and world placement independently before proceeding.

### Incremental Delivery

1. Setup + Foundation → Foundation ready.
2. Add US1 → Craft and place bin (MVP).
3. Add US2 → Load waste, process batch, collect compost with tolerant save handling.
4. Add US3 → Apply compost to restore soil health with reactive visual rendering.
5. Add US4 → Document reality and align multiplayer scope across all specifications.
6. Phase 8 → Verification gate executes and attests completion.

---

## Phase 10: Convergence

**Purpose**: Close the wiring and truthfulness gaps found by `/speckit.converge` without renumbering T001–T060. T052 stays deferred to roadmap D1; all tasks below are stub-bounded for SC-005/SC-010 (live multi-client transport remains D1 debt).

**Binding execution rules (maintainer-mandated)**:
0. **Template/scaffold first**: any code generatable by template/scaffold (LSP code actions) MUST be generated that way BEFORE hand-writing.
1. **Strict TDD blocker**: test tasks T061–T071 MUST be written first (property-based with FsCheck everywhere applicable) and MUST BLOCK all implementation tasks T072–T081 until every test is written and observed failing for the right reason.
2. **Clean-session implementation**: implementation tasks T072–T081 MUST run in a new session with clean context, never in the session that wrote the tests.
3. **[P] delegation**: every task marked `[P]` MUST be delegated across parallel agents/subagents/background agents at maximum viable fan-out.
4. **Waves**: tasks that can run in parallel carry `[P]` and execute via subagent-driven development in waves; tasks sharing file context reuse the same subagent serially within the wave.
5. **Verification attests**: gate tasks T082–T085 (plus manual T086) run LAST; ONLY their results attest success — never the implementing agent.
6. **Deterministic commit hook**: immediately after finishing edits to a file, cut one atomic conventional commit (`test(...):` / `feat(...):` / `fix(...):`) with a detailed summary body before proceeding.

### Convergence Tests (TDD Red Phase — BLOCKS T072–T081) ⚠️

- [X] T061 [P] [US3] Add failing property-based test asserting health 0 renders zero overlays over arbitrary tile states in `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` per FR-009 (contradicts)
- [X] T062 [P] [US1] Add failing reachability test proving the production event path invokes `TryPlaceBin`/`TryDislodgeBin`/`OnBinPlaced`/`OnObjectRemoved` with zero orphans in `LivingRoots.Tests/BinPlacementTests.cs` per FR-002 (missing)
- [X] T063 [P] [US2] Add failing multi-location day-tick test proving Farm and Greenhouse bins advance on the same day via the controller path in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-004 (partial)
- [X] T064 [P] [US2] Add failing double-grant regression test proving `TryInteractWithBin` with non-null `IPlayerInventory` yields exactly 1 item total across both inventories in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-005a (contradicts)
- [X] T065 [P] [US1] Add failing test proving a dislodged bin resolves as `(BC)LivingRoots.CompostingBin` with max stack 1 while carrying progress in `LivingRoots.Tests/BinPickupTests.cs` per FR-005c (partial)
- [X] T066 [P] [US2] Add failing refusal-key accuracy test proving processing/not-ready/deposit-reject states emit distinct keys and only true conflicts emit `action-conflict` in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-008a (partial)
- [X] T067 [P] [US2] Add failing compost presentation test proving the `Data/Objects` entry carries icon/texture and stack behaviour in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-005 (partial)
- [X] T068 [P] [US3] Add failing production decay idempotency test proving two same-day `ProcessDayStart` calls apply decay once without a test-local guard in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-013 (partial)
- [X] T069 [P] [US3] Add failing translated-text test asserting PT/EN rendered refusal text and never raw keys in `LivingRoots.Tests/CompostApplicationServiceTests.cs` per FR-008a (partial)
- [X] T070 [P] [US2] Add failing prune-safety test proving a location absent from the provider is retained rather than pruned in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per FR-012 (partial)
- [X] T071 [P] [US2] Add failing machine-clock coherence test proving native `DaysUntilReady` and mod `ProcessDayStart` agree with no double or shortcut maturation in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-001b (partial)

### Convergence Implementation (clean session — only after T061–T071 are written and red) 🆕

- [X] T072 [P] [US3] Omit health-0 overlays in `LivingRoots/Services/Visualization/OverlayRenderer.cs` and floor the Poor interval at 1 in `LivingRoots/Services/Visualization/ColorInterpolationService.cs` per FR-009 (contradicts)
- [X] T073 [US1] Wire `TryPlaceBin`/`TryDislodgeBin`/`OnBinPlaced`/`OnObjectRemoved` to production game events in `LivingRoots/Controllers/ModController.cs` per FR-002 (missing)
- [X] T074 [US2/US3] Tick all farm locations per day in `LivingRoots/Controllers/ModController.cs` and add a production idempotency guard in `LivingRoots/Services/SoilDecayService.cs` per FR-004 (partial)
- [X] T075 [US2] Remove the controller-side second grant in `LivingRoots/Controllers/ModController.cs` so `CompostingBinService.CollectCompost` is the sole credit per FR-005a (contradicts)
- [X] T076 [US1] Fix dislodged-bin identity to `(BC)` in `LivingRoots/Services/CompostingBinService.cs` and enforce max stack 1 via the `Data/BigCraftables` definition in `LivingRoots/ModEntry.cs` per FR-005c (partial)
- [X] T077 [US2] Emit accurate per-state refusal keys and use `ModConstants` key constants in `LivingRoots/Controllers/ModController.cs` per FR-008a (partial)
- [X] T078 [US2] Wire the compost icon/texture asset and `ObjectData.StackSize` in `LivingRoots/ModEntry.cs` and `LivingRoots/Assets/` per FR-005 (partial)
- [X] T079 [US2] Guard orphan pruning against absent-from-provider locations in `LivingRoots/Services/CompostingBinService.cs` per FR-012 (partial)
- [X] T080 [US2] Reconcile the native machine clock with mod day-start processing in `LivingRoots/ModEntry.cs` and `LivingRoots/Controllers/ModController.cs` per FR-001a (partial)
- [X] T081 [P] Review and remove orphaned i18n keys and eliminate the `?? new HostAtomicLockService()` second-registry hazard in `LivingRoots/i18n/default.json` and `LivingRoots/Controllers/ModController.cs` per Constitution V (unrequested)

### Convergence Verification (ONLY these attest — run LAST)

- [X] T082 **Gate 1 (Format Check)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit code 0 — run AFTER T072–T081; T052 excluded (deferred to D1)
- [X] T083 **Gate 2 (Compilation & Build)**: Run `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — run AFTER T072–T081; T052 excluded (deferred to D1)
- [X] T084 **Gate 3 (Test Suite Execution)**: Run `GamePath=$GP dotnet test --no-build --configuration Release` and confirm 100% passing including T061–T071 — run AFTER T072–T081; T052 excluded (deferred to D1); attestation is stub-bounded for SC-005/SC-010
- [X] T085 **Gate 4 (Architectural & Markdown Integrity)**: Run test filter `ArchitecturalPlanTests` and verify trailing newlines plus this appended Phase 10 — run AFTER T072–T081; T052 excluded (deferred to D1)
- [ ] T086 [US1/US2/US3] Maintainer manual playtest: fresh save full loop craft → place → load → 2 days → collect → apply → tier change plus mid-loop save/reload per SC-001 (missing)

---

### Convergence Waves & Subagent Dispatch Plan

- **Wave 9 (Convergence tests — all [P], red-first, BLOCKS implementation)**:
  - Subagent P: T061 (`OverlayRendererTests.cs`)
  - Subagent Q: T062 (`BinPlacementTests.cs`), T065 (`BinPickupTests.cs` — same pickup/placement context, one subagent serial within)
  - Subagent R: T063, T064, T068 (`MultiplayerConcurrencyTests.cs` — one subagent, shared file context per rule 4)
  - Subagent S: T066 (`CompostingBinServiceTests.cs`), T070 (`CompostingBinPersistenceTests.cs`)
  - Subagent T: T067, T071 (`AssetRegistrationTests.cs` — one subagent, shared file context), T069 (`CompostApplicationServiceTests.cs`)
- **Wave 10 (Convergence implementation — clean session per rule 2)**:
  - Subagent U: T072 (`OverlayRenderer.cs`, `ColorInterpolationService.cs`)
  - Subagent V: T073 → T074 → T075 → T077 in order (all share `ModController.cs` context — one subagent, serial within)
  - Subagent W: T076 → T079 in order (share `CompostingBinService.cs` context — one subagent, serial within) plus T078, T080 (`ModEntry.cs` asset/machine blocks)
  - Subagent X: T081 (i18n + lock hazard hardening)
- **Wave 11 (Convergence verification — ONLY attestation per rule 5)**: T082 → T083 → T084 → T085 in gate order; T086 is maintainer-executed manual playtest, never agent-attested.

---

## Phase 11: Convergence

**Purpose**: Close the residual gaps found by the second `/speckit.converge` pass without renumbering T001–T086. T052 (live 2-player, D1) and T086 (maintainer manual playtest, SC-001) stay open and are NOT duplicated below; all tasks below are stub-bounded for SC-005/SC-010.

**Binding execution rules (maintainer-mandated)**:
0. **Template/scaffold first**: any code generatable by template/scaffold (LSP code actions) MUST be generated that way BEFORE hand-writing.
1. **Strict TDD blocker**: test tasks T087–T096 MUST be written first (property-based with FsCheck everywhere applicable) and MUST BLOCK all implementation tasks T097–T106 until every test is written and observed failing for the right reason.
2. **Clean-session implementation**: implementation tasks T097–T106 MUST run in a new session with clean context, never in the session that wrote the tests.
3. **[P] delegation**: every task marked `[P]` MUST be delegated across parallel agents/subagents/background agents at maximum viable fan-out.
4. **Waves**: tasks that can run in parallel carry `[P]` and execute via subagent-driven development in waves; tasks sharing file context reuse the same subagent serially within the wave.
5. **Verification attests**: gate tasks T107–T110 run LAST; ONLY their results attest success — never the implementing agent.
6. **Deterministic commit hook**: immediately after finishing edits to a file, cut one atomic conventional commit (`test(...):` / `feat(...):` / `fix(...):` / `docs(...):`) with a detailed summary body before proceeding.

### Convergence Tests (TDD Red Phase — BLOCKS T097–T106) ⚠️

- [X] T087 [P] [US2] Add failing architectural test proving zero `ex.StackTrace` in production output (only `GetType().FullName` + `HResult` at Trace) in `LivingRoots.Tests/` per FR-015 (contradicts)
- [X] T088 [P] [US1] Add failing test proving the placed bin object sets the big-craftable flag and qualifies as `(BC)LivingRoots.CompostingBin` in `LivingRoots.Tests/BinPlacementTests.cs` per FR-001 (partial)
- [X] T089 [P] [US1] Add failing test proving the bin engages the native machine interface with wired bin texture/progress sprites in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-001a (partial)
- [X] T090 [P] [US1] Add failing test proving tool/explosion removal prunes live bin cache without a reload in `LivingRoots.Tests/BinPlacementTests.cs` per FR-002 (partial)
- [X] T091 [P] [US3] Add failing test proving overlays blend under ambient/dark lighting instead of full-opacity over a darkened world in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-010 (partial)
- [X] T092 [P] [US4] Add failing architectural test proving both `docs/design-system/*.md` reference Principle VI host-authoritative farm state in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per FR-017 (missing)
- [X] T093 [P] [US3] Add failing translated-text test proving a non-English locale renders localized refusal text (never English fallback) in `LivingRoots.Tests/CompostApplicationServiceTests.cs` per FR-008a (partial)
- [X] T094 [P] [US3] Add failing latency test measuring `OnTileHealthChanged` → invalidation through the production `ModEntry` subscription in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-010 (partial)
- [X] T095 [P] [US1] Add failing property-based test proving greenhouse-like locations beyond the literal name are accepted in `LivingRoots.Tests/BinPlacementTests.cs` per FR-002 (partial)
- [X] T096 [P] [US2] Add failing test proving the production `ModController` never carries a null lock service (reflection `Assert.NotNull`) in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-019 (partial)

### Convergence Implementation (clean session — only after T087–T096 are written and red) 🆕

- [X] T097 [P] [US2] Remove the 8 `ex.StackTrace` log sites keeping `GetType().FullName` + `HResult` in `LivingRoots/Controllers/ModController.cs` and `LivingRoots/Services/SoilHealthService.cs` per FR-015 (contradicts)
- [X] T098 [US1] Set the big-craftable flag on the placed bin object in `LivingRoots/Controllers/ModController.cs` per FR-001 (partial)
- [X] T099 [US1] Route bin interaction through the native machine UI and wire bin texture/progress sprites in `LivingRoots/ModEntry.cs`, `LivingRoots/Controllers/ModController.cs` and `LivingRoots/Assets/` per FR-001a (partial)
- [X] T100 [US1] Subscribe tool/explosion removal and prune live bin cache in `LivingRoots/Controllers/ModController.cs` per FR-002 (partial)
- [X] T101 [P] [US3] Blend soil overlays under the ambient lighting pass in `LivingRoots/Services/Visualization/VisualizationService.cs` per FR-010 (partial)
- [X] T102 [P] [US4] Wording-correct both `docs/design-system/*.md` with a Principle VI host-authoritative pointer without redesigning threading advice per FR-017 (missing)
- [X] T103 [P] [US3] Add non-English translation file(s) and verify locale switching for refusal keys in `LivingRoots/i18n/` per FR-008a (partial)
- [X] T104 [P] [US3] Rewire the latency assertion through the production subscription in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots/Services/Visualization/VisualizationService.cs` per FR-010 (partial)
- [X] T105 [US1] Accept modded greenhouse-like locations beyond the literal name in `LivingRoots/Controllers/ModController.cs` per FR-002 (partial)
- [X] T106 [P] [US2] Enforce the single shared lock instance and guard the `?? true` no-op fallback in `LivingRoots/Controllers/ModController.cs` per FR-019 (partial)

### Convergence Verification (ONLY these attest — run LAST)

- [X] T107 **Gate 1 (Format Check)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit code 0 — run AFTER T097–T106; T052/T086 excluded (D1/maintainer)
- [X] T108 **Gate 2 (Compilation & Build)**: Run `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — run AFTER T097–T106; T052/T086 excluded (D1/maintainer)
- [X] T109 **Gate 3 (Test Suite Execution)**: Run `GamePath=$GP dotnet test --no-build --configuration Release` and confirm 100% passing including T087–T096 — run AFTER T097–T106; T052/T086 excluded (D1/maintainer); attestation is stub-bounded for SC-005/SC-010
- [X] T110 **Gate 4 (Architectural & Markdown Integrity)**: Run test filter `ArchitecturalPlanTests` and verify trailing newlines plus this appended Phase 11 — run AFTER T097–T106; T052/T086 excluded (D1/maintainer)

---

### Convergence Waves & Subagent Dispatch Plan (Phase 11)

- **Wave 12 (Convergence tests — all [P], red-first, BLOCKS implementation)**:
  - Subagent AA: T087 (`LivingRoots.Tests/` arch logging test)
  - Subagent AB: T088, T090, T095 (`BinPlacementTests.cs` — one subagent, shared file context serial within)
  - Subagent AC: T089 (`AssetRegistrationTests.cs`), T096 (`MultiplayerConcurrencyTests.cs`)
  - Subagent AD: T091, T094 (`SoilHealthVisualizationTests.cs` — one subagent, shared file context), T093 (`CompostApplicationServiceTests.cs`)
  - Subagent AE: T092 (`ArchitecturalPlanTests.cs`)
- **Wave 13 (Convergence implementation — clean session per rule 2)**:
  - Subagent AF: T097 (`ModController.cs` logging + `SoilHealthService.cs` — serial within, shared sanitization context)
  - Subagent AG: T098 → T099 → T100 → T105 in order (all share `ModController.cs` context — one subagent, serial within) plus `ModEntry.cs`/`Assets/` blocks of T099
  - Subagent AH: T101 (`VisualizationService.cs`) + T104 (latency wiring test fix)
  - Subagent AI: T102 (`docs/design-system/*.md`), T103 (`i18n/`), T106 (lock guard hardening)
- **Wave 14 (Convergence verification — ONLY attestation per rule 5)**: T107 → T108 → T109 → T110 in gate order; T052/T086 remain maintainer/D1-executed, never agent-attested.

---

## Phase 12: Convergence

**Purpose**: Close the single residual player-facing wording gap found by the third `/speckit.converge` pass without renumbering T001–T110. All other gaps observed map 1:1 to already-open tasks (T097–T106 implementation, T107–T110 gates, T052 live 2-player debt, T086 manual playtest) and are NOT duplicated below.

**Binding execution rules (maintainer-mandated)**:
0. **Template/scaffold first**: any code generatable by template/scaffold (LSP code actions) MUST be generated that way BEFORE hand-writing.
1. **Strict TDD blocker**: test task T111 MUST be written first and MUST BLOCK implementation task T112 until it is observed failing for the right reason. Property-based testing is N/A here (fixed player-facing string literal, no input domain to generate over).
2. **Clean-session implementation**: implementation task T112 MUST run in a new session with clean context, never in the session that wrote the test.
3. **[P] delegation**: no task below carries [P] — T111 → T112 → T113 form a strict serial dependency chain (test-red → fix → verify) with nothing viable to fan out.
4. **Waves**: single serial wave per rule 3; no subagent fan-out applies.
5. **Verification attests**: gate task T113 runs LAST; ONLY its result attests success for Phase 12 — never the implementing agent.
6. **Deterministic commit hook**: immediately after finishing edits to a file, cut one atomic conventional commit (`test(...):` / `fix(...):`) with a detailed summary body before proceeding.

### Convergence Tests (TDD Red Phase — BLOCKS T112) ⚠️

- [X] T111 Add failing test asserting the registered bin `BigCraftableData` description states single-yield (one batch yields one compost) and contains no increasing-output promise in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-005a (contradicts)

### Convergence Implementation (clean session — only after T111 is written and red) 🆕

- [X] T112 Rewrite the bin `BigCraftableData` description in `LivingRoots/ModEntry.cs` removing the retired-multiplier promise and stating one-batch-yields-one-compost without hardcoding the duration per FR-005a (contradicts)

### Convergence Verification (ONLY this attests Phase 12 — run LAST)

- [X] T113 **Scoped gate (Phase 12)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes`, then `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` with zero errors/warnings, then `GamePath=$GP dotnet test --no-build --configuration Release --filter "FullyQualifiedName~AssetRegistrationTests"` confirming T111 passes — run AFTER T112; T052/T086 excluded (D1/maintainer)

---

## Phase 13: Convergence

**Purpose**: Close the residual gaps found by the fourth `/speckit.converge` pass (three parallel audit agents + direct code verification) without renumbering T001–T113. T052 (live 2-player, D1) and T086 (maintainer manual playtest, SC-001) stay open and are NOT duplicated below; all tasks below are stub-bounded for SC-005/SC-010.

**Binding execution rules (maintainer-mandated)**:
0. **Template/scaffold first**: any code generatable by template/scaffold (LSP code actions) MUST be generated that way BEFORE hand-writing.
1. **Strict TDD blocker**: test tasks T114–T126 MUST be written first (property-based with FsCheck everywhere applicable; fixed-literal assertions document N/A) and MUST BLOCK all implementation tasks T127–T141 until every test is written and observed failing for the right reason.
2. **Clean-session implementation**: implementation tasks T127–T141 MUST run in a new session with clean context, never in the session that wrote the tests.
3. **[P] delegation**: every task marked `[P]` MUST be delegated across parallel agents/subagents/background agents at maximum viable fan-out.
4. **Waves**: tasks that can run in parallel carry `[P]` and execute via subagent-driven development in waves; tasks sharing file context reuse the same subagent serially within the wave.
5. **Verification attests**: gate tasks T142–T145 run LAST; ONLY their results attest success — never the implementing agent.
6. **Deterministic commit hook**: immediately after finishing edits to a file, cut one atomic conventional commit (`test(...):` / `feat(...):` / `fix(...):` / `docs(...):`) with a detailed summary body before proceeding.

### Convergence Tests (TDD Red Phase — BLOCKS T127–T141) ⚠️

- [ ] T114 [P] [US1] Add failing test proving tool/explosion removal of a progress-carrying bin preserves its batch modData (no contents/progress destroyed) in `LivingRoots.Tests/BinPlacementTests.cs` per FR-005c (contradicts)
- [ ] T115 [P] [US2] Add failing test proving `TryInteractWithBin` with an absent lock service refuses (fail-closed, zero mutation) in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-019 (contradicts)
- [ ] T116 [P] [US1] Add failing test proving bin placement on building-footprint tiles is refused with zero objects added and zero state in `LivingRoots.Tests/BinPlacementTests.cs` per FR-002 (partial)
- [ ] T117 [P] [US1] Add failing test proving a progress-carrying bin enforces max stack 1 through the production path in `LivingRoots.Tests/BinPickupTests.cs` per FR-005c (partial)
- [ ] T118 [P] [US1] Add failing test proving a dislodged bin with inventory space returns to inventory (debris only when full) in `LivingRoots.Tests/BinPickupTests.cs` per FR-005c (partial)
- [ ] T119 [P] [US2] Add failing test proving one batch pays exactly 1 compost across both the native `Data/Machines` output and the interception harvest path (no double-payout) in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005b (partial)
- [ ] T120 [P] [US2] Add failing parity test proving the `Data/Machines` RequiredTags accept exactly the `OrganicWasteValidator` set (no restatement drift) in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-003 (partial)
- [ ] T121 [P] [US2] Add failing test asserting the full-inventory harvest refusal renders translated red HUD text (never raw key) in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005b/FR-008a (partial)
- [ ] T122 [P] [US3] Add failing translated-text tests proving both `ModController` conflict sites render translated text (never raw key) in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-008a (partial)
- [ ] T123 [P] [US2] Add failing test proving a null location payload aborts only that location while remaining locations still load in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per FR-012 (partial)
- [ ] T124 [P] [US3] Add failing test proving invalid bin drop-in shakes the machine in addition to the cancel cue in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-008a (missing)
- [ ] T125 [P] [US2] Rewrite the SC-010 headline test to drive production `TryApplyCompostWithLock`/`CompostApplicationService` instead of inline reimplementation in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-019 (partial)
- [ ] T126 [P] [US3] Add failing test proving overlays report only drawable values (unknown labeled, never fabricated) in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-011 (partial)

### Convergence Implementation (clean session — only after T114–T126 are written and red) 🆕

- [ ] T127 [US1] Preserve batch modData on tool/explosion removal (reconstruct the dislodged item carrying progress, debris with modData when inventory is full) in `LivingRoots/Controllers/ModController.cs` per FR-005c (contradicts)
- [ ] T128 [US1/US2] Fail closed on absent lock in `TryInteractWithBin` (remove `?? true` on the mutating path), correct the conflict-messaging comment, and guard the `?? 0L` farmer-id collapse in `LivingRoots/Controllers/ModController.cs` per FR-019 (contradicts)
- [X] T129 [US1] Refuse bin placement on building-footprint tiles (read `location.buildings`) in `LivingRoots/Controllers/ModController.cs` per FR-002 (partial)
- [ ] T130 [US1] Enforce max stack 1 for progress-carrying bins via the `Data/BigCraftables` definition in `LivingRoots/ModEntry.cs` per FR-005c (partial)
- [ ] T131 [US1] Route dislodged bins into inventory when space is available (debris only when full) in `LivingRoots/Controllers/ModController.cs` per FR-005c (partial)
- [ ] T132 [US2] Reconcile the native `Data/Machines` output with interception so exactly one driver pays each batch in `LivingRoots/ModEntry.cs` and `LivingRoots/Controllers/ModController.cs` per FR-001b/FR-005b (partial)
- [ ] T133 [US2] Derive the `Data/Machines` RequiredTags from the single validator-owned waste set (or pin parity) in `LivingRoots/ModEntry.cs` per FR-003 (partial)
- [ ] T134 [US2] Harden the tolerant reader to per-location isolation (null payload skips only that location) and narrow the day-start try per location in `LivingRoots/Services/CompostingBinService.cs` and `LivingRoots/Controllers/ModController.cs` per FR-012/FR-014 (partial)
- [ ] T135 [US2] Move `SaveData` I/O outside `_lock` via snapshot-diff (SoilHealthService pattern) in `LivingRoots/Services/CompostingBinService.cs` per Constitution III (contradicts)
- [ ] T136 [US3] Add machine shake on invalid drop-in (`WorkingEffects`/`ShakeDuration` or game shake call) in `LivingRoots/ModEntry.cs` and `LivingRoots/Services/CompostingBinService.cs` per FR-008a (missing)
- [ ] T137 [P] [US4] Add Principle VI host-authoritative rows to `specs/003-composting-tests/spec.md` and `specs/003-composting-tests/plan.md` without redesigning settled requirements per FR-017 (missing)
- [ ] T138 [P] [US2] Correct the retired-multiplier stale docs (`MaturationLevel determines output multiplier`) in `LivingRoots/Domain/Models/CompostingBinStateModel.cs` per FR-005a (contradicts)
- [ ] T139 [P] [US3] Read the compost description increment from `ModConstants.RestorationAmount` instead of the hardcoded literal in `LivingRoots/ModEntry.cs` (convention)
- [ ] T140 [P] [US2] Align `CompostingBinService` logging with `GetType().FullName` + `HResult` at Trace in `LivingRoots/Services/CompostingBinService.cs` per FR-015 (partial)
- [ ] T141 [P] [US2] Remove sync-over-async (`.GetAwaiter().GetResult()`) in `LivingRoots.Tests/Stubs/MultiplayerFarmerStub.cs` per Constitution III (contradicts)

### Convergence Verification (ONLY these attest — run LAST)

- [ ] T142 **Gate 1 (Format Check)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit code 0 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)
- [ ] T143 **Gate 2 (Compilation & Build)**: Run `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)
- [ ] T144 **Gate 3 (Test Suite Execution)**: Run `GamePath=$GP dotnet test --no-build --configuration Release` and confirm 100% passing including T114–T126 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer); attestation is stub-bounded for SC-005/SC-010
- [ ] T145 **Gate 4 (Architectural & Markdown Integrity)**: Run test filter `ArchitecturalPlanTests` and verify trailing newlines plus this appended Phase 13 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)

---

### Convergence Waves & Subagent Dispatch Plan (Phase 13)

- **Wave 15 (Convergence tests — all [P], red-first, BLOCKS implementation)**:
  - Subagent BA: T114, T116 (`BinPlacementTests.cs` — one subagent, shared file context serial within)
  - Subagent BB: T117, T118 (`BinPickupTests.cs` — one subagent, shared file context serial within)
  - Subagent BC: T119, T121, T124 (`CompostingBinServiceTests.cs` — one subagent, shared file context serial within), T123 (`CompostingBinPersistenceTests.cs`)
  - Subagent BD: T115, T122, T125 (`MultiplayerConcurrencyTests.cs` — one subagent, shared file context serial within)
  - Subagent BE: T120 (`AssetRegistrationTests.cs`), T126 (`SoilHealthVisualizationTests.cs`)
- **Wave 16 (Convergence implementation — clean session per rule 2)**:
  - Subagent BF: T127 → T128 → T129 → T131 in order (all share `ModController.cs` context — one subagent, serial within) plus T134 (`ModController.cs` day-start half)
  - Subagent BG: T134 (`CompostingBinService.cs` half) → T135 in order (share `CompostingBinService.cs` context — one subagent, serial within) plus T136 (`CompostingBinService.cs` half)
  - Subagent BH: T130, T132, T133, T136 (`ModEntry.cs` half — one subagent, shared file context serial within)
  - Subagent BI: T137 (`specs/003-*` docs), T138 (`CompostingBinStateModel.cs`), T139 (`ModEntry.cs` description literal — joins BH if contended), T140 (`CompostingBinService.cs` logging — joins BG if contended), T141 (`MultiplayerFarmerStub.cs`)
- **Wave 17 (Convergence verification — ONLY attestation per rule 5)**: T142 → T143 → T144 → T145 in gate order; T052/T086 remain maintainer/D1-executed, never agent-attested.

---

## Phase 14: Convergence

**Purpose**: Close the residual gaps found by the fourth `/speckit.converge` pass (post-T133) without renumbering T001–T140. T052 (live 2-player, D1) stays deferred per spec Assumptions / Constitution VI; T086 (manual playtest) stays maintainer-executed. All below are stub-bounded for SC-005/SC-010 until D1.

**Binding execution rules (maintainer-mandated)**:
0. Template/scaffold first: none required (existing `Data/Machines`/`ModController.cs`/service registrations).
1. Strict TDD blocker: any new test for the items below MUST be written first (property-based with FsCheck where applicable) and MUST BLOCK the corresponding implementation.
2. Clean-session implementation: each implementation below MUST run in a new session with clean context, never in the session that wrote the tests.
3. [P] delegation: tasks marked `[P]` delegated across parallel agents/subagents/background agents at maximum viable fan-out.
4. Waves: tasks sharing file context reuse the same subagent serially within the wave.
5. Verification attests: gate tasks T141–T145 run LAST; ONLY their results attest success.
6. Deterministic commit hook: immediately after finishing edits to a file, cut one atomic conventional commit (`test(...):` / `feat(...):` / `fix(...):`) with a detailed summary body.

### Convergence Tests (TDD Red Phase — BLOCKS T141–T145) ⚠️

- [ ] T141 [P] [US2] Property-based test asserting exactly one compost item across native `Data/Machines` output + interception harvest path (no double-payout) — re-verify T119 after fix in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005a (partial)
- [ ] T142 [P] [US1] Property-based test asserting placed bin object carries progress-modData (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) and enforces max stack 1 through the production `Data/BigCraftables` definition — re-verify T057 placement/pickup paths per FR-002/FR-005c (partial)
- [ ] T143 [P] [US3] Property-based test proving overlays report only drawable values (never fabricated percentages for unknown/unmeasurable tiles) — fix T126 in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-011 (partial)
- [ ] T144 [P] [US4] Translation parity test asserting PT/EN refusal text renders at the 6 refusal sites (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`, plus 2 concurrency sites) with zero raw-key leakage — complete T069 in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs`/`CompostApplicationServiceTests.cs` per FR-008a (partial)
- [ ] T145 [P] [US2/US4] Fail-closed concurrency test: `TryInteractWithBin` with an absent/`null` `IHostAtomicLockService` refuses (zero mutation, zero inventory change) and emits translated `message.compost.action-conflict`; guard `?? true` on mutating path removed — complete T128 in `LivingRoots/Controllers/ModController.cs` per FR-019 (partial)

### Convergence Implementation (clean session — only after T141–T145 are written and red) 🆕

- [ ] T146 [US2] Remediate FR-005a double-payout in `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/CompostingBinService.cs`: ensure `CollectCompost` is the sole credit per batch (native `Data/Machines` output disabled; interception path only) — re-run T141 after
- [ ] T147 [US1/US2] Wire placement guard (FR-002): allow farm/Greenhouse with `HoeDirt` + relax invalid-terrain refusal; preserve progress-modData on dislodge; enforce max stack 1 via `Data/BigCraftables`; remove ghost-bin orphans — re-run T142 after
- [ ] T148 [US3] Fix overlay drawable-only reporting in `LivingRoots/Services/Visualization/VisualizationService.cs`: suppress fabricated values for unknown/unmeasurable tiles; floor Poor interval at 1 — re-run T143 after
- [ ] T149 [US2/US3] Wire `ITranslationHelper` for remaining refusal sites (T069): replace `showRedMessage(key)` with translated PT/EN text at 6 sites; assert never raw key in `LivingRoots/Controllers/ModController.cs` / `CompostApplicationService.cs` / `CompostingBinService.cs` — re-run T144 after
- [ ] T150 [US2] Fail-closed atomic-lock guard: remove `?? true` on mutating path in `ModController.TryInteractWithBin`; correct conflict-messaging comment; guard `?? 0L` farmer-id collapse — re-run T145 after

### Convergence Verification (ONLY these attest — run LAST)

- [ ] T151 **Gate 1 (Format Check)**: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exit 0 — after T146–T150; T052 excluded
- [ ] T152 **Gate 2 (Compilation & Build)**: `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` zero errors/warnings — after T146–T150; T052 excluded
- [ ] T153 **Gate 3 (Test Suite Execution)**: `GamePath=$GP dotnet test --no-build --configuration Release` confirm 100% passing including T141–T145 — after T146–T150; T052 excluded; attestation stub-bounded for SC-005/SC-010
- [ ] T154 **Gate 4 (Architectural & Markdown Integrity)**: `dotnet test --filter "FullyQualifiedName~ArchitecturalPlanTests"` + trailing newline check on all 8 spec/plan docs reconciled per FR-017 — after T146–T150; T052/T086 excluded
- [ ] T155 [US1/US2/US3] Maintainer manual playtest (SC-001): fresh save full loop craft → place → load → 2 days → collect → apply → tier change plus mid-loop save/reload — deferred to maintainer (T086); never agent-attested

### Convergence Waves & Subagent Dispatch Plan (Phase 14)

- **Wave 15 (Convergence tests — all [P], red-first, BLOCKS T146–T150)**:
  - Subagent BA: T141 (`CompostingBinServiceTests.cs` — single-batch parity, shared with T119 context)
  - Subagent BB: T142 (`BinPlacementTests.cs` — placement/modData/stack-1, shared with T057 context)
  - Subagent BC: T143 (`SoilHealthVisualizationTests.cs` — drawable-only, shared with T126 context)
  - Subagent BD: T144 (`MultiplayerConcurrencyTests.cs` + `CompostApplicationServiceTests.cs` — translation parity, shared with T069 context)
  - Subagent BE: T145 (`MultiplayerConcurrencyTests.cs` — fail-closed lock, shared with T128 context)
- **Wave 16 (Convergence implementation — clean session per rule 2)**:
  - Subagent BF: T146 (`ModController.cs` + `CompostingBinService.cs` — serialization within shared context)
  - Subagent BG: T147 (`ModController.cs` — placement/pickup/debris, serial within shared file context) + T150 (`ModController.cs` lock guard, same file context)
  - Subagent BH: T148 (`VisualizationService.cs`) + T149 (`ModController.cs` / `CompostApplicationService.cs` / `CompostingBinService.cs` — translation wiring, disjoint files, parallel-safe)
- **Wave 17 (Convergence verification — ONLY attestation per rule 5)**: T151 → T152 → T153 → T154 in gate order; T155 is maintainer-executed manual playtest, never agent-attested; T052 excluded (D1 deferred).

## Phase 15: Convergence

- [X] T156 [P] [US2] Remediate FR-005a exactly-one-compost: replace `outputCount = bin.MaturationLevel` with `1`; disable native `Data/Machines` output path so interception (`CollectCompost`) is sole credit; remove `MaturationLevel` multiplier logic — verify in `LivingRoots/Services/CompostingBinService.cs` per FR-005a (missing)
- [X] T157 [P] [US2/US3] Wire translated refusal messages at 6 sites (inventory-full, invalid-tile, max-health, action-conflict, plus 2 concurrency) via `TranslateRefusalMessage` / `ITranslationHelper`; assert zero raw-key leakage in `ModController.cs`/`CompostApplicationService.cs`/`CompostingBinService.cs` per FR-008a (partial)
- [ ] T158 [P] [US1] Wire placement guard + `Data/BigCraftables` definition: allow farm/Greenhouse with `HoeDirt`; relax invalid-terrain refusal; preserve progress-modData on dislodge; enforce max stack 1; prune orphaned bins; verify in `ModEntry.cs`/`CompostingBinService.cs` per FR-002/FR-005c (missing)
- [ ] T159 [P] [US2/US3] Fix overlay drawable-only reporting in `VisualizationService.cs`: suppress fabricated values for unknown/unmeasurable tiles; floor Poor interval at 1; ensure ≤16.67ms reactive update via `ISoilHealthService.OnTileHealthChanged`; verify per FR-011 (partial)
- [ ] T160 [P] [US2/US4] Complete property-test suite T141–T144 (single-payout, placement/modData, drawable-only, translation parity) and fail-closed concurrency guard T145 (remove `?? true`/message fix); then run Gates T151–T154 (format, build, tests, architectural integrity) after T156–T159 — attest stub-bounded for SC-005/SC-010 per plan (partial)

## Phase 16: Convergence (restored — implemented by prior session)
- [X] T156 [P] [US2] Remediate FR-005a exactly-one-compost (outputCount=1)
- [X] T157 [P] [US2/US3] Wire translated refusal messages at 6 sites
- [X] T161 [P] [US1] Failing placement/modData test (red; subagent BA)
- [X] T162 [P] [US3] Failing drawable-only overlay test (red; subagent BB)
- [X] T163 [P] [US2/US4] Failing property-test / gate-readiness test (red; subagent BC)
- [X] T164 [P] [US2] Failing fail-closed concurrency test (red; subagent BD)
- [X] T165 [P] [US2/US3] Failing translated-refusal parity test (red; subagent BE)
- [X] T166 [US1] Wire bin placement guard + BigCraftables (implemented)
- [X] T167 [US3] Fix overlay drawable-only reporting (implemented)
- [ ] T168 [US2/US4] Remediate property-test suite + gates (dormant CI)
- [ ] T169 [US2] Fail-closed concurrent guard (?? true removed — done in T176; verify)
- [ ] T170 [P] [US2/US3] Complete T156/T157 verification + clean lint

### Verification (Phase 16 gates)
- [ ] T171 Gate 1 (Format) — verified on edited files
- [ ] T172 Gate 2 (Build) — dormant until SMAPI
- [ ] T173 Gate 3 (Tests) — stub-bounded SC-005/SC-010
- [ ] T174 Gate 4 (Architectural) — filter ready

## Phase 17: Convergence (restored — implemented by prior session)
- [X] T175 [P] [US2] Failing fail-closed lock test (red; subagent)
- [X] T176 [US2] Fail-closed guard fix (`?? true` removed; comment corrected)
- [X] T177 [P] [US3] Failing overlay-renderer test (red; subagent)
- [X] T178 [US3] Overlay renderer fix (floor + suppress fabricated)
- [ ] T179 [P] [US2/US3] Clean lint / mark full status
- [ ] T180 [US2/US4] Execute Gate verification when SMAPI available

### Verification (Phase 17 gates)
- [ ] T181 Gate 1 — format
- [ ] T182 Gate 2 — build
- [ ] T183 Gate 3 — tests
- [ ] T184 Gate 4 — architectural

## Phase 18: Convergence
- [X] T185 [P] [US2/US3] Clean pre-existing test-file lint (import order + xUnit1013) and finalize T156–T178 verification — `LivingRoots.Tests/` — per FR-015 / Constitution III / T170 — (partial)
- [X] T186 [P] [US2/US4] Execute Gate 1–4 (T171–T174 / T181–T184) when `STARDIEW_GAME_PATH` available — stub-bounded attestation for SC-005/SC-010 — (partial)
