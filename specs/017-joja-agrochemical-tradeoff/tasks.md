---
description: "Task list for 017 Joja / Agrochemical Tradeoff (MVP)"
---

# Tasks: 017 Joja / Agrochemical Tradeoff (MVP)

**Input**: Design documents from `/specs/017-joja-agrochemical-tradeoff/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`  
**Tests**: **Required (TDD, Principle IV)**. Write tests first, use property-based testing (FsCheck) where applicable, assert invariants, and complete the **RED phase** (all 017 tests written and failing for the right reasons) before any implementation task begins.  
**Organization**: Tasks are grouped by user story for traceability, with a global RED gate because core implementation touches shared files (`ModController.cs`, `AgrochemicalApplicationService.cs`, `ModEntry.cs`, `Constants.cs`, i18n). `[P]` means different files and no dependency on incomplete tasks.  
**Task count**: 55 tasks (T001–T055).

## Execution Rules (must follow)

- **Scaffolds/templates before hand-written implementation**: create minimal compile-safe scaffolds (interfaces, empty class/test shells, constant/i18n key placeholders) before behavior implementation. Use LSP/code actions or template patterns where they help; do not implement behavior in scaffold tasks.
- **TDD / RED gate**: T001–T024 are limited to context/scaffolds, failing tests, and pending-status verification notes. **T025 is a hard gate**: record RED evidence (targeted test run showing expected failures). Do **not** start T026+ until T025 is satisfied.
- **Parallel waves ([P], disjoint files)**: run parallel subagents only on disjoint files. Tasks that share context/files MUST reuse the same subagent/wave (especially `LivingRoots/Controllers/ModController.cs`, `LivingRoots/Services/AgrochemicalApplicationService.cs`, `LivingRoots/ModEntry.cs`, `LivingRoots/Constants.cs`).
- **Single `ModController.cs` writer sequence**: T036–T042 are strictly serialized in that order. Do not mark any of them `[P]` or split them across concurrent subagents. Routing/interception and C4/host orchestration (T036–T038) MUST precede feedback mapping (T039); targeted fixes (T041–T042) come last.
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
- Persistence checks reuse existing save/load seams (`ISoilHealthService.SaveData`/`LoadData` + `IModDataService`); the mock-capture pattern is demonstrated by `LivingRoots.Tests/SaveReloadConsistencyTests.cs`. Do not add a second persistence layer.

---

## Phase 1: Setup & Scaffolds (Shared)

**Purpose**: Create compile-safe scaffolds and test shells before writing behavior. Scaffolds must not satisfy behavior tests.

- [x] T001 Verify feature context and read `specs/017-joja-agrochemical-tradeoff/{spec.md,plan.md,research.md,data-model.md,quickstart.md,contracts/*.md}`; confirm target names/paths follow `LivingRoots/` DDD structure and `ModEntry.cs` composition-root rule.
- [x] T002 [P] Scaffold domain contract `LivingRoots/Domain/Interfaces/IAgrochemicalApplicationService.cs` with minimal interface/result shape per `contracts/agrochemical-application-service.md` (no behavior); use file template/LSP-assisted creation if available.
- [x] T003 [P] Scaffold service class `LivingRoots/Services/AgrochemicalApplicationService.cs` implementing `IAgrochemicalApplicationService` with minimal constructor/dependency stubs and `NotImplementedException`/safe placeholder behavior (no domain logic) per `contracts/agrochemical-application-service.md`.
- [x] T004 [P] Scaffold unit test file `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` (xUnit class, Moq/FsCheck usings as needed, no passing behavior assertions yet).
- [x] T005 [P] Scaffold controller test file `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` (xUnit class, Moq, existing controller test seams/patterns, no passing behavior assertions yet).
- [x] T006 [P] Scaffold content/i18n parity test file `LivingRoots.Tests/AgrochemicalContentParityTests.cs` for 017 key/item framing checks (no passing assertions yet beyond compile-safe skeleton).
- [x] T007 Add provisional 017 constant/key placeholders in `LivingRoots/Constants.cs` under `ModConstants` only where needed for scaffolds (stable names, explicit provisional/TODO values or safe defaults documented by spec; no gameplay behavior yet): e.g. agrochemical penalty `10f` (0–100, C3), item/translation key names, and message key constants referenced by contracts.

**Checkpoint**: Scaffolds compile conceptually; no 017 behavior implemented.

---

## Phase 2: Foundational RED — Write All Tests First (Blocking)

**Purpose**: Create comprehensive failing tests before implementation. Covers C1–C4, FR/SC invariants, persistence, refusal safety, MP authority, i18n parity, fallback behavior, boundary honesty, and property-based bounds.

**⚠️ CRITICAL**: Complete all RED-phase tasks T008–T024, then satisfy RED gate T025. No implementation tasks T026+ may start before T025.

### US1/US2/US4 domain invariants (service tests) — same file, same subagent/wave

