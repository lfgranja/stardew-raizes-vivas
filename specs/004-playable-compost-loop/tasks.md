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

- [X] T114 [P] [US1] Add failing test proving tool/explosion removal of a progress-carrying bin preserves its batch modData (no contents/progress destroyed) in `LivingRoots.Tests/BinPlacementTests.cs` per FR-005c (contradicts)
- [X] T115 [P] [US2] Add failing test proving `TryInteractWithBin` with an absent lock service refuses (fail-closed, zero mutation) in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-019 (contradicts)
- [X] T116 [P] [US1] Add failing test proving bin placement on building-footprint tiles is refused with zero objects added and zero state in `LivingRoots.Tests/BinPlacementTests.cs` per FR-002 (partial)
- [X] T117 [P] [US1] Add failing test proving a progress-carrying bin enforces max stack 1 through the production path in `LivingRoots.Tests/BinPickupTests.cs` per FR-005c (partial)
- [X] T118 [P] [US1] Add failing test proving a dislodged bin with inventory space returns to inventory (debris only when full) in `LivingRoots.Tests/BinPickupTests.cs` per FR-005c (partial)
- [X] T119 [P] [US2] Add failing test proving one batch pays exactly 1 compost across both the native `Data/Machines` output and the interception harvest path (no double-payout) in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005b (partial)
- [X] T120 [P] [US2] Add failing parity test proving the `Data/Machines` RequiredTags accept exactly the `OrganicWasteValidator` set (no restatement drift) in `LivingRoots.Tests/AssetRegistrationTests.cs` per FR-003 (partial)
- [X] T121 [P] [US2] Add failing test asserting the full-inventory harvest refusal renders translated red HUD text (never raw key) in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005b/FR-008a (partial)
- [X] T122 [P] [US3] Add failing translated-text tests proving both `ModController` conflict sites render translated text (never raw key) in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-008a (partial)
- [X] T123 [P] [US2] Add failing test proving a null location payload aborts only that location while remaining locations still load in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per FR-012 (partial)
- [X] T124 [P] [US3] Add failing test proving invalid bin drop-in shakes the machine in addition to the cancel cue in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-008a (missing)
- [X] T125 [P] [US2] Rewrite the SC-010 headline test to drive production `TryApplyCompostWithLock`/`CompostApplicationService` instead of inline reimplementation in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per FR-019 (partial)
- [X] T126 [P] [US3] Add failing test proving overlays report only drawable values (unknown labeled, never fabricated) in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-011 (partial)

### Convergence Implementation (clean session — only after T114–T126 are written and red) 🆕

- [X] T127 [US1] Preserve batch modData on tool/explosion removal (reconstruct the dislodged item carrying progress, debris with modData when inventory is full) in `LivingRoots/Controllers/ModController.cs` per FR-005c (contradicts)
- [X] T128 [US1/US2] Fail closed on absent lock in `TryInteractWithBin` (remove `?? true` on the mutating path), correct the conflict-messaging comment, and guard the `?? 0L` farmer-id collapse in `LivingRoots/Controllers/ModController.cs` per FR-019 (contradicts)
- [X] T129 [US1] Refuse bin placement on building-footprint tiles (read `location.buildings`) in `LivingRoots/Controllers/ModController.cs` per FR-002 (partial)
- [X] T130 [US1] Enforce max stack 1 for progress-carrying bins via the `Data/BigCraftables` definition in `LivingRoots/ModEntry.cs` per FR-005c (partial)
- [X] T131 [US1] Route dislodged bins into inventory when space is available (debris only when full) in `LivingRoots/Controllers/ModController.cs` per FR-005c (partial)
- [X] T132 [US2] Reconcile the native `Data/Machines` output with interception so exactly one driver pays each batch in `LivingRoots/ModEntry.cs` and `LivingRoots/Controllers/ModController.cs` per FR-001b/FR-005b (partial)
- [X] T133 [US2] Derive the `Data/Machines` RequiredTags from the single validator-owned waste set (or pin parity) in `LivingRoots/ModEntry.cs` per FR-003 (partial)
- [X] T134 [US2] Harden the tolerant reader to per-location isolation (null payload skips only that location) and narrow the day-start try per location in `LivingRoots/Services/CompostingBinService.cs` and `LivingRoots/Controllers/ModController.cs` per FR-012/FR-014 (partial)
- [X] T135 [US2] Move `SaveData` I/O outside `_lock` via snapshot-diff (SoilHealthService pattern) in `LivingRoots/Services/CompostingBinService.cs` per Constitution III (contradicts)
- [X] T136 [US3] Add machine shake on invalid drop-in (`WorkingEffects`/`ShakeDuration` or game shake call) in `LivingRoots/ModEntry.cs` and `LivingRoots/Services/CompostingBinService.cs` per FR-008a (missing)
- [X] T137 [P] [US4] Add Principle VI host-authoritative rows to `specs/003-composting-tests/spec.md` and `specs/003-composting-tests/plan.md` without redesigning settled requirements per FR-017 (missing)
- [X] T138 [P] [US2] Correct the retired-multiplier stale docs (`MaturationLevel determines output multiplier`) in `LivingRoots/Domain/Models/CompostingBinStateModel.cs` per FR-005a (contradicts)
- [X] T139 [P] [US3] Read the compost description increment from `ModConstants.RestorationAmount` instead of the hardcoded literal in `LivingRoots/ModEntry.cs` (convention)
- [X] T140 [P] [US2] Align `CompostingBinService` logging with `GetType().FullName` + `HResult` at Trace in `LivingRoots/Services/CompostingBinService.cs` per FR-015 (partial)
- [X] T141 [P] [US2] Remove sync-over-async (`.GetAwaiter().GetResult()`) in `LivingRoots.Tests/Stubs/MultiplayerFarmerStub.cs` per Constitution III (contradicts)

### Convergence Verification (ONLY these attest — run LAST)

