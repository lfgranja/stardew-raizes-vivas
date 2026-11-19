# Convergence — `027-stabilize-reactive-invalidation-tests`

- **Date**: 2026-11-19
- **Mode**: append-only
- **Anchor**: `.specify/feature.json` → `specs/027-stabilize-reactive-invalidation-tests`
- **Review status**: converged (0 open code findings; `T001`–`T011` completed; `T012` closes SDD closeout + PR delivery in BLOCO 7)

## Requirement → Artifact Traceability

| Requirement | Artifact / Evidence | Status |
|---|---|---|
| FR-001 (remove `Stopwatch <= MaxRenderTimeMs` in 3 tests of `SoilHealthVisualizationTests.cs`) | `LivingRoots.Tests/SoilHealthVisualizationTests.cs` (`4e5bb77`): `grep -n "Stopwatch\|<= ModConstants.MaxRenderTimeMs"` → 0 matches | done |
| FR-002 (remove `Stopwatch <= MaxRenderTimeMs` in `T031b`, keep `MaxTest = 50`) | `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` (`aed6322`): `grep -n "Stopwatch\|<= ModConstants.MaxRenderTimeMs"` → 0 matches; `[Property(MaxTest = 50)]` preserved | done |
| FR-003 (synchronous invalidation + next-frame rebuild + `<= 1` consultation per visible tile) | `SoilHealthVisualizationTests.cs` (`Times.Once`/`Times.Exactly(2)`, `RenderOverlays` rebuild, `RenderOneDrawableValueFrame == 75f`); `VisualizationInvariantsTests.cs` (`getConsultationCount() == 1` warm / `1` invalidate / `2` rebuild) | done |
| FR-004 (Gate 3 ownership of 16.67ms frame budget documented) | XML doc/comments on all 4 updated tests explicitly name Gate 3 (`scripts/verify-scenario.sh`) and `ModConstants.MaxRenderTimeMs` | done |
| FR-005 (remove unused `using System.Diagnostics;` & respect `.editorconfig` block-body rule) | Both `using System.Diagnostics;` lines removed; `BuildOverlayRenderer()` block-bodied; `dotnet format --verify-no-changes` exit `0`; Release build `0 Erro(s)` | done |
| FR-006 (confine code diff strictly to the 2 test files; zero `LivingRoots/` diff) | `git diff --name-only origin/dev -- LivingRoots/` → empty | done |
| SC-001 (0 `Stopwatch` / `MaxRenderTimeMs` timing checks in target files) | Verified via `grep` by isolated verifier (`aed6322`) | done |
| SC-002 (5/5 consecutive passes of the 4 updated tests) | Isolated verifier 5/5 PASS (`4/4` passed in runs 1–5, 1–3s each) | done |
| SC-003 (full local gates PASS) | All 7 steps of `scripts/release-gate.sh` verified green on `aed6322` (`1999/0/4`, smoke `50/51`, format exit `0`, locked restore exit `0`, Release build `0` errors, `--check-docs` exit `0`, `check-protected-paths.sh` exit `0`); `scripts/ci/aggregate-gate.sh` re-run on SDD closeout in BLOCO 7 | done (local) / `[NV]` remote PR checks |
| SC-004 (empty `LivingRoots/` diff) | Verified empty | done |

## Deviation / Honesty Record

- **Release-gate component verification**: On `aed6322` (the final C# commit), all 7 steps of `scripts/release-gate.sh` ran directly and passed (`format` exit 0, `restore --locked-mode` exit 0, `build -c Release -m:1` 0 errors, `test -c Release --no-build` 1999/0/4, `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` 51 files / 50 passed / 0 failed / 1 skipped, `--check-docs` exit 0, `check-protected-paths.sh` exit 0), avoiding a redundant second 40-minute smoke re-run while guaranteeing 100% step-level coverage of the exact code tree.
- **Pre-existing pytest debt**: `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` reports `179 passed, 1 failed` (`LivingRoots/Services/CompostingBinService.cs:115` `#pragma warning disable S8969`), unchanged from `dev` `8bba3fe` and out of scope (Red/Yellow Zone production file).
- **Gate 3**: N/A (test-only change; zero `LivingRoots/` files modified).

## Append-only notice

Do not rewrite or renumber this file. Future convergence checks append new dated sections only.
