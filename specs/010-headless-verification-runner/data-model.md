# Data Model: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-27
**Schema**: `contracts/scenario.schema.json` (Draft 2020-12, `schemaVersion: 2`, immutable `$id`)

This document defines the key entities the runner produces and consumes. Three machine-readable contracts are authoritative for field-level validation: `contracts/scenario.schema.json` (scenario input), `contracts/evidence-bundle.schema.json` (finalized `manifest.json`), and `contracts/never-quarantine.schema.json` (never-quarantine registry). This document mirrors them for readability and MUST stay in sync (CI consistency check, C-SCN-36). The on-disk evidence-bundle layout still follows `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md`, with the spec-010 additions recorded below; spec 010 does not maintain a second layout contract.

## Entities

### Status Terminology (qualified terms)

The words "pass", "verified", and "status" are ambiguous on their own. Every specification, report, and commit message MUST use one of the following qualified terms and MUST NOT substitute one for another.

| Term | Closed value set | Owner / writer | Meaning |
|------|------------------|----------------|---------|
| `testMdStatus` | `partial`, `machine-pass`, `verified` | Runner writes `partial`/`machine-pass`; a human maintainer writes `verified` | The `status` field inside one `test.md` GATE3 block. See `contracts/test-md-template.md`. |
| `gate3Status` | `partial`, `machine-pass`, `verified` | Runner writes `partial`/`machine-pass`; maintainer writes `verified` | Feature-level Gate 3 rollup in root docs / `VERIFICATION_PROTOCOL.md`. It is not a per-run verdict and is never inferred from a single run. |
| `scenarioStatus` | `active` (no registry entry), `candidate`, `quarantined`, `disabled`, `retired` | Automation proposes; a human approves | Quarantine lifecycle state from `quarantine.json`. Only this status affects the blocking gate, and only after human approval. |
| `statisticalStatus` | `not-evaluated`, `insufficient-data`, `confirmed-flaky` | Runner/analysis, derived | The N ≥ 30 / Wilson 95 % statistical classification. It is not a quarantine stage and is never required for quarantine. |

The following are **not** statuses and MUST NOT be rendered in a `status` field: the run verdict (`pass`, `fail`, `errored`, `partial`, `skipped`), the derived `flaky` mark, `honestyLevel` (`W`/`R`/`E`), and `compatibility.tier` (`INFO`/`WARN`/`HARD-FAIL`).

Two further collisions are resolved explicitly:

- **Run lease vs. evidence claim.** A *run lease* is a short-lived coordination file (`<resourceId>.claim` under `${XDG_STATE_HOME:-$HOME/.local/state}/livingroots/claims/`) used only to prevent two instances from mutating the same working copy or display. A lease is never evidence, never a status, and never a substitute for a claim in `claims.json`. An *evidence claim* is a row in `claims.json` (`claimId`, `evidencePointer`, `risk`, `honestyLevel`, `safeWording`, `refusalSite`). The unqualified word "claim" is ambiguous and MUST be qualified.
- **Run verdict vs. `test.md` status.** A run verdict `pass` is not a promotion. Only a clean, non-flaky, host-run `pass` with three agreeing signals and `honestyLevel ≥ R` may be written as `testMdStatus: machine-pass`. `verified` additionally requires the human `E` attestation and is never written by the runner.

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
| `environment.config` | object | yes | — (no implicit value) | `saveMode` (default `save`), `headless` (default `true`), `skipBuild` (default `false`). Members are optional; the object itself is required and must be written as `{}` when no member is set. |
| `milestoneSave` | string | yes | — | Source milestone save folder name in the private builddeps repo; must match the milestone manifest `saveName`. Path separators rejected. |
| `background` | array of init commands | no | `[]` | Shared preconditions executed before `initCommands` (Background/common block). |
| `initCommands` | array of objects | yes | `[]` allowed | `{kind: "macro", file}` or `{kind: "console", command}`. Macros are `.txt` files, one `/` command per line; console commands must not start with `debug `. |
| `steps` | array of step objects | yes | — | Ordered array (1–5 items); array order is the single source of truth (no `dependsOn`). |
| `steps[].action` | enum | yes | — | Closed primitive vocabulary: `sleep`, `warp`, `harvest`, `applyCompost`, `insertWaste`, `setSoilHealth`, `observe`, `dumpCropPhase`, `runMacro`. |
| `steps[].target` | string | yes | — | Tile/location/object identifier. |
| `steps[].days` | integer (1–28) | no | `1` | Days spanned by the action. |
| `steps[].inputs` | object | no | `{}` | Closed action-specific inputs (`tileX`, `tileY`, `location`, `itemId`, `count`, `soilHealth`, `macroFile`); `steps[].allOf` conditionals require `itemId`+`count` for `insertWaste`, `soilHealth` for `setSoilHealth`, and `macroFile` for `runMacro`. `soilHealth` is the typed 0–100 value; `count` is the item quantity. There is no generic `amount` field. |
| `thresholds` | array of threshold objects | yes | — | Machine-evaluable oracle; `{signal, subject?, predicate, modConstant?, description?, condition?}`. |
| `thresholds[].subject` | object | no | — | Selector `{location?, tileX?, tileY?}` (`minProperties: 1` if present) for a multi-record `cropPhaseDelta`/`soilHealthStateDelta` predicate. If supplied, it must match exactly one record; zero or multiple matches exit `14` (`unresolvable-subject`). Omitted means the step target must identify exactly one record (`ambiguous-subject`). Forbidden for single-record `smapiLog`. |
| `thresholds[].predicate` | object | yes | — | Discriminated `op`: `cmp`, `between`, `multiplier`, `sentinel`, `unchanged`, `allOf`/`anyOf`, `not`. |
| `thresholds[].modConstant` | string | no | — | Optional `ModConstants` member name; the numeric value in the predicate stays authoritative. If present, the runner MUST cross-check it and exit `14` on mismatch; absent means no check. |
| `thresholds[].condition` | string | no | `""` | Migration provenance: the original v1 human condition, retained only so the offline v1→v2 migration reviewer can compare it with the generated `predicate`. Never the oracle; new v2 scenarios use `description`. |
| `expectedSignals` | array of 3 unique enums | yes | — | Exactly `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog` (SC-002). |
| `timeoutSeconds` | integer (1–86400) | no | `300` | Per-scenario timeout; a macro timeout is retryable and retains inherited exit `4` after retries are exhausted. |
| `inputs` | object | no | `{}` | Scenario-level inputs: `season` (default `spring`), `day` (default `1`), `weather` (default `sunny`), `money` (default `500`). Declares `required: []`; no hidden global state. |
| `rngSeed` | integer | yes | — | Declared RNG seed for reproducibility. |
| `time` | string (ISO 8601 UTC) | yes | — | Declared time context (`YYYY-MM-DDTHH:MM:SSZ`). |
| `determinism` | object | yes | — | Required idempotency declaration plus any controlled non-deterministic sources. |
| `criticalPath` | boolean | no | `false` | Marks a never-quarantinable scenario (C-REL-23). |
| `scenarioKind` | enum `{positive, negative, boundary}` | yes | — | Positive declares `pass`; negative declares a non-pass verdict; boundary declares `pass` or `fail`. |
| `expectedVerdict` | enum `{pass, fail, errored, partial, skipped}` | conditional | `pass` | Required for negative/boundary (via `if/then`); a mismatch is a scenario-contract failure (exit 19). `flaky` is derived metadata, not a base verdict. |
| `hostGuard` | object | conditional | — | Required when `playerRole=farmhand`; forbidden when `playerRole=host`: `skipLine` (const), `forbiddenEvidence` (`elapsedTrace`, `phaseDaysMutation`). The skip line is always mandatory in v2; there is no `requireSkipLine` toggle. |