- [x] T008 [US1] Write failing service tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for successful host-eligible application: measured baseline produces `clamp(baseline - ModConstants.AgrochemicalSoilPenalty, MinSoilHealth, MaxSoilHealth)` and explicit absolute `SetSoilHealth` measured write (FR-004, FR-005, FR-006, SC-001, C3).
- [x] T009 [US1] Write failing service tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for C2 unknown/`NaN` baseline: baseline resolves to `MaxSoilHealth` (100), `P=10` provisional => measured `SetSoilHealth(90)` unless clamped, no invisible no-op, no negative `UpdateHealth` from unknown (FR-006, SC-001, edge case).
- [x] T010 [US1] Add FsCheck/property-based tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for invariants: for any finite baseline in `[0,100]` and penalty in valid configured range, post-penalty is in `[0,100]`; C2 unknown baseline uses 100; measured explicit `0` remains distinct from Unknown semantics where applicable (data-model validation rules 1–4,8). Same file as T008–T009/T013; do not parallelize (T011 uses a separate persistence test file but remains in the same service-test wave).
- [x] T011 [US1] Write failing persistence round-trip tests in `LivingRoots.Tests/Services/AgrochemicalPersistenceTests.cs` using existing persistence seams only: `ISoilHealthService.SaveData`/`LoadData` with a mocked `IModDataService` capture (pattern: `LivingRoots.Tests/SaveReloadConsistencyTests.cs`). Apply via the 017 service to a C2 unknown/`NaN` tile (`P=10`, expected measured `90`), save, reload through a fresh service/load, and assert the C2 post-penalty result persists as measured (not Unknown), remains bounded, and sparse semantics hold; include one measured-baseline case. If a unit-level round trip cannot faithfully represent the existing save/load seam, record why and downgrade to a bounded integration-style test plus explicit Phase 7 evidence task T049; do not overclaim real disk persistence.
- [x] T012 [US1] Write failing service/contract tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` proving C1 no double-counting boundary for MVP path: no 014/015 quality/growth mechanical buff API usage and no dependency on vanilla fertilizer-tier or Harmony fertilizer hooks; confirmation is proxy/attribution only (FR-003, FR-004, SC-006). Controller-side no-write/no-hook assertions are covered in T015 where applicable; do not edit the controller test file from this task.
- [x] T013 [US1] Write failing boundary-honest service tests in `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` for min-health/clamp: soil `0` and `5` with `P=10`; assert `baseline_used`, `post_penalty`, `requested_penalty`, and `applied_penalty` (or equivalent explicit `was_clamped` + applied delta) are exposed; post-penalty remains bounded (`0`), and no result/message-selection data implies the full nominal `10`-point penalty was applied when clamped (FR-005, FR-007, SC-003).

### US2/US3 controller authority, refusal ordering, and feedback — same file, same subagent/wave

- [x] T014 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for C4/FR-008/FR-009: unsupported farmhand attempt is refused **before** soil mutation, item consumption, and false-success feedback; no authoritative/local soil mutation; no FR-009 misleading success/broadcast (contracts `multiplayer-gate3.md`, `agrochemical-application-service.md`).
- [x] T015 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for host success path: per-tile lock acquired/released via `IHostAtomicLockService`/pattern, explicit measured soil commit succeeds, held item consumed **only after** authoritative measured commit, confirmation feedback emitted, `BroadcastSoilHealthSnapshotIfHost`/FR-009 snapshot occurs after commit, and no `HoeDirt.fertilizer` write/Harmony fertilizer-hook dependency is introduced (FR-009, FR-004, SC-006, contract ordering).
- [x] T016 [US3] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for first-wins contention/lock failure/conflict: explicit refusal/failure outcome, no unsafe mutation, no item consumption, no false-success feedback, no success broadcast (FR-010, SC-002).
- [x] T017 [US2] Write failing controller/service refusal tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for ineligible target, missing/invalid input, and boundary-honest outcomes (e.g., soil already at min/clamped result): refusal reason/message key class exposed, no mutation/consumption/false success; successful clamped outcomes remain honest/non-misleading (FR-005, FR-007, FR-008, edge cases).
- [x] T018 [US2] Write failing boundary-honest controller/feedback tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for soil `0`/`5` with `P=10`: confirmation/attribution derives from the service result (`baseline_used`, `post_penalty`, `requested_penalty`, `applied_penalty`/`was_clamped`) and does not claim a full `10`-point drop; if the outcome is refusal at minimum health, assert no mutation/consumption/false success; if success, assert the explicit clamped outcome is non-misleading (FR-005, FR-007).
- [x] T019 [US2] Write failing controller tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` for the unavailable-feedback/fallback branch: simulate the normal HUD/message presentation path being unavailable or throwing; assert the outcome stays non-misleading (no false success message/claim), no unsafe side effects (no duplicate soil mutation, extra item consumption, erroneous FR-009 broadcast, or escaped exception), and no claim promotion (fallback alone cannot satisfy FR-007/FR-013/SC-002). Cover both post-commit feedback failure and pre-mutation refusal/feedback failure where representable (FR-007, FR-013, SC-002).
- [x] T020 [US3] Add FsCheck/property-style refusal-invariant tests in `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` where practical: for all refusal classes, postconditions hold (`soil unchanged`, `item unchanged/not consumed`, `no success feedback`, `no FR-009 broadcast`) given mocked initial state (SC-002). Same file as T014–T019; do not parallelize.

