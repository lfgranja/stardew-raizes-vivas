# Bug Verification: SonarCloud Reliability Rating Failed (C vs A)

- **Slug**: sonarcloud-reliability-rating
- **Tested**: 2026-10-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The four SonarCloud `BUG` issues that drove the `C Reliability Rating on New Code` are removed at the source: the two S1244 exact-float comparisons are replaced by a range check (confirmed by running the real Sonar C# analyzer — S1244 fires on the pre-fix source and is silent on the fixed source), the S2583 always-true guard is deleted, and the S4158 false positive is suppressed with a justified `//NOSONAR`. All new/affected tests pass and the regression suite shows only pre-existing failures. The original reproduction — the SonarCloud Quality Gate itself — **could not be exercised locally** (no push, no scanner/token), so the result is `partial`: the server-side gate is confirmed only by the next analysis on push.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — server-side Quality Gate | observe the SonarCloud analysis after a push to `dev` | **not-run** | Requires push + SonarCloud scanner/token; not available locally. This is the assessment's stated reproduction and it was **not** exercised. |
| Reproduction (equivalent) — S1244 analyzer | temp copy of source + `SonarAnalyzer.CSharp 10.35.0.4138`, `dotnet build` (pre-fix vs post-fix) | **pass** | S1244 fired on the pre-fix copy (`warning S1244` ×4 lines); zero S1244 on the fixed copy. |
| S2583 source elimination | `grep -n granted LivingRoots/Services/CompostingBinService.cs` | **pass** | The `granted` flag and the `if (granted)` guard are gone; only explanatory comments mention the word. The flagged construct no longer exists. |
| S4158 suppression | `grep -n NOSONAR LivingRoots/Controllers/ModController.cs` | **pass** | `//NOSONAR` present on the flagged `ContainsKey` line, with the false-positive reason inline. |
| New / updated tests | `dotnet test --filter "…GetOverlays_HealthWithinTolerance|…GetOverlays_OpacityWithinTolerance|…GetOverlays_NonFiniteSentinels|…T143_OverlayDrawableOnly"` | **pass** | 4/4 — the three added regression tests plus the `T143` test that caught the first (NaN-broken) attempt. |
| Regression suite | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` | **pass (with pre-existing failures)** | 1263 passed, 1 skipped, 3 failed — all 3 pre-existing and unrelated (see below). |
| Build | `GamePath=… dotnet build -c Release` | **pass** | 0 errors. |
| Format gate | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | exit 0. |
| SonarCloud API (current server state) | `api/issues/search?…types=BUG` | **informational** | Still `total: 4` OPEN — expected, because the analysis runs on push and the fix is not pushed yet. |

## Output Excerpts

Local Sonar analyzer reproduction (real analyzer, temp copy, repo untouched):

```
=== BUILD BASELINE (pre-fix) — Sonar rules S1244 / S2583 / S4158 ===
warning S1244: Do not check floating point equality with exact values, use a range instead. [base/LivingRoots.csproj]
=== BUILD FIXED — Sonar rules S1244 / S2583 / S4158 ===
(none)
```

> S2583 and S4158 are Sonar **symbolic-execution** rules that the standalone `SonarAnalyzer.CSharp` package keeps off by default; they did not fire on the baseline copy even with `dotnet_diagnostic.S2583/S4158.severity = warning` in `.editorconfig`. Those two are therefore verified by source elimination/suppression, not by a local analyzer run.

Fix tests:

```
Aprovado! – Com falha: 0, Aprovado: 4, Ignorado: 0, Total: 4
```

Full regression suite:

```
Com falha! – Com falha: 3, Aprovado: 1263, Ignorado: 1, Total: 1267
  Com falha …SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters
  Com falha …ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode
  Com falha …ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive
```

Format gate: `exit=0`.

## Residual Risks

- **The Quality Gate was not reproduced.** The rating (`C → A`) and the leak-period scoping are computed server-side by SonarCloud. Clearing all four `BUG` issues is necessary and sufficient for a `BUG`-only reliability rating to return to A, but the confirmation is the next analysis on push.
- **S2583 / S4158 not locally reproduced.** They could not be exercised with the standalone analyzer (SonarCloud-only rule profile). S2583 is certain because the flagged construct is deleted; S4158 relies on SonarCloud honoring `//NOSONAR`, which is standard but was not testable here.
- **Pre-existing, unrelated failures.** Three tests fail on the baseline commit as well: `UnicodeNormalizer_NormalizesFullwidthCharacters` and the two `ProductionSourceGuardTests` that hard-fail with `Constants.cs not found`.
- **Pre-existing flaky property tests.** `T031b_OverlayDistinguishable_DistinctHealth_DistinctColors` (a real `ColorInterpolationService` threshold gap at `(70, 71)`) and `T222_GetOverlays_HealthZero_RendersZeroOverlays` (FsCheck edge-case floats) can make a full-suite run intermittently red. Neither touches the changed comparison path.
- **`//NOSONAR` is a suppression.** It is scoped to one line and justified inline, but it is a suppression rather than a code change; the vendor-recommended alternative is a "False Positive" marking in the SonarCloud UI.

## Recommendation

Hold the bug open as `partial` until the next push to `dev`: the source-level fix is verified (S1244 reproduced and cleared with the real analyzer; S2583 eliminated; S4158 suppressed) and the regression suite is green apart from pre-existing failures, but the original symptom — the SonarCloud Quality Gate — can only be confirmed by the server-side analysis. **After pushing, re-check `https://sonarcloud.io/api/issues/search?componentKeys=lfgranja_stardew-raizes-vivas&branch=dev&resolved=false&types=BUG` (expect `total: 0`) and the Quality Gate on the run.** If it is still red, re-run `/speckit.bug.assess` with the fresh SonarCloud evidence.
