# Scenario Authoring Guide

JSON is the source of truth for a scenario. A Markdown companion is optional and
human-facing only; it never affects validation.

## Workflow

1. Copy an existing scenario JSON (e.g. `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json`).
2. Create a new directory under `specs/<feature>/scenarios/`.
3. Edit the JSON: set `featureSlug`, `testMdPath`, `scenarioId`, thresholds, and
   `expectedVerdict`. `testMdPath` must point to a document containing exactly
   one `<!-- GATE3:BEGIN --> ... <!-- GATE3:END -->` block.
4. Run `scripts/verify-scenario.sh --scenario <file|dir>` (launch-free checks
   first via `--check-docs`).
5. Validate against both validators (Python `jsonschema` and the .NET helper).

## subject selection

Thresholds declare a `signal` and a `field`. Allowed fields per signal are
enforced by the runner's cross-field checks.

## expected-verdict rules

- `positive` scenarios: `expectedVerdict` must be `pass` (or unset).
- `negative` scenarios: any non-pass verdict (`fail`, `errored`, `partial`,
  `skipped`).
- `boundary` scenarios: `pass` or `fail`.

## zero-core-change rule

Adding a scenario for a new feature MUST NOT edit any runner source
(`scripts/verify-scenario.sh`, `scripts/lib/*`, or `LivingRoots*`). The PR diff
contains only scenario/docs/fixtures files. Suite discovery is provided by
`scripts/lib/suite.sh` (Phase 2).
