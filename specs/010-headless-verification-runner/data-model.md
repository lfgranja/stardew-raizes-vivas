# Data Model: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-26

This document defines the key entities the runner produces and consumes. Field-level schema for the Scenario Definition lives in `contracts/scenario.schema.json` (JSON Schema Draft 2020-12); the evidence bundle layout follows `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md`.

## Entities

### Scenario Definition

A declarative description of a verification scenario, authored as JSON (canonical) with an optional Markdown companion checklist. Consumed by the runner; provided by a feature.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `scenarioId` | string | yes | Unique identifier (e.g., `004-compost-growth`) |
| `featureSlug` | string | yes | Roadmap feature slug (e.g., `004-playable-compost-loop`) |
| `testMdPath` | string (path) | yes | Path to `specs/<NNN-slug>/test.md`; fail-fast validated (FR-008/FR-016) |
| `playerRole` | enum {host, farmhand} | yes | Expected player role for host-guard scenarios (FR-011) |
| `minimumGameVersion` | string (semver) | yes | Per-scenario floor (must satisfy mod minimums) |
| `milestoneSave` | string | yes | Name/id of the milestone save to seed from (private builddeps repo) |
| `initCommands` | array of strings | yes | SMAPI macros + custom commands to place bin, insert waste, set crop phases/soil health |
| `steps` | array of step objects | yes | Ordered steps: sleep N days, harvest, apply compost, observe |
| `thresholds` | array of threshold objects | yes | `{signal, condition, boundary, multiplier, minPhaseDays, sentinelUntouched}`; reference `ModConstants` by name + numeric value |
| `expectedSignals` | array of enum {soilHealthStateDelta, cropPhaseDelta, smapiLog} | yes | All three mandatory (SC-002) |
| `timeoutSeconds` | integer | yes | Per-scenario step timeout (macro timeout is retryable) |
| `schemaVersion` | const 1 | yes | Payload versioning |

Validation: strict `additionalProperties: false`; bundled `$ref`s at build time; `JsonSchema.Net` on runner side.

### Evidence Bundle

A directory `evidence-bundle-<run-id>/` produced by every run. Layout per `specs/009/contracts/evidence-bundle.md`.

| File | Purpose |
|------|---------|
| `manifest.json` | Schema version, run id, timestamp ISO 8601, tool/game/mod versions, environment, SHA-256 per file |
| `claims.json` | Claim → evidence pointer, risk, safe wording (W/R/E honesty levels) |
| `artifacts/` | Screenshots, SMAPI logs, save-deltas before/after, reflection outputs, Crop-phase dump, Anomaly Report entries |
| `attempts/<n>/` | Per-attempt artifacts (`attempt.json`, stdout/stderr, intermediate verdict, signals) when retried |
| `junit.xml` | CI integration |
| `allure.json` | Rich report |
| `compatibility` block | Observed/minimum/maximum/tier per component, modSelfCheck, deviation (embedded in manifest) |

### Run Verdict

One of `pass` / `fail` / `errored` / `partial` / `skipped` / `flaky`, with attached diagnostics and artifact paths.

| Verdict | Condition |
|---------|-----------|
| `pass` | All three signals agree; thresholds satisfied; verdict exit `18`? No — exit `0` (PASS). |
| `fail` | Threshold violation (exit 18) |
| `errored` | Crash or incomplete signal set (exit 19) |
| `partial` | Signal disagreement (e.g., dump vs. save) (exit 19) |
| `skipped` | Quarantined or intentionally not run (exit 20) |
| `flaky` | Passed on retry after a retryable failure (exit 18, flagged via metadata) |

Exit codes per the NFR-003 exit-code contract (new codes start at 18).

### Signal Set

The three required signals plus optional visual evidence.

| Signal | Source | Mandatory |
|--------|--------|-----------|
| SoilHealthState delta | Before/after `modData/soil_health_data_{saveId}.json` (SaveComparator, missing key → 0 at persistence layer, 0.0001f tolerance, NaN ignored) | Yes |
| Crop phase delta | In-process dump (primary oracle) + save-file inspection (cross-check); divergence → `partial` | Yes |
| SMAPI log excerpt | Elapsed-ms trace line + DayEnding event name | Yes |
| Visual evidence | Screenshots/video (optional) | No |

