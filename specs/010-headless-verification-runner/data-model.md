# Data Model: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-27
**Schema**: `contracts/scenario.schema.json` (Draft 2020-12, `schemaVersion: 2`, immutable `$id`)

This document defines the key entities the runner produces and consumes. The schema is authoritative for field-level validation; this document mirrors it for readability and MUST stay in sync (CI consistency check, C-SCN-36). The evidence bundle layout follows `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` and `contracts/evidence-bundle.md`.

## Entities

### Scenario Definition

A declarative description of a verification scenario, authored as JSON (canonical) with an optional Markdown companion checklist. Consumed by the runner; provided by a feature. Strict `additionalProperties: false` at every object level; unknown fields fail loudly.

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `schemaVersion` | const `2` | yes | — | Immutable payload version. v2 is the first published schema. |
| `scenarioId` | string (kebab-case) | yes | — | Stable test-case identifier (e.g., `compost-growth-poor-soil`). |
| `featureSlug` | string (`NNN-slug`) | yes | — | Feature folder under `specs/` (e.g., `004-playable-compost-loop`). |
| `testMdPath` | string (`specs/NNN-slug/test.md`) | yes | — | Must exist and contain exactly one `GATE3` marker block; validated fail-fast (FR-008/FR-016). |
| `summary` | string (1–500) | yes | — | Required plain-language intent; advisory only. |
| `playerRole` | enum `{host, farmhand}` | yes | — | Expected role for host-guard evaluation (FR-011). |
| `minimumGameVersion` | semver string | yes | — | Per-scenario floor; must be ≥ the mod manifest's `MinimumGameVersion`. |
| `environment` | object | yes | — | Declared expected environment: `gameVersion`, `smapiVersion`, `modVersion`, `apiVersion`, `saveId`, `config`. |
| `environment.apiVersion` | semver string | yes | — | Required `ILivingRootsModApi.ApiVersion` contract version; distinct from `modVersion`; mismatch is HARD-FAIL. |
| `environment.config` | object | yes | `{}` | `saveMode` (default `save`), `headless` (default `true`), `skipBuild` (default `false`). Members are optional; the object itself is required. |
| `milestoneSave` | string | yes | — | Source milestone save folder name in the private builddeps repo; must match the milestone manifest `saveName`. Path separators rejected. |
| `background` | array of init commands | no | `[]` | Shared preconditions executed before `initCommands` (Background/common block). |
| `initCommands` | array of objects | yes | `[]` allowed | `{kind: "macro", file}` or `{kind: "console", command}`. Macros are `.txt` files, one `/` command per line; console commands must not start with `debug `. |
| `steps` | array of step objects | yes | — | Ordered array (1–5 items); array order is the single source of truth (no `dependsOn`). |
| `steps[].action` | enum | yes | — | Closed primitive vocabulary: `sleep`, `warp`, `harvest`, `applyCompost`, `insertWaste`, `setSoilHealth`, `observe`, `dumpCropPhase`, `runMacro`. |
| `steps[].target` | string | yes | — | Tile/location/object identifier. |
| `steps[].days` | integer (1–28) | no | `1` | Days spanned by the action. |
| `steps[].inputs` | object | no | `{}` | Closed action-specific inputs (`tileX`, `tileY`, `location`, `itemId`, `count`, `soilHealth`, `macroFile`); the object declares `required: []`, so action-specific presence is enforced by the runner. `soilHealth` is the typed 0–100 value; `count` is the item quantity. There is no generic `amount` field. |
| `thresholds` | array of threshold objects | yes | — | Machine-evaluable oracle; `{signal, predicate, modConstant?, description?, condition?}`. |
| `thresholds[].predicate` | object | yes | — | Discriminated `op`: `cmp`, `between`, `multiplier`, `sentinel`, `allOf`/`anyOf`, `not`. |
| `thresholds[].modConstant` | string | no | — | Optional `ModConstants` member name; the numeric value in the predicate stays authoritative. |
| `thresholds[].condition` | string | no | `""` | Deprecated alias for `description`; retained for one major version. |
| `expectedSignals` | array of 3 unique enums | yes | — | Exactly `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog` (SC-002). |
| `timeoutSeconds` | integer (1–86400) | no | `300` | Per-scenario timeout; macro timeout is retryable. |
| `inputs` | object | no | `{}` | Scenario-level inputs: `season` (default `spring`), `day` (default `1`), `weather` (default `sunny`), `money` (default `500`). Declares `required: []`; no hidden global state. |
| `rngSeed` | integer | yes | — | Declared RNG seed for reproducibility. |
| `time` | string (ISO 8601 UTC) | yes | — | Declared time context (`YYYY-MM-DDTHH:MM:SSZ`). |
| `determinism` | object | yes | — | Required idempotency declaration plus any controlled non-deterministic sources. |
| `criticalPath` | boolean | no | `false` | Marks a never-quarantinable scenario (C-REL-23). |
| `scenarioKind` | enum `{positive, negative, boundary}` | yes | — | Positive declares `pass`; negative declares a non-pass verdict; boundary declares `pass` or `fail`. |
| `expectedVerdict` | enum `{pass, fail, errored, partial, skipped}` | conditional | `pass` | Required for negative/boundary (via `if/then`); a mismatch is a scenario-contract failure (exit 19). `flaky` is derived metadata, not a base verdict. |
| `hostGuard` | object | conditional | — | Required when `playerRole=farmhand`; forbidden when `playerRole=host`: `skipLine` (const), `forbiddenEvidence` (`elapsedTrace`, `phaseDaysMutation`), `requireSkipLine: true`. |

