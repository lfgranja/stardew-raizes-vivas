# Traceability Matrix: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Created**: 2026-10-27
**Updated**: 2026-10-27 (remediated artifacts: `.sh` suites, `LivingRoots.Verification.Runner/`, `machine-pass` vs. `verified`)
**Purpose**: Link every checklist item (CHK) to its governing requirement, planned test case, and code location. Satisfies C-CON-17, C-CON-21, C-REL-19, C-SCN-06, C-SCN-10, and the traceability items in each checklist.

**Status**: Planned-test matrix. Test-case IDs are planned; code locations name the intended remediated artifacts. Where a row has an executable test, it also names the `.sh` suite, C# test file, or manual verification artifact that must exist and pass before the row is satisfied; requirements-quality and documentation rows that have no executable suite say so by naming the reviewed document. This matrix is the contract the implementation must fill in, not an attestation of success.

**Attestation vocabulary**: The runner and every automated/CI path write only `partial` or `machine-pass`. `verified` is written exclusively by the human E attestation (T148 in `tasks.md`). No row below assigns `verified` to a runner, script, CI job, or automated test.

**Checklist ID spaces**: CHK IDs restart at `CHK001` in each checklist and are local to that file, not globally unique. Cite them as `<file>#CHK0NN`.

| Checklist | Local CHK range | Items |
|-----------|-----------------|-------|
| `checklists/requirements.md` | CHK001–CHK016 | 16 |
| `checklists/scenario-contract.md` | CHK001–CHK055 | 55 |
| `checklists/concurrency.md` | CHK001–CHK049 | 49 |
| `checklists/reliability.md` | CHK001–CHK059 | 59 |
| `checklists/security.md` | CHK001–CHK053 | 53 |

**Task-plan mapping**: Checklist rows map to planned `TC-*` test-case IDs for requirement-to-test traceability. The executable task IDs (`T###`) live in `tasks.md` and are intentionally not duplicated here; when a `TC-*` is implemented, this row keeps the `TC-*` ID and points at the real suite/code path created by the corresponding `T###`.

## Requirements Quality (`checklists/requirements.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001 | Content quality: tools named are the feature's subject | TC-REQ-001 | `spec.md` review |
| CHK002 | User value / business need | TC-REQ-002 | `spec.md` US1–US3 |
| CHK003 | Non-technical stakeholder readability | TC-REQ-003 | `spec.md` review |
| CHK004 | Mandatory sections complete | TC-REQ-004 | `spec.md` template |
| CHK005 | No `[NEEDS CLARIFICATION]` markers | TC-REQ-005 | `spec.md` grep gate |
| CHK006 | Testable/unambiguous requirements | TC-REQ-006 | `spec.md` FR/NFR |
| CHK007 | Measurable success criteria | TC-REQ-007 | `spec.md` SC-001..SC-014 |
| CHK008 | Technology-agnostic success criteria | TC-REQ-008 | `spec.md` SC review |
| CHK009 | Acceptance scenarios defined | TC-REQ-009 | `spec.md` US1–US3 |
| CHK010 | Edge cases identified | TC-REQ-010 | `spec.md` Edge Cases |
| CHK011 | Scope bounded | TC-REQ-011 | `spec.md` NFR/Assumptions/Out of Scope |
| CHK012 | Dependencies and assumptions identified | TC-REQ-012 | `spec.md` A1–A16 |
| CHK013 | Functional requirements have acceptance criteria | TC-REQ-013 | `spec.md` FR-001..FR-027 |
| CHK014 | User scenarios cover primary flows | TC-REQ-014 | `spec.md` US1–US3 |
| CHK015 | Success criteria are measurable, method-backed, and traceable (spec-time); actual evidence is Gate 3 | TC-REQ-015 | `spec.md` SC-001..SC-014; `contracts/test-md-template.md` (Gate 3) |
| CHK016 | No implementation leakage | TC-REQ-016 | `spec.md` review |

