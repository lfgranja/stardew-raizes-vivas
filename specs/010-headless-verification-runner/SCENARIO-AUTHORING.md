# Scenario Authoring Guide

JSON is the source of truth for a scenario. A Markdown companion is optional and
human-facing only; it never affects validation.

## JSON-first workflow

1. Copy an existing scenario JSON (e.g.
   `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json`).
2. Create a new directory under `specs/<feature>/scenarios/`.
3. Edit the JSON: set `featureSlug`, `testMdPath`, `scenarioId`, thresholds, and
   `expectedVerdict`. `testMdPath` must point to a document containing exactly
   one `<!-- GATE3:BEGIN --> ... <!-- GATE3:END -->` block.
4. Validate against both validators (Python `jsonschema` and the .NET helper
   `validate-scenario`).
5. Run `scripts/verify-scenario.sh --scenario <file|dir>` (launch-free checks
   first via `--check-docs`).
6. Optionally add a Markdown companion using
   `contracts/scenario-companion-template.md`.

## Optional Markdown companion

The companion is a human-facing checklist. It is never read by the runner and
never affects validation. Its filename is `<scenario-id>.md` next to the JSON.
The runner discovers only `*.scenario.json` (fallback `*.json`); companion files
are ignored by suite discovery.

## Subject selection

Thresholds declare a `signal` and a `field`. Allowed fields per signal are
enforced by the runner's cross-field checks (exit `14` on mismatch):

- `cropPhaseDelta`: `phaseDays`, `currentPhase`
- `soilHealthStateDelta`: `soilHealth`, `healthTier`
- `multiplier` predicates require `base` suffix equal to `field` and never
  multiply/clamp the sentinel `99999`.
- `sentinel` predicates map a named sentinel to its field (`phaseDays`).
- `between` requires `min <= max`.
- `cmp`/`allOf`/`anyOf`/`not`/`unchanged` compose over the same field set.

## expected-verdict rules

- `positive` scenarios: `expectedVerdict` must be `pass` (or unset).
- `negative` scenarios: any non-pass verdict (`fail`, `errored`, `partial`,
  `skipped`).
- `boundary` scenarios: `pass` or `fail`.

A scenario whose observed verdict differs from `expectedVerdict` exits `19`.

## zero-core-change rule

Adding a scenario for a new feature MUST NOT edit any runner source
(`scripts/verify-scenario.sh`, `scripts/lib/*`, or `LivingRoots*`). The PR diff
contains only scenario/docs/fixtures files. Suite discovery is provided by
`scripts/lib/suite.sh` (Phase 2); no orchestrator edit is required. Verify with
`scripts/tests/test-zero-core-change.sh`.