Validation: strict `additionalProperties: false` at every object level; bundled `$ref`s at build time; `JsonSchema.Net` on the runner side; `check-jsonschema`/`ajv-cli` in CI/pre-commit.

#### Absence vs. null vs. default

| Form | Meaning | Schema behavior |
|------|---------|-----------------|
| Field omitted (optional) | Author accepts the declared default | Valid; runner applies the default. |
| Field omitted (required) | Incomplete scenario | Invalid; fail-fast exit 14. |
| Field present with `null` | Never a meaningful value | Invalid for every field; no field is nullable. |
| Field present with `""` | Explicit empty string where `minLength` allows it | Valid only for `description`, `condition`, `summary`; not for identifiers or paths. |
| `"NaN"` string | Unmeasured/NaN sentinel (numeric fields) | Valid only where `numericValue`/`soilHealthValue` allows it; never a `cmp` operand. |
| Missing soil-health key | No stored entry (non-participant) | Represented by `MISSING_KEY`, not by `0` or `"NaN"`. |
| Stored `NaN` health | Unmeasured value (non-participant) | Represented by `NAN_HEALTH`; distinct from `MISSING_KEY`. |
| `0` health | Measured floor (non-participant for growth) | A real number; never collapsed into missing/unknown. |

#### IEEE 829 §6.2 mapping

| IEEE 829 Test Case Specification field | Scenario/schema mapping | Notes |
|----------------------------------------|-------------------------|-------|
| Identifier | `scenarioId` (+ `featureSlug`) | Stable across runs; used in reports/quarantine. |
| Test items | `featureSlug`, `testMdPath`, `summary` | Names the feature and its Gate 3 record. |
| Inputs | `inputs`, `rngSeed`, `time`, `environment`, `milestoneSave`, `background`, `initCommands`, `steps` | All explicit; no hidden global state. |
| Outputs | `thresholds`, `expectedSignals`, `expectedVerdict` | Machine oracle plus expected verdict. |
| Environmental needs | `environment` (`gameVersion`, `smapiVersion`, `modVersion`, `apiVersion`, `saveId`, `config`), `minimumGameVersion`, `playerRole` | Declared and checked pre-launch. |
| Procedural requirements | `steps`, `initCommands`, `timeoutSeconds`, `determinism` | Ordered execution, bounded time, idempotency. |
| Intercase dependencies | Ordered `steps` array; suite order is lexicographic (C-SCN-34) | No `dependsOn`; array order is authoritative. |

