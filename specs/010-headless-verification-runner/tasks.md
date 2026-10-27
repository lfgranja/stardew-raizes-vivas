---
description: "Task list for the Headless Game Verification Runner"
---

# Tasks: Headless Game Verification Runner

**Input**: Design documents from `/specs/010-headless-verification-runner/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `.specify/memory/constitution.md`

**Tests**: REQUIRED. The user explicitly requested TDD, property-based testing where applicable, and a blocking RED phase before implementation. Every user-story phase therefore starts with test tasks and a RED-gate task; implementation tasks MUST NOT start until the gate passes.

**Organization**: Tasks are grouped by user story. US1 is the MVP; US2 and US3 build on the US1 runner but touch disjoint files and may run in parallel after US1.

## Execution Rules (apply to every task)

- **Scaffold first (user rule 0)**: if a file can be produced by a template, `dotnet new`, an existing project file, or an existing script substrate, scaffold it first and only then edit by hand. T001–T005 are the scaffold tasks; do not hand-write those files before their scaffold step.
- **TDD / RED gate (user rule 1)**: for every story, all test tasks complete first, then the RED-gate task records the failing output. No implementation task in that story may start before the RED gate. Property-based tests are mandatory for the listed invariants.
- **Parallel waves (user rule 2)**: `[P]` means different files and no dependency on an incomplete task. Use the wave plan below. A subagent that owns a context (for example all `scripts/lib/*.sh`) MUST be reused for that context rather than spawning a new agent per file.
- **Shared tooling is serialized (user rule)**: only one agent at a time may run `dotnet build`, `dotnet test`, `dotnet format`, `scripts/tests/run-all.sh`, `npx ajv-cli`, `check-jsonschema`, Xvfb, the game, or any long-running test/build command. Other agents queue and wait; never kill or interrupt a command started by another agent, thread, or process.
- **Verification-only attestation (user rule 3)**: implementation agents MUST NOT claim success, completeness, or “green.” Only the verification tasks T110–T118 attest to the implementation. A task is complete only when its own tests pass and its atomic commit exists.
- **Deterministic commit (user rule 4)**: after every task that edits files, run `git add <paths> && git commit -m "<type>(010): <summary> — <files> — <why>"`. Do not batch edits from different tasks into one commit. Never use `--no-verify`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no incomplete dependency)
- **[Story]**: `[US1]`, `[US2]`, `[US3]` for user-story tasks only
- Every task names its exact file path

## Path Conventions

Repository root is `/home/luis/development/stardew-raizes-vivas`. Production mod code lives in `LivingRoots/`; the dev-only verification mod in `LivingRoots.Verification/`; the runner and its libraries in `scripts/`; C# tests in `LivingRoots.Tests/Verification/`; bash tests in `scripts/tests/`; contracts and fixtures in `specs/010-headless-verification-runner/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the new projects/scripts from templates and add the version-controlled contracts. No business logic yet.

- [ ] T001 Scaffold the verification mod with the SDK template (`dotnet new classlib -n LivingRoots.Verification`) and only then edit the generated `LivingRoots.Verification/LivingRoots.Verification.csproj` to `net6.0`, `Nullable=enable`, `LangVersion=latest`, `CheckForOverflowUnderflow=true`, `AppendTargetFrameworkToOutputPath=false`, and `Pathoschild.Stardew.ModBuildConfig 4.4.0`; record the template command in the commit body.
- [ ] T002 [P] Scaffold `LivingRoots.Verification/manifest.json` from the existing `LivingRoots/manifest.json` template; set the unique ID `lfgranja.LivingRoots.Verification`, `Dependencies` on `lfgranja.LivingRoots`, no update keys, and no release-zip inclusion.
- [ ] T003 [P] Scaffold `LivingRoots.Verification/ModEntry.cs` from the SMAPI `Mod` template with an empty `Entry(IModHelper)`; no dump logic yet (implementation is T073/T074).
- [ ] T004 Scaffold `scripts/verify-scenario.sh` by copying the existing `scripts/smoke-validation.sh` substrate (header, argument parser, logging, process-group, cleanup, exit-code rendering) and pruning smoke-only logic; leave an explicit `TODO` for the US1 orchestration body.
- [ ] T005 Scaffold `scripts/verify-evidence-bundle.sh` from `scripts/check-protected-paths.sh` plus `scripts/tests/lib/harness.sh` conventions (strict mode, `--help`, usage errors, exit-code rendering); leave verifier logic for T076.
- [ ] T006 [P] Add `specs/010-headless-verification-runner/compat-matrix.json` with the pinned Stardew Valley 1.6.x line, matching SMAPI 4.4.0+, minimum `1.6.14`, and a `failureClasses` map for the retryable/non-retryable exit codes (C-REL-06).
- [ ] T007 [P] Add `specs/010-headless-verification-runner/gamepath.lock.json` with `releaseId`, `assetId`, `assetName`, archive SHA-256, and the mandatory per-file digest map; values come from the private builddeps release and MUST be reviewed; if unavailable, ship a fail-closed placeholder that makes the runner exit `19` rather than executing an unverified tree.
- [ ] T008 [P] Add `specs/010-headless-verification-runner/quarantine.json` as an empty registry (`{"schemaVersion":1,"entries":[]}`) with the status vocabulary `candidate|quarantined|disabled|retired`.
- [ ] T009 [P] Add `scripts/tests/lib/runner-harness.sh` extending `scripts/tests/lib/harness.sh` with fake Xvfb, fake SMAPI log, fake game install, fake milestone save, and a `run_verify_scenario` helper that always isolates `HOME`/`XDG_*`.
- [ ] T010 [P] Add `scripts/tests/lib/schema-validate.sh` wrapping Python `jsonschema` and `ajv-cli` so every schema test runs both validators and asserts identical accept/reject outcomes (C-SCN-38).
- [ ] T011 [P] Update `scripts/tests/run-all.sh` to discover and run the new `test-*.sh` suites, fail on any non-zero suite, and print a per-suite summary; no new logic beyond discovery.

**Checkpoint**: Scaffolds and contracts exist; no runner behavior yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain API, shared evaluators, persistence/security primitives, and shared bash libraries that every user story uses.

**⚠️ CRITICAL**: No user-story implementation may start until the Phase 2 RED gate (T025) has run and the Phase 2 checkpoint (T039) is green.

### Tests for Phase 2 (write FIRST)

- [ ] T012 [P] xUnit contract tests for `ILivingRootsModApi` in `LivingRoots.Tests/Verification/ModApiContractTests.cs`: event is raised on the game thread, `SoilGrowthModifierCompletedEventArgs` carries `DayKey`, `IsMainPlayer`, `LocationsProcessed`, `ElapsedMilliseconds`, `Success`, and subscriber exceptions are isolated.
- [ ] T013 [P] FsCheck property tests for the predicate evaluator in `LivingRoots.Tests/Verification/PredicateEvaluatorPropertyTests.cs`: `cmp` eq/ne tolerance truth table, `between` effective bounds, `multiplier` rounding/clamping, `unchanged`, sentinel mapping, `allOf`/`anyOf`/`not`, and unmeasured-actual ⇒ `fail` with `unmeasured-actual`.
- [ ] T014 [P] FsCheck property tests for `SaveComparator` in `LivingRoots.Tests/Verification/SaveComparatorPropertyTests.cs`: `0.0001f` tolerance, missing key = 0 only at the persistence layer, NaN skipped, `added`/`removed`/`changed` are complete and non-overlapping.
- [ ] T015 [P] FsCheck property tests for path sanitization in `LivingRoots.Tests/Verification/SanitizerPropertyTests.cs`: NFC normalization, `..`, absolute paths, drive letters, backslashes, NUL/control characters, symlink escapes, and homoglyph-confusable segments are rejected; valid vectors round-trip.
- [ ] T016 [P] FsCheck property tests for the manifest digest/anchor in `LivingRoots.Tests/Verification/ManifestDigestPropertyTests.cs`: every subject entry is hashed, `manifest.sha256` covers the finalized manifest, a single changed byte changes the digest, and `files` is accepted only as the deprecated alias.
- [ ] T017 [P] FsCheck property tests for the verdict state machine in `LivingRoots.Tests/Verification/VerdictStateMachinePropertyTests.cs`: closed five-base-verdict set, `flaky` never a base verdict, `19 > 18 > 21 > 20 > 0` aggregation, expected-verdict match/mismatch, and no transition from flaky/quarantined to `verified`.
- [ ] T018 [P] Bash behavioral tests for shared constants/exit codes in `scripts/tests/test-common.sh`: code `0`, `9`, `10`, `12`, `13`, `14`, `18`, `19`, `20`, `21`; code `8` retired; code `11` inherited but never emitted; unknown flag exits `14`.
- [ ] T019 [P] Bash behavioral tests for atomic writes in `scripts/tests/test-atomic.sh`: temp-in-target-dir, data `fsync`, rename, parent `fsync`, `EXDEV` is an internal error, target untouched on failure, temp unlinked on failure.
- [ ] T020 [P] Bash behavioral tests for lock/claim in `scripts/tests/test-lock.sh`: advisory locks only, bounded backoff, global order `game-cache → build-output → shared-doc`, `O_EXCL|O_CREAT|O_NOFOLLOW`, live-token conflict, stale reclaim guard, and no signal without token match.
- [ ] T021 [P] Bash behavioral tests for bash/C# sanitizer parity in `scripts/tests/test-sanitize.sh` using `scripts/tests/fixtures/sanitize-vectors.tsv`; both implementations must produce identical accept/reject results.
- [ ] T022 [P] Bash contract tests for the scenario schema in `scripts/tests/test-schema-contract.sh`: all `fixtures/valid/*.json` accepted, all `fixtures/invalid/*.json` rejected with exactly one error, all `fixtures/runner-invalid/*.json` schema-accepted but rejected by the runner's cross-field checks.
- [ ] T023 [P] Bash behavioral tests for the version gate in `scripts/tests/test-version-gate.sh`: SMAPI `SemanticVersion` ordering (`1.11 < 1.2`), `-unofficial.*`, HARD-FAIL/WARN/INFO tiers, null `MaximumGameVersion` as an upper-bound deviation, and no Xvfb launch on HARD-FAIL.
- [ ] T024 [P] Bash behavioral tests for exit-code mapping in `scripts/tests/test-exit-codes.sh`: pass/flaky `0`, fail/matched-fail `18`, errored/partial/mismatch `19`, no-tests/all-skipped `20`, policy/drift/overreach `21`, integrity carve-out for `--ignore-exit-code`.
- [ ] T025 **RED gate (Phase 2)**: run `scripts/tests/run-all.sh` plus `dotnet test` and record the failing output in the commit body; every test file above must exist and fail for the intended reason before T026+ start. Do not implement yet.

### Implementation for Phase 2

- [ ] T026 [P] Add `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs` with `ApiVersion`, `SoilGrowthModifierCompleted`, and `SoilGrowthModifierCompletedEventArgs`; keep it SMAPI-free.
- [ ] T027 [P] Modify `LivingRoots/Controllers/ModController.cs` to raise `SoilGrowthModifierCompleted` synchronously on the game thread at the tail of `OnDayEnding`, after `ApplySoilGrowthModifier` for all locations (success or caught exception), before the day transition; isolate subscriber exceptions.
- [ ] T028 [P] Implement `LivingRoots.Verification/Evaluation/PredicateEvaluator.cs` for `cmp`, `between`, `multiplier`, `sentinel`, `unchanged`, `allOf`, `anyOf`, `not`; enforce unmeasured-actual ⇒ `fail`, multiplier field minima (`phaseDays` 1, all other numeric fields 0), and 99999 never multiplied/clamped.
- [ ] T029 [P] Implement `LivingRoots.Verification/Evaluation/SaveComparator.cs` with `0.0001f` tolerance, missing key = 0 only at the persistence layer, NaN ignored, and `added`/`removed`/`changed` output.
- [ ] T030 [P] Implement `LivingRoots.Verification/Security/Sanitizer.cs` with NFC normalization, traversal/drive/backslash/NUL/control rejection, symlink-escape rejection, and homoglyph-confusable detection; C# is authoritative for mod data.
- [ ] T031 [P] Implement `scripts/lib/sanitize.sh` with the same contract as T030 for runner-owned inputs; expose one normalized repo-relative path or fail with exit `14`/`19` as appropriate; consume the shared `sanitize-vectors.tsv`.
- [ ] T032 [P] Implement `LivingRoots.Verification/Evidence/ManifestBuilder.cs` producing `manifest.json` with required fields (`schemaVersion`, `runId`, `timestamp`, `toolVersions`, `gameVersion`, `modVersion`, `environment`, `expectedSignals`, `subject`, `verdict`, `honestyLevel`, `attempts`) and a detached `manifest.sha256` written after finalization.
- [ ] T033 [P] Implement `scripts/lib/common.sh` with all runner constants and the exit-code enum from `contracts/exit-codes.md`; no magic numbers elsewhere.
- [ ] T034 [P] Implement `scripts/lib/atomic.sh` (`atomic_write`, `fsync_dir`, temp cleanup) per C-CON-04/C-CON-13/C-CON-14.
- [ ] T035 [P] Implement `scripts/lib/concurrency.sh` (`lock_acquire`, `claim_acquire`, `pid_identity`, `orphan_registry`) with the C-CON-08/C-CON-09/C-CON-22/C-CON-23 contracts.
- [ ] T036 [P] Implement `scripts/lib/logging.sh` with JSON Lines audit events, application logs, and pre-write redaction for success/failure/CI/JUnit/Allure sinks.
- [ ] T037 [P] Implement `scripts/lib/version-gate.sh` parsing `Stardew Valley.dll`, `StardewModdingAPI.dll`, and `manifest.json` with SMAPI `SemanticVersion`, then classifying against `compat-matrix.json` (HARD-FAIL/WARN/INFO).
- [ ] T038 [P] Implement `scripts/lib/scenario-validate.sh` invoking the .NET helper for Draft 2020-12 validation and enforcing the cross-field rules: `between.min <= between.max`; `multiplier.base` suffix equals `field`; sentinel-to-field mapping; signal-to-field mapping; action-to-required-input; `subject` rules; `expectedVerdict` rules; exit `14` with a field path on failure.
- [ ] T039 **Phase 2 checkpoint**: run the Phase 2 suites once via `scripts/tests/run-all.sh` (queued, single agent) and confirm green; commit `feat(010): ...`.

**Checkpoint**: Foundation ready; user stories can begin.

---

## Phase 3: User Story 1 - Maintainer runs full verification unattended (Priority: P1) 🎯 MVP

**Goal**: One command acquires/reuses game files, launches Stardew under Xvfb + SMAPI, injects the scenario, captures all three signals, evaluates thresholds, writes the evidence bundle, and promotes `test.md` only on a clean non-flaky `pass`.

**Independent Test**: On a host with `STARDEW_GAME_PATH` and either `BUILDDEPS_TOKEN` or a valid cache, run `./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/valid/compost-growth.scenario.json`; expect exit `0`, three signal artifacts, a valid bundle, and `test.md` promoted. With a tampered threshold, expect exit `18` and no promotion.

### Tests for US1 (write FIRST)

- [ ] T040 [P] [US1] CLI/usage tests in `scripts/tests/test-verify-cli.sh`: required/unknown flags, `--scenario` file/dir, `--no-acquire`, `--force-refresh` incompatibility, `--override` allow/deny list, `--ignore-exit-code` integrity carve-out, `--verify-bundle`, `--check-docs`.
- [ ] T041 [P] [US1] Acquisition/cache tests in `scripts/tests/test-acquisition.sh`: valid cache with no token/network, cache miss with token, `--no-acquire` cache miss exits `9`, checksum mismatch exits `19` with `integrity-failure.json`, 401/403/404 terminal, 5xx/429 retry, immutable asset pinning.
- [ ] T042 [P] [US1] Display/launch tests in `scripts/tests/test-display-launch.sh`: `-displayfd` allocation, no `/tmp/.X*-lock` probe, ready-state sentinel + inode change, `Loaded N mods:`, skipped-mod detection, `GameLaunched` marker, display failure exit `10`.
- [ ] T043 [P] [US1] Scenario injection tests in `scripts/tests/test-injection.sh`: typed `initCommands`, `/`-prefixed macro lines, `.txt` extension, no nested macros, `debug runmacro`, console commands, no `debug ` prefix, `/sleep` last, completion markers.
- [ ] T044 [P] [US1] Signal capture tests in `scripts/tests/test-signals.sh`: three independent channels, dump written after `SoilGrowthModifierCompleted`, `.done` sentinel last, save-file cross-check, divergence ⇒ `partial`, farmhand absence of mutation + skip line.
- [ ] T045 [P] [US1] Threshold/evaluation tests in `scripts/tests/test-evaluation.sh`: 33/34/66/67 ⇒ 1.15/1.00/1.00/0.85, `phaseDays >= 1`, 99999 untouched, missing/NaN/≤0 non-participants, `healthTier` five values, `subject` selection, unmeasured-actual.
- [ ] T046 [P] [US1] Evidence-bundle tests in `scripts/tests/test-evidence.sh`: `manifest.json`, mandatory `manifest.sha256`, `claims.json`, `artifacts/`, `junit.xml`, `allure.json`, `subject` digests, `attempts/<n>/attempt.json`, `integrity-failure.json` on integrity failure, offline verifier refusal sites.
- [ ] T047 [P] [US1] Promotion tests in `scripts/tests/test-promotion.sh`: `partial → verified` only on clean non-flaky `pass` + three agreeing signals + R; flaky pass stays `partial`; `fail`/`errored`/`partial`/`skipped` never promote; marker replacement is byte-preserving outside the block; missing/duplicate/inverted markers exit `14`.
- [ ] T048 [P] [US1] Save-preservation tests in `scripts/tests/test-save-preservation.sh`: player save never opened; disposable copy; source hash unchanged; restored copy matches source; mismatch exit `19`; sidecar backup and atomic restore.
- [ ] T049 [P] [US1] Cleanup/orphan tests in `scripts/tests/test-cleanup.sh`: process-group SIGTERM→SIGKILL→`waitpid`; PGID-reuse guard; owned-display socket removal; orphan registry report-only; temp cleanup preserves exit code; `cleanup_failed` recorded.
- [ ] T050 [P] [US1] Retry/flaky tests in `scripts/tests/test-flaky.sh`: only classified transients retry; 2 retries / 3 attempts; 5 s cooldown; per-attempt artifacts; flaky = attempt-1 failure + later pass on same commit/seed/environment; flaky exits `0` and never promotes; confirmed-flaky N ≥ 30 Wilson.
- [ ] T051 [P] [US1] Quarantine tests in `scripts/tests/test-quarantine.sh`: 2 flaky runs ⇒ candidate; human approval; 14-day box + one 14-day extension; 28-day auto-disable; retirement is human-approved; quarantined reports `<skipped>`; shadow-pass re-enablement.
- [ ] T052 [P] [US1] Docs-drift tests in `scripts/tests/test-docs.sh`: `--check-docs` `0`/`21`/`14`; marker-only replacement; idempotency; timestamps only on state change; root docs never updated on flaky pass.
- [ ] T053 [US1] **RED gate (US1)**: run all US1 test files via `scripts/tests/run-all.sh` and record the failing output; no US1 implementation may start until every test file exists and fails for the intended reason.

### Implementation for US1

- [ ] T054 [P] [US1] Implement `scripts/lib/acquisition.sh`: cache validation, `--no-acquire`, `--force-refresh`, GitHub Releases asset download with `Authorization` header, `.part` + atomic rename, SHA-256 + per-file digest verification, no token in argv/URLs/logs, `integrity-failure.json`.
- [ ] T055 [P] [US1] Implement `scripts/lib/preflight.sh`: host tools, `STARDEW_GAME_PATH` unset/invalid/valid branches, six required assemblies, deployment check, non-root guard, version-matrix gate before Xvfb.
- [ ] T056 [P] [US1] Implement `scripts/lib/display.sh`: `-displayfd`, per-instance display, bounded cleanup (ESRCH/ENOENT/connect/no-zombie), owned-token force-unlink only, orphan registry report-only.
- [ ] T057 [P] [US1] Implement `scripts/lib/save.sh`: per-instance `mktemp -d`, `XDG_CONFIG_HOME`/`HOME`/`XDG_*`, milestone copy, sidecar backup, hash-before/after, idempotent atomic restore.
- [ ] T058 [P] [US1] Implement `scripts/lib/launch.sh`: SMAPI launch env (`SMAPI_NO_TERMINAL`, software GL, SDL/audio vars), log-sentinel + inode freshness, `Loaded N mods:`, skipped-mod check, `GameLaunched` marker, two-phase timeout.
- [ ] T059 [P] [US1] Implement `scripts/lib/injection.sh`: typed `initCommands`, macro validation (UTF-8/BOM, `/` lines, `.txt`), `debug runmacro`, console commands, `/sleep` ordering, completion markers, no nested macros.
- [ ] T060 [P] [US1] Implement `scripts/lib/signals.sh`: SoilHealthState delta, in-process Crop dump + save cross-check, SMAPI log excerpt, divergence ⇒ `partial`, mandatory `expectedSignals` presence.
- [ ] T061 [P] [US1] Implement `scripts/lib/verdict.sh`: five base verdicts + derived `flaky`, expected-verdict match/mismatch, host-guard breach, suite aggregation `19 > 18 > 21 > 20 > 0`, exit mapping.
- [ ] T062 [P] [US1] Implement `scripts/lib/evidence.sh`: run-id format, bundle layout, `manifest.json` + `claims.json` + `artifacts/`, `manifest.sha256`, per-attempt manifests, append-only `attempts[]`, `integrity-failure.json`.
- [ ] T063 [P] [US1] Implement `scripts/lib/promotion.sh`: GATE3 marker parsing, byte-preserving replacement, idempotency check, `partial`/`verified` transitions, root-doc updates only on clean pass, `--check-docs`.
- [ ] T064 [P] [US1] Implement `scripts/lib/cleanup.sh`: trap/finally, process-group teardown, `waitpid`, temp/display cleanup, orphan report, exit-code preservation, `cleanup_failed`.
- [ ] T065 [P] [US1] Implement `scripts/lib/retry.sh`: transient classification from `compat-matrix.json`, 3 attempts, 5 s cooldown, per-attempt artifacts, flaky derivation, suite rerun cap.
- [ ] T066 [P] [US1] Implement `scripts/lib/quarantine.sh`: candidate detection, human approval gate, 14/28-day time-box, auto-disable, retirement, shadow-pass counting, `<skipped>` reporting.
- [ ] T067 [P] [US1] Implement `scripts/lib/performance.sh`: one discarded warmup, per-location ≤ 5 ms, young-generation collection delta, `gc-attributed` tag.
- [ ] T068 [US1] Wire `scripts/verify-scenario.sh` to T054–T067 in the required order (preflight → acquisition → version gate → isolate → launch → inject → capture → evaluate → evidence → promote → cleanup); include `--ci`, `--verify-bundle`, `--check-docs`, `--override`, `--milestone-save`.
- [ ] T069 [P] [US1] Implement `LivingRoots.Verification/ModEntry.cs`: subscribe to `ILivingRootsModApi` in `GameLaunched`, capture at `DayEnding` after the event, invoke the dump writer, isolate exceptions, unsubscribe on dispose.
- [ ] T070 [P] [US1] Implement `LivingRoots.Verification/Evidence/CropPhaseDumpWriter.cs`: temp file → fsync → atomic rename → parent fsync → `.done` sentinel last; delete stale sentinel at capture start.
- [ ] T071 [P] [US1] Implement `LivingRoots.Verification/Evidence/CropPhaseDump.cs`: top-level and per-crop fields including `soilHealthState` (`measured|missing|nan`), `healthTier` five values, `basePhaseDays` from `CropData` without `ResetPhaseDays`, no `expected*` fields.
- [ ] T072 [P] [US1] Implement `scripts/verify-evidence-bundle.sh`: schema validation, digest recompute against `manifest.sha256`, mandatory-artifact refusal sites, claim/honesty gating, `0`/`19`/`21`.
- [ ] T073 [US1] Add `--verify-bundle` and `--check-docs` dispatch to `scripts/verify-scenario.sh` without launching the game; both forms must be launch-free and return the contract codes.
- [ ] T074 [US1] **US1 checkpoint**: run the US1 suite via `scripts/tests/run-all.sh` (queued, single agent), then a launch-free end-to-end with `scripts/tests/lib/runner-harness.sh`; record the result. Do not claim real-host success yet.

**Checkpoint**: US1 is independently functional and testable.

---

## Phase 4: User Story 2 - Contributor reuses the runner for another feature (Priority: P2)

**Goal**: A contributor adds a JSON scenario (optional Markdown companion) for a second feature and runs the same runner with zero runner-source changes.

**Independent Test**: Add the second scenario, run it against the same harness, and confirm the PR diff contains only scenario/docs/fixture files — no `scripts/` or `LivingRoots*` source changes.

### Tests for US2 (write FIRST)

- [ ] T075 [P] [US2] Suite-discovery tests in `scripts/tests/test-suite.sh`: single file, directory glob `*.scenario.json` (fallback `*.json`), lexicographic order, suite rerun cap, empty suite exit `20`, aggregate precedence.
- [ ] T076 [P] [US2] Scenario-authoring tests in `scripts/tests/test-scenario-authoring.sh`: a second scenario validates against both validators, `testMdPath` exists with one GATE3 block, optional Markdown companion does not affect validation, and the runner executes it with no core changes.
- [ ] T077 [US2] **RED gate (US2)**: run `scripts/tests/test-suite.sh` and `scripts/tests/test-scenario-authoring.sh` and record the failing output; no US2 implementation may start yet.

### Implementation for US2

- [ ] T078 [US2] Implement `scripts/lib/suite.sh` and wire directory discovery/aggregation into `scripts/verify-scenario.sh`; preserve per-scenario JUnit/Allure entries and the aggregate verdict.
- [ ] T079 [US2] Add the second feature scenario `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json` with a different threshold/expected verdict but the same contract; no runner-source edits.
- [ ] T080 [US2] Add `specs/010-headless-verification-runner/SCENARIO-AUTHORING.md` with the JSON-first workflow, optional Markdown companion, `subject` selection, expected-verdict rules, and the zero-core-change rule.
- [ ] T081 [P] [US2] Add `specs/010-headless-verification-runner/contracts/scenario-companion-template.md` as the optional human-facing checklist template.
- [ ] T082 [P] [US2] Update `specs/010-headless-verification-runner/fixtures/README.md` with the second scenario and the directory-suite example.
- [ ] T083 [US2] **US2 checkpoint**: run `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json` through `scripts/verify-scenario.sh` with the fake host; assert the diff contains only scenario/docs/fixtures.

**Checkpoint**: US1 and US2 both work independently.

---

## Phase 5: User Story 3 - CI runs verification on a self-hosted runner (Priority: P3)

**Goal**: CI mode runs the same entry point non-interactively, emits JUnit XML + Allure JSON, uses a self-hosted runner with the builddeps token, and keeps public GitHub-hosted CI dormant.

**Independent Test**: On a self-hosted runner with `STARDEW_GAME_PATH` and `BUILDDEPS_TOKEN` (or cache), `.github/workflows/verify-scenario.yml` runs a scenario, uploads the bundle, and reports `fail`/`errored`/`partial` as non-zero; a public-hosted PR does not invoke it.

### Tests for US3 (write FIRST)

- [ ] T084 [P] [US3] CI-mode tests in `scripts/tests/test-ci-mode.sh`: `--ci` is non-interactive, same verdict/schema as local, no prompts, stdout/stderr separation, exit mapping unchanged.
- [ ] T085 [P] [US3] Reporter tests in `scripts/tests/test-reporters.sh`: JUnit XML with `<skipped>` for quarantine, Allure JSON with `flaky: true`, per-scenario entries preserved, malformed paths rejected.
- [ ] T086 [P] [US3] Workflow-contract tests in `scripts/tests/test-ci-workflow.sh`: self-hosted labels, token scoping, `cache-mode`, least privilege, high/low runner group separation, no invocation on public-hosted PRs.
- [ ] T087 [US3] **RED gate (US3)**: run `scripts/tests/test-ci-mode.sh`, `scripts/tests/test-reporters.sh`, and `scripts/tests/test-ci-workflow.sh` and record the failing output; no US3 implementation may start yet.

### Implementation for US3

- [ ] T088 [US3] Implement `scripts/lib/ci.sh` and wire `--ci` into `scripts/verify-scenario.sh`: non-interactive, machine-readable summary, no behavior change to verdict/security.
- [ ] T089 [US3] Implement `scripts/lib/reporters.sh`: JUnit XML + Allure JSON writers, `<skipped>` for quarantine, `flaky` metadata, per-scenario entries.
- [ ] T090 [US3] Add `.github/workflows/verify-scenario.yml` using a self-hosted runner, least-privilege `GITHUB_TOKEN`, `cache-mode`, token from secrets, bundle upload + durable-store export, and a public-hosted guard.
- [ ] T091 [US3] Update `specs/010-headless-verification-runner/GOVERNANCE-ANNEX.md` with the CI control matrix, runner-group split, and cache-mode settings referenced by the workflow.
- [ ] T092 [US3] **US3 checkpoint**: run `scripts/tests/test-ci-mode.sh` and `scripts/tests/test-reporters.sh` with stubs; assert local/CI verdict parity and that `scripts/tests/test-ci-workflow.sh` keeps public-hosted CI dormant.

**Checkpoint**: All three user stories work independently.

---

## Phase 6: Polish & Verification (ONLY these tasks attest success)

**Purpose**: Documentation, hardening, and the authoritative verification pass. Implementation agents must not claim completion before T110–T118.

- [ ] T093 [P] Update `specs/010-headless-verification-runner/quickstart.md` with the final command lines, the two-validator schema check, and the verification-only attestation note.
- [ ] T094 [P] Update `specs/010-headless-verification-runner/traceability.md` from planned to actual test case IDs and code paths; keep one row per CHK and per SC.
- [ ] T095 [P] Add `scripts/tests/test-redaction.sh` covering success/failure/verbose/CI/JUnit/Allure sinks with a fake token; the build fails if the token survives any sink.
- [ ] T096 [P] Add `scripts/tests/test-performance.sh` for the 5 ms bound, warmup discard, young-generation delta, and `gc-attributed`.
- [ ] T097 [P] Add `scripts/tests/test-retention.sh` for the quarterly sample, durable-store receipt, 365-day retention, and denied early deletion.
- [ ] T098 [P] Add `scripts/tests/test-security-controls.sh` for the non-root guard, `GITHUB_TOKEN` read-only, token-in-argv/URL prohibition, and archive member validation.
- [ ] T099 [P] Add `scripts/tests/test-bundle-negative.sh` with one tampered fixture per artifact class; the offline verifier must reject every one.
- [ ] T100 [P] Add `scripts/tests/test-quarantine-shadow.sh` for the ≥20 consecutive shadow-pass re-enablement gate and reset-on-failure.
- [ ] T101 [P] Add `scripts/tests/test-check-docs-negative.sh` for missing/duplicate/inverted GATE3 markers in `test.md`, `ROADMAP.md`, and `COMPLETION_SUMMARY.md`.
- [ ] T102 [P] Add `scripts/tests/test-acquisition-negative.sh` for non-HTTPS redirects, non-GitHub hosts, mutable `latest`, asset ID/name mismatch, and absent per-file digest map.
- [ ] T103 [P] Add `scripts/tests/test-suite-negative.sh` for empty directory, malformed scenario in a suite, and the suite rerun cap.
- [ ] T104 [P] Add `scripts/tests/test-farmhand.sh` for the `debug split` host-guard path, skip line, forbidden evidence, and non-promotion.
- [ ] T105 [P] Add `specs/010-headless-verification-runner/VERIFICATION.md` recording the exact verification commands, expected outputs, and the Gate 3 evidence bundle path.
- [ ] T106 Update `specs/010-headless-verification-runner/CONCURRENCY.md` §9 review date and confirm every primitive, including process-group reaping and the XDG probe, has a row.
- [ ] T107 Update `specs/010-headless-verification-runner/GOVERNANCE-ANNEX.md` with the final review record and any deviations found during verification.
- [ ] T108 Update `specs/004-playable-compost-loop/test.md` GATE3 block only after T114 produces a clean non-flaky pass; never hand-edit a non-pass block to `verified`.
- [ ] T109 Update `ROADMAP.md` and `COMPLETION_SUMMARY.md` GATE3 blocks only after T114; marker-only replacement.
- [ ] T110 **Verification — build**: run `dotnet build Stardew-LivingRoots.sln --configuration Release`; record the exact output. A warning-as-error or build failure blocks the feature.
- [ ] T111 **Verification — tests**: run `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal` plus `scripts/tests/run-all.sh`; record per-suite counts. Any failure blocks the feature.
- [ ] T112 **Verification — formatting**: run `dotnet format Stardew-LivingRoots.sln --verify-no-changes`; record the result. Any drift blocks the feature.
- [ ] T113 **Verification — shell**: run `bash -n` on every `scripts/**/*.sh` and `shellcheck` where available; record the result.
- [ ] T114 **Verification — schema**: run `scripts/tests/lib/schema-validate.sh` with Python `jsonschema` and `ajv-cli`; assert 28/28 valid, 14/14 invalid with one error, 2/2 runner-invalid schema-accepted, identical outcomes.
- [ ] T115 **Verification — launch-free e2e**: run VS-01 through VS-10 from `quickstart.md` against the fake host; record each exit code and bundle verdict.
- [ ] T116 **Verification — real-host e2e**: run VS-01 on a host with `STARDEW_GAME_PATH` and a valid cache/token; capture the SMAPI log, bundle, and `test.md` result. If no host is available, record `BLOCKED` with the exact missing prerequisite; do not claim success.
- [ ] T117 **Verification — Gate 3**: produce the multi-signal evidence bundle, write the `test.md` GATE3 block, and record the session date + observation notes + artifact comparison + SMAPI log. Only this task may attest `verified`.
- [ ] T118 **Verification — checklist/traceability audit**: re-run one reviewer per checklist, confirm every `[x]` still holds, reopen any false green, and update `traceability.md` with actual test/code locations.
- [ ] T119 **Final commit**: commit `specs/010-headless-verification-runner/VERIFICATION.md`, `specs/010-headless-verification-runner/traceability.md`, and the Gate 3 blocks in `specs/004-playable-compost-loop/test.md` with `docs(010): ...`; do not amend earlier commits.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: starts immediately; T001/T004/T005 are sequential scaffold steps, the rest are `[P]`.
- **Foundational (Phase 2)**: depends on Phase 1; T025 RED gate blocks T026–T038; T039 blocks all user stories.
- **US1 (Phase 3)**: depends on Phase 2; T053 RED gate blocks T054–T073.
- **US2 (Phase 4)**: depends on US1 T074; T077 RED gate blocks T078–T082.
- **US3 (Phase 5)**: depends on US1 T074; T087 RED gate blocks T088–T091. US2 and US3 touch disjoint files and may run in parallel.
- **Polish/Verification (Phase 6)**: depends on US1+US2+US3; T110–T118 are the only attestation tasks.

### User Story Dependencies

- **US1 (P1)**: MVP; no dependency on US2/US3.
- **US2 (P2)**: reuses the US1 runner; no source edits to the runner core.
- **US3 (P3)**: reuses the US1 runner; adds CI mode/reporters/workflow only.

### Within Each Story

1. All test tasks complete.
2. RED-gate task records failing output; no implementation before it.
3. Implementation tasks in dependency order (libraries before the orchestrator; models before writers).
4. Story checkpoint before the next priority.

---

## Parallel Execution Waves (subagent-driven)

Use one subagent per context; reuse it for files in that context. Every wave waits for the previous wave’s shared-tooling command to finish.

### Wave 0 — Scaffold (sequential)
T001 → T004 → T005; T002/T003/T006/T007/T008/T009/T010/T011 are `[P]` after T001.

### Wave 1 — Foundational tests (`[P]`, 4 subagents)
- Agent A (C# domain): T012–T017
- Agent B (bash libs): T018–T021
- Agent C (schema/version): T022–T023
- Agent D (exit codes/run-all): T024
Then **single agent** runs T025 RED gate.

### Wave 2 — Foundational implementation (`[P]`, 4 subagents)
- Agent A (mod API): T026–T027
- Agent B (evaluators/security/evidence): T028–T032
- Agent C (bash libs/common/atomic/concurrency/logging): T033–T036
- Agent D (version/validation/wiring): T037–T038
Then **single agent** runs T039 checkpoint.

### Wave 3 — US1 tests (`[P]`, 4 subagents)
- Agent A (CLI/acquisition/display/launch): T040–T042
- Agent B (injection/signals/evaluation/evidence): T043–T046
- Agent C (promotion/save/cleanup): T047–T049
- Agent D (flaky/quarantine/docs): T050–T052
Then **single agent** runs T053 RED gate.

### Wave 4 — US1 implementation
- Wave 4a `[P]` (3 subagents): T054–T067 split by `scripts/lib/` domain; T069–T071 verification mod.
- Wave 4b (single agent, shared `scripts/verify-scenario.sh`): T068, T073.
- Wave 4c (single agent, shared verifier): T072.
Then **single agent** runs T074 checkpoint.

### Wave 5 — US2 and US3 in parallel
- Agent E (US2): T075–T083.
- Agent F (US3): T084–T092.
Both may run concurrently; they touch different files. The shared `scripts/tests/run-all.sh` is only run by the single checkpoint agent, not by both.

### Wave 6 — Verification (serialized shared tooling)
- T093–T109 docs/hardening may be `[P]` by disjoint files, but only one agent runs `dotnet test`/`run-all`/validators/game at a time.
- T110–T118 run sequentially in the order listed; T116 may be `BLOCKED` if no real host is available, and a `BLOCKED` real-host result is not a success claim.

---

## Parallel Example: US1 tests

```bash
# Wave 3, one agent per disjoint file group:
Task: "CLI/usage tests in scripts/tests/test-verify-cli.sh"          # T040
Task: "Acquisition/cache tests in scripts/tests/test-acquisition.sh" # T041
Task: "Display/launch tests in scripts/tests/test-display-launch.sh" # T042
Task: "Injection tests in scripts/tests/test-injection.sh"           # T043
# ... same wave, different files; no two agents edit the same test file.
```

## Parallel Example: US1 implementation libraries

```bash
# Wave 4a, disjoint scripts/lib files:
Task: "Implement scripts/lib/acquisition.sh"  # T054
Task: "Implement scripts/lib/preflight.sh"    # T055
Task: "Implement scripts/lib/display.sh"      # T056
# The orchestrator scripts/verify-scenario.sh (T068) waits for all libraries.
```

## Parallel Example: US2

```bash
# Wave 5, one agent owns US2; these files are disjoint:
Task: "Implement scripts/lib/suite.sh"                              # T078
Task: "Add specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json" # T079
Task: "Add specs/010-headless-verification-runner/SCENARIO-AUTHORING.md" # T080
# T081/T082 may run in parallel with T079/T080; T083 waits for all.
```

## Parallel Example: US3

```bash
# Wave 5, one agent owns US3; these files are disjoint:
Task: "Implement scripts/lib/ci.sh"                 # T088
Task: "Implement scripts/lib/reporters.sh"          # T089
Task: "Add .github/workflows/verify-scenario.yml"   # T090
# T091 updates GOVERNANCE-ANNEX.md concurrently; T092 waits for all.
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 scaffold.
2. Complete Phase 2 with the RED gate and checkpoint.
3. Complete US1 tests → RED gate → implementation → checkpoint.
4. Run the launch-free VS-01; only then consider real-host execution.
5. Do not start US2/US3 until US1’s checkpoint is green.