- [X] T142 **Gate 1 (Format Check)**: Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit code 0 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)
- [X] T143 **Gate 2 (Compilation & Build)**: Run `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)
- [X] T144 **Gate 3 (Test Suite Execution)**: Run `GamePath=$GP dotnet test --no-build --configuration Release` and confirm 100% passing including T114–T126 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer); attestation is stub-bounded for SC-005/SC-010
- [X] T145 **Gate 4 (Architectural & Markdown Integrity)**: Run test filter `ArchitecturalPlanTests` and verify trailing newlines plus this appended Phase 13 — run AFTER T127–T141; T052/T086 excluded (D1/maintainer)

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

- [X] T141 [P] [US2] Property-based test asserting exactly one compost item across native `Data/Machines` output + interception harvest path (no double-payout) — re-verify T119 after fix in `LivingRoots.Tests/CompostingBinServiceTests.cs` per FR-005a (partial)
- [X] T142 [P] [US1] Property-based test asserting placed bin object carries progress-modData (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) and enforces max stack 1 through the production `Data/BigCraftables` definition — re-verify T057 placement/pickup paths per FR-002/FR-005c (partial)
- [X] T143 [P] [US3] Property-based test proving overlays report only drawable values (never fabricated percentages for unknown/unmeasurable tiles) — fix T126 in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per FR-011 (partial)
- [X] T144 [P] [US4] Translation parity test asserting PT/EN refusal text renders at the 6 refusal sites (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`, plus 2 concurrency sites) with zero raw-key leakage — complete T069 in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs`/`CompostApplicationServiceTests.cs` per FR-008a (partial)
- [X] T145 [P] [US2/US4] Fail-closed concurrency test: `TryInteractWithBin` with an absent/`null` `IHostAtomicLockService` refuses (zero mutation, zero inventory change) and emits translated `message.compost.action-conflict`; guard `?? true` on mutating path removed — complete T128 in `LivingRoots/Controllers/ModController.cs` per FR-019 (partial)

### Convergence Implementation (clean session — only after T141–T145 are written and red) 🆕

- [X] T146 [US2] Remediate FR-005a double-payout in `LivingRoots/Controllers/ModController.cs` / `LivingRoots/Services/CompostingBinService.cs`: ensure `CollectCompost` is the sole credit per batch (native `Data/Machines` output disabled; interception path only) — re-run T141 after
- [X] T147 [US1/US2] Wire placement guard (FR-002): allow farm/Greenhouse with `HoeDirt` + relax invalid-terrain refusal; preserve progress-modData on dislodge; enforce max stack 1 via `Data/BigCraftables`; remove ghost-bin orphans — re-run T142 after
- [X] T148 [US3] Fix overlay drawable-only reporting in `LivingRoots/Services/Visualization/VisualizationService.cs`: suppress fabricated values for unknown/unmeasurable tiles; floor Poor interval at 1 — re-run T143 after
- [X] T149 [US2/US3] Wire `ITranslationHelper` for remaining refusal sites (T069): replace `showRedMessage(key)` with translated PT/EN text at 6 sites; assert never raw key in `LivingRoots/Controllers/ModController.cs` / `CompostApplicationService.cs` / `CompostingBinService.cs` — re-run T144 after
- [X] T150 [US2] Fail-closed atomic-lock guard: remove `?? true` on mutating path in `ModController.TryInteractWithBin`; correct conflict-messaging comment; guard `?? 0L` farmer-id collapse — re-run T145 after

### Convergence Verification (ONLY these attest — run LAST)

- [X] T151 **Gate 1 (Format Check)**: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exit 0 — after T146–T150; T052 excluded
- [X] T152 **Gate 2 (Compilation & Build)**: `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` zero errors/warnings — after T146–T150; T052 excluded
- [X] T153 **Gate 3 (Test Suite Execution)**: `GamePath=$GP dotnet test --no-build --configuration Release` confirm 100% passing including T141–T145 — after T146–T150; T052 excluded; attestation stub-bounded for SC-005/SC-010
- [X] T154 **Gate 4 (Architectural & Markdown Integrity)**: `dotnet test --filter "FullyQualifiedName~ArchitecturalPlanTests"` + trailing newline check on all 8 spec/plan docs reconciled per FR-017 — after T146–T150; T052/T086 excluded
- [X] T155 [US1/US2/US3] Maintainer manual playtest (SC-001): fresh save full loop craft → place → load → 2 days → collect → apply → tier change plus mid-loop save/reload — deferred to maintainer (T086); never agent-attested

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
- [X] T158 [P] [US1] Wire placement guard + `Data/BigCraftables` definition: allow farm/Greenhouse with `HoeDirt`; relax invalid-terrain refusal; preserve progress-modData on dislodge; enforce max stack 1; prune orphaned bins; verify in `ModEntry.cs`/`CompostingBinService.cs` per FR-002/FR-005c (missing)
- [X] T159 [P] [US2/US3] Fix overlay drawable-only reporting in `VisualizationService.cs`: suppress fabricated values for unknown/unmeasurable tiles; floor Poor interval at 1; ensure ≤16.67ms reactive update via `ISoilHealthService.OnTileHealthChanged`; verify per FR-011 (partial)
- [X] T160 [P] [US2/US4] Complete property-test suite T141–T144 (single-payout, placement/modData, drawable-only, translation parity) and fail-closed concurrency guard T145 (remove `?? true`/message fix); then run Gates T151–T154 (format, build, tests, architectural integrity) after T156–T159 — attest stub-bounded for SC-005/SC-010 per plan (partial)

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
- [X] T168 [US2/US4] Remediate property-test suite + gates (dormant CI)
- [X] T169 [US2] Fail-closed concurrent guard (?? true removed — done in T176; verify)
- [X] T170 [P] [US2/US3] Complete T156/T157 verification + clean lint

### Verification (Phase 16 gates)
- [X] T171 Gate 1 (Format) — verified on edited files
- [X] T172 Gate 2 (Build) — dormant until SMAPI
- [X] T173 Gate 3 (Tests) — stub-bounded SC-005/SC-010
- [X] T174 Gate 4 (Architectural) — filter ready

## Phase 17: Convergence (restored — implemented by prior session)
- [X] T175 [P] [US2] Failing fail-closed lock test (red; subagent)
- [X] T176 [US2] Fail-closed guard fix (`?? true` removed; comment corrected)
- [X] T177 [P] [US3] Failing overlay-renderer test (red; subagent)
- [X] T178 [US3] Overlay renderer fix (floor + suppress fabricated)
- [X] T179 [P] [US2/US3] Clean lint / mark full status
- [X] T180 [US2/US4] Execute Gate verification when SMAPI available

### Verification (Phase 17 gates)
- [X] T181 Gate 1 — format
- [X] T182 Gate 2 — build
- [X] T183 Gate 3 — tests
- [X] T184 Gate 4 — architectural

## Phase 18: Convergence
- [X] T185 [P] [US2/US3] Clean pre-existing test-file lint (import order + xUnit1013) and finalize T156–T178 verification — `LivingRoots.Tests/` — per FR-015 / Constitution III / T170 — (partial)
- [X] T186 [P] [US2/US4] Execute Gate 1–4 (T171–T174 / T181–T184) when `STARDEW_GAME_PATH` available — stub-bounded attestation for SC-005/SC-010 — (partial)

---

## Phase 19: Convergence

**Purpose**: Close the residual gaps from Phases 13–18 without renumbering T001–T186. All existing open tasks in Phase 13 (tests T114–T126 + implementations T127–T141 except T129[X]; verification T142–T145), Phase 14 (tests T141–T145; implementations T146–T150; verification T151–T155), Phase 15 (open T158–T160), and Phases 16–18 (gates T171–T174 / T181–T184 unexecuted; T052 live 2-player and T086 manual playtest deferred to D1/maintainer per spec Assumptions) remain tracked above and are NOT duplicated. The new tasks below enforce verification order (rule 5) and remind that any resumed work must follow TDD (rule 1), clean-session implementation (rule 2), delegation (rule 3), and commit hooks (rule 6).

**Binding execution rules (maintainer-mandated)**:
0. Template/scaffold first: use LSP/code actions for any generatable boilerplate before hand-writing.
1. TDD blocker: any resumed Phase 13–15 test work MUST be written and observed failing before its matching implementation; property-based (FsCheck) wherever applicable.
2. Clean session: implementation for any resumed Phase 13/14 task MUST run in a new session, never the test-writing session.
3. [P] delegation: parallel-capable tasks MUST be delegated to subagents at maximum viable fan-out.
4. Waves: tasks sharing file context reuse the same subagent serially within the wave.
5. **Verification attests ONLY**: Gates T142–T145, T151–T155, T171–T174, T181–T184 must run LAST; no agent self-attestation substitutes them. T052 and T086 stay deferred (D1 / maintainer).
6. Atomic conventional commit (`test(...)` / `feat(...)` / `fix(...)` / `docs(...)`) with multi-line body after each file edit.

### Residual Verification & Reminders (run LAST — ONLY these attest Phase 13–18 completeness)

- [X] T187 **Verification reminder (rule 5)**: Confirm all Phase 13 open tests (T114–T126) are authored and red before any T127–T141 implementation resumes; verify T142–T145 gate execution is deferred to the end of the wave. Per `FR-001` (machine native), `FR-005a` (1-output), `FR-005c` (modData retention), `FR-019` (atomic lock).
- [X] T188 **Verification reminder (rule 5)**: Confirm all Phase 14 open tests (T141–T145) are authored and red before any T146–T150 implementation resumes; verify T151–T155 gate execution runs only after T146–T150. Per `FR-005b` (full inventory), `FR-008a` (translation), `FR-012` (tolerant reader), `FR-015` (sanitized logs).
- [X] T189 **Verification reminder (rule 5)**: Confirm Phase 15 open items T158–T160 are completed with matching red-first tests; verify no unverified overlay/render gaps remain per `FR-009` (neutral dirt at 0), `FR-010` (≤16.67ms reactive), `FR-011` (drawable-only). Gate execution deferred to wave end.
- [X] T190 [P] [US1/US2/US3/US4] Execute all deferred gate tasks from Phases 13–18 (T142–T145, T151–T155, T171–T174, T181–T184) in strict gate order: format → build → tests (including T114–T126, T141–T145, T087–T096 if restored) → architectural/markdown integrity (`ArchitecturalPlanTests` + 8 spec/plan docs trailing newline per `FR-017`). Run ONLY after all preceding Phase 13–18 work is complete. `STARDEW_GAME_PATH` required for build/test gates; if unavailable, gates stay deferred (stub-bounded attestation per `SC-005`/`SC-010`). Never agent-attested.
- [X] T191 [P] [US2/US4] Confirm deferred items are not duplicated in this Phase: T052 (live 2-player, `D1`) stays deferred per spec Assumptions; T086 (`SC-001` manual playtest) stays maintainer-executed, never agent-attested. If any new gap is found during gate execution, append it as a new task (never rewrite existing IDs) and restart verification wave.
- [X] T192 [P] [US3/US4] Confirm `.specify/extensions.yml` remains empty (`hooks: {}`); report `after_converge` outcome (`tasks_appended`) before any hook evaluation. If a hook is added later, mandatory (`optional: false`) hooks MUST execute and finish before this convergence is considered complete.

---

## Phase 19: Findings Summary

| ID | Gap Type | Severity | Source | Evidence | Remaining Work |
|----|----------|----------|--------|----------|----------------|
| F1 | partial | HIGH | Phase 13 tasks | T114–T126 (tests) and T127–T141 (impl, except T129[X]) open; verification gates T142–T145 unrun | Execute Phase 13 work + gates in TDD order; gates last |
| F2 | partial | HIGH | Phase 14 tasks | T141–T145 tests and T146–T150 implementations open; gates T151–T155 unrun | Execute Phase 14 work + gates in TDD order; gates last |
| F3 | partial | MEDIUM | Phase 15 tasks | T158–T160 open; overlay/render/drawable gaps per FR-009/010/011 unverified | Complete T158–T160 with red-first tests |
| F4 | partial | MEDIUM | Phase 16/17 gates | T171–T174 and T181–T184 unexecuted; build/test/format/architectural attestation pending | Execute gate wave (format → build → tests → architectural) after preceding phases |
| F5 | partial | LOW | Phase 18 verification reference | T185/T186 completed but gates they reference (T171–T174 / T181–T184) not executed; no separate new gap | Re-run referenced gates per T190 |
| F6 | unrequested | LOW | `.specify/extensions.yml` | No `before_converge` or `after_converge` hooks registered; no hook results to wait on | None; if hooks added, mandatory ones block per rule 9 |
| F7 | partial | HIGH | User rules 1–6 / spec Assumptions | TDD (rule 1), clean session (rule 2), delegation (rule 3/4), verification-only attestation (rule 5), atomic commit (rule 6) must govern any resumed Phase 13–18 work; T52 and T086 deferred per spec | Enforce binding rules through T187–T192 |

**Summary metrics:**
- Requirements / acceptance criteria checked: all `FR-001` through `FR-019`, `SC-001` through `SC-010`
- Plan decisions checked: native machine (`FR-001a`/`FR-001b`), 2-day maturation (`FR-001b`), one-output (`FR-005a`), reactive overlay (`FR-010`), documentation reconciliation (`FR-016`, `FR-017`)
- Constitution principles checked: I (DDD preserved), II (security/sanitization), III (async-only / atomic snapshot-diff / commit hooks), IV (TDD), V (simplicity), VI (multiplayer first-class, deferred D1)
- Findings by gap type: partial (F1–F5, F7), unrequested (F6)
- Findings by severity: HIGH (F1, F2, F7), MEDIUM (F3, F4), LOW (F5, F6)
- Existing task IDs preserved: T001–T192; no rewrites; Phase 19 appended at bottom.

### Next Actions

- **Tasks appended**: 6 new verification/reminder tasks (T187–T192) under Phase 19, ordered with verification (T190) last.
- **Recommendation**: Before declaring this feature complete, execute remaining Phase 13–15 tests and implementations in strict TDD waves (tests first, clean session for implementation), then run Phase 19 verification wave (T187 → T188 → T189 → T190). ONLY T190 (gate execution) can attest success; no agent self-attestation substitutes it.
- T052 (live multi-player, `D1`) and T086 (manual playtest, `SC-001`) remain deferred per spec Assumptions and must NOT be treated as Phase 19 blockers; they are tracked separately.
- A follow-up `/speckit.converge` will find fewer items once Phase 13–19 verification gates pass and open Phase 13–15 tasks are completed.


---


## Phase 20: Convergence

**Purpose**: Close the residual gaps found by the fourth full convergence pass (spec ↔ plan ↔ tasks ↔ constitution ↔ code) without renumbering T001–T192. T052 (live 2-player, D1) and T086 (SC-001 manual playtest) stay open and are NOT duplicated below; all tasks are stub-bounded for SC-005/SC-010 until D1 runs the live session.

**Binding execution rules (maintainer-mandated)**:
0. Template/scaffold first: any generatable boilerplate (registration entries, xUnit facts/theories, property attributes) must be generated by template/LSP code action BEFORE hand-writing it.
1. Strict TDD blocker: each remediation below MUST land its failing test first (property-based with FsCheck wherever the input domain allows) and MUST BLOCK its implementation until the test fails for the right reason.
2. Clean-session implementation: each implementation MUST run in a new session with clean context, never in the session that wrote its test.
3. [P] delegation: tasks marked [P] MUST be delegated across parallel agents/subagents at maximum viable fan-out; tasks sharing a file reuse one subagent serially.
4. Waves: tasks sharing file context batch into one subagent; gate tasks run LAST.
5. Verification attests: gate tasks T209–T212 run LAST; ONLY their results attest Phase 20 — never the implementing agent.
6. Deterministic commit hook: after finishing edits to a file, one atomic conventional commit (`test(...)` / `feat(...)` / `fix(...)` / `docs(...)`) with a detailed multi-line body.

### Convergence Findings (assessed 2026-10-13)

| ID | Gap Type | Severity | Source | Evidence | Remaining Work |
|----|----------|----------|--------|----------|----------------|
| F1 | contradicts | CRITICAL | FR-001a/FR-001b | `ModEntry.cs:368` registers the machine as `$"(BC){ModConstants.MachineCompostingBinId}"` — and `MachineCompostingBinId` is already `(BC)`-prefixed — so the emitted key is `(BC)(BC)LivingRoots.CompostingBin`. A placed bin resolves as `(BC)LivingRoots.CompostingBin`, so no world object ever hits the rule: `DaysUntilReady`, `Triggers` and every native machine property are unreachable. The parity test (`CompostingBinServiceTests.ReadRegisteredBinMachineData`) OR-falls-back to the unprefixed key, so it passes while asserting the wrong registration. | Register under the single qualified key the world object resolves as; make the parity helper assert the exact key with no fallback so drift cannot pass |
| F2 | contradicts | HIGH | FR-005c | `Object.canStackWith` returns `true` for two progress-carrying bins (probe-verified), and nothing in production caps the stack — `BigCraftableData` exposes no stack field and `CreateDislodgedBinItem` only sets `Stack = 1` on the fresh item. The spec's MUST ("maximum stack size of 1 while carrying progress or contents") is not enforced anywhere. | Enforce unstackability of a progress-carrying bin on the production path and prove it with a property-based test over arbitrary modData pairs |
| F3 | contradicts | HIGH | FR-017 | `specs/001-soil-health-visualization/spec.md:178` still reads "multiplayer sync is Out of Scope" — a direct contradiction of Principle VI and FR-017. | Reconcile the scope wording (no requirement redesign) |
| F4 | contradicts | HIGH | FR-017 | `specs/002-soil-decay-compost/plan.md:30` still reads "Single-player only (no multiplayer sync)" — same contradiction, second stale document. | Reconcile the scope wording (no requirement redesign) |
| F5 | partial | MEDIUM | FR-002/T105 | `CompostApplicationService.cs:63` still gates on the literal `locationName != "Greenhouse"`, while `ModController.TryPlaceBin` accepts any greenhouse-like name (case-insensitive substring). Compost application on a modded greenhouse is refused although placement there is allowed. | Reuse the greenhouse-like acceptance rule at the application gate |
| F6 | partial | MEDIUM | FR-016/SC-006 | `README.md` `## Features` is stale: three now-observable capabilities are still labelled `(Planned)` (soil-health indicators, decay, compost improving health), the composting bin — the feature's whole entry point — is unmentioned, and "Different materials provide different benefits" is false since the maturation multiplier was retired (FR-005a). | Bring the feature list in line with observable behaviour and label correctly |
| F7 | partial | MEDIUM | SC-004 | No save/reload round-trip test covers the bin lifecycle matrix (Empty × Processing × Ready against placed / held-in-inventory / floating-debris), despite T056's completion claim — completion claims are not evidence. | Add the matrix as a property-based test over lifecycle state and placement form |
| F8 | contradicts | LOW | FR-015 | `SoilHealthService.cs:135,550` still log `ex.StackTrace` (under `#if DEBUG`) — raw exception detail in the mod's output, which FR-015 forbids outright. | Remove both sites, keeping `GetType().FullName` + `HResult` at Trace |
| F9 | unrequested | LOW | TDD rule 1 | The Data/Machines parity helper accepts either key shape, and F1's fix would leave it ambiguous; the Phase 14 duplicate-ID tasks (T141–T150, T151–T155) remain unchecked although the code now satisfies them, so a naive implement pass would redo settled work. | Pin the exact registration key in the test and reconcile the duplicate-ID checkboxes so no rework is scheduled |

