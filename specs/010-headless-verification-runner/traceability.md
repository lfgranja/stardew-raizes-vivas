# Traceability Matrix: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Created**: 2026-10-27
**Purpose**: Link every checklist item (CHK) to its governing requirement, planned test case, and code location. Satisfies C-CON-17, C-CON-21, C-REL-19, C-SCN-06, C-SCN-10, and the traceability items in each checklist.

**Status**: Test-case IDs are planned (Phase 2 `/speckit-tasks` assigns implementation order); code locations are the intended files. A row is satisfied only when the test exists and passes; this matrix is the contract the implementation must fill in.

## Requirements Quality (`checklists/requirements.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001 | Content quality: tools named are the feature's subject | TC-REQ-001 | `spec.md` review |
| CHK002 | User value / business need | TC-REQ-002 | `spec.md` US1–US3 |
| CHK003 | Non-technical stakeholder readability | TC-REQ-003 | `spec.md` review |
| CHK004 | Mandatory sections complete | TC-REQ-004 | `spec.md` template |
| CHK005 | No `[NEEDS CLARIFICATION]` markers | TC-REQ-005 | `spec.md` grep gate |
| CHK006 | Testable/unambiguous requirements | TC-REQ-006 | `spec.md` FR/NFR |
| CHK007 | Measurable success criteria | TC-REQ-007 | `spec.md` SC-001..014 |
| CHK008 | Technology-agnostic success criteria | TC-REQ-008 | `spec.md` SC review |
| CHK009 | Acceptance scenarios defined | TC-REQ-009 | `spec.md` US1–US3 |
| CHK010 | Edge cases identified | TC-REQ-010 | `spec.md` Edge Cases |
| CHK011 | Scope bounded | TC-REQ-011 | `spec.md` NFR/Assumptions/Out of Scope |
| CHK012 | Dependencies and assumptions identified | TC-REQ-012 | `spec.md` A1–A16 |
| CHK013 | Functional requirements have acceptance criteria | TC-REQ-013 | `spec.md` FR-001..FR-027 |
| CHK014 | User scenarios cover primary flows | TC-REQ-014 | `spec.md` US1–US3 |
| CHK015 | Feature meets measurable outcomes | TC-REQ-015 | `spec.md` SC-001..014 |
| CHK016 | No implementation leakage | TC-REQ-016 | `spec.md` review |

## Scenario Contract (`checklists/scenario-contract.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK007 | FR-007; C-SCN-36 (required-field set); strict `additionalProperties: false`; IEEE 829 mapping (C-SCN-17); conditional `if/then` | TC-SCN-001..007 | `contracts/scenario.schema.json`; `LivingRoots.Verification/Validation/ScenarioValidator.cs` |
| CHK008–CHK014 | FR-010; C-SCN-29 (structured `predicate`, inclusivity, typed operands, sentinel enum); boundary-value cases | TC-SCN-008..014 | `contracts/scenario.schema.json#/$defs/predicate`; `scripts/tests/thresholds.bats` |
| CHK015–CHK020 | FR-006/FR-009; ordered `steps`, explicit `environment`, `rngSeed`, `time`; idempotency; ≤5 steps | TC-SCN-015..020 | `contracts/scenario.schema.json`; `scripts/verify-scenario.sh` |
| CHK021–CHK025 | FR-006/FR-009; typed `initCommands` (C-SCN-33); injection lifecycle (C-SCN-30); versioned `ILivingRootsModApi` | TC-SCN-021..025 | `contracts/scenario.schema.json`; `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs` |
| CHK026–CHK030 | SC-001..014; C-SCN-06 (machine-evaluable acceptance, traceability) | TC-SCN-026..030 | `spec.md` SC; `traceability.md` |
| CHK031–CHK035 | FR-010; C-SCN-37 (missing vs. 0 vs. NaN vs. sentinel); sentinel registry; equivalence classes | TC-SCN-031..035 | `contracts/scenario.schema.json#/$defs/sentinel`; `scripts/tests/thresholds.bats` |
| CHK036–CHK040 | FR-007; C-SCN-31 (BACKWARD_TRANSITIVE, immutable `vN`, bump discipline, migration) | TC-SCN-036..040 | `contracts/scenario.schema.json`; `schemas/scenarios/v2/` |
| CHK041–CHK045 | FR-007/FR-022; `$defs`/`$ref`; parameterization; C-SCN-38 cross-validator agreement | TC-SCN-041..045 | `contracts/scenario.schema.json`; `fixtures/valid/` and `fixtures/invalid/`; `scripts/tests/schema-contract.bats` |
| CHK046–CHK050 | FR-007; `title`/`description`/`examples`; short scenarios; machine validity independent of prose | TC-SCN-046..050 | `contracts/scenario.schema.json`; `contracts/test-md-template.md` |
| CHK051–CHK055 | C-SCN-10 (necessary, unambiguous, consistent, verifiable, traceable) | TC-SCN-051..055 | `spec.md`; this matrix |

