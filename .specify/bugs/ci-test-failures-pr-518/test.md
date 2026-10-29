# Bug Verification: PR #518 CI test failures resolved (83 → 0)

- **Slug**: `ci-test-failures-pr-518`
- **Tested**: 2026-10-29T04:12:26Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The pre-fix commit `758325f` reproduces the exact CI symptom locally: **83 failed / 1338 passed / 4 skipped / 1425 total**, exit code 1, matching run `40793022307` case-for-case. The fixed tree (`17ea4a5`, tested at branch head `47ad2c3`) passes the same solution-level build and test commands with **1436 passed / 4 skipped / 0 failed / 1440 total**, and the 27 classes that contained the 83 failures run **495 tests with 0 failures**. No regressions or formatting violations were found. A fresh GitHub Actions run was not performed in this session.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Pre-fix reproduction | `git archive 758325f` into a throwaway copy; `dotnet build Stardew-LivingRoots.sln --configuration Release`; `dotnet test Stardew-LivingRoots.sln --no-build --configuration Release` | fail (expected) | 83 failed / 1338 passed / 4 skipped / 1425 total; exit 1; exact match to the CI job summary |
| Post-fix reproduction | Same build/test commands at fixed HEAD (`47ad2c3`, code `17ea4a5`) | pass | 1436 passed / 4 skipped / 0 failed / 1440 total; exit 0 |
| Previously failing classes | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build --configuration Release --filter "<27 class filter>"` | pass | 495 passed / 0 failed; covers every class that produced the 83 failures |
| New / updated tests | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName~HealthCategory\|FullyQualifiedName~ColorInterpolationService"` | pass | 77 passed / 0 failed; includes the 13-case cross-resolver parity theory |
| Regression suite | `dotnet test Stardew-LivingRoots.sln --no-build --configuration Release` | pass | Full suite; 1436 passed / 4 skipped / 0 failed / 1440 total |
| Build | `dotnet build Stardew-LivingRoots.sln --configuration Release` | pass | 0 errors, 17 warnings (pre-existing nullable warnings in `SoilHealthPersistenceInvariantsTests.cs`) |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | exit 0 |
| Fresh CI run | GitHub Actions `Build and Test` on PR #518 (run `40872910600`) | pass | Triggered by the push of `d03a21b`; 1436 passed / 4 skipped / 0 failed / 1440 total; exit 0 |
| Gate 3 gameplay | Manual SMAPI session per `VERIFICATION_PROTOCOL.md` §3 | not-run | Out of scope for this CI-test bug; gameplay-visible bin/soil behavior still needs manual verification |

## Output Excerpts

Pre-fix reproduction at `758325f`:

```text
Com falha! – Com falha:    83, Aprovado:  1338, Ignorado:     4, Total:  1425, Duração: 1 m 5 s - LivingRoots.Tests.dll (net6.0)
```

Post-fix full suite at `47ad2c3`:

```text
Aprovado! – Com falha:     0, Aprovado:  1436, Ignorado:     4, Total:  1440, Duração: 48 s - LivingRoots.Tests.dll (net6.0)
```

Previously failing classes (27 classes, 495 tests):

```text
Aprovado! – Com falha:     0, Aprovado:   495, Ignorado:     0, Total:   495, Duração: 7 s - LivingRoots.Tests.dll (net6.0)
```

Targeted health-category / interpolation tests:

```text
Aprovado! – Com falha:     0, Aprovado:    77, Ignorado:     0, Total:    77, Duração: 849 ms - LivingRoots.Tests.dll (net6.0)
```

Post-fix build:

```text
    17 Aviso(s)
    0 Erro(s)
```

## Residual Risks

- **CI run now green.** Run `40872910600` on PR #518 completed with `success`: 1436 passed / 4 skipped / 0 failed / 1440 total, matching the local result exactly. No further CI evidence is needed.
- **No Gate 3 gameplay session.** Bin placement/pickup, zero-health rendering, and fail-closed locks are gameplay-visible changes that unit tests do not exercise in-game.
- **Behavior change in `HealthCategoryExtensions.FromHealthValue`.** Finite out-of-range values now clamp to `Poor`/`Healthy` instead of `Unknown`. No production caller was found, but any future consumer expecting `Unknown` must be updated.
- **Test count changed from 1425 to 1440.** The delta is the reconciliation cases from `16f43c0` plus the 13-case cross-resolver theory; the previously failing classes themselves remain green.
- **Doc-only commits after the tested code.** `47ad2c3` contains the fix report; the behavioral change is in `17ea4a5`.

## Recommendation

Close the bug for the CI test-failure symptom: the exact pre-fix failure set reproduces locally and is gone under the same build/test commands, including every previously failing class. CI runner parity is now confirmed (run `40872910600`, success, 1436/4/0/1440). The only remaining item is a Gate 3 manual session if gameplay claims are needed for the bin/soil changes.