#### Predicate semantics

All predicates evaluate against the captured signal named by `thresholds[].signal`; a predicate that references a field not present in that signal is a scenario-contract error (exit 14).

| `op` | Pass condition | Operand rules |
|------|----------------|---------------|
| `cmp` | `left <operator> right` within `tolerance` | `left` is a signal field; `right` is numeric for numeric fields, boolean for boolean fields, and a string for string fields. `lt/lte/gt/gte` only for numeric fields. `"NaN"` is forbidden. `healthTier` operands must use the `unknown/poor/moderate/healthy` enum. |
| `between` | `min < actual < max` with inclusive bounds by default | Numeric fields only. `exclusiveMin`/`exclusiveMax` default `false` (inclusive); set `true` for an open bound. `tolerance` is required and applies to both bounds. `min <= max` is enforced by the runner (exit 14 if violated). |
| `multiplier` | `actual == round(base × factor)` within `tolerance`, clamped to the field minimum | Numeric fields only. `base` is a pre-run field reference (`pre.<field>`) whose suffix MUST equal `field`; `factor` is bounded to `[0.01, 100]`; `rounding` defaults to `round` (midpoint-to-even) and `clampToFieldMinimum` defaults to `true` (`phaseDays` minimum 1). The 99999 sentinel is never multiplied. |
| `sentinel` | Named sentinel condition holds; if `mustNotChange=true`, unchanged from the pre-run baseline | Sentinel-to-field mapping is enforced: `SENTINEL_UNTOUCHED_99999` → `phaseDays`; `NAN_HEALTH`/`MISSING_KEY`/`HEALTH_LE_ZERO`/`POOR_POSITIVE_LT_34` → `healthTier`; `PHASE_MINUS_ONE` → `currentPhase`. |
| `allOf` | All child predicates pass | Short-circuit failure is allowed but all children are recorded. |
| `anyOf` | At least one child passes | Failure records each child's result. |
| `not` | Child predicate fails | Used for negative assertions. |

`healthTier` is a derived enum: `unknown` (missing key or stored NaN; non-participant), `poor` (strictly positive health < 34; zero is a measured floor and also non-participant), `moderate` ([34, 67)), `healthy` (≥ 67).

Truth table for the three-valued signal state (present/absent/divergent): a threshold can only pass when its signal is present and internally consistent; an absent signal yields `partial`; a divergent signal yields `partial`; a present signal with a false predicate yields `fail`. No threshold can turn `partial`/`errored` into `pass`.

#### Schema versioning and migration

- **Identity**: the contract is identified by the versioned `$id` `.../contracts/scenario.v2.schema.json`, not by the authored file name. Published `vN` files and `$id`s are immutable; a breaking change creates a new file and a new `$id`.
- **Version field**: `schemaVersion` is an integer major (`2`), not SemVer. Because every object is strict (`additionalProperties: false`), any field addition, removal, rename, type change, or constraint tightening is a major bump. There are no minor/patch schema versions.
- **Compatibility mode**: `BACKWARD_TRANSITIVE` (C-SCN-31). Old scenarios remain readable by the runner for the supported major; the runner rejects an unsupported `schemaVersion` with exit `14`.
- **v1 → v2 migration**: v1 was a pre-release draft with `condition`-only thresholds. v2 replaces `condition` with the structured `predicate` oracle, adds `environment.apiVersion`, makes `summary`/`determinism`/`scenarioKind` required, and removes the unused `stepLibrary`/`keyword`/`args` fields. v1 scenarios are migrated by translating each `condition` into a `predicate` and adding the newly required fields; the runner does not auto-migrate at runtime.
- **Deprecated aliases**: `condition` (alias for `description`) and `files` (alias for `subject` in the evidence manifest) are retained for one major version, then removed.

### Evidence Bundle

A directory `evidence-bundle-<run-id>/` produced by every run. `runId` format: `run-<YYYYMMDDTHHMMSSZ>-<4 lowercase hex>` (C-BND-02). Layout per `specs/009/contracts/evidence-bundle.md` plus the spec-010 additions below.

