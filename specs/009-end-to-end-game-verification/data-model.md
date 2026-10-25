# Data Model: End-to-End In-Game Verification (Gate 3)

**Feature**: 009-end-to-end-game-verification
**Date**: 2026-10-25

## Overview

This feature does not introduce new domain entities or modify existing data schemas. It corrects existing behavior (growth modifier) and formalizes verification artifacts. All domain models remain unchanged.

## Existing Entities Involved (No Schema Changes)

### SoilHealthState

**Location**: `LivingRoots/Domain/SoilHealthState.cs`

```csharp
public class SoilHealthState
{
    public Dictionary<string, Dictionary<string, float>> LocationHealthData { get; set; }
}
```

- **Outer key**: Location name (e.g., "Farm", "Greenhouse")
- **Inner key**: Tile coordinates as `"X,Y"` string (e.g., "10,5")
- **Value**: Health float (0–100)
- **Persistence**: `modData/soil_health_data_{saveId}.json` via `ModDataService`
- **Sparse cache**: Zero/default values omitted on save (`if (Math.Abs(clampedValue) > 0.0001f)`)
- **Role in this feature**: Save comparator reads before/after snapshots to detect health deltas. No schema changes.

### Crop (Vanilla Stardew Valley)

**Location**: Stardew Valley assembly (`StardewValley.Crop`)

| Field | Type | Role in this feature |
|-------|------|---------------------|
| `currentPhase` | `NetInt` | Phase index in `phaseDays` list. Growth modifier does not change this directly. |
| `dayOfCurrentPhase` | `NetInt` | Progress within current phase. Growth modifier adjusts this (multiplicative on `phaseDays`). |
| `phaseDays` | `List<int>` | Days required per phase. Growth modifier caches base value and applies tier multiplier. |

- **Role in this feature**: Growth modifier reads/writes `dayOfCurrentPhase` and caches `phaseDays`. No schema changes to vanilla type.

### CompostingBinData

**Location**: `LivingRoots/Domain/CompostingBinData.cs`

- **Role in this feature**: Not directly involved. Composting bin verification is in `test.md` scope (bug assessment), not spec 009 scope. Save preservation includes `composting_bins_{saveId}.json` sidecar.

## New Artifacts (Not Domain Entities)

### gate3-verification-contract.md

**Location**: `specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md`

Formal SC-009 checklist. Structure:

```
## SC-009 — Per-Criterion Manual Verification Protocol

### Pre-state (baseline)
- Save backup, mod versions, SMAPI log rotation, pre-state screenshot, state variables

### Per-Criterion Observation (each SC verified individually)
| SC | Criterion | Observation Method | Pass Threshold |
| SC-001 | Poor-tile growth slower | Plant same seed on Poor + Medium; compare days | Poor > Medium |
| SC-002 | Healthy-tier growth faster | Plant same seed on Healthy + Medium; compare days | Healthy < Medium |
| SC-003 | Medium-tier parity | Compare Medium to vanilla (no compost) | Medium = vanilla |
| SC-004 | Harvest quality reflects tier | Harvest ≥10 per tier; compare quality | Poor=0, Medium=vanilla, Healthy>vanilla |
| SC-005 | Extra harvest on Healthy | Count extra items over ≥10 harvests | Healthy > base rate |
| SC-007 | Save/reload preserves effect | Save, exit, reload; verify health + growth | Health unchanged; multiplier reapplied |

### Multi-Signal Evidence (ALL THREE required)
- Observation: typed notes/video per criterion
- Artifact: save-file diff (before/after SoilHealthState JSON)
- Log: SMAPI console excerpt (no WARN/ERROR; event fired; no skipped mods)

### Post-State Comparison
- Pre/post screenshot comparison
- Pre/post save artifact diff
- No unexpected secondary effects
- Divergence handling: if signals disagree, document as gap (not partial success)

### Claim Discipline
- "verified by manual end-to-end session + multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]"
- Status: "maintainer-verified with multi-signal evidence" (not "independently verified")
```

### Save Comparator

**Location**: `LivingRoots.Tests/` (C# xUnit utility — canonical; `scripts/` is reserved for environment/smoke validation and is NOT used for the save comparator)

Before/after `SoilHealthState` JSON diff tool. Reads `modData/soil_health_data_{saveId}.json` files.

**Input**: Two JSON files (before and after)
**Output**: Diff report showing added/removed/changed health values per tile
**Validation**: Detects expected health delta (e.g., compost application → health +15 on target tile)

### research.md

**Location**: `specs/009-end-to-end-game-verification/research.md`

Phase 0 output. Consolidated from assessment artifacts and 6 clarification rounds. Already created.

## State Transitions

### Growth Modifier Execution Flow

```
DayEnding event fires (SMAPI)
  → OnDayEnding handler
    → if (!Context.IsMainPlayer) return; (host guard)
    → if (already processed today) return; (idempotency)
    → farmLocationNames = {currentLocation, "Farm", "Greenhouse"}
    → foreach locationName in farmLocationNames:
        → location = ILocationProvider.GetLocationByName(locationName)
        → if location == null: skip
        → foreach tile in location:
            → health = SoilHealthService.GetSoilHealth(locationName, tile)
            → if terrainFeatures[tile] is HoeDirt with crop:
                → cache base phaseDays (if not cached)
                → tier = HealthCategory(health) // Poor/Moderate/Healthy
                → multiplier = tier == Poor ? 1.15 : tier == Healthy ? 0.85 : 1.00
                → restore base phaseDays
                → apply multiplier to phaseDays
                → adjust dayOfCurrentPhase accordingly
    → mark day as processed (idempotency key)
```

### Verification State Machine

```
test.md status: partial
  → SC-009 manual session executed
    → 3 signals captured (observation + artifact + log)
      → all signals agree → test.md = verified
      → signals disagree → document as gap (not partial success)
      → incomplete signals → partial (never auto-promoted to pass)
```

## Validation Rules

1. **Health bounds**: Soil health values must be 0–100 (`ModConstants`). Save comparator validates this.
2. **Sparse cache**: Zero values omitted on save. Save comparator must handle missing keys (treat as 0).
3. **NaN handling**: Tiles with NaN health (never sampled) are out of scope for `verified`. Save comparator ignores NaN tiles.
4. **Host authority**: Growth modifier only executes on main player. Verification assumes single-player or host session.
5. **Save preservation**: Verification runs against disposable save copy. Original must be byte-identical after verification.