### Incremental Delivery

1. US1: unattended single-scenario verification (MVP).
2. US2: second scenario + directory suites, zero core changes.
3. US3: CI mode, reporters, self-hosted workflow.
4. Phase 6 verification attests the whole feature; implementation agents never do.

### Parallel Team / Subagent Strategy

1. One subagent owns one context (C# domain, bash libs, schema/version, CI).
2. Reuse the same subagent for tasks in the same context; do not spawn a new agent per file.
3. `[P]` tasks are issued in waves; non-`[P]` tasks wait.
4. Shared long-running tooling is queued: one `dotnet test`, one `run-all.sh`, one validator run, one game launch at a time.
5. Never kill or interrupt another agent’s process; wait for it to finish.

---

## Appendix A — Data-model constraints to enforce verbatim

These are quoted from `data-model.md` and `contracts/scenario.schema.json`; T038 and the evaluator/writer tasks must enforce them without reinterpretation.

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
| Verdict | closed set `{pass,fail,errored,partial,skipped}` + derived `flaky`; exit `0/18/19/20/21` per contract |
| Promotion | `verified` only on clean non-flaky `pass` + three agreeing signals + honesty ≥ R; flaky pass stays `partial` |
| Manifest | mandatory `manifest.sha256`; `subject` is canonical; `files` deprecated alias; per-attempt manifests listed in top-level `subject`, `attempts[]` only holds `attempts/<n>/attempt.json` |

---

## Notes

- Tests are not optional for this feature; the RED gate is a hard block.
- [P] means different files and no incomplete dependency; never parallelize edits to `scripts/verify-scenario.sh` or any shared test harness.
- Verification tasks T110–T118 are the only source of truth for completion. A green implementation checkpoint is not an attestation.
- If a real-host prerequisite is missing at T116, record `BLOCKED` with the exact missing item; never present a launch-free run as real gameplay evidence.
- Commit after every task; keep verification evidence and Gate 3 records in the same commit as the run that produced them.
