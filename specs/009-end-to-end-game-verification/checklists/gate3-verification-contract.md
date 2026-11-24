# Gate 3 Verification Contract — SC-009 Per-Criterion Checklist

**Feature**: 009-end-to-end-game-verification
**Label**: SC-009 (project-specific Living Roots identifier — NOT an IEEE/ISO/SMAPI standard)
**Source of truth**: `../contracts/verification-protocol.md` (interface/protocol spec). This checklist is the per-criterion observation vehicle.

> Scope note: distinct from `../contracts/verification-protocol.md` (interface/protocol spec) and `./verification-protocol.md` (requirements-quality checklist). Cross-reference all three.

> **Automated counterpart (spec 010 FR-021).** The headless verification runner in `specs/010-headless-verification-runner/spec.md` is the automated counterpart to this per-criterion checklist. FR-021 preserves this checklist and `VERIFICATION_PROTOCOL.md` §3 as the documented manual fallback for hosts where the runner cannot execute, requires both documents to cross-link back to spec 010 FR-021, and requires a doc-link check that exits `21` on drift. A runner `machine-pass` is not `verified`; the maintainer E attestation recorded by this checklist remains the only path to `verified`.

## Pre-State (Baseline — capture BEFORE touching feature)

- [ ] Save-file backup created (copy entire `StardewValley/Saves/` to dated backup directory)
- [ ] Mod list / version pinned (record `manifest.json` versions of all loaded mods)
- [ ] Pre-state screenshot recorded (default soil health / bin state at target location)
- [ ] SMAPI log rotated (start clean session; note start time / log file path)
- [ ] State variables noted: player location, day/time, season (seasonal decay multiplier is domain-relevant)

## Per-Criterion Observation (each SC verified individually)

| SC | Criterion | Observation Method | Pass Threshold |
|----|-----------|-------------------|----------------|
| SC-001 | Poor-tile growth slower | Plant same seed on Poor (health < 34) + Medium (34–67) tiles; compare days to harvest | Poor > Medium (days) |
| SC-002 | Healthy-tier growth faster | Plant same seed on Healthy (≥67) + Medium tiles; compare days to harvest | Healthy < Medium (days) |
| SC-003 | Medium-tier parity | Compare Medium tile to vanilla (no compost) | Medium = vanilla (days) |
| SC-004 | Harvest quality reflects tier | Harvest ≥10 from each tier; compare quality distribution | DEFERRED — out of scope for `verified` claim (`ComputeHarvestQualityRoll` not implemented). Completeness only. |
| SC-005 | Extra harvest on Healthy | Count extra items over ≥10 harvests per tier | DEFERRED — out of scope for `verified` claim. Completeness only. |
| SC-007 | Save/reload preserves effect | Save, exit, reload; verify health + growth state | Health value unchanged; multiplier reapplied |

## Growth Hook Specific Criteria (spec 009)

| # | Criterion | Observation Method | Pass Threshold |
|---|-----------|-------------------|----------------|
| 1 | Growth hook activates during DayEnding | SMAPI log shows DayEnding event fired; ApplySoilGrowthModifier executed | Log contains "DayEnding" + no exception |
| 2 | Growth hook reads all farm locations | Verify crops in Farm + Greenhouse (not just currentLocation) are processed | Log shows location names in farmLocationNames |
| 3 | Poor-tile growth slower (multiplicative) | Poor tile (health < 34) advances phase slower than Medium | Poor phase advancement < Medium |
| 4 | Healthy-tile growth faster (multiplicative) | Healthy tile (health ≥ 67) advances phase faster than Medium | Healthy phase advancement > Medium |
| 5 | Host guard prevents farmhand execution | Run as farmhand; verify growth modifier does NOT execute | Log shows "not main player" skip or no execution |
| 6 | Idempotency prevents reprocessing | Save/load mid-day; verify growth modifier does NOT re-execute | Log shows "idempotent skip" |
| 7 | Save comparator detects health delta | Before/after `soil_health_data_{saveId}.json` diff shows expected delta | Diff shows health change on target tile |

## Multi-Signal Evidence (ALL THREE required)

- [ ] **Observation**: Typed notes or video per criterion (not "looked fine").
- [ ] **Artifact**: Save-file diff (`modData/soil_health_data_{saveId}.json` before vs after). Save comparator must confirm expected health delta.
- [ ] **Log**: SMAPI console excerpt (`SMAPI-latest.txt`) — no WARN/ERROR; confirm no "Skipping mod"; confirm `DayEnding` event fired; confirm `ApplySoilGrowthModifier` executed; no exception traces.

## Post-State Comparison

- [ ] Pre-state screenshot vs post-state screenshot
- [ ] Pre-state save artifacts vs post-state artifacts (diff of `SoilHealthState` persistence fields)
- [ ] No unexpected secondary effects (nearby tiles within 3-tile radius unchanged; no new ERROR/WARN; other mods still loaded; composting bin state unchanged unless expected; no changes to unrelated modData files)
- [ ] Divergence handling: if signals disagree, document as gap (not partial success). Do NOT auto-promote `partial` to `pass`.

## Claim Discipline

**Write**: "verified by manual end-to-end session + multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]."

**Do NOT write**: "verified by build / N passed" or "verified by save diff alone."

**Status**: "maintainer-verified with multi-signal evidence" (not "independently verified"). `independently verified` = future goal (≥2 sessions + third-party review) for automation framework.