| File | Purpose |
|------|---------|
| `manifest.json` | `schemaVersion`, `runId`, `timestamp` (ISO 8601 UTC), `toolVersions`, `gameVersion`, `modVersion`, `environment`, `subject` (array of `{name, digest: {sha256}}`), `verdict`, `honestyLevel`, `attempts`, optional `integrityFailureRef`, `compatibility`, `commands`, `materials`, `seeds`, `determinism`, completeness flags. `files` is a deprecated alias for `subject` for one major version (C-BND-01). |
| `manifest.sha256` | Detached SHA-256 of `manifest.json`, published outside the bundle (CI job summary/commit status/durable audit log) so manifest tampering is detectable even if the bundle directory is altered (C-SEC-35). |
| `claims.json` | `claimId`, `evidencePointer`, `risk` (low/medium/high), `honestyLevel` (W/R/E), `safeWording`, `refusalSite` (C-REL-24). |
| `artifacts/` | Screenshots, SMAPI logs, save-deltas before/after, reflection outputs, Crop-phase dump, Anomaly Report entries. |
| `attempts/<n>/attempt.json` | Per-attempt metadata (`attempt`, `verdict`, `failureClass`, `signals`, timestamps); stdout/stderr and intermediate artifacts live beside it. Append-only. |
| `junit.xml` | CI integration; quarantined scenarios emit `<skipped>`. |
| `allure.json` | Rich report; `flaky: true` metadata when a retry passed. |
| `integrity-failure.json` | Tamper-evident record on integrity failure (FR-024); append-only, retained with the bundle (FR-027). |
| `compatibility` block | Observed/minimum/maximum/tier per component, `modSelfCheck`, deviation (embedded in manifest). |

Mandatory evidence for honesty level R/E: `commands`, `materials`, at least one seed material, `claims.json`, and recomputed digests (C-SEC-29). A bundle missing any mandatory item for its claimed level fails closed in the offline verifier (C-SEC-34).

### Run Verdict

The **run outcome** is one of five base verdicts. `flaky` is a derived metadata flag, not a base verdict (C-REL-01/C-REL-26). Scenario-level flaky classification (`healthy`/`monitor`/`quarantined`/`insufficient-data`) is derived statistically from history (N ≥ 30) and is a separate concept from a single run's retry flag.

| Verdict | Condition | Exit |
|---------|-----------|------|
| `pass` | All three signals agree; thresholds satisfied; no divergence; expectedVerdict matches. | `0` |
| `fail` | Threshold violation, assertion failure, or host-guard breach; expectedVerdict `fail` matched. | `18` |
| `errored` | Crash, capture failure, version mismatch, save-preservation breach, missing required artifact, or expectedVerdict mismatch (contract failure). | `19` |
| `partial` | Incomplete signal set or signal disagreement (e.g., dump vs. save). | `19` |
| `skipped` | Quarantined or intentionally not run. | `20` (all skipped) / `0` (mixed suite with at least one pass) |

`flaky: true` is metadata on a run whose first attempt failed with a retryable class and a later attempt passed on the same commit/seed/environment. The run outcome is `pass` and the exit code is `0`; flakiness never fails the run by itself. A flaky pass is **not** eligible for auto-promotion to `verified` (FR-016); it is recorded in `test.md` as `partial` with `flaky: true` until a clean non-flaky pass occurs. The resolved policy is recorded in `spec.md` (Clarifications, Session 2026-10-27) and `contracts/exit-codes.md`.

Expected-fail semantics: when `expectedVerdict: fail` matches an actual `fail`, the verdict remains `fail` and the exit code remains `18` (the gameplay assertion failed as designed). A mismatch is a scenario-contract failure and exits `19`. Expected-failure lanes must opt into `--ignore-exit-code 18`; the override is recorded as an audit event. Integrity failures (`19` with `integrity-failure.json`) are never demotable.

### Signal Set

The three required signals plus optional visual evidence. Signals are distinct **observation channels**, not statistically independent failure domains: a game crash can invalidate all three at once. A shared failure mode is classified as `errored`, not as three independent failures. Signal independence means independent capture paths (persistence diff, in-process dump, log parse), not independent infrastructure.

