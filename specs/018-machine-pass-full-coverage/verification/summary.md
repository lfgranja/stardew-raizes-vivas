# 018 — Verification Summary (honest status)

**Last updated:** 2026-11-08 (pass 10, ciclo 80/20 MDMV adaptado)
**Scope:** aggregate status only. `machine-pass` means the runner promoted a host run with complete, agreeing declared signals and honesty level R. It is **not** human verification, **not** D1, and **not** SC-004.

## Deterministic gates (fresh, pass 10)

| Gate | Command | Result |
|---|---|---|
| Formatting | `GamePath=/home/luis/stardew-assemblies dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **exit 0** |
| Docs markers | `./scripts/verify-scenario.sh --check-docs` | **exit 0** |
| Doc links | `bash scripts/tests/test-doc-links.sh` | **PASS** |
| Full smoke suite | `SMOKE_SKIP_INTEGRATION=1 ./scripts/tests/run-all.sh` | **50 files, 49 passed, 0 failed, 1 skipped, exit 0** |
| 017 scenarios | `bash scripts/tests/test-017-agrochemical.sh` | **23/23 passed** |
| 017 claims | `bash scripts/tests/test-017-claims.sh` | **PASS** (12 rows, 19 paths, 2 digests) |
| Scenario `modVersion` | `specs/**/*.scenario.json` sweep | **69 declarations, all `0.1.0`** |

The single smoke skip is `test-integration.sh`, intentionally skipped by `SMOKE_SKIP_INTEGRATION=1`; real-game integration is covered by Gate 3 runs, not by the launch-free suite. The smoke log is a local artifact (`/tmp/opencode/smoke-run-all-pass10.txt`), not committed.

## Evidence integrity

- **17/17** distinct bundle IDs cited in `specs/*/verification/claim-records.md` (004/005/013/014/015/017) are present locally and pass `./scripts/verify-scenario.sh --verify-bundle` (exit 0).
- **74** real local bundles (with `manifest.json`): **62 OK, 12 legacy failures** (7 missing `schemaVersion`; 5 `retention-receipt.json` outside the subject; era 2026-11-04/05).
- **94** additional `evidence-bundle-run-*` directories contain only `artifacts/` + `attempts/` (partial attempts without manifest; not evidence bundles).
- Of the 3 failing bundles still referenced anywhere in `specs/`, provenance is historical only: `4acd`/`0a6b` are past promotion transitions superseded by the current 004 GATE3 pointer `535d` (verify OK); `07ba` is a 010 observation pointer already annotated as legacy/incompatible in pass 5. None is cited by a current claim-record or feature GATE3. Decision: retain as historical, never delete, rewrite, or re-promote.

## Claim population

- **6** specs carry a `machine-pass` GATE3 block: 004, 005, 013, 014, 015, 017. Each has `verification/claim-records.md` and `verification/host-only-gate3.md`.
- **Blocked/pending, never `machine-pass`:** farmhand guard / D1 transport (T002 split-screen `isMainPlayer` flip not demonstrated), SC-004 visual comprehension, human `verified` attestation, render legibility, and balance/playtest.
- 018 is an aggregation feature and has no machine claim of its own beyond the per-feature records.

## Residual work

- **T034** mutation checks and **T035** determinism 2× require real game runs.
- **T006/T007** mulch unit tests require extracting a seam from static `Game1`/`GameLocation` coupling; current coverage is parser-unit + 013 Gate 3 integration.
- **12 legacy bundles** remain as historical artifacts under a retain-only policy; no regeneration was attempted.
- **94 partial attempts** are not bundles and are not cited as evidence.

## Pointers

- `verification/cycle-2026-11-07-80-20-pass10.md` — pass 10 detailed record.
- `verification/final-inventory.md` — feature × path inventory and addenda.
- `verification/t002-farmhand-host-guard-attempt.md` — farmhand/split-screen evidence and `DebugCommands.TryHandle` lead.
