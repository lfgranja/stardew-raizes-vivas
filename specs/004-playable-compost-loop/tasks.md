# Tasks: Playable Compost Loop (004)

**Input**: Design documents from `/specs/004-playable-compost-loop/` (plan.md, spec.md, data-model.md, contracts/contracts.md, research/research.md, quickstart.md)

**Feature**: Playable Compost Loop | **Branch**: `004-playable-compost-loop`

**Prerequisites**: `plan.md` (tech: .NET 6 / SMAPI / C# latest; `LivingRoots/` structure), `spec.md` (4 user stories), `constitution.md` (Principles I–VI; VI conditional, D1 debt explicit), `data-model.md`, `contracts/contracts.md`

**Tests**: MANDATORY (RED-phase blocking per Constitution IV / user rule #1) — all RED tasks (T000-R1..R9) must exist and be FAILING before any implementation starts (Phase 0a compile scaffolding is structural-only and exempt); property-based invariants (FsCheck `ForAll`) required wherever applicable (user rule #2).

---

## Phase 0a: Compile Scaffolding (structural only — exempt from the RED gate)

**Purpose**: Minimal compile-time scaffolding required so that RED tests can compile. Contains NO production logic and NO behaviour; all behaviour is implemented in Phase 2+. This resolves the circular dependency between RED tests and setup.

- [ ] T003 [P] Create `LivingRoots/Domain/Interfaces/` skeleton (`ICompostApplicationService.cs`, `ICompostingBinService.cs`, `ISoilHealthService.cs`) — signatures only, no logic, per `AGENTS.md` DDD pattern (interface in Domain → impl in Services)
- [ ] T004 [P] Create `ModEntry.cs` `OnAssetRequested` handler skeleton (empty registration stubs for `Data/Objects`, `Data/BigCraftables`, `Data/Machines`, `Data/CraftingRecipes`) — no asset content yet, per `plan.md` and `contracts/contracts.md` §1
- [ ] T004a [P] Create `LivingRoots.Tests/Invariants/` directory with shared FsCheck fixture infrastructure (property-based test setup reused by T000-R1..R9 and T031a/T031b) per `plan.md` project structure

---

## Phase 0: RED Phase — Write Failing Tests (TDD — BLOCKING; must NOT pass before any implementation)

**Purpose**: Per Constitution IV / user input #1 — tests written first, failing for the right reason (RED), blocking all implementation until written (not passing; failure attests design understanding). Delegated to up to 9 concurrent subagents (`[P]`).

- [ ] T000-R1 [P] Write failing `CompostingBinDataTests.cs` (tolerant reader: corrupt entries quarantined, obsolete fields ignored) — must fail before T005. Example-based tests for corrupt-entry isolation (given corrupt entry → quarantined; given obsolete fields → ignored); property-based invariant: `ForAll(save => AllEntriesValidOrQuarantined(save))`; RED-phase only: design invariant for tolerant reader; not integration test.
- [ ] T000-R2 [P] Write failing `CompostingBinStateModelTests.cs` (state-machine monotonicity: Empty→Processing→Ready, max stack=1 when progress) — must fail before T006. Property-based invariant (FsCheck.ForAll): state-machine monotonicity Empty→Processing→Ready and batch yield == 1 exactly; never skip, never duplicate, never reset on inventory pickup.
- [ ] T000-R3 [P] Write failing `SoilHealthServiceTests.cs` (bounds invariant 0–100, +15 clamp, reactive event fires) — must fail before T008. Property-based `FsCheck.ForAll` required: `h => h >= 0 && h <= 100`; apply +15 => clamped to 100 or refused.
- [ ] T000-R4 [P] Write failing `CompostApplicationServiceTests.cs` (exactly 1 consumed, refusal reasons, crop tolerance, **side-dressing invariant: active crop preserved**; property-based `FsCheck.ForAll`: consumption always exactly 1 (or refused) and soil health +15 (or refused); crop presence unchanged on application) — must fail before T022.
- [ ] T000-R5 [P] Write failing `MultiplayerConcurrencyTests.cs` stub (first-wins atomic lock, refusal message; stub-only / D1 debt explicit — SC-010 / FR-019) — must fail before T011c; stub-based simulation tests for first-wins atomic lock; property-based invariant: `ForAll(concurrentPairs => ExactlyOneSucceeds(concurrentPairs))`
- [ ] T000-R6 [P] Write failing `VisualizationServiceTests.cs` (overlay render-safety: MUST NOT render when cache is null/uninitialized; 3-tier interpolation; reactive invalidation ≤16.67ms) — must fail before T024. Property-based invariant: `ForAll(cache => cache == null ? !renderCalled : renderCalled)` (`FR-010` / `FR-011`).
- [ ] T000-R7 [P] Write failing `DailyDecayTests.cs` (once-per-day decay; seasonal multiplier 1.5× summer / 0.5× spring & fall / 0× winter; greenhouse exempt; clamp 0–100) — must fail before T008a. Property-based invariant: `ForAll(health, season => IsClamped(ApplyDecay(health, season)))` (`FR-013`).
- [ ] T000-R8 [P] Write failing `AssetRegistrationTests.cs` (US1: `LivingRoots.Compost` present in `Data/Objects`, `LivingRoots.CompostingBin` in `Data/BigCraftables`, recipe in `Data/CraftingRecipes`, machine in `Data/Machines`; recipe listed as craftable; bin placeable on farm / Greenhouse / Ginger Island farm, removable, no ghost on rejected placement) — must fail before T012/T013/T014/T015 (US1 asset registration) and T021 (`Data/Machines` declaration, shared with US2). Property-based invariant: `ForAll(placement => ValidTerrain(placement) == PlacementSucceeds(placement))` (`FR-001` / `FR-002`).
- [ ] T000-R9 [P] Write failing `CompostingBinServiceTests.cs` (US2: insert qualifying waste → `Processing`; collect when `Ready` → exactly 1 `Compost`; collect when not `Ready` → nothing produced; full inventory → bin stays `Ready`; one in-game day advances processing exactly once) — must fail before T017/T018/T019/T020. Property-based invariant: `ForAll(bin => Collect(bin).Yield == (bin.State == Ready ? 1 : 0))` (`FR-004` / `FR-005a` / `FR-005b`); FR-003 qualifying-set invariant: `ForAll(item => IsCompostable(item) == ExpectedFromSpec002(item))`.

**Gate**: All 9 RED tasks (T000-R1..R9) exist and FAIL → unlock Phase 1 (Setup) and Phase 2 (Foundational). No implementation anywhere before its RED test exists and fails. RED → phase mapping: Phase 2 ← R1/R2/R3/R5/R7; Phase 3 (US1) ← R8; Phase 4 (US2) ← R9 (state-model transitions shared with R2); Phase 5 (US3) ← R4/R6; Phase 6 (US4) is documentation, verified by T027/T027a/T029 rather than a RED test.

---

## Phase 1: Setup (Shared Infrastructure) — RUN AFTER Phase 0 RED tests exist and are FAILING

**Purpose**: Project initialization and asset-registration data. Blocked until RED-phase tasks (T000-R1..R9) are failing (not passing).

- [ ] T001 [P] Initialize `ModConstants.cs` with `MaturationDays = 2`, `RestorationAmount = 15`, item IDs (`LivingRoots.Compost`, `LivingRoots.CompostingBin`), recipe ID per `plan.md` and `contracts/contracts.md` §1.1–§1.4 (T053 spike-decision.md route settled; no task needed — documented in spec Assumptions)
- [ ] T002 [P] Configure `LivingRoots/i18n/default.json` with refusal keys (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`) per `contracts/contracts.md` §3 / `spec.md` FR-008a

---

## Phase 2: Foundational (Blocking Prerequisites — MUST complete before ANY user story)

**Purpose**: Core persistence, domain models, and native-machine contracts that all stories depend on

**⚠️ CRITICAL**: No user story implementation begins until Phase 2 complete.

- [ ] T005 [P] Implement `CompostingBinData` persistence DTO (`LivingRoots/Domain/Models/`) with tolerant-reader rules: ignore obsolete fields (`MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays`), per-entry `try/catch`, quarantined corrupt records with sanitized logs (`FR-012`, `FR-014`, `FR-015` per `data-model.md`) — load MUST continue (no abort) when corrupt entries quarantined; verify with `T031` round-trip
- [ ] T006 [P] Implement `CompostingBinStateModel` runtime state (`LivingRoots/Domain/Models/`) holding `Location`, `State` (`Empty`/`Processing`/`Ready`), `InputId`, `DaysRemaining`, `ProgressPaused`; enforce `max stack = 1` when progress active (`FR-005c` / `data-model.md`)
- [ ] T007 [P] Implement `IModDataService` persistence layer (`LivingRoots/Services/ModDataService.cs`) with `SanitizePathSegments()` for tile keys; save keys `composting_bins_{saveId}` / `soil_health_data_{saveId}`; sparse storage (do not store default `0` health) per `AGENTS.md`
- [ ] T008 [P] Implement `SoilHealthService.cs` (`LivingRoots/Services/`) with `GetHealth`, `ApplyCompost`, reactive `OnTileHealthChanged` event, bounds clamp to `[0, 100]` (`ModConstants.Min/Max`); allow application on active crop (`FR-007` clarification 2026-10-04)
- [ ] T009 [P] Implement `CompostingBinService.cs` (`LivingRoots/Services/`) state machine (`Empty → Processing → Ready`); `MaturationDays = 2`; exactly-one batch yield `1`; progress preserved in `Item.modData`; processing pauses unplaced (`FR-001b`, `FR-005c` / `data-model.md`)
- [ ] T010 [P] Create `VisualizationService.cs` skeleton (`LivingRoots/Services/Visualization/`) with reactive subscription to `ISoilHealthService.OnTileHealthChanged` and stub render method (no overlay logic yet — full 3-tier RGB interpolation implemented in T024) (`FR-009`, `FR-010` / `data-model.md`)
- [ ] T011a [P] Configure `ModController.cs` placement/dislodge handlers with `ExecuteWithConcurrencyGuard` and progress preservation (`FR-005c`)
- [ ] T011b [P] Configure `ModController.cs` right-click compost-application handler with atomically-locked refusal reasons (`FR-007`/`FR-008`/`FR-019`)
- [ ] T011c [P] Configure `ModController.cs` host-authoritative atomic lock mechanism per tile/machine (**host-lock only**, `Principle VI` / `FR-019`: first-wins concurrent action; stub-bounded, D1 debt documented) — use `Interlocked.CompareExchange` on a dedicated per-tile/machine lock flag for first-wins atomic lock (separate from the daily-decrement guard flag); refusal on second concurrent action preserved
- [ ] T008a [P] Implement `SoilHealthService` daily decay once per day (`FR-013` / `FR-011`): apply `decayRate * seasonalMultiplier` to bare tilled tiles at day start; seasonal rules 1.5x summer, 0.5x spring/fall, 0 winter; exempt greenhouse; inherited from `specs/002-soil-decay-compost/` (spec §FR-001/FR-011); property-based invariant: health clamped 0–100 after decay. DEPENDENCY NOTE: independent of US1 (bin existence); executes in Phase 2 parallel with T008–T011c; US4 (visual) requires US2 for compost source; no cross-block.

**Checkpoint**: Foundation ready — `ISoilHealthService`, `ICompostingBinService`, persistence, visualization, and atomic-lock controller all functional and independently testable.

---

## Phase 3: User Story 1 — Obtain and place a composting bin (Priority: P1) 🎯 MVP

**Goal**: Crafting recipe visible; bin craftable; placeable on farm / Greenhouse / Ginger Island farm; removable without ghost state.

**Independent Test**: Launch mod → open crafting menu → craft `LivingRoots.CompostingBinRecipe` → place on farm / Greenhouse / Ginger Island farm → pick up → verify no stray object; reject placement on water/building (`FR-002` / `quickstart.md` Scenario A).

**Story dependencies**: None (after Phase 2); all other stories eventually depend on bin existing.

- [ ] T012 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/Objects` (`LivingRoots.Compost`) per `contracts/contracts.md` §1.1
- [ ] T013 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/BigCraftables` (`LivingRoots.CompostingBin`) per `contracts/contracts.md` §1.2
- [ ] T014 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/CraftingRecipes` (`LivingRoots.CompostingBinRecipe`) per `contracts/contracts.md` §1.4
- [ ] T015 [US1] Implement `CompostingBinService.TryPlace()` / removal logic with valid-terrain check (farm / Greenhouse / Ginger Island farm); rejected placement must leave no ghost; dislodge preserves `Item.modData` (`FR-002`, `FR-005c` / `data-model.md`)

---

## Phase 4: User Story 2 — Turn organic waste into compost (Priority: P1)

**Goal**: Bin accepts qualified organic waste; processes exactly 2 in-game days; yields exactly 1 compost; full inventory retries; no destruction.

**Independent Test**: Place empty bin → insert qualifying waste (`FR-003` inherited set) → advance 2 days (`FR-001b`, `FR-004`) → collect 1 compost (`FR-005a`) → verify inventory item exists with correct name/icon; full inventory → collect retries later (`FR-005b` / `quickstart.md` Scenario B, C).

**Story dependencies**: Requires bin from US1 (can test with pre-placed bin independently).

- [ ] T017 [US2] Implement `CompostingBinService.TryInsertOrganicWaste()` with qualifying-set check (inherited, not restated; `FR-003` / `data-model.md`) and native-machine shake/cancel on invalid drop-in (`FR-008a` clarification 2026-10-13). **FR-003 verification (moved here from T022):** property-based invariant `ForAll(item => IsCompostable(item) == ExpectedFromSpec002(item))` confirming qualifying set matches spec 002 exactly (artisan goods excluded unless 002 updated).
- [ ] T018 [US2] Implement `CompostingBinService.OnDayStarted()` exactly-once **daily-decrement guard** (`FR-001b` fallback permitted) — NOTE: uses `Interlocked.CompareExchange` on a dedicated decrement flag (not the same lock as T011c's concurrent-action first-wins lock); decrement `DaysRemaining` once per day regardless of player count (`FR-004`); T011c lock and T018 guard are independent mechanisms on separate fields
- [ ] T019 [US2] Implement `CompostingBinService.TryCollect()` yielding exactly `1` `LivingRoots.Compost`; full inventory leaves `Ready`; retry delivers full batch; `message.harvest.inventory-full` delivered (`FR-005b` / `FR-008a`)
- [ ] T020 [US2] Enforce `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) serializes into inventory bin; `max stack = 1` while progress present; processing pauses unplaced (`FR-005c` / `data-model.md`)
- [ ] T021 [P] [US2] Implement `ModEntry.OnAssetRequested` edit for `Data/Machines` (`Triggers`, `DaysUntilReady = 2`, output quantity `1`) per `contracts/contracts.md` §1.3 (**retire old 1-5 multiplier**; confirm obsolete fields `MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays` absent from definition) (FR-001b / FR-005a / A2). **Verification of Data/Machines declaration consolidated here (was T016); T021 now covers declaration + implementation + retirement of multiplier.**

---

## Phase 5: User Story 3 — See soil health change on the ground (Priority: P2)

**Goal**: Compost application +15 pts clamped to 100 on tilled `HoeDirt`; visible 3-tier RGB overlay; 0 = neutral; ≤16.67ms reactive update.

**Independent Test**: Apply compost to tilled tile → health increases by 15 (clamped); visualize tier change; toggle display off → soil improves, overlay hidden (`FR-009`, `FR-010` / `quickstart.md` Scenario D, E).

**Story dependencies**: Requires compost item from US2 (can test with manually added item independently).

- [ ] T022 [US3] Implement `CompostApplicationService.TryApply()` consuming exactly `1` compost; `+15` clamped to `100`; refused on untilled / non-farm / `100` / building / indoor (`FR-007`, `FR-008` / `data-model.md`). Note: qualifying-set verification lives in T017 (FR-003), not here.
- [ ] T023 [US3] Wire refusal messages through `Game1.showRedMessage` + `ITranslationHelper` with keys `message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict` (no custom UI; `FR-008a` / `contracts/contracts.md` §3)
- [ ] T024 [US3] Implement full `VisualizationService` 3-tier RGB interpolation (Poor 1–33 / Moderate 34–66 / Healthy 67–100) and ambient-light blending; `0` health renders neutral vanilla dirt without overlay; build on T010 skeleton (`FR-009`, `FR-010` / `data-model.md`)
- [ ] T025 [US3] Confirm `ISoilHealthService.OnTileHealthChanged` triggers `VisualizationService` cache invalidation on next frame (`≤16.67ms`); zero polling (`FR-010` / `data-model.md`)
- [ ] T025b [P] [US3] Failure-mode stub: `VisualizationService` MUST NOT render when health cache is null/uninitialized; "no overlay" defined as: VisualizationService.Draw is not called when cache is null; no fallback to default or zero-value rendering occurs; property-based invariant `ForAll(cache => cache == null ? !renderCalled : renderCalled)` (`FR-011` / `data-model.md`)
- [ ] T026 [P] [US3] Verify crop-tolerance invariant: active growing crop is not harmed or removed by compost application (side-dressing / top-dressing); property-based `ForAll(crop => CropPreservedAfterApplication(crop))` (`FR-007` clarification 2026-10-04). **FR-018 invariant**: property-based `ForAll(state => SinglePlayerPath(state) == MultiPlayerPath(state))` — no code path may behave differently on multiplayer farm than single-player farm (Constitution VI / FR-018)

---

## Phase 6: User Story 4 — Trust what the project says about itself (Priority: P3)

**Goal**: README feature claims accurate; 8 stale spec/docs reconciled against Constitution VI; `(Planned)` labels on unbuilt capabilities.

**Independent Test**: Read `README.md` → every claim observable or labelled `(Planned)`; check 8 docs (`specs/001-003` + design-system docs) for multiplayer out-of-scope contradictions (`FR-016`, `FR-017`, `SC-006`, `SC-007` / `spec.md`).

**Story dependencies**: None (documentation); ideally after US1–US3 verified but can proceed independently.

- [ ] T027 [P] [US4] Audit `README.md` `## Features`; label all unbuilt capabilities with `(Planned)` suffix; remove false claims (`FR-016` / `spec.md` clarification 2026-10-04)

- [ ] T027a [P] [US4] Reconcile 8 stale spec/doc scope statements declaring multiplayer out of scope — scope-wording correction only (changes limited to sentences containing 'multiplayer' or 'single-player'; no changes to requirement IDs, acceptance criteria, or technical constraints); reference `FR-017` / Constitution VI / `plan.md` Constitution Check CONDITIONAL PASS. Sub-tasks:
  - [ ] T027a-1 Reconcile `specs/001-soil-health-visualization/spec.md` (scope words)
  - [ ] T027a-2 Reconcile `specs/001-soil-health-visualization/plan.md` (scope words)
  - [ ] T027a-3 Reconcile `specs/002-soil-decay-compost/spec.md` (scope words)
  - [ ] T027a-4 Reconcile `specs/002-soil-decay-compost/plan.md` (scope words)
  - [ ] T027a-5 Reconcile `specs/003-composting-tests/spec.md` (scope words)
  - [ ] T027a-6 Reconcile `specs/003-composting-tests/plan.md` (scope words)
  - [ ] T027a-7 Reconcile `docs/design-system/REALITY-CHECK-EVALUATION.md` (scope words)
  - [ ] T027a-8 Reconcile `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` (scope words)
  Verification gate upon T027a completion: confirm each file corrected, no redesign.
- [ ] T029 [P] [US4] Verify no spec declares multiplayer out of scope where it contradicts Constitution Principle VI (`SC-007` / `spec.md` Assumptions — multiplayer verification boundary)

---

## Phase 7: Polish & Implementation (Cross-Cutting)

**Purpose**: Concurrency stub implementation, save/load fault isolation, security, format, invariant test suites

- [ ] T030 [P] Implement `MultiplayerConcurrencyTests.cs` automated multi-threaded stub / `ThreadSafeGameLoopEventsStub` verifying first-wins atomic lock (`Principle VI` / FR-019) and `OnTileHealthChanged` reactive invalidation (`SC-005`, `SC-010`; stub-bounded, not live proof — D1 debt explicit per spec Assumptions (multiplayer verification boundary) / Constitution §VI). ADD: simulated disconnect/reconnect sequence + in-process network-fault injection (message drop, reorder) limited to simulated message ordering within the test stub — not actual network protocol testing; stub alone never reproduces live SMAPI message ordering (CHESS/CTrigger/Jepsen limits documented). (FR-019 / Principle VI). **FR-018 invariant**: property-based `ForAll(state => SinglePlayerPath(state) == MultiPlayerPath(state))` — no code path may behave differently on multiplayer farm than single-player farm (Constitution VI / FR-018)
- [ ] T031a [P] Property-based invariant tests (FsCheck `ForAll`): soil health always clamped 0–100; compost application always +15 (or refused); bin state-machine never regresses — FR-009/FR-007/FR-001a invariants. Delegate to subagent wave (reuse T000-R3/R4 context). **Consolidation note**: T031a promotes the RED invariants from T000-R2/R3/R4 into the permanent invariant suite in the same files; it MUST NOT duplicate test code. Reuse note: Property fixture shared with T000-R1..R9 (FsCheck ForAll setup); subagent wave may reuse same test-project context to avoid duplicate setup.
- [ ] T031b [P] Property-based invariant tests (FsCheck `ForAll`): first-wins concurrent action always yields exactly one applied change; visual overlay distinguishable (RGB shift <5% vs untreated at same light; blend alpha ≥0.65); overlay timing ≤16.67ms validated by reactive event `OnTileHealthChanged` — FR-010/FR-019 invariants. Delegate to subagent wave (reuse T000-R5 context). **Consolidation note**: T031b promotes the RED invariants from T000-R5/R6 into the permanent invariant suite in the same files; it MUST NOT duplicate test code. Reuse note: Property fixture shared with T000-R1..R9.
- [ ] T031 [P] Implement save/load round-trip tests (`SC-004`, `FR-012`): corrupt / obsolete / orphaned entries quarantined; valid entries preserved; visual overlays re-rendered. Note: Integration round-trip: builds on T000-R1 invariant; validates save/load with corrupt + orphan + obsolete entries.
- [ ] T032 [P] Security validation: `ModDataService.SanitizePathSegments()` on tile keys; no raw exception messages to output (`FR-015` / `AGENTS.md` — `ex.GetType().FullName` + `HResult` at `LogLevel.Trace` only, never `Message`); `CheckForOverflowUnderflow` never suppressed; bounds `0–100` enforced (`Principle II`)
- [ ] T033 [P] Format verification: `dotnet format Stardew-LivingRoots.sln --verify-no-changes`; build green (`dotnet build --nologo`) (`Gate 1` / `AGENTS.md`)
- [ ] T035 [P] Confirm `Item.modData` progress serialization works across pickup / placement / dislodgement; floating-world-debris case with full inventory (`FR-005c` / `data-model.md`)
- [ ] T036 [P] Property-based invariant for FR-006: compost obtainable ONLY through bin processing — `ForAll(player => CanObtainCompost(player) == HasCompletedBinCycle(player))`; no bypass path exists (no direct inventory grant, no crafting recipe for compost, no loot table) (`FR-006` / `data-model.md`)

---

## Phase 8: Verification — Attestation Only (MUST run last; the ONLY tasks that attest success)

**Purpose**: Per user input #5 — ONLY these verification tasks attest success and completeness of the implementation. The implementing agent MUST NOT self-attest. Every task here MUST run after all Phase 0a–7 work is complete.

- [ ] T034 [P] Run `quickstart.md` scenarios A–H; confirm Gate 2 (product-level) satisfied; for Gate 3 (multi) CONFIRM ONLY THAT D1 LIVE SESSION (host + farmhand) HAS PASSED — if D1 not complete, T034 MUST stay OPEN and feature MUST NOT be declared complete (Constitution VI / spec Assumptions (multiplayer verification boundary) / FR-018/FR-019); document D1 debt explicitly; verification-only attestation — NEVER self-attest; MUST NOT declare complete for multi Gate 3 until D1 passes
- [ ] T034a [P] Explicit SC-003 audit: verify 100% of observed soil health values within [0,100] across all bin lifecycle states; property-based invariant `ForAll<int>(h => h >= 0 && h <= 100)`
- [ ] T035a [P] Explicit SC-009 audit: repeated collect from full inventory + pick-up/re-place mid-batch + floating-world-debris with unavailable inventory; **property-based invariant: `Item.modData` keys `LivingRoots.InputId` + `LivingRoots.DaysRemaining` preserved after debris placement**; zero destruction attested
- [ ] T037 [US4] SC-008 discoverability marker: manual playtest confirms recipe appears in crafting menu unconditionally, bin placeable on first attempt, organic waste accepted on first insert — no external docs required; validated during Gate 2/3 playthrough only, not automatable
- [ ] T038 [P] SC-002 audit: verify every step of the player-facing loop (craft → place → insert → advance → collect → apply → observe) produces a visible in-game result; no step completes silently; property-based invariant: `ForAll(step => ProducesVisibleChange(step))` over all loop stages (`SC-002`)
- [ ] T039 [P] FR-018 invariant audit: verify `ForAll(state => SinglePlayerPath(state) == MultiPlayerPath(state))` across all code paths; no code path may behave differently on multiplayer farm than single-player farm (Constitution VI / FR-018); property-based test delegates to subagent wave (reuse T000-R5 context)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Depends on Phase 0 RED tasks (T000-R1..R9) existing and FAILING; Phase 0a compile scaffolding (T003/T004/T004a) precedes Phase 0.
- **Phase 2 (Foundational)**: Depends on Phase 1; **BLOCKS all user stories** (`T005`–`T011c` + `T008a` must finish before user-story tasks begin).
- **Phase 3 (US1)**: After Phase 2; can begin in parallel with US2/US3/US4 if staffed (independent tests), but US2 requires bin object existence (sequential dependency for full loop).
- **Phase 4 (US2)**: After Phase 2; depends on US1 for full journey, but independently testable with pre-placed bin.
- **Phase 5 (US3)**: After Phase 2 + US2 for compost source; independently testable with manually added compost.
- **Phase 6 (US4)**: After Phase 2; no runtime dependency on US1–US3 for docs, but accuracy requires verification against implemented behavior.
- **Phase 7 (Polish & Implementation)**: After all desired stories complete; includes concurrency stubs (`T030`) and invariant suites. **Phase 8 (Verification)** runs last and is the only phase that attests success.

### User Story Dependency Order

- **US1 (P1)**: No story dependencies — MVP entry point.
- **US2 (P1)**: Depends on US1 (needs bin object); can be implemented in parallel once foundational complete if bin fixture available.
- **US3 (P2)**: Depends on US2 (needs compost item); depends on US1 (needs bin for soil-change verification context).
- **US4 (P3)**: Independent of runtime stories; depends only on design accuracy (Phase 2 completed).

### Within Each Story (Implementation Order)

- Contracts / asset registrations first (models / interfaces already in Phase 2)
- Core service logic (`CompostingBinService`, `SoilHealthService`, `CompostApplicationService`)
- Controller / event wiring (`ModController`)
- Validation / refusal messages (`i18n/default.json`, `Game1.showRedMessage`)
- Independent test / quickstart verification

### Parallel Opportunities

- `T003` / `T004` / `T004a` (Phase 0a scaffolding, different files, no shared state) — `[P]` marked.
DEPENDENCY: Phase 0a scaffolding (T003/T004/T004a) must exist before the RED tests (T000-R1..R9) can compile, but it contains no production logic — structural only, exempt from the RED gate.
- `T001` / `T002` (Phase 1 setup, different files) — `[P]` marked.
- `T005`–`T011c` + `T008a` (foundational) can run in parallel within Phase 2 if staffed.
- Once Phase 2 completes: `US1`, `US2` (with fixtures), `US3` (with fixtures), `US4` can proceed in parallel by different developers.
- `T030` / `T031` / `T031a` / `T031b` / `T032` / `T033` / `T035` / `T036` (polish / stub / security / format) independent of story order — can start once relevant service code exists.

### Subagent Wave Plan (user input #3/#4 — delegate `[P]` tasks to the maximum viable agents)

| Wave | Tasks | Shared context (reuse one subagent per context) | Mode |
|------|-------|--------------------------------------------------|------|
| W0 | T003, T004, T004a | Compile scaffolding (Domain/Interfaces + ModEntry skeleton + test fixture) | background `[P]` |
| W1 | T000-R1..R9 | RED tests — one subagent per test file (max 9 concurrent) | background `[P]` |
| W2 | T001, T002 | Constants + i18n (different files) | background `[P]` |
| W3 | T005, T006, T007, T008, T009, T010, T011a, T011b, T011c, T008a | Foundational services — group by shared context: persistence (T005/T006/T007), soil (T008/T008a), bin (T009), visualization (T010), controller (T011a/b/c) | background `[P]` |
| W4 | T012, T013, T014, T015, T017–T021, T022–T026 | User stories US1–US3 — one subagent per story; within a story, tasks touching the same file (e.g. ModEntry.cs) run sequentially inside that subagent | background |
| W5 | T027, T027a, T029 | US4 documentation — reuse one docs subagent | background `[P]` |
| W6 | T030, T031, T031a, T031b, T032, T033, T035, T036 | Polish/implementation test suites (excludes verification T034, which is W7) | background `[P]` |
| W7 | T034, T034a, T035a, T037, T038, T039 | Verification only — fresh subagent(s); MUST NOT reuse implementing context | background |

Reuse rule: tasks that share a context (same service / same test project / same doc set) reuse the same subagent; tasks in different contexts run as separate background agents.

---

## Sequential Example: User Story 1 (shared ModEntry.cs)

```bash
# Asset registrations + placement logic (T012/T013/T014/T021 share ModEntry.cs → sequential; T015 is a separate file):
T012-US1: LivingRoots/ModEntry.cs — Data/Objects edit
T013: LivingRoots/ModEntry.cs — Data/BigCraftables edit
T014: LivingRoots/ModEntry.cs — Data/CraftingRecipes edit
T015: LivingRoots/Services/CompostingBinService.cs — TryPlace / removal
T021 [P]: LivingRoots/ModEntry.cs — Data/Machines declaration + implementation (Triggers, DaysUntilReady=2, qty 1); retire 1-5 multiplier per FR-005a.
# NOTE: T012/T013/T014/T021 all edit ModEntry.cs — assign them to ONE subagent and run sequentially (no parallel writes to the same file).
```

---

## Implementation Strategy

### MVP (User Story 1 Only + Foundational)

1. Complete Phase 0a + Phase 0 + Phase 1 + Phase 2 (`T003`–`T011c`).
2. Complete Phase 3 (`T012-US1`–`T015`).
3. **STOP / VALIDATE**: Independent test — craft → place → pick up → verify no ghost (`quickstart.md` A). Confirm `ModEntry.OnAssetRequested` delivers real `Data/Objects`, `Data/BigCraftables`, `Data/CraftingRecipes`, `Data/Machines`.
4. Deploy / demo.

### Incremental Delivery

1. Phase 0a + 0 + 1 + 2 → Foundation (scaffolding → RED → setup → foundational).
2. Phase 3 (US1) → Bin exists and is playable.
3. Phase 4 (US2) → Loop closes (waste → 2-day → compost). Verify with `quickstart.md` B/C.
4. Phase 5 (US3) → Soil change visible. Verify with `quickstart.md` D/E.
5. Phase 6 (US4) → Docs accurate; reconciliation complete. Verify with `quickstart.md` H.
6. Phase 7 (Polish & Implementation) → Stubs, save/load, security, format. Phase 8 (Verification) → attestation only, run last; D1 debt documented.

### Team Strategy

- Developer A: Phase 2 (persistence, soil health, visualization, controller) + Phase 3.
- Developer B: Phase 2 (bin service, contracts) + Phase 4.
- Developer C: Phase 5 (application, visual) + Phase 6 (docs) + Phase 7 (stubs / security / format).

---

## Notes

- **Atomic commit rule (user input #6):** after every file edit, run `.specify/scripts/atomic-commit-hook.sh <file> "<summary>" "<why>"` (or `git add <file> && git commit`) so each edit lands as an atomic conventional commit.
- **D1 debt boundary (applies to all concurrency tasks):** Automated stubs attest mechanism within their bounds; live 2-player verification is owed to roadmap D1. 004 MUST NOT be declared complete for multi Gate 3 until D1 runs. Stubs systematically miss interleavings/network semantics (CHESS/CTrigger/Jepsen limits).
- All tasks use exact file paths from `AGENTS.md` / `plan.md` project structure.
- Constraint quotes from `data-model.md` embedded in tasks (e.g., `max stack = 1` in `T020`, `+15` clamped in `T022`).
- `T030` explicitly bounded as stub-only; live 2-player verification deferred to Roadmap D1 (`spec.md` Assumptions / Constitution §VI / `quickstart.md` G).
- No bespoke UI / text-drawing components (`FR-008a`; `Principle V`).
- `T034` validates `quickstart.md` scenarios; Gate 3 requires real-player journey through full loop (`spec.md` Success Criteria / `AGENTS.md`).