### Compat Matrix (`compat-matrix.json`)

Version-controlled, in-repo. Single pinned stable line (latest SV 1.6.x + matching SMAPI).

| Component | Fields |
|-----------|--------|
| Stardew Valley | observed, minimum, maximum, tier (HARD-FAIL/WARN/INFO) |
| SMAPI | observed (SemanticVersion), minimum, maximum, tier |
| LivingRoots mod | version, MinimumApiVersion, MinimumGameVersion, modSelfCheck |
| Deviation | recorded when observed exceeds matrix maximum (null `MaximumGameVersion` → upper-bound deviation → warn) |

### Quarantine Registry (`quarantine.json`)

Committed in-repo. One entry per quarantined scenario.

| Field | Type | Notes |
|-------|------|-------|
| `scenarioId` | string | |
| `owner` | string | Resolved via `--quarantine-owner` → `QUARANTINE_OWNER` env → git identity → repo owner (lfgranja) |
| `timeBoxStart` | ISO 8601 | |
| `timeBoxDays` | integer | 14, one 14-day extension (28 total) then escalate |
| `flakyRunCount` | integer | Trigger at 2 flaky runs in rolling 14-day window (or 2 of last 3) |
| `escalated` | boolean | Auto-re-enable + GitHub issue on expiry |

### Milestone Save Manifest (private builddeps repo)

| Field | Type | Notes |
|-------|------|-------|
| `gameVersion` | string | |
| `smapiVersion` | string | |
| `saveName` / `saveId` | string | |
| `sha256` | string | Of the save folder archive |
| `documentedContents` | string | Human-readable description of progression state |

### ILivingRootsModApi (production mod, Domain/Interfaces)

SMAPI-free interface exposed via `ModEntry.GetApi()`.

| Member | Type | Notes |
|--------|------|-------|
| `SoilGrowthModifierCompleted` | `event EventHandler<SoilGrowthModifierCompletedEventArgs>` | Raised synchronously on game thread at tail of `OnDayEnding`, after `ApplySoilGrowthModifier` loop for all locations (success or caught exception), before day transition |

`SoilGrowthModifierCompletedEventArgs`: `DayKey` (string), `IsMainPlayer` (bool), `LocationsProcessed` (int), `ElapsedMilliseconds` (long), `Success` (bool), `Error` (Exception?).

## State Transitions

- **Run verdict flow**: `pass` requires all three signals agree + thresholds satisfied + no divergence. `partial`/`errored`/`fail` block auto-promotion. `flaky` requires attempt 1 retryable failure + later pass. `skipped` requires quarantine or intentional skip.
- **Quarantine flow**: active → (2 flaky runs in window) → quarantined (14-day time-box) → (extension) → escalated (auto-re-enable + issue).
- **Save preservation**: original hashed before run; restored/verified byte-identical after; failure → `errored` with before/after state.
- **Version gate**: file-based classification before Xvfb launch → HARD-FAIL (abort) / WARN (proceed + record) / INFO (proceed + log).

## Validation Rules

- Scenario JSON: strict `additionalProperties: false`; required fields per FR-007 (`testMdPath`, etc.); thresholds reference `ModConstants` by name + numeric value.
- Soil health thresholds: poor < 34 → 1.15×; healthy ≥ 67 → 0.85×; moderate [34,67) → 1.00×; `phaseDays` never below 1; 99999 sentinel never touched; NaN tiles skipped; missing key / health ≤ 0 → non-participant.
- Performance: per-location elapsed ms ≤ 5 (single shot after discarded warmup; Gen0 delta recorded; >0 tagged `gc-attributed`).
- Save preservation: byte-identical original (tolerance 0.0001f for floats in SaveComparator; NaN ignored; missing key → 0 at persistence layer only).