## Scenario Contract (`checklists/scenario-contract.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK007 | FR-007; C-SCN-36 (required-field set); strict `additionalProperties: false`; IEEE 829 mapping (C-SCN-17); conditional `if/then` | TC-SCN-001..007 | `contracts/scenario.schema.json`; `LivingRoots.Verification.Runner/Validation/ScenarioValidator.cs`; `scripts/tests/test-schema-contract.sh`; `data-model.md` |
| CHK008–CHK014 | FR-010; C-SCN-29 (structured `predicate`, `subject`, `modConstant`, inclusivity, typed operands, sentinel enum); boundary-value cases | TC-SCN-008..014 | `contracts/scenario.schema.json#/$defs/predicate` and `#/$defs/threshold`; `LivingRoots.Verification.Runner/Evaluation/PredicateEvaluator.cs`; `scripts/tests/test-evaluation.sh`; `scripts/tests/test-schema-contract.sh` |
| CHK015–CHK020 | FR-006/FR-009; ordered `steps`, explicit `environment`, `rngSeed`, `time`; idempotency; ≤5 steps | TC-SCN-015..020 | `contracts/scenario.schema.json`; `scripts/verify-scenario.sh`; `scripts/lib/injection.sh`; `scripts/tests/test-injection.sh`; `scripts/tests/test-schema-contract.sh` |
| CHK021–CHK025 | FR-006/FR-009; typed `initCommands` (C-SCN-33); injection lifecycle (C-SCN-30); versioned `ILivingRootsModApi` | TC-SCN-021..025 | `contracts/scenario.schema.json`; `scripts/lib/injection.sh`; `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs`; `scripts/tests/test-injection.sh` |
| CHK026–CHK030 | SC-001..SC-014; C-SCN-06 (machine-evaluable acceptance, traceability) | TC-SCN-026..030 | `spec.md` SC; `scripts/tests/test-verdict.sh`; `scripts/tests/test-evidence.sh`; this matrix |
| CHK031–CHK035 | FR-010; C-SCN-37 (missing vs. 0 vs. NaN vs. sentinel); sentinel registry; equivalence classes | TC-SCN-031..035 | `contracts/scenario.schema.json#/$defs/sentinel`; `LivingRoots.Verification.Runner/Evaluation/PredicateEvaluator.cs`; `scripts/tests/test-evaluation.sh` |
| CHK036–CHK040 | FR-007; C-SCN-31 (BACKWARD_TRANSITIVE, immutable `vN`, bump discipline, migration) | TC-SCN-036..040 | `contracts/scenario.schema.json`; `data-model.md` (migration path); `scripts/tests/test-schema-contract.sh` |
| CHK041–CHK045 | FR-007/FR-022; `$defs`/`$ref`; parameterization; C-SCN-38 cross-validator agreement | TC-SCN-041..045 | `contracts/scenario.schema.json`; `fixtures/valid/` and `fixtures/invalid/`; `scripts/tests/lib/schema-validate.sh`; `scripts/tests/test-schema-contract.sh` |
| CHK046–CHK050 | FR-007; `title`/`description`/`examples`; short scenarios; machine validity independent of prose | TC-SCN-046..050 | `contracts/scenario.schema.json`; `contracts/test-md-template.md`; `SCENARIO-AUTHORING.md` |
| CHK051–CHK055 | C-SCN-10 (necessary, unambiguous, consistent, verifiable, traceable) | TC-SCN-051..055 | `spec.md`; this matrix |

