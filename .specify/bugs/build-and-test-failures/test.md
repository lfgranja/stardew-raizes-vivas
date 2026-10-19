# Bug Verification: "Build and Test" red — 3 failing tests fixed

- **Slug**: build-and-test-failures
- **Tested**: 2026-10-19
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The three reported failures no longer reproduce: the targeted suite is 47/47 and the full suite passes locally (1268 passed, 1 skipped, 0 failed). The fix held with no regressions. The result is `partial` because the `Build and Test` workflow itself was **not** re-run (no push), and a **pre-existing flaky** property test (`T031b`) can still intermittently redden the full suite — so the CI job is not yet *reliably* green.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — the 3 reported failures | `dotnet test --filter "FullyQualifiedName~UnicodeNormalizer|FullyQualifiedName~ProductionSourceGuardTests"` | **pass** | 47/47 (was 3 failing). |
| New / updated tests | same run (includes the 2 added normalizer tests) | **pass** | `UnicodeNormalizer_NormalizesFullwidthDigitsAndPunctuation`, `UnicodeNormalizer_PreservesNonLatinScript`. |
| Regression suite | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` | **pass (with a pre-existing flaky test)** | Rerun: 1268 passed, 1 skipped, **0 failed**. One earlier run hit the pre-existing flaky `T031b`. |
| Build | `dotnet build -c Release` | **pass** | 0 errors, 15 pre-existing warnings. |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | exit 0. |
| CI job (`Build and Test`) | push to `dev` + observe the workflow | **not-run** | `dev` is protected (PR required); not exercised by this command. |

## Output Excerpts

Targeted reproduction (the 3 previously-failing tests + 2 new):

```
Aprovado! – Com falha: 0, Aprovado: 47, Ignorado: 0, Total: 47
```

Full regression suite (rerun):

```
Aprovado! – Com falha: 0, Aprovado: 1268, Ignorado: 1, Total: 1269
```

Pre-existing flaky test seen in one full-suite run (unrelated to the fix):

```
[FAIL] LivingRoots.Tests.Invariants.VisualizationInvariantsTests.T031b_OverlayDistinguishable_DistinctHealth_DistinctColors
Falsifiable, after 3 tests … Original: (87.47000122f, 70.93000031f)
```

`T031b` passes in isolation (`Aprovado! 1/1`); its counterexample `(87.47, 70.93)` is the known pre-existing `ColorInterpolationService` category-gap bug: `GetCategoryForHealth` returns `Unknown` for the `(70, 71)` gap while `GetColorForHealth` returns the Healthy colour.

## Residual Risks

- **The CI job was not exercised.** The local run uses the same `dotnet test` command and the same game assemblies the workflow provisions, and the three reported failures were environment-independent — so CI is *expected* green, but this is not confirmed server-side.
- **A pre-existing flaky test can still redden the suite.** `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` fails intermittently in full-suite runs (observed roughly 1 in 3), so `Build and Test` is not yet *reliably* green even after this fix. It is not caused by this change — it is the `ColorInterpolationService` `(70, 71)` gap already logged in `.specify/bugs/sonarcloud-reliability-rating/fix.md`. `T222_GetOverlays_HealthZero_RendersZeroOverlays` is a second pre-existing flaky property test.
- **The `MaxTilledTiles` wiring is a production change** in the crop-growth path (output-preserving by construction, but not exercised by a live-game run).

## Recommendation

Hold as `partial` and push the branch to confirm the workflow turns green (the local reproduction is already green). Separately, file a bug for the `ColorInterpolationService` `(70, 71)` category gap — until that flakiness is removed, `Build and Test` will keep going red intermittently regardless of this fix. If the CI run on the push is green, this bug can be closed.