Validation: strict `additionalProperties: false` at every object level; bundled `$ref`s at build time; `JsonSchema.Net` on the runner side; `check-jsonschema`/`ajv-cli` in CI/pre-commit.

#### Nested required-field mirror (C-SCN-36)

The union of `required` arrays in `contracts/scenario.schema.json` is authoritative. This table mirrors it for readability; the CI consistency check asserts equality with the schema.

| Object path | Required fields |
|-------------|-----------------|
| `#` (root) | `schemaVersion`, `scenarioId`, `featureSlug`, `testMdPath`, `summary`, `playerRole`, `minimumGameVersion`, `environment`, `milestoneSave`, `initCommands`, `steps`, `thresholds`, `expectedSignals`, `rngSeed`, `time`, `determinism`, `scenarioKind` |
| `environment` | `gameVersion`, `smapiVersion`, `modVersion`, `apiVersion`, `saveId`, `config` |
| `environment.config` | none (members optional) |
| `inputs` | none (members optional) |
| `determinism` | `idempotent`, `declaredSources` |
| `initCommands[]` (macro) | `kind`, `file` |
| `initCommands[]` (console) | `kind`, `command` |
| `steps[]` | `action`, `target` |
| `steps[].inputs` | none (members optional) |
| `thresholds[]` | `signal`, `predicate` |
| `predicate` (`cmp`) | `op`, `left`, `operator`, `right` |
| `predicate` (`between`) | `op`, `field`, `min`, `max`, `tolerance` |
| `predicate` (`multiplier`) | `op`, `field`, `base`, `factor` |
| `predicate` (`sentinel`) | `op`, `field`, `sentinel` |
| `predicate` (`unchanged`) | `op`, `field` |
| `predicate` (`allOf`/`anyOf`) | `op`, `predicates` |
| `predicate` (`not`) | `op`, `predicate` |
| `hostGuard` (farmhand only) | `skipLine`, `forbiddenEvidence` |
| Conditional (`playerRole=farmhand`) | `hostGuard` |
| Conditional (`scenarioKind=negative` or `boundary`) | `expectedVerdict` |

#### Absence vs. null vs. default

| Form | Meaning | Schema behavior |
|------|---------|-----------------|
| Field omitted (optional) | Author accepts the declared default | Valid; runner applies the default. |
| Field omitted (required) | Incomplete scenario | Invalid; fail-fast exit 14. |
| Field present with `null` | Never a meaningful value | Invalid for every field; no field is nullable. |
| Field present with `""` | Explicit empty string where `minLength` allows it | Valid only for `description` and `condition` (both default `""`); **not** for `summary` (required `minLength: 1`), identifiers, or paths. |
| `"NaN"` string | Unmeasured/NaN sentinel (numeric fields) | Valid only through the named `sentinel` enum; the string `"NaN"` is never a `cmp` operand and `numericValue` was removed in v2. |
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

All predicates evaluate against the captured signal named by `thresholds[].signal`; a predicate that references a field not present in that signal is a scenario-contract error (exit 14). If the actual value is absent or `"NaN"`, every numeric predicate (`cmp`, `between`, `multiplier`, `unchanged`) **fails** with reason `unmeasured-actual`; only the `sentinel` predicate may assert unmeasured or missing values.