## Concurrency (`checklists/concurrency.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001 | Atomic display allocation via `-displayfd` (no TOCTOU) — FR-020, C-CON-01 | TC-CON-001 | `scripts/lib/display.sh` (`display_start`); `scripts/tests/test-display-launch.sh` |
| CHK002 | No `/tmp/.X*-lock` probing; rationale documented — FR-020, C-CON-01 | TC-CON-002 | `CONCURRENCY.md` §Display; `scripts/tests/test-display-launch.sh` |
| CHK003 | Per-instance display number — FR-020, C-CON-01 | TC-CON-003 | `scripts/lib/display.sh` (`display_start`); `scripts/tests/test-display-launch.sh` |
| CHK004 | Server-side socket removal on cleanup — FR-020, C-CON-01/C-CON-22 | TC-CON-004 | `scripts/lib/display.sh` (`display_stop`); `scripts/tests/test-cleanup.sh` |
| CHK005 | Orphan detection is report-only (no signalling) — C-CON-01/C-CON-22 | TC-CON-005 | `scripts/lib/display.sh` (`orphan_registry`); `scripts/tests/test-cleanup.sh` |
| CHK006 | Disposable save copy, never the player's save — FR-018, C-CON-20 | TC-CON-006 | `scripts/lib/save.sh` (`save_snapshot`); `scripts/tests/test-save-preservation.sh` |
| CHK007 | Byte-identical original via SHA-256 before/after — FR-018, SC-005, C-CON-02 | TC-CON-007 | `scripts/lib/save.sh` (`save_hash`); `scripts/tests/test-save-preservation.sh` |
| CHK008 | SHA-256 for saves; 0.0001f float tolerance for deltas — FR-012, C-CON-02 | TC-CON-008 | `LivingRoots.Verification.Runner/Evaluation/SaveComparator.cs` (reuses/extracts `LivingRoots.Tests/Verification/SaveComparator.cs`); `LivingRoots.Tests/Verification/SaveComparatorPropertyTests.cs`; `scripts/tests/test-save-preservation.sh` |
| CHK009 | Backup of `Saves/<Name>/` and sidecars before mutation — FR-018, C-CON-20 | TC-CON-009 | `scripts/lib/save.sh` (`save_snapshot`); `scripts/tests/test-save-preservation.sh` |
| CHK010 | Non-identical save → `errored` with before/after — FR-018, C-CON-03 | TC-CON-010 | `scripts/lib/save.sh` (`save_restore`); `scripts/tests/test-save-preservation.sh` |
| CHK011 | Atomic sidecar writes (temp→fsync→rename→fsync) — C-CON-04 | TC-CON-011 | `scripts/lib/atomic.sh`; `scripts/tests/test-atomic.sh` |
| CHK012 | `XDG_CONFIG_HOME` per-instance absolute temp dir — FR-018, C-CON-05 | TC-CON-012 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK013 | Temp root via `mktemp -d` — FR-020, C-CON-07 | TC-CON-013 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK014 | `HOME` redirected when XDG is ignored — C-CON-05 | TC-CON-014 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK015 | Trap/finally cleanup with observable failure — FR-019, C-CON-06 | TC-CON-015 | `scripts/lib/cleanup.sh` (`cleanup`); `scripts/tests/test-cleanup.sh` |
| CHK016 | Runtime dirs 0700 — C-CON-07 | TC-CON-016 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK017 | No whole-run `flock`; short-lived resource locks only — FR-020, C-CON-08 | TC-CON-017 | `scripts/lib/concurrency.sh`; `CONCURRENCY.md`; `scripts/tests/test-lock.sh` |
| CHK018 | Advisory locks only — C-CON-08 | TC-CON-018 | `scripts/lib/concurrency.sh`; `scripts/tests/test-lock.sh` |
| CHK019 | Bounded non-blocking lock acquisition — C-CON-08/C-CON-23 | TC-CON-019 | `scripts/lib/concurrency.sh` (`claim_acquire`); `scripts/tests/test-lock.sh` |
| CHK020 | OFD locks preferred — C-CON-08/C-CON-23 | TC-CON-020 | `scripts/lib/concurrency.sh`; `scripts/tests/test-lock.sh` |
| CHK021 | Global lock order documented — C-CON-08/C-CON-23 | TC-CON-021 | `CONCURRENCY.md` §Lock Order; `scripts/tests/test-lock.sh` |
| CHK022 | Conflict detection scoped to instance's own game process — FR-020, C-CON-09 | TC-CON-022 | `scripts/lib/preflight.sh` (`preflight_process_conflict`); `scripts/tests/test-lock.sh` |
| CHK023 | Atomic check-and-claim (`O_EXCL`, never check-then-create) — C-CON-09 | TC-CON-023 | `scripts/lib/concurrency.sh` (`claim_acquire`); `scripts/tests/test-lock.sh` |
| CHK024 | PID liveness via identity token, not `kill -0` alone — C-CON-09 | TC-CON-024 | `scripts/lib/concurrency.sh` (`pid_identity`); `scripts/tests/test-lock.sh` |
| CHK025 | Distinct instance IDs — C-CON-10 | TC-CON-025 | `scripts/lib/save.sh` (`instance_init`); `scripts/tests/test-stress.sh` |
| CHK026 | Orphan display detect-and-report only — C-CON-01/C-CON-22 | TC-CON-026 | `scripts/lib/display.sh` (`orphan_registry`); `scripts/tests/test-cleanup.sh` |
| CHK027 | Child reaping (`waitpid`, SIGTERM→SIGKILL) — FR-019, C-CON-11 | TC-CON-027 | `scripts/lib/cleanup.sh` (`cleanup`); `scripts/tests/test-cleanup.sh` |
| CHK028 | Signal forwarding via process group — FR-019, C-CON-11 | TC-CON-028 | `scripts/lib/cleanup.sh` (`cleanup`); `scripts/tests/test-cleanup.sh` |
| CHK029 | Stale-PID safety: never signal without token match — C-CON-09 | TC-CON-029 | `scripts/lib/concurrency.sh` (`pid_identity`); `scripts/tests/test-lock.sh` |
| CHK030 | `mktemp -d`, not deprecated `tempfile` — FR-020, C-CON-07 | TC-CON-030 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK031 | Trap/finally cleanup with observable failure — FR-019, C-CON-06 | TC-CON-031 | `scripts/lib/cleanup.sh` (`cleanup`); `scripts/tests/test-cleanup.sh` |
| CHK032 | Temp dirs on local filesystem only — C-CON-12 | TC-CON-032 | `scripts/lib/save.sh` (`assert_local_fs`); `scripts/tests/test-save-preservation.sh` |
| CHK033 | Temp dir permissions 0700 — C-CON-07 | TC-CON-033 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK034 | Unique instance namespace; no shared writable run state — C-CON-10 | TC-CON-034 | `scripts/lib/save.sh` (`instance_init`); `scripts/tests/test-stress.sh` |
| CHK035 | Dynamic display allocation — FR-020, C-CON-01 | TC-CON-035 | `scripts/lib/display.sh` (`display_start`); `scripts/tests/test-display-launch.sh` |
| CHK036 | Sidecar isolation via distinct temp roots — FR-020, C-CON-10 | TC-CON-036 | `scripts/lib/save.sh` (`isolate_env`); `scripts/tests/test-save-preservation.sh` |
| CHK037 | Correct, non-interleaved verdicts (D1–D5) — C-CON-10/C-CON-21 | TC-CON-037 | `scripts/tests/test-stress.sh`; `scripts/tests/stress/run_stress.sh` |
| CHK038 | Hash-verified backup before mutation — FR-018, C-CON-20 | TC-CON-038 | `scripts/lib/save.sh` (`save_snapshot`); `scripts/tests/test-save-preservation.sh` |
| CHK039 | Idempotent restore — FR-018, C-CON-13 | TC-CON-039 | `scripts/lib/save.sh` (`save_restore`); `scripts/tests/test-save-preservation.sh` |
| CHK040 | Atomic restore — C-CON-13 | TC-CON-040 | `scripts/lib/save.sh` (`save_restore`); `scripts/lib/atomic.sh`; `scripts/tests/test-atomic.sh`; `scripts/tests/test-save-preservation.sh` |
| CHK041 | Restore failure leaves original untouched — FR-018, C-CON-13 | TC-CON-041 | `scripts/lib/save.sh` (`save_restore`); `scripts/tests/test-save-preservation.sh` |
| CHK042 | `O_EXCL` paired with `O_CREAT` for locks/temp — C-CON-14 | TC-CON-042 | `scripts/lib/concurrency.sh`; `scripts/lib/atomic.sh`; `scripts/tests/test-lock.sh`; `scripts/tests/test-atomic.sh` |
| CHK043 | `fsync` data before rename, parent after — C-CON-04/C-CON-14 | TC-CON-043 | `scripts/lib/atomic.sh`; `scripts/tests/test-atomic.sh` |
| CHK044 | Torn-write prevention for all persisted artifacts — C-CON-04 | TC-CON-044 | `scripts/lib/atomic.sh`; `scripts/tests/test-atomic.sh` |
| CHK045 | Atomic operations documented with failure modes — C-CON-15 | TC-CON-045 | `CONCURRENCY.md`; `scripts/tests/test-atomic.sh` |
| CHK046 | Local/CI parity — FR-023, C-CON-16 | TC-CON-046 | `scripts/tests/test-runall.sh`; `scripts/tests/test-ci-mode.sh` |
| CHK047 | Concurrency tests (Coyote, FsCheck, bash, stress) — C-CON-19 | TC-CON-047 | `LivingRoots.Tests/Verification/CoyoteSchedulingTests.cs`; `LivingRoots.Tests/Verification/SaveComparatorPropertyTests.cs`; `LivingRoots.Tests/Verification/PredicateEvaluatorPropertyTests.cs`; `scripts/tests/test-stress.sh`; `scripts/tests/test-lock.sh`; `scripts/tests/test-atomic.sh` |
| CHK048 | CHK → test → code traceability — C-CON-17 | TC-CON-048 | this matrix |
| CHK049 | TOCTOU review of every primitive — C-CON-18 | TC-CON-049 | `CONCURRENCY.md` §9; `scripts/tests/test-lock.sh`; `scripts/tests/test-atomic.sh` |