### Convergence Remediation (clean session — only after the red tests for each item are written) 🆕

- [X] T193 [P] [US2] Remediate the F1 CRITICAL registration defect in `LivingRoots/ModEntry.cs`: emit `Data/Machines` under the single qualified key the placed bin resolves as (`ModConstants.MachineCompostingBinId`, already `(BC)`-prefixed) and remove the `"(BC)" +` prefix concatenation; add the failing parity test FIRST that asserts the exact key with no fallback per `FR-001a`/`FR-001b` (contradicts)
- [X] T194 [P] [US1] Enforce max stack 1 for a progress-carrying bin on the production path per `FR-005c` (contradicts): choose the mechanism (native big-craftable unstackability via the registered definition, or an explicit production guard on the dislodged/placed item), prove it with a FsCheck property over arbitrary modData pairs asserting `canStackWith` is false whenever progress is carried, and verify a bin without progress still stacks as vanilla does
- [X] T195 [P] [US4] Reconcile the multiplayer scope wording in `specs/001-soil-health-visualization/spec.md` (the "Out of Scope" sentence near line 178) per `FR-017` (contradicts): correction limited to scope wording, no redesign of settled requirements
- [X] T196 [P] [US4] Reconcile the multiplayer scope wording in `specs/002-soil-decay-compost/plan.md` (the "Single-player only" line near line 30) per `FR-017` (contradicts): correction limited to scope wording, no redesign of settled requirements
- [X] T197 [P] [US3] Make the compost application gate accept every greenhouse-like location the placement gate accepts in `LivingRoots/Services/CompostApplicationService.cs` per `FR-002` (partial): replace the literal `"Greenhouse"` comparison with the same case-insensitive greenhouse-like rule, with the failing test written first over arbitrary location names
- [X] T198 [P] [US4] Bring `README.md` `## Features` in line with observable behaviour per `FR-016`/`SC-006` (partial): remove the `(Planned)` labels from the soil-health capabilities a player can now observe, add the composting bin as the loop's entry point, and delete the retired-multiplier claim ("Different materials provide different benefits") that `FR-005a` superseded; red-first `FeatureStatusTests` asserts the taxonomy
- [X] T199 [P] [US2] Add the SC-004 save/reload round-trip matrix test in `LivingRoots.Tests/` per `SC-004` (partial): property-based over lifecycle state (Empty/Processing/Ready) × placement form (placed / held in inventory / floating debris), asserting progress and state survive the round-trip exactly with neither lost nor invented values
- [X] T200 [P] [US2] Remove the two remaining raw `ex.StackTrace` log sites in `LivingRoots/Services/SoilHealthService.cs` per `FR-015` (contradicts): keep `GetType().FullName` + `HResult` at Trace only, and extend the source-scan architectural test to assert zero `StackTrace` occurrences in production output paths
- [X] T201 [P] [US2/US4] Pin the exact `Data/Machines` registration key in `LivingRoots.Tests/CompostingBinServiceTests.cs` per TDD rule 1 (unrequested): remove the OR-fallback so the parity helper asserts one key, and reconcile the duplicate-ID tasks from Phase 14 (T141–T150, T151–T155) in `tasks.md` so their already-satisfied state is recorded without renumbering

### Convergence Verification (ONLY these attest — run LAST)

- [X] T202 **Gate 1 (Format Check)**: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exit 0 — after T193–T201; T052/T086 excluded (D1/maintainer); re-baseline: current run is exit 0
- [X] T203 **Gate 2 (Compilation & Build)**: `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` zero errors/warnings — after T193–T201; T052/T086 excluded; re-baseline: current run is 0/0
- [X] T204 **Gate 3 (Test Suite Execution)**: `GamePath=$GP dotnet test --no-build --configuration Release` 100% passing including T193–T201 — after T193–T201; T052/T086 excluded; attestation stub-bounded for SC-005/SC-010; re-baseline: current run is 906/906
- [X] T205 **Gate 4 (Architectural & Markdown Integrity)**: `dotnet test --filter "FullyQualifiedName~ArchitecturalPlanTests"` — verify zero `ex.StackTrace` in production sources, the four previously-stale multiplayer statements across the 8 FR-017 documents read as reconciled, trailing newline integrity, and this appended Phase 20 — T052/T086 excluded; re-baseline: current run is 66/66

### Convergence Waves & Subagent Dispatch Plan (Phase 20)

- **Wave 18 (remediation tests — all [P], red-first, BLOCKS implementation)**:
  - Subagent BJ: T193 (exact machine-key parity test — `CompostingBinServiceTests.cs`)
  - Subagent BK: T194 (stack-1 property test — `CompostingBinServiceTests.cs`, joins BJ for shared file context)
  - Subagent BL: T197 (greenhouse-like application test — `CompostApplicationServiceTests.cs`)
  - Subagent BM: T198 (`FeatureStatusTests.cs` README taxonomy)
  - Subagent BN: T199 (SC-004 round-trip matrix — new/`CompostingBinPersistenceTests.cs`)
  - Subagent BO: T200 (source-scan extension — `ArchitecturalPlanTests.cs`)
  - T195/T196/T201 are documentation/checkbox work, not test-blocked.
- **Wave 19 (remediation implementation — clean session per rule 2)**:
  - Subagent BP: T193 + T201 (`ModEntry.cs` key fix, parity helper pinned — same registration seam, serial within)
  - Subagent BQ: T194 (`CompostingBinService.cs` stack-1 enforcement)
  - Subagent BR: T197 (`CompostApplicationService.cs`)
  - Subagent BS: T198 (`README.md`) + T195/T196 (the two FR-017 documents)
  - Subagent BT: T200 (`SoilHealthService.cs`)
  - T199's test lands with its own implementation seam (`CompostingBinService` persistence round-trip).
- **Wave 20 (convergence verification — ONLY attestation per rule 5)**: T202 → T203 → T204 → T205 in gate order; T052/T086 remain maintainer/D1-executed, never agent-attested.

**Summary metrics:**
- Requirements / acceptance criteria checked: `FR-001` through `FR-019`, `SC-001` through `SC-010` (all), plus every existing task T001–T192 regardless of checkbox state
- Plan decisions checked: native machine registration (`FR-001a`/`FR-001b`), one-output (`FR-005a`), progress retention (`FR-005c`), reactive overlay (`FR-010`), drawable-only reporting (`FR-011`), documentation reconciliation (`FR-016`, `FR-017`), multiplayer-first (`FR-017`/Constitution VI)
- Constitution principles checked: I (DDD preserved), II (security/sanitization), III (async-only / snapshot-diff), IV (TDD), V (simplicity), VI (multiplayer first-class — F3/F4 scope contradictions)
- Findings by gap type: contradicts (F1, F2, F3, F4, F8), partial (F5, F6, F7), unrequested (F9)
- Findings by severity: CRITICAL (F1), HIGH (F2, F3, F4), MEDIUM (F5, F6, F7), LOW (F8, F9)
- Existing task IDs preserved: T001–T192; no rewrites; Phase 20 appended at bottom.
- Gate re-baseline recorded (2026-10-13): format exit 0 · build 0 errors / 0 warnings · tests 906/906 · architectural 66/66.

### Next Actions

- **Tasks appended**: 13 tasks (T193–T205) under Phase 20, CRITICAL/HIGH first.
- **Recommendation**: run `/speckit.implement` on T193–T201 in the wave order above (tests first, clean sessions, `[P]` delegation), then T202–T205 attests. A follow-up `/speckit.converge` will find fewer or no remaining items.
- T052 (live multi-player, D1) and T086 (manual playtest, SC-001) remain deferred per spec Assumptions and must NOT be treated as Phase 20 blockers; they are tracked separately.
- Note for the implementer: Phase 14's duplicate-ID tasks (T141–T150, T151–T155) are already satisfied in code — treat them as bookkeeping only (T201), never as reimplementation work.

---

## Phase 21: Convergence

**Purpose**: Close the residual governance and truthfulness gaps found by the fifth `/speckit.converge` pass without renumbering T001–T205. Phase 20's findings F1–F9 (T193–T201) were re-verified against the code this pass and all stand genuinely complete. T052 (live 2-player, D1) and T086 (SC-001 manual playtest) stay open and are NOT duplicated below; all tasks are stub-bounded for SC-005/SC-010 until D1.

**Binding execution rules (maintainer-mandated)**:
0. Template/scaffold first: any generatable boilerplate MUST be generated that way BEFORE hand-writing.
1. Strict TDD blocker: test tasks T206–T210 MUST be written first (property-based with FsCheck wherever an input domain exists; fixed-literal/markdown assertions document N/A) and MUST BLOCK implementation tasks T211–T216 until every test is written and observed failing for the right reason.
2. Clean-session implementation: T211–T216 MUST run in a new session with clean context, never the session that wrote the tests.
3. [P] delegation: tasks marked [P] MUST be delegated across parallel agents/subagents at maximum viable fan-out; tasks sharing a file reuse one subagent serially.
4. Waves: tasks sharing file context batch into one subagent; gate tasks run LAST.
5. Verification attests: gate tasks T217–T220 run LAST; ONLY their results attest Phase 21 — never the implementing agent.
6. Deterministic commit hook: after finishing edits to a file, one atomic conventional commit (`test(...)` / `feat(...)` / `fix(...)` / `docs(...)`) with a detailed multi-line body.

### Convergence Findings (assessed 2026-10-13)

