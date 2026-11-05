# Tasks: Gate 3 Runner Hardening

**Input**: Design documents from `/specs/016-gate3-runner-hardening/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: TDD enforced — tests written first, must FAIL (RED) before implementation. Property-based testing for invariants where applicable.

**Organization**: Tasks grouped by user story (P1, P2, P3) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Shell scripts: `scripts/`, `scripts/lib/`, `scripts/tests/`
- Contracts: `specs/016-gate3-runner-hardening/contracts/`
- Verification: `specs/010-headless-verification-runner/verification/`
- Evidence bundles: `evidence-bundle-run-*/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm baseline state and prepare for hardening work

- [X] T001 Verify clean build: `GamePath=... dotnet build Stardew-LivingRoots.sln --configuration Release` exits 0 (baseline)
- [X] T002 [P] Verify shell syntax: `bash -n` on all `scripts/**/*.sh` passes
- [X] T003 [P] Verify format: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exits 0

**Checkpoint**: Baseline confirmed green before any changes

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update contracts and data-model to reflect clarified decisions; must complete before user story work

- [X] T004 Update `specs/016-gate3-runner-hardening/data-model.md` SKIP set to match verifier code: `manifest.json`, `manifest.sha256`, `.manifest-input.json`, `.attempts_list` (not "all dotfiles"); also document FR-006 ordering (claims.json before manifest assembly)
- [X] T005 [P] Update `specs/016-gate3-runner-hardening/contracts/verifier-completeness.md` to state the SKIP set explicitly and the exit code 19 on violation
- [X] T006 [P] Update `specs/016-gate3-runner-hardening/contracts/log-capture.md` to reference `RUNNER_SMAPI_CAPTURE` env var (already done in clarify; verify consistency)
- [X] T007 [P] Update `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` subject description to clarify completeness (documentation only; no schema bump required unless field set changes, per FR-005)

**Checkpoint**: Contracts and data-model aligned with implementation

---

## Phase 3: User Story 1 — Regression Guards for the Three Defects (Priority: P1) 🎯 MVP

**Goal**: Make the three defects that blocked Gate 3 impossible to reintroduce silently.

**Independent Test**: Revert each fix in isolation → corresponding test fails → restore fix → test passes.

### Tests for User Story 1 (RED PHASE — write first, must FAIL before implementation)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T008 [P] [US1] Add RED test: `scripts/tests/test-save-preservation.sh` asserts `save_prepare` fails loudly (exit 19) when milestone save folder and inner file name disagree and `save_align_inner_name` is disabled
- [X] T009 [P] [US1] Add RED test: `scripts/tests/test-smapi-log-full.sh` asserts `artifacts/smapi-log.txt` is byte-identical to the source SMAPI log (not a filtered excerpt)
- [X] T010 [P] [US1] Add RED test: `scripts/tests/test-manifest-schema.sh` asserts `verify-evidence-bundle.sh` returns 19 when `manifest.json` is not schema-valid
- [X] T011 [P] [US1] Add RED test: `scripts/tests/test-manifest-schema.sh` asserts `verify-evidence-bundle.sh` returns 19 when `manifest.sha256` does not match manifest bytes
- [X] T012 [US1] Run RED tests, confirm all FAIL with intended assertions (record in `red-gate-phase-6.log`)

**Checkpoint (RED)**: All US1 tests fail for the right reason before any implementation

### Implementation for User Story 1 (GREEN PHASE)