## Reliability (`checklists/reliability.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK005 | FR-011; retryable/non-retryable sets; verdict-state eligibility; idempotency gate; `errored`/`partial` vs. `fail` | TC-REL-001..005 | `scripts/lib/retry.sh` (`classify_retry`); `compat-matrix.json` (`failureClasses`); `contracts/exit-codes.md`; `scripts/tests/test-flaky.sh` |
| CHK006–CHK011 | FR-011; C-REL-04 (per-scenario + suite caps); fixed 5 s cooldown; per-attempt artifacts; stability across SMAPI | TC-REL-006..011 | `scripts/lib/retry.sh` (`run_with_retries`); `scripts/lib/suite.sh` (`suiteRerunCount`/`suiteRerunCapHit`); `scripts/tests/test-flaky.sh`; `scripts/tests/test-suite.sh` |
| CHK012–CHK016 | FR-011; C-REL-07/C-REL-13/C-REL-26 (derived `flaky`, N≥30 Wilson gate, RAFT, expiry) | TC-REL-012..016 | `scripts/lib/retry.sh` (`derive_flaky`); `scripts/tests/test-flaky.sh`; `scripts/tests/test-quarantine.sh` |
| CHK017–CHK023 | FR-011; C-REL-10/C-REL-14/C-REL-21/C-REL-23/C-REL-27 (trigger, 28-day cap, auto-disable, owner chain, never-quarantine, human approval, `<skipped>`) | TC-REL-017..023 | `scripts/lib/quarantine.sh` (`quarantine_gate`); `quarantine.json`; `never-quarantine.json`; `scripts/tests/test-quarantine.sh`; `scripts/tests/test-reporters.sh`; `scripts/tests/test-contract-registries.sh` |
| CHK024–CHK029 | FR-011; NFR-003; C-REL-01 (closed five-base verdict set + derived `flaky`; crash/incomplete/disagreement; exit mapping) | TC-REL-024..029 | `scripts/lib/verdict.sh` (`compute_verdict`); `scripts/lib/version-gate.sh`; `contracts/exit-codes.md`; `scripts/tests/test-verdict.sh`; `scripts/tests/test-version-gate.sh` |
| CHK030–CHK034 | FR-016; C-REL-15 (runner writes `partial`/`machine-pass` only; `machine-pass` requires clean non-flaky `pass` + three agreeing signals + honesty ≥ R + no open anomaly; `verified` is human E only; re-promotion = new run) | TC-REL-030..034 | `scripts/lib/promotion.sh` (`promote_test_md`); `promotion-triggers.json`; `contracts/test-md-template.md`; `scripts/tests/test-promotion.sh`; `scripts/tests/test-docs.sh`; `scripts/tests/test-contract-registries.sh` |
| CHK035–CHK040 | FR-014/FR-015; C-REL-24 (W/R/E definitions, sequential assignment, bounded claims, `claims.json`) | TC-REL-035..040 | `scripts/lib/evidence.sh`; `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs`; `scripts/verify-evidence-bundle.sh`; `scripts/tests/test-evidence.sh`; `scripts/tests/test-bundle-negative.sh` |
| CHK041–CHK045 | FR-009; C-REL-12 (three signals, divergence hard stop, no single-source pass, necessary-not-sufficient, independence caveat) | TC-REL-041..045 | `scripts/lib/signals.sh` (`evaluate_signals`); `scripts/tests/test-signals.sh` |
| CHK046–CHK050 | FR-014/FR-015; SC-005; C-BND-01 (minimum evidence set, byte-identical save, per-attempt artifacts, digest recompute, offline verification) | TC-REL-046..050 | `scripts/lib/evidence.sh`; `contracts/evidence-bundle.schema.json`; `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs`; `scripts/verify-evidence-bundle.sh`; `scripts/tests/test-evidence.sh`; `scripts/tests/test-bundle-negative.sh` |
| CHK051–CHK055 | FR-009/FR-011/FR-013 (false-positive control, verdict separation, warmup, `gc-attributed`, flaky tracking) | TC-REL-051..055 | `scripts/lib/performance.sh`; `scripts/tests/test-performance.sh`; `scripts/tests/test-verdict.sh`; `scripts/tests/test-flaky.sh` |
| CHK056–CHK059 | FR-023; C-REL-19/C-REL-20 (local/CI parity, verdict tests, traceability, tiered review) | TC-REL-056..059 | `scripts/tests/test-runall.sh`; `scripts/tests/test-ci-mode.sh`; `scripts/tests/test-verdict.sh`; this matrix; `GOVERNANCE-ANNEX.md` |