| Signal | Source | Mandatory |
|--------|--------|-----------|
| SoilHealthState delta | Before/after `modData/soil_health_data_{saveId}.json` (SaveComparator, missing key → 0 at persistence layer, 0.0001f tolerance, NaN ignored) | Yes |
| Crop phase delta | In-process dump (primary oracle) + save-file inspection (cross-check); divergence → `partial` | Yes for host runs; farmhand runs assert absence of mutation |
| SMAPI log excerpt | Elapsed-ms trace line + `DayEnding` event name + host-guard skip line when applicable | Yes |
| Visual evidence | Screenshots/video (optional) | No |

Signal-to-field mapping: `soilHealthStateDelta` → `soilHealth`, `healthTier`, `sentinel`; `cropPhaseDelta` → `phaseDays`, `currentPhase`, `dayOfCurrentPhase`, `fullyGrown`, `dead`, `indexOfHarvest`; `smapiLog` → `elapsedMilliseconds`, `eventName`, `locationsProcessed`, `isMainPlayer`, `sentinel`.

### Compat Matrix (`compat-matrix.json`) and Game-Path Lock (`gamepath.lock.json`)

Version-controlled, in-repo. Single pinned stable line (latest SV 1.6.x + matching SMAPI). Six required assemblies: `Stardew Valley.dll`, `StardewModdingAPI.dll`, `StardewValley.GameData.dll`, `MonoGame.Framework.dll`, `xTile.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`. `gamepath.lock.json` holds the authoritative SHA-256 for the game archive and, when available, a per-file digest map used to re-verify every extracted file; it is updated only by reviewed PR. The release `.sha256` is advisory and never trusted alone.

| Component | Fields |
|-----------|--------|
| Stardew Valley | observed, minimum, maximum, tier (HARD-FAIL/WARN/INFO) |
| SMAPI | observed (SemanticVersion), minimum, maximum, tier |
| LivingRoots mod | version, MinimumApiVersion, MinimumGameVersion, modSelfCheck |
| Deviation | recorded when observed exceeds matrix maximum (null `MaximumGameVersion` → upper-bound deviation → warn) |

### Quarantine Registry (`quarantine.json`)

Committed in-repo at `specs/010-headless-verification-runner/quarantine.json`. One entry per quarantined scenario. CODEOWNERS-gated changes; automation proposes, a human approves.

| Field | Type | Notes |
|-------|------|-------|
| `scenarioId` | string | |
| `owner` | string | Resolved via `--quarantine-owner` → `QUARANTINE_OWNER` env → git identity → repo owner (lfgranja). |
| `timeBoxStart` | ISO 8601 | |
| `timeBoxDays` | integer | 14; one 14-day extension (28 total); then auto-disable (not auto-re-enable). |
| `extensionReason` | string | Required for the single extension; written reason + owner sign-off. |
| `flakyRunCount` | integer | Trigger at 2 flaky runs in a rolling 14-day window (or 2 of the last 3). |
| `status` | enum `{quarantined, disabled}` | `disabled` after the 28-day cap; removal from the blocking gate. |
| `linkedIssue` | string | GitHub issue URL; created/reopened at escalation. |

Gate layering: (1) a single run is `flaky` when attempt 1 fails with a retryable class and a later attempt passes; (2) 2 such runs in the window make the scenario a **quarantine candidate** (automation opens a pending entry); (3) a human approves entry; (4) confirmed-flaky **status** requires N ≥ 30 runs and a Wilson 95% CI excluding 100% with failure rate ≥ 30%; (5) pass-rate tiers (`healthy ≥ 99%`, `monitor 95–99%`, `< 95%` alert) are monitoring signals, not an independent quarantine trigger. Below N, the status is `insufficient-data`.

### Milestone Save Manifest (private builddeps repo)

| Field | Type | Notes |
|-------|------|-------|
| `gameVersion` | string | |
| `smapiVersion` | string | |
| `saveName` / `saveId` | string | |
| `sha256` | string | Of the save folder archive. |
| `documentedContents` | string | Human-readable description of progression state. |

