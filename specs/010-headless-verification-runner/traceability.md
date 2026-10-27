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
| CHK001–CHK005 | FR-020; C-CON-01/C-CON-22 (atomic `-displayfd`, no lock probing, per-instance display, bounded cleanup, orphan detect-and-report) | TC-CON-001..005 | `scripts/verify-scenario.sh` (`display_start`, `display_stop`); `scripts/tests/concurrency.bats` |
| CHK006–CHK011 | FR-018; SC-005; C-CON-20 (hash-verified backup, disposable copy, sidecars, `errored` on mismatch, atomic sidecar writes) | TC-CON-006..011 | `scripts/verify-scenario.sh` (`save_snapshot`, `save_restore`); `scripts/tests/save-preservation.bats` |
| CHK012–CHK016 | FR-018/FR-019/FR-020; C-CON-07 (per-instance `XDG_CONFIG_HOME`, `mktemp -d`, `HOME` redirect, trap cleanup, 0700) | TC-CON-012..016 | `scripts/verify-scenario.sh` (`isolate_env`); `scripts/tests/isolation.bats` |
| CHK017–CHK021 | FR-020; C-CON-08/C-CON-23 (advisory locks only, bounded backoff, OFD preferred, lock order) | TC-CON-017..021 | `scripts/lib/concurrency.sh`; `scripts/tests/locking.bats` |
| CHK022–CHK025 | FR-020; C-CON-09/C-CON-10 (instance-scoped conflict, atomic claim, PID identity token, unique instance ID) | TC-CON-022..025 | `scripts/lib/concurrency.sh`; `scripts/tests/process-conflict.bats` |
| CHK026–CHK029 | FR-019; C-CON-01/C-CON-09 (orphan display report-only, reaping, process-group signals, no unrelated kills) | TC-CON-026..029 | `scripts/verify-scenario.sh` (`cleanup`); `scripts/tests/orphans.bats` |
| CHK030–CHK033 | FR-019/FR-020; C-CON-07/C-CON-12 (`mktemp -d`, trap/finally, local FS only, 0700) | TC-CON-030..033 | `scripts/verify-scenario.sh` (`mktemp`); `scripts/tests/tempdir.bats` |
| CHK034–CHK037 | FR-020; C-CON-10/C-CON-21 (unique namespace, dynamic display, sidecar isolation, non-interleaved verdicts D1–D5) | TC-CON-034..037 | `scripts/verify-scenario.sh`; `scripts/tests/concurrency.bats` |
| CHK038–CHK041 | FR-018; C-CON-13/C-CON-20 (hash-verified backup, idempotent restore, atomic restore, original untouched on failure) | TC-CON-038..041 | `scripts/verify-scenario.sh` (`save_restore`); `scripts/tests/save-preservation.bats` |
| CHK042–CHK045 | C-CON-04/C-CON-14/C-CON-15 (`O_EXCL` + `O_CREAT` + `O_NOFOLLOW`, fsync data+dir, torn-write prevention, documented failure modes) | TC-CON-042..045 | `scripts/lib/concurrency.sh`; `CONCURRENCY.md` |
| CHK046–CHK049 | FR-023; C-CON-17/C-CON-19/C-CON-21 (local/CI parity, Coyote+FsCheck+stress, TOCTOU review) | TC-CON-046..049 | `scripts/tests/`; `LivingRoots.Tests/` (Coyote/FsCheck) |

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

## Success Criteria (`spec.md` SC-001..SC-014)

| SC | Measurable outcome | Governing requirements | Test case |
|----|--------------------|------------------------|-----------|
| SC-001 | One documented invocation completes feature 004's scenario with zero keystrokes and all mandatory artifacts | FR-001–FR-014, FR-019, FR-022, FR-023 | TC-SC-001 |
| SC-002 | 100% of `pass` host runs have all three signal artifacts and digests; fewer than three signals never `pass` | FR-009, FR-011, FR-014 | TC-SC-002 |
| SC-003 | Boundary tiers 33/34/66/67 yield 1.15/1.15/1.00/0.85, clamped ≥ 1, sentinel untouched | FR-010, FR-012 | TC-SC-003 |
| SC-004 | A second feature scenario is added with zero runner-source changes and the same bundle schema | FR-022, FR-014 | TC-SC-004 |
| SC-005 | Source milestone save and restored working copy hash to their pre-run SHA-256; mismatch is `errored` | FR-018, FR-020 | TC-SC-005 |
| SC-006 | Every published exit code is covered by contract tests: 0, 14, 18, 19, 20, 21 | FR-011, NFR-003 | TC-SC-006 |
| SC-007 | Offline verifier recomputes every digest and rejects any single tampered byte per artifact class | FR-015, FR-014 | TC-SC-007 |
| SC-008 | A clean Linux host follows `quickstart.md` and completes within 30 minutes, excluding acquisition | FR-001–FR-005, FR-023 | TC-SC-008 |
| SC-009 | Flaky attempt-1 failure + later pass exits `0` with `flaky: true`; 2 flaky runs create a candidate; no flaky/quarantined promotion | FR-011, FR-016 | TC-SC-009 |
| SC-010 | After one discarded warmup, every measured per-location elapsed value is ≤ 5 ms or the run fails; Gen0 delta recorded | FR-013 | TC-SC-010 |
| SC-011 | K ≥ 30 concurrent instances use 30 distinct displays and save copies, with 0 interleaved verdicts and 0 hash mismatches | FR-020, C-CON-21 | TC-SC-011 |
| SC-012 | 100% of tampered archives are rejected before launch with `integrity-failure.json` and no retry | FR-024, FR-025, C-SEC-28 | TC-SC-012 |
| SC-013 | 100% of audit/evidence artifacts export to durable storage and are retained ≥ 365 days; early deletion requires an approval event | FR-027, C-SEC-24, C-SEC-30 | TC-SC-013 |
| SC-014 | A second maintainer on a different Linux host reproduces the verdict for the same commit, save, and seed | FR-001, FR-006, FR-023 | TC-SC-014 |

## Coverage Summary

| Checklist | Items | Covered | Gaps |
|-----------|-------|---------|------|
| requirements.md | 16 | 16 | 0 |
| scenario-contract.md | 55 | 55 | 0 |
| concurrency.md | 49 | 49 | 0 |
| reliability.md | 59 | 59 | 0 |
| security.md | 53 | 53 | 0 |
| **Total** | **232** | **232** | **0** |

Every CHK appears in exactly one range row above; the test-case family for a range covers each item in that range. When a test is renamed or a code path moves, update the row in the same PR (enforced by review, not automation).