### US2 i18n/content parity — same file, same subagent/wave

- [x] T021 [US2] Write failing i18n/content parity tests in `LivingRoots.Tests/AgrochemicalContentParityTests.cs` asserting all new/touched 017 keys exist in `LivingRoots/i18n/default.json`, `LivingRoots/i18n/en.json`, and `LivingRoots/i18n/pt.json`; keys cover before-use framing, success confirmation/attribution (including C2 no-history implication intent), and refusal classes with minimum structure (cause, no-success, next-step implication where relevant) per FR-012 and `contracts/content-registration.md`. Do not parallelize with T022 (same file).
- [x] T022 [US2] Add failing tests/checks in `LivingRoots.Tests/AgrochemicalContentParityTests.cs` for no-double-counting framing and tone constraints representable as key/contract assertions (no 014/015/vanilla-tier mechanical buff claim in success framing; cozy/non-punitive no-shaming requirement traceable) and for fallback/non-misleading expectation notes in contract-driven test names/comments as applicable (FR-002, FR-004, FR-007, FR-013). Same file as T021; do not parallelize.

### US4 verification/evidence conventions — docs-oriented checks (disjoint where possible)

- [x] T023 [P] [US4] Create/update lightweight claim-record template/checklist artifact `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` with fields: claim, evidence class, source artifact/path, status (`pending|complete|infeasible|blocked`), scope note; include 017 MVP planned claims (automated, persistence round-trip, host-only if feasible, D1 pending, human verified pending, SC-004 qualitative follow-up) per FR-015/SC-005/contracts. Do not also rewrite `quickstart.md` here.
- [x] T024 [P] [US4] Add failing or pending-status verification scaffolding notes in `specs/017-joja-agrochemical-tradeoff/quickstart.md` or `specs/017-joja-agrochemical-tradeoff/verification/red-phase.md` for infeasible host-only e2e classification and provenance preservation (no retroactive promotion), ensuring no claim is pre-marked complete without evidence. Do not edit `claim-records.md` here.

### RED gate (hard blocker)

- [x] T025 **RED GATE (BLOCKING)**: Run targeted 017 tests and record RED evidence in `specs/017-joja-agrochemical-tradeoff/verification/red-phase.md` (status=`red`). Required evidence fields: exact command; date; filter used; total/passed/failed/skipped counts; failing test names; failure-reason category for each failure (`expected missing behavior`, `NotImplementedException`, or expected assertion failure vs unrelated compile/build breakage); raw output/log path. Suggested command (adjust filter to actual names): `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~AgrochemicalPersistence|FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~AgrochemicalContentParity"`. If failures are compile breakage or unrelated to 017 behavior, fix scaffolding and rerun; do not count those as RED. **Do not begin T026+ until complete.**

**Checkpoint**: RED evidence recorded; implementation may begin in controlled waves.

---

## Phase 3: User Story 1 — Consequential Shortcut Choice (Priority: P1) 🎯 MVP

**Goal**: Host can apply one mod-intercepted Joja/agrochemical input to eligible soil, receive immediate confirmation/attribution, and record an immediate bounded soil penalty (C1/C2/C3) with no silent no-op, no double-counting, and valid save/load persistence.  
**Independent Test**: Host-eligible application on measured and unknown/`NaN` soil records expected measured bounded result; repeated application applies same bounded rule; C2 `90` result survives the existing save/load seam (quickstart S1, S2).

### Implementation (shared context; prefer same subagent/wave for service + controller integration)

