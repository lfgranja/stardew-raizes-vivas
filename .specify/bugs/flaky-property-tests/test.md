# Bug Verification: flaky property tests (T031b + T222)

- **Slug**: flaky-property-tests
- **Tested**: 2026-10-19
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The flakiness is gone. The two property tests no longer reproduce the intermittent failures: **11 consecutive green full-suite runs** (6 in this verification + 5 recorded in `fix.md`) with `1271 passed / 0 failed`, and `0/15` targeted failures. The corrected properties and the new deterministic pins all pass, and the format gate is clean. No production code changed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — full suite, repeated | `dotnet test --no-build -c Release` ×6 | **pass** | `1271 passed, 1 skipped, 0 failed` every run (prior flakiness ~1 in 3). |
| Reproduction (post-fix) — the flaky tests, repeated | `--filter "…T031b_OverlayDistinguishable|…T222_GetOverlays_HealthZero|…T031b_PaletteSeam|…T031b_HealthyTier"` ×15 | **pass** | `0/15` failures. |
| New / updated tests | same targeted run | **pass** | Corrected T031b property; `T031b_PaletteSeam_SharesTheEndpointColour(30,31)/(70,71)`; `T031b_HealthyTier_IsFlat`; corrected T222. |
| Regression suite | full suite (changed modules: `VisualizationInvariantsTests`, `OverlayRendererTests`) | **pass** | `VisualizationInvariantsTests` + `OverlayRendererTests` = 27/27. |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | exit 0. |
| CI (`Build and Test`) | push + observe workflow | **not-run** | Expected green (same suite); not exercised here. |

## Output Excerpts

Full suite (6 consecutive runs):

```
Aprovado! – Com falha: 0, Aprovado: 1271, Ignorado: 1, Total: 1272
(x6)
```

Targeted reproduction:

```
targeted failures: 0/15
```

Format gate: `exit=0`.

## Residual Risks

- **The CI job was not re-run** in this verification. The workflow runs the same `dotnet test` suite, and the failure mode was environment-independent, so it is expected green — but that is a formality pending the next push.
- **The non-integer category gaps `(30,31)`/`(70,71)` remain**, intentionally (per the assessment's Clarifications) — unreachable for integer health and no longer exercised by the corrected property.
- The corrected T031b property is **weaker** than before (it no longer asserts cross-tier distinctness); that is deliberate — the original assertion was false against the shipped palette (`T253/FR-010`).

## Recommendation

**Close the bug — verified.** The original symptom (intermittent T031b/T222 failures reddening `Build and Test`) no longer reproduces: the corrected properties are deterministic and the full suite is green across 11 runs. Optionally push to confirm the CI check turns green, but no further action is required.