## Security (`checklists/security.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK007 | FR-002; C-SEC-14/C-SEC-15/C-SEC-16/C-SEC-21/C-SEC-26 (scope, 90-day max/30-day rotation, storage, preflight validation, no token logs, OIDC upgrade, no sharing) | TC-SEC-001..007 | `scripts/lib/acquisition.sh` (`acquire_game_files`); `scripts/lib/preflight.sh`; `scripts/tests/test-acquisition.sh`; `scripts/tests/test-redaction.sh` |
| CHK008–CHK014 | FR-002; C-SEC-02/C-SEC-17/C-SEC-18/C-SEC-23/C-SEC-28 (SHA-256, before/after extraction, `gamepath.lock.json`, signature deferral, mismatch response, archive validation, tamper record) | TC-SEC-008..014 | `gamepath.lock.json`; `scripts/lib/acquisition.sh` (`verify_archive`); `LivingRoots.Verification.Runner/Extraction/ArchiveExtractor.cs`; `contracts/evidence-bundle.schema.json`; `scripts/tests/test-acquisition.sh`; `scripts/tests/test-extraction.sh`; `scripts/tests/test-bundle-negative.sh` |
| CHK015–CHK020 | FR-001; C-SEC-22/C-SEC-25 (cache trust tuple, per-job `cache-mode`, untrusted re-verification, content-hash key, no secrets in cache, branch isolation) | TC-SEC-015..020 | `scripts/lib/acquisition.sh` (`cache_gate`); `.github/workflows/verify-scenario.yml`; `scripts/tests/test-acquisition.sh`; `scripts/tests/test-ci-workflow.sh` |
| CHK021–CHK026 | FR-002; C-SEC-04/C-SEC-12 (external input validation, traversal rejection, Form C/homoglyph, archive members, no path disclosure, no env interpolation) | TC-SEC-021..026 | `scripts/lib/sanitize.sh`; `LivingRoots.Verification.Runner/Validation/ScenarioValidator.cs`; `LivingRoots.Verification.Runner/Extraction/ArchiveExtractor.cs`; `LivingRoots/Services/ModDataService.cs` (`SanitizePathSegments`); `scripts/tests/test-sanitize.sh`; `scripts/tests/test-extraction.sh` |
| CHK027–CHK030 | NFR-004; C-SEC-05/C-SEC-06/C-SEC-30 (redaction, no plaintext/encoded secrets, leaked-secret retention, audit vs. application logs) | TC-SEC-027..030 | `scripts/lib/logging.sh`; `scripts/tests/test-redaction.sh`; `scripts/tests/test-retention.sh` |
| CHK031–CHK035 | FR-014/FR-015; C-SEC-29/C-BND-01 (mandatory commands/materials/seeds, signing deferred, minimum evidence, offline verifier, retention/immutability) | TC-SEC-031..035 | `scripts/lib/evidence.sh`; `LivingRoots.Verification.Runner/Evidence/ManifestBuilder.cs`; `contracts/evidence-bundle.schema.json`; `scripts/verify-evidence-bundle.sh`; `scripts/lib/retention.sh`; `scripts/tests/test-evidence.sh`; `scripts/tests/test-bundle-negative.sh` |
| CHK036–CHK039 | FR-002; C-SEC-18/C-SEC-26 (minimal scopes, non-root, runner-group segregation, read-only `GITHUB_TOKEN`) | TC-SEC-036..039 | `scripts/lib/preflight.sh` (root startup guard); `.github/workflows/verify-scenario.yml`; `GOVERNANCE-ANNEX.md`; `scripts/tests/test-game-path.sh`; `scripts/tests/test-ci-workflow.sh` |
| CHK040–CHK044 | FR-002/FR-007/FR-014; C-SEC-28 (fail-closed integrity, missing evidence, unknown fields, partial download, override approval) | TC-SEC-040..044 | `scripts/lib/acquisition.sh`; `scripts/verify-evidence-bundle.sh`; `scripts/tests/test-bundle-negative.sh`; `scripts/tests/test-acquisition.sh`; `scripts/tests/test-extraction.sh` |
| CHK045–CHK049 | NFR-004; C-SEC-24/C-SEC-30 (audit events, JSON fields, sensitive-data exclusion, aggregation SHOULD, retention/immutability) | TC-SEC-045..049 | `scripts/lib/logging.sh`; `scripts/lib/retention.sh`; `GOVERNANCE-ANNEX.md`; `scripts/tests/test-redaction.sh`; `scripts/tests/test-retention.sh` |
| CHK050–CHK053 | FR-023; C-SEC-19/C-SEC-20/C-SEC-24/C-SEC-26/C-SEC-27 (local/CI parity, threat mapping, ownership, review cadence) | TC-SEC-050..053 | `GOVERNANCE-ANNEX.md`; `scripts/tests/test-runall.sh`; `scripts/tests/test-ci-workflow.sh`; this matrix |

