---
description: "Task list for the Headless Game Verification Runner"
---

# Tasks: Headless Game Verification Runner

**Input**: Design documents from `/specs/010-headless-verification-runner/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `.specify/memory/constitution.md`

**Tests**: REQUIRED. The user explicitly requested TDD, property-based testing where applicable, and a blocking RED phase before implementation. Every phase therefore starts with test tasks and a RED-gate task; implementation tasks MUST NOT start until the gate passes.

**Organization**: Tasks are grouped by user story. US1 is the MVP; US2 and US3 build on the US1 runner. US2 is **scenario-only** and edits no runner source. US1 (T099) and US3 (T120) are **serialized** because both edit the single entry point `scripts/verify-scenario.sh`; only one agent may own that file at a time.

## Execution Rules (apply to every task)

- **Scaffold first (user rule 0)**: if a file can be produced by a template, an existing project file, or an existing script substrate, scaffold it first and only then edit by hand. T001–T018 are the scaffold tasks; do not hand-write those files before their scaffold step. `LivingRoots.Verification` is scaffolded by **copying `LivingRoots/LivingRoots.csproj` and renaming it**, never by `dotnet new classlib`; `LivingRoots.Verification.Runner` and every new bash suite are scaffolded from the existing project/script substrate named in the task.
- **TDD / RED gate (user rule 1)**: for every phase, all test tasks complete first, then the RED-gate task records the failing output in `specs/010-headless-verification-runner/red-gate-<phase>.log`. No implementation task in that phase may start before the RED gate. Property-based tests (`FsCheck`) and interleaving tests (`Microsoft.Coyote`) are mandatory where listed.
- **Parallel waves (user rule 2)**: `[P]` means different files and no dependency on an incomplete task. Use the wave plan below. A subagent that owns a context (for example all `scripts/lib/*.sh`) MUST be reused for that context rather than spawning a new agent per file.
- **Shared tooling is serialized (user rule)**: only one agent at a time may run `dotnet build`, `dotnet test`, `dotnet format`, `scripts/tests/run-all.sh`, `scripts/tests/lib/schema-validate.sh`, `npx ajv-cli`, `check-jsonschema`, Xvfb, the game, or any long-running test/build command. Other agents queue and wait; never kill or interrupt a command started by another agent, thread, or process.
- **Single orchestrator owner (user rule)**: `scripts/verify-scenario.sh` has exactly one owner at a time. The US1 wiring (T099) and the US3 `--ci` wiring (T120) are the only orchestrator-edit tasks and are serialized, never concurrent. US2 (T110) is scenario-only and never edits runner source; suite discovery lives in Phase 2 (T061b).
- **Verification-only attestation (user rule 3)**: implementation agents MUST NOT claim success, completeness, or “green.” Only the verification tasks T131–T150 attest to the implementation. The **runner writes `machine-pass`**; the **human E attestation writes `verified`** and nothing else does. A task is complete only when its own tests pass and its atomic commit exists.
- **Deterministic commit (user rule 4)**: after every task that edits files, run `git add <paths> && git commit -m "<type>(010): <summary> — <files> — <why>"`. Do not batch edits from different tasks into one commit. Never use `--no-verify`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no incomplete dependency)
- **[Story]**: `[US1]`, `[US2]`, `[US3]` for user-story tasks only
- Every task names at least one exact file path

## Path Conventions

Repository root is `/home/luis/development/stardew-raizes-vivas`. Production mod code lives in `LivingRoots/`; the dev-only capture mod in `LivingRoots.Verification/`; the helper console app in `LivingRoots.Verification.Runner/`; bash libraries and tests in `scripts/`; C# tests in `LivingRoots.Tests/Verification/`; bash tests in `scripts/tests/`; contracts and fixtures in `specs/010-headless-verification-runner/`; verification evidence in `specs/010-headless-verification-runner/verification/` and `specs/010-headless-verification-runner/gate3-evidence/`.

---

## Phase 1: Scaffold-First Setup (Shared Infrastructure)

**Purpose**: Scaffold the two new projects and every shared test substrate from existing templates before any hand-written logic. Add the version-controlled contracts and the feature-005 documentation target. No business logic yet.

- [ ] T001 Scaffold `LivingRoots.Verification/LivingRoots.Verification.csproj` by copying `LivingRoots/LivingRoots.csproj` and renaming the project file (do **not** use `dotnet new classlib`), then edit the copy for `net6.0`, `Nullable=enable`, `LangVersion=latest`, `CheckForOverflowUnderflow=true`, `AppendTargetFrameworkToOutputPath=false`, `Pathoschild.Stardew.ModBuildConfig 4.4.0`, and release-zip exclusion; record the copy/rename command in the commit body.
- [ ] T002 [P] Scaffold `LivingRoots.Verification/manifest.json` from the existing `LivingRoots/manifest.json` template; set the unique ID `lfgranja.LivingRoots.Verification`, `Dependencies` on `lfgranja.LivingRoots`, no update keys, and no release-zip inclusion.
- [ ] T003 [P] Scaffold `LivingRoots.Verification/ModEntry.cs` from the existing `LivingRoots/ModEntry.cs` SMAPI `Mod` template with an empty `Entry(IModHelper)` and a `Dispose` stub; no dump logic yet (implementation is T102).
- [ ] T004 Scaffold `LivingRoots.Verification.Runner/LivingRoots.Verification.Runner.csproj` as a `net6.0` console project copied from the existing SDK project shape, adding `JsonSchema.Net`, `Nullable=enable`, `LangVersion=latest`, and `CheckForOverflowUnderflow=true`; no orchestration, launch, or verdict authority.
- [ ] T005 [P] Scaffold `LivingRoots.Verification.Runner/Program.cs` with the four subcommands `validate-scenario`, `evaluate`, `extract`, and `build-manifest`, each returning the usage contract and exit codes documented in `contracts/exit-codes.md`; leave the bodies as explicit stubs.
- [ ] T006 [P] Scaffold the helper source files `LivingRoots.Verification.Runner/Validation/ScenarioValidator.cs`, `LivingRoots.Verification.Runner/Evaluation/PredicateEvaluator.cs`, `LivingRoots.Verification.Runner/Evaluation/SaveComparator.cs`, `LivingRoots.Verification.Runner/Extraction/ArchiveExtractor.cs`, and `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs` with the exact public shape used by the tests, not implementations.
- [ ] T007 Add both projects to `Stardew-LivingRoots.sln` with `dotnet sln add`, and add `LivingRoots.Verification` plus the helper `ProjectReference`s to `LivingRoots.Tests/LivingRoots.Tests.csproj`; assert the solution contains `LivingRoots`, `LivingRoots.Tests`, `LivingRoots.Verification`, and `LivingRoots.Verification.Runner`.
- [ ] T008 [P] Create `scripts/tests/fixtures/sanitize-vectors.tsv` before T037/T058, with one vector per line covering NFC, `..`, absolute paths, drive letters, backslashes, NUL/control characters, symlink escapes, homoglyph-confusable segments, and valid round-trips.
- [ ] T009 [P] Create `scripts/tests/test-runall.sh` before changing discovery, scaffolded directly from `scripts/tests/lib/harness.sh` plus the header/teardown conventions of `scripts/tests/test-cli.sh` (the same sources T018 packages as the shared template); it initially asserts the existing `scripts/tests/run-all.sh` behavior so the later discovery change has a failing-first test.
- [ ] T010 [P] Add `specs/005-soil-health-to-crop-growth/test.md` with exactly one `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` block copied from `specs/010-headless-verification-runner/contracts/test-md-template.md`; US2 uses feature **005**, never 004.
- [ ] T011 [P] Verify `ROADMAP.md` and `COMPLETION_SUMMARY.md` each contain exactly one GATE3 block (they now do); record the assertion in the commit body and **do not recreate or reset** either block.
- [ ] T012 [P] Validate `specs/010-headless-verification-runner/never-quarantine.json` against `specs/010-headless-verification-runner/contracts/never-quarantine.schema.json`; validate `specs/010-headless-verification-runner/promotion-triggers.json` for exactly the shape `{schemaVersion, components[], transitions[]}` where each component carries `component`/`owner`/`trigger` and `transitions[]` starts empty (every machine-pass appends `{component, from, to, runId, owner, recordedAt}` per C-REL-15); and validate `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` as strict Draft 2020-12; fail closed on any drift.
- [ ] T013 [P] Add `specs/010-headless-verification-runner/compat-matrix.json` with the pinned Stardew Valley 1.6.x line, matching SMAPI 4.4.0+, minimum `1.6.14`, and a `failureClasses` map for retryable/non-retryable exit codes (C-REL-06); also validate the new `specs/010-headless-verification-runner/contracts/claims.schema.json` as strict Draft 2020-12 (root `schemaVersion`/`runId`/`honestyLevel`/`claims`; claim `claimId`/`evidencePointer`/`risk`/`honestyLevel`/`safeWording`/`refusalSite`) under both validators.
- [ ] T014 [P] Add `specs/010-headless-verification-runner/gamepath.lock.json` with `releaseId`, `assetId`, `assetName`, archive SHA-256, and the mandatory per-file digest map; if the private builddeps values are unavailable, ship a fail-closed placeholder that makes the runner exit `19` instead of executing an unverified tree.
- [ ] T015 [P] Add `specs/010-headless-verification-runner/quarantine.json` as an empty registry (`{"schemaVersion":1,"entries":[]}`) with the closed status vocabulary `active|candidate|quarantined|disabled|retired`.
- [ ] T016 [P] Add `scripts/tests/lib/runner-harness.sh` extending `scripts/tests/lib/harness.sh` with fake Xvfb, fake SMAPI log, fake game install, fake milestone save, and a `run_verify_scenario` helper that always isolates `HOME`/`XDG_*`.
- [ ] T017 [P] Add `scripts/tests/lib/schema-validate.sh` wrapping Python `jsonschema` and `ajv-cli` so every schema test runs both validators and asserts identical accept/reject outcomes (C-SCN-38).
- [ ] T018 [P] Add `scripts/tests/lib/test-suite-template.sh` copied from `scripts/tests/lib/harness.sh` plus the header/teardown conventions of `scripts/tests/test-cli.sh`; every new bash suite in T034–T042 and later phases MUST be copied from this template, not hand-written from scratch.

**Checkpoint**: Scaffolds, contracts, and the feature-005 GATE3 target exist; no runner behavior yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain API, shared evaluators, persistence/security primitives, archive extraction, manifest production, and shared bash libraries that every user story uses.

**⚠️ CRITICAL**: No user-story implementation may start until the Phase 2 RED gate (T043) has run and the Phase 2 checkpoint (T062) is green.

### Tests for Phase 2 (write FIRST)

- [ ] T019 [P] xUnit contract tests for `ILivingRootsModApi` in `LivingRoots.Tests/Verification/ModApiContractTests.cs`: the event is raised on the game thread, `SoilGrowthModifierCompletedEventArgs` carries `DayKey`, `IsMainPlayer`, `LocationsProcessed`, `ElapsedMilliseconds`, `Success`, and subscriber exceptions are isolated.
- [ ] T020 [P] xUnit tests for the implementation in `LivingRoots.Tests/Verification/LivingRootsModApiTests.cs`: subscribe/unsubscribe, exception isolation per subscriber, idempotent dispose, no event after dispose, and a stable API instance.
- [ ] T021 [P] xUnit tests for the controller tail in `LivingRoots.Tests/Verification/ModControllerSoilGrowthEventTests.cs`: ordering after `ApplySoilGrowthModifier` for all locations, one event per day, multi-location coverage, the caught-exception path still raises, and dispose/unsubscribe happens under the controller lock.
- [ ] T022 [P] xUnit tests for `ModEntry.GetApi()` in `LivingRoots.Tests/Verification/ModEntryGetApiTests.cs` written **before** T046: `GetApi()` returns a non-null `ILivingRootsModApi` before and after `Entry`, and the same instance is returned across calls.
- [ ] T023 [P] FsCheck property tests for the predicate evaluator in `LivingRoots.Tests/Verification/PredicateEvaluatorPropertyTests.cs`: `cmp` eq/ne tolerance truth table, `between` effective bounds, `multiplier` rounding/clamping, `unchanged`, sentinel mapping, `allOf`/`anyOf`/`not`, and unmeasured-actual ⇒ `fail` with `unmeasured-actual`.
- [ ] T024 [P] FsCheck property tests in `LivingRoots.Tests/Verification/SaveComparatorPropertyTests.cs`: `0.0001f` tolerance, missing key = 0 only at the persistence layer, NaN skipped, and `added`/`removed`/`changed` complete and non-overlapping; replace the existing `LivingRoots.Tests/Verification/SaveComparatorTests.cs` so there is **one subject file**, and reuse/extract the existing helper `LivingRoots.Tests/Verification/SaveComparator.cs` rather than duplicating it.
- [ ] T025 [P] FsCheck property tests for path sanitization in `LivingRoots.Tests/Verification/SanitizerPropertyTests.cs`: NFC normalization, `..`, absolute paths, drive letters, backslashes, NUL/control characters, symlink escapes, and homoglyph-confusable segments are rejected; valid vectors round-trip; assert parity with `ModDataService.SanitizePathSegments`.
- [ ] T026 [P] FsCheck property tests for the manifest digest/anchor in `LivingRoots.Tests/Verification/ManifestDigestPropertyTests.cs`: every subject entry is hashed, `manifest.sha256` covers the finalized manifest, a single changed byte changes the digest, `files` is accepted only as the deprecated alias with the same `{name,digest:{sha256}}` shape, and `{path,sha256}` is rejected.
- [ ] T027 [P] FsCheck property tests for the verdict state machine in `LivingRoots.Tests/Verification/VerdictStateMachinePropertyTests.cs`: closed five-base-verdict set, `flaky` never a base verdict, `19 > 18 > 21 > 20 > 0` aggregation, expected-verdict match/mismatch, code `22` never emitted, and no transition from flaky/quarantined to `verified`.
- [ ] T028 [P] xUnit tests for `ScenarioValidator` in `LivingRoots.Tests/Verification/ScenarioValidatorTests.cs`: Draft 2020-12 acceptance, strict `additionalProperties`, `subject` rules, `modConstant` resolution, `expectedVerdict` rules, signal-field mapping, action-to-required-input rules, and exit `14` with a field path; add `testMdPath` marker validation cases (missing file, wrong extension, missing/duplicate/inverted `<!-- GATE3:BEGIN -->`/`<!-- GATE3:END -->` block) that must exit `14` **before launch**.
- [ ] T029 [P] xUnit tests for `ArchiveExtractor` in `LivingRoots.Tests/Verification/ArchiveExtractorTests.cs`: traversal, absolute paths, drive letters, symlink/hardlink escapes, NUL bytes, overlong names, duplicate members, non-regular members, member-count/byte/ratio defaults `10000`/`2147483648`/`100`, per-file digest mismatch, missing/incomplete map, and target untouched on failure.
- [ ] T030 [P] xUnit tests for the dump payload in `LivingRoots.Tests/Verification/CropPhaseDumpTests.cs`: all top-level and per-crop fields, `soilHealthState` (`measured|missing|nan`), `healthTier` five values, `basePhaseDays` without `ResetPhaseDays`, no `expected*` fields, and `dayKey`/`capturedAtUtc` correlation.
- [ ] T031 [P] xUnit tests for the dump writer in `LivingRoots.Tests/Verification/CropPhaseDumpWriterTests.cs`: temp file → data `fsync` → atomic rename → parent `fsync` → `.done` sentinel **last**, stale sentinel deleted at capture start, asynchronous queue off the frame callback, `done` field/sidecar agreement, and write/fsync/rename failure yields `errored`.
- [ ] T032 [P] xUnit tests for verification-mod shutdown in `LivingRoots.Tests/Verification/VerificationModDisposeTests.cs`: the dispose lock pattern, idempotent unsubscribe, no dump after dispose, and no subscriber exception escaping `Dispose`.
- [ ] T033 [P] Add the `Microsoft.Coyote` package to `LivingRoots.Tests/LivingRoots.Tests.csproj` and write `LivingRoots.Tests/Verification/CoyoteSchedulingTests.cs` to explore concurrent event-raise/dispose interleavings deterministically.
- [ ] T034 [P] Bash behavioral tests in `scripts/tests/test-common.sh`, scaffolded from `scripts/tests/lib/test-suite-template.sh`: codes `0`, `9`, `10`, `12`, `13`, `14`, `18`, `19`, `20`, `21`; code `8` retired; code `11` inherited but never emitted; code `22` reserved and never emitted; unknown flag exits `14`; and a constants-consistency assertion comparing the `ModConstants` class in `LivingRoots/Constants.cs` with `scripts/lib/common.sh`.
- [ ] T034b [P] Bash behavioral tests in `scripts/tests/test-root-guard.sh`, scaffolded from `scripts/tests/lib/test-suite-template.sh`: EUID `0` fails fast with the documented non-root refusal, a non-root invocation passes, and **no Xvfb or game launch** occurs on either path (C-SEC-24 root guard).
- [ ] T034c [P] Bash behavioral tests in `scripts/tests/test-capability.sh` for **C-SEC-32**: the verification mod is excluded from the release zip, writes only its own data directory, performs no network access, uses no reflection beyond `ILivingRootsModApi`, declares the `lfgranja.LivingRoots` manifest dependency with no update keys, and the production mod carries no reference to `LivingRoots.Verification`.
- [ ] T035 [P] Bash behavioral tests in `scripts/tests/test-atomic.sh`: temp-in-target-dir, data `fsync`, rename, parent `fsync`, `EXDEV` is an internal error, target untouched on failure, temp unlinked on failure.
- [ ] T036 [P] Bash behavioral tests in `scripts/tests/test-lock.sh`: advisory locks only, bounded backoff, global order `game-cache → build-output → shared-doc`, `O_EXCL|O_CREAT|O_NOFOLLOW`, live-token conflict, stale reclaim guard, host-shared claim root `RUNNER_CLAIM_ROOT`, and no signal without token match.
- [ ] T037 [P] Bash behavioral tests for bash/C# sanitizer parity in `scripts/tests/test-sanitize.sh` using `scripts/tests/fixtures/sanitize-vectors.tsv`; both implementations must produce identical accept/reject results.
- [ ] T038 [P] Bash contract tests for the scenario schema in `scripts/tests/test-schema-contract.sh`: all `fixtures/valid/*.json` accepted, all `fixtures/invalid/*.json` rejected with exactly one error, all `fixtures/runner-invalid/*.json` schema-accepted but rejected by the runner's cross-field checks, and both validators agree.
- [ ] T039 [P] Bash behavioral tests for the version gate in `scripts/tests/test-version-gate.sh`: SMAPI `SemanticVersion` ordering (`1.11 < 1.2`), `-unofficial.*`, HARD-FAIL/WARN/INFO tiers, null `MaximumGameVersion` as an upper-bound deviation, and no Xvfb launch on HARD-FAIL.
- [ ] T040 [P] Bash behavioral tests for discovery in `scripts/tests/test-runall.sh`: every `scripts/tests/test-*.sh` discovered, non-matching files ignored, explicit file/path selection, per-suite summary, any non-zero suite fails the run, empty directory fails clearly, and no duplicate execution.
- [ ] T041 [P] Bash behavioral tests for retention/WORM in `scripts/tests/test-retention.sh`: quarterly sample ≥30 bundles, durable-store receipt, retention timestamp ≥365 days, early deletion denied with an audit event, and append-only/WORM export path.
- [ ] T042 [P] Bash contract tests in `scripts/tests/test-contract-registries.sh`: `never-quarantine.json` validates against `contracts/never-quarantine.schema.json` (closed ten-class set); `promotion-triggers.json` has the `{schemaVersion, components[], transitions[]}` shape with pre-populated deferred components, `transitions[]` starting empty, and no new thresholds; `contracts/evidence-bundle.schema.json` validates as strict Draft 2020-12 under both validators; and `contracts/claims.schema.json` validates as strict Draft 2020-12 with its required root and claim fields.
- [ ] T043 **RED gate (Phase 2)**: run `scripts/tests/run-all.sh` plus `dotnet test` and record the failing output in `specs/010-headless-verification-runner/red-gate-phase-2.log` and the commit body; every test file above must exist and fail for the intended reason before T044+ start. Do not implement yet.

### Implementation for Phase 2

- [ ] T044 [P] Add `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs` with `ApiVersion`, `SoilGrowthModifierCompleted`, and `SoilGrowthModifierCompletedEventArgs`; keep it SMAPI-free.
- [ ] T045 Add `LivingRoots/Services/LivingRootsModApi.cs` implementing the interface, isolating every subscriber exception, and exposing idempotent subscription/disposal; depends on T044.
- [ ] T046 Modify `LivingRoots/ModEntry.cs` to implement `GetApi()` returning the stable `ILivingRootsModApi` instance; this task runs after the failing T022 and after T044/T045.
- [ ] T047 Modify `LivingRoots/Controllers/ModController.cs` to raise `SoilGrowthModifierCompleted` synchronously on the game thread via `RaiseSoilGrowthModifierCompleted(args)` at the correct tail of `OnDayEnding`, after `ApplySoilGrowthModifier` for all locations (success or caught exception) and before the day transition; isolate subscriber exceptions; depends on T044.
- [ ] T048 Add the farmhand guard to the scaffolded subscriber in `LivingRoots.Verification/ModEntry.cs`: `if (!args.IsMainPlayer) return;` before any dump work; no farmhand dump or seeding path; depends on T044.
- [ ] T049 [P] Implement `LivingRoots.Verification.Runner/Validation/ScenarioValidator.cs` for Draft 2020-12 plus the cross-field subject/`modConstant`/`expectedVerdict`/signal-field/action-input rules, returning exit `14` with a field path.
- [ ] T050 [P] Implement `LivingRoots.Verification.Runner/Evaluation/PredicateEvaluator.cs` for `cmp`, `between`, `multiplier`, `sentinel`, `unchanged`, `allOf`, `anyOf`, `not`; enforce unmeasured-actual ⇒ `fail`, multiplier field minima (`phaseDays` 1, all other numeric fields 0), and 99999 never multiplied/clamped.
- [ ] T051 [P] Implement `LivingRoots.Verification.Runner/Evaluation/SaveComparator.cs` with `0.0001f` tolerance, missing key = 0 only at the persistence layer, NaN ignored, and `added`/`removed`/`changed` output; this is the only comparator the US1 evidence path may use.
- [ ] T052 [P] Implement `LivingRoots.Verification.Runner/Extraction/ArchiveExtractor.cs` in-process with member-count/byte/ratio limits (defaults `10000`/`2147483648`/`100`), duplicate/non-regular rejection, per-file digest verification, and target untouched on failure.
- [ ] T053 [P] Implement `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs` as the **sole producer** of `manifest.json`, the detached `manifest.sha256`, and each per-attempt manifest; include all fields required by `contracts/evidence-bundle.schema.json`, with `files` only as the deprecated alias.
- [ ] T054 [P] Implement `scripts/lib/common.sh` with all runner constants, the exit-code enum from `contracts/exit-codes.md`, and the `machine-pass`/`verified` vocabulary; no magic numbers elsewhere.
- [ ] T055 [P] Implement `scripts/lib/atomic.sh` (`atomic_write`, `fsync_dir`, temp cleanup) per C-CON-04/C-CON-13/C-CON-14.
- [ ] T056 [P] Implement `scripts/lib/concurrency.sh` (`lock_acquire`, `claim_acquire`, `pid_identity`, `orphan_registry`) with the C-CON-08/C-CON-09/C-CON-22/C-CON-23 contracts and the host-shared claim root `RUNNER_CLAIM_ROOT`.
- [ ] T057 [P] Implement `scripts/lib/logging.sh` with JSON Lines audit events, application logs, and pre-write redaction for success/failure/verbose/CI/JUnit/Allure/error sinks; never log a raw exception message or a token (including Base64/URL/hex variants).
- [ ] T058 [P] Implement `scripts/lib/sanitize.sh` with the same contract as `ModDataService.SanitizePathSegments`; expose one normalized repo-relative path or fail with exit `14`/`19` as appropriate; consume the shared `sanitize-vectors.tsv`.
- [ ] T059 [P] Implement `scripts/lib/version-gate.sh` parsing `Stardew Valley.dll`, `StardewModdingAPI.dll`, and `manifest.json` with SMAPI `SemanticVersion`, then classifying against `compat-matrix.json` (HARD-FAIL/WARN/INFO).
- [ ] T060 [P] Implement `scripts/lib/scenario-validate.sh` invoking the .NET helper for Draft 2020-12 validation and enforcing `between.min <= between.max`, `multiplier.base` suffix equals `field`, sentinel-to-field mapping, signal-to-field mapping, action-to-required-input, `subject` rules, `expectedVerdict` rules, and exit `14` with a field path.
- [ ] T061 [P] Implement `scripts/lib/retention.sh`: durable-store receipts, per-artifact retention timestamps, WORM/append-only export, and approval-gated deletion that refuses before expiry and writes an audit event.
- [ ] T061b [P] Implement `scripts/lib/suite.sh` as a standalone library: file/directory suite discovery (`*.scenario.json` with `*.json` fallback), lexicographic ordering, per-suite rerun cap, and aggregate precedence, with no orchestrator wiring here; T099 consumes it through `--scenario <dir>` and `scripts/tests/test-suite.sh` (T106) covers its behavior.
- [ ] T061c [P] Implement discovery in `scripts/tests/run-all.sh`: every `scripts/tests/test-*.sh` is discovered, explicit file/path selection is honored, an empty directory fails clearly, and no suite ever executes twice; `scripts/tests/test-runall.sh` (T009/T040) is the failing-first test.
- [ ] T061d [P] Add `scripts/tests/test-spec-consistency.sh` as the runnable wrapper for `scripts/tests/spec-consistency.py` plus the **C-SCN-36** mirror check that the `data-model.md` required-field table equals the `contracts/scenario.schema.json` required arrays; exit non-zero on drift.
- [ ] T061e [P] Implement statistics in `scripts/lib/statistics.sh`: Wilson 95% CI gate (N ≥ 30, upper bound < 1.0, observed p ≥ 0.30), pass-rate tiers (`healthy` ≥ 99%, `monitor` 95–99%, `alert` < 95%), and RAFT/Benjamini–Hochberg multiple-comparison control at q ≤ 0.05, feeding the `statisticalStatus` closed set (T076).
- [ ] T062 **Phase 2 checkpoint**: run the Phase 2 suites once via `scripts/tests/run-all.sh` (queued, single agent) and confirm green; commit `feat(010): ...`. This checkpoint is not an attestation of the feature.

**Checkpoint**: Foundation ready; user stories can begin.

---

## Phase 3: User Story 1 - Maintainer runs full verification unattended (Priority: P1) 🎯 MVP

**Goal**: One command acquires/reuses game files, launches Stardew under Xvfb + SMAPI, injects the scenario, captures all three signals, evaluates thresholds, writes the evidence bundle, and writes `machine-pass` only on a clean non-flaky `pass`.

**Independent Test**: On a host with `STARDEW_GAME_PATH` and either `BUILDDEPS_TOKEN` or a valid cache, run `./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json`; expect exit `0`, three signal artifacts, a valid bundle, and `machine-pass` (never `verified`) in `specs/004-playable-compost-loop/test.md`. With a tampered threshold, expect exit `18` and no promotion.

### Tests for US1 (write FIRST)

- [ ] T063 [P] [US1] CLI/usage tests in `scripts/tests/test-verify-cli.sh`: required/unknown flags, `--scenario` file/dir, `--scenario-glob`, `--no-acquire`, `--force-refresh` incompatibility, `--override` allow/deny list, `--ignore-exit-code` integrity carve-out (never demote integrity/security or claim overreach `21`), `--verify-bundle`, and `--check-docs`.
- [ ] T064 [P] [US1] Game-path/build-override tests in `scripts/tests/test-game-path.sh`: `--game-path` missing/empty exits `14`, set-but-invalid follows cache/`9`, valid path reuses and is recorded in `environmentOverrides`, `--skip-build` missing artifact exits `12` with `buildSkipped: true`, and a missing prerequisite records `BLOCKED` with the exact missing `STARDEW_GAME_PATH`/assembly; also assert that the prebuilt production mod **and** the verification mod are both present under `Mods/`, that a missing deployment exits `12`, and that a missing required assembly exits `9` — all before launch (FR-004).
- [ ] T065 [P] [US1] Acquisition/cache tests in `scripts/tests/test-acquisition.sh`: token via `gh` environment or `curl --config`, never argv/URL/logs; valid cache with no token/network; cache miss with token; `--no-acquire` cache miss exits `9`; checksum mismatch exits `19` with `integrity-failure.json`; 401/403/404 terminal; 5xx/429 retry; immutable asset pinning; non-HTTPS or non-GitHub redirect rejected; mutable `latest` rejected; asset ID/name mismatch rejected; absent/incomplete per-file digest map fails closed.
- [ ] T066 [P] [US1] Display/launch tests in `scripts/tests/test-display-launch.sh`: `-displayfd` allocation, no `/tmp/.X*-lock` probe, ready-state sentinel + inode change, `Loaded N mods:`, skipped-mod detection, `GameLaunched` marker, display failure exit `10`, and the two-phase timeout.
- [ ] T067 [P] [US1] Scenario injection tests in `scripts/tests/test-injection.sh`: typed `initCommands`, `/`-prefixed macro lines, `.txt` extension, no nested macros, `debug runmacro`, console commands, no `debug ` prefix, `/sleep` last, completion markers, host atomic lock for seeding (reuse the `HostAtomicLockServiceTests` pattern), and **no farmhand seeding**; assert `xdotool` is absent, stdin is closed, and zero stdin events are observed (SC-001).
- [ ] T068 [P] [US1] Signal capture tests in `scripts/tests/test-signals.sh`: three independent channels, dump written after `SoilGrowthModifierCompleted`, `.done` sentinel last, save-file cross-check, divergence ⇒ `partial` plus `artifacts/anomaly-report.json` with `runId`, `signal`, `kind`, `observed`, `expected`, `owner`, `date`, `rootCause`, and farmhand absence of mutation plus the host-guard skip line.
- [ ] T069 [P] [US1] Threshold/evaluation tests in `scripts/tests/test-evaluation.sh`: 33/34/66/67 ⇒ 1.15/1.00/1.00/0.85, `phaseDays >= 1`, 99999 untouched, missing/NaN/≤0 non-participants, `healthTier` five values, `subject` selection, and unmeasured-actual.
- [ ] T070 [P] [US1] Verdict/exit-semantics tests in `scripts/tests/test-verdict.sh`: five base verdicts plus derived `flaky`; expected-verdict mismatch exits `19`; a missing **signal** artifact yields `partial`/`19` while a missing `manifest.json`/`claims.json`/`manifest.sha256`/named per-attempt record yields `errored`/`19`; code `21` is POLICY; code `22` is reserved and never emitted; a mixed suite with a pass exits `0` while all-skipped exits `20`; aggregation is `19 > 18 > 21 > 20 > 0`; `attempts[]` contains only `attempts/<n>/attempt.json`; `files` uses the alias shape.
- [ ] T071 [P] [US1] Evidence-bundle tests in `scripts/tests/test-evidence.sh`: `manifest.json`, mandatory `manifest.sha256`, `claims.json`, `artifacts/`, `junit.xml`, `allure.json`, `subject` digests, `attempts/<n>/attempt.json`, per-attempt manifests listed in `subject` (never in `attempts[]`), warmup not an attempt, `integrity-failure.json`, validation against `contracts/evidence-bundle.schema.json`, the **single-producer** rule (no competing digest or hand-written manifest), and `commands`/`materials`/`seeds` completeness: present and non-empty for honesty level R/E, absence allowed for W, completeness flags (`commandsCompleteness`, `materialsCompleteness`, `seedsCompleteness`) agreeing with presence, and every `subject` entry covered.
- [ ] T072 [P] [US1] Promotion tests in `scripts/tests/test-promotion.sh`: `partial → machine-pass` only on a clean non-flaky `pass` with three agreeing signals and honesty ≥ R; flaky pass stays `partial`; `fail`/`errored`/`partial`/`skipped` never promote; farmhand/`debug split` never `machine-pass`; marker replacement is byte-preserving outside the block; missing/duplicate/inverted markers exit `14`; and the runner **never writes `verified`** (only the human E task does).
- [ ] T073 [P] [US1] Save-preservation tests in `scripts/tests/test-save-preservation.sh`: the player save is never opened; a disposable copy is made; the source hash is unchanged; the working copy is restored and matches the source; mismatch exits `19`; sidecar backup and atomic restore; and the terms player/source/working are used consistently.
- [ ] T074 [P] [US1] Cleanup/clean-quit tests in `scripts/tests/test-cleanup.sh`: per-instance temporary mods path (real `Mods/` untouched), verification mod removed/absent with a hash check, process-group SIGTERM→SIGKILL→`waitpid`, PGID-reuse guard, owned-display socket removal, orphan registry report-only, temp cleanup preserves the exit code, `cleanup_failed` recorded, and the clean-quit postconditions owned by `scripts/lib/quit.sh` (T094b): save-mode completion, mtime/size stability ≥ 30 s, graceful SMAPI exit, SIGKILL backstop, and the `cleanQuit` flag (no children, clean mods path, restored working copy, no display socket).
- [ ] T075 [P] [US1] Retry/flaky tests in `scripts/tests/test-flaky.sh`: only classified transients retry; 2 retries / 3 attempts; 5 s cooldown; per-attempt artifacts; flaky = attempt-1 failure + later pass on the same commit/seed/environment; flaky exits `0` and never promotes; save I/O exhaustion exits `19`; macro timeout inherits `4`; there is exactly **one** 14-day flaky/quarantine trigger (no duplicate trigger path); retry is **idempotent** (attempt paths are unique — never duplicated or overwritten — within a run); and the quarantine owner chain resolves `--quarantine-owner` → `QUARANTINE_OWNER` → git identity → repo owner, recorded in `quarantine.json`.
- [ ] T076 [P] [US1] Quarantine tests in `scripts/tests/test-quarantine.sh`: a **single** quarantine trigger (2 flaky runs ⇒ candidate); human approval; 14-day box + one 14-day extension; 28-day auto-disable; retirement is human-approved; quarantined reports `<skipped>`; shadow-pass re-enablement; never-quarantine classes/scenarios are never candidates; and statistical status tests for the Wilson 95% CI gate (N ≥ 30, upper < 1.0, p ≥ 0.30), RAFT/Benjamini–Hochberg q ≤ 0.05, and the closed `statisticalStatus` set `not-evaluated|insufficient-data|confirmed-flaky|stable`.
- [ ] T077 [P] [US1] Docs-drift tests in `scripts/tests/test-docs.sh`: `--check-docs` `0`/`21`/`14`; marker-only replacement; idempotency; timestamps only on state change; root docs never updated on a flaky pass; negative missing/duplicate/inverted GATE3 markers; and the block says `machine-pass`, never `verified`.
- [ ] T078 [P] [US1] Extraction-negative tests in `scripts/tests/test-extraction.sh`: zip-slip, absolute paths, drive letters, symlink/hardlink escapes, NUL, overlong names, duplicate members, non-regular members, member-count/byte/ratio bombs, missing/incomplete per-file digest map, target untouched, and the in-process (no `unzip`/`tar` shell-out) rule.
- [ ] T079 [P] [US1] Stress tests in `scripts/tests/test-stress.sh`: K ≥ 30 interleaving runs, the 1000-loop harness, `interleaving_rate=0`, `correctness_rate=1.0`, `sentinel_order_violations=0`, and the differential golden oracle.
- [ ] T080 [P] [US1] Performance tests in `scripts/tests/test-performance.sh`: one warmup discarded; each per-location elapsed value evaluated independently against **5 ms**, with **any** single value > 5 ms failing (exit `18`); no averaging/p95/sum; Gen0 collection delta recorded; and `gc-attributed` tagged when > 0.
- [ ] T081 [P] [US1] Redaction/argv tests in `scripts/tests/test-redaction.sh`: a fake token placed in every sink (success/failure/verbose/CI/JUnit/Allure/error) must not survive; raw exception messages are redacted; the token never appears in argv, URLs, or logs; and Base64/URL-encoded/hex variants are also caught.
- [ ] T082 [P] [US1] Evidence-bundle negative-fixture tests in `scripts/tests/test-bundle-negative.sh`: one tampered fixture per artifact class (missing manifest, missing `manifest.sha256`, missing `claims.json`, each missing signal, missing per-attempt record, digest tamper) plus a claim/honesty overreach that exits `21`; negative fixtures for `commands`/`materials`/`seeds` (missing for R/E, empty arrays, incomplete completeness flags, uncovered `subject`); the offline verifier must reject every one.
- [ ] T083 [US1] **RED gate (US1)**: run all US1 test files via `scripts/tests/run-all.sh` and record the failing output in `specs/010-headless-verification-runner/red-gate-phase-3.log`; no US1 implementation may start until every test file exists and fails for the intended reason.

### Implementation for US1

- [ ] T084 [P] [US1] Implement `scripts/lib/acquisition.sh`: cache validation, `--no-acquire`, `--force-refresh`, GitHub Releases asset download with the token supplied by environment/`gh` or `curl --config` (never argv), `.part` + atomic rename, SHA-256 + per-file digest verification, redirect/host pinning, retry classification, and `integrity-failure.json`.
- [ ] T085 [P] [US1] Implement `scripts/lib/preflight.sh`: host tools, `STARDEW_GAME_PATH` and `--game-path` missing/invalid/valid branches, six required assemblies, deployment check, non-root guard, version-matrix gate before Xvfb, `--skip-build` missing-artifact exit `12`, and a `BLOCKED` record naming the exact missing prerequisite.
- [ ] T086 [P] [US1] Implement `scripts/lib/display.sh`: `-displayfd`, per-instance display, bounded cleanup (ESRCH/ENOENT/connect/no-zombie), owned-token force-unlink only, orphan registry report-only.
- [ ] T087 [P] [US1] Implement `scripts/lib/save.sh`: per-instance `mktemp -d`, `XDG_CONFIG_HOME`/`HOME`/`XDG_*`, milestone copy as the working copy, sidecar backup, source hash before/after, player save never opened, idempotent atomic restore.
- [ ] T088 [P] [US1] Implement `scripts/lib/launch.sh`: SMAPI launch env (`SMAPI_NO_TERMINAL`, software GL, SDL/audio vars), log-sentinel + inode freshness, `Loaded N mods:`, skipped-mod check, `GameLaunched` marker, and the two-phase timeout.
- [ ] T089 [P] [US1] Implement `scripts/lib/injection.sh`: typed `initCommands`, macro validation (UTF-8/BOM, `/` lines, `.txt`), `debug runmacro`, console commands, `/sleep` ordering, completion markers, no nested macros, host atomic lock for seeding, and no farmhand seeding path.
- [ ] T090 [P] [US1] Implement `scripts/lib/signals.sh`: SoilHealthState delta, in-process Crop dump + save cross-check, SMAPI log excerpt, mandatory `expectedSignals` presence, and `artifacts/anomaly-report.json` on any divergence/missing/mismatch; divergence ⇒ `partial`.
- [ ] T091 [P] [US1] Implement `scripts/lib/verdict.sh`: five base verdicts + derived `flaky`, expected-verdict match/mismatch, missing-signal ⇒ `partial` vs missing-manifest/claims/anchor ⇒ `errored`, host-guard breach, suite aggregation `19 > 18 > 21 > 20 > 0`, and exit mapping including code `21` POLICY and reserved code `22`.
- [ ] T092 [P] [US1] Implement `scripts/lib/evidence.sh`: run-id format, bundle layout, invocation of `ManifestBuilder` (the single producer), `claims.json`, `artifacts/`, `manifest.sha256` only after finalization, per-attempt `attempt.json`, append-only `attempts[]`, `integrity-failure.json`, and wiring to `scripts/lib/retention.sh` for the durable-store receipt/export; never hand-write manifest JSON or compute a competing digest.
- [ ] T093 [P] [US1] Implement `scripts/lib/promotion.sh`: GATE3 marker parsing, byte-preserving replacement, idempotency check, only the six legal promotion transitions (C-REL-15), append `{component, from, to, runId, owner, recordedAt}` to `promotion-triggers.json` on every machine-pass, root-doc updates only on a clean non-flaky pass, one promotion owner, `--check-docs`, and **no code path that writes `verified`**.
- [ ] T094 [P] [US1] Implement `scripts/lib/cleanup.sh`: trap/finally, process-group teardown, `waitpid`, per-instance temporary mods path, verification-mod absence/hash assertion against the real `Mods/`, display/temp cleanup, orphan report, exit-code preservation, and `cleanup_failed`.
- [ ] T094b [P] [US1] Implement `scripts/lib/quit.sh`: save-mode request, `debug` sleep/end-of-day progression, mtime/size stability for ≥ 30 s, graceful exit, a SIGKILL backstop after 90–120 s, and the `cleanQuit` flag consumed by cleanup; the postconditions are tested by T074.
- [ ] T095 [P] [US1] Implement `scripts/lib/retry.sh`: transient classification from `compat-matrix.json`, 3 attempts, 5 s cooldown, per-attempt artifacts, flaky derivation, a single trigger path, save-I/O exhaustion ⇒ `19`, macro timeout ⇒ `4`, and `--ignore-exit-code` never rewriting the recorded verdict.
- [ ] T096 [P] [US1] Implement `scripts/lib/quarantine.sh`: single-trigger candidate detection, human approval gate, 14/28-day time-box, auto-disable, retirement, shadow-pass counting, `<skipped>` reporting, and the `never-quarantine.json` exclusion.
- [ ] T097 [P] [US1] Implement `scripts/lib/performance.sh`: one discarded warmup, per-location ≤ 5 ms with **any** violation failing, young-generation collection delta, and the `gc-attributed` tag.
- [ ] T098 [P] [US1] Implement the stress harness in `scripts/tests/stress/run_stress.sh` (K ≥ 30 and 1000-loop) and make it callable from the orchestrator without changing verdict semantics.
- [ ] T099 [US1] Wire `scripts/verify-scenario.sh` to T084–T098 in the required order (preflight → acquisition → version gate → isolate → launch → inject → capture → evaluate → evidence → promote → cleanup); include `--game-path`, `--skip-build`, `--override`, `--milestone-save`, `--quarantine-owner`, and `--scenario <dir>` wired to `scripts/lib/suite.sh` (T061b); `--verify-bundle`/`--check-docs` dispatch stays in T104 and the CI flag is added only by T120; **single agent only**, never run concurrently with US3.
- [ ] T100 [P] [US1] Implement `LivingRoots.Verification/Evidence/CropPhaseDump.cs`: top-level and per-crop fields including `soilHealthState` (`measured|missing|nan`), `healthTier` five values, `basePhaseDays` from `CropData` without `ResetPhaseDays`, and no `expected*` fields.
- [ ] T100b [P] [US1] Capture the **C-REL-25** implementation-time research in `specs/010-headless-verification-runner/research/clean-quit.md`: SIGTERM/`OnExiting` behavior, the save-complete signal, and `Game.Exit` under Xvfb, with citations, before any promotion beyond Phase 1.
- [ ] T101 [US1] Implement `LivingRoots.Verification/Evidence/CropPhaseDumpWriter.cs`: stale sentinel deletion → temp file → `fsync` → atomic rename → parent `fsync` → `.done` sentinel last; asynchronous write queue; failure is a capture-path `errored`; depends on T100.
- [ ] T102 [US1] Implement `LivingRoots.Verification/ModEntry.cs` async dump: subscribe to `ILivingRootsModApi` in `GameLaunched`, capture at `DayEnding` after the event, enforce `if (!args.IsMainPlayer) return;`, invoke the writer, isolate exceptions, and unsubscribe on dispose under the lock pattern; depends on T048/T100/T101.
- [ ] T103 [US1] Implement `scripts/verify-evidence-bundle.sh`: schema validation against `contracts/evidence-bundle.schema.json`, digest recompute against `manifest.sha256`, mandatory-artifact refusal sites, claim/honesty gating (`0`/`19`/`21`), and no game launch; depends on T092 plus the Phase 2 primitives (T054–T061e).
- [ ] T104 [US1] Add `--verify-bundle` and `--check-docs` dispatch to `scripts/verify-scenario.sh` without launching the game; both forms must be launch-free and return the contract codes; runs explicitly after T099 and T103; dispatch only, with no launch or suite implementation.
- [ ] T105 [US1] **US1 checkpoint**: run the US1 suite via `scripts/tests/run-all.sh` (queued, single agent), then a launch-free end-to-end with `scripts/tests/lib/runner-harness.sh`; record the result in `specs/010-headless-verification-runner/verification/us1-checkpoint.log`. Do not claim real-host success or `machine-pass` yet.

**Checkpoint**: US1 is independently functional and testable.

---

## Phase 4: User Story 2 - Contributor reuses the runner for another feature (Priority: P2)

**Goal**: A contributor adds a JSON scenario (optional Markdown companion) for a second feature and runs the same runner with zero runner-source changes.

**Independent Test**: Add the feature-005 scenario, run it against the same harness, and confirm the PR diff contains only scenario/docs/fixture files — no `scripts/` or `LivingRoots*` source changes.

### Tests for US2 (write FIRST)

- [ ] T106 [P] [US2] Suite-discovery tests in `scripts/tests/test-suite.sh`, scaffolded from `scripts/tests/lib/test-suite-template.sh`: single file, directory glob `*.scenario.json` (fallback `*.json`), lexicographic order, suite rerun cap, empty suite exit `20`, aggregate precedence, and the suite-negative cases (empty directory, malformed scenario in a suite, rerun cap).
- [ ] T107 [P] [US2] Scenario-authoring tests in `scripts/tests/test-scenario-authoring.sh`: the second feature **005** scenario validates against both validators, `testMdPath` exists with exactly one GATE3 block, the optional Markdown companion does not affect validation, and the runner executes it with no core changes.
- [ ] T108 [P] [US2] Zero-core-change tests in `scripts/tests/test-zero-core-change.sh`: the diff for the second feature contains only `specs/005-soil-health-to-crop-growth/` scenario/docs/fixtures, with no `scripts/` or `LivingRoots*` source edits.
- [ ] T109 [US2] **RED gate (US2)**: run `scripts/tests/test-suite.sh`, `scripts/tests/test-scenario-authoring.sh`, and `scripts/tests/test-zero-core-change.sh`; record the failing output in `specs/010-headless-verification-runner/red-gate-phase-4.log`; no US2 implementation may start yet.

### Implementation for US2

- [ ] T110 [US2] Add only the feature-005 directory-suite fixture (the `specs/005-soil-health-to-crop-growth/scenarios/` directory consumed by `scripts/lib/suite.sh` (T061b) together with the T111 scenario), run it through the existing `--scenario <dir>` wiring, and assert the US2 diff has **zero runner-source changes** — no edit to `scripts/verify-scenario.sh`, `scripts/lib/suite.sh`, or any other runner source — while preserving per-scenario JUnit/Allure entries and the aggregate verdict. **Serialized after T105**.
- [ ] T111 [US2] Add the second-feature scenario `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json` with a different threshold/expected verdict but the same contract; no runner-source edits.
- [ ] T112 [US2] Add `specs/010-headless-verification-runner/SCENARIO-AUTHORING.md` with the JSON-first workflow, optional Markdown companion, `subject` selection, expected-verdict rules, and the zero-core-change rule.
- [ ] T113 [P] [US2] Add `specs/010-headless-verification-runner/contracts/scenario-companion-template.md` as the optional human-facing checklist template.
- [ ] T114 [P] [US2] Update `specs/010-headless-verification-runner/fixtures/README.md` with the second feature-005 scenario and the directory-suite example.
- [ ] T115 [US2] **US2 checkpoint**: run the feature-005 directory suite (`specs/005-soil-health-to-crop-growth/scenarios/`) through `scripts/verify-scenario.sh --scenario` with the fake host; assert the aggregate verdict, per-scenario JUnit/Allure entries, and that the diff contains only scenario/docs/fixtures with zero runner-source changes.

**Checkpoint**: US1 and US2 both work independently.

---

## Phase 5: User Story 3 - CI runs verification on a self-hosted runner (Priority: P3)

**Goal**: CI mode runs the same entry point non-interactively, emits JUnit XML + Allure JSON, uses a self-hosted runner with the builddeps token, and keeps public GitHub-hosted CI dormant.

**Independent Test**: On a self-hosted runner with `STARDEW_GAME_PATH` and `BUILDDEPS_TOKEN` (or cache), `.github/workflows/verify-scenario.yml` runs a scenario, uploads the bundle, and reports `fail`/`errored`/`partial` as non-zero; a public-hosted PR does not invoke it.

### Tests for US3 (write FIRST)

- [ ] T116 [P] [US3] CI-mode tests in `scripts/tests/test-ci-mode.sh`, scaffolded from `scripts/tests/lib/test-suite-template.sh`: `--ci` is non-interactive, same verdict/schema as local, no prompts, stdout/stderr separation, and exit mapping unchanged.
- [ ] T117 [P] [US3] Reporter tests in `scripts/tests/test-reporters.sh`: JUnit XML with `<skipped>` for quarantine, Allure JSON with `flaky: true`, per-scenario entries preserved, and malformed paths rejected.
- [ ] T118 [P] [US3] Workflow-contract tests in `scripts/tests/test-ci-workflow.sh`: self-hosted labels, token scoping, `cache-mode`, least privilege, high/low runner group separation, and no invocation on public-hosted PRs (public-hosted dormancy).
- [ ] T119 [US3] **RED gate (US3)**: run `scripts/tests/test-ci-mode.sh`, `scripts/tests/test-reporters.sh`, and `scripts/tests/test-ci-workflow.sh`; record the failing output in `specs/010-headless-verification-runner/red-gate-phase-5.log`; no US3 implementation may start yet.

### Implementation for US3

- [ ] T120 [US3] Implement `scripts/lib/ci.sh` and wire `--ci` into `scripts/verify-scenario.sh`: non-interactive, machine-readable summary, no behavior change to verdict/security. **Serialized after T099 and US2 T115**; the same single orchestrator owner as T099.
- [ ] T121 [US3] Implement `scripts/lib/reporters.sh`: JUnit XML + Allure JSON writers, `<skipped>` for quarantine, `flaky` metadata, and per-scenario entries.
- [ ] T122 [US3] Scaffold `.github/workflows/verify-scenario.yml` from the existing `.github/workflows/build_and_test.yml` substrate, then scope it to a self-hosted runner, least-privilege `GITHUB_TOKEN`, `cache-mode`, token from secrets, bundle upload + durable-store export, and a public-hosted guard.
- [ ] T122b [US3] Create `.github/workflows/security-aggregation.yml`: daily `schedule` plus `workflow_dispatch`; `permissions: contents: read` and `issues: write`; fail closed when `vars.RUNNER_AUDIT_STORE` is unset; query the trailing 24 h for `401`/`403` events; at ≥ 3 occurrences open or update a single issue.
- [ ] T122c [US3] Create `.github/CODEOWNERS` assigning `@lfgranja` as owner of `specs/010-headless-verification-runner/`, `scripts/verify-scenario.sh`, `scripts/verify-evidence-bundle.sh`, `LivingRoots.Verification/`, and `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs`.
- [ ] T123 [US3] Update `specs/010-headless-verification-runner/GOVERNANCE-ANNEX.md` with the CI control matrix, runner-group split, cache-mode settings, and public-hosted dormancy referenced by the workflow.
- [ ] T124 [US3] **US3 checkpoint**: run `scripts/tests/test-ci-mode.sh` and `scripts/tests/test-reporters.sh` with stubs; assert local/CI verdict parity and that `scripts/tests/test-ci-workflow.sh` keeps public-hosted CI dormant.

**Checkpoint**: All three user stories work independently, with the single orchestrator edited serially.

---

## Phase 6: Polish & Verification (ONLY these tasks attest success)

**Purpose**: Documentation, hardening evidence, and the authoritative verification pass. Implementation agents must not claim completion before T131–T150. Negative/security test **authoring** lives in the owning phases (T028, T034b, T034c, T065, T067/T068, T077, T082, T106, T125b); Phase 6 only executes those suites and records the RED/green path.

- [ ] T125 [P] Update `specs/010-headless-verification-runner/quickstart.md` with the final command lines, the two-validator schema check, `--game-path`/`--skip-build` documentation, and the verification-only attestation note; record commands and placeholders only, not pre-stated actuals.
- [ ] T125b [P] Author `scripts/tests/test-doc-links.sh`: assert `VERIFICATION_PROTOCOL.md` §3 and `specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md` exist, that the required anchors are present, and that both link back to `specs/010-headless-verification-runner/spec.md` FR-021; drift exits `21`.
- [ ] T126 [P] Update `specs/010-headless-verification-runner/traceability.md` from planned to actual test case IDs and code paths; keep one row per CHK and per SC.
- [ ] T127 [P] Update `specs/010-headless-verification-runner/CONCURRENCY.md` §9 review date and confirm every primitive, including process-group reaping and the XDG probe, has a row.
- [ ] T128 [P] Update `specs/010-headless-verification-runner/GOVERNANCE-ANNEX.md` with the final review record and any deviations found during verification.
- [ ] T129 [P] Create `specs/010-headless-verification-runner/VERIFICATION.md` with the exact verification commands and **placeholders only** for outputs; actuals are recorded by T131–T147, never pre-stated here.
- [ ] T130 [P] Record the RED-gate evidence in `specs/010-headless-verification-runner/red-gate-phase-2.log`, `specs/010-headless-verification-runner/red-gate-phase-3.log`, `specs/010-headless-verification-runner/red-gate-phase-4.log`, and `specs/010-headless-verification-runner/red-gate-phase-5.log`; each log must show the intended failing assertion, not a missing-file error.
- [ ] T131 **Verification — build**: run `dotnet build Stardew-LivingRoots.sln --configuration Release` and record the exact per-project output in `specs/010-headless-verification-runner/verification/build.log`; if `STARDEW_GAME_PATH` or a required assembly is missing, record `BLOCKED` with the exact missing prerequisite. `BLOCKED` is not success and must not be reported as a green Gate 1.
- [ ] T132 **Verification — tests**: run `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` plus `scripts/tests/run-all.sh`; record per-suite counts in `specs/010-headless-verification-runner/verification/test.log`; if the build prerequisite is missing, record `BLOCKED` with the exact missing `STARDEW_GAME_PATH`/assembly. `BLOCKED` is not success.
- [ ] T133 **Verification — formatting**: run `dotnet format Stardew-LivingRoots.sln --verify-no-changes`; record the result in `specs/010-headless-verification-runner/verification/format.log`; if the solution cannot be loaded because the game prerequisite is missing, record `BLOCKED` with the exact missing item. `BLOCKED` is not success.
- [ ] T134 **Verification — shell**: run `bash -n` on every `scripts/**/*.sh` and `shellcheck` where available; record the result in `specs/010-headless-verification-runner/verification/shell.log`.
- [ ] T135 **Verification — schema**: run `scripts/tests/lib/schema-validate.sh` with Python `jsonschema` and `ajv-cli`; record 28/28 valid, 14/14 invalid with one error, 2/2 runner-invalid schema-accepted, identical outcomes, and `evidence-bundle.schema.json` validation in `specs/010-headless-verification-runner/verification/schema.log`.
- [ ] T136 **Verification — launch-free e2e**: run all twelve VS cases — VS-01 through VS-10 plus **VS-09b** and **VS-09c** — from `quickstart.md` against the fake host; record each exit code and bundle verdict in `specs/010-headless-verification-runner/verification/launch-free.log`.
- [ ] T137 **Verification — real-host e2e**: require `gamepath.lock.json` to be populated with the real release ID, asset ID, asset name, archive SHA-256, and per-file digests (no placeholder, no fail-closed stub) before the run; then run VS-01 on a host with `STARDEW_GAME_PATH` and a valid cache/token; capture the SMAPI log, bundle, and `test.md` result in `specs/010-headless-verification-runner/verification/real-host.log`. If no host is available, record `BLOCKED` with the exact missing `STARDEW_GAME_PATH`/assembly; do not claim success.
- [ ] T138 **Verification — second-host 3/3**: on a second Linux host, run the same commit/scenario/save/seed three times and record verdict equality 3/3 plus any `artifacts/anomaly-report.json` in `specs/010-headless-verification-runner/verification/second-host.log`; divergence blocks `machine-pass` until an anomaly report names owner, date, and root cause.
- [ ] T139 **Verification — timed clean-host dry run**: execute the ≤30-minute clean-host procedure and record wall-clock timings in `specs/010-headless-verification-runner/verification/clean-host-dry-run.log`; exceeding 30 minutes is a documented finding, not a silent pass.
- [ ] T140 **Verification — quarterly audit/deletion drill**: run the quarterly durable-store audit (sample ≥30 bundles, 365-day retention, WORM receipt) and the denied-early-deletion drill; record receipts, timestamps, and the audit event in `specs/010-headless-verification-runner/verification/audit-deletion-drill.log`.
- [ ] T141 **Verification — evidence-bundle negatives**: run the existing `scripts/tests/test-bundle-negative.sh` authored in T082 and record the RED/green path and every refusal site in `specs/010-headless-verification-runner/verification/bundle-negative.log`; this task authors no new tests.
- [ ] T142 **Verification — check-docs negatives**: run the negative marker cases authored in T077 and record the missing/duplicate/inverted GATE3 outcomes in `specs/010-headless-verification-runner/verification/check-docs-negative.log`; this task authors no new tests.
- [ ] T143 **Verification — acquisition negatives**: run the redirect/host/mutable/asset/digest cases authored in T065 and record each rejection in `specs/010-headless-verification-runner/verification/acquisition-negative.log`; this task authors no new tests.
- [ ] T144 **Verification — suite negatives**: run the empty/malformed/rerun-cap cases authored in T106 and record the aggregate outcomes in `specs/010-headless-verification-runner/verification/suite-negative.log`; this task authors no new tests.
- [ ] T145 **Verification — farmhand path**: exercise the `debug split` host-guard path authored in T067/T068; record the skip line, absence of mutation, forbidden evidence, and non-promotion in `specs/010-headless-verification-runner/verification/farmhand.log`.
- [ ] T146 **Promotion (machine-pass only)**: only after **T131, T132, T133, T134, T135, T136, T137, T138, T139, T140, T141, T142, T143, T144, and T145** (build, tests, format, shell, schema, launch-free, real-host, second-host 3/3, timed dry run, audit/deletion drill, bundle negatives, check-docs negatives, acquisition negatives, suite negatives, and farmhand) are complete, confirm **the run** wrote `machine-pass` (never `verified`) in `specs/004-playable-compost-loop/test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md` through the single promotion owner `scripts/lib/promotion.sh`; if the runner did not write it, this task fails rather than hand-editing. No promotion may follow schema validation alone.
- [ ] T147 **Gate 3 evidence only**: assemble `specs/010-headless-verification-runner/gate3-evidence/` with the observation notes, artifact comparison (before/after save JSON), SMAPI log, session date, and bundle pointer produced by the real-host run; this task records evidence and does not promote.
- [ ] T148 **Human E attestation (the ONLY task that writes `verified`)**: a maintainer reviews the clean non-flaky `machine-pass`, the 3/3 second-host result, and `gate3-evidence/`, then records `verified` with reviewer, date, and session in `specs/004-playable-compost-loop/test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md`; the runner and CI never write `verified`, and no other task may.
- [ ] T149 **Verification — checklist/traceability audit**: re-run one reviewer per checklist, confirm every `[x]` still holds, reopen any false green, and update `specs/010-headless-verification-runner/traceability.md` with actual test/code locations.
- [ ] T150 **Final commit**: commit `specs/010-headless-verification-runner/VERIFICATION.md`, `specs/010-headless-verification-runner/traceability.md`, `specs/010-headless-verification-runner/red-gate-phase-2.log`, `specs/010-headless-verification-runner/red-gate-phase-3.log`, `specs/010-headless-verification-runner/red-gate-phase-4.log`, `specs/010-headless-verification-runner/red-gate-phase-5.log`, every artifact under `specs/010-headless-verification-runner/verification/` (`build.log`, `test.log`, `format.log`, `shell.log`, `schema.log`, `launch-free.log`, `real-host.log`, `second-host.log`, `clean-host-dry-run.log`, `audit-deletion-drill.log`, `bundle-negative.log`, `check-docs-negative.log`, `acquisition-negative.log`, `suite-negative.log`, `farmhand.log`, `us1-checkpoint.log`), `specs/010-headless-verification-runner/gate3-evidence/`, and the three GATE3 blocks in `specs/004-playable-compost-loop/test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md` with `docs(010): ...`; list every artifact in the commit body and **do not amend** earlier commits.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Scaffold (Phase 1)**: starts immediately; T001 → T004 → T007 is the project-scaffold chain; T002/T003/T005/T006/T008–T018 are `[P]` after their prerequisite scaffold.
- **Foundational (Phase 2)**: depends on Phase 1; T043 RED gate blocks T044–T061e; T062 blocks all user stories. T061b delivers standalone suite discovery for T099 and US2.
- **US1 (Phase 3)**: depends on Phase 2 T062; T083 RED gate blocks T084–T104; T099 is the single orchestrator owner.
- **US2 (Phase 4)**: depends on US1 T105; T109 RED gate blocks T110–T114; T110 is **scenario-only** (005 directory-suite fixture + zero-runner-source assertion) and serializes after T105; suite discovery is owned by Phase 2 T061b.
- **US3 (Phase 5)**: depends on US2 T115; T119 RED gate blocks T120–T123; T120 is the only other orchestrator owner and serializes after T099 and US2 T115.
- **Verification (Phase 6)**: depends on US1+US2+US3; T131–T150 are the only attestation tasks. T131–T145 complete before **any** promotion (T146); T146 completes before the human E attestation (T148); T150 is the final commit.

### User Story Dependencies

- **US1 (P1)**: MVP; no dependency on US2/US3.
- **US2 (P2)**: reuses the US1 runner and the Phase 2 `scripts/lib/suite.sh`; edits no runner source; T110 adds only the 005 directory-suite fixture/scenario and asserts the US2 diff has zero runner-source changes.
- **US3 (P3)**: reuses the US1/US2 runner; adds CI mode/reporters/workflow and the serialized `scripts/verify-scenario.sh` wiring after US1 (T099 → T120).

### Within Each Phase

1. All test tasks complete.
2. RED-gate task records failing output in `red-gate-<phase>.log`; no implementation before it.
3. Implementation tasks in dependency order (libraries before the orchestrator; models before writers).
4. Phase checkpoint before the next priority.

---

## Parallel Execution Waves (subagent-driven)

Use one subagent per context; reuse it for files in that context. Every wave waits for the previous wave’s shared-tooling command to finish.

### Wave 0 — Scaffold (sequential project chain, parallel contracts)
T001 → T004 → T007. After T001/T004, run T002, T003, T005, T006, T008, T009, T010, T011, T012, T013, T014, T015, T016, T017, T018 in parallel (`[P]`).

### Wave 1 — Foundational tests (`[P]`, 4 subagents)
- Agent A (C# domain/API): T019–T022 and T033.
- Agent B (C# evaluators/dump/security): T023–T032.
- Agent C (bash constants/locks/sanitize/schema): T034–T039.
- Agent D (run-all/retention/registries): T040–T042.
Then **single agent** runs T043 RED gate.

### Wave 2 — Foundational implementation (`[P]`, 4 subagents)
- Agent A (mod API): T044–T048.
- Agent B (helper validation/evaluation/extraction/manifest): T049–T053.
- Agent C (bash common/atomic/concurrency/logging/sanitize): T054–T058.
- Agent D (version/validation/retention/suite/statistics): T059–T061e.
Then **single agent** runs T062 checkpoint.

### Wave 3 — US1 tests (`[P]`, 4 subagents)
- Agent A (CLI/game-path/acquisition/display/launch): T063–T066.
- Agent B (injection/signals/evaluation/verdict/evidence): T067–T071.
- Agent C (promotion/save/cleanup/flaky/quarantine): T072–T076.
- Agent D (docs/extraction/stress/performance/redaction/bundle-negative): T077–T082.
Then **single agent** runs T083 RED gate.

### Wave 4 — US1 implementation
- Wave 4a `[P]` (3 subagents): T084–T098 split by `scripts/lib/` domain; T100–T102 verification mod.
- Wave 4b (single agent, shared `scripts/verify-scenario.sh`): T099, T104.
- Wave 4c (single agent, shared verifier): T103.
Then **single agent** runs T105 checkpoint.

### Wave 5 — US2 (scenario-only, serialized after US1)
- Agent E owns T106–T115 as scenario/docs/fixture work only. No runner source and no orchestrator edit: suite discovery was delivered in Phase 2 by T061b, and T110 only adds the 005 directory-suite fixture and asserts the zero-runner-source diff.

### Wave 6 — US3 (serialized after US2)
- Agent F owns T116–T124; the same agent owns `scripts/verify-scenario.sh` at T120 and does not run concurrently with Agent E.

### Wave 7 — Verification (serialized shared tooling)
- T125–T130 documentation/evidence may be `[P]` by disjoint files, but only one agent runs `dotnet test`/`run-all`/validators/game at a time.
- T131–T145 run sequentially in the order listed; T137/T138/T139/T140 must complete before T146.
- T146 (machine-pass confirmation) → T147 (Gate 3 evidence) → T148 (human E `verified`, the only writer) → T149 (audit) → T150 (final commit, no amend).

---

## Parallel Example: US1 tests

```bash
# Wave 3, one agent per disjoint file group:
Task: "CLI/usage tests in scripts/tests/test-verify-cli.sh"          # T063
Task: "Game-path/skip-build tests in scripts/tests/test-game-path.sh" # T064
Task: "Acquisition/cache tests in scripts/tests/test-acquisition.sh" # T065
Task: "Display/launch tests in scripts/tests/test-display-launch.sh" # T066
# ... same wave, different files; no two agents edit the same test file.
```

## Parallel Example: US1 implementation libraries

```bash
# Wave 4a, disjoint scripts/lib files:
Task: "Implement scripts/lib/acquisition.sh"  # T084
Task: "Implement scripts/lib/preflight.sh"    # T085
Task: "Implement scripts/lib/display.sh"      # T086
# The orchestrator scripts/verify-scenario.sh (T099) waits for all libraries.
```

## Parallel Example: US2

```bash
# Wave 5, one agent owns US2; these files are disjoint and scenario-only:
Task: "Add the feature-005 directory-suite fixture + zero-runner-source assertion" # T110 (no orchestrator edit)
Task: "Add specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json" # T111
Task: "Add specs/010-headless-verification-runner/SCENARIO-AUTHORING.md" # T112
# T113/T114 may run in parallel with T111/T112; T115 waits for all.
```

## Parallel Example: US3

```bash
# Wave 6, one agent owns US3; these files are disjoint:
Task: "Implement scripts/lib/ci.sh"                 # T120 (same orchestrator owner, after US2)
Task: "Implement scripts/lib/reporters.sh"          # T121
Task: "Add .github/workflows/verify-scenario.yml"   # T122
# T123 updates GOVERNANCE-ANNEX.md concurrently; T124 waits for all.
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 scaffold.
2. Complete Phase 2 with the RED gate and checkpoint.
3. Complete US1 tests → RED gate → implementation → checkpoint.
4. Run the launch-free VS-01; only then consider real-host execution.
5. Do not start US2 until US1’s checkpoint is green; do not start US3 until US2’s checkpoint is green.

