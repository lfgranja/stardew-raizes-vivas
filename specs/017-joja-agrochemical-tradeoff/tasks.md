---
description: "Task list for 017 Joja / Agrochemical Tradeoff (MVP)"
---

# Tasks: 017 Joja / Agrochemical Tradeoff (MVP)

**Input**: Design documents from `/specs/017-joja-agrochemical-tradeoff/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`  
**Tests**: **Required (TDD, Principle IV)**. Write tests first, use property-based testing (FsCheck) where applicable, assert invariants, and complete the **RED phase** (all 017 tests written and failing for the right reasons) before any implementation task begins.  
**Organization**: Tasks are grouped by user story for traceability, with a global RED gate because core implementation touches shared files (`ModController.cs`, `AgrochemicalApplicationService.cs`, `ModEntry.cs`, `Constants.cs`, i18n). `[P]` means different files and no dependency on incomplete tasks.

## Execution Rules (must follow)

- **Scaffolds/templates before hand-written implementation**: create minimal compile-safe scaffolds (interfaces, empty class/test shells, constant/i18n key placeholders) before behavior implementation. Use LSP/code actions or template patterns where they help; do not implement behavior in scaffold tasks.
- **TDD / RED gate**: T001–T020 produce failing tests/scaffolds only. **T021 is a hard gate**: record RED evidence (targeted test run showing expected failures). Do **not** start T022+ until T021 is satisfied.
- **Parallel waves ([P], disjoint files)**: run parallel subagents only on disjoint files. Tasks that share context/files MUST reuse the same subagent/wave (especially `LivingRoots/Controllers/ModController.cs`, `LivingRoots/Services/AgrochemicalApplicationService.cs`, `LivingRoots/ModEntry.cs`, `LivingRoots/Constants.cs`).
- **Shared tooling constraint**: never run conflicting shared long-running tools from multiple agents at once; queue shared tooling. Never kill/interrupt/bypass a tool started by another agent/thread/process.
- **Atomic commit hook (deterministic)**: whenever a file edit/task completes, run `git add <file(s)> && git commit -m "<type>(<scope>): <summary> — <files> — <why>"`. Before committing, run `git diff --cached --stat` to ensure only intended files are staged.
- **Verification attestation**: only Phase 7 verification tasks produce success/completeness evidence. Agents must not self-attest completion.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: parallelizable (different files, no incomplete dependency)
- **[US#]**: user story label for story-mapped tasks
- Every task includes exact file path(s)

## Path Conventions

- Mod code: `LivingRoots/`
- Tests: `LivingRoots.Tests/`
- Feature docs: `specs/017-joja-agrochemical-tradeoff/`

---

## Phase 1: Setup & Scaffolds (Shared)

**Purpose**: Create compile-safe scaffolds and test shells before writing behavior. Scaffolds must not satisfy behavior tests.

- [ ] T001 Verify feature context and read `specs/017-joja-agrochemical-tradeoff/{spec.md,plan.md,research.md,data-model.md,quickstart.md,contracts/*.md}`; confirm target names/paths follow `LivingRoots/` DDD structure and `ModEntry.cs` composition-root rule.
- [ ] T002 [P] Scaffold domain contract `LivingRoots/Domain/Interfaces/IAgrochemicalApplicationService.cs` with minimal interface/result shape per `contracts/agrochemical-application-service.md` (no behavior); use file template/LSP-assisted creation if available.
- [ ] T003 [P] Scaffold service class `LivingRoots/Services/AgrochemicalApplicationService.cs` implementing `IAgrochemicalApplicationService` with minimal constructor/dependency stubs and `NotImplementedException`/safe placeholder behavior (no domain logic) per `contracts/agrochemical-application-service.md`.
- [ ] T004 [P] Scaffold unit test file `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` (xUnit class, Moq/FsCheck usings as needed, no passing behavior assertions yet).
- [ ] T005 [P] Scaffold controller test file `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` (xUnit class, Moq, existing controller test seams/patterns, no passing behavior assertions yet).
- [ ] T006 [P] Scaffold content/i18n parity test file `LivingRoots.Tests/AgrochemicalContentParityTests.cs` for 017 key/item framing checks (no passing assertions yet beyond compile-safe skeleton).
- [ ] T007 Add provisional 017 constant/key placeholders in `LivingRoots/Constants.cs` under `ModConstants` only where needed for scaffolds (stable names, explicit provisional/TODO values or safe defaults documented by spec; no gameplay behavior yet): e.g. agrochemical penalty `10f` (0–100, C3), item/translation key names, and message key constants referenced by contracts.

**Checkpoint**: Scaffolds compile conceptually; no 017 behavior implemented.

---

## Phase 2: Foundational RED — Write All Tests First (Blocking)

**Purpose**: Create comprehensive failing tests before implementation. Covers C1–C4, FR/SC invariants, refusal safety, MP authority, i18n parity, and property-based bounds.

**⚠️ CRITICAL**: Complete all test tasks T008–T020, then satisfy RED gate T021. No implementation tasks T022+ may start before T021.

### US1/US2/US4 domain invariants (service tests) — same file, same subagent/wave

- [ ] T008 [US1] Write failing service tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for successful host-eligible application: measured baseline produces `clamp(baseline - ModConstants.AgrochemicalSoilPenalty, MinSoilHealth, MaxSoilHealth)` and explicit absolute `SetSoilHealth` measured write (FR-004, FR-005, FR-006, SC-001, C3).
- [ ] T009 [US1] Write failing service tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for C2 unknown/`NaN` baseline: baseline resolves to `MaxSoilHealth` (100), `P=10` provisional => measured `SetSoilHealth(90)` unless clamped, no invisible no-op, no negative `UpdateHealth` from unknown (FR-006, SC-001, edge case).
- [ ] T010 [P] [US1] Add FsCheck/property-based tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for invariants: for any finite baseline in `[0,100]` and penalty in valid configured range, post-penalty is in `[0,100]`; C2 unknown baseline uses 100; measured explicit `0` remains distinct from Unknown semantics where applicable (data-model validation rules 1–4,8).
- [ ] T011 [US2] Write failing service/controller-contract tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` and/or `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` proving C1 no double-counting boundary for MVP path: no `HoeDirt.fertilizer` writes, no Harmony fertilizer hook dependency, no 014/015 quality/growth mechanical buff API usage; confirmation is proxy/attribution only (FR-003, FR-004, SC-006).

### US2/US3 controller authority, refusal ordering, and feedback — same file, same subagent/wave

- [ ] T012 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for C4/FR-008/FR-009: unsupported farmhand attempt is refused **before** soil mutation, item consumption, and false-success feedback; no authoritative/local soil mutation; no FR-009 misleading success/broadcast (contracts `multiplayer-gate3.md`, `agrochemical-application-service.md`).
- [ ] T013 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for host success path: per-tile lock acquired/released via `IHostAtomicLockService`/pattern, explicit measured soil commit succeeds, held item consumed **only after** authoritative measured commit, confirmation feedback emitted, `BroadcastSoilHealthSnapshotIfHost`/FR-009 snapshot occurs after commit (FR-009, contract ordering).
- [ ] T014 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for first-wins contention/lock failure/conflict: explicit refusal/failure outcome, no unsafe mutation, no item consumption, no false-success feedback, no success broadcast (FR-010, SC-002).
- [ ] T015 [US2] Write failing controller/service refusal tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for ineligible target, missing/invalid input, and boundary-honest outcomes (e.g., soil already at min/clamped result): refusal reason/message key class exposed, no mutation/consumption/false success; successful clamped outcomes remain honest/non-misleading (FR-005, FR-007, FR-008, edge cases).
- [ ] T016 [P] [US3] Add FsCheck/property-style refusal-invariant tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` where practical: for all refusal classes, postconditions hold (`soil unchanged`, `item unchanged/not consumed`, `no success feedback`, `no FR-009 broadcast`) given mocked initial state (SC-002).

### US2 i18n/content parity — disjoint file

- [ ] T017 [P] [US2] Write failing i18n/content parity tests in `LivingRoots.Tests/AgrochemicalContentParityTests.cs` asserting all new/touched 017 keys exist in `LivingRoots/i18n/default.json`, `LivingRoots/i18n/en.json`, and `LivingRoots/i18n/pt.json`; keys cover before-use framing, success confirmation/attribution (including C2 no-history implication intent), and refusal classes with minimum structure (cause, no-success, next-step implication where relevant) per FR-012 and `contracts/content-registration.md`.
- [ ] T018 [P] [US2] Add failing tests/checks in `LivingRoots.Tests/AgrochemicalContentParityTests.cs` for no-double-counting framing and tone constraints representable as key/contract assertions (no 014/015/vanilla-tier mechanical buff claim in success framing; cozy/non-punitive no-shaming requirement traceable) and for fallback/non-misleading expectation notes in contract-driven test names/comments as applicable (FR-002, FR-004, FR-007, FR-013).

### US4 verification/evidence conventions — docs-oriented checks (disjoint where possible)

- [ ] T019 [P] [US4] Create/update lightweight claim-record template/checklist artifact `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` (or document equivalent in `quickstart.md` if preferred by project convention) with fields: claim, evidence class, source artifact/path, status (`pending|complete|infeasible|blocked`), scope note; include 017 MVP planned claims (automated, host-only if feasible, D1 pending, human verified pending, SC-004 qualitative follow-up) per FR-015/SC-005/contracts.
- [ ] T020 [P] [US4] Add failing or pending-status verification scaffolding notes in `specs/017-joja-agrochemical-tradeoff/quickstart.md`/`verification/` for infeasible host-only e2e classification and provenance preservation (no retroactive promotion), ensuring no claim is pre-marked complete without evidence.

### RED gate (hard blocker)

- [ ] T021 **RED GATE (BLOCKING)**: Run targeted 017 tests and record evidence that newly added 017 tests fail for expected reasons (missing behavior, not compile breakage from unrelated code) before implementation. Suggested commands (adjust filters to actual names): `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~ModControllerAgrochemical"`; save summary/output reference in `specs/017-joja-agrochemical-tradeoff/verification/red-phase.md` (status=`red`, failing tests listed, date/command). **Do not begin T022+ until complete.**

**Checkpoint**: RED evidence recorded; implementation may begin in controlled waves.

---

## Phase 3: User Story 1 — Consequential Shortcut Choice (Priority: P1) 🎯 MVP

**Goal**: Host can apply one mod-intercepted Joja/agrochemical input to eligible soil, receive immediate confirmation/attribution, and record an immediate bounded soil penalty (C1/C2/C3) with no silent no-op and no double-counting.  
**Independent Test**: Host-eligible application on measured and unknown/`NaN` soil records expected measured bounded result; repeated application applies same bounded rule; save/load compatibility preserved by existing soil semantics (quickstart S1, S2).

### Implementation (shared context; prefer same subagent/wave for service + controller integration)

- [ ] T022 [US1] Implement domain/service C2/C3 mutation logic in `LivingRoots/Services/AgrochemicalApplicationService.cs`: resolve unknown/`NaN` baseline to `MaxSoilHealth` (100), compute `clamp(baseline - ModConstants.AgrochemicalSoilPenalty, Min, Max)` with provisional `P=10f` from `LivingRoots/Constants.cs`, and write explicit measured value via absolute `ISoilHealthService.SetSoilHealth`; never use negative `UpdateHealth` from unknown (FR-005, FR-006, contract).
- [ ] T023 [US1] Implement service success/failure result shape in `LivingRoots/Services/AgrochemicalApplicationService.cs` exposing success/refusal class, localized message key, and values useful for assertions (baseline/post-penalty) without performing controller-owned UI/broadcast/consumption side effects (contract Result shape).
- [ ] T024 [US1] Finalize 017 constants in `LivingRoots/Constants.cs` (`ModConstants`): agrochemical penalty (`10f`, 0–100, provisional/tunable), stable item ID/key names, translation key constants, and any bounded behavior constants required by implementation; keep centralized, no hardcoded magic values outside `ModConstants`.
- [ ] T025 [US1] Run targeted US1 tests for `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` and fix only minimum implementation in `LivingRoots/Services/AgrochemicalApplicationService.cs`/`LivingRoots/Constants.cs` to satisfy C1/C2/C3 service invariants; keep tests green (`GamePath=/home/luis/stardew-assemblies dotnet test ... --filter "FullyQualifiedName~AgrochemicalApplicationService"`).

**Checkpoint**: Service/domain soil penalty path satisfies US1 invariants under tests.

---

## Phase 4: User Story 2 — Understand the Tradeoff Before Committing (Priority: P1)

**Goal**: Player-facing before-use framing, immediate attributed after-use feedback, and explicit refusal messaging are clear, localized, cozy/non-punitive, honest at bounds, and understandable for C2 unknown-baseline path.  
**Independent Test**: i18n parity tests pass for touched 017 keys; controller/service tests assert confirmation/refusal message key classes and non-misleading behavior; no false success on refusal/fallback (quickstart S2, S5, S6).

### Implementation (i18n files parallelizable; controller wiring shares context)

- [ ] T026 [P] [US2] Add 017 before-use framing keys/values in `LivingRoots/i18n/default.json` (industrial shortcut + soil-health cost, cozy/non-punitive factual tone) per `contracts/content-registration.md` and FR-002.
- [ ] T027 [P] [US2] Add equivalent-meaning/tone 017 keys in `LivingRoots/i18n/en.json` (parity with `default.json` touched keys) per FR-012.
- [ ] T028 [P] [US2] Add equivalent-meaning/tone 017 keys in `LivingRoots/i18n/pt.json` (parity with `default.json`/`en.json` touched keys, including refusal/attribution/next-step implication where relevant) per FR-012.
- [ ] T029 [US2] Add success confirmation/attribution and refusal message keys for all refusal classes in `LivingRoots/i18n/default.json`, `LivingRoots/i18n/en.json`, and `LivingRoots/i18n/pt.json` (same subagent/wave as T026–T028 or sequential pass): include C2 no prior-history implication, boundary-honest wording where applicable, refusal minimum structure (cause, no-success/non-application, next-step implication where relevant).
- [ ] T030 [US2] Implement controller feedback/refusal presentation mapping in `LivingRoots/Controllers/ModController.cs` for agrochemical path: display localized explicit refusal/confirmation via existing HUD/message patterns; preserve non-misleading fallback (no false success if normal feedback path unavailable) (FR-007, FR-008, FR-013).
- [ ] T031 [US2] Run targeted US2 tests for `LivingRoots.Tests/AgrochemicalContentParityTests.cs` and relevant `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` cases; fix only `LivingRoots/i18n/*.json`, `LivingRoots/Constants.cs` key names, and `LivingRoots/Controllers/ModController.cs` message mapping to satisfy FR-002/007/008/012.

**Checkpoint**: Telegraphed tradeoff, attribution, and refusal text/feedback paths are test-covered and localized with parity.

---

## Phase 5: User Story 3 — Preserve Trustworthy Shared-Farm Behavior (Priority: P1)

**Goal**: Host-authoritative MVP mutation with per-tile lock/first-wins, C4 farmhand refusal before mutation/consumption/false-success, and FR-009 snapshot broadcast after successful host commit only.  
**Independent Test**: Controller tests prove host lock/commit/consume-after-commit/broadcast ordering, conflict refusal, and farmhand refusal ordering/no broadcast (quickstart S3, S4).

### Implementation (ModController/ModEntry shared; same subagent/wave)

- [ ] T032 [US3] Implement mod-intercepted agrochemical input handling in `LivingRoots/Controllers/ModController.cs` (right-click/input pattern consistent with compost/mulch): resolve held item/context through existing seams, determine eligible target, and route to agrochemical application path without Harmony fertilizer hooks or `HoeDirt.fertilizer` writes (FR-003, contract).
- [ ] T033 [US3] Implement C4 authority gate in `LivingRoots/Controllers/ModController.cs`: detect unsupported farmhand/non-host MVP path and refuse with localized message **before** service call, soil mutation, item consumption, lock-acquired mutation path, or false-success feedback (FR-008, FR-009, C4).
- [ ] T034 [US3] Implement host authoritative orchestration in `LivingRoots/Controllers/ModController.cs`: acquire per-tile lock (first-wins, fail closed when unsafe), call `IAgrochemicalApplicationService` for measured soil commit, consume held item **only after** authoritative measured commit, emit confirmation, then `BroadcastSoilHealthSnapshotIfHost`/FR-009-compatible snapshot after commit; on conflict/failure/refusal, no mutation/consumption/false-success/broadcast (FR-009, FR-010, contract ordering).
- [ ] T035 [US3] Register/wire `IAgrochemicalApplicationService` and `AgrochemicalApplicationService` in `LivingRoots/ModEntry.cs` composition root only, passing required dependencies consistent with existing patterns; do not create services elsewhere (Principle I, plan structure).
- [ ] T036 [US3] Run targeted US3 tests for `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` and any integration-style controller tests; fix only `LivingRoots/Controllers/ModController.cs`, `LivingRoots/ModEntry.cs`, and minimal required constants/interfaces to satisfy MP authority/refusal/lock/broadcast invariants (`GamePath=/home/luis/stardew-assemblies dotnet test ... --filter "FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~Agrochemical"`).

**Checkpoint**: US1+US2 behavior is host-authoritative and MP-safe within MVP boundaries.

---

## Phase 6: User Story 4 — Verify the MVP Honestly (Priority: P2)

**Goal**: Verification artifacts and claim records make evidence class/provenance explicit; host-only machine evidence is never promoted to human `verified`/D1.  
**Independent Test**: Claim-record artifact/template exists and is consistent with FR-015/SC-005; quickstart/verification notes classify feasible vs infeasible host-only e2e and pending D1/human/SC-004 follow-ups.

### Implementation (docs/evidence conventions; disjoint from runtime code)

- [ ] T037 [P] [US4] Finalize `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` (or project-conventional equivalent) with 017 claim rows/placeholders: automated test evidence, host-only machine-pass (if feasible), D1 pending, human verified pending, SC-004 qualitative follow-up pending/complete; each row requires claim, evidence class, source artifact/path, status, scope note.
- [ ] T038 [P] [US4] Update `specs/017-joja-agrochemical-tradeoff/quickstart.md` verification notes if implementation details changed (actual test filters/filenames, claim-record path, infeasible-e2e recording), preserving honesty rules and provenance/no-retroactive-promotion.
- [ ] T039 [US4] If a host-only Gate 3 scenario is added/feasible during implementation, document it under `specs/017-joja-agrochemical-tradeoff/verification/` with required signals and classify as host-only `machine-pass` only; if infeasible, record infeasibility reason and claim only completed lower-level evidence (do not fabricate). Do not claim human `verified` or D1.

**Checkpoint**: 017 verification claims are auditable and honestly bounded.

---

## Phase 7: Verification & Attestation (Final — only these tasks attest)

**Purpose**: Produce objective evidence. No agent self-attestation.

- [ ] T040 Run targeted 017 automated tests and record pass/fail evidence in `specs/017-joja-agrochemical-tradeoff/verification/automated-tests.md`: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~ModControllerAgrochemical"` (adjust filters to actual names). Include command, date, result summary, and source output/log reference.
- [ ] T041 Run full regression suite and record evidence in `specs/017-joja-agrochemical-tradeoff/verification/automated-tests.md`: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release`. Report exact counts; if failures occur outside 017 scope, classify separately (do not claim 017 complete).
- [ ] T042 Run formatting gate and record evidence in `specs/017-joja-agrochemical-tradeoff/verification/static-checks.md`: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` (and build if needed by project workflow). Record pass/fail and command.
- [ ] T043 [P] Complete/update `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` with actual evidence classes/source paths/statuses for executed checks (automated complete/pending as applicable). Mark host-only e2e infeasible/blocked/pending or machine-pass **only** with real evidence; keep D1 and human `verified` pending unless separately attested.
- [ ] T044 [P] Perform Gate 3 honesty/provenance review recorded in `specs/017-joja-agrochemical-tradeoff/verification/honesty-review.md`: confirm no claim of `verified` without human attestation, no D1 claim without real farmhand evidence, no Harmony fertilizer-hook/vanilla-fertilizer-slot MVP claim, no final balance claim, no double-counting claim, and no rewrite/retroactive promotion of prior verification artifacts.
- [ ] T045 Produce final implementation status summary in `specs/017-joja-agrochemical-tradeoff/verification/summary.md` based **only** on recorded artifacts from T040–T044 (pass/fail/pending/infeasible/blocked per claim). This summary is evidence-derived; agent narrative alone is not attestation.

**Checkpoint**: Completion can be reported only from Phase 7 evidence artifacts.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup & Scaffolds)**: starts immediately.
- **Phase 2 (Foundational RED)**: depends on Phase 1 scaffolds/paths; **blocks all implementation**.
- **T021 RED gate**: depends on T008–T020; blocks T022+.
- **Phase 3 (US1)**: starts after T021; core soil-penalty/service invariants.
- **Phase 4 (US2)**: starts after T021; i18n tasks `[P]` can begin early, but controller feedback mapping integrates with US1/US3 behavior and should follow/coordinate with `ModController` wave.
- **Phase 5 (US3)**: starts after T021; depends on US1 service contract/result shape and coordinates with US2 feedback keys. Shares `ModController.cs`/`ModEntry.cs` with US2/US3 implementation — use same subagent/wave.
- **Phase 6 (US4)**: docs/evidence tasks `[P]` may proceed after Phase 2; finalization depends on implementation/verification reality.
- **Phase 7 (Verification)**: depends on all desired implementation tasks complete.

