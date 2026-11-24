# Test Evidence — `027-stabilize-reactive-invalidation-tests`

- **Feature**: `027-stabilize-reactive-invalidation-tests`
- **Branch**: `feature/027-stabilize-reactive-invalidation-tests`
- **Base commit**: `dev` @ `8bba3fe`
- **Code commit verified**: `aed6322` (commits `4e5bb77` + `aed6322`, Green Zone test-only)

## 1. Baseline RED (before fix, on `dev` `8bba3fe`)

- **Command**: `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh` (2026-11-19T19:50:27Z–20:12:41Z)
- **Result**: Steps 1–3 OK (`dotnet format`, `dotnet restore --locked-mode`, `dotnet build -c Release -m:1 --disable-build-servers`); **Step 4 FAILED (`exit 14`)**: `1 failed, 1998 passed, 4 skipped (2003 total)` in `3 m 43 s`:
  `LivingRoots.Tests.SoilHealthVisualizationTests.OnTileHealthChanged_ThroughProductionModEntrySubscription_InvalidatesOverlayCacheWithinOneFrame` (`LivingRoots.Tests/SoilHealthVisualizationTests.cs:419`):
  `LATENCY DEFECT: reactive cache invalidation took 27,346ms, which exceeds the frame budget of 16,67ms.`
- **Isolated 5x rerun on `8bba3fe`**: **5/5 PASSED** (1–2s each), confirming host timing flake.

## 2. Isolated Verifier Subagent (`aed6322`, 2026-11-19T16:36:55-04:00)

- **Diff scope**: `git diff --name-only origin/dev -- LivingRoots/` → **empty** (0 production files touched).
- **Hazard grep**: `grep -n "Stopwatch\|<= ModConstants.MaxRenderTimeMs" LivingRoots.Tests/SoilHealthVisualizationTests.cs LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` → **no matches** (`using System.Diagnostics;` removed from both).
- **Release build**: `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" dotnet build Stardew-LivingRoots.sln -c Release --no-restore -m:1 --disable-build-servers` → exit `0`, `0 Erro(s)`, `13 Aviso(s)` (pre-existing NuGet `net6.0` + `MSB3277` `Mono.Cecil.Pdb` warnings), `00:02:41.41`.
- **5 consecutive runs** of the 4 updated tests (`--no-build -c Release`):
  - Run 1: exit `0` — 4/4 passed (`1 s`)
  - Run 2: exit `0` — 4/4 passed (`2 s`)
  - Run 3: exit `0` — 4/4 passed (`3 s`)
  - Run 4: exit `0` — 4/4 passed (`1 s`)
  - Run 5: exit `0` — 4/4 passed (`1 s`)

## 3. Full Authoritative Release-Gate Component Checks (`aed6322`, 2026-11-19)

| Release-Gate Step | Command | Timestamp | Observed Output |
|---|---|---|---|
| Step 1 (format) | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | `2026-11-19T17:25:01`→`17:30:10-04:00` | `FORMAT_EXIT=0` |
| Step 2 (locked restore) | `dotnet restore --locked-mode` | `2026-11-19T17:30:10`→`17:30:16-04:00` | `RESTORE_EXIT=0` (4 projects restored) |
| Step 3 (Release build) | `GamePath="..." dotnet build Stardew-LivingRoots.sln -c Release --no-restore -m:1 --disable-build-servers` | `2026-11-19T16:36–16:40-04:00` | exit `0`, `0 Erro(s)` |
| Step 4 (full test suite) | `GamePath="..." dotnet test Stardew-LivingRoots.sln -c Release --no-build --verbosity minimal` | `2026-11-19T16:41:57-04:00` (`2 m 27 s`) | `TEST_EXIT=0`: **1999 passed / 0 failed / 4 skipped (2003)** |
| Step 5 (launch-free smoke) | `TMPDIR="/home/luis/.smoke-tmp-lrv" SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` | `2026-11-19T21:24Z` | `SMOKE_EXIT=0`: **51 files, 50 passed, 0 failed, 1 skipped** (`test-integration.sh`) |
| Step 6 (docs markers) | `./scripts/verify-scenario.sh --check-docs` | `2026-11-19T21:24Z` | exit `0` |
| Step 7 (protected paths) | `./scripts/check-protected-paths.sh` | `2026-11-19T21:24Z` | exit `0` (`protected-path preflight: all checks passed.`) |
| i18n parity | `python3 scripts/ci/i18n-parity-check.py` | `2026-11-19T17:30:32-04:00` | `I18N_EXIT=0`: `PASS (0 errors, 0 warnings)` |
| Quality-gate pytest | `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` | `2026-11-19T17:30:16-04:00` | **179 passed, 1 failed (`10.34s`)** — pre-existing baseline failure in `LivingRoots/Services/CompostingBinService.cs:115` (`#pragma warning disable S8969`); 0 regressions |

## 4. Gate 3 Scope Notice

- **Gate 3 (`scripts/verify-scenario.sh` under Xvfb)**: **N/A** — zero production files under `LivingRoots/` were added or modified (`git diff --name-only origin/dev -- LivingRoots/` is empty). No `machine-pass` or human Gate 3 claim is promoted.