| `op` | Pass condition | Operand rules |
|------|----------------|---------------|
| `cmp` | `left <operator> right` | `left` is a signal field; `right` is numeric for numeric fields, boolean for boolean fields, and a string for string fields. `lt/lte/gt/gte` only for numeric fields. `"NaN"` is forbidden. `healthTier` operands must use the `unknown/nonParticipant/poor/moderate/healthy` enum. Tolerance truth table: `eq` passes iff `|left − right| <= tolerance`; `ne` passes iff `|left − right| > tolerance`; ordering operators compare exact values and ignore tolerance. |
| `between` | `effectiveMin <= actual <= effectiveMax` with inclusive bounds by default | Numeric fields only. `effectiveMin = min − tolerance`, `effectiveMax = max + tolerance`; `exclusiveMin`/`exclusiveMax` default `false` (inclusive) and apply to the effective bounds. `tolerance` is required. `min <= max` is enforced by the runner (exit 14 if violated). |
| `multiplier` | `actual == clamp(rounding(base × factor), fieldMinimum)` within `tolerance` | Numeric fields only. `base` is a pre-run field reference (`pre.<field>`) whose suffix MUST equal `field`; `factor` is bounded to `[0.01, 100]`; `rounding` defaults to `round` (midpoint-to-even) and `clampToFieldMinimum` defaults to `true`. Field minima: `phaseDays` 1; all other numeric fields 0. The 99999 sentinel is never multiplied or clamped. |
| `sentinel` | Named sentinel condition holds; if `mustNotChange=true`, the same condition also holds in the pre-run capture; if `false`, only the post-run capture is asserted | Sentinel-to-field mapping is enforced: `SENTINEL_UNTOUCHED_99999` → `phaseDays` (`== 99999`); `NAN_HEALTH`/`MISSING_KEY`/`HEALTH_LE_ZERO`/`POOR_POSITIVE_LT_34` → `healthTier` (`unknown`/`unknown`/`nonParticipant`/`poor`), with `NAN_HEALTH` and `MISSING_KEY` further distinguished by `soilHealthState` (`nan` vs `missing`); `PHASE_MINUS_ONE` → `currentPhase` (`== -1` exactly). |
| `unchanged` | `|post − pre| <= tolerance` | Numeric fields only; `tolerance` defaults to 0. This is the expected-absence/no-mutation oracle (for example, farmhand runs). |
| `allOf` | All child predicates pass | Short-circuit failure is allowed but all children are recorded. |
| `anyOf` | At least one child passes | Failure records each child's result. |
| `not` | Child predicate fails | Used for negative assertions. |

`healthTier` is a derived five-value enum: `unknown` (missing key or stored NaN; unmeasured), `nonParticipant` (measured health ≤ 0; skipped by the growth modifier), `poor` (`0 < health < 34`), `moderate` ([34, 67)), `healthy` (≥ 67). A measured zero may still render as the game's Poor visualization category; that is a visualization concern, separate from growth participation.

Truth table for the three-valued signal state (present/absent/divergent): a threshold can only pass when its signal is present and internally consistent; an absent signal yields `partial`; a divergent signal yields `partial`; a present signal with a false predicate yields `fail`. No threshold can turn `partial`/`errored` into `pass`.

#### Schema versioning and migration

- **Identity**: the contract is identified by the versioned logical `$id` `urn:lfgranja:livingroots:scenario:v2`, not by the authored file name and not by a file path. Published `vN` files and `$id`s are immutable; a breaking change creates a new file and a new `$id`.
- **Version field**: `schemaVersion` is an integer major (`2`), not SemVer. Because every object is strict (`additionalProperties: false`), any field addition, removal, rename, type change, or constraint tightening is a major bump. There are no minor/patch schema versions.
- **Compatibility mode**: `BACKWARD_TRANSITIVE` (C-SCN-31) for published v2-and-later scenarios. The v2 runner does not read the pre-release v1 draft; v1 scenarios must be migrated offline. The runner rejects an unsupported `schemaVersion` with exit `14`.
- **v1 → v2 migration**: v1 was a pre-release draft with `condition`-only thresholds. v2 replaces `condition` with the structured `predicate` oracle, adds `environment.apiVersion`, makes `summary`/`determinism`/`scenarioKind` required, and removes the unused `stepLibrary`/`keyword`/`args` fields. v1 scenarios are migrated by translating each `condition` into a `predicate` and adding the newly required fields; the runner does not auto-migrate at runtime.
- **Retained compatibility fields**: `condition` (v1 migration provenance, never the oracle) and `files` (a deprecated alias for `subject` in the evidence manifest) are retained for one major version, then removed. `files` uses exactly the same shape as `subject` — an array of `{name, digest: {sha256}}` — and is **never** `{path, sha256}`. Spec 009's contract still documents the old `{path, sha256}` shape and MUST be reconciled to this shape (open reconciliation item; see §Validation Rules → Contract alignment with `contracts/scenario.schema.json`).

### CropPhaseDump (Crop-phase signal artifact)

The dev/CI-only `LivingRoots.Verification` mod writes one JSON dump per capture at `{modFolder}/data/crop_phase_dump_{sanitizedDayKey}.json`, with a detached `...json.done` sidecar sentinel written **last**. The dump is the primary `cropPhaseDelta` oracle; save-file inspection is the independent cross-check. The dump MUST NOT contain any `expected*` field — the runner computes expected values from `basePhaseDays` and the ModConstants multipliers.

**Top-level fields** (`schemaVersion: 1`):

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `schemaVersion` | const `1` | yes | Dump payload version, independent of the scenario schema version. |
| `dayKey` | string | yes | In-game day identifier used for correlation (`season` + day, stable across a run). |
| `capturedAtUtc` | string (ISO 8601 UTC) | yes | Capture timestamp; runner correlates with `dayKey` and rejects a mismatched/stale capture. |
| `isMainPlayer` | boolean | yes | `true` only for the host; a farmhand dump is itself a host-guard breach. |
| `locationsProcessed` | integer ≥ 0 | yes | Locations enumerated in this dump; must equal the SMAPI log's `locationsProcessed`. |
| `elapsedMilliseconds` | integer ≥ 0 | yes | Dump traversal time; informational, never a pass criterion. |
| `done` | boolean | yes | Must be `true`; must agree with the `.done` sidecar (C-DMP-02). |
| `crops` | array of crop objects | yes | May be empty; one entry per `HoeDirt` with a non-null crop. |