### User Story Dependencies

- **US1 (P1)**: foundational domain outcome; no other story required beyond RED gate.
- **US2 (P1)**: independently testable for text/parity/refusal messaging, but runtime confirmation/refusal display integrates with US1/US3 controller path.
- **US3 (P1)**: independently testable for authority/refusal/lock/broadcast via controller tests; runtime depends on US1 service/result and US2 message keys.
- **US4 (P2)**: independently testable as evidence/claim-honesty documentation; final status depends on Phase 7 artifacts.

### Parallel Opportunities / Subagent Waves

| Wave | Subagent (reuse same for shared context) | Parallelizable tasks | Notes |
|---|---|---|---|
| A | Scaffold agent | T002, T003, T004, T005, T006 | Disjoint new files. T007 touches `Constants.cs` alone after or serialized. |
| B | Service-test agent | T008–T011 (same file mostly), T010 marked `[P]` only if split into separate file; otherwise sequential in `AgrochemicalApplicationServiceTests.cs` | Keep same subagent for that test file. |
| C | Controller-test agent | T012–T016 in `ModControllerAgrochemicalTests.cs` | Same subagent/file context. |
| D | i18n/content-test agent | T017, T018, T019, T020 | T017/T018 same file; T019/T020 disjoint docs. |
| E | Service implementation agent | T022–T025, T035 coordination input | `AgrochemicalApplicationService.cs`/`Constants.cs` shared context. |
| F | i18n agent | T026, T027, T028 | Disjoint locale files, parallel. T029 may follow as consolidated pass. |
| G | Controller/composition agent (single) | T030, T032, T033, T034, T035, T036 | `ModController.cs` + `ModEntry.cs` shared; do not parallelize. |
| H | Verification agent(s) | T037, T038 parallel docs; T040–T042 serialized/queued shared tooling (`dotnet`); T043, T044 parallel docs; T045 last | Queue shared `dotnet test`/format/build invocations. |