- [X] T013 [P] [US1] Ensure `save_align_inner_name` in `scripts/lib/save.sh` renames single non-sidecar file to match folder; fail loudly (exit 19) on zero or multiple candidates
- [X] T014 [P] [US1] Ensure `signals_smapi_log_excerpt` in `scripts/lib/signals.sh` performs full `cp -f` (not filtered grep); document capture level
- [X] T015 [US1] Ensure `evidence_finalize` in `scripts/lib/evidence.sh` invokes `ManifestBuilder` with full payload; `verify-evidence-bundle.sh` validates schema + digest
- [X] T048 [P] [US1] Add RED test: `scripts/tests/test-home-preservation.sh` asserts `save_prepare` preserves original HOME for Python helpers (exit 0 with HOME unchanged after save isolation)
- [X] T049 [US1] Ensure `save_prepare` in `scripts/lib/save.sh` preserves original HOME in `RUNNER_ORIGINAL_HOME` before rewriting HOME/XDG (FR-008)
- [X] T016 [US1] Run US1 tests, confirm all PASS (GREEN)

**Checkpoint**: US1 complete — regression guards for all three defects in place

---

## Phase 4: User Story 2 — Complete Subject Coverage (Priority: P2)

**Goal**: Make every bundle file covered by `subject[]` so the detached anchor is tamper-evident for the whole directory.

**Independent Test**: Add an uncovered file to a valid bundle → verifier returns 19.

### Tests for User Story 2 (RED PHASE — write first, must FAIL before implementation)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [P] [US2] Add RED test: `scripts/tests/test-bundle-negative.sh` asserts verifier returns 19 when an extra file is added to a bundle that is not in `subject[]`
- [X] T018 [P] [US2] Add RED test: `scripts/tests/test-bundle-negative.sh` asserts verifier returns 0 when `subject[]` covers every bundle file
- [X] T019 [US2] Run RED tests, confirm FAIL with intended assertions

**Checkpoint (RED)**: US2 tests fail for the right reason

### Implementation for User Story 2 (GREEN PHASE)

- [X] T020 [US2] Add completeness check to `scripts/verify-evidence-bundle.sh`: enumerate all bundle files, verify each (except SKIP set) appears in `subject[]` with matching digest; return 19 on violation
- [X] T021 [US2] Update `evidence_finalize` in `scripts/lib/evidence.sh` to move `retention_receipt` before manifest input assembly so `retention-receipt.json` is covered
- [X] T022 [US2] Run US2 tests, confirm all PASS (GREEN)

**Checkpoint**: US2 complete — subject[] covers every bundle file

---

## Phase 5: User Story 3 — Genuine Red-Gate Evidence (Priority: P3)

**Goal**: Document the red-gate deviation honestly rather than fabricating evidence.

**Independent Test**: Each red-gate log contains either a failing assertion or a documented deviation statement.

### Tests for User Story 3 (RED PHASE — write first, must FAIL before implementation)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T023 [P] [US3] Add RED test: `scripts/tests/test-red-gate-evidence.sh` asserts each `red-gate-phase-*.log` contains either `not ok`/`FAIL` or a documented deviation statement
- [X] T024 [US3] Run RED test, confirm FAIL (current logs have neither genuine RED nor deviation statement)

**Checkpoint (RED)**: US3 test fails for the right reason

### Implementation for User Story 3 (GREEN PHASE)

- [X] T025 [US3] Add deviation statement to each `red-gate-phase-*.log`: "Implementation pre-existed; genuine RED evidence cannot be reconstructed retroactively. Tests are regression guards, not TDD RED evidence."
- [X] T026 [US3] Update `specs/010-headless-verification-runner/tasks.md` T130 to reflect the documented deviation (not a fabricated RED gate)
- [X] T027 [US3] Run US3 test, confirm PASS (GREEN)

**Checkpoint**: US3 complete — red-gate evidence is honest

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories (verification tasks are in Phase 7 only)

- [X] T028 [P] Update `specs/010-headless-verification-runner/verification/schema.log` with post-fix bundle validation (already done; verify consistency)
- [X] T029 [P] Update `specs/010-headless-verification-runner/verification/real-host.log` with final run details (already done; verify consistency)

---

## Phase 7: Verification (ONLY these tasks attest success)

**Purpose**: Documentation, hardening evidence, and the authoritative verification pass. Implementation agents must not claim completion before T036–T045. Phase 7 only executes existing test suites and records the RED/green path.