**Per-crop fields**:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `location` | string | yes | Location name from the same location set as `OnDayEnding` (`{current, Greenhouse, Farm}`). |
| `tileX`, `tileY` | integer | yes | Tile coordinates. |
| `tileKey` | string | yes | Stable selector equivalent to the scenario `thresholds[].subject`. |
| `seedId` | string | yes | Seed item identifier. |
| `isForageCrop` | boolean | yes | Forage crops are excluded from the growth oracle. |
| `indexOfHarvest` | integer | yes | Harvest index. |
| `currentPhase` | integer | yes | `-1` is the game's excluded sentinel phase (`PHASE_MINUS_ONE`). |
| `dayOfCurrentPhase` | integer | yes | Days spent in the current phase. |
| `phaseDays` | array of integers | yes | Current per-phase day budget; the post-run value the oracle reads. |
| `basePhaseDays` | array of integers | yes | Phase budget read from `CropData` **without** calling `ResetPhaseDays`; the pre-modifier baseline. |
| `fullyGrown` | boolean | yes | |
| `dead` | boolean | yes | |
| `soilHealth` | number in [0, 100] or `null` | yes | `null` iff the value is missing or NaN; a measured `0` is a real floor, never `null`. |
| `healthTier` | enum `unknown`, `nonParticipant`, `poor`, `moderate`, `healthy` | yes | Derived tier; exactly the five-value set used by the scenario predicates. |
| `soilHealthState` | enum `measured`, `missing`, `nan` | yes | Disambiguates a missing key from a stored NaN; required for `NAN_HEALTH`/`MISSING_KEY`. |
| `isSentinelPhase` | boolean | yes | `true` when `currentPhase == -1`. |

Enumeration and derivation rules:

- Enumerate `location.terrainFeatures.Pairs` filtered to `HoeDirt` with `crop != null`, over the same location set as `OnDayEnding`. Garden pots (`IndoorPot`) are excluded to match the modifier scope.
- `soilHealthState: measured` with health ≤ 0 → `healthTier: nonParticipant`; `0 < health < 34` → `poor`; `[34, 67)` → `moderate`; `≥ 67` → `healthy`. `soilHealthState: missing` or `nan` → `healthTier: unknown` (unmeasured, never `poor`).
- Host only. A farmhand run MUST produce no dump and MUST assert the host-guard skip line plus the absence of `phaseDaysMutation`/`elapsedTrace`.

**Asynchronous write ordering** (C-DMP-01/C-DMP-02):

1. Delete any stale `.done` sentinel at capture start.
2. Write the payload to a temp file in the same directory.
3. `fsync` the temp file.
4. Atomically rename over the final path.
5. `fsync` the parent directory.
6. Write the `.done` sidecar sentinel **last** and `fsync` it.
7. Set `done: true` in the payload. The JSON field and the sidecar must agree.

The write runs asynchronously off the frame callback; the runner polls for the `.done` sentinel within `timeoutSeconds` and never treats the presence of the data file alone as completion. Missing sentinel, `done: false`, sidecar/field disagreement, `dayKey` mismatch, or a stale `capturedAtUtc` yields `partial` (exit `19`). A write/fsync/rename failure is a capture-path failure and yields `errored` (exit `19`).

### Evidence Bundle

A directory `evidence-bundle-<run-id>/` produced by every run. `runId` format: `run-<YYYYMMDDTHHMMSSZ>-<4 lowercase hex>` (C-BND-02). Layout per `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` plus the spec-010 additions below.

| File | Purpose |
|------|---------|
| `manifest.json` | The run manifest, written **only** by the single `ManifestBuilder` producer. See the field table below. |
| `manifest.sha256` | **Mandatory** detached SHA-256 anchor of the finalized `manifest.json`, stored alongside the bundle (outside `manifest.json` itself) and recorded in the audit log, so manifest tampering is detectable even if the bundle directory is altered (C-BND-01/FR-014). It is written after finalization and is the offline verifier's trust root. A missing anchor yields `errored` (exit `19`). |
| `claims.json` | `claimId`, `evidencePointer`, `risk` (low/medium/high), `honestyLevel` (W/R/E), `safeWording`, `refusalSite` (C-REL-24). Referenced by the manifest as `claimsFile: "claims.json"`. |
| `artifacts/` | Screenshots, SMAPI logs, save-deltas before/after, reflection outputs, Crop-phase dump, Anomaly Report entries. The manifest's `artifacts[]` lists the relative paths. |
| `attempts/<n>/attempt.json` | Per-attempt metadata (`attempt`, `verdict`, `failureClass`, `signals`, timestamps); stdout/stderr and intermediate artifacts live beside it. Append-only. **This is the only path family allowed inside the top-level `attempts[]`.** |
| `attempts/<n>/manifest.json` | Per-attempt digest manifest for that attempt's own artifacts. It is listed in the top-level `subject[]` (with its digest), **never** in `attempts[]`. The discarded `attempts/warmup/` directory is not an attempt. |
| `junit.xml` | CI integration; quarantined scenarios emit `<skipped>`. |
| `allure.json` | Rich report; `flaky: true` metadata when a retry passed. |
| `integrity-failure.json` | Tamper-evident record on integrity failure (FR-024); append-only, retained with the bundle (FR-027); referenced by the optional `integrityFailureRef`. |

#### Manifest field set (single producer: `ManifestBuilder`)

`ManifestBuilder` is the **only** component allowed to create or modify `manifest.json` and its detached `manifest.sha256`. No other runner component, test harness, or external tool may append to either file. `manifest.sha256` is computed over the finalized manifest bytes and written after the manifest is closed; a producer that writes the anchor first is invalid.