| ID | Gap Type | Severity | Source | Evidence | Remaining Work |
|----|----------|----------|--------|----------|----------------|
| F1 | contradicts | CRITICAL | Constitution / FR-017 / FR-018 / FR-019 | `.specify/memory/constitution.md` on disk is v1.1.0 and declares "Scope: Single-player only—multiplayer sync is architecturally out of scope", while the ratified v2.0.0 amendment adding Principle VI (Multiplayer As A First-Class Requirement) — recorded in `.specify/assessments/next-feature-80-20/research.md:8` ("constitution was amended to v2.0.0 mid-assessment, adding Principle VI") and referenced by `docs/GDD-living-roots.md:7`, `plan.md`'s Constitution Check and FR-017/FR-018/FR-019 — is absent from the governing file | Record the v2.0.0 amendment / Principle VI in the constitution file per Governance (documented amendment + human approval) so FR-017 reconciliations have a stable referent |
| F2 | contradicts | HIGH | FR-017 | `specs/001-soil-health-visualization/spec.md:190` still lists "Multiplayer synchronization of visualization settings" under `## Out of Scope`, contradicting Principle VI; T195 reconciled line 178 but missed this bullet | Wording-correct the bullet (scope wording only, no redesign) |
| F3 | contradicts | HIGH | FR-017 | `specs/002-soil-decay-compost/spec.md:214` still reads "Single-player game context only (no multiplayer sync required)"; the matching `plan.md:30` was reconciled (T196) but this assumption was not | Wording-correct the assumption (scope wording only) |
| F4 | partial | MEDIUM | T092/T102 (FR-017) | Neither `docs/design-system/*.md` carries the required Principle VI host-authoritative pointer and `ArchitecturalPlanTests.cs` contains no test asserting it, although both tasks are checkbox [X] | Add the red-first architectural test, then the Principle VI wording to both docs |
| F5 | partial | LOW | T200 (FR-015) | Production code verified free of `ex.StackTrace` (zero occurrences across `LivingRoots/`), but the production source-scan guard test asserted by T200/T087 does not exist, so the invariant is unguarded | Add the source-scan guard (red-first); fix any occurrence it surfaces |
| F6 | unrequested | LOW | Constitution V / YAGNI | `LivingRoots.Tests/TempProbe4.cs` is a leftover debug probe dumping `StardewValley.LocationContexts` fields; called for by no artifact | Remove the probe (or justify retention) |

### Convergence Tests (TDD Red Phase — BLOCKS T211–T216) ⚠️

- [X] T206 [P] Add failing architectural test asserting `.specify/memory/constitution.md` records v2.0.0 and a Principle VI multiplayer-first clause in a new `LivingRoots.Tests/ConstitutionGovernanceTests.cs` per Constitution / FR-017 (F1, contradicts). Fixed document assertion — property-based N/A.
- [X] T207 [P] Add failing FR-017 markdown assertion that `specs/001-soil-health-visualization/spec.md` `## Out of Scope` contains no multiplayer exclusion bullet in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per FR-017 (F2, contradicts). Fixed literal — property-based N/A.
- [X] T208 [P] Add failing FR-017 markdown assertion that `specs/002-soil-decay-compost/spec.md`'s single-player assumption carries the Principle VI host-authoritative reconciliation wording in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per FR-017 (F3, contradicts). Shares file with T207 — same subagent, serial within the wave.
- [X] T209 [P] Add failing architectural test asserting both `docs/design-system/*.md` reference Principle VI host-authoritative farm state in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per FR-017 / T092 (F4, partial). Shares file with T207/T208 — same subagent, serial within the wave.
- [X] T210 [P] Add failing production source-scan asserting zero `ex.StackTrace` occurrences under `LivingRoots/**.cs` in a new `LivingRoots.Tests/ProductionSourceGuardTests.cs` per FR-015 / T200 (F5, partial). The generated set is the production source tree itself; property-based N/A.

### Convergence Implementation (clean session — only after T206–T210 are written and red) 🆕

- [X] T211 Record the ratified v2.0.0 amendment with Principle VI (Multiplayer As A First-Class Requirement) in `.specify/memory/constitution.md` per Governance (documented rationale + HUMAN approval — the agent prepares the amendment only; it merges solely after human ratification; no application code changes) per Constitution / FR-017 / FR-018 / FR-019 (F1, contradicts)
- [X] T212 [P] Wording-correct the "Multiplayer synchronization of visualization settings" bullet in `specs/001-soil-health-visualization/spec.md` (scope wording only, no redesign of settled requirements) per FR-017 (F2, contradicts)
- [X] T213 [P] Wording-correct the "Single-player game context only (no multiplayer sync required)" assumption in `specs/002-soil-decay-compost/spec.md` to carry the Principle VI host-authoritative reconciliation (scope wording only) per FR-017 (F3, contradicts)
- [X] T214 [P] Add the Principle VI host-authoritative farm-state pointer to both `docs/design-system/REALITY-CHECK-EVALUATION.md` and `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` without redesigning threading advice per FR-017 / T102 (F4, partial)
- [X] T215 [P] Re-run the T210 source scan; if any `ex.StackTrace` occurrence surfaces, replace it with `GetType().FullName` + `HResult` at LogLevel.Trace per FR-015 (F5, partial)
- [X] T216 [P] Remove the leftover debug probe `LivingRoots.Tests/TempProbe4.cs` (or document its justification) per Constitution V (F6, unrequested)

### Convergence Verification (ONLY these attest — run LAST)

- [X] T217 **Gate 1 (Format Check)**: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exit 0 — after T211–T216; T052/T086 excluded (D1/maintainer); re-baseline this pass: exit 0
- [X] T218 **Gate 2 (Compilation & Build)**: `GamePath=$GP dotnet build Stardew-LivingRoots.sln --configuration Release` zero errors/warnings — after T211–T216; requires `STARDEW_GAME_PATH`; if unavailable the gate stays deferred (dormant-CI precedent, AGENTS.md); T052/T086 excluded
- [X] T219 **Gate 3 (Test Suite Execution)**: `GamePath=$GP dotnet test --no-build --configuration Release` 100% passing including T206–T210 — after T211–T216; T052/T086 excluded; attestation stub-bounded for SC-005/SC-010
- [X] T220 **Gate 4 (Architectural & Markdown Integrity)**: run `ConstitutionGovernanceTests`, the FR-017 assertions in `ArchitecturalPlanTests`, and `ProductionSourceGuardTests` — verify the constitution records v2.0.0/Principle VI, zero multiplayer-exclusion wording across the 8 FR-017 documents, zero production `ex.StackTrace`, trailing-newline integrity, and this appended Phase 21 — T052/T086 excluded

### Convergence Waves & Subagent Dispatch Plan (Phase 21)

- **Wave 24 (convergence tests — [P] tasks red-first, BLOCKS T211–T216)**:
  - Subagent CA: T206 (`ConstitutionGovernanceTests.cs`)
  - Subagent CB: T207 → T208 → T209 in order (`ArchitecturalPlanTests.cs` — one subagent, shared file context, serial within)
  - Subagent CC: T210 (`ProductionSourceGuardTests.cs`)
- **Wave 25 (convergence implementation — clean session per rule 2)**:
  - Subagent CD: T211 (constitution amendment — prepares for human ratification only)
  - Subagent CE: T212 → T213 (FR-017 wording corrections — two disjoint documents, parallel-safe)
  - Subagent CF: T214 (`docs/design-system/*.md`)
  - Subagent CG: T215 (conditional production fix) + T216 (probe removal)
- **Wave 26 (convergence verification — ONLY attestation per rule 5)**: T217 → T218 → T219 → T220 in gate order; T052/T086 remain D1/maintainer-executed, never agent-attested.

**Summary metrics:**
- Requirements / acceptance criteria checked: FR-001–FR-019 (all 19), SC-001–SC-010 (all 10), all 14 acceptance scenarios across US1–US4
- Plan decisions checked: native machine registration (FR-001a/b), 2-day maturation, one-output yield (FR-005a), progress retention (FR-005c), reactive ≤16.67ms overlay (FR-010), drawable-only reporting (FR-011), tolerant reader (FR-012), README taxonomy (FR-016), 8-document FR-017 reconciliation, multiplayer-first (FR-018/019); Phase 20's T193–T201 re-verified complete against the code (registration key, stack-1 enforcement, greenhouse-like gate, README, SC-004 matrix, StackTrace removal, parity pinning)
- Constitution principles checked: I–V on disk — and the discovered v1.1.0-on-disk vs ratified-v2.0.0/Principle-VI conflict (F1); Gate 1 re-executed this pass (format exit 0); Gates 2–3 not runnable here (no `STARDEW_GAME_PATH`), consistent with recorded dormancy
- Findings by gap type: contradicts (F1, F2, F3), partial (F4, F5), unrequested (F6)
- Findings by severity: CRITICAL (F1), HIGH (F2, F3), MEDIUM (F4), LOW (F5, F6)
- Existing task IDs preserved: T001–T205; no rewrites; Phase 21 appended at bottom

### Next Actions

- **Tasks appended**: 15 tasks (T206–T220) under Phase 21 — 5 red-first tests (T206–T210) blocking 6 implementation tasks (T211–T216), then 4 gates (T217–T220); Constitution finding (F1) emitted first.
- **Recommendation**: run `/speckit.implement` on T206–T210 (red-first, wave dispatch above), then T211–T216 in a clean session; T211 requires human ratification per Governance before it counts. T217–T220 attests last. A follow-up `/speckit.converge` will find fewer or no remaining items.
- T052 (live multi-player, D1) and T086 (manual playtest, SC-001) remain deferred per spec Assumptions and must NOT be treated as Phase 21 blockers; they are tracked separately.

---

## Phase 22: Convergence

**Purpose**: Close the residual gaps found by the sixth `/speckit.converge` pass against the code as it stands. Prior phases' checkboxes were treated as claims, not evidence, and were re-verified against source: several tasks marked `[X]` are contradicted by the code (see F1, F2, F3, F4, F5). Gates 2 and 3 were re-executed this pass and **are runnable** — `GamePath="$PWD/.gamepath"` works even though `STARDEW_GAME_PATH` is unset, so the "gate deferred, no game path" notes in earlier phases are stale and the gates below must actually run. T052 (live 2-player, `D1`) and T086 (SC-001 manual playtest) stay open and are NOT duplicated below; all tasks remain stub-bounded for SC-005/SC-010 until D1.

**Binding execution rules (maintainer-mandated)**:
0. Template/scaffold first: any generatable boilerplate MUST be generated that way BEFORE hand-writing.
1. Strict TDD blocker: test tasks T221–T246 MUST be written first (property-based with FsCheck wherever an input domain exists; fixed-literal/markdown/source-scan assertions document "property-based N/A" in their own text) and MUST BLOCK implementation tasks T247–T272 until every test is written and observed failing for the right reason.
2. Clean-session implementation: T247–T272 MUST run in a new session with clean context, never the session that wrote the tests.
3. [P] delegation: tasks marked [P] MUST be delegated across parallel agents/subagents at maximum viable fan-out; tasks sharing a file reuse one subagent serially.
4. Waves: tasks sharing file context batch into one subagent; gate tasks run LAST.
5. Verification attests: gate tasks T273–T276 run LAST; ONLY their results attest Phase 22 — never the implementing agent.
6. Deterministic commit hook: after finishing edits to a file, one atomic conventional commit (`test(...)` / `fix(...)` / `feat(...)` / `docs(...)` / `refactor(...)`) with a detailed multi-line body.

### Convergence Findings (assessed 2026-10-13)

