# 018 — Verification Summary (honest status)

**Last updated:** 2026-11-08 (pass 12, ciclo 80/20 MDMV adaptado)
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
| Determinism 2× (pass 11) | 004 and 013 rerun on the same seed (2 of 19 scenarios) | **semantic PASS** both; raw logs not byte-identical (documented variance) |
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

- **T034/T035 advanced in pass 11** with real game runs on a representative pair (004/013): 2/2 mutants killed and semantic determinism confirmed. This covers **2 of 19** scenario files; the full per-scenario coverage required by SC-MP-6/SC-MP-7 remains residual. See `verification/mutation-checks.md` and `verification/determinism-checks.md`. Log-level bit-identity is not claimed.
- **T006/T007 closed in pass 12.** 33 new unit cases cover `MulchApplicationService` eligibility/refusal/lifetime/persistence, the pure `MulchVerificationRules` host/count gate and bounded slot placement, and `ILivingRootsModApi.ApplyMulch` bound/unbound/throwing/disposed behavior. RED was demonstrated (`CS0103` before the rule existed); targeted 37/37 and full suite 1937/4/0 are GREEN. Honest limit: `GrantMulch`'s item-creation success path is Gate-3-attested, not unit-tested, and farmhand gating is pure-rule-only.
- **12 legacy bundles** remain as historical artifacts under a retain-only policy; no regeneration was attempted.
- **94 partial attempts** are not bundles and are not cited as evidence.
- **Smoke suite re-certification:** pass 12 did not produce a clean full-suite green on this host (T060 harness/TMPDIR artifact; T081 fork/exec race; test-logparse 167/167). Required before any pre-push on a quiet host.

## Pointers

- `verification/cycle-2026-11-08-80-20-pass12.md` — pass 12 detailed record (T006/T007 closed, 33 new tests, 013 expiry + refusal Gate 3).
- `verification/cycle-2026-11-08-80-20-pass11.md` — pass 11 detailed record (T034/T035 partial advance, 2/19 scenarios).
- `verification/mutation-checks.md` — mutation negative controls (004/013).
- `verification/determinism-checks.md` — determinism 2× evidence (004/013).
- `verification/cycle-2026-11-07-80-20-pass10.md` — pass 10 detailed record.
- `verification/final-inventory.md` — feature × path inventory and addenda.
- `verification/t002-farmhand-host-guard-attempt.md` — farmhand/split-screen evidence and `DebugCommands.TryHandle` lead.