| Field | Required | Type / values | Notes |
|-------|----------|---------------|-------|
| `schemaVersion` | yes | const `1` | Manifest payload version. |
| `runId` | yes | `run-<YYYYMMDDTHHMMSSZ>-<4 lowercase hex>` | Must equal the `evidence-bundle-<runId>/` directory component (C-BND-02). |
| `timestamp` | yes | ISO 8601 UTC | Run start or finalization time; one value per manifest. |
| `toolVersions` | yes | object, non-empty | Free-form tool-name → version map (`dotnet`, `smapi`, runner). |
| `gameVersion` | yes | semver | Observed game version. |
| `modVersion` | yes | semver | Observed mod version. |
| `environment` | yes | object | Observed environment: `gameVersion`, `smapiVersion`, `modVersion`, `apiVersion`, `saveId`, and optional `mode`/`config`. |
| `environmentOverrides` | no | array of `{key, value}` | Applied `--override`/`--milestone-save` entries, recorded so a run is reproducible and auditable (FR-008). |
| `expectedSignals` | yes | exactly 3 unique enums | Copied from the scenario: `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`. |
| `subject` | yes | array of `{name, digest: {sha256}}` | Canonical, SLSA-aligned file-digest list. Includes every artifact, `claims.json`, per-attempt manifests, and `integrity-failure.json` when present. |
| `files` | no | **same shape as `subject`** | Deprecated alias retained for one major version. It is `{name, digest: {sha256}}`, **not** `{path, sha256}`; the verifier prefers `subject` when both are present. See §Contract alignment for the spec 009 reconciliation item. |
| `verdict` | yes | `pass`/`fail`/`errored`/`partial`/`skipped` | Aggregate run verdict. `flaky` is never a verdict value. |
| `honestyLevel` | yes | `W`/`R`/`E` | `E` is human-assigned only. |
| `attempts` | yes | array of path strings | Append-only; each item matches `attempts/<n>/attempt.json`. Per-attempt manifests are in `subject[]`, never here. |
| `claimsFile` | yes | `"claims.json"` | Required bundle member. |
| `artifacts` | yes | array of relative paths | The captured artifacts, including the three mandatory signal artifacts when present. Each path MUST also appear in `subject[].name`. |
| `compatibility` | no | object | Observed/minimum/maximum/tier per component, `modSelfCheck`, `deviation` (embedded in the manifest). |
| `commands` | no | array of command records | argv, workingDir, timestamps, exitCode, stdout/stderr refs (C-SEC-29). Mandatory for R/E. |
| `materials` | no | array of resource descriptors | `uri`, `sha256`, `name`, `mediaType`, `size`, `annotations.role`; roles `game-file`, `mod-manifest`, `tool`, `seed-save`, `dataset-instance` (C-SEC-29). Mandatory for R/E. |
| `seeds` | no | array of seed records | At least one seed material is mandatory for R/E; carries `seedId`, `source`, `baseCommit`/`baseSaveHash`. |
| `determinism` | no | object | `idempotent` (const true) + `declaredSources`; copied from the scenario when present. |
| `commandsCompleteness`, `materialsCompleteness`, `seedsCompleteness` | no | boolean, default `false` | `true` only when the evidence is provably complete/hermetic (C-SEC-29). |
| `suiteRerunCount` | no | integer ≥ 0 | Suite reruns consumed (`RunnerMaxSuiteReruns = 4`). |
| `suiteRerunCapHit` | no | boolean | `true` when the suite rerun cap was reached; explains a `no-tests`/`policy` outcome. |
| `cleanup_failed` | no | boolean, default `false` | `true` when working-copy/save cleanup failed to complete; forces `errored` (exit `19`) even if the gameplay verdict was `pass`. |
| `ignoreExitCodeRefused` | no | boolean, default `false` | `true` when `--ignore-exit-code` was refused for an integrity/security or claim-overreach failure; the process exits with the original code. |
| `integrityFailureRef` | no | `"integrity-failure.json"` | Present only when an integrity failure occurred. |

`manifest.sha256` is deliberately **not** a field of `manifest.json`; it is external.

Mandatory evidence for honesty level R/E: `commands`, `materials`, at least one seed material, `claims.json`, and recomputed digests (C-SEC-29). A bundle missing any mandatory item for its claimed level fails closed in the offline verifier (FR-015/C-SEC-29).

### Save Set and Hash Targets

Three save terms are distinct and MUST NOT be used interchangeably. The runner never writes to the player's real save, and the source milestone save is never mutated.

| Term | Role | Mutation | Hash target |
|------|------|----------|-------------|
| **Player save** | The user's real `Saves/<Name>/` under the real game data path | Never opened, copied, or modified | None. There is no hash target; the startup probe asserts the redirected `XDG_CONFIG_HOME`/`Constants.DataPath` is inside the instance root. A read or write of the player-save path is `errored` (exit `19`). |
| **Source milestone save** | Curated read-only folder from the private builddeps repo, named by `milestoneSave`/`saveName` | Read-only; only copied from | `sha256(source archive/folder)` recorded in the milestone manifest and recomputed at acquisition; it MUST match and MUST remain unchanged after the run. |
| **Working copy** | Disposable per-instance copy under the redirected `XDG_CONFIG_HOME` | Mutated by the scenario; restored after capture | `sha256(working copy before run)` and `sha256(working copy after restore)`; the post-restore digest MUST equal the source digest. |

- The byte-identical rule applies to the save folder and its sidecars (`soil_health_data_{saveId}.json`, `composting_bins_{saveId}.json`); the `0.0001f` tolerance belongs only to the `SaveComparator` float comparison, not to the save-folder digest.
- A working-copy mismatch after restore is `errored` (exit `19`), non-retryable, and records before/after state. A source-digest mismatch at acquisition is an integrity failure (`integrity-failure.json`, exit `19`).
- Backups cover `Saves/<Name>/` plus the enumerated sidecars and the whole mod-data tree as a completeness net (C-CON-20).

### Run Verdict

The **run outcome** is one of five base verdicts. `flaky` is a derived metadata flag, not a base verdict (C-REL-01/C-REL-26). Three flaky axes are kept separate and MUST NOT be conflated: (a) the **per-run mark** (`flaky: true`); (b) the qualified **`scenarioStatus`** (`candidate` → `quarantined` → `disabled`/`retired`, plus the independent `statisticalStatus` `confirmed-flaky`/`insufficient-data`); and (c) the **suite signal** (`healthy` ≥ 99 %, `monitor` 95–99 %, `alert` < 95 %, flaky fraction). Only `scenarioStatus` affects the blocking gate, and only after human approval.

| Verdict | Condition | Exit |
|---------|-----------|------|
| `pass` | All three signals agree; thresholds satisfied; no divergence; expectedVerdict matches. | `0` |
| `fail` | Threshold violation, assertion failure, or host-guard breach; expectedVerdict `fail` matched. | `18` |
| `errored` | Crash, capture-path failure, version mismatch, save-preservation/working-copy breach, **missing bundle or contract artifact** (`manifest.json`, `claims.json`, `manifest.sha256`, or a per-attempt record named in `attempts[]`), save I/O error after retries, or expectedVerdict mismatch (contract failure). | `19` |
| `partial` | **One or more of the three mandatory signal artifacts missing** (incomplete signal set), signal disagreement (e.g., dump vs. save, `.done` sidecar mismatch), or a spurious/undecodable signal. | `19` |
| `skipped` | Quarantined or intentionally not run. | `20` when **all** selected scenarios are skipped; `0` in a mixed suite with at least one `pass`. |