## Concurrency (`checklists/concurrency.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001 | Atomic display allocation via `-displayfd` (no TOCTOU) — FR-020, C-CON-01 | TC-CON-001 | `scripts/verify-scenario.sh` (`display_start`); `scripts/tests/concurrency.bats` |
| CHK002 | No `/tmp/.X*-lock` probing; rationale documented — FR-020, C-CON-01 | TC-CON-002 | `CONCURRENCY.md` §Display; `scripts/tests/concurrency.bats` |
| CHK003 | Per-instance display number — FR-020, C-CON-01 | TC-CON-003 | `scripts/verify-scenario.sh` (`display_start`) |
| CHK004 | Server-side socket removal on cleanup — FR-020, C-CON-01/C-CON-22 | TC-CON-004 | `scripts/verify-scenario.sh` (`display_stop`); `scripts/tests/orphans.bats` |
| CHK005 | Orphan detection is report-only (no signalling) — C-CON-01/C-CON-22 | TC-CON-005 | `scripts/verify-scenario.sh` (`orphan_registry`); `scripts/tests/orphans.bats` |
| CHK006 | Disposable save copy, never the player's save — FR-018, C-CON-20 | TC-CON-006 | `scripts/verify-scenario.sh` (`save_snapshot`) |
| CHK007 | Byte-identical original via SHA-256 before/after — FR-018, SC-005, C-CON-02 | TC-CON-007 | `scripts/verify-scenario.sh` (`save_hash`) |
| CHK008 | SHA-256 for saves; 0.0001f float tolerance for deltas — FR-012, C-CON-02 | TC-CON-008 | `LivingRoots/Services/SaveComparator.cs`; `scripts/tests/save-preservation.bats` |
| CHK009 | Backup of `Saves/<Name>/` and sidecars before mutation — FR-018, C-CON-20 | TC-CON-009 | `scripts/verify-scenario.sh` (`save_snapshot`) |
| CHK010 | Non-identical save → `errored` with before/after — FR-018, C-CON-03 | TC-CON-010 | `scripts/verify-scenario.sh` (`save_restore`); `scripts/tests/save-preservation.bats` |
| CHK011 | Atomic sidecar writes (temp→fsync→rename→fsync) — C-CON-04 | TC-CON-011 | `scripts/lib/atomic.sh`; `scripts/tests/atomic.bats` |
| CHK012 | `XDG_CONFIG_HOME` per-instance absolute temp dir — FR-018, C-CON-05 | TC-CON-012 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK013 | Temp root via `mktemp -d` — FR-020, C-CON-07 | TC-CON-013 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK014 | `HOME` redirected when XDG is ignored — C-CON-05 | TC-CON-014 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK015 | Trap/finally cleanup with observable failure — FR-019, C-CON-06 | TC-CON-015 | `scripts/verify-scenario.sh` (`cleanup`) |
| CHK016 | Runtime dirs 0700 — C-CON-07 | TC-CON-016 | `scripts/verify-scenario.sh` (`isolate_env`); `scripts/tests/isolation.bats` |
| CHK017 | No whole-run `flock`; short-lived resource locks only — FR-020, C-CON-08 | TC-CON-017 | `scripts/lib/concurrency.sh`; `CONCURRENCY.md` |
| CHK018 | Advisory locks only — C-CON-08 | TC-CON-018 | `scripts/lib/concurrency.sh`; `scripts/tests/locking.bats` |
| CHK019 | Bounded non-blocking lock acquisition — C-CON-08/C-CON-23 | TC-CON-019 | `scripts/lib/concurrency.sh` (`claim_acquire`) |
| CHK020 | OFD locks preferred — C-CON-08/C-CON-23 | TC-CON-020 | `scripts/lib/concurrency.sh` |
| CHK021 | Global lock order documented — C-CON-08/C-CON-23 | TC-CON-021 | `CONCURRENCY.md` §Lock Order |
| CHK022 | Conflict detection scoped to instance's own game process — FR-020, C-CON-09 | TC-CON-022 | `scripts/verify-scenario.sh` (`preflight_process_conflict`) |
| CHK023 | Atomic check-and-claim (`O_EXCL`, never check-then-create) — C-CON-09 | TC-CON-023 | `scripts/lib/concurrency.sh` (`claim_acquire`); `scripts/tests/process-conflict.bats` |
| CHK024 | PID liveness via identity token, not `kill -0` alone — C-CON-09 | TC-CON-024 | `scripts/lib/concurrency.sh` (`pid_identity`) |
| CHK025 | Distinct instance IDs — C-CON-10 | TC-CON-025 | `scripts/verify-scenario.sh` (`instance_init`) |
| CHK026 | Orphan display detect-and-report only — C-CON-01/C-CON-22 | TC-CON-026 | `scripts/verify-scenario.sh` (`orphan_registry`); `scripts/tests/orphans.bats` |
| CHK027 | Child reaping (`waitpid`, SIGTERM→SIGKILL) — FR-019, C-CON-11 | TC-CON-027 | `scripts/verify-scenario.sh` (`cleanup`) |
| CHK028 | Signal forwarding via process group — FR-019, C-CON-11 | TC-CON-028 | `scripts/verify-scenario.sh` (`cleanup`) |
| CHK029 | Stale-PID safety: never signal without token match — C-CON-09 | TC-CON-029 | `scripts/lib/concurrency.sh` (`pid_identity`) |
| CHK030 | `mktemp -d`, not deprecated `tempfile` — FR-020, C-CON-07 | TC-CON-030 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK031 | Trap/finally cleanup with observable failure — FR-019, C-CON-06 | TC-CON-031 | `scripts/verify-scenario.sh` (`cleanup`) |
| CHK032 | Temp dirs on local filesystem only — C-CON-12 | TC-CON-032 | `scripts/verify-scenario.sh` (`assert_local_fs`) |
| CHK033 | Temp dir permissions 0700 — C-CON-07 | TC-CON-033 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK034 | Unique instance namespace; no shared writable run state — C-CON-10 | TC-CON-034 | `scripts/verify-scenario.sh` (`instance_init`) |
| CHK035 | Dynamic display allocation — FR-020, C-CON-01 | TC-CON-035 | `scripts/verify-scenario.sh` (`display_start`) |
| CHK036 | Sidecar isolation via distinct temp roots — FR-020, C-CON-10 | TC-CON-036 | `scripts/verify-scenario.sh` (`isolate_env`) |
| CHK037 | Correct, non-interleaved verdicts (D1–D5) — C-CON-10/C-CON-21 | TC-CON-037 | `scripts/tests/stress-concurrency.sh` |
| CHK038 | Hash-verified backup before mutation — FR-018, C-CON-20 | TC-CON-038 | `scripts/verify-scenario.sh` (`save_snapshot`) |
| CHK039 | Idempotent restore — FR-018, C-CON-13 | TC-CON-039 | `scripts/verify-scenario.sh` (`save_restore`) |
| CHK040 | Atomic restore — C-CON-13 | TC-CON-040 | `scripts/verify-scenario.sh` (`save_restore`); `scripts/tests/atomic.bats` |
| CHK041 | Restore failure leaves original untouched — FR-018, C-CON-13 | TC-CON-041 | `scripts/verify-scenario.sh` (`save_restore`) |
| CHK042 | `O_EXCL` paired with `O_CREAT` for locks/temp — C-CON-14 | TC-CON-042 | `scripts/lib/concurrency.sh`; `scripts/lib/atomic.sh` |
| CHK043 | `fsync` data before rename, parent after — C-CON-04/C-CON-14 | TC-CON-043 | `scripts/lib/atomic.sh`; `scripts/tests/atomic.bats` |
| CHK044 | Torn-write prevention for all persisted artifacts — C-CON-04 | TC-CON-044 | `scripts/lib/atomic.sh`; `scripts/tests/atomic.bats` |
| CHK045 | Atomic operations documented with failure modes — C-CON-15 | TC-CON-045 | `CONCURRENCY.md` |
| CHK046 | Local/CI parity — FR-023, C-CON-16 | TC-CON-046 | `scripts/tests/` |
| CHK047 | Concurrency tests (Coyote, FsCheck, bats, stress) — C-CON-19 | TC-CON-047 | `LivingRoots.Tests/`; `scripts/tests/` |
| CHK048 | CHK → test → code traceability — C-CON-17 | TC-CON-048 | this matrix |
| CHK049 | TOCTOU review of every primitive — C-CON-18 | TC-CON-049 | `CONCURRENCY.md` §9 |