### Incremental Delivery

1. US1: unattended single-scenario verification (MVP).
2. US2: second feature-005 scenario + directory-suite fixture, zero runner-source changes (suite discovery is Phase 2 T061b).
3. US3: CI mode, reporters, self-hosted workflow.
4. Phase 6 verification attests the whole feature; implementation agents never do.

### Parallel Team / Subagent Strategy

1. One subagent owns one context (C# domain, bash libs, schema/version, CI).
2. Reuse the same subagent for tasks in the same context; do not spawn a new agent per file.
3. `[P]` tasks are issued in waves; non-`[P]` tasks wait.
4. Shared long-running tooling is queued: one `dotnet test`, one `run-all.sh`, one validator run, one game launch at a time.
5. `scripts/verify-scenario.sh` has one owner at a time: T099 (US1) → T120 (US3); never parallelize those. US2 (T110) is scenario-only and never edits it.
6. Never kill or interrupt another agent’s process; wait for it to finish.

---

## Appendix A — Data-model constraints to enforce verbatim

These are quoted from `data-model.md` and `contracts/scenario.schema.json`; T060 and the evaluator/writer tasks must enforce them without reinterpretation.

| Field / rule | Constraint |
|---|---|
| `schemaVersion` | const `2`; unsupported version exits `14` |
| `$id` | `urn:lfgranja:livingroots:scenario:v2`; logical URN, not a file path |
| Object strictness | `additionalProperties: false` at every object level |
| Root required | `schemaVersion`, `scenarioId`, `featureSlug`, `testMdPath`, `summary`, `playerRole`, `minimumGameVersion`, `environment`, `milestoneSave`, `initCommands`, `steps`, `thresholds`, `expectedSignals`, `rngSeed`, `time`, `determinism`, `scenarioKind` |
| `environment` required | `gameVersion`, `smapiVersion`, `modVersion`, `apiVersion`, `saveId`, `config` |
| `determinism` required | `idempotent` (const true), `declaredSources` |
| `summary` | `type: string`, `minLength: 1`, `maxLength: 500`; null is never valid |
| `steps` | `minItems: 1`, `maxItems: 5`; array order is the single source of truth (no `dependsOn`) |
| `steps[].inputs` | `insertWaste` requires `itemId`+`count`; `setSoilHealth` requires `soilHealth`; `runMacro` requires `macroFile` |
| `hostGuard` (farmhand only) | required when `playerRole=farmhand`, forbidden when `playerRole=host`: `skipLine` (const) and `forbiddenEvidence` (`elapsedTrace`, `phaseDaysMutation`); no `requireSkipLine` toggle |
| `thresholds[].subject` | `minProperties: 1` if present; must match exactly one record; forbidden for `smapiLog`; zero/multiple ⇒ exit `14` |
| `expectedSignals` | exactly the three unique values `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`; copied into `manifest.json` |
| `scenarioKind` | required for every scenario: `positive`, `negative`, `boundary` |
| `expectedVerdict` | positive: only `pass` (optional, default `pass`); boundary: mandatory `pass`/`fail`; negative: mandatory `fail`/`errored`/`partial`/`skipped` |
| `healthTier` | `unknown`, `nonParticipant`, `poor` (`0 < health < 34`), `moderate` (`[34,67)`), `healthy` (`>=67`) |
| `soilHealthState` | `measured`, `missing`, `nan`; distinguishes NaN from missing |
| `cmp` | `eq`: `|left-right| <= tolerance`; `ne`: `|left-right| > tolerance`; ordering ignores tolerance |
| `between` | `effectiveMin = min - tolerance`, `effectiveMax = max + tolerance`; inclusive/exclusive applies to effective bounds; `min <= max` |
| `multiplier` | `computed = clamp(round(base × factor), fieldMinimum)`; `phaseDays` minimum 1, other numeric fields 0; 99999 never multiplied/clamped |
| `sentinel` | `SENTINEL_UNTOUCHED_99999`→`phaseDays`; `NAN_HEALTH`/`MISSING_KEY`/`HEALTH_LE_ZERO`/`POOR_POSITIVE_LT_34`→`healthTier`; `PHASE_MINUS_ONE`→`currentPhase`; `mustNotChange` asserts both captures |
| `unchanged` | numeric only; `|post-pre| <= tolerance` (default 0) |
| Unmeasured actual | any numeric predicate on absent/`"NaN"` fails with `unmeasured-actual`; only `sentinel` asserts unmeasured/missing |
| Verdict | closed set `{pass,fail,errored,partial,skipped}` + derived `flaky`; exit `0/18/19/20/21` per contract; `22` reserved and never emitted |
| Missing artifact split | missing signal ⇒ `partial`/19; missing manifest/claims/anchor/per-attempt ⇒ `errored`/19 |
| Promotion | machine writes `machine-pass` only on clean non-flaky `pass` + three agreeing signals + honesty ≥ R + no open anomaly; human E writes `verified`; farmhand/`debug split` never `machine-pass` |
| Manifest | single producer `ManifestBuilder`; mandatory `manifest.sha256`; `subject` is canonical; `files` deprecated alias with `{name,digest:{sha256}}`; per-attempt manifests listed in top-level `subject`, `attempts[]` only holds `attempts/<n>/attempt.json`; warmup is not an attempt |
| Retention | audit/evidence ≥365 days, application logs ≥30 days, durable-store receipt, WORM/append-only, approval-gated deletion |

---

## Notes

- Tests are not optional for this feature; every RED gate is a hard block, and each gate writes `specs/010-headless-verification-runner/red-gate-<phase>.log`.
- Renumbering reconciliation: the former build/test/format tasks T110/T111/T112 are now T131/T132/T133, and each records `BLOCKED` with the exact missing `STARDEW_GAME_PATH`/assembly. The former duplicate promotion tasks T108/T109/T117 are removed; the new T108/T109 are the US2 zero-core-change test and RED gate, and T117 is the US3 reporter test. The only promotion tasks are T146 (confirm the run wrote `machine-pass`) and T148 (the sole human E writer of `verified`).
- `[P]` means different files and no incomplete dependency; never parallelize edits to `scripts/verify-scenario.sh` or any shared test harness.
- The runner writes `machine-pass`; only the human E task T148 writes `verified`. A green implementation checkpoint is not an attestation.
- `BLOCKED` at T131/T132/T133 means the exact `STARDEW_GAME_PATH`/assembly prerequisite is missing; `BLOCKED` is never recorded as a success or green Gate 1.
- Negative/security suites are authored in the owning phase (T028, T034b, T034c, T065, T067, T068, T077, T082, T106) and only executed in Phase 6; Phase 6 must not add new `test-*.sh` files.
- If a real-host prerequisite is missing at T137, record `BLOCKED` with the exact missing item; never present a launch-free run as real gameplay evidence.
- Commit after every task; keep verification evidence and Gate 3 records in the same commit as the run that produced them. T150 is the final commit and must not amend earlier commits.
