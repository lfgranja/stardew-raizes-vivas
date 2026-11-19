# Validation Report: Stabilize reactive invalidation tests against wall-clock flakiness

- **Feature**: `027-stabilize-reactive-invalidation-tests`
- **Date**: 2026-11-19
- **Stage**: R4 (`validate`) — mandatory before R7 (`release`)
- **Verdict**: **APPROVED**

## Scope Validated

Test-only determinism fix across two Green-Zone files:
- `LivingRoots.Tests/SoilHealthVisualizationTests.cs` (`4e5bb77`)
- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` (`aed6322`)

Zero runnable code under `LivingRoots/`, zero localization files, and zero save/multiplayer/CI workflow files were modified.

## Verified Evidence (Commands Executed + Outputs)

| Check | Outcome | Timestamp / Scope |
|---|---|---|
| Baseline RED (`8bba3fe`) | `scripts/release-gate.sh` failed Step 4 (`exit 14`): `SoilHealthVisualizationTests.cs:419` (`27.346ms > 16.67ms`); isolated 5x rerun 5/5 PASS | `2026-11-19T19:50Z–20:12Z` |
| Isolated verifier (`aed6322`) | `0` `Stopwatch` timing assertions; `0` `LivingRoots/` diff; Release build `0 Erro(s)`; **5/5** consecutive runs passed (`4/4` each) | `2026-11-19T16:36–16:41-04:00` |
| `dotnet format --verify-no-changes` | `FORMAT_EXIT=0` | `2026-11-19T17:25:01–17:30:10-04:00` |
| `dotnet restore --locked-mode` | `RESTORE_EXIT=0` | `2026-11-19T17:30:10–17:30:16-04:00` |
| Full `dotnet test -c Release --no-build` | **1999 passed / 0 failed / 4 skipped (2003)** (`TEST_EXIT=0`, `2 m 27 s`) | `2026-11-19T16:41:57-04:00` |
| Smoke suite (`SMOKE_SKIP_INTEGRATION=1`) | **51 files, 50 passed, 0 failed, 1 skipped** (`SMOKE_EXIT=0`) | `2026-11-19T21:24Z` |
| Docs markers & protected paths | `./scripts/verify-scenario.sh --check-docs` exit `0`; `./scripts/check-protected-paths.sh` exit `0` | `2026-11-19T21:24Z` |
| `scripts/ci/i18n-parity-check.py` | `PASS (0 errors, 0 warnings)` (`I18N_EXIT=0`) | `2026-11-19T17:30:32-04:00` |
| `pytest scripts/quality-gate/tests -q` | `179 passed, 1 failed` (`CompostingBinService.cs:115` pre-existing S8969 pragma; 0 regressions) | `2026-11-19T17:30:16-04:00` |
| `git diff --name-only origin/dev -- LivingRoots/` | empty | verified |

## Requirements & Success Criteria Disposition

- `FR-001` ✅ | `FR-002` ✅ | `FR-003` ✅ | `FR-004` ✅ | `FR-005` ✅ | `FR-006` ✅
- `SC-001` ✅ | `SC-002` ✅ | `SC-003` ✅ (local; remote PR CI **[NV]** until observed after push/PR creation) | `SC-004` ✅

## Gate 3 & Residual Debt

- **Gate 3 (real SMAPI session)**: **N/A** (zero runnable `LivingRoots/` code modified).
- **Carried debts (unchanged, not silently edited)**: `CompostingBinService.cs:115` dead `#pragma warning disable S8969`; `.trivyignore` expiring `2026-11-30`; `RUNNER_AUDIT_STORE` missing (issue #523); 2 invalid `.archify/` candidates; `specs/026/tasks.md` 11 unchecked boxes; 7 open dependabot PRs `#790–#796`.

## R4 Decision

**APPROVED.** Proceed to R7 (`release.md`) and BLOCO 7 PR delivery (`dev/` target, no merge).