#### Missing-artifact verdicts

The distinction is deliberate: a missing **signal** is an incomplete observation, while a missing **manifest/claims/anchor** means the bundle cannot be trusted at all.

| Missing item | Verdict | Exit |
|--------------|---------|------|
| Any mandatory signal artifact: SoilHealthState delta, Crop-phase dump, SMAPI log excerpt | `partial` | `19` |
| `manifest.json` | `errored` | `19` |
| `manifest.sha256` (detached anchor) | `errored` | `19` |
| `claims.json` | `errored` | `19` |
| A per-attempt record named in `attempts[]` | `errored` | `19` |
| `integrity-failure.json` when a non-zero integrity exit occurred | `errored` | `19` |

A signature/capture-path failure (as opposed to an absent artifact) is also `errored`; the missing-artifact table is about absence, not about a signal that was captured but is unreadable. Farmhand host-guard runs still require all three channels; their Crop-phase channel asserts absence of mutation rather than a phase delta.

**Exit exemptions inside `errored`:** the Run Verdict table's `19` is the default for `errored`. Two classified transients keep a different inherited code: a **save I/O error** exits `19` (retried first), while a **macro timeout** keeps inherited code `4` (TIMEOUT) after retries. Both still record `verdict: errored` in the attempt record; only the process exit code differs. `cleanup_failed: true` forces `19` and can never hide behind the `4` exemption.

`flaky: true` is metadata on a run whose first attempt failed with a retryable class and a later attempt passed on the same commit/seed/environment. The run outcome is `pass` and the exit code is `0`; flakiness never fails the run by itself. A flaky pass is **not** eligible for promotion to `machine-pass` or `verified` (FR-016); it is recorded in `test.md` as `testMdStatus: partial` with `flaky: true` until a clean non-flaky pass occurs. The resolved policy is recorded in `spec.md` (Clarifications, Session 2026-10-27), `contracts/test-md-template.md`, and `contracts/exit-codes.md`.

Expected-verdict semantics: a `negative` scenario may declare any non-pass `expectedVerdict` (`fail`, `errored`, `partial`, `skipped`); `positive` scenarios are restricted to `pass`, and `boundary` scenarios to `pass`/`fail`. When it matches the actual verdict, the verdict is preserved and the exit code follows that verdict's normal mapping (`fail` → `18`; `errored`/`partial` → `19`; `skipped` → `20` when the whole selection is skipped, or `0` in a mixed suite with at least one `pass`). A mismatch — including an unexpected pass — is a scenario-contract failure and exits `19`. Expected-failure lanes must opt into `--ignore-exit-code 18` for a matched `fail`; the override is recorded as an audit event and never rewrites the verdict to `pass`. Integrity/security failures (FR-024/FR-025) and claim/honesty overreach are never demotable: the runner records `ignoreExitCodeRefused: true` and exits with the original code. Code `22` is reserved and cannot be listed.

### Signal Set

The three required signals plus optional visual evidence. Signals are distinct **observation channels**, not statistically independent failure domains: a game crash can invalidate all three at once. A shared failure mode is classified as `errored`, not as three independent failures. Signal independence means independent capture paths (persistence diff, in-process dump, log parse), not independent infrastructure.

| Signal | Source | Mandatory |
|--------|--------|-----------|
| SoilHealthState delta | Before/after `modData/soil_health_data_{saveId}.json` (SaveComparator, missing key → 0 at persistence layer, 0.0001f tolerance, NaN ignored) | Yes |
| Crop phase delta | In-process `CropPhaseDump` (primary oracle, `.done` sentinel required) + save-file inspection (cross-check); divergence → `partial` | Yes for host runs; farmhand runs assert absence of mutation |
| SMAPI log excerpt | Elapsed-ms trace line + `DayEnding` event name + host-guard skip line when applicable | Yes |
| Visual evidence | Screenshots/video (optional) | No |

**Signal failure modes** (C-REL-12): *failed* = the capture path errored (`errored`, unless the cause is a classified transient, then retry); *incomplete* = the signal artifact is absent (`partial`); *divergent* = two captured signals disagree (`partial`). A missing `manifest.json`/`claims.json`/`manifest.sha256` is **not** a signal failure — it is `errored` (see §Missing-artifact verdicts). A shared failure mode across signals (e.g., a crash) is one `errored`, never three independent failures.

