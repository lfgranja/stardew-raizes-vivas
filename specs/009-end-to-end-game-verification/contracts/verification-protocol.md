# Verification Protocol Contract: End-to-End In-Game Verification (Gate 3)

**Feature**: 009-end-to-end-game-verification

> **Scope note (remediation)**: This document is the *interface/protocol specification*. The per-criterion observation checklist lives in `../checklists/gate3-verification-contract.md` (T030); requirements-quality validation lives in `../checklists/verification-protocol.md`. See those for non-overlapping concerns.
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
| SC-004 | Harvest quality reflects tier | Harvest ≥10 from each tier; compare quality distribution | DEFERRED — out of scope for `verified` claim (spec §Non-Goals; `ComputeHarvestQualityRoll` not implemented). Included for completeness only. |
| SC-005 | Extra harvest on Healthy | Count extra items over ≥10 harvests per tier | DEFERRED — out of scope for `verified` claim (spec §Non-Goals). Included for completeness only. |
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

## Canonical Artifact Structure (FR-001c is authoritative)
Canonical structure (MIRROR-style): `evidence-bundle-<run-id>/` with:
- `manifest.json` (schema version, run id, timestamp ISO 8601, tool/game/mod versions, environment) carrying SHA-256 per file
- `claims.json` (claim → evidence pointer, risk, safe wording)
- `artifacts/` (screenshots, SMAPI logs, save-deltas before/after, reflection outputs)
- JUnit XML (CI integration) and Allure JSON (rich report)
Quickstart's simpler `verification/009-end-to-end-game-verification/` structure is a simplified view; FR-001c is canonical for evidence bundles.

## Honesty Levels (W/R/E) — Assignment Criteria
- **W (launch/connect):** Assigned when tooling/launch is verified (SMAPI loads, mod connects) but no runtime evidence is collected.
- **R (runtime evidence collected):** Assigned when evidence artifacts are present and valid (save delta, log excerpt, observation notes).
- **E (human judgment):** Assigned only by the maintainer. Never auto-assigned. Requires W + R as prerequisites.
Assignment rule: W → R → E (sequential). E cannot be assigned without both W and R. Auto-promotion to E is prohibited.

## Save Comparator Specification
- **Form:** C# test utility (consistent with project tech stack: .NET 6, xUnit). Located in `LivingRoots.Tests/`.
- **Input:** Two `modData/soil_health_data_{saveId}.json` files (before and after).
- **Comparison algorithm:** Key-based by `(LocationName, "X,Y")`. Float tolerance: 0.0001f (matching sparse cache threshold). Missing keys treated as 0 (sparse cache). Output: JSON diff with `added`/`removed`/`changed` entries per tile.
- **Validation:** Detects expected health delta. Ignores NaN tiles (out of scope for `verified`).

## Cross-Cutting Non-Functional Checks (Principles II, III, VI)
| # | Check | Method | Pass Threshold |
|---|-------|--------|----------------|
| 1 | No raw exception/stack trace in SMAPI log | Grep `SMAPI-latest.txt` for `Exception`, `at LivingRoots` | 0 matches |
| 2 | Soil health values in bounds (0–100) | Inspect `soil_health_data_{saveId}.json` | All values ∈ [0,100] |
| 3 | Save file path resolves under mod-data dir | Verify file location | No `..` or traversal in path |
| 4 | No homoglyph/Unicode anomaly in save key | Inspect save filename | FormC-normalized, no mixed-script |
| 5 | No UI freeze or deadlock during save/reload | Observe game responsiveness | Game remains responsive; DayEnding fires within expected timeframe; no "not responding" state |
| 6 | Growth modifier scan completes within 5 ms | SMAPI log (LogLevel.Trace) shows elapsed ms | ≤5 ms for up to 40,000 tilled tiles |

## Post-State Comparison

- [ ] Pre-state screenshot vs post-state screenshot (same camera/zoom if overlay involved)
- [ ] Pre-state save artifacts vs post-state artifacts (diff of `SoilHealthState` persistence fields)
- [ ] No unexpected secondary effects: (1) nearby tiles (within 3-tile radius of modified tiles) unchanged vs pre-state baseline; (2) no new ERROR/WARN entries in SMAPI log vs pre-state log snapshot; (3) other mods still loaded (no "Skipping mod" messages); (4) composting bin state unchanged unless expected; (5) no changes to unrelated modData files. Baseline = pre-state capture (save backup + log snapshot).
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