- [x] T026 [US1] Implement domain/service C2/C3 mutation logic in `LivingRoots/Services/AgrochemicalApplicationService.cs`: resolve unknown/`NaN` baseline to `MaxSoilHealth` (100), compute `clamp(baseline - ModConstants.AgrochemicalSoilPenalty, Min, Max)` with provisional `P=10f` from `LivingRoots/Constants.cs`, and write explicit measured value via absolute `ISoilHealthService.SetSoilHealth`; never use negative `UpdateHealth` from unknown (FR-005, FR-006, contract).
- [x] T027 [US1] Implement service success/failure result shape in `LivingRoots/Services/AgrochemicalApplicationService.cs` exposing success/refusal class, localized message key, `baseline_used`, `post_penalty`, and `requested_penalty`/`applied_penalty` or an explicit `was_clamped` + applied-delta equivalent, without performing controller-owned UI/broadcast/consumption side effects (contract Result shape; FR-005/FR-007).
- [x] T028 [US1] Finalize 017 constants in `LivingRoots/Constants.cs` (`ModConstants`): agrochemical penalty (`10f`, 0–100, provisional/tunable), stable item ID/key names, translation key constants, and any bounded behavior constants required by implementation; keep centralized, no hardcoded magic values outside `ModConstants`.
- [x] T029 [US1] Make the T011 persistence round-trip tests pass using existing `ISoilHealthService.SaveData`/`LoadData` and `IModDataService` seams only; verify the C2 unknown-baseline application records measured `90` for `P=10` and survives save/load as measured within bounds, with sparse semantics preserved. Do not add a second persistence layer or rewrite historical soil persistence behavior. If unit-level round-trip is infeasible, implement/record a bounded integration-style test and retain explicit Phase 7 evidence task T049; do not claim real disk save/load.
- [x] T030 [US1] Run targeted US1 tests for `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs` and `LivingRoots.Tests/Services/AgrochemicalPersistenceTests.cs`; fix only minimum implementation in `LivingRoots/Services/AgrochemicalApplicationService.cs`/`LivingRoots/Constants.cs` to satisfy C1/C2/C3 and persistence invariants; keep tests green (`GamePath=/home/luis/stardew-assemblies dotnet test ... --filter "FullyQualifiedName~AgrochemicalApplicationService|FullyQualifiedName~AgrochemicalPersistence"`).

**Checkpoint**: Service/domain soil penalty path satisfies US1 invariants under tests, including the existing save/load seam.

---

## Phase 4: User Story 2 — Understand the Tradeoff Before Committing (Priority: P1)

**Goal**: Player-facing before-use framing, attributed after-use feedback, and explicit refusal messaging are clear, localized, cozy/non-punitive, honest at bounds, and understandable for the C2 unknown-baseline path.  
**Independent Test**: i18n parity tests pass for touched 017 keys; service/controller result tests assert confirmation/refusal message key classes and non-misleading behavior; controller display/fallback is completed in the Phase 5 `ModController` sequence (quickstart S2, S5, S6).

### Implementation (i18n files parallelizable; controller wiring shares context)

- [x] T031 [P] [US2] Add 017 before-use framing keys/values in `LivingRoots/i18n/default.json` (industrial shortcut + soil-health cost, cozy/non-punitive factual tone) per `contracts/content-registration.md` and FR-002.
- [x] T032 [P] [US2] Add equivalent-meaning/tone 017 keys in `LivingRoots/i18n/en.json` (parity with `default.json` touched keys) per FR-012.
- [x] T033 [P] [US2] Add equivalent-meaning/tone 017 keys in `LivingRoots/i18n/pt.json` (parity with `default.json`/`en.json` touched keys, including refusal/attribution/next-step implication where relevant) per FR-012.
- [x] T034 [US2] Serialized consolidation pass after T031–T033: add success confirmation/attribution and refusal message keys for all refusal classes in `LivingRoots/i18n/default.json`, `LivingRoots/i18n/en.json`, and `LivingRoots/i18n/pt.json`; include C2 no prior-history implication and boundary-honest clamp wording where applicable. Waits for T031–T033; do not parallelize.
- [x] T035 [US2] Run targeted US2 i18n/content parity tests for `LivingRoots.Tests/AgrochemicalContentParityTests.cs` and fix only `LivingRoots/i18n/*.json` and `LivingRoots/Constants.cs` key names to satisfy FR-002/FR-012. Do not edit `LivingRoots/Controllers/ModController.cs` here; controller feedback fixes are T041/T042 after the controller path exists.

**Checkpoint**: Telegraphed tradeoff, attribution, and refusal key/text parity are test-covered and localized; runtime controller display is sequenced in Phase 5.

---

## Phase 5: User Story 3 — Preserve Trustworthy Shared-Farm Behavior (Priority: P1)

**Goal**: Host-authoritative MVP mutation with per-tile lock/first-wins, C4 farmhand refusal before mutation/consumption/false-success, FR-009 snapshot broadcast after successful host commit only, and non-misleading feedback/fallback.  
**Independent Test**: Controller tests prove host lock/commit/consume-after-commit/broadcast ordering, conflict refusal, farmhand refusal ordering/no broadcast, boundary-honest feedback, and fallback behavior (quickstart S3, S4, S5).

### Serialized `ModController.cs` sequence (single writer; no `[P]`)

**Preconditions**: T026–T028 stabilize the service contract/constants; T031–T035 stabilize i18n keys. Execute T036–T042 in this exact order in one subagent/wave: minimal route/interception → C4 authority gate → host orchestration/FR-009 ordering → feedback mapping/fallback → composition wiring → targeted feedback fixes → targeted US3 fixes. Do not start T039 feedback mapping before T036–T038 exist.