## Parallel Example: RED test wave

```text
Wave D (disjoint): T019 claim-record template in specs/017-joja-agrochemical-tradeoff/verification/claim-records.md
Wave D (disjoint): T020 verification scaffolding notes in specs/017-joja-agrochemical-tradeoff/quickstart.md or specs/017-joja-agrochemical-tradeoff/verification/red-phase.md
Wave F-prep (disjoint locale files after keys known): T026 default.json, T027 en.json, T028 pt.json — only when test/key names are stable
```

## Parallel Example: User Story 2

```text
[P] T026 LivingRoots/i18n/default.json
[P] T027 LivingRoots/i18n/en.json
[P] T028 LivingRoots/i18n/pt.json
Then T029/T030/T031 in controller+i18n context (same wave/subagent for shared files)
```

## Implementation Strategy

### MVP First (US1 + minimal US2/US3 safety)

1. Complete Phase 1 scaffolds.
2. Complete Phase 2 RED tests and **T021 gate**.
3. Deliver US1 service/domain penalty path (T022–T025).
4. Add minimum US2/US3 controller+i18n path required for safe host application/refusal (T026–T036), keeping C4 and no-false-success invariants.
5. **STOP and VALIDATE** using Phase 7 T040–T045 evidence artifacts before claiming MVP.

