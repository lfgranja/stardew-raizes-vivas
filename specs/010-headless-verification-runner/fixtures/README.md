# Scenario Fixtures

Contract-test inputs for `contracts/scenario.schema.json` (Draft 2020-12, `schemaVersion: 2`). They prove the schema accepts valid definitions, rejects one specific defect per invalid file, and (for `runner-invalid/`) that the runner's cross-field checks reject schema-valid but semantically invalid scenarios. They reference `specs/004-playable-compost-loop/test.md`, which exists with an initial GATE3 block, so the fixtures also pass the `testMdPath` existence/marker preflight; gameplay execution still requires the private milestone save and a configured host.

## `valid/`

| File | Purpose |
|------|---------|
| `compost-growth.scenario.json` | Poor-soil path, multiplier 1.15, host role |
| `compost-growth-healthy.scenario.json` | Healthy-soil path, multiplier 0.85, host role |
| `compost-growth-moderate.scenario.json` | Moderate interval, `[34, 67)` |
| `boundary-33-poor.scenario.json` | Boundary value 33: last Poor value, 1.15× |
| `boundary-34-moderate.scenario.json` | Boundary value 34: first Moderate value, 1.00× |
| `boundary-66-moderate.scenario.json` | Boundary value 66: last Moderate value, 1.00× |
| `boundary-67-healthy.scenario.json` | Boundary value 67: first Healthy value, 0.85× |
| `boundary-67-healthy-fail.scenario.json` | Boundary non-pass: matched `expectedVerdict: fail` is allowed |
| `phase-days-clamp-min.scenario.json` | `round(1 × 0.85)` clamped to the `phaseDays` minimum of 1 |
| `sentinel-untouched.scenario.json` | Sentinel + negation predicates |
| `farmhand-host-guard.scenario.json` | Farmhand role with conditional `hostGuard` |
| `farmhand-unchanged.scenario.json` | Farmhand expected absence via the `unchanged` predicate |
| `negative-crash-errored.scenario.json` | Negative scenario: crash → `errored` |
| `negative-incomplete-signals-partial.scenario.json` | Negative scenario: incomplete signals → `partial` |
| `negative-version-mismatch-errored.scenario.json` | Negative scenario: version mismatch → `errored` |
| `negative-expected-fail.scenario.json` | Negative scenario: matched `fail` |
| `negative-skipped.scenario.json` | Negative scenario: matched `skipped` |
| `sentinel-phase-minus-one.scenario.json` | `PHASE_MINUS_ONE` (`currentPhase == -1`) |
| `sentinel-poor-positive.scenario.json` | `POOR_POSITIVE_LT_34` (`healthTier: poor`) |
| `actions-coverage.scenario.json` | All remaining action primitives (`warp`, `setSoilHealth`, `applyCompost`, `harvest`, `runMacro`) |
| `equivalence-nan-health.scenario.json` | Stored NaN health → `unknown` (never Poor) |
| `equivalence-missing-key.scenario.json` | Missing key → `unknown` (never Poor) |
| `equivalence-zero-health.scenario.json` | Measured zero → `nonParticipant` for growth |
| `equivalence-35-moderate.scenario.json` | Health 35 → Moderate |
| `equivalence-68-healthy.scenario.json` | Health 68 → Healthy |
| `composite-allof.scenario.json` | `allOf` composite predicate |
| `composite-anyof.scenario.json` | `anyOf` composite predicate |
| `cmp-boolean-string.scenario.json` | Typed `cmp` on boolean and string fields |

All 28 MUST validate with zero errors. Every fixture includes the required `summary`, `environment.apiVersion`, `determinism`, and `scenarioKind` fields, and uses the v2 predicate shapes (`cmp` with `left`/`operator`/`right`; `between` with `exclusiveMin`/`exclusiveMax`; `multiplier` with `base`/`factor`; `unchanged` with `field`/`tolerance`).

## `invalid/`

| File | Expected single failure |
|------|-------------------------|
| `missing-test-md-path.scenario.json` | Missing required `testMdPath` |
| `unknown-property.scenario.json` | Unknown top-level property rejected by `additionalProperties: false` |
| `wrong-schema-version.scenario.json` | `schemaVersion` is not `2` |
| `farmhand-without-host-guard.scenario.json` | `playerRole: farmhand` without conditional `hostGuard` |
| `negative-scenario-expecting-pass.scenario.json` | `scenarioKind: negative` with `expectedVerdict: pass` |
| `cmp-wrong-operand-type.scenario.json` | `cmp` uses a numeric `right` for boolean field `isMainPlayer` |
| `health-tier-invalid-value.scenario.json` | `cmp` on `healthTier` uses a value outside the enum |
| `sentinel-wrong-field.scenario.json` | `SENTINEL_UNTOUCHED_99999` is mapped to `currentPhase` instead of `phaseDays` |
| `too-many-steps.scenario.json` | Six steps exceeds `maxItems: 5` |
| `null-summary.scenario.json` | `summary: null` violates `type: string` |
| `empty-summary.scenario.json` | `summary: ""` violates `minLength: 1` |
| `duplicate-signals.scenario.json` | Repeated `expectedSignals` entry violates `uniqueItems` |
| `macro-traversal.scenario.json` | Macro `file` contains `..` |
| `debug-console-prefix.scenario.json` | Console `command` starts with `debug ` |

Each invalid file MUST fail with exactly the named error (no incidental second error).

## `runner-invalid/`

These are **schema-valid** by construction but rejected by the runner's cross-field checks with exit `14` (scenario-contract error). They prove the runner does not rely on JSON Schema alone for semantic validity.

| File | Expected runner failure |
|------|-------------------------|
| `between-min-greater-than-max.scenario.json` | `between.min` > `between.max` |
| `multiplier-base-mismatch.scenario.json` | `multiplier.base` suffix does not equal `multiplier.field` |

## Cross-validator agreement (C-SCN-38)

Run the same fixtures through at least two independent validators before merging schema changes:

```bash
python3 - <<'PY'
import json, pathlib
from jsonschema import Draft202012Validator
schema = json.loads(pathlib.Path("contracts/scenario.schema.json").read_text())
v = Draft202012Validator(schema)
for p in sorted(pathlib.Path("fixtures/valid").glob("*.json")):
    errors = list(v.iter_errors(json.loads(p.read_text())))
    assert not errors, (p, errors)
for p in sorted(pathlib.Path("fixtures/invalid").glob("*.json")):
    errors = list(v.iter_errors(json.loads(p.read_text())))
    assert len(errors) == 1, (p, len(errors))
for p in sorted(pathlib.Path("fixtures/runner-invalid").glob("*.json")):
    errors = list(v.iter_errors(json.loads(p.read_text())))
    assert not errors, (p, errors)  # schema-valid; runner rejects on cross-field rules
print("schema OK: 28 valid accepted, 14 invalid rejected with one error each, 2 runner-invalid schema-accepted")
PY
```

`check-jsonschema`/`ajv-cli` in CI MUST produce the same accept/reject outcomes. The invalid files are intentionally minimal: each includes every newly required field except the one defect it is designed to prove, so a failure count greater than one signals an accidental schema regression.

**Cross-validator result (2026-10-27):** Python `jsonschema` 4.26.0 (`Draft202012Validator`) and `ajv-cli` 5 (`--spec=draft2020`) agree exactly: **28/28 valid accepted**, **14/14 invalid rejected**, and **2/2 runner-invalid schema-accepted**. Both validators were run over the same files; this is the C-SCN-38 evidence.