- [x] T036 [US3] Implement minimal mod-intercepted agrochemical route/input handling in `LivingRoots/Controllers/ModController.cs` (right-click/input pattern consistent with compost/mulch): use constants/keys stabilized in T028 and T031–T035, resolve held item/context through existing seams, determine eligible target, and route to the agrochemical application path without Harmony fertilizer hooks or `HoeDirt.fertilizer` writes (FR-003, contract). Do not add new i18n keys here.
- [x] T037 [US3] Implement C4 authority gate in `LivingRoots/Controllers/ModController.cs` after T036: detect unsupported farmhand/non-host MVP path and refuse with localized message **before** service call, soil mutation, item consumption, lock-acquired mutation path, or false-success feedback (FR-008, FR-009, C4).
- [x] T038 [US3] Implement host authoritative orchestration in `LivingRoots/Controllers/ModController.cs` after T037: acquire per-tile lock (first-wins, fail closed when unsafe), call `IAgrochemicalApplicationService` for measured soil commit, consume held item **only after** authoritative measured commit, invoke the feedback seam (detailed mapping/fallback completed in T039), then `BroadcastSoilHealthSnapshotIfHost`/FR-009-compatible snapshot after commit; on conflict/failure/refusal, no mutation/consumption/false-success/broadcast (FR-009, FR-010, contract ordering).
- [x] T039 [US2] After T036–T038, implement controller feedback/refusal presentation mapping in `LivingRoots/Controllers/ModController.cs`: display localized explicit refusal/confirmation via existing HUD/message patterns; derive bounded-outcome text from result data (`baseline_used`, `post_penalty`, clamp/applied delta) so a clamped outcome is not presented as a full nominal penalty; preserve a non-misleading fallback when the normal feedback path is unavailable (no false success), satisfying T018–T019 (FR-007, FR-008, FR-013, SC-002).
- [x] T040 [US3] Register/wire `IAgrochemicalApplicationService` and `AgrochemicalApplicationService` in `LivingRoots/ModEntry.cs` composition root only, passing required dependencies consistent with existing patterns; do not create services elsewhere (Principle I, plan structure). Depends on the T026–T027 service contract.
- [x] T041 [US2] Run targeted controller feedback/i18n tests for `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` and relevant `LivingRoots.Tests/AgrochemicalContentParityTests.cs` cases; fix only `LivingRoots/Controllers/ModController.cs`, `LivingRoots/i18n/*.json`, and `LivingRoots/Constants.cs` key names to satisfy FR-002/007/008/012/013. Depends on T039–T040; serialized after the controller path.
- [x] T042 [US3] Run targeted US3 tests for `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs` and any integration-style controller tests; fix only `LivingRoots/Controllers/ModController.cs`, `LivingRoots/ModEntry.cs`, and minimal required constants/interfaces to satisfy MP authority/refusal/lock/broadcast/boundary invariants (`GamePath=/home/luis/stardew-assemblies dotnet test ... --filter "FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~Agrochemical"`). Final controller pass after T041.

**Checkpoint**: US1+US2 behavior is host-authoritative and MP-safe within MVP boundaries, including honest boundary/fallback outcomes.

---

## Phase 6: User Story 4 — Verify the MVP Honestly (Priority: P2)

**Goal**: Verification artifacts and claim records make evidence class/provenance explicit; host-only machine evidence is never promoted to human `verified`/D1; SC-004 remains pending unless a real protocol run exists.  
**Independent Test**: Claim-record artifact/template exists and is consistent with FR-015/SC-005; quickstart/verification notes classify feasible vs infeasible host-only e2e, document MVP-safe provisioning, and keep D1/human/SC-004 follow-ups pending.

### Implementation (docs/evidence conventions; disjoint from runtime code)

- [x] T043 [P] [US4] Finalize `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` (or project-conventional equivalent) with 017 claim rows/placeholders: automated test evidence, persistence round-trip evidence, host-only machine-pass (if feasible), D1 pending, human verified pending, SC-004 qualitative follow-up pending/complete; each row requires claim, evidence class, source artifact/path, status, scope note.
- [x] T044 [P] [US4] Update `specs/017-joja-agrochemical-tradeoff/quickstart.md` verification notes if implementation details changed (actual test filters/filenames, claim-record path, persistence evidence scope, infeasible-e2e recording), preserving honesty rules and provenance/no-retroactive-promotion.
- [x] T045 [P] [US4] Finalize/validate the existing `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md` (create only if missing; do not rewrite historical run evidence) so it contains the exact minimal protocol: participant/context scope; first-use timing (`before` or `immediately after first supported use`); a prompt asking participants to identify both the short-term shortcut and the soil-health cost; binary `pass|fail` scoring requiring both; an evidence note with sample size/denominator, pass count, timing, and scope note; target `>=90%` pass. Keep execution status `pending` unless a real recorded run exists; this task documents/finalizes the protocol only and must not claim SC-004 complete.
- [x] T046 [P] [US4] Record an MVP-safe provisioning/validation path for obtaining the agrochemical input in S1–S5/manual/optional Gate 3 validation in `specs/017-joja-agrochemical-tradeoff/verification/provisioning-validation.md` (new file, so it is disjoint): choose and document a debug/console grant, test fixture, or explicit scoped decision, with exact command/fixture/scope and validation-only status. Explicitly exclude `Data/Shops`, full Joja economy/content expansion, and any gameplay claim beyond the scoped validation path.
- [x] T047 [US4] If a host-only Gate 3 scenario is added/feasible during implementation, document it under `specs/017-joja-agrochemical-tradeoff/verification/` with required signals and classify as host-only `machine-pass` only; if infeasible, record infeasibility reason and claim only completed lower-level evidence (do not fabricate). Use the T046 provisioning path where relevant. Do not claim human `verified` or D1. Depends on T043–T046.

