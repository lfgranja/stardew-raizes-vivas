# Scenario Fixtures

Contract-test inputs for `contracts/scenario.schema.json` (Draft 2020-12, `schemaVersion: 2`). They exist to prove the schema accepts valid definitions and rejects one specific defect per invalid file. They reference `specs/004-playable-compost-loop/test.md`, which now exists with an initial GATE3 block, so the fixtures also pass the `testMdPath` existence/marker preflight; gameplay execution still requires the private milestone save and a configured host.

## `valid/`

| File | Purpose |
|------|---------|
| `compost-growth.scenario.json` | Poor-soil path, multiplier 1.15, host role |
| `compost-growth-healthy.scenario.json` | Healthy-soil path, multiplier 0.85, host role |
| `compost-growth-moderate.scenario.json` | Boundary scenario, `[34, 67)` interval |
| `boundary-33-poor.scenario.json` | Boundary value 33: last Poor value, 1.15× |
| `boundary-34-moderate.scenario.json` | Boundary value 34: first Moderate value, 1.00× |
| `boundary-66-moderate.scenario.json` | Boundary value 66: last Moderate value, 1.00× |
| `boundary-67-healthy.scenario.json` | Boundary value 67: first Healthy value, 0.85× |
| `phase-days-clamp-min.scenario.json` | `round(1 × 0.85)` clamped to the `phaseDays` minimum of 1 |
| `sentinel-untouched.scenario.json` | Sentinel + negation predicates |
| `farmhand-host-guard.scenario.json` | Farmhand role with conditional `hostGuard` |

All ten MUST validate with zero errors. Every fixture includes the required `summary`, `environment.apiVersion`, `determinism`, and `scenarioKind` fields, and uses the v2 predicate shapes (`cmp` with `left`/`operator`/`right`; `between` with `exclusiveMin`/`exclusiveMax`; `multiplier` with `base`/`factor`).

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

Each invalid file MUST fail with exactly the named error (no incidental second error).

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
print("schema OK: 10 valid accepted, 9 invalid rejected with one error each")
PY
```

`check-jsonschema`/`ajv-cli` in CI MUST produce the same accept/reject outcomes. The invalid files are intentionally minimal: each includes every newly required field except the one defect it is designed to prove, so a failure count greater than one signals an accidental schema regression.