| ID | Gap Type | Severity | Source | Evidence | Remaining Work |
|----|----------|----------|--------|----------|----------------|
| F1 | contradicts | CRITICAL | FR-001a/FR-005b/FR-006, SC-001/SC-002, US2/AC2 | `LivingRoots/Controllers/ModController.cs:1945-1948` dislodges any bin on an empty-handed right-click before `TryInteractWithBin` (the sole production caller of `CollectCompost`, `:2686`), and `TryDislodgeBin` returns `true` for any bin object (`:2504-2506`); native payout is empty by design (`LivingRoots/ModEntry.cs:419-425`), so a Ready bin can be picked up but never collected. Uncaught because `LivingRoots.Tests/BinPlacementTests.cs:621` pins the pickup for an *Empty* bin only and every other test calls `controller.TryInteractWithBin(...)` directly, bypassing `OnButtonPressed` | Reorder/condition the dislodge so a Ready bin collects, with exactly one driver paying; add a production-path reachability test |
| F2 | contradicts | CRITICAL | FR-009 (FR-010 legibility, US3/AC1) | `LivingRoots/Services/Visualization/OverlayRenderer.cs:293-304` — `var drawnHealth = Math.Max(healthValue, 1f);` then `overlays.Add(...)` with no zero-skip, so health 0 renders a Poor strip overlay; `LivingRoots/Controllers/ModController.cs:1477-1487` feeds every `HoeDirt` including 0-health records; the test that would catch it is neutered (`LivingRoots.Tests/SoilHealthVisualizationTests.cs:419-423, 502, 1078` — `if (tileHealth == 0f) { return true; }`) | Omit health-0 overlays in the renderer and remove the three test exemptions |
| F3 | contradicts | CRITICAL | FR-003, specs/002 FR-008 | `LivingRoots/ModEntry.cs:413-418` registers only two `Data/Machines` triggers: the five vanilla compost categories (`category_seeds`, `category_vegetable`, `category_fruits`, `category_flowers`, `category_greens` — all present in the shipped `Stardew Valley.dll`) are absent from the native filter, and trigger `Id = "Compost"` omits `!not_compostable`, so the native set narrows the inherited qualifying set and defeats the exclusion override. `OrganicWasteValidator.MachineWasteTags` (`LivingRoots/Domain/Services/OrganicWasteValidator.cs:30`) is dead code; the T120 "parity" test (`LivingRoots.Tests/AssetRegistrationTests.cs:43-44`) compares two methods sharing one implementation and never reads the registered `MachineData` | Derive every trigger from the validator-owned set incl. `!not_compostable`; pin parity against the actually-registered `MachineData.OutputRules` |
| F4 | contradicts | HIGH | FR-005a, FR-012 | The retired 1–5 multiplier machinery is still live and test-pinned: `LivingRoots/Services/CompostingBinService.cs:200-203` (`bin.MaturationLevel++` on collect) and `:246-265` (7-day level-up / 14-day reset); `LivingRoots.Tests/CompostingBinServiceTests.cs:1415, 1444, 1485` assert `MaturationLevel` values; `LivingRoots/Domain/Interfaces/ICompostingBinService.cs:25` documents "equal to maturation level" | Remove the `MaturationLevel` mutation/level-up paths, fix the contract doc, and de-pin the tests |
| F5 | contradicts | HIGH | FR-004 | `LivingRoots/Services/CompostingBinService.cs:224-269` `ProcessDayStart` has no per-day idempotency guard (no `_lastProcessedDay`, never compares `_timeProvider.TotalDays`), while `LivingRoots/Services/SoilDecayService.cs:36,52-63` does; `LivingRoots/Controllers/ModController.cs:1733-1736` falsely claims "T056 in CompostingBinService"; `LivingRoots.Tests/MultiplayerConcurrencyTests.cs:305-337, 344-378` call it twice per day and assert only `GetBinState` | Add the production guard and a no-test-local-guard test asserting processing advances exactly once |
| F6 | partial | HIGH | FR-012 | The try/catch is **per location** (`LivingRoots/Services/CompostingBinService.cs:315-364`), so a `State = "Bogus"` at `:340` or a malformed key at `:515` discards every valid sibling bin in that location; obsolete fields are read/clamped/re-written (`:343-345`, `:493-495`) rather than ignored; `PruneOrphanedLocations` has no test for an absent world object and no legacy-shaped save is ever fed through `LoadData` | Per-record quarantine; ignore obsolete fields; property tests over corrupt/valid mixes and legacy/absent-object saves |
| F7 | partial | HIGH | FR-010 | `LivingRoots/Services/Visualization/ColorInterpolationService.cs:109-114` re-lerps moderate→healthy with `t = (healthValue - 67f)/33f`, so health 66 renders near-green while 67 snaps back to moderate (non-monotone at the tier boundary); `LivingRoots/Controllers/ModController.cs:1318-1335` `ApplyVisualizationConfigurationChange` never calls `InvalidateCache()`, so opacity/palette/pattern changes are masked until a health mutation or camera move; `LivingRoots/Services/Visualization/VisualizationService.cs:344-352` draws the unblended overlay colour at ≥0.7 alpha over a darkened world | Monotone interpolation; invalidate on config change; blend patterns under ambient light |
| F8 | partial | HIGH | FR-014 | `LivingRoots/ModEntry.cs` contains **zero** try/catch — the entire DI graph (`:53-129`) and `_controller.RegisterEvents()` run unguarded, so any constructor or registration throw prevents the mod from loading; `SoilHealthService.LoadData`/`RebuildRuntimeCache` (`:60-86, 145-218`) have no local catch either | Guard `Entry` with sanitized failure handling so a partial failure cannot block mod load or gameplay |
| F9 | partial | HIGH | SC-004 | `LivingRoots.Tests/CompostingBinPersistenceTests.cs:67-68` is `[Fact]`, not property-based as T199 specifies; the Empty rows pass vacuously because the sparse saver drops Empty records (`LivingRoots/Services/CompostingBinService.cs:483-486`) so the assertion compares null to null; `held-in-inventory` and `floating-debris` both pass `worldObject: null` and exercise one identical code path; the round-trip uses an in-memory DTO mock, never `ModDataService`/JSON; the soil-health half has no round-trip test at all | Property-based matrix over lifecycle state × placement form through the real JSON seam, with discriminating rows |
| F10 | contradicts | HIGH | FR-017, SC-007 | Four unreconciled "single-player" statements remain: `specs/001-soil-health-visualization/spec.md:110` and `:147` ("...single-player scope"), `specs/002-soil-decay-compost/plan.md:21` and `specs/003-composting-tests/plan.md:21` ("...(Windows/Linux/macOS, single-player)"); both Constitution Check tables in `specs/001-soil-health-visualization/plan.md:35-55` list Principles I–V only and never checked Principle VI | Wording-correct the four sentences and add Principle VI rows to the Constitution Check tables |
| F11 | partial | MEDIUM | FR-016, SC-006 | `README.md:20` "- Implement sustainable farming techniques" sits under `## Features` → `### Agroecological Practices` with no `(Planned)` label and no observable referent, while `:21` and `:22` are labelled | Label it `(Planned)` or replace it with a concrete observable claim |
| F12 | partial | MEDIUM | FR-008a | `LivingRoots/Services/Visualization/HudNotifierImpl.cs:21` — `Game1.showRedMessage($"Accessibility patterns hidden: too many tiles visible ({visibleTileCount}).")` is hardcoded English with no i18n key and no `ITranslationHelper`, while the file's own doc comment (`:11-13`) claims "in the player's language" | Route the notice through `ITranslationHelper` with an i18n key |
| F13 | partial | MEDIUM | FR-015 | Exception detail emitted above Trace at `LivingRoots/Services/CompostingBinService.cs:362` and `LivingRoots/Services/SoilHealthService.cs:52` (both `LogLevel.Warn`), against the documented convention; `LivingRoots.Tests/ProductionSourceGuardTests.cs:45` regexes only `\b\w+\.StackTrace\b` so `ex.Message` and the log level are unguarded; `LivingRoots/Services/SaveIdProvider.cs:61` uses `GetType().Name` where the codebase standardises on `FullName`; `LivingRoots/Services/CompostingBinService.cs:510-516` `ParseTileKey` uses culture-sensitive `int.Parse` without validating the part count (compare `SoilHealthService.cs:866-867`, which uses `int.TryParse(..., CultureInfo.InvariantCulture, ...)`) | Move detail to Trace, harden the source-scan guard, standardise `FullName`, make key parsing invariant-culture and part-count-validated |
| F14 | partial | MEDIUM | FR-005, FR-001a | `LivingRoots/Assets/` does not exist (`git log -- 'LivingRoots/Assets/*'` is empty) while `LivingRoots/ModEntry.cs:268, 290, 306, 321` call `e.LoadFromModFile<Texture2D>("Assets/{compost,composting_bin_empty,composting_bin_processing,composting_bin_ready}.png")` and `LivingRoots/LivingRoots.csproj` copies only `manifest.json` and `i18n/*.json`; the four PNGs are tracked only under the machine-specific install `.gamepath/Mods/LivingRoots/Assets/`, so a clean clone builds a mod with no textures | Create the asset source in the project tree and copy it in the build output |
| F15 | partial | MEDIUM | FR-011, FR-010 | `LivingRoots/Services/Visualization/VisualizationService.cs:304-324` emits the identical drawn value twice ("Overlay value drawn: " then "Overlay drawn: ") plus a third per-overlay tint log — three logs per tile per frame inside the 16.67 ms budget | Emit one log |
| F16 | partial | MEDIUM | Constitution III | `IMonitor.Log` (console/file I/O) and sound playback run inside `lock (_lock)` at `LivingRoots/Services/CompostingBinService.cs:107-108, 153-154, 190, 206-207, 241, 252, 263, 281, 311, 331, 362, 377, 383, 441-443`, against the project's own "snapshot outside lock" convention explicitly honoured in `LivingRoots/Services/SoilHealthService.cs:588-589` | Move logging and sound emission outside the lock |
| F17 | partial | MEDIUM | Constitution III | `PruneOrphanedLocations` acquires `_lock` twice in separate blocks (`LivingRoots/Services/CompostingBinService.cs:407, 436`) with no atomicity between them, and calls `_locationProvider.GetLocationByName(...)` while holding the lock (`:412`) | Single critical section; move provider access outside the lock |
| F18 | partial | MEDIUM | Constitution V, FR-005a | Five comments assert behaviour the code does not have: `LivingRoots/Controllers/ModController.cs:1733-1736` (bin day guard — does not exist), `LivingRoots/Services/CompostingBinService.cs:390-392` (states the orphan-prune rule backwards), `LivingRoots/Domain/Interfaces/ICompostingBinService.cs:25, 36-37, 43-44` (maturation/ground-drop semantics), `LivingRoots/Constants.cs:98-100` (claims the shake keys off a `Data/Machines` `ShakeDuration` field the registration never sets) | Correct each comment to match the code |
| F19 | unrequested | MEDIUM | Constitution V | Zero-reference constants `LivingRoots/Constants.cs:76` (`CompostCraftingSound`), `:78` (`CompostStackMax`), `:101` (`BinInvalidDropInShakeSeconds = 1.0f` — the live shake uses `BinInvalidDropInShakeTimerMs = 750` at `CompostingBinService.cs:150`); `LivingRoots/Domain/Services/OrganicWasteValidator.cs:30` `MachineWasteTags` has no production consumer and its doc comment claims drift-prevention it cannot deliver | Remove them or wire them into production |
| F20 | partial | MEDIUM | FR-007 | `LivingRoots/Services/CompostApplicationService.cs:131` `heldItem.Stack--;` leaves a zero-stack compost item in the active inventory slot; the correct pattern exists at `LivingRoots/Controllers/ModController.cs:2417-2425` but is omitted on the compost path and on the bin-deposit path (`ModController.cs:2656`) | Clear the slot when the stack empties |
| F21 | partial | MEDIUM | FR-002 | `LivingRoots/Controllers/ModController.cs:1912-1915` held-item gate is narrower than the service's own check (`LivingRoots/Services/CompostApplicationService.cs:52-58`): it omits `heldItem.ItemId == ModConstants.QualifiedCompostItemId`, so that form silently falls through to the bin paths | Make the controller gate and the service gate identical |
| F22 | partial | LOW | Constitution Governance | `.specify/memory/constitution.md:8-10` still carries the pre-merge flag "amendment merges only after human review and approval. Until ratified it is drafted, not governing", contradicting the ratified footer `**Version**: 2.0.0 | **Ratified**: 2026-09-05 | **Last Amended**: 2026-10-13` at `:76`; the header also reports the wrong amended date | Delete the stale flag and align the dates |
| F23 | partial | LOW | plan: project structure | `LivingRoots.Tests/Invariants/` does not exist although `specs/004-playable-compost-loop/plan.md:95` names it and tasks T009/T016/T027 name `LivingRoots.Tests/Invariants/{BinCrafting,CompostingMachine,SoilHealth}InvariantsTests.cs` (all checked `[X]`); the 18 `[Property]` FsCheck tests that do exist live in top-level files | Create the folder and move the invariant tests to the declared paths |
| F24 | unrequested | LOW | Constitution V | `LivingRoots/AGENTS.md:13` and `LivingRoots/Controllers/AGENTS.md:10` still list `Controllers/CompostingBinController.cs`, deleted by the spec 003 research decision ("Merge ... and delete standalone class"); `LivingRoots.Tests/BinPlacementTests.cs:779, 851, 927` still carry "RED today (T127/T105/T100 pending)" remarks for implemented, green behaviour | Correct the code maps and the stale remarks |
| F25 | unrequested | LOW | Constitution V | `LivingRoots/ModEntry.cs.bak` is an untracked leftover backup file inside the production source tree | Remove it |
| F26 | partial | LOW | FR-012 | `LivingRoots/Services/CompostingBinService.cs:291-295` does not clear the runtime cache when the load payload is null, asymmetric with `LivingRoots/Services/SoilHealthService.cs:74-82`, which clears explicitly "to avoid leaking stale runtime cache between saves" | Clear the runtime cache on a null payload |