## Verdict Requirements (`checklists/reliability.md` CHK024–CHK029)

| Verdict | Governing requirement | Exit | Planned test |
|---------|-----------------------|------|--------------|
| `pass` | FR-011/C-REL-01; all three signals agree, thresholds satisfied, expected verdict matches | `0` | TC-REL-024, TC-REL-029 |
| `fail` | FR-011/C-REL-01; threshold/assertion violation or host-guard breach; matched `expectedVerdict: fail` | `18` | TC-REL-025, TC-REL-029 |
| `errored` | FR-011/C-REL-01/C-CON-03; crash, capture failure, save-preservation breach, version mismatch, expected mismatch | `19` | TC-REL-026, TC-REL-029 |
| `partial` | FR-011/FR-019/C-REL-12; incomplete signal set, signal disagreement, or `cleanQuit: false` | `19` | TC-REL-027, TC-REL-029 |
| `skipped` | FR-011/C-REL-10; quarantined or intentionally not run | `20` all-skipped / `0` mixed | TC-REL-028, TC-REL-029 |
| `flaky` (derived) | FR-011/C-REL-13/C-REL-26; attempt-1 retryable failure + later pass on same commit/seed/environment | `0`; never promotes to `machine-pass` | TC-REL-012..016 |

`pass` is a verdict, not a Gate 3 status. Only a clean non-flaky `pass` with three agreeing signals, honesty ≥ R, and no unresolved anomaly may write `machine-pass`; the runner never writes `verified`, which is the human E attestation (T148) alone.