- [X] T036 **Verification — build**: run `dotnet build` (Release), record in `verification/build.log`
- [X] T037 **Verification — tests**: run `dotnet test` + `run-all.sh`, record counts in `verification/test.log`
- [X] T038 **Verification — format**: run `dotnet format --verify-no-changes`, record in `verification/format.log`
- [X] T039 **Verification — shell**: run `bash -n` on all scripts, record in `verification/shell.log`
- [X] T040 **Verification — schema**: run schema validation, record 30/30 valid, 14/14 invalid, 2/2 runner-invalid, 0 mismatches in `verification/schema.log`
- [X] T041 **Verification — launch-free e2e**: run all 12 VS cases, record in `verification/launch-free.log`
- [X] T042 **Verification — real-host e2e**: run VS-01 on real host, record in `verification/real-host.log`
- [X] T043 **Verification — subject completeness**: add uncovered file to bundle, confirm verifier returns 19, record in `verification/bundle-negative.log`
- [X] T044 **Verification — save alignment**: revert `save_align_inner_name`, confirm test fails, restore, confirm pass, record in `verification/`
- [X] T045 **Verification — log capture**: revert full-log capture, confirm test fails, restore, confirm pass, record in `verification/`
- [X] T046 **Verification — red-gate evidence**: confirm each log has deviation statement or failing assertion, record in `verification/`
- [X] T047 **Final commit**: commit all artifacts with `docs(016): ...`; list every artifact in commit body; do not amend earlier commits

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — RED tests first, then implementation
- **Phase 4 (US2)**: Depends on Phase 2 — RED tests first, then implementation
- **Phase 5 (US3)**: Depends on Phase 2 — RED tests first, then implementation
- **Phase 6 (Polish)**: Depends on US1, US2, US3
- **Phase 7 (Verification)**: Depends on Phase 6

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependencies on other stories
- **US2 (P2)**: Can start after Phase 2 — depends on US1's verifier infrastructure (T020 builds on T015)
- **US3 (P3)**: Can start after Phase 2 — independent of US1/US2

### Within Each User Story

- Tests (RED) MUST be written and FAIL before implementation (GREEN)
- Tests marked [P] can run in parallel
- Implementation tasks marked [P] can run in parallel (different files)
- Story complete before moving to next priority

### Parallel Opportunities

- Phase 2: T005, T006, T007 can run in parallel
- US1 tests: T008, T009, T010, T011 can run in parallel
- US1 impl: T013, T014 can run in parallel
- US2 tests: T017, T018 can run in parallel
- US3 tests: only T023 (single test)
- Phase 6: T028, T029, T030, T031 can run in parallel

### Parallel Example: User Story 1 (RED phase)

```bash
# Launch all RED tests for US1 together:
Task: "Add RED test for save-name alignment in scripts/tests/test-save-preservation.sh"
Task: "Add RED test for full SMAPI log in scripts/tests/test-smapi-log-full.sh"
Task: "Add RED test for manifest schema in scripts/tests/test-manifest-schema.sh"
# Then run all, confirm FAIL, then implement
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US1 (RED tests → GREEN implementation)
4. **STOP and VALIDATE**: US1 regression guards pass
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1 (regression guards) → Test independently → MVP
3. Add US2 (subject completeness) → Test independently
4. Add US3 (red-gate documentation) → Test independently
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple agents:
1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Agent A: US1 (regression guards)
   - Agent B: US2 (subject completeness)
   - Agent C: US3 (red-gate documentation)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (RED before GREEN)
- Commit after each task or logical group (atomic conventional commits)
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- TDD enforced: tests written first, must FAIL before implementation makes them pass
- Property-based testing: use shell-based fuzzing (random inputs to `verify-evidence-bundle.sh`) for invariants (e.g., subject completeness, digest matching) where applicable; FsCheck is C#-only and not applicable to shell tests