### Incremental Delivery

1. Scaffolds + RED gate → safe TDD baseline.
2. US1 soil mutation invariants (C2/C3) → domain MVP core.
3. US2 telegraphing/refusal localization → legible/cozy trust.
4. US3 host authority/lock/FR-009/farmhand refusal → MP-safe MVP.
5. US4/Phase 7 evidence records → honest attestation.

## Notes

- `[P]` = disjoint files only; shared files (`ModController.cs`, `ModEntry.cs`, `Constants.cs`, same test file) require sequencing/same subagent.
- Tests use xUnit + Moq + FsCheck patterns already present in `LivingRoots.Tests/`; keep one subject focus per new test file.
- C1 boundary: tests/implementation must not add 014/015 quality/growth mechanical buffs, `HoeDirt.fertilizer` writes, or Harmony fertilizer hooks.
- C2 boundary: never `UpdateHealth(-P)` from unknown/unmeasured soil; use absolute measured `SetSoilHealth`.
- C4 boundary: refuse unsupported farmhand paths before mutation/consumption/false-success feedback.
- Gate 3 honesty: host-only `machine-pass` ≠ human `verified` ≠ D1; infeasible runs recorded, never fabricated; no retroactive promotion of prior evidence.
- Commit after each completed task/file edit with atomic conventional commit and staged-file check.
