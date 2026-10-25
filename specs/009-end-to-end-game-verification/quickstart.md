# Quickstart: End-to-End In-Game Verification (Gate 3)

**Feature**: 009-end-to-end-game-verification
**Date**: 2026-10-25

## Prerequisites

- `STARDEW_GAME_PATH` configured (or `GamePath` in `LivingRoots.csproj`)
- SMAPI 4.5+ installed
- Stardew Valley 1.6+ installed
- `dotnet` SDK 6.0+
- Save backup created (copy entire `StardewValley/Saves/` to dated backup directory)

## Validation Scenarios

### Scenario 1: Growth Modifier Correction — Unit Tests

**Goal**: Verify the corrected growth modifier logic (multiplicative, DayEnding, host guard, idempotency, all locations) passes unit tests.

**Steps**:
1. Build the test project: `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj`
2. Run growth modifier tests: `dotnet test --filter "GrowthModifier"`
3. Verify all pass (0 failures)

**Expected**: All growth modifier tests pass, including multiplicative math, host guard, idempotency, and all-location scope.

### Scenario 2: Save Comparator — Before/After Diff

**Goal**: Verify the save comparator detects health deltas.

**Steps**:
1. Create a test save with known soil health values
2. Run the save comparator: `dotnet test --filter "SaveComparator"`
3. Verify it detects added/removed/changed health values

**Expected**: Save comparator correctly identifies health deltas between before and after snapshots.

### Scenario 3: Manual SC-009 Verification Session

**Goal**: Execute the full manual verification protocol.

**Steps**:
1. Launch SMAPI: `StardewModdingAPI.exe` (or via Steam with SMAPI)
2. Verify SMAPI header in console (mod loaded, no "Skipping mod" messages)
3. Follow `gate3-verification-contract.md` checklist:
   - Pre-state: save backup, mod versions, SMAPI log rotation, pre-state screenshot
   - Execute: till → craft bin → place → load waste → wait 2 in-game days → harvest → apply → observe
   - Capture 3 signals: observation notes + save diff + SMAPI log excerpt
   - Post-state: pre/post screenshot comparison + save diff + no side effects
4. If all 3 signals agree: update `test.md` from `partial` to `verified`

**Expected**: All 3 signals converge; `test.md` updated to `verified` with evidence file paths.

### Scenario 4: Full Build Verification

**Goal**: Verify no regressions from verification tooling changes.

**Steps**:
1. `dotnet build Stardew-LivingRoots.sln --configuration Release`
2. `dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal`
3. `dotnet format Stardew-LivingRoots.sln --verify-no-changes`

**Expected**: 0 build errors, 0 test failures, 0 format violations.

## Evidence Artifacts

The **canonical** bundle layout is the MIRROR-style `evidence-bundle-<run-id>/` defined in `contracts/verification-protocol.md` (FR-001c): `manifest.json` (schema version, run id, timestamp, tool/game/mod versions, environment, SHA-256 per file) + `claims.json` + `artifacts/` + JUnit XML + Allure JSON, with honesty levels W/R/E. The list below is the **simplified human-readable view** of the minimum artifact set; a bundle is not complete without `manifest.json` + `claims.json` + per-file SHA-256.

After successful verification, the following artifacts should exist:

```
verification/009-end-to-end-game-verification/   # simplified view (canonical = evidence-bundle-<run-id>/)
├── observation-notes.md       # Typed notes per criterion (not "looked fine")
├── save-diff.json             # Before/after SoilHealthState JSON diff
├── smapi-log-excerpt.txt      # SMAPI console excerpt (no WARN/ERROR)
├── pre-state-screenshot.png   # Pre-verification screenshot
├── post-state-screenshot.png  # Post-verification screenshot
└── gate3-verification-contract.md  # Completed checklist
```

## Claim Discipline

After verification, write in release notes / COMPLETION_SUMMARY.md:
> "Verified by manual end-to-end session + multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]."

Do NOT write: "verified by build / 1447 passed"

Status: "maintainer-verified with multi-signal evidence" (not "independently verified").