**Checkpoint**: 017 verification claims are auditable, honestly bounded, and provisioning/SC-004 follow-ups are explicit.

---

## Phase 7: Verification & Attestation (Final — only these tasks attest)

**Purpose**: Produce objective evidence. No agent self-attestation.

- [x] T048 Run targeted 017 automated tests and record pass/fail evidence in `specs/017-joja-agrochemical-tradeoff/verification/automated-tests.md`: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~AgrochemicalPersistence|FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~AgrochemicalContentParity"` (adjust filters to actual names). Include command, date, result summary, and source output/log reference.
- [x] T049 Run and record persistence round-trip evidence in `specs/017-joja-agrochemical-tradeoff/verification/persistence-roundtrip.md`: command/date/filter, pass/fail counts, seam used (`ISoilHealthService.SaveData`/`LoadData` with mocked `IModDataService`, or a bounded integration harness if unit-level round-trip was infeasible), assertion that the C2 unknown-baseline application persists/reloads as measured `90` for `P=10`, and sparse/bounds scope. Classify as automated/integration evidence only (not real-game save/load or Gate 3) unless a real run was executed; if infeasible, record blocked/pending and do not overclaim.
- [x] T050 Run full regression suite and record evidence in `specs/017-joja-agrochemical-tradeoff/verification/automated-tests.md`: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release`. Report exact counts; if failures occur outside 017 scope, classify separately (do not claim 017 complete).
- [x] T051 Run formatting gate and record evidence in `specs/017-joja-agrochemical-tradeoff/verification/static-checks.md`: `dotnet format Stardew-LivingRoots.sln --verify-no-changes` (and build if needed by project workflow). Record pass/fail and command.
- [x] T052 [P] [US4] Complete the manual FR-002/FR-012 semantic tone/meaning/legibility checklist for touched 017 keys in `specs/017-joja-agrochemical-tradeoff/verification/manual-review.md`: record reviewer, date, keys/locales (`default`/`en`/`pt`), shortcut/cost/attribution/refusal meaning parity, cozy/non-punitive tone, and legibility notes. This is manual review, not automated coverage, and does not attest feature completion. May run alongside T048–T051 (disjoint file, no shared tooling).
- [x] T053 Complete/update `specs/017-joja-agrochemical-tradeoff/verification/claim-records.md` with actual evidence classes/source paths/statuses for executed checks (automated/persistence complete or pending as applicable). Mark host-only e2e infeasible/blocked/pending or machine-pass **only** with real evidence; keep D1 and human `verified` pending unless separately attested; keep SC-004 pending unless T045's protocol was actually run. Depends on T048–T052.
- [x] T054 Perform Gate 3 honesty/provenance review recorded in `specs/017-joja-agrochemical-tradeoff/verification/honesty-review.md`: confirm no claim of `verified` without human attestation, no D1 claim without real farmhand evidence, no Harmony fertilizer-hook/vanilla-fertilizer-slot MVP claim, no final balance claim, no double-counting claim, no persistence-overclaim beyond the tested seam, no false success/SC-002/FR-013 claim promoted from fallback-only tests, no SC-004 complete without a recorded run, and no rewrite/retroactive promotion of prior verification artifacts. Depends on T053.
- [x] T055 Produce final implementation status summary in `specs/017-joja-agrochemical-tradeoff/verification/summary.md` based **only** on recorded artifacts from T048–T054 (pass/fail/pending/infeasible/blocked per claim). This summary is evidence-derived; agent narrative alone is not attestation.

**Checkpoint**: Completion can be reported only from Phase 7 evidence artifacts.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup & Scaffolds)**: starts immediately.
- **Phase 2 (Foundational RED)**: depends on Phase 1 scaffolds/paths; **blocks all implementation**.
- **T025 RED gate**: depends on T008–T024; blocks T026+.
- **Phase 3 (US1)**: starts after T025; T029 depends on T011 and T026–T027; T030 is the last US1 fix pass.
- **Phase 4 (US2 i18n)**: starts after T025; T031–T033 `[P]` only after key names are stable, T034 waits for all three, T035 waits for T034.
- **Phase 5 (US3/US2 controller)**: starts after T026–T028 and T031–T035. T036–T042 are one serialized `ModController`/`ModEntry` sequence; T039 waits for T036–T038, T041 waits for T039–T040, T042 waits for T041.
- **Phase 6 (US4)**: docs/evidence tasks `[P]` may proceed after Phase 2; finalization depends on implementation/verification reality. T047 waits for T043–T046.
- **Phase 7 (Verification)**: depends on all desired implementation tasks and Phase 6 finalization. T048–T051 are serialized because they share `dotnet` tooling; T053 waits for T048–T052; T054 waits for T053; T055 is last.

### User Story Dependencies

- **US1 (P1)**: foundational domain outcome plus save/load round-trip; no other story required beyond RED gate.
- **US2 (P1)**: independently testable for text/parity/refusal messaging; runtime confirmation/refusal display and fallback integrate with the US3 controller sequence (T039) after routing/authority/orchestration exist.
- **US3 (P1)**: independently testable for authority/refusal/lock/broadcast via controller tests; runtime depends on US1 service/result and US2 message keys.
- **US4 (P2)**: independently testable as evidence/claim-honesty documentation; final status depends on Phase 7 artifacts.

### Parallel Opportunities / Subagent Waves

| Wave | Subagent (reuse same for shared context) | Parallelizable tasks | Notes |
|---|---|---|---|
| A | Scaffold agent | T002, T003, T004, T005, T006 | Disjoint new files. T007 touches `Constants.cs` alone after or serialized. |
| B | Service-test agent | T008–T013 | Same `AgrochemicalApplicationServiceTests.cs` mostly; T011 uses a separate persistence test file but remains in this wave. No `[P]` within the file. |
| C | Controller-test agent | T014–T020 | Same `ModControllerAgrochemicalTests.cs`; no `[P]` within the file. |
| D1 | i18n/content-test agent | T021–T022 | Same `AgrochemicalContentParityTests.cs`; sequential. |
| D2 | Verification-doc agent | T023, T024 | Disjoint docs, parallel. |
| E | Service implementation agent | T026–T030 | `AgrochemicalApplicationService.cs`/`Constants.cs`/persistence tests shared context; serialized. |
| F | i18n agent | T031, T032, T033 | Disjoint locale files, parallel. T034 consolidation waits; T035 parity fixes after. |
| G | Controller/composition agent (single) | T036–T042 | `ModController.cs` + `ModEntry.cs` shared; strictly serialized. |
| H | Verification-doc agent | T043, T044, T045, T046 | Disjoint docs, parallel where files differ. T047 after. |
| I | Final verification agent(s) | T048–T051 serialized/queued shared tooling (`dotnet`); T052 `[P]` manual review; T053–T054 serialized docs; T055 last | Queue shared `dotnet test`/format/build invocations. |

## Parallel Example: RED test wave

```text
Wave D2 (disjoint docs): T023 claim-record template in specs/017-joja-agrochemical-tradeoff/verification/claim-records.md
Wave D2 (disjoint docs): T024 verification scaffolding notes in specs/017-joja-agrochemical-tradeoff/quickstart.md or specs/017-joja-agrochemical-tradeoff/verification/red-phase.md
Wave F-prep (disjoint locale files after keys known): T031 default.json, T032 en.json, T033 pt.json — only when test/key names are stable
Note: T008–T013 share the service test file and T014–T020 share the controller test file; those are same-file sequences, not parallel work.
```

## Parallel Example: User Story 2

```text
[P] T031 LivingRoots/i18n/default.json
[P] T032 LivingRoots/i18n/en.json
[P] T033 LivingRoots/i18n/pt.json
Then T034 consolidation pass (waits for T031–T033), then T035 parity fixes (serialized).
Phase 5 controller work T036–T042 is a single writer sequence; no [P].
```

## Implementation Strategy

### MVP First (US1 + minimal US2/US3 safety)

1. Complete Phase 1 scaffolds.
2. Complete Phase 2 RED tests (including persistence/boundary/fallback) and **T025 gate**.
3. Deliver US1 service/domain penalty path and save/load round-trip (T026–T030).
4. Add US2 i18n keys/parity (T031–T035).
5. Run the single US3/US2 controller sequence (T036–T042), keeping C4, no-false-success, fallback, and FR-009 invariants.
6. **STOP and VALIDATE** using Phase 7 T048–T055 evidence artifacts before claiming MVP.

### Incremental Delivery

1. Scaffolds + RED gate → safe TDD baseline.
2. US1 soil mutation invariants + persistence (C2/C3, FR-011) → domain MVP core.
3. US2 telegraphing/refusal localization → legible/cozy trust.
4. US3 host authority/lock/FR-009/farmhand refusal/fallback → MP-safe MVP.
5. US4/Phase 7 evidence records → honest attestation.

## Notes

- `[P]` = disjoint files only; shared files (`ModController.cs`, `ModEntry.cs`, `Constants.cs`, same test file) require sequencing/same subagent. T010, T020–T022 are intentionally not `[P]` (same test files). T031–T033 remain `[P]` only because they are distinct locale files; T034 waits for all three.
- Tests use xUnit + Moq + FsCheck patterns already present in `LivingRoots.Tests/`; keep one subject focus per new test file.
- C1 boundary: tests/implementation must not add 014/015 quality/growth mechanical buffs, `HoeDirt.fertilizer` writes, or Harmony fertilizer hooks.
- C2 boundary: never `UpdateHealth(-P)` from unknown/unmeasured soil; use absolute measured `SetSoilHealth`.
- C4 boundary: refuse unsupported farmhand paths before mutation/consumption/false-success feedback.
- Persistence boundary: verify through existing `ISoilHealthService.SaveData`/`LoadData` + `IModDataService` seams; do not add a second persistence layer or claim real disk save/load unless executed.
- Boundary honesty: clamped outcomes must expose `baseline_used`/`post_penalty`/clamp or applied-delta data; do not communicate a full nominal penalty when clamping prevented it.
- Fallback boundary: unavailable feedback must preserve a non-misleading outcome and must not be counted as success or as FR-007/FR-013/SC-002 completion by itself.
- SC-004 boundary: the existing comprehension protocol is finalized/validated as documentation only; execution remains `pending` unless a real run with denominator/pass counts exists.
- Provisioning boundary: use a debug/console grant, test fixture, or scoped validation decision; do not add `Data/Shops`/full Joja economy.
- Gate 3 honesty: host-only `machine-pass` ≠ human `verified` ≠ D1; infeasible runs recorded, never fabricated; no retroactive promotion of prior evidence.
- Commit after each completed task/file edit with atomic conventional commit and staged-file check.

## Phase 8: Convergence — Runner-Based Gate 3 Verification

**Purpose**: Close the runner-based Gate 3 verification gap recorded as `infeasible` in T047/T055. The 016-hardened runner is used to verify the 017 MVP path end-to-end. Runner support is validation-only and keeps existing MVP honesty boundaries.

- [x] T056 Write RED runner test `specs/017-joja-agrochemical-tradeoff/**` plus `scripts/tests/test-017-agrochemical.sh`: generated-macro/property-style cases for `runMacro` step flattening, plus the 017 scenario/`test.md` marker validation; this task BLOCKS T057–T060. per FR-015, US4/AC2 (missing)
- [x] T057 Implement `runMacro` support in `scripts/lib/injection.sh` `injection_build_queue` (steps phase; resolve macro relative to scenario; flatten `/`-prefixed commands with the same semantics as init macros; unknown/missing macro or invalid arguments exit non-zero); keep `/sleep` dropped from the queue. per FR-015 (missing)
- [x] T058 Add validation-only `lr_give_agrochemical <count>` and `lr_apply_agrochemical <tileX> <tileY> [location]` to `LivingRoots.Verification/ModEntry.cs` and `LivingRoots.Verification/Queue/QueueCommandParser.cs`: parse/validate, grant into the host player's inventory, set the current held item, and invoke verification-only application; no `Data/Shops`, economy, or shipped item source. per FR-001, FR-015, US1/AC1, US4/AC1, US4/AC2 (missing)
- [x] T059 Extend `ILivingRootsModApi` + `LivingRoots.Services.LivingRootsModApi` + `LivingRoots/ModEntry.cs` wiring with a host-only `ApplyAgrochemical(locationName, tileX, tileY)` delegating to `ModController.TryApplyAgrochemicalWithLock`; keep the surface dev/CI-only and do not add a second source of truth. per FR-009, FR-015 (missing)
- [x] T060 Create `specs/017-joja-agrochemical-tradeoff/test.md` (initial `partial` GATE3 block, exactly one marker pair) and `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json` (schemaVersion 3): init seeds a tilled farm tile via existing `lr_plant_crop` + `lr_set_soil_health 60 15 50`; a `runMacro` step grants + applies once; expected signals/thresholds assert `soilHealthStateDelta` as `multiplier` on `pre.soilHealth` factor `0.8` (measured 50 -> 40 for `P=10`), `cropPhaseDelta` unchanged (`factor 1.0`), and `smapiLog` host/main-player eligibility (`cmp` `isMainPlayer` `eq` `true`, tolerance `0`). per FR-001, FR-009, FR-011, FR-015, US1/AC1, US1/AC2, US1/AC3, US4/AC2, SC-001, SC-002, SC-003, SC-006 (missing)
- [ ] T061 Rebuild the solution and re-run the full automated suite plus `dotnet format --verify-no-changes` after wiring changes; record counts in `verification/static-checks.md` and `verification/automated-tests.md` without rewriting prior artifacts. per FR-015 (partial)
- [ ] T062 Run the host-only Gate 3 via the 016-hardened runner: `./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build`; record the bundle and let the runner promote only if it is a clean host pass. per FR-015, US1/AC1, US1/AC2, US4/AC2, SC-001, SC-002, SC-003 (missing)
- [ ] T063 After T062, update `verification/host-only-gate3.md` (or a new linked run record), `verification/claim-records.md`, and `verification/summary.md` to reflect the actual run: host-only machine-pass only if the runner promotes; otherwise record failure/infeasibility with the exact artifacts. Keep D1, human `verified`, and SC-004 `pending`; do not demote prior claims into `complete`. per FR-015, US4/AC3, SC-005 (partial)