## Reliability (`checklists/reliability.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK005 | FR-011; retryable/non-retryable sets; verdict-state eligibility; idempotency gate; `errored`/`partial` vs. `fail` | TC-REL-001..005 | `scripts/verify-scenario.sh` (`classify_retry`); `scripts/tests/retry.bats` |
| CHK006–CHK011 | FR-011; C-REL-04 (per-scenario + suite caps); fixed 5 s cooldown; per-attempt artifacts; stability across SMAPI | TC-REL-006..011 | `scripts/verify-scenario.sh` (`run_with_retries`); `scripts/tests/retry.bats` |
| CHK012–CHK016 | FR-011; C-REL-07/C-REL-13/C-REL-26 (derived `flaky`, N≥30 Wilson gate, RAFT, expiry) | TC-REL-012..016 | `scripts/verify-scenario.sh` (`derive_flaky`); `scripts/tests/flaky.bats` |
| CHK017–CHK023 | FR-011; C-REL-10/C-REL-14/C-REL-21/C-REL-23/C-REL-27 (trigger, 28-day cap, auto-disable, owner chain, never-quarantine, human approval, `<skipped>`) | TC-REL-017..023 | `specs/010-headless-verification-runner/quarantine.json`; `scripts/verify-scenario.sh` (`quarantine_gate`); `scripts/tests/quarantine.bats` |
| CHK024–CHK029 | FR-011; NFR-003; C-REL-01 (closed five-base verdict set + derived `flaky`; crash/incomplete/disagreement; exit mapping) | TC-REL-024..029 | `contracts/exit-codes.md`; `scripts/verify-scenario.sh` (`compute_verdict`) |
| CHK030–CHK034 | FR-016; C-REL-15 (promotion only on full triad `pass`; no auto-`E`; re-promotion = new run) | TC-REL-030..034 | `scripts/verify-scenario.sh` (`promote_test_md`); `scripts/tests/promotion.bats` |
| CHK035–CHK040 | FR-014/FR-015; C-REL-24 (W/R/E definitions, sequential assignment, bounded claims, `claims.json`) | TC-REL-035..040 | `scripts/verify-evidence-bundle.sh`; `scripts/tests/claims.bats` |
| CHK041–CHK045 | FR-009; C-REL-12 (three signals, divergence hard stop, no single-source pass, necessary-not-sufficient, independence caveat) | TC-REL-041..045 | `scripts/verify-scenario.sh` (`evaluate_signals`); `scripts/tests/signals.bats` |
| CHK046–CHK050 | FR-014/FR-015; SC-005; C-BND-01 (minimum evidence set, byte-identical save, per-attempt artifacts, digest recompute, offline verification) | TC-REL-046..050 | `scripts/verify-evidence-bundle.sh`; `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` |
| CHK051–CHK055 | FR-009/FR-011/FR-013 (false-positive control, verdict separation, warmup, `gc-attributed`, flaky tracking) | TC-REL-051..055 | `scripts/verify-scenario.sh`; `scripts/tests/performance.bats` |
| CHK056–CHK059 | FR-023; C-REL-19/C-REL-20 (local/CI parity, verdict tests, traceability, tiered review) | TC-REL-056..059 | `scripts/tests/`; this matrix; `GOVERNANCE-ANNEX.md` |

