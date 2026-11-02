# Quickstart: Soil Health → Crop Quality — Validation Guide

**Feature**: `014-soil-health-crop-quality` | **Branch**: `014-soil-health-crop-quality`

---

## Prerequisites

1. `GamePath` spike complete: `Crop.harvest` and `Object.Quality` member names verified against the 1.6.3/1.6.14 binary (Q2). If names differ, adjust hook strategy (public-field vs. Harmony).
2. `SC-008` rollback verification green: `CompostingBinService.CollectCompost` two-phase commit + `CollectCompost_WhenInventoryRefuses_RetainsBinReadyState` passing (Q4 prerequisite).
3. Stardew Valley + SMAPI installed (for Gate 3 in-game session, Q10).
4. .NET 6 SDK; repo builds (`dotnet build -c Release`).

## Setup

```bash
# Build
dotnet build Stardew-LivingRoots.sln --configuration Release

# Run full test suite
dotnet test Stardew-LivingRoots.sln --configuration Release

# Format check
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

## Validation Scenarios

### Scenario 1 — Poor soil yields no stars (SC-001, SC-002)
1. Create 3 tiles at 0%, ~50%, ~90% soil health (via compost or test setup).
2. Plant the same seed on Poor (0%) and Medium (50%) tiles; harvest both.
3. Confirm Poor tile item is always normal quality (0 stars) regardless of Farming level/fertilizer.
4. Confirm Medium tile quality follows the vanilla roll.
5. Confirm transient "Soil too poor for quality crops" + `cancel` SFX on Poor; none on Medium.

### Scenario 2 — Living soil enhances quality (SC-001)
1. Plant same seed on Living (≥71%) and Medium (50%) tiles.
2. Harvest both; confirm Living produces silver/gold/iridium more often than Medium.
3. Confirm `PoorTier_HarvestQualityIsAlwaysZero` and `ModerateTier_QualityMatchesVanilla` assertions pass.
4. Confirm extra-harvest items are normal quality (0) (research-c2).

### Scenario 3 — Extra harvest (SC-005)
1. Harvest a Living tile repeatedly; confirm occasional extra item (base + 0.10).
2. Confirm "Extra harvest!" floating text when extra occurs.
3. Confirm when both enhanced quality and extra occur, both "Living Soil" and "Extra harvest!" show as two sprites.
4. Confirm a Living harvest yielding normal quality with no extra shows no feedback.

### Scenario 4 — Inventory full (research-c1)
1. Fill inventory; harvest a Living tile with extra-harvest chance.
2. Confirm main item blocked (red message, crop stays) if inventory full.
3. Confirm extra item drops as debris (not lost, not duplicated).

### Scenario 5 — Medium byte-identical to vanilla (SC-003)
1. Plant same seed on Medium (50%) and unmodded tile; harvest both.
2. Confirm quality distributions statistically indistinguishable (within test tolerance) for same seed/day/fertilizer.

### Scenario 6 — NaN/Infinity (Unknown) → vanilla (internal-c3)
1. Set a tile's health to NaN/Infinity (corrupt/missing).
2. Harvest; confirm vanilla roll (no forced 0, no +0.08, no feedback).

### Scenario 7 — Band scale 31/71 (SC-006)
1. Grep codebase for 34/67 references (excluding migration history); confirm none.
2. Confirm `HealthCategory`, `ColorInterpolationService`, `ModController` growth modifier, tests, Gate 3 thresholds use 31/71.

### Scenario 8 — Host authority (SC-004)
1. Simulate second player; confirm no client-side roll occurs; host rolls once; client re-syncs on next `DayStarted`.

### Scenario 9 — Transient feedback immunity (SC-005)
1. Render >1000 visible tiles; confirm feedback still appears and does not exceed 16.67 ms/frame.

### Scenario 10 — Gate 3 in-game (SC-008, Q10)
1. Run in-game SMAPI session (~30 min, rebuild stale dll).
2. Capture 3 signals: observation notes + save diff (`modData/soil_health_data_{saveId}.json`, float tolerance 0.0001f) + SMAPI log.
3. Include second player for multiplayer verification (D1).

## Expected Outcomes

- `dotnet build -c Release` passes with 0 errors.
- `dotnet test -c Release` passes with 0 failures.
- `dotnet format --verify-no-changes` clean.
- All SC-001–SC-007 automated assertions pass.
- SC-008 attested by maintainer with multi-signal evidence.

## Test Commands (filter examples)

```bash
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~HarvestQuality"
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~HealthCategory"
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~CompostingBinService"
```