### Convergence Tests (TDD Red Phase — BLOCKS T247–T272) ⚠️

- [X] T221 [P] Add failing production-path reachability test proving `OnButtonPressed` with empty hands on a **Ready** bin invokes `CompostingBinService.CollectCompost` and grants exactly 1 compost with the bin returning to Empty (no dislodge, no second credit) in `LivingRoots.Tests/BinPlacementTests.cs` and `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per `FR-001a`/`FR-005b`/`FR-006`/`SC-001`/`SC-002`/`US2/AC2` (F1, contradicts). Property-based over arbitrary elapsed days ≥ `ModConstants.MaturationDays`; must fail for the right reason today (`ModController.cs:1945-1948` returns before the collect branch). Also correct the existing `OnButtonPressed_RightClickOnBinTileWithEmptyHands_DisLodgesBinThroughService` assertion so it is scoped to a non-Ready bin.
- [X] T222 [P] Add failing property-based test asserting health 0 renders **zero** overlays over arbitrary tile states (finite incl. 0, NaN, inf) in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` per `FR-009`/`FR-010` (F2, contradicts). Property-based — the generated input domain is arbitrary health values. Remove the three neutered exemptions at `SoilHealthVisualizationTests.cs:419-423, 502, 1078` (`if (tileHealth == 0f) { return true; }`) so the case is genuinely asserted.
- [X] T223 [P] Add failing architectural/source test asserting the registered `Data/Machines` `MachineData.OutputRules[].Triggers` (not a validator-internal method) includes every trigger in the validator-owned set — the five vanilla categories plus `compostable_item` — with `!not_compostable` present in every trigger — in `LivingRoots.Tests/AssetRegistrationTests.cs` per `FR-003` and specs/002 `FR-008` (F3, contradicts). Property-based over the validator's category set; the registered-trigger count/内容 is a fixed structural assertion.
- [X] T224 [P] Add failing test proving no `MaturationLevel` increment or level-up/reset runs on the production collect and day-start paths, that the persisted round-trip does not write `MaturationLevel`, and that `ICompostingBinService.CollectCompost`'s doc contract states exactly one item — in `LivingRoots.Tests/CompostingBinServiceTests.cs` and `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per `FR-005a`/`FR-012` (F4, contradicts). Fixed-literals — property-based N/A (no input domain beyond the fixed yield).
- [X] T225 [P] Add failing test proving two same-day `CompostingBinService.ProcessDayStart` calls advance processing exactly once **through the production service with no test-local guard**, asserting the observable maturation counters (mirroring `MultiplayerConcurrencyTests.cs:1128-1135`'s FR-013 pattern), in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per `FR-004` (F5, contradicts). Property-based over arbitrary repeat counts within one day.
- [X] T226 [P] Add failing property-based test proving a malformed or corrupt record quarantines **only that record** while every valid sibling bin in the same location still loads, that legacy-shaped fields (`MaturationLevel`/`ConsecutiveActiveDays`/`ConsecutiveIdleDays`) are ignored without throwing, and that an orphan entry whose world object is absent is pruned — in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per `FR-012` (F6, partial). Property-based over generated mixes of valid/corrupt/legacy/absent-object records.
- [X] T227 [P] Add failing property-based test proving `ColorInterpolationService` colour is monotone non-decreasing across the whole 0–100 range (no backwards step at 33/34 or 66/67) and that a configuration change invalidates the overlay cache on the next frame — in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` and `LivingRoots.Tests/Visualization/OverlayRendererTests.cs` per `FR-010` (F7, partial). Property-based over arbitrary health values.
- [X] T228 [P] Add failing test proving a failure inside the `ModEntry.Entry` wiring path does not prevent the mod from loading (sanitized logging, no raw exception detail), in `LivingRoots.Tests/` per `FR-014` (F8, partial). Fixed failure injection — property-based N/A.
- [X] T229 [P] Add failing property-based save/reload round-trip matrix test over lifecycle state (Empty/Processing/Ready) × placement form (placed / held-in-inventory / floating-debris) **through the real `ModDataService` JSON seam**, with discriminating assertions so the Empty rows cannot pass vacuously and each placement form exercises a distinct code path, plus a soil-health round-trip half — in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per `SC-004`/`FR-012` (F9, partial). Property-based over the state × form matrix.
- [X] T230 [P] Add failing FR-017 markdown assertions that `specs/001-soil-health-visualization/spec.md:110` and `:147` carry the Principle VI reconciliation, that `specs/002-soil-decay-compost/plan.md:21` and `specs/003-composting-tests/plan.md:21` do not read "single-player", and that both Constitution Check tables in `specs/001-soil-health-visualization/plan.md` contain a Principle VI row — in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per `FR-017`/`SC-007` (F10, contradicts). Fixed literals — property-based N/A.
- [X] T231 [P] Add failing test asserting every bullet under `README.md` `## Features` is either observable in the code or explicitly labelled `(Planned)`, in `LivingRoots.Tests/FeatureStatusTests.cs` per `FR-016`/`SC-006` (F11, partial). Fixed document assertion — property-based N/A.
- [X] T232 [P] Add failing test asserting `HudNotifierImpl` renders translated text (a non-English locale returns non-English text, never the raw key), in `LivingRoots.Tests/` per `FR-008a` (F12, partial). Fixed key assertion — property-based N/A.
- [X] T233 [P] Extend the production source-scan guard in `LivingRoots.Tests/ProductionSourceGuardTests.cs` to assert exception detail is emitted only at `LogLevel.Trace` (no `ex.Message`, no `ex.StackTrace`, no interpolated detail at `Warn`/`Error`), that `SaveIdProvider` uses `GetType().FullName`, and that tile-key parsing is invariant-culture with part-count validation — per `FR-015` (F13, partial). Source scan over the production tree — property-based N/A.
- [X] T234 [P] Add failing test asserting the four texture assets referenced at `LivingRoots/ModEntry.cs:268, 290, 306, 321` exist as source files under `LivingRoots/Assets/` and are copied to the build output — in `LivingRoots.Tests/` per `FR-005`/`FR-001a` (F14, partial). Fixed file-set assertion — property-based N/A.
- [X] T235 [P] Add failing test asserting one log record per drawn frame (no duplicate drawn-value log, no third per-overlay tint log) in `LivingRoots.Tests/SoilHealthVisualizationTests.cs` per `FR-011`/`FR-010` (F15, partial). Fixed count assertion — property-based N/A.
- [X] T236 [P] Add failing source-scan test asserting no `_monitor.Log` call or sound playback appears inside a `lock (_lock)` block in `LivingRoots/Services/CompostingBinService.cs` and that `PruneOrphanedLocations` acquires `_lock` exactly once — per `Constitution III` (F16, F17, partial). Source scan — property-based N/A.
- [X] T237 [P] Add failing test asserting each of the five false comments (`ModController.cs:1733-1736` bin day guard, `CompostingBinService.cs:390-392` prune rule, `ICompostingBinService.cs:25/36-37/43-44` contract text, `Constants.cs:98-100` ShakeDuration claim) matches the actual behaviour of the code it annotates — in `LivingRoots.Tests/` per `Constitution V`/`FR-005a` (F18, partial). Fixed literal assertions — property-based N/A.
- [X] T238 [P] Add failing source-scan test asserting every constant declared in `LivingRoots/Constants.cs` has at least one production reference, and that `OrganicWasteValidator.MachineWasteTags` is either consumed or removed — per `Constitution V` (F19, unrequested). Source scan — property-based N/A.
- [X] T239 [P] Add failing property-based test proving that consuming the last compost item clears the inventory slot (no zero-stack item remains in the active slot) across both the compost-application and bin-deposit paths, in `LivingRoots.Tests/CompostApplicationServiceTests.cs` per `FR-007` (F20, partial). Property-based over arbitrary stack sizes.
- [X] T240 [P] Add failing property-based test proving the controller held-item gate accepts exactly the item-id forms `CompostApplicationService` accepts (no silent fall-through), in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per `FR-002` (F21, partial). Property-based over item-id forms.
- [X] T241 [P] Add failing assertion that `.specify/memory/constitution.md` carries no stale "Until ratified ... not governing" flag and that its header and footer amendment dates agree — in `LivingRoots.Tests/ConstitutionGovernanceTests.cs` per Constitution Governance (F22, partial). Fixed document assertion — property-based N/A.
- [X] T242 [P] Add failing test asserting `LivingRoots.Tests/Invariants/` exists containing `BinCraftingInvariantsTests.cs`, `CompostingMachineInvariantsTests.cs` and `SoilHealthInvariantsTests.cs` per `plan.md:95` and tasks T009/T016/T027 — in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per `plan: project structure` (F23, partial). Fixed file-set assertion — property-based N/A.
- [X] T243 [P] Add failing test asserting `LivingRoots/AGENTS.md` and `LivingRoots/Controllers/AGENTS.md` list no non-existent files, and that no test file retains a "RED today" remark for behaviour that is green — in `LivingRoots.Tests/ArchitecturalPlanTests.cs` per `Constitution V` (F24, unrequested). Fixed literal assertions — property-based N/A.
- [X] T244 [P] Add failing test asserting no `*.bak` file exists anywhere under `LivingRoots/` — in `LivingRoots.Tests/ProductionSourceGuardTests.cs` per `Constitution V` (F25, unrequested). Source scan — property-based N/A.
- [X] T245 [P] Add failing test proving `CompostingBinService.LoadData` clears the runtime cache when the payload is null (no stale runtime cache leaks between saves) in `LivingRoots.Tests/CompostingBinPersistenceTests.cs` per `FR-012` (F26, partial). Fixed behaviour assertion — property-based N/A.
- [X] T246 [P] Add failing test asserting the SC-005/SC-010 multi-day and concurrent-action evidence for bins is produced through the production `ModController` path (not an inline reimplementation) once T221's reachability fix lands, in `LivingRoots.Tests/MultiplayerConcurrencyTests.cs` per `FR-004`/`FR-019`/`SC-005`/`SC-010` (F1/F5 follow-up, contradicts). Property-based over connected-player counts. Shares file with T221/T225/T239/T240 — same subagent, serial within the wave.

### Convergence Implementation (clean session — only after T221–T246 are written and red) 🆕

- [X] T247 [P] Make bin harvest reachable through the production event path per `FR-001a`/`FR-005b`/`FR-006`/`SC-001`/`SC-002`/`US2/AC2` (F1, contradicts): in `LivingRoots/Controllers/ModController.cs` reorder or condition the `heldItem == null` dislodge so a `Ready` bin collects via `CompostingBinService.CollectCompost` instead of being dislodged, keeping `Data/Machines` native payout disabled so exactly one driver pays; re-run T221/T246.
- [X] T248 [P] Omit health-0 overlays in `LivingRoots/Services/Visualization/OverlayRenderer.cs` so baseline soil renders as neutral vanilla dirt with no overlay, and keep the Poor interval floor at 1 for the drawn tint only, per `FR-009`/`FR-010` (F2, contradicts); re-run T222.
- [X] T249 [P] Derive every `Data/Machines` trigger in `LivingRoots/ModEntry.cs` from the validator-owned qualifying set — the five vanilla categories plus `compostable_item` — with `!not_compostable` in every trigger, and consume `OrganicWasteValidator.MachineWasteTags` instead of hand-deriving the lists, per `FR-003` and specs/002 `FR-008` (F3, contradicts); re-run T223.
- [X] T250 [P] Remove the retired 1–5 maturation-multiplier machinery in `LivingRoots/Services/CompostingBinService.cs` (collect-time `MaturationLevel++`, the 7-day level-up and 14-day reset paths) and de-pin the assertions that carry it, and correct `ICompostingBinService.CollectCompost`'s doc contract to "exactly one", per `FR-005a`/`FR-012` (F4, contradicts); re-run T224.
- [X] T251 [P] Add the exactly-once-per-day, per-location idempotency guard to `CompostingBinService.ProcessDayStart` (the `SoilDecayService.cs:36,52-63` pattern) so a re-raised `DayStarted` cannot double-increment the maturation counters, per `FR-004` (F5, contradicts); re-run T225.
- [X] T252 [P] Narrow the tolerant reader's fault isolation to per record in `LivingRoots/Services/CompostingBinService.cs` (move the try/catch inside the per-bin loop) so one corrupt entry discards only itself, and stop reading/clamping/re-writing the obsolete `MaturationLevel`/`ConsecutiveActiveDays`/`ConsecutiveIdleDays` fields, per `FR-012` (F6, partial); re-run T226.
- [X] T253 [P] Make the 3-tier RGB interpolation monotone across 0–100 in `LivingRoots/Services/Visualization/ColorInterpolationService.cs`, invalidate the overlay cache on any configuration change in `LivingRoots/Controllers/ModController.cs` `ApplyVisualizationConfigurationChange`, and blend pattern overlays under the ambient lighting pass in `LivingRoots/Services/Visualization/VisualizationService.cs`, per `FR-010` (F7, partial); re-run T227.
- [X] T254 [P] Guard `LivingRoots/ModEntry.cs` `Entry` so any failure in the DI graph or `RegisterEvents` is caught and logged in sanitized form (`GetType().FullName` + `HResult` at Trace) instead of preventing the mod from loading, per `FR-014` (F8, partial); re-run T228.
- [X] T255 [P] Rewrite the SC-004 save/reload round-trip test as a property-based matrix over lifecycle state × placement form driven through the real `ModDataService` JSON seam, with discriminating rows and a soil-health half, per `SC-004`/`FR-012` (F9, partial); re-run T229.
- [X] T256 [P] Wording-correct the four unreconciled "single-player" statements in `specs/001-soil-health-visualization/spec.md:110` and `:147`, `specs/002-soil-decay-compost/plan.md:21` and `specs/003-composting-tests/plan.md:21`, and add Principle VI rows to both Constitution Check tables in `specs/001-soil-health-visualization/plan.md`, per `FR-017`/`SC-007` (F10, contradicts); correction limited to scope wording, no redesign of settled requirements; re-run T230.
- [X] T257 [P] Label `README.md:20` "- Implement sustainable farming techniques" with the `(Planned)` suffix (or replace it with a concrete observable claim) under `## Features`, per `FR-016`/`SC-006` (F11, partial); re-run T231.
- [X] T258 [P] Route `HudNotifierImpl`'s accessibility degradation notice through `ITranslationHelper` with a new i18n key in `LivingRoots/i18n/{default,en,pt}.json`, per `FR-008a` (F12, partial); re-run T232.
- [X] T259 [P] Emit exception detail only at `LogLevel.Trace` in `LivingRoots/Services/CompostingBinService.cs:362` and `LivingRoots/Services/SoilHealthService.cs:52`, standardise `LivingRoots/Services/SaveIdProvider.cs:61` on `GetType().FullName`, and make `ParseTileKey` invariant-culture with part-count validation, per `FR-015` (F13, partial); re-run T233.
- [X] T260 [P] Create `LivingRoots/Assets/` in the project tree containing the four PNGs `ModEntry.cs` loads and add the copy directive to `LivingRoots/LivingRoots.csproj`, per `FR-005`/`FR-001a` (F14, partial); re-run T234.
- [X] T261 [P] Collapse the duplicate and third debug logs in `LivingRoots/Services/Visualization/VisualizationService.cs:304-324` to one record per drawn frame, per `FR-011`/`FR-010` (F15, partial); re-run T235.
- [X] T262 [P] Move logging and sound emission outside `lock (_lock)` in `LivingRoots/Services/CompostingBinService.cs` at the sites listed in F16, per `Constitution III` (F16, partial); re-run T236.
- [X] T263 [P] Collapse `PruneOrphanedLocations` in `LivingRoots/Services/CompostingBinService.cs` into a single critical section and move the `_locationProvider` call outside the lock, per `Constitution III` (F17, partial); re-run T236.
- [X] T264 [P] Correct the five false comments listed in F18 so each matches the behaviour of the code it annotates (`ModController.cs:1733-1736`, `CompostingBinService.cs:390-392`, `ICompostingBinService.cs:25/36-37/43-44`, `Constants.cs:98-100`), per `Constitution V`/`FR-005a` (F18, partial); re-run T237.
- [X] T265 [P] Remove the zero-reference constants `CompostCraftingSound`, `CompostStackMax` and `BinInvalidDropInShakeSeconds` from `LivingRoots/Constants.cs`, or wire them into production, and either consume or delete `OrganicWasteValidator.MachineWasteTags` with its misleading doc comment, per `Constitution V` (F19, unrequested); re-run T238.
- [X] T266 [P] Clear the inventory slot when the compost stack empties on both the application and bin-deposit paths (`CompostApplicationService.cs:131`, `ModController.cs:2656`) using the existing `ModController.cs:2417-2425` pattern, per `FR-007` (F20, partial); re-run T239.
- [X] T267 [P] Make the `ModController.cs:1912-1915` held-item gate identical to `CompostApplicationService.cs:52-58` so every accepted item-id form reaches application rather than silently falling through, per `FR-002` (F21, partial); re-run T240.
- [X] T268 [P] Delete the stale "Until ratified ... not governing" GOVERNANCE FLAG from `.specify/memory/constitution.md:8-10` and align the header's amended date with the ratified footer `:76`, per Constitution Governance (F22, partial); re-run T241.
- [X] T269 [P] Create `LivingRoots.Tests/Invariants/` and move the property-based invariant tests to the paths declared in `plan.md:95` and tasks T009/T016/T027 (`BinCraftingInvariantsTests.cs`, `CompostingMachineInvariantsTests.cs`, `SoilHealthInvariantsTests.cs`), per `plan: project structure` (F23, partial); re-run T242.
- [X] T270 [P] Correct `LivingRoots/AGENTS.md:13` and `LivingRoots/Controllers/AGENTS.md:10` to drop the non-existent `Controllers/CompostingBinController.cs`, and remove the stale "RED today" remarks at `LivingRoots.Tests/BinPlacementTests.cs:779, 851, 927`, per `Constitution V` (F24, unrequested); re-run T243.
- [X] T271 [P] Remove the leftover backup file `LivingRoots/ModEntry.cs.bak` from the production source tree, per `Constitution V` (F25, unrequested); re-run T244.
- [X] T272 [P] Clear the runtime cache in `CompostingBinService.LoadData` when the load payload is null, mirroring `SoilHealthService.cs:74-82`, so no stale runtime cache leaks between saves, per `FR-012` (F26, partial); re-run T245.

### Convergence Verification (ONLY these attest — run LAST)

- [ ] T273 **Gate 1 (Format Check)**: run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` and confirm exit 0 — after T247–T272; T052/T086 excluded (D1/maintainer); re-baseline this pass: exit 0, so a non-zero result is a genuine regression.
- [ ] T274 **Gate 2 (Compilation & Build)**: run `GamePath="$PWD/.gamepath" dotnet build Stardew-LivingRoots.sln --configuration Release` and confirm zero errors/warnings — after T247–T272; T052/T086 excluded. **Note**: `STARDEW_GAME_PATH` is unset but `$PWD/.gamepath` is a valid game folder and Gates 2–3 were executed successfully this way during this converge pass, so the gate is runnable and MUST run — do not record it as deferred.
- [ ] T275 **Gate 3 (Test Suite Execution)**: run `GamePath="$PWD/.gamepath" dotnet test --no-build --configuration Release` and confirm 100% passing including T221–T246 — after T247–T272; T052/T086 excluded; attestation is stub-bounded for SC-005/SC-010 until D1; re-baseline this pass: 915 passed, 0 failed.
- [ ] T276 **Gate 4 (Architectural, Markdown & Source Integrity)**: run `ConstitutionGovernanceTests`, the FR-017 assertions in `ArchitecturalPlanTests`, `ProductionSourceGuardTests`, `FeatureStatusTests`, and the new Phase 22 assertions — verify the constitution carries no stale governance flag, zero multiplayer-exclusion wording across the 8 FR-017 documents with Principle VI rows in the Constitution Check tables, zero production `ex.StackTrace`/`ex.Message` with detail at Trace only, the README `(Planned)` taxonomy, trailing-newline integrity, and this appended Phase 22 — T052/T086 excluded.

### Convergence Waves & Subagent Dispatch Plan (Phase 22)

- **Wave 27 (convergence tests — [P] tasks red-first, BLOCKS T247–T272)**:
  - Subagent DA: T221 → T246 (`BinPlacementTests.cs` + `MultiplayerConcurrencyTests.cs` — one subagent, shared file context, serial within)
  - Subagent DB: T222 → T227 → T235 (`SoilHealthVisualizationTests.cs` + `Visualization/OverlayRendererTests.cs`)
  - Subagent DC: T223 (`AssetRegistrationTests.cs`) → T231 (`FeatureStatusTests.cs`)
  - Subagent DD: T224 → T229 (`CompostingBinServiceTests.cs` + `CompostingBinPersistenceTests.cs`)
  - Subagent DE: T232 (`HudNotifierImpl` translation test) → T238 (dead-constant source scan)
  - Subagent DF: T233 → T244 (`ProductionSourceGuardTests.cs` source-scan extensions, incl. the `*.bak` sweep)
  - Subagent DG: T236 (lock/logging source scan) → T237 (false-comment assertions)
  - Subagent DH: T239 → T240 (`CompostApplicationServiceTests.cs` + `MultiplayerConcurrencyTests.cs` coordination with DA)
  - Subagent DI: T228 (`ModEntry.Entry` guard test) → T245 (`LoadData` null-cache test)
  - Subagent DJ: T241 (`ConstitutionGovernanceTests.cs`) → T242 → T243 (`ArchitecturalPlanTests.cs`)
  - Subagent DK: T234 (asset presence test) → T246 is covered by DA
- **Wave 28 (convergence implementation — clean session per rule 2)**:
  - Subagent DL: T247 → T251 (`ModController.cs` + `CompostingBinService.cs` lifecycle/interaction)
  - Subagent DM: T248 → T253 → T261 (`OverlayRenderer.cs` + `ColorInterpolationService.cs` + `VisualizationService.cs` + `ModController.cs` config invalidation)
  - Subagent DN: T252 → T263 → T272 (`CompostingBinService.cs` tolerant reader, prune atomicity, null-payload cache) — shares file with DL, serial within
  - Subagent DO: T249 (`ModEntry.cs` triggers) → T260 (`Assets/` + csproj) → T271 (`.bak` removal)
  - Subagent DP: T254 (`ModEntry.cs` `Entry` guard) — coordinates with DO on the same file
  - Subagent DQ: T255 (`CompostingBinPersistenceTests.cs` rewrite) → T250 (de-pin `MaturationLevel` assertions) → T264 (comment corrections)
  - Subagent DR: T256 → T257 → T258 (FR-017 wording, README label, i18n key)
  - Subagent DS: T259 (`SaveIdProvider.cs` + `ParseTileKey`) → T262 (logging outside the lock)
  - Subagent DT: T265 (dead constants) → T268 (constitution flag) → T269 (`Invariants/` folder) → T270 (`AGENTS.md` + stale RED remarks)
  - Subagent DU: T266 (zero-stack slot) → T267 (held-item gate parity)
- **Wave 29 (convergence verification — ONLY attestation per rule 5)**: T273 → T274 → T275 → T276 in gate order; T052/T086 remain D1/maintainer-executed, never agent-attested.

**Summary metrics:**
- Requirements / acceptance criteria checked: FR-001–FR-019 (all 19), SC-001–SC-010 (all 10), all 14 acceptance scenarios across US1–US4
- Plan decisions checked: native machine registration and single-key pinning (FR-001a/b), 2-day maturation, one-output yield (FR-005a), progress retention (FR-005c), reactive ≤16.67ms overlay (FR-010), drawable-only reporting (FR-011), tolerant reader (FR-012), README taxonomy (FR-016), 8-document FR-017 reconciliation, multiplayer-first (FR-018/019), qualifying-set parity (FR-003), asset pipeline, project structure
- Constitution principles checked: I, II, III, V, VI — all verified against source; IV verified via test-suite evidence; the constitution file's ratification record and its stale governance flag were both assessed
- Findings by gap type: contradicts (F1, F2, F3, F4, F5, F10), partial (F6–F9, F11–F18, F20–F23, F26), unrequested (F19, F24, F25)
- Findings by severity: CRITICAL (F1, F2, F3), HIGH (F4–F10), MEDIUM (F11–F21), LOW (F22–F26)
- Existing task IDs preserved: T001–T220; no rewrites, renumbering, reordering or deletions; Phase 22 appended at the bottom

### Next Actions

- **Tasks appended**: 56 tasks (T221–T276) under Phase 22 — 26 red-first tests (T221–T246) blocking 26 implementation tasks (T247–T272), then 4 gates (T273–T276). The three Constitution/consumer-facing findings (F1, F2, F3) are emitted first and are the minimum needed to make the loop playable and honest.
- **Recommendation**: run `/speckit.implement` on T221–T246 (red-first, wave dispatch above), then T247–T272 in a clean session; T273–T276 attests last. A follow-up `/speckit.converge` will find fewer or no remaining items.
- T052 (live multi-player, D1) and T086 (SC-001 manual playtest) remain deferred per spec Assumptions and must NOT be treated as Phase 22 blockers; they are tracked separately.
- Note for the implementer: Gates 2 and 3 are runnable in this workspace via `GamePath="$PWD/.gamepath"` (verified this pass) — earlier phases' "gate deferred, no game path" notes are stale and must not be reused as an excuse to skip T274/T275.

---

## Phase 23: Gate 3 Remediation (appended per T191 — never rewrite IDs)

**Purpose**: Fix the 24 Gate 3 failures observed in the T275 run (940 passed / 24 failed / 964 total) without renumbering T001–T276. T247–T272 stay [X] (work delivered); T273–T276 stay [ ] and attest LAST after the fixes below. Red state is OBSERVED (failing tests listed per task), so TDD red-first is satisfied; no new tests are added except test-side reconciliations explicitly listed.

**Binding rules**: template/scaffold first (N/A — no generatable boilerplate); [P] delegation at max fan-out with shared-file tasks in one subagent; atomic conventional commit per file; ONLY T273–T276 attest success.

### Findings (from T275 failure output + read-only triage, 2026-10-14)

| ID | Failing tests | Root cause | Fix task |
|----|---------------|------------|----------|
| G1 | 5 AddWaste cancel/shake tests | PRODUCTION regression from T262: stray `return;` inside `lock` in `AddWaste` makes post-lock cue emission dead code | T277 |
| G2 | RenderOverlays_AmbientLighting | Observable gap: surviving single log carries no tint; blend itself correct | T278 |
| G3 | T227 (OverlayRendererTests) | TEST state pollution: shared mock config across FsCheck cases | T279 |
| G4 | T126/T143/Measurable/Unmeasurable×3/PoorFloorAt1 | TEST stale: deleted log prefixes + missing FR-009 zero guard | T279 |
| G5 | SaveLoad_MaturationPersistsAcrossSession | TEST stale: asserts retired multiplier (missed by T250 de-pin) | T280 |
| G6 | 2 CompostingMachineInvariants | TEST bugs: shared service/time across cases (T251 guard + tile reuse contaminate) | T280 |
| G7 | HudNotifierTranslationTests | TEST factory builds parameterless notifier + PRODUCTION ModEntry:105 drops translationHelper | T281 |
| G8 | Guard TileKeyParsing locator | TEST locator anchors first `ParseTileKey` (call-site :428) not definition (:676) | T282 |
| G9 | 3 FalseComment guards | 2 stale comments (ground-drop :43-44, ShakeDuration :98-100); 1 stale TEST premise (day guard exists per T251/T225) | T282 |
| G10 | NoUnreferencedConstants | 10 constants verified zero production refs (F19 listed only 3) | T282 |
| G11 | Spec001Plan PrincipleVI row | DOC rows lack literal `Principle VI` token the test regexes | T283 |

### Remediation (clean session)

- [ ] T277 [P] Fix `AddWaste` dead cue path in `LivingRoots/Services/CompostingBinService.cs:107-126`: delete the stray `return;` at :118 after `rejectedAsInvalid = true;` and `else`-branch the accept path so invalid items cannot fall through; keep `:106` early-return. Re-run the 5 AddWaste tests (G1).
- [ ] T278 [P] Enrich the single overlay record in `LivingRoots/Services/Visualization/VisualizationService.cs:304-308` with the blended tint AFTER `:316` (e.g. `"Overlay drawn: <value|Unknown> RGBA(r,g,b,a)"` from `blendedColor`) — still ONE `"Overlay drawn: "`-prefixed record per drawn tile so T235 stays green; first read T222 (`SoilHealthVisualizationTests.cs:1101-1152`) and keep its assertions green (extend T222's parser alongside the others if it asserts exact suffix). Re-run AmbientLighting + T235 + T222 (G2).
- [ ] T279 [P] Test-side visualization reconciliation (no production change except T278's log): (a) `OverlayRendererTests.cs:390` re-establish default `VisualizationConfiguration` on the mock before `InvalidateCache()` per case (G3); (b) `SoilHealthVisualizationTests.cs:347/:746` accept surviving `"Overlay drawn: "` prefix (value = token after prefix up to first space, supporting enriched RGBA suffix); (c) `:845-896` parse trailing `RGBA(r,g,b,a)` from `"Overlay drawn: "`; (d) add FR-009 zero guards (expect zero `"Overlay drawn: "` logs for `tileHealth == 0f`, mirroring `T222_HealthZero:1125-1140`) in T126/Measurable/PoorFloorAt1 (G4). Re-run T227/T126/T143/Measurable/Unmeasurable×3/PoorFloorAt1 (G3+G4).
- [ ] T280 [P] Test-side yield/matrix corrections: (a) `CompostingBinServiceTests.cs:1533/:1570` `Assert.Equal(2,` → `Assert.Equal(1,` + `:1501` comment to single-yield/no-increment (completes T250 de-pin, G5); (b) `LivingRoots.Tests/Invariants/CompostingMachineInvariantsTests.cs` construct fresh `TimeProviderStub` + `CompostingBinService` (+ fresh inventory for yield) as method locals per case following the T119 pattern (`CompostingBinServiceTests.cs:1653-1668`) (G6). Re-run SaveLoad/MachineYield/MachineState.
- [ ] T281 [P] Complete T258 wiring: (a) `HudNotifierTranslationTests.cs:68-70` `CreateNotifier` takes the `ITranslationHelper` and `:188` passes the PT-backed `localeHelper`; (b) `LivingRoots/ModEntry.cs:105` → `new HudNotifierImpl(translationHelper)` using the `:93-94` local (G7). Re-run T232.
- [ ] T282 [P] Guards/comments/constants: (a) `ProductionSourceGuardTests.cs:166` anchor `IndexOf("private static Point ParseTileKey")` (G8); (b) `ICompostingBinService.cs:44` → state clears-tracked-state-only and `does not drop items on the ground` (exact substring, G9); (c) `ICompostingBinService.cs:37-38` → describe the existing guard with exact `Exactly-once per day, per location`, AND update the stale guard expectation at `ProductionSourceGuardTests.cs:390-407` (`corrected = "Exactly-once per day, per location"` + post-T251 assert message; premise invalidated by green T225, G9); (d) `Constants.cs:96-99` → `worldObject.shakeTimer` reality, remove `ShakeDuration` claim (G9); (e) delete verified-zero-ref `MaxPathSegmentLength`, `MaxPathDepth`, `MaxDataValueSizeBytes`, `CompostEmptyColor`, `CompostProcessingColor`, `CompostReadyColor`, `WaterColor`, `SunColor`, `MulchColor` from `Constants.cs`, and `TooltipDebounceMs` (wire into `TooltipRenderer.cs` ONLY if a literal `50` debounce exists there, else delete), and `MaxFileNameLength` (`Constants.cs:27`: rewire `FileNameSanitizationService`'s shadowing `private const` to the shared constant if values are equal, else delete the shared one) — re-grep zero refs at edit time (G10); (f) delete stale `RED today (T251 pending)` remark at `MultiplayerConcurrencyTests.cs:1537-1549`. Re-run all `ProductionSourceGuardTests` (G8+G9+G10).
- [ ] T283 [P] Add the literal `Principle ` token to both VI rows in `specs/001-soil-health-visualization/plan.md:42` and `:55` (first cells → `| Principle VI. Multiplayer As A First-Class Requirement |`, G11). Re-run Spec001Plan test.

### Verification (ONLY T273–T276 attest — re-run AFTER T277–T283, in gate order)

T052/T086 excluded (D1/maintainer) as before.

### Wave 30 (remediation implementation — [P] fan-out, disjoint files)

- Subagent EA: T277 (`CompostingBinService.cs` AddWaste region only)
- Subagent EB: T278 + T279 (`VisualizationService.cs` log + `OverlayRendererTests.cs` + `SoilHealthVisualizationTests.cs` — shared log-format contract, one agent serially)
- Subagent EC: T280 + T281-test-half (`CompostingBinServiceTests.cs`, `CompostingMachineInvariantsTests.cs`, `HudNotifierTranslationTests.cs` — test files only)
- Subagent ED: T281-prod (`ModEntry.cs:105` one-liner) + T283 (`plan.md` rows)
- Subagent EE: T282 (interface comments + guard test + `Constants.cs` + tooltip/filename checks + stale remark)
