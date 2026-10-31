# Tasks: Mulch / Soil Cover (Spec 013)

**Input**: Design documents from `/specs/013-mulch-soil-cover/`
**Prerequisites**: plan.md, spec.md, data-model.md, quickstart.md, research.md, constitution.md

**Tests**: REQUIRED. TDD (Red-Green-Refactor). Property-based tests (FsCheck) for invariants where applicable.

**Organization**: Tasks grouped by user story. Phase 2 is a global test-first gate (RED). No implementation may start until tests are written and failing for the right reason.

## Path Conventions

- Mod project: `LivingRoots/`
- Test project: `LivingRoots.Tests/`
- Constants: `LivingRoots/Constants.cs` (class `ModConstants`)
- Domain interfaces: `LivingRoots/Domain/Interfaces/`
- Services: `LivingRoots/Services/`
- Controllers: `LivingRoots/Controllers/ModController.cs`
- Entry point: `LivingRoots/ModEntry.cs`
- Assets: `LivingRoots/Assets/`
- i18n: `LivingRoots/i18n/`
- Data: registered via `ModEntry.OnAssetRequested`

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: [US1], [US2], [US3] — user stories from spec.md
- Setup, foundational, polish, and verification tasks have no story label
- Every task names the exact file(s) it edits or creates

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Constants, asset placeholder, i18n keys — scaffold before any logic.

