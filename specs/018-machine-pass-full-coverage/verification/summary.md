# 018 — Verification Summary (honest status)

**Last updated:** 2026-11-08 (pass 20, ciclo 80/20 MDMV adaptado)
**Scope:** aggregate status only. `machine-pass` means the runner promoted a host run with complete, agreeing declared signals and honesty level R. It is **not** human verification, **not** D1, and **not** SC-004.

## Deterministic gates (pass 10 baseline + pass 12 additions)

| Gate | Command | Result |
|---|---|---|
| Formatting (pass 12) | `GamePath=/home/luis/stardew-assemblies dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **exit 0** |
| Full test suite (pass 12) | `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-restore` | **1937 passed / 4 skipped / 0 failed** (1941 total, 2m36s) |
| Targeted mulch tests (pass 12) | `dotnet test ... --filter "MulchApplicationServiceTests\|MulchVerificationRulesTests\|LivingRootsModApiTests"` | **37/37 passed** (33 new) |
| Docs markers (pass 12) | `./scripts/verify-scenario.sh --check-docs` | **exit 0** |
| Doc links | `bash scripts/tests/test-doc-links.sh` | **PASS** |
| Full smoke suite (pass 10 baseline) | `SMOKE_SKIP_INTEGRATION=1 ./scripts/tests/run-all.sh` | **pass 10: 50 files, 49 passed, 0 failed, 1 skipped, exit 0** (`/tmp/opencode/smoke-run-all-pass10.txt`) |
| Full smoke suite (pass 12) | same, `TMPDIR` repo-relative first, then `test-preflight.sh` with `TMPDIR` outside the repo | **exit 1 — 48 passed / 1 failed (`test-preflight` T060 ×2, artifact of `TMPDIR` inside repo) / 1 skipped**; `test-logparse` PASS (167/167 standalone); `test-preflight` rerun with `TMPDIR=/home/luis/.smoke-tmp-lrv`: **379/380** (only `T081` `/proc`-exe fork/exec race; passed in the full run). No code regression; smoke not re-certified green from one clean full run on this host — rerun before pre-push |
| Mutation checks (pass 11) | 004 `SoilGrowthPoorMultiplier` and 013 `MulchDurationDays` mutants, real headless runs (2 of 19 scenarios) | **2/2 mutants killed** at the expected predicate; exit 19; source restored and rebuilt |
| Mutation checks (pass 13) | 005 `SoilGrowthHealthyMultiplier 0.85→1.00`, 017 `AgrochemicalSoilPenalty 10→20` (2 of 19 more) | **2/2 mutants killed**; exit 19; source restored, post-restore reruns pass |
| Mutation checks (pass 15) | 013 `MulchDecayMultiplier 0→1`, 014 `HarvestQualityService` Poor `ForceQualityZero` (decision seam); 1 selection-layer mutant discarded (`SoilQualityHealthyBoost=0`, 12× unmeasured-actual) | **2 clean kills** (single-assertion for 014); exit 19; sources restored, post-restore reruns pass |
| Mutation checks (pass 16) | 005-poor `SoilGrowthPoorMultiplier 1.15→1.00`, 017-clamped `AgrochemicalSoilPenalty 10→0` (2 of 19 more) | **2/2 mutants killed**; exit 19; source restored, post-restore reruns pass |
| Mutation checks (pass 17) | 015-harmony & 014-zero (`HarvestQualityService` Poor `ForceQualityZero`, single-assertion each), 004-healthy `SoilGrowthHealthyMultiplier 0.85→1.00` (3 of 19 more) | **3/3 mutants killed**; exit 19; sources restored, post-restore reruns pass |
| Mutation checks (pass 18) | 004-moderate & 005-moderate (shared edge `ModerateHealthyBandEdge 71→0`) | **2/2 mutants killed**; exit 19; source restored, post-restore reruns pass |
| Mutation checks (pass 19) | 017-unknown-baseline `AgrochemicalSoilPenalty 10→20` | **1 clean kill** (predicate `soilHealth eq 90` false); exit 19; source restored, post-restore reruns pass; refused-at-minimum recorded as deterministic fail (not covered) |
| Mutation checks (pass 20) | 014-unknown composite NaN-branch `Decide` flip (Tier=Poor, IsInScope, ForceQualityZero) | **1 clean kill** (singled-asserted at predicate-2, `unknownHarvestQuality eq 1`); exit 19; source restored, post-restore reruns pass; one inert mutant attempt documented (not a kill) |
| Determinism 2× (pass 11) | 004 and 013 rerun on the same seed (2 of 19 scenarios) | **semantic PASS** both; raw logs not byte-identical (documented variance) |
| Determinism 2× (pass 13) | 005 healthy and 017 tradeoff rerun on the same seed (2 of 19 more) | **semantic PASS** both; filtered logs: 005 identical, 017 one TRACE line swapped position |
| Determinism 2× (pass 15) | 013 bare-tile and 014 harvest rerun on the same seed (2 of 19 more) | **semantic PASS** both; filtered logs identical (93/93, 34/34 lines), **0 diffs** |
| Determinism 2× (pass 16) | 005-poor and 017-clamped rerun on the same seed (2 of 19 more) | **semantic PASS** both; 005 filtered 80/80 with 2 diff lines (1 identical Overlay-renderer line swapped, load-order); 017 filtered 37/37, **0 diffs** |
| Determinism 2× (pass 17) | 015, 014-zero, 004-healthy rerun on the same seed (3 of 19 more) | **semantic PASS** all; 015 filtered 34/34 **0 diffs**; 014-zero 36/36, 4 diff lines (identical lines swapped, load-order); 004-healthy 82/83, 1 identical `Pruned orphan bin record` extra |
| Determinism 2× (pass 18) | 004-moderate and 005-moderate rerun on the same seed (2 of 19 more) | **semantic PASS** both; 004 filtered 83/83 **0 diffs**; 005 81/80, 1 identical `Pruned orphan bin record` extra in run 1 |
| Determinism 2× (pass 19) | 017-unknown-baseline rerun on the same seed; 017-refused-at-minimum rerun as failure record | **semantic PASS** for unknown-baseline (37/37 lines identical, **0 diffs**); refused-at-minimum **deterministic fail** at predicate-1 |
| 017 scenarios | `bash scripts/tests/test-017-agrochemical.sh` | **23/23 passed** |
| 017 claims | `bash scripts/tests/test-017-claims.sh` | **PASS** (12 rows, 19 paths, 2 digests) |
| Scenario `modVersion` | `specs/**/*.scenario.json` sweep | **69 declarations, all `0.1.0`** |

The single smoke skip is `test-integration.sh`, intentionally skipped by `SMOKE_SKIP_INTEGRATION=1`; real-game integration is covered by Gate 3 runs, not by the launch-free suite. Smoke logs are local artifacts (`/tmp/opencode/smoke-run-all-pass10.txt`, `/tmp/opencode/smoke-run-all-pass12-full.txt`, `/tmp/opencode/test-preflight-pass12.log`, `/tmp/opencode/test-logparse-pass12-long.log`), not committed. **Pass 12 smoke is not claimed green** (see cycle record): the `T060` failure was my `TMPDIR` choice; `T081` is a fork/exec race; `test-logparse` passed.

## Evidence integrity

- **17/17** distinct bundle IDs cited in `specs/*/verification/claim-records.md` (004/005/013/014/015/017) are present locally and pass `./scripts/verify-scenario.sh --verify-bundle` (exit 0).
- **74** real local bundles (with `manifest.json`): **62 OK, 12 legacy failures** (7 missing `schemaVersion`; 5 `retention-receipt.json` outside the subject; era 2026-11-04/05).
- **94** additional `evidence-bundle-run-*` directories contain only `artifacts/` + `attempts/` (partial attempts without manifest; not evidence bundles).
- Of the 3 failing bundles still referenced anywhere in `specs/`, provenance is historical only: `4acd`/`0a6b` are past promotion transitions superseded by the current 004 GATE3 pointer `535d` (verify OK); `07ba` is a 010 observation pointer already annotated as legacy/incompatible in pass 5. None is cited by a current claim-record or feature GATE3. Decision: retain as historical, never delete, rewrite, or re-promote.
- **Pass 11:** all **8/8** bundles produced in this cycle pass `--verify-bundle` (exit 0): four determinism bundles, two mutation negative controls, two post-restore revalidations. Mutation bundles carry `honestyLevel: W` / `verdict: errored` by design and are never cited as machine-pass.

## Claim population

- **6** specs carry a `machine-pass` GATE3 block: 004, 005, 013, 014, 015, 017. Each has `verification/claim-records.md` and `verification/host-only-gate3.md`.
- **Blocked/pending, never `machine-pass`:** farmhand guard / D1 transport (T002 split-screen `isMainPlayer` flip not demonstrated), SC-004 visual comprehension, human `verified` attestation, render legibility, and balance/playtest.
- 018 is an aggregation feature and has no machine claim of its own beyond the per-feature records.

## Residual work

- **T034/T035 advanced through passes 11, 13, 15, 16, 17, 18, 19 and 20.** Pass 11 covered 004/013-expiry; pass 13 covered 005-healthy and 017-tradeoff; pass 15 covered 013-bare-tile and 014; pass 16 covered 005-poor and 017-clamped; pass 17 covered 015-harmony, 014-zero and 004-healthy; pass 18 covered 004-moderate and 005-moderate; pass 19 covered 017-unknown-baseline; pass 20 added 2× determinism for 013-decay-exemption, 013-refused-on-ineligible, 014-unknown (all green) plus 017-refused-contention, and a clean mutation kill for 014-unknown. **Determinism coverage is now 18 of 19** (only `017-refused-at-minimum` is not green); mutation kill coverage is **15 of 19** (013-decay-exemption, 013-refused-on-ineligible, 017-refused-contention have 2× evidence but no feasible decision-seam kill; refused-at-minimum's stale assertion is not mutation evidence). Honest note: `agrochemical-refused-at-minimum` now fails deterministically at predicate-1 (`cropPhaseDelta ×1.0`, pre 4→actual 5) on the current build — the historical machine-pass `run-20261107T135531Z-7c4e` was a capture-order flake; the assertion as written is stale. See `verification/mutation-checks.md` and `verification/determinism-checks.md`. Log-level bit-identity is not claimed.
- **T006/T007 closed in pass 12.** 33 new unit cases cover `MulchApplicationService` eligibility/refusal/lifetime/persistence, the pure `MulchVerificationRules` host/count gate and bounded slot placement, and `ILivingRootsModApi.ApplyMulch` bound/unbound/throwing/disposed behavior. RED was demonstrated (`CS0103` before the rule existed); targeted 37/37 and full suite 1937/4/0 are GREEN. Honest limit: `GrantMulch`'s item-creation success path is Gate-3-attested, not unit-tested, and farmhand gating is pure-rule-only.
- **12 legacy bundles** remain as historical artifacts under a retain-only policy; no regeneration was attempted.
- **94 partial attempts** are not bundles and are not cited as evidence.
- **Smoke suite re-certification:** pass 12 did not produce a clean full-suite green on this host (T060 harness/TMPDIR artifact; T081 fork/exec race; test-logparse 167/167). Required before any pre-push on a quiet host.

## Pointers

- `verification/cycle-2026-11-08-80-20-pass20.md` — pass 20 detailed record (T034/T035 extended to 013-decay-exemption/013-refused-on-ineligible/014-unknown/017-refused-contention; determinism 18/19, kill 15/19).
- `verification/cycle-2026-11-08-80-20-pass19.md` — pass 19 detailed record (T034/T035 extended to 017-unknown-baseline; coverage 14/19; refused-at-minimum deterministic fail).
- `verification/cycle-2026-11-08-80-20-pass18.md` — pass 18 detailed record (T034/T035 extended to 004/005-moderate; coverage 13/19).
- `verification/cycle-2026-11-08-80-20-pass17.md` — pass 17 detailed record (T034/T035 extended to 015/014-zero/004-healthy; coverage 11/19).
- `verification/cycle-2026-11-08-80-20-pass16.md` — pass 16 detailed record (T034/T035 extended to 005-poor/017-clamped; coverage 8/19).
- `verification/cycle-2026-11-08-80-20-pass15.md` — pass 15 detailed record (T034/T035 extended to 013-bare-tile/014; coverage 6/19; one discard).
- `verification/cycle-2026-11-08-80-20-pass13.md` — pass 13 detailed record (T034/T035 extended to 005/017; coverage 4/19).
- `verification/cycle-2026-11-08-80-20-pass12.md` — pass 12 detailed record (T006/T007 closed, 33 new tests, 013 expiry + refusal Gate 3).
- `verification/cycle-2026-11-08-80-20-pass11.md` — pass 11 detailed record (T034/T035 partial advance, 2/19 scenarios).
- `verification/mutation-checks.md` — mutation negative controls (004/013).
- `verification/determinism-checks.md` — determinism 2× evidence (004/013).
- `verification/cycle-2026-11-07-80-20-pass10.md` — pass 10 detailed record.
- `verification/final-inventory.md` — feature × path inventory and addenda.
- `verification/t002-farmhand-host-guard-attempt.md` — farmhand/split-screen evidence and `DebugCommands.TryHandle` lead.
