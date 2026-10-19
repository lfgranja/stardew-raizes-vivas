# Bug Verification: SonarCloud Reliability Rating Failed (C vs A)

- **Slug**: sonarcloud-reliability-rating
- **Tested**: 2026-10-18 (local) · 2026-10-19 (server-side confirmation)
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

Verified end-to-end. The fix was delivered through PR #284 (`fix/sonarcloud-reliability` → `dev`, merge commit `da99de8`, since `dev` is a protected branch requiring a PR). The **SonarCloud Code Analysis check passed on the PR**, and after the merge the `dev` analysis reports **0 `BUG` issues** and a **green Quality Gate** (`new_reliability_rating = 1.0` = A; the failing condition `> 1` no longer triggers). The four `BUG` issues that produced the `C Reliability Rating on New Code` are gone. All fix tests pass; the only failing tests are pre-existing and unrelated.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — server-side Quality Gate | SonarCloud `api/qualitygates/project_status?projectKey=lfgranja_stardew-raizes-vivas&branch=dev` | **pass** | `status: OK`; `new_reliability_rating` actualValue `1.0`, threshold `> 1`. |
| Reproduction (post-fix) — BUG issues on `dev` | SonarCloud `api/issues/search?…&branch=dev&resolved=false&types=BUG` | **pass** | `total: 0` (was `4`). |
| CI check on the PR | SonarCloud Code Analysis on PR #284 | **pass** | `conclusion: success`. |
| Reproduction (equivalent) — S1244 analyzer | temp copy + `SonarAnalyzer.CSharp 10.35.0.4138`, `dotnet build` (pre-fix vs post-fix) | **pass** | S1244 fired on the pre-fix copy; zero on the fixed copy. |
| S2583 source elimination | `grep -n granted LivingRoots/Services/CompostingBinService.cs` | **pass** | `granted` flag + always-true guard removed. |
| S4158 suppression | `grep -n NOSONAR LivingRoots/Controllers/ModController.cs` | **pass** | Justified `//NOSONAR` on the flagged line; no S4158 on `dev`. |
| New / updated tests | `dotnet test --filter "…GetOverlays_HealthWithinTolerance|…GetOverlays_OpacityWithinTolerance|…GetOverlays_NonFiniteSentinels|…T143_OverlayDrawableOnly"` | **pass** | 4/4. |
| Regression suite | `dotnet test Stardew-LivingRoots.sln --no-build -c Release` | **pass (with pre-existing failures)** | 1263 passed, 1 skipped, 3 failed — all pre-existing and unrelated. |
| Build | `GamePath=… dotnet build -c Release` | **pass** | 0 errors. |
| Format gate | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | exit 0; also green as `Check Formatting` on the PR. |

## Output Excerpts

SonarCloud Quality Gate for `dev` (after merge):

```json
{"projectStatus":{"status":"OK","conditions":[
  {"status":"OK","metricKey":"new_reliability_rating","comparator":"GT","errorThreshold":"1","actualValue":"1"},
  …]}}
```

SonarCloud measures for `dev`:

```json
{"metric":"bugs","value":"0"}, {"metric":"reliability_rating","value":"1.0"},
{"metric":"new_bugs","periods":[{"value":"0"}]}, {"metric":"new_reliability_rating","periods":[{"value":"1.0"}]},
{"metric":"alert_status","value":"OK"}
```

BUG issues on `dev`: `{"total":0,…,"issues":[]}`.

CI checks on PR #284: `SonarCloud Code Analysis: success`, `Check Formatting: success`, `Check Protected Paths: success`, `Build and Test: failure`.

`Build and Test` failure (pre-existing, unrelated — confirmed from the job log):

```
[FAIL] …SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters
       Assert.Equal() Failure: Strings differ  Expected: "ABC"  Actual: "ＡＢＣ"
[FAIL] …ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive
[FAIL] …ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode
       Constants.cs not found at …/LivingRoots/Constants.cs.
```

## Residual Risks

- **`Build and Test` is red on `dev`** for the three pre-existing, unrelated failures above. This predates the fix and is out of scope; it is not a regression. (Per the repo's `AGENTS.md`, this workflow is not a required status check.)
- **`//NOSONAR` is a suppression.** It is scoped to one line and justified inline. The vendor-recommended alternative is a "False Positive" marking in the SonarCloud UI. The `dev` analysis confirms it is honoured (0 S4158).
- **Pre-existing flaky property tests** (`T031b_OverlayDistinguishable_…` — a real `ColorInterpolationService` threshold gap at `(70, 71)` — and `T222_GetOverlays_HealthZero_…`) can make a full-suite run intermittently red. Neither touches the changed comparison path.
- **`dev` was updated via a PR**, not a direct push (branch protection). The fix is now on `dev` at merge commit `da99de8`.

## Recommendation

**Close the bug — verified end-to-end.** The original symptom (SonarCloud Quality Gate failing with `C Reliability Rating on New Code`) no longer reproduces: `dev` shows 0 `BUG` issues and `status: OK`. Follow up separately on the three pre-existing test failures and the `ColorInterpolationService` category-gap bug, which are unrelated to this fix.
