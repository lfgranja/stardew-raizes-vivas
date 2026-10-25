# Verification Protocol Contract: End-to-End In-Game Verification (Gate 3)

**Feature**: 009-end-to-end-game-verification
**Date**: 2026-10-25
**Label**: SC-009 (project-specific Living Roots identifier — NOT an IEEE/ISO/SMAPI standard)

## Contract: SC-009 — Per-Criterion Manual Verification Protocol

This contract defines the verification protocol that must be followed to claim `verified` status for the Phase 2 growth hook. It is the formal definition of SC-009 in spec 009, resolving the overloaded-label ambiguity (SC-009 means four different things in four specs: spec 004 = compost conservation audit; spec 005 = per-criterion manual verification; spec 008 = pre-flight prerequisite detection; spec 009 = manual verification protocol for Phase 2 growth hook).

## Pre-State (Baseline — Capture BEFORE Touching Feature)

- [ ] Save-file backup created (copy entire `StardewValley/Saves/` to dated backup directory)
- [ ] Mod list / version pinned (record `manifest.json` versions of all loaded mods)
- [ ] Pre-state screenshot recorded (default soil health / bin state at target location)
- [ ] SMAPI log rotated (start clean session; note start time / log file path)
- [ ] State variables noted: player location, day/time, season (seasonal decay multiplier is domain-relevant)

## Per-Criterion Observation (Each SC Verified Individually)

| SC | Criterion | Observation Method | Pass Threshold |
|----|-----------|-------------------|----------------|
| SC-001 | Poor-tile growth slower | Plant same seed on Poor (health < 34) + Medium (34–67) tiles; compare days to harvest | Poor > Medium (days) |
| SC-002 | Healthy-tier growth faster | Plant same seed on Healthy (≥67) + Medium tiles; compare days to harvest | Healthy < Medium (days) |
| SC-003 | Medium-tier parity | Compare Medium tile to vanilla (no compost) | Medium = vanilla (days) |
| SC-004 | Harvest quality reflects tier | Harvest ≥10 from each tier; compare quality distribution | Poor=0 stars, Medium=vanilla, Healthy>vanilla |
| SC-005 | Extra harvest on Healthy | Count extra items over ≥10 harvests per tier | Healthy > base rate |
| SC-007 | Save/reload preserves effect | Save, exit, reload; verify health + growth state | Health value unchanged; multiplier reapplied |

## Growth Hook Specific Criteria (Spec 009 Addition)

| # | Criterion | Observation Method | Pass Threshold |
|---|-----------|-------------------|----------------|
| 1 | Growth hook activates during DayEnding | SMAPI log shows DayEnding event fired; ApplySoilGrowthModifier executed | Log contains "DayEnding" + no exception |
| 2 | Growth hook reads all farm locations | Verify crops in Farm + Greenhouse (not just currentLocation) are processed | Log shows location names in farmLocationNames |
| 3 | Poor-tile growth slower (multiplicative) | Poor tile (health < 34) advances phase slower than Medium | Poor phase advancement < Medium |
| 4 | Healthy-tile growth faster (multiplicative) | Healthy tile (health ≥ 67) advances phase faster than Medium | Healthy phase advancement > Medium |
| 5 | Host guard prevents farmhand execution | Run as farmhand; verify growth modifier does NOT execute | Log shows "not main player" skip or no execution |
| 6 | Idempotency prevents reprocessing | Save/load mid-day; verify growth modifier does NOT re-execute | Log shows "already processed" skip |
| 7 | Save comparator detects health delta | Before/after `soil_health_data_{saveId}.json` diff shows expected delta | Diff shows health change on target tile |

## Multi-Signal Evidence (ALL THREE Required)

- [ ] **Observation**: Typed notes or video per criterion (not "looked fine" — describe specifically what was seen: "bin UI showed Processing and timer incremented"; "crop on poor tile at phase 2 day 3, healthy tile at phase 3 day 1")
- [ ] **Artifact**: Save-file diff (`modData/soil_health_data_{saveId}.json` before vs after; `SoilHealthState` JSON delta). Save comparator must confirm expected health delta.
- [ ] **Log**: SMAPI console excerpt (`SMAPI-latest.txt`) — no WARN/ERROR; confirm no "Skipping mod" message; confirm `DayEnding` event fired; confirm `ApplySoilGrowthModifier` executed; no exception traces.

## Post-State Comparison

- [ ] Pre-state screenshot vs post-state screenshot (same camera/zoom if overlay involved)
- [ ] Pre-state save artifacts vs post-state artifacts (diff of `SoilHealthState` persistence fields)
- [ ] No unexpected secondary effects (nearby tiles unchanged; other mods not skipped; no new ERROR entries)
- [ ] Divergence handling: if signals disagree, document as gap (not partial success). Do NOT auto-promote `partial` to `pass`.

## Claim Discipline

**Write**: "verified by manual end-to-end session + multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]."

**Do NOT write**: "verified by build / 1447 passed" or "verified by save diff alone."

**Status**: "maintainer-verified with multi-signal evidence" (not "independently verified").

**Independently verified** = future goal (≥2 sessions + third-party review) for automation framework.

## Notes

- SC-009 is a **project-specific Living Roots identifier** — not an IEEE/ISO/SMAPI standard. No external standard uses "SC-009" as a canonical label.
- `VERIFICATION_PROTOCOL.md` §3 already contains the substance of this protocol; this contract adds the SC-009 label mapping and spec-009-specific growth hook criteria.
- The 3-signal triangulation is retained (no reduction or extension). Research (BetterEvaluation triangulation, Huang/Meyer/Oriol 2026, ITEA/NIST) confirms no single signal is sufficient.
- "verified" = "maintainer-verified with multi-signal evidence" (1 session + 3 signals, no replication). "independently verified" = future goal for automation framework.