Signal-to-field mapping (authoritative; the schema's `signal` description repeats it): `soilHealthStateDelta` → `soilHealth`, `healthTier`, `soilHealthState`; `cropPhaseDelta` → `phaseDays`, `currentPhase`, `dayOfCurrentPhase`, `fullyGrown`, `dead`, `indexOfHarvest`, `healthTier`, `soilHealthState`; `smapiLog` → `elapsedMilliseconds`, `eventName`, `locationsProcessed`, `isMainPlayer`. The `sentinel` predicate is additional and uses the sentinel-to-field mapping. `healthTier` is available in both delta signals because the dump carries it alongside the crop record. A predicate referencing a field outside its signal's row is a scenario-contract error (exit `14`).

### Compat Matrix (`compat-matrix.json`) and Game-Path Lock (`gamepath.lock.json`)

Version-controlled, in-repo. Single pinned stable line (latest SV 1.6.x + matching SMAPI). Six required assemblies: `Stardew Valley.dll`, `StardewModdingAPI.dll`, `StardewValley.GameData.dll`, `MonoGame.Framework.dll`, `xTile.dll`, `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`. `gamepath.lock.json` holds the authoritative SHA-256 for the game archive **and a mandatory per-file digest map** used to re-verify every extracted file; an absent or incomplete map fails closed with `errored` (exit `19`). It is updated only by reviewed PR. The release `.sha256` is advisory and never trusted alone. `compat-matrix.json` also declares `failureClasses` (the classified retryable/non-retryable exit-code taxonomy per supported SMAPI version) so retry classification is version-controlled (C-REL-06).

| Component | Fields |
|-----------|--------|
| Stardew Valley | observed, minimum, maximum, tier (HARD-FAIL/WARN/INFO) |
| SMAPI | observed (SemanticVersion), minimum, maximum, tier, `failureClasses` |
| LivingRoots mod | version, MinimumApiVersion, MinimumGameVersion, modSelfCheck |
| Deviation | recorded when observed exceeds matrix maximum (null `MaximumGameVersion` → upper-bound deviation → warn) |

### Quarantine Registry (`quarantine.json`)

Committed in-repo at `specs/010-headless-verification-runner/quarantine.json`. The registry is a strict envelope `{"schemaVersion": 1, "entries": []}`; each entry is one scenario. CODEOWNERS-gated changes; automation proposes, a human approves. `scenarioStatus` is the only status vocabulary here; it is never conflated with `testMdStatus`, `gate3Status`, or `statisticalStatus`.

| Field | Type | Notes |
|-------|------|-------|
| `scenarioId` | string | |
| `owner` | string | Resolved via `--quarantine-owner` → `QUARANTINE_OWNER` env → git identity → repo owner (lfgranja). |
| `timeBoxStart` | ISO 8601 | |
| `timeBoxDays` | integer | 14; one 14-day extension (28 total); then auto-disable (not auto-re-enable). |
| `extensionReason` | string | Required for the single extension; written reason + owner sign-off. |
| `flakyRunCount` | integer | **The only automatic trigger**: 2 flaky runs in a rolling 14-day window. There is no "2 of the last 3" alternative. |
| `status` | enum `{candidate, quarantined, disabled, retired}` | `candidate` is automation-proposed and awaits human approval; `quarantined` is approved and in the time-box; `disabled` is the automatic terminal state at the 28-day cap; `retired` is human-approved permanent removal from the suite, distinct from auto-disable. |
| `shadowPassCount` | integer | Consecutive out-of-band shadow passes; ≥ 20 is required for re-enablement. |
| `shadowHistory` | array of `{runId, verdict, date}` | Append-only shadow-run record; a `fail`/`errored`/`partial` resets the consecutive count. A shadow run is never counted toward promotion or `flakyRunCount`. |
| `linkedIssue` | string | GitHub issue URL; created/reopened at escalation. |

Gate layering: (1) a single run is `flaky` when attempt 1 fails with a retryable class and a later attempt passes; (2) **only** 2 such runs in the rolling 14-day window make the scenario a **quarantine candidate** (automation opens a pending entry); (3) a human approves entry; (4) `statisticalStatus: confirmed-flaky` is a separate statistical classification requiring N ≥ 30 runs and a Wilson 95 % CI whose upper bound is < 1.0 with point failure rate ≥ 30 %; below N it is `insufficient-data`; (5) pass-rate tiers (`healthy ≥ 99 %`, `monitor 95–99 %`, `< 95 %` alert) are monitoring signals only. Neither `statisticalStatus` nor the tiers is a quarantine trigger, and `confirmed-flaky` is not a quarantine stage.

### Never-Quarantine Registry (`never-quarantine.json`)

Committed in-repo at `specs/010-headless-verification-runner/never-quarantine.json` and validated by `contracts/never-quarantine.schema.json` (Draft 2020-12, strict). It is CODEOWNERS-gated: automation may never add a class, remove a critical-path scenario, or weaken the file, and changes require maintainer approval.

| Field | Type | Notes |
|-------|------|-------|
| `schemaVersion` | const `1` | Registry payload version. |
| `classes` | array of the closed enum below | Exactly the full closed set; no class may be added without a schema major bump. |
| `criticalPathScenarios` | array of `{scenarioId, featureSlug, reason?}` | Every scenario tagged `criticalPath: true` in its scenario definition; these are never quarantinable. |
| `updatedAt` | ISO 8601 UTC | Last reviewed change. |
| `owner` | string | CODEOWNERS handle accountable for the registry. |

Closed class set: `host-guard-breach`, `version-mismatch`, `missing-dependency`, `threshold-violation`, `crash`, `incomplete-signals`, `integrity-failure`, `security-failure`, `deterministic-regression`, `claim-overreach`.

A scenario or run matching any class is **never** proposed for quarantine: the failure is a regression or a security/integrity event to bisect, fix, or escalate. A deterministic 100 %-failing scenario is a regression, not flake. Security/integrity failures and claim overreach are additionally never demotable via `--ignore-exit-code`.

### Source Milestone Save Manifest (private builddeps repo)

| Field | Type | Notes |
|-------|------|-------|
| `gameVersion` | string | |
| `smapiVersion` | string | |
| `saveName` / `saveId` | string | Must match the scenario's `milestoneSave` and `environment.saveId`. |
| `sha256` | string | Hash target for the read-only source milestone save folder/archive; verified at acquisition and after the run. |
| `documentedContents` | string | Human-readable description of progression state. |

### ILivingRootsModApi (production mod, Domain/Interfaces)

SMAPI-free interface exposed via `ModEntry.GetApi()`.

| Member | Type | Notes |
|--------|------|-------|
| `ApiVersion` | `string` (read-only) | Contract version of this interface (currently `1.0.0`). Scenarios declare the required version as `environment.apiVersion`; a mismatch is HARD-FAIL. Bump on any breaking signature change. |
| `SoilGrowthModifierCompleted` | `event EventHandler<SoilGrowthModifierCompletedEventArgs>` | Raised synchronously on game thread at tail of `OnDayEnding`, after `ApplySoilGrowthModifier` loop for all locations (success or caught exception), before day transition. |

`SoilGrowthModifierCompletedEventArgs`: `DayKey` (string), `IsMainPlayer` (bool), `LocationsProcessed` (int), `ElapsedMilliseconds` (long), `Success` (bool), `Error` (Exception?).

## State Transitions

- **Run verdict flow**: `pass` requires all three signals agree + thresholds satisfied + no divergence + expectedVerdict match. `partial`/`errored`/`fail` block promotion; `flaky` and `scenarioStatus: quarantined` also block promotion. `flaky: true` requires attempt-1 retryable failure + later pass on the same commit/seed/environment. `skipped` requires quarantine or intentional skip. A clean non-flaky host `pass` may write `testMdStatus: machine-pass`; it never writes `verified`.
- **Quarantine flow**: `active` (no entry) → (2 flaky runs in the rolling 14-day window) → `candidate` → human approval → `quarantined` (14-day time-box) → (one 14-day extension) → `disabled` (auto-disable + issue + owner/manager notification). Auto-disable is the only automatic terminal action; **retirement** requires human approval and is distinct from disable. Re-enablement requires stability evidence (≥ 20 consecutive **shadow passes** — full out-of-band runs recorded with `shadow: true`, not counted toward promotion or flakiness, and reset by any non-pass), never the calendar alone. A scenario listed in `never-quarantine.json` is never a candidate.
- **Save preservation**: the player save is never opened; the source milestone save is copied read-only and re-hashed; the working copy is hashed before the run and after restore, and the post-restore digest must equal the source digest. Failure → `errored` with before/after state.
- **Version gate**: file-based classification before Xvfb launch → HARD-FAIL (exit 19) / WARN (proceed + record) / INFO (proceed + log).
- **Suite aggregation**: precedence `errored/partial (19) > fail (18) > policy (21) > no-tests (20) > pass (0)`. A mixed suite with at least one `pass` and some `skipped` exits `0`; a suite in which **every** selected scenario is `skipped` exits `20`; `flaky: true` never changes the aggregate exit code. Code `22` is reserved and never participates in aggregation.
- **Root-doc promotion**: `test.md`/`ROADMAP.md`/`COMPLETION_SUMMARY.md` updates use GATE3 marker blocks; a missing/duplicate marker is exit `14`. The runner writes `testMdStatus: partial` or `machine-pass` only; `verified` is human-only. `--check-docs` reports drift with exit `21` (POLICY).

## Validation Rules

- Scenario JSON: strict `additionalProperties: false` at every object level; required fields per the schema's `required` arrays (`summary`, `determinism`, `scenarioKind`, and `environment.apiVersion` are required); `testMdPath` must exist and contain the GATE3 markers; `milestoneSave` and macro paths must be relative and traversal-free; `milestoneSave` and `environment.saveId` must both match the selected milestone manifest; `min <= max` for `between`; `cmp` `right` type must match the `left` field type; `multiplier.base` must be `pre.<field>` and its suffix must equal `field`; `sentinel` must map to its declared field; `positive` scenarios can only expect `pass`; `boundary` scenarios can expect `pass`/`fail`; `negative` scenarios must declare a non-pass verdict.
- **Signal-to-field map** (runner-enforced): `soilHealthStateDelta` → `soilHealth`, `healthTier`, `soilHealthState`; `cropPhaseDelta` → `phaseDays`, `currentPhase`, `dayOfCurrentPhase`, `fullyGrown`, `dead`, `indexOfHarvest`, `healthTier`, `soilHealthState`; `smapiLog` → `elapsedMilliseconds`, `eventName`, `locationsProcessed`, `isMainPlayer`. The `sentinel` predicate is additional and uses the sentinel-to-field mapping. A predicate field outside its signal's row exits `14`.
- **Action-to-required-input map** (schema conditionals): `insertWaste` → `itemId`, `count`; `setSoilHealth` → `soilHealth`; `runMacro` → `macroFile`; all other actions require no extra inputs beyond `target`.
- **Unmeasured actual**: numeric predicates on an absent or `"NaN"` actual fail as `unmeasured-actual`; only `sentinel` may assert unmeasured/missing values.
- Soil health thresholds: poor positive < 34 → 1.15×; healthy ≥ 67 → 0.85×; moderate [34, 67) → 1.00×; `phaseDays` never below 1; 99999 sentinel never touched; NaN tiles skipped; missing key / health ≤ 0 → non-participant.
- Performance: per-location elapsed ms ≤ 5 (single shot after discarded warmup; young-generation collection-count delta recorded; >0 tagged `gc-attributed`).
- Save preservation: player save untouched; source milestone save re-hashed and unchanged; working copy byte-identical to the source digest after restore. The `0.0001f` tolerance belongs to `SaveComparator` float comparison only; NaN ignored; missing key → 0 at the persistence layer only.
- Manifest validation: `contracts/evidence-bundle.schema.json` (Draft 2020-12, strict) validates the finalized `manifest.json`; `manifest.sha256` is external and mandatory; `attempts[]` contains only `attempts/<n>/attempt.json` paths; every per-attempt manifest appears in `subject[]`; `files` (when emitted) uses the `subject` shape; `cleanup_failed: true` forces exit `19`; `ignoreExitCodeRefused: true` preserves the original exit code.
- **Contract alignment with `contracts/scenario.schema.json`** (no divergent copy is permitted):
  - `thresholds[].subject` is the selector object `{location?, tileX?, tileY?}` with `minProperties: 1`; it is forbidden for `smapiLog`; zero or multiple matches exit `14`.
  - `soilHealthState` is the closed enum `measured | missing | nan`; it distinguishes a missing key from a stored NaN and is required for `NAN_HEALTH`/`MISSING_KEY`.
  - `healthTier` is the closed five-value enum `unknown | nonParticipant | poor | moderate | healthy`, derived exactly as in §CropPhaseDump.
  - `expectedVerdict` is the closed enum `pass | fail | errored | partial | skipped`; `positive` scenarios allow only `pass`, `boundary` allow `pass`/`fail`, and `negative` require a non-pass value.
  - `expectedSignals` is exactly three unique values: `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`.
  - `condition` is v1 migration provenance only (`default: ""`), never the oracle; new v2 scenarios use `description`.
- Isolation: `XDG_CONFIG_HOME`, `HOME`, `XDG_DATA_HOME`, `XDG_STATE_HOME`, `XDG_CACHE_HOME` set to per-instance dirs before launch; startup probe asserts the resolved `Constants.DataPath` is inside `XDG_CONFIG_HOME`; mismatch → `errored` (exit 19).