## Success Criteria (`spec.md` SC-001..SC-014)

| SC | Measurable outcome | Governing requirements | Test case | Suite / location |
|----|--------------------|------------------------|-----------|------------------|
| SC-001 | One documented invocation completes feature 004's scenario with zero keystrokes, exits `0`, and produces a bundle that passes offline verification with every mandatory artifact | FR-001–FR-014, FR-019, FR-022, FR-023 | TC-SC-001 | `scripts/tests/test-verify-cli.sh`; `scripts/tests/lib/runner-harness.sh`; `scripts/verify-evidence-bundle.sh` |
| SC-002 | 100% of `pass` host runs have all three signal artifacts and digests; fewer than three signals never `pass` (they are `partial`/`19`) | FR-009, FR-011, FR-014 | TC-SC-002 | `scripts/tests/test-signals.sh`; `scripts/tests/test-evidence.sh`; `scripts/tests/test-verdict.sh` |
| SC-003 | Boundary tiers 33/34/66/67 yield 1.15/1.00/1.00/0.85, `round(base × multiplier)` clamped to ≥ 1; ≤0/missing/NaN unchanged; 99999 sentinel untouched; 100% of boundary cases match | FR-010, FR-012 | TC-SC-003 | `scripts/tests/test-evaluation.sh`; `LivingRoots.Verification.Runner/Evaluation/PredicateEvaluator.cs` |
| SC-004 | A second feature scenario is added in a PR that changes zero runner-source files and produces the same bundle schema and a verdict | FR-022, FR-014 | TC-SC-004 | `scripts/tests/test-scenario-authoring.sh`; `scripts/tests/test-zero-core-change.sh` |
| SC-005 | Source milestone save and restored working copy hash to their pre-run SHA-256; mismatch is `errored`/`19` | FR-018, FR-020 | TC-SC-005 | `scripts/tests/test-save-preservation.sh`; `scripts/lib/save.sh` |
| SC-006 | Exit-code contract covers `0` (pass and flaky pass), `9`, `10`, `12`, `13`, `14`, `18` (incl. matched expected-fail), `19` (errored/partial/mismatch and matched expected `errored`/`partial`), `20` (no-tests/all-skipped and matched expected `skipped`), `21` (policy/drift/overreach); pass-through fixtures for inherited `1–7`/`15–17`; `8` retired; `11` inherited-but-never-emitted; 100% match | FR-011, NFR-003 | TC-SC-006 | `scripts/tests/test-verdict.sh`; `scripts/tests/test-common.sh`; `contracts/exit-codes.md` |
| SC-007 | Offline verifier recomputes every digest in `manifest.json` and rejects any single tampered byte, with at least one tampered fixture per artifact class | FR-015, FR-014 | TC-SC-007 | `scripts/tests/test-bundle-negative.sh`; `scripts/verify-evidence-bundle.sh`; `contracts/evidence-bundle.schema.json` |
| SC-008 | A clean Linux host follows `quickstart.md` and completes within 30 minutes of the first command, excluding one-time acquisition, without editing any file | FR-001–FR-005, FR-023 | TC-SC-008 | `scripts/tests/test-verify-cli.sh`; `quickstart.md`; timed manual run (T139) |
| SC-009 | Flaky attempt-1 failure + later pass exits `0` with `flaky: true`; 2 flaky runs in the rolling window create a candidate; no flaky/quarantined run is promoted to `machine-pass`; no runner path writes `verified` | FR-011, FR-016 | TC-SC-009 | `scripts/tests/test-flaky.sh`; `scripts/tests/test-quarantine.sh`; `scripts/tests/test-promotion.sh` |
| SC-010 | After one discarded warmup, each measured per-location elapsed value is ≤ 5 ms or the run fails; young-generation collection-count delta recorded for 100% of measured runs | FR-013 | TC-SC-010 | `scripts/tests/test-performance.sh`; `scripts/lib/performance.sh` |
| SC-011 | K ≥ 30 concurrent instances use K distinct display identities and K distinct save-copy paths, with 0 interleaved verdicts and 0 hash mismatches | FR-020, C-CON-21 | TC-SC-011 | `scripts/tests/test-stress.sh`; `scripts/tests/stress/run_stress.sh` |
| SC-012 | 100% of tampered archives (traversal, oversized, duplicate member, wrong digest) are rejected before launch with `integrity-failure.json` and no retry | FR-024, FR-025, C-SEC-28 | TC-SC-012 | `scripts/tests/test-extraction.sh`; `scripts/tests/test-acquisition.sh`; `scripts/tests/test-bundle-negative.sh` |
| SC-013 | Quarterly sample (≥30 bundles) shows durable-store receipt, valid digest, ≥365-day retention; early deletion denied and audited | FR-027, C-SEC-24, C-SEC-30 | TC-SC-013 | `scripts/tests/test-retention.sh`; `scripts/lib/retention.sh`; manual audit drill (T140) |
| SC-014 | A second maintainer on a different Linux host reproduces the verdict in 3/3 runs; any divergence produces an Anomaly Report with owner/date/root cause before `machine-pass` eligibility | FR-001, FR-006, FR-023 | TC-SC-014 | `verification/second-host.log` (T138); `scripts/tests/test-stress.sh` |

