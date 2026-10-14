# Tasks: Playable Compost Loop (004)

**Input**: Design documents from `/specs/004-playable-compost-loop/` (plan.md, spec.md, data-model.md, contracts/contracts.md, research/research.md, quickstart.md)

**Feature**: Playable Compost Loop | **Branch**: `004-playable-compost-loop`

**Prerequisites**: `plan.md` (tech: .NET 6 / SMAPI / C# latest; `LivingRoots/` structure), `spec.md` (4 user stories), `constitution.md` (Principles I–VI; VI conditional, D1 debt explicit), `data-model.md`, `contracts/contracts.md`

**Tests**: MANDATORY (RED-phase blocking per Constitution IV / user rule #1) — all RED tasks (T000-R1..R5) must exist and be FAILING before any Phase 2 implementation starts; property-based invariants (FsCheck `ForAll`) required wherever applicable (user rule #2).

---

## Phase 1: Setup (Shared Infrastructure) — RUN AFTER Phase 0 RED PASSES

**Purpose**: Project initialization and asset-registration infrastructure. Blocked until RED-phase tasks (T000-R1..R5) are failing (not passing).

- [ ] T001 Initialize `LivingRoots/Constants.cs` / `ModConstants.cs` with `MaturationDays = 2`, `RestorationAmount = 15`, item IDs (`LivingRoots.Compost`, `LivingRoots.CompostingBin`), recipe ID per `plan.md` and `contracts/contracts.md` §1.1–§1.4 (T053 spike-decision.md route settled; no task needed — documented in spec Assumptions)
- [ ] T002 Configure `LivingRoots/i18n/default.json` with refusal keys (`message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict`, `message.harvest.inventory-full`) per `contracts/contracts.md` §3 / `spec.md` FR-008a
- [ ] T003 [P] Set up `LivingRoots/Domain/Interfaces/` skeleton (`ICompostApplicationService.cs`, `ICompostingBinService.cs`, `ISoilHealthService.cs`) per `AGENTS.md` DDD pattern (interface in Domain → impl in Services)
- [ ] T004 [P] Configure `ModEntry.cs` `OnAssetRequested` handler structure for `Data/Objects`, `Data/BigCraftables`, `Data/Machines`, `Data/CraftingRecipes` per `plan.md` and `contracts/contracts.md` §1

---

## Phase 0: RED Phase — Write Failing Tests (TDD — BLOCKING; must NOT pass before Phase 2)

**Purpose**: Per Constitution IV / user input #1 — tests written first, failing for the right reason (RED), blocking all implementation until written (not passing; failure attests design understanding). Delegated to maximum viable parallel agents (`[P]`).

- [ ] T000-R1 [P] Write failing `CompostingBinDataTests.cs` (tolerant reader: corrupt entries quarantined, obsolete fields ignored) — must fail before T005. Property-based invariant (`FsCheck.ForAll`) required for corrupt-entry isolation; RED-phase only: design invariant for tolerant reader; not integration test.
- [ ] T000-R2 [P] Write failing `CompostingBinStateModelTests.cs` (state-machine monotonicity: Empty→Processing→Ready, max stack=1 when progress) — must fail before T006. Property-based invariant (FsCheck.ForAll): state-machine monotonicity Empty→Processing→Ready and batch yield == 1 exactly; never skip, never duplicate, never reset on inventory pickup.
- [ ] T000-R3 [P] Write failing `SoilHealthServiceTests.cs` (bounds invariant 0–100, +15 clamp, reactive event fires) — must fail before T008. Property-based `FsCheck.ForAll` required: `h => h >= 0 && h <= 100`; apply +15 => clamped to 100 or refused.
- [ ] T000-R4 [P] Write failing `CompostApplicationServiceTests.cs` (exactly 1 consumed, refusal reasons, crop tolerance) — must fail before T022. Property-based `FsCheck.ForAll`: consumption always exactly 1 (or refused) and soil health +15 (or refused).
- [ ] T000-R5 [P] Write failing `MultiplayerConcurrencyTests.cs` stub (first-wins atomic lock, refusal message; stub-only / D1 debt explicit — SC-010 / FR-019) — must fail before T011c; property-based via FsCheck `ForAll` for invariants

**Gate**: These 5 tasks exist and are RED (failing) → unlock Phase 1 (Setup) and Phase 2 (Foundational). No GREEN before Phase 2 starts.

## Phase 2: Foundational (Blocking Prerequisites — MUST complete before ANY user story)

**Purpose**: Core persistence, domain models, and native-machine contracts that all stories depend on

**⚠️ CRITICAL**: No user story implementation begins until Phase 2 complete.

- [ ] T005 Implement `CompostingBinData` persistence DTO (`LivingRoots/Domain/Models/`) with tolerant-reader rules: ignore obsolete fields (`MaturationLevel`, `ConsecutiveActiveDays`, `ConsecutiveIdleDays`), per-entry `try/catch`, quarantined corrupt records with sanitized logs (`FR-012`, `FR-014`, `FR-015` per `data-model.md`) — load MUST continue (no abort) when corrupt entries quarantined; verify with `T031` round-trip
- [ ] T006 Implement `CompostingBinStateModel` runtime state (`LivingRoots/Domain/Models/`) holding `Location`, `State` (`Empty`/`Processing`/`Ready`), `InputId`, `DaysRemaining`, `ProgressPaused`; enforce `max stack = 1` when progress active (`FR-005c` / `data-model.md`)
- [ ] T007 Implement `IModDataService` persistence layer (`LivingRoots/Services/ModDataService.cs`) with `SanitizePathSegments()` for tile keys; save keys `composting_bins_{saveId}` / `soil_health_data_{saveId}`; sparse storage (do not store default `0` health) per `AGENTS.md`
- [ ] T008 [P] Implement `SoilHealthService.cs` (`LivingRoots/Services/`) with `GetHealth`, `ApplyCompost`, reactive `OnTileHealthChanged` event, bounds clamp to `[0, 100]` (`ModConstants.Min/Max`); allow application on active crop (`FR-007` clarification 2026-10-04)
- [ ] T009 [P] Implement `CompostingBinService.cs` (`LivingRoots/Services/`) state machine (`Empty → Processing → Ready`); `MaturationDays = 2`; exactly-one batch yield `1`; progress preserved in `Item.modData`; processing pauses unplaced (`FR-001b`, `FR-005c` / `data-model.md`)
- [ ] T010 [P] Implement `VisualizationService.cs` (`LivingRoots/Services/Visualization/`) reactive subscription to `ISoilHealthService.OnTileHealthChanged`; 3-tier RGB interpolation (Poor 1–33 / Moderate 34–66 / Healthy 67–100); `0` = neutral vanilla dirt, no overlay (`FR-009`, `FR-010` / `data-model.md`)
- [ ] T011a [P] Configure `ModController.cs` placement/dislodge handlers with `ExecuteWithConcurrencyGuard` and progress preservation (`FR-005c`)
- [ ] T011b [P] Configure `ModController.cs` right-click compost-application handler with atomically-locked refusal reasons (`FR-007`/`FR-008`/`FR-019`)
- [ ] T011c [P] Configure `ModController.cs` host-authoritative atomic lock mechanism per tile/machine (`Principle VI` / `FR-019`; stub-bounded, D1 debt documented) — use `Interlocked.CompareExchange` on `MachineState.DaysRemaining` (per FR-001b exactly-once guard) for first-wins atomic lock; refusal on second concurrent action preserved
- [ ] T012 [P] Reconcile 8 stale spec/doc scope statements (`specs/001-003` + `docs/design-system`) declaring multiplayer out of scope — scope-word correction only, no redesign; reference `FR-017` / Constitution VI / `plan.md` Constitution Check CONDITIONAL PASS
  FR-016 coverage: confirm README.md under ## Features has zero false/unlabelled claims; if any (Planned) suffix missing, add before declaring T012 complete.

**Checkpoint**: Foundation ready — `ISoilHealthService`, `ICompostingBinService`, persistence, visualization, and atomic-lock controller all functional and independently testable.

---

## Phase 3: User Story 1 — Obtain and place a composting bin (Priority: P1) 🎯 MVP

**Goal**: Crafting recipe visible; bin craftable; placeable on farm / Greenhouse; removable without ghost state.

**Independent Test**: Launch mod → open crafting menu → craft `LivingRoots.CompostingBinRecipe` → place on farm → pick up → verify no stray object; reject placement on water/building (`FR-002` / `quickstart.md` Scenario A).

**Story dependencies**: None (after Phase 2); all other stories eventually depend on bin existing.

- [ ] T012 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/Objects` (`LivingRoots.Compost`) per `contracts/contracts.md` §1.1
- [ ] T013 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/BigCraftables` (`LivingRoots.CompostingBin`) per `contracts/contracts.md` §1.2
- [ ] T014 [US1] Implement `ModEntry.OnAssetRequested` edit for `Data/CraftingRecipes` (`LivingRoots.CompostingBinRecipe`) per `contracts/contracts.md` §1.4
- [ ] T015 [US1] Implement `CompostingBinService.TryPlace()` / removal logic with valid-terrain check (farm / Greenhouse); rejected placement must leave no ghost; dislodge preserves `Item.modData` (`FR-002`, `FR-005c` / `data-model.md`)
- [ ] [P] T016 [US1] Verify `Data/Machines` declaration (`Triggers` + `DaysUntilReady`) registered (native machine interface `FR-001a` / `contracts/contracts.md` §1.3); no bespoke UI (FR-001a / FR-001b / contracts)

---

## Phase 4: User Story 2 — Turn organic waste into compost (Priority: P1)

**Goal**: Bin accepts qualified organic waste; processes exactly 2 in-game days; yields exactly 1 compost; full inventory retries; no destruction.

**Independent Test**: Place empty bin → insert qualifying waste (`FR-003` inherited set) → advance 2 days (`FR-001b`, `FR-004`) → collect 1 compost (`FR-005a`) → verify inventory item exists with correct name/icon; full inventory → collect retries later (`FR-005b` / `quickstart.md` Scenario B, C).

**Story dependencies**: Requires bin from US1 (can test with pre-placed bin independently).

- [ ] T017 [US2] Implement `CompostingBinService.TryInsertOrganicWaste()` with qualifying-set check (inherited, not restated; `FR-003` / `data-model.md`) and native-machine shake/cancel on invalid drop-in (`FR-008a` clarification 2026-10-13)
- [ ] T018 [US2] Implement `CompostingBinService.OnDayStarted()` exactly-once guard (`FR-001b` fallback permitted) with `DaysRemaining` decrement; advance exactly once per day regardless of player count (`FR-004`)
- [ ] T013c [P] Implement `SoilHealthService` daily decay once per day (`FR-013` / `FR-011` / `FR-001`): apply `decayRate * seasonalMultiplier` to bare tilled tiles at day start; seasonal rules 1.5x summer, 0.5x spring/fall, 0 winter; exempt greenhouse; inherited from `specs/002-soil-decay-compost/` (spec §FR-001/FR-011); property-based invariant: health clamped 0–100 after decay. DEPENDENCY NOTE: independent of US1 (bin existence); executes in Phase 2 parallel with T008–T011c; US4 (visual) requires US2 for compost source; no cross-block.
- [ ] T019 [US2] Implement `CompostingBinService.TryCollect()` yielding exactly `1` `LivingRoots.Compost`; full inventory leaves `Ready`; retry delivers full batch; `message.harvest.inventory-full` delivered (`FR-005b` / `FR-008a`)
- [ ] T020 [US2] Enforce `Item.modData` (`LivingRoots.InputId`, `LivingRoots.DaysRemaining`) serializes into inventory bin; `max stack = 1` while progress present; processing pauses unplaced (`FR-005c` / `data-model.md`)
- [ ] T021 [P] [US2] Implement `ModEntry.OnAssetRequested` edit for `Data/Machines` (`Triggers`, `DaysUntilReady = 2`, output quantity `1`) per `contracts/contracts.md` §1.3 (FR-001b / FR-005a)

---

## Phase 5: User Story 3 — See soil health change on the ground (Priority: P2)

**Goal**: Compost application +15 pts clamped to 100 on tilled `HoeDirt`; visible 3-tier RGB overlay; 0 = neutral; ≤16.67ms reactive update.

**Independent Test**: Apply compost to tilled tile → health increases by 15 (clamped); visualize tier change; toggle display off → soil improves, overlay hidden (`FR-009`, `FR-010` / `quickstart.md` Scenario D, E).

**Story dependencies**: Requires compost item from US2 (can test with manually added item independently).

- [ ] T022 [US3] Implement `CompostApplicationService.TryApply()` consuming exactly `1` compost; `+15` clamped to `100`; refused on untilled / non-farm / `100` / building / indoor (`FR-007`, `FR-008` / `data-model.md`)
- [ ] T023 [US3] Wire refusal messages through `Game1.showRedMessage` + `ITranslationHelper` with keys `message.compost.invalid-tile`, `message.compost.max-health`, `message.compost.action-conflict` (no custom UI; `FR-008a` / `contracts/contracts.md` §3)
- [ ] T024 [US3] Implement `VisualizationService` 3-tier RGB interpolation and ambient-light blending; `0` health renders neutral vanilla dirt without overlay (`FR-009`, `FR-010` / `data-model.md`)
- [ ] T025 [US3] Confirm `ISoilHealthService.OnTileHealthChanged` triggers `VisualizationService` cache invalidation on next frame (`≤16.67ms`); zero polling (`FR-010` / `data-model.md`)
- [ ] T026 [P] [US3] Allow application on active growing crop (side-dressing / top-dressing); crop not harmed or removed (`FR-007` clarification 2026-10-04) (FR-007 / clarification 2026-10-04)

---

## Phase 6: User Story 4 — Trust what the project says about itself (Priority: P3)

**Goal**: README feature claims accurate; 8 stale spec/docs reconciled against Constitution VI; `(Planned)` labels on unbuilt capabilities.

**Independent Test**: Read `README.md` → every claim observable or labelled `(Planned)`; check 8 docs (`specs/001-003` + design-system docs) for multiplayer out-of-scope contradictions (`FR-016`, `FR-017`, `SC-006`, `SC-007` / `spec.md` lines 244–247, 302–303).

**Story dependencies**: None (documentation); ideally after US1–US3 verified but can proceed independently.

- [ ] T027 [US4] Audit `README.md` `## Features`; label all unbuilt capabilities with `(Planned)` suffix; remove false claims (`FR-016` / `spec.md` clarification 2026-10-04)
- [ ] T028 [US4] Reconcile scope statements in 8 stale docs (`specs/001-soil-health-visualization/{spec,plan}.md`, `specs/002-soil-decay-compost/{spec,plan}.md`, `specs/003-composting-tests/{spec,plan}.md`, `docs/design-system/REALITY-CHECK-EVALUATION.md`, `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`) — wording correction only, no redesign (`FR-017`)
- [ ] T029 [US4] Verify no spec declares multiplayer out of scope where it contradicts Constitution Principle VI (`SC-007` / `spec.md` Assumptions 302–303)

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Concurrency verification, save/load fault isolation, security, format, documentation of D1 debt

- [ ] T030 [P] Implement `MultiplayerConcurrencyTests.cs` automated multi-threaded stub / `ThreadSafeGameLoopEventsStub` verifying first-wins atomic lock (`Principle VI` / FR-019) and `OnTileHealthChanged` reactive invalidation (`SC-05`, `SC-10`; stub-bounded, not live proof — D1 debt explicit per spec lines 302–303 / Constitution §VI). ADD: simulated disconnect/reconnect sequence + network-fault injection (message drop, reorder) required to approach interleaving coverage; stub alone never reproduces live SMAPI message ordering (CHESS/CTrigger/Jepsen limits documented). (FR-019 / Principle VI)
- [ ] T031b [P] Property-based invariant tests (FsCheck `ForAll`): soil health always clamped 0–100; compost application always +15 (or refused); bin state-machine never regresses; first-wins concurrent action always yields exactly one applied change; visual overlay distinguishable from untreated neighbor (treated tile health > neighbor or overlay color diff > 0) — delegated to subagent wave (reuse T000-R5 context)
- [ ] T031 [P] Implement save/load round-trip tests (`SC-04`, `FR-012`): corrupt / obsolete / orphaned entries quarantined; valid entries preserved; visual overlays re-rendered. Note: Integration round-trip: builds on T000-R1 invariant; validates save/load with corrupt + orphan + obsolete entries.
- [ ] T032 [P] Security validation: `ModDataService.SanitizePathSegments()` on tile keys; no raw exception messages to output (`FR-015` / `AGENTS.md` — `ex.GetType().FullName` + `HResult` at `LogLevel.Trace` only, never `Message`); `CheckForOverflowUnderflow` never suppressed; bounds `0–100` enforced (`Principle II`)
- [ ] T033 [P] Format verification: `dotnet format Stardew-LivingRoots.sln --verify-no-changes`; build green (`dotnet build --nologo`) (`Gate 1` / `AGENTS.md`)
- [ ] T034 [P] Run `quickstart.md` scenarios A–H; confirm Gate 2 (product-level) and Gate 3 (end-to-end real-player journey) satisfied; document D1 live-verification debt (`Principle VI` / FR-018/FR-019) in task notes, not hidden as complete; verification-only attestation — MUST NOT declare complete for multi Gate 3 until D1 runs
- [ ] T034b [P] Explicit SC-003 audit: verify 100% of observed soil health values within [0,100] across all bin lifecycle states; property-based invariant `ForAll<int>(h => h >= 0 && h <= 100)`
- [ ] T035b [P] Explicit SC-009 audit: repeated collect from full inventory + pick-up/re-place mid-batch + floating-world-debris with unavailable inventory; zero destruction attested
- [ ] T035 [P] Confirm `Item.modData` progress serialization works across pickup / placement / dislodgement; floating-world-debris case with full inventory (`FR-005c` / `data-model.md`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1; **BLOCKS all user stories** (`T005`–`T011c` must finish before `T012`+).
- **Phase 3 (US1)**: After Phase 2; can begin in parallel with US2/US3/US4 if staffed (independent tests), but US2 requires bin object existence (sequential dependency for full loop).
- **Phase 4 (US2)**: After Phase 2; depends on US1 for full journey, but independently testable with pre-placed bin.
- **Phase 5 (US3)**: After Phase 2 + US2 for compost source; independently testable with manually added compost.
- **Phase 6 (US4)**: After Phase 2; no runtime dependency on US1–US3 for docs, but accuracy requires verification against implemented behavior.
- **Phase 7 (Polish)**: After all desired stories complete; includes concurrency stubs (`T030`) and quickstart validation (`T034`).

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

- `T003` / `T004` / `T012`–`T014` / `T015` (different files, no shared state) — `[P]` marked.
DEPENDENCY: T003 (structural skeleton) must exist before T000-R1 (RED tests) can compile, but T003 contains no production logic — structural only.
- `T005`–`T011c` (foundational) can run in parallel within Phase 2 if staffed.
- Once Phase 2 completes: `US1`, `US2` (with fixtures), `US3` (with fixtures), `US4` can proceed in parallel by different developers.
- `T030` / `T031` / `T032` / `T033` (polish / stub / security / format) independent of story order — can start once relevant service code exists.

---

## Parallel Example: User Story 1

```bash
# Launch asset registrations + placement logic in parallel (different files):
T012: LivingRoots/ModEntry.cs — Data/Objects edit
T013: LivingRoots/ModEntry.cs — Data/BigCraftables edit
T014: LivingRoots/ModEntry.cs — Data/CraftingRecipes edit
T015: LivingRoots/Services/CompostingBinService.cs — TryPlace / removal
T016 [P]: LivingRoots/ModEntry.cs — Data/Machines declaration verification
```

---

## Implementation Strategy

### MVP (User Story 1 Only + Foundational)

1. Complete Phase 1 + Phase 2 (`T001`–`T011c`).
2. Complete Phase 3 (`T012`–`T016`).
3. **STOP / VALIDATE**: Independent test — craft → place → pick up → verify no ghost (`quickstart.md` A). Confirm `ModEntry.OnAssetRequested` delivers real `Data/Objects`, `Data/BigCraftables`, `Data/CraftingRecipes`, `Data/Machines`.
4. Deploy / demo.

### Incremental Delivery

1. Phase 1 + 2 → Foundation.
2. Phase 3 (US1) → Bin exists and is playable.
3. Phase 4 (US2) → Loop closes (waste → 2-day → compost). Verify with `quickstart.md` B/C.
4. Phase 5 (US3) → Soil change visible. Verify with `quickstart.md` D/E.
5. Phase 6 (US4) → Docs accurate; reconciliation complete. Verify with `quickstart.md` H.
6. Phase 7 (Polish) → Stubs, save/load, security, format, D1 debt documented.

### Team Strategy

- Developer A: Phase 2 (persistence, soil health, visualization, controller) + Phase 3.
- Developer B: Phase 2 (bin service, contracts) + Phase 4.
- Developer C: Phase 5 (application, visual) + Phase 6 (docs) + Phase 7 (stubs / security / format).

---

## Notes

- All tasks use exact file paths from `AGENTS.md` / `plan.md` project structure.
- Constraint quotes from `data-model.md` embedded in tasks (e.g., `max stack = 1` in `T020`, `+15` clamped in `T022`).
- `T030` explicitly bounded as stub-only; live 2-player verification deferred to Roadmap D1 (`spec.md` Assumptions / Constitution §VI / `quickstart.md` G).
- No bespoke UI / text-drawing components (`FR-008a`; `Principle V`).
- `T034` validates `quickstart.md` scenarios; Gate 3 requires real-player journey through full loop (`spec.md` Success Criteria / `AGENTS.md`).