### ILivingRootsModApi (production mod, Domain/Interfaces)

SMAPI-free interface exposed via `ModEntry.GetApi()`.

| Member | Type | Notes |
|--------|------|-------|
| `ApiVersion` | `string` (read-only) | Contract version of this interface (currently `1.0.0`). Scenarios declare the required version as `environment.apiVersion`; a mismatch is HARD-FAIL. Bump on any breaking signature change. |
| `SoilGrowthModifierCompleted` | `event EventHandler<SoilGrowthModifierCompletedEventArgs>` | Raised synchronously on game thread at tail of `OnDayEnding`, after `ApplySoilGrowthModifier` loop for all locations (success or caught exception), before day transition. |

`SoilGrowthModifierCompletedEventArgs`: `DayKey` (string), `IsMainPlayer` (bool), `LocationsProcessed` (int), `ElapsedMilliseconds` (long), `Success` (bool), `Error` (Exception?).

## State Transitions

- **Run verdict flow**: `pass` requires all three signals agree + thresholds satisfied + no divergence + expectedVerdict match. `partial`/`errored`/`fail` block auto-promotion; `flaky` and `quarantined` also block auto-promotion. `flaky: true` requires attempt 1 retryable failure + later pass on the same commit/seed/environment. `skipped` requires quarantine or intentional skip.
- **Quarantine flow**: active → (2 flaky runs in window) → quarantine candidate → human approval → quarantined (14-day time-box) → (one 14-day extension) → disabled (auto-disable + issue + owner/manager notification). Auto-disable is the only automatic terminal action; re-enablement requires stability evidence (≥20 consecutive shadow passes), never the calendar alone.
- **Save preservation**: original hashed before run; restored/verified byte-identical after; failure → `errored` with before/after state.
- **Version gate**: file-based classification before Xvfb launch → HARD-FAIL (exit 19) / WARN (proceed + record) / INFO (proceed + log).
- **Suite aggregation**: precedence `errored/partial (19) > fail (18) > policy/threshold (21) > no-tests (20) > pass (0)`. A mixed suite with at least one `pass` and some `skipped` exits 0; `flaky: true` never changes the aggregate exit code.
- **Root-doc promotion**: `test.md`/`ROADMAP.md`/`COMPLETION_SUMMARY.md` updates use GATE3 marker blocks; a missing/duplicate marker is exit 14. `--check-docs` reports drift with exit 21.

## Validation Rules

- Scenario JSON: strict `additionalProperties: false` at every object level; required fields per the schema's `required` arrays (`summary`, `determinism`, `scenarioKind`, and `environment.apiVersion` are required); `testMdPath` must exist and contain the GATE3 markers; `milestoneSave` and macro paths must be relative and traversal-free; `milestoneSave` and `environment.saveId` must both match the selected milestone manifest; `min <= max` for `between`; `cmp` `right` type must match the `left` field type; `multiplier.base` must be `pre.<field>`; `sentinel` must map to its declared field; `positive` scenarios cannot expect `fail`.
- Soil health thresholds: poor positive < 34 → 1.15×; healthy ≥ 67 → 0.85×; moderate [34, 67) → 1.00×; `phaseDays` never below 1; 99999 sentinel never touched; NaN tiles skipped; missing key / health ≤ 0 → non-participant.
- Performance: per-location elapsed ms ≤ 5 (single shot after discarded warmup; Gen0 delta recorded; >0 tagged `gc-attributed`).
- Save preservation: byte-identical original (tolerance 0.0001f for floats in SaveComparator; NaN ignored; missing key → 0 at persistence layer only).
- Isolation: `XDG_CONFIG_HOME`, `HOME`, `XDG_DATA_HOME`, `XDG_STATE_HOME`, `XDG_CACHE_HOME` set to per-instance dirs before launch; startup probe asserts the resolved `Constants.DataPath` is inside `XDG_CONFIG_HOME`; mismatch → `errored` (exit 19).