## Security (`checklists/security.md`)

| CHK | Requirement | Test case | Code location |
|-----|-------------|-----------|---------------|
| CHK001–CHK007 | FR-002; C-SEC-14/C-SEC-15/C-SEC-16/C-SEC-21/C-SEC-26 (scope, 90-day max/30-day rotation, storage, preflight validation, no token logs, OIDC upgrade, no sharing) | TC-SEC-001..007 | `scripts/verify-scenario.sh` (`acquire_game_files`); `scripts/tests/acquisition.bats` |
| CHK008–CHK014 | FR-002; C-SEC-02/C-SEC-17/C-SEC-18/C-SEC-23/C-SEC-28 (SHA-256, before/after extraction, `gamepath.lock.json`, signature deferral, mismatch response, archive validation, tamper record) | TC-SEC-008..014 | `gamepath.lock.json`; `scripts/verify-scenario.sh` (`verify_archive`); `scripts/tests/integrity.bats` |
| CHK015–CHK020 | FR-001; C-SEC-22/C-SEC-25 (cache trust tuple, per-job `cache-mode`, untrusted re-verification, content-hash key, no secrets in cache, branch isolation) | TC-SEC-015..020 | `scripts/verify-scenario.sh` (`cache_gate`); `.github/workflows/build_and_test.yml` |
| CHK021–CHK026 | FR-002; C-SEC-04/C-SEC-12 (external input validation, traversal rejection, Form C/homoglyph, archive members, no path disclosure, no env interpolation) | TC-SEC-021..026 | `scripts/lib/sanitize.sh`; `LivingRoots/Services/ModDataService.cs` (`SanitizePathSegments`) |
| CHK027–CHK030 | NFR-004; C-SEC-05/C-SEC-06/C-SEC-30 (redaction, no plaintext/encoded secrets, leaked-secret retention, audit vs. application logs) | TC-SEC-027..030 | `scripts/lib/logging.sh`; `scripts/tests/logging.bats` |
| CHK031–CHK035 | FR-014/FR-015; C-SEC-29/C-BND-01 (mandatory commands/materials/seeds, signing deferred, minimum evidence, offline verifier, retention/immutability) | TC-SEC-031..035 | `scripts/verify-evidence-bundle.sh`; `manifest.json` schema |
| CHK036–CHK039 | FR-002; C-SEC-18/C-SEC-26 (minimal scopes, non-root, runner-group segregation, read-only `GITHUB_TOKEN`) | TC-SEC-036..039 | `.github/workflows/build_and_test.yml`; `GOVERNANCE-ANNEX.md` |
| CHK040–CHK044 | FR-002/FR-007/FR-014; C-SEC-28 (fail-closed integrity, missing evidence, unknown fields, partial download, override approval) | TC-SEC-040..044 | `scripts/verify-scenario.sh`; `scripts/verify-evidence-bundle.sh`; `scripts/tests/fail-closed.bats` |
| CHK045–CHK049 | NFR-004; C-SEC-24/C-SEC-30 (audit events, JSON fields, sensitive-data exclusion, aggregation SHOULD, retention/immutability) | TC-SEC-045..049 | `scripts/lib/logging.sh`; `GOVERNANCE-ANNEX.md` |
| CHK050–CHK053 | FR-023; C-SEC-19/C-SEC-20/C-SEC-24/C-SEC-26/C-SEC-27 (local/CI parity, threat mapping, ownership, review cadence) | TC-SEC-050..053 | `GOVERNANCE-ANNEX.md`; this matrix |

