# Scenario Companion (optional)

Human-facing checklist for a scenario. Does not affect validation.

- [ ] Scenario JSON validates against `contracts/scenario.schema.json` under both validators (Python `jsonschema` and the .NET helper)
- [ ] `testMdPath` exists with exactly one `<!-- GATE3:BEGIN --> ... <!-- GATE3:END -->` block
- [ ] `featureSlug`, `scenarioId`, and `testMdPath` are consistent
- [ ] Thresholds reviewed against the signal/field allow-list
- [ ] `expectedVerdict` justified by `scenarioKind`
- [ ] Subject selection documented (which signals are measured)
- [ ] No runner source edited (zero-core-change rule)