## Coverage Summary

The matrix maps every CHK to a governing requirement, test case, and intended code location. **Mapping is not proof:** a checklist item is `[x]` only when its requirements-quality criterion is actually satisfied, and a test row is only "covered" once the test exists and passes. The final audit/re-audit (2026-10-27) closed every gap. Two user decisions shaped the result: CHK003 is a layered specification (non-technical front matter, precise normative body); CHK015 is spec-time measurability/verification-method/trace, with actual run evidence tracked at Gate 3.

| Checklist | Items | Mapped | Audit-pass | Audit-fail | Audit-unclear | N/A |
|-----------|-------|--------|------------|------------|---------------|-----|
| requirements.md | 16 | 16 | 16 | 0 | 0 | 0 |
| scenario-contract.md | 55 | 55 | 55 | 0 | 0 | 3 |
| concurrency.md | 49 | 49 | 49 | 0 | 0 | 0 |
| reliability.md | 59 | 59 | 59 | 0 | 0 | 0 |
| security.md | 53 | 53 | 53 | 0 | 0 | 0 |
| **Total** | **232** | **232** | **232** | **0** | **0** | **3** |

The scenario-contract N/A count (3) is included in the 55 pass total; those three items are legitimately not applicable in Phase 1 and are documented as such.

Concurrency CHK001–CHK049 now have one row each. When a test is renamed or a code path moves, update the row in the same PR (enforced by review, not automation).