## Verdict Requirements (`checklists/reliability.md` CHK024–CHK029)

| Verdict | Governing requirement | Exit | Planned test |
|---------|-----------------------|------|--------------|
| `pass` | FR-011/C-REL-01; all three signals agree, thresholds satisfied, expected verdict matches | `0` | TC-REL-024, TC-REL-029 |
| `fail` | FR-011/C-REL-01; threshold/assertion violation or host-guard breach; matched `expectedVerdict: fail` | `18` | TC-REL-025, TC-REL-029 |
| `errored` | FR-011/C-REL-01/C-CON-03; crash, capture failure, save-preservation breach, version mismatch, expected mismatch | `19` | TC-REL-026, TC-REL-029 |
| `partial` | FR-011/C-REL-12; incomplete signal set or signal disagreement | `19` | TC-REL-027, TC-REL-029 |
| `skipped` | FR-011/C-REL-10; quarantined or intentionally not run | `20` all-skipped / `0` mixed | TC-REL-028, TC-REL-029 |
| `flaky` (derived) | FR-011/C-REL-13/C-REL-26; attempt-1 retryable failure + later pass on same commit/seed/environment | `0`; never promotes | TC-REL-012..016 |

## Success Criteria (`spec.md` SC-001..SC-014)

| SC | Measurable outcome | Governing requirements | Test case |
|----|--------------------|------------------------|-----------|
| SC-001 | One documented invocation completes feature 004's scenario with zero keystrokes and all mandatory artifacts | FR-001–FR-014, FR-019, FR-022, FR-023 | TC-SC-001 |
| SC-002 | 100% of `pass` host runs have all three signal artifacts and digests; fewer than three signals never `pass` | FR-009, FR-011, FR-014 | TC-SC-002 |
| SC-003 | Boundary tiers 33/34/66/67 yield 1.15/1.00/1.00/0.85, clamped ≥ 1; ≤0/missing/NaN unchanged; sentinel untouched | FR-010, FR-012 | TC-SC-003 |
| SC-004 | A second feature scenario is added with zero runner-source changes and the same bundle schema | FR-022, FR-014 | TC-SC-004 |
| SC-005 | Source milestone save and restored working copy hash to their pre-run SHA-256; mismatch is `errored` | FR-018, FR-020 | TC-SC-005 |
| SC-006 | Every emittable code is covered: 0, 9, 10, 12, 13, 14, 18, 19, 20, 21; inherited pass-through 1–7/15–17; 8 retired; 11 never emitted | FR-011, NFR-003 | TC-SC-006 |
| SC-007 | Offline verifier recomputes every digest and rejects any single tampered byte per artifact class | FR-015, FR-014 | TC-SC-007 |
| SC-008 | A clean Linux host follows `quickstart.md` and completes within 30 minutes, excluding acquisition | FR-001–FR-005, FR-023 | TC-SC-008 |
| SC-009 | Flaky attempt-1 failure + later pass exits `0` with `flaky: true`; 2 flaky runs create a candidate; no flaky/quarantined promotion | FR-011, FR-016 | TC-SC-009 |
| SC-010 | After one discarded warmup, every measured per-location elapsed value is ≤ 5 ms or the run fails; young-generation collection-count delta recorded | FR-013 | TC-SC-010 |
| SC-011 | K ≥ 30 concurrent instances use K distinct display identities and save copies, with 0 interleaved verdicts and 0 hash mismatches | FR-020, C-CON-21 | TC-SC-011 |
| SC-012 | 100% of tampered archives are rejected before launch with `integrity-failure.json` and no retry | FR-024, FR-025, C-SEC-28 | TC-SC-012 |
| SC-013 | 100% of audit/evidence artifacts export to durable storage and are retained ≥ 365 days; early deletion requires an approval event | FR-027, C-SEC-24, C-SEC-30 | TC-SC-013 |
| SC-014 | A second maintainer on a different Linux host reproduces the verdict for the same commit, save, and seed | FR-001, FR-006, FR-023 | TC-SC-014 |

## Coverage Summary

The matrix maps every CHK to a governing requirement, test case, and intended code location. **Mapping is not proof:** a checklist item is `[x]` only when its requirements-quality criterion is actually satisfied, and a test row is only "covered" once the test exists and passes. The last audit (2026-10-27) found 177 pass / 39 fail / 13 unclear / 3 N/A; those gaps are recorded in the checklists and are not hidden by this matrix. The re-audit after the reconciliation pass updates this table.

| Checklist | Items | Mapped | Audit-pass | Audit-fail | Audit-unclear | N/A |
|-----------|-------|--------|------------|------------|---------------|-----|
| requirements.md | 16 | 16 | 6 | 8 | 2 | 0 |
| scenario-contract.md | 55 | 55 | 37 | 15 | 0 | 3 |
| concurrency.md | 49 | 49 | 44 | 4 | 1 | 0 |
| reliability.md | 59 | 59 | 52 | 3 | 4 | 0 |
| security.md | 53 | 53 | 38 | 9 | 6 | 0 |
| **Total** | **232** | **232** | **177** | **39** | **13** | **3** |

Concurrency CHK001–CHK049 now have one row each. When a test is renamed or a code path moves, update the row in the same PR (enforced by review, not automation).