- [ ] T001 [P] Add mulch constants to `LivingRoots/Constants.cs` (`ModConstants`): `MulchItemId = "LivingRoots.Mulch"`, `QualifiedMulchItemId = "(O)LivingRoots.Mulch"`, `MulchDurationDays = 7`, `MulchDecayMultiplier = 0f`, `MulchTag = "mulch_item"`, `MulchColor = new Color(146, 64, 14)` (#92400E). File: `LivingRoots/Constants.cs`
- [ ] T002 [P] Create placeholder sprite `LivingRoots/Assets/mulch.png` (16×16, base #92400E with highlights #A16207/#78716C). File: `LivingRoots/Assets/mulch.png`
- [ ] T003 [P] Add i18n keys to `LivingRoots/i18n/en.json` and `LivingRoots/i18n/default.json`: `mulch.name`, `mulch.description`, `message.mulch.invalid-tile`, `message.mulch.already`. Files: `LivingRoots/i18n/en.json`, `LivingRoots/i18n/default.json`
- [ ] T004 [P] Register `LivingRoots_Mulch` object and `LivingRoots_MulchRecipe` in `ModEntry.OnAssetRequested` (following compost pattern at `ModEntry.cs:116-151`). Object: Category −26, ContextTags `{"mulch_item", "compostable_item"}`, Price 5, Texture `Mods/LivingRoots/Mulch`, SpriteIndex 0. Recipe: `"771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"`. Files: `LivingRoots/ModEntry.cs`

**Checkpoint**: Constants, asset, i18n, and Data registration in place. No logic yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 [P] Create `IMulchApplicationService` interface in `LivingRoots/Domain/Interfaces/IMulchApplicationService.cs` with method `bool TryApplyMulch(Farmer who, HoeDirt hoeDirt, GameLocation location, out string messageKey)`. Follow DDD: interface in Domain. File: `LivingRoots/Domain/Interfaces/IMulchApplicationService.cs`
- [ ] T006 [P] Add `HasMulch` (bool) and `DaysRemaining` (int) to the tile state model / persistence contract. Ensure sparse cache (omit defaults). File: `LivingRoots/Domain/Models/SoilHealthState.cs` (or new `MulchTileState` if separate)
- [ ] T007 Modify `SoilDecayService.ProcessDayStart` to check `tile.HasMulch` → `decay *= ModConstants.MulchDecayMultiplier (0f)`. **CRITICAL**: mulch day-countdown must run **outside** the Winter early-return guard (`if (multiplier == 0f) return;`) so expiry is day-based, not season-based. Decrement `DaysRemaining` each day; when 0, set `HasMulch = false`. File: `LivingRoots/Services/SoilDecayService.cs`
- [ ] T008 Add `MulchColor` and `MulchTag` consumption in `VisualizationService` / `OverlayRenderer` — render mulched tiles with brown tint (MulchColor) at 0.5 opacity (DefaultOpacity) + `Dots` pattern. Composite render order: health color base → mulch tint/pattern → dotted border for mulch+plant. File: `LivingRoots/Services/Visualization/OverlayRenderer.cs`
- [ ] T009 Wire `IMulchApplicationService` in `ModEntry.cs`: instantiate `new MulchApplicationService(...)` and inject into `ModController` constructor. File: `LivingRoots/ModEntry.cs`

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 — Apply Mulch to Protect Soil (Priority: P1) 🎯 MVP

**Goal**: Player can apply mulch to a tilled tile to zero soil decay for 7 days.

**Independent Test**: Craft mulch with 10 fiber, apply to tilled tile, advance 7 days, confirm soil health unchanged; on day 8, decay resumes.

### Tests for User Story 1 (RED — write first, must FAIL before implementation)

- [ ] T010 [P] [US1] Test: `TryApplyMulch` on non-tilled tile returns false and message key `message.mulch.invalid-tile`. File: `LivingRoots.Tests/MulchApplicationServiceTests.cs`
- [ ] T011 [P] [US1] Test: `TryApplyMulch` on already-mulched tile returns false and message key `message.mulch.already`. File: `LivingRoots.Tests/MulchApplicationServiceTests.cs`
- [ ] T012 [P] [US1] Test: `TryApplyMulch` on tilled tile (bare) sets `HasMulch = true`, `DaysRemaining = 7`, consumes 1 mulch from inventory, returns true. File: `LivingRoots.Tests/MulchApplicationServiceTests.cs`
- [ ] T013 [P] [US1] Test: `TryApplyMulch` on tilled tile with growing crop sets `HasMulch = true` (no crop-presence guard). File: `LivingRoots.Tests/MulchApplicationServiceTests.cs`
- [ ] T014 [P] [US1] Test: `SoilDecayService.ProcessDayStart` with mulched tile → decay = 0 (0 × seasonal). Property-based: for any season, mulched tile health unchanged. File: `LivingRoots.Tests/MulchDecayTests.cs`
- [ ] T015 [P] [US1] Test: mulch expires after 7 days → decay resumes at normal rate (respecting seasonal multiplier). File: `LivingRoots.Tests/MulchDecayTests.cs`
- [ ] T016 [P] [US1] Test: rain does not remove mulch (decay stays 0). File: `LivingRoots.Tests/MulchDecayTests.cs`
- [ ] T017 [P] [US1] Test: mulch state persists via `ModDataService` (sparse cache — zeros omitted). File: `LivingRoots.Tests/MulchPersistenceTests.cs`
- [ ] T018 [P] [US1] Test: mulch + compost coexistence on same tile (both states independent). File: `LivingRoots.Tests/MulchPersistenceTests.cs`

### Implementation for User Story 1

- [ ] T019 [US1] Implement `MulchApplicationService` in `LivingRoots/Services/MulchApplicationService.cs`: validate `HoeDirt` terrain (no crop check), check `HasMulch` (already → `message.mulch.already`), check non-tilled (→ `message.mulch.invalid-tile`), set `HasMulch = true` + `DaysRemaining = MulchDurationDays`, consume 1 mulch from inventory, host-authoritative guard (`Context.IsMainPlayer`), `ExecuteWithConcurrencyGuard`. Follow `CompostApplicationService` pattern. File: `LivingRoots/Services/MulchApplicationService.cs`
- [ ] T020 [US1] Hook mulch application in `ModController.OnButtonPressed` (right-click with mulch item), following compost pattern at `ModController.cs:845`. Guard with `ExecuteWithConcurrencyGuard`. File: `LivingRoots/Controllers/ModController.cs`
- [ ] T021 [US1] Ensure `SoilDecayService` decrements `DaysRemaining` outside Winter early-return guard. File: `LivingRoots/Services/SoilDecayService.cs`

**Checkpoint**: User Story 1 fully functional and testable independently. Mulch protects soil for 7 days.

---

## Phase 4: User Story 2 — Craft Mulch Early (Priority: P1)

**Goal**: Player can craft mulch from fiber on day 1.

**Independent Test**: With 10 fiber in inventory, craft mulch at crafting menu; confirm 1 mulch produced and 10 fiber consumed.

### Tests for User Story 2 (RED — write first, must FAIL before implementation)

- [ ] T022 [P] [US2] Test: `Data/Objects` contains `LivingRoots_Mulch` with Category −26 and ContextTags `{"mulch_item", "compostable_item"}`. File: `LivingRoots.Tests/MulchDataTests.cs`
- [ ] T023 [P] [US2] Test: crafting recipe `LivingRoots_MulchRecipe` is parseable in legacy format and produces 1 mulch from 10 fiber. File: `LivingRoots.Tests/MulchDataTests.cs`
- [ ] T024 [P] [US2] Test: recipe is available on day 1 (no Elena dependency for craftable path). File: `LivingRoots.Tests/MulchDataTests.cs`

### Implementation for User Story 2

- [ ] T025 [US2] Verify `Data/Objects` and `Data/CraftingRecipes` registration via `ModEntry.OnAssetRequested` (done in T004). Confirm `mulch.png` sprite loads. File: `LivingRoots/ModEntry.cs`
- [ ] T026 [US2] Verify i18n keys resolve in both `en.json` and `default.json`. File: `LivingRoots/i18n/en.json`, `LivingRoots/i18n/default.json`

**Checkpoint**: User Story 2 complete. Mulch craftable day 1 with fiber.

---

## Phase 5: User Story 3 — Visual Mulch Indicator (Priority: P2)

**Goal**: Player can see which tiles are mulched via overlay.

**Independent Test**: Apply mulch to a tile; confirm overlay shows brown tint (`MulchColor`) with `Dots` pattern at 0.5 opacity.

### Tests for User Story 3 (RED — write first, must FAIL before implementation)

- [ ] T027 [P] [US3] Test: mulched tile overlay uses `MulchColor` (#92400E) at 0.5 opacity with `Dots` pattern. File: `LivingRoots.Tests/MulchOverlayTests.cs`
- [ ] T028 [P] [US3] Test: mulched tile with plant shows dotted border (mulch + plant indicator). File: `LivingRoots.Tests/MulchOverlayTests.cs`
- [ ] T029 [P] [US3] Test: overlay color is applied via `IVisualizationService` (never hardcoded in controller). File: `LivingRoots.Tests/MulchOverlayTests.cs`
- [ ] T030 [P] [US3] Test: overlay composite render order — health color base → mulch tint → Dots pattern → dotted border for crop. File: `LivingRoots.Tests/MulchOverlayTests.cs`

### Implementation for User Story 3

- [ ] T031 [US3] Implement mulch overlay in `OverlayRenderer`: apply `MulchColor` tint at `DefaultOpacity` (0.5) + `Dots` pattern when `tile.HasMulch`. Composite with health color base and crop indicator (dotted border). File: `LivingRoots/Services/Visualization/OverlayRenderer.cs`
- [ ] T032 [US3] Verify overlay z-order does not conflict with spec 001 health overlays (manual visual check). File: `LivingRoots/Services/Visualization/OverlayRenderer.cs`

**Checkpoint**: User Story 3 complete. Mulched tiles visually distinct.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements affecting multiple user stories.

- [ ] T033 [P] Add unit tests for `MulchApplicationService` edge cases (null farmer, null hoeDirt, null location). File: `LivingRoots.Tests/MulchApplicationServiceTests.cs`
- [ ] T034 [P] Add property-based test: for any tile with mulch, decay = 0 regardless of season (FsCheck). File: `LivingRoots.Tests/MulchDecayTests.cs`
- [ ] T035 [P] Add property-based test: mulch expiry is exactly 7 days (for any start day). File: `LivingRoots.Tests/MulchDecayTests.cs`
- [ ] T036 [P] Verify `dotnet format --verify-no-changes` passes clean. File: `LivingRoots/`
- [ ] T037 [P] Verify no global `NoWarn` added; all suppressions scoped and justified. File: `LivingRoots/`
- [ ] T038 [P] Add multiplayer stub test: farmhand applies mulch → host-mediated, lock acquired per tile. File: `LivingRoots.Tests/MulchMultiplayerTests.cs`
- [ ] T039 Verify `CheckForOverflowUnderflow=true` respected (no overflow suppression). File: `LivingRoots/`

**Checkpoint**: Polish complete. All tests green, format clean.

---

## Phase 7: Verification (Final)

**Purpose**: Independently verify the implementation. Only these tasks attest to success.

- [ ] T040 Run `dotnet build Stardew-LivingRoots.sln -c Release` → 0 warnings, 0 errors. Save to `specs/013-mulch-soil-cover/verification/build.log`. File: `specs/013-mulch-soil-cover/verification/build.log`
- [ ] T041 Run `dotnet test Stardew-LivingRoots.sln -c Release --no-build` → 0 failures, baseline 1436+ tests pass. Save to `specs/013-mulch-soil-cover/verification/test.log`. File: `specs/013-mulch-soil-cover/verification/test.log`
- [ ] T042 Run `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → clean. Save to `specs/013-mulch-soil-cover/verification/format.log`. File: `specs/013-mulch-soil-cover/verification/format.log`
- [ ] T043 Run `bash scripts/quality-gate/analyzer-probe.sh` → PASS. Save to `specs/013-mulch-soil-cover/verification/analyzer-probe.log`. File: `specs/013-mulch-soil-cover/verification/analyzer-probe.log`
- [ ] T044 Verify all FR-001..FR-020 and SC-001..SC-011 are satisfied (manual checklist review). File: `specs/013-mulch-soil-cover/verification.md`
- [ ] T045 Verify quickstart.md scenarios 1-10 pass (manual or automated). File: `specs/013-mulch-soil-cover/verification/quickstart.log`
- [ ] T046 Run atomic-commit audit: `git log --oneline --no-merges` for implementation commits; verify format. File: `specs/013-mulch-soil-cover/verification/commit-audit.log`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. BLOCKS all user stories.
- **Phase 3 (US1)**: Depends on Phase 2. No dependencies on other stories.
- **Phase 4 (US2)**: Depends on Phase 2. No dependencies on other stories (can run in parallel with US1).
- **Phase 5 (US3)**: Depends on Phase 2. Depends on US1 (overlay needs mulch state from application service).
- **Phase 6 (Polish)**: Depends on US1, US2, US3.
- **Phase 7 (Verification)**: Depends on Phase 6.

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2. No dependencies on other stories.
- **US2 (P1)**: Can start after Phase 2. Independent of US1.
- **US3 (P2)**: Can start after Phase 2. Depends on US1 (needs mulch state for overlay).

### Within Each User Story

- Tests (RED) MUST be written and FAIL before implementation.
- Models before services.
- Services before controller hooks.
- Core implementation before integration.
- Story complete before moving to next priority.

### Parallel Opportunities

- Phase 1: T001, T002, T003, T004 all [P] (disjoint files).
- Phase 2: T005, T006 [P] (disjoint files); T007, T008, T009 sequential (shared SoilDecayService/OverlayRenderer/ModEntry).
- Phase 3 tests: T010–T018 all [P] (same file but independent test methods — can be written in parallel by one agent).
- Phase 3 impl: T019 → T020 → T021 sequential (shared files).
- Phase 4 tests: T022–T024 [P].
- Phase 5 tests: T027–T030 [P].
- Phase 6: T033–T039 all [P] (disjoint files).
- US1 and US2 can start in parallel after Phase 2 (different files).

### Subagent Ownership Map

| Subagent | Owns | Reuse reason |
|---|---|---|
| Agent A — .NET core | T001–T009, T019–T021, T025–T026, T031–T032 | Constants, services, controller, ModEntry |
| Agent B — tests | T010–T018, T022–T024, T027–T030, T033–T035, T038 | Test files, one per subject |
| Agent C — visualization | T008, T031–T032 | OverlayRenderer, MulchColor integration |
| Agent D — verification | T040–T046 | Fresh context, independent attestation |

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1: Setup (constants, asset, i18n, Data registration).
2. Complete Phase 2: Foundational (interface, persistence, decay hook, overlay, wiring).
3. Complete Phase 3: User Story 1 (tests RED → implement → GREEN).
4. **STOP and VALIDATE**: Test US1 independently (mulch protects soil 7 days).
5. Deploy/demo if ready.

### Incremental Delivery

1. Setup + Foundational → Foundation ready.
2. Add US1 → Test independently → Deploy/Demo (MVP!).
3. Add US2 → Test independently → Deploy/Demo.
4. Add US3 → Test independently → Deploy/Demo.
5. Polish + Verification.

### Parallel Team Strategy

With multiple developers:
1. Team completes Setup + Foundational together.
2. Once Foundational is done:
   - Developer A: US1 (apply mulch + decay protection)
   - Developer B: US2 (crafting + data)
   - Developer C: US3 (overlay visualization)
3. Stories complete and integrate independently.

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story independently completable and testable
- Verify tests FAIL before implementing (RED phase)
- Commit after each task or logical group (atomic conventional commit)
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies
- **TDD RED gate**: Phase 3/4/5 tests MUST be written and failing before implementation starts
- **Property-based testing**: FsCheck for invariants (decay × multiplier, duration expiry, seasonal synergy)
- **Constitution**: all 6 principles must be respected (DDD, security, async, TDD, YAGNI, multiplayer)