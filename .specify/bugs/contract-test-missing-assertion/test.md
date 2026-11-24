# Bug Verification: SonarCloud S2699 — test method with no assertion (SoilHealthServiceContractTests)

- **Slug**: contract-test-missing-assertion
- **Tested**: 2026-11-01T05:24:54Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The code-level condition behind SonarCloud rule `csharpsquid:S2699` is fixed: `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` now contains an assertion (`Assert.Equal(60f, ...)`), and the contract test suite (13 tests) plus the full suite (1443 passed, 4 skipped, 0 failed) pass with no regressions. However, the original reproduction mechanism (SonarCloud reporting issue `AaFPP3NvEHNTGUcquNLc`) was not actually exercised because no SonarCloud scanner/credentials are available locally, so closure of the finding is inferred from the rule's documented requirement rather than observed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — offending condition | `sed -n '159,169p' .../SoilHealthServiceContractTests.cs` | pass | Test method now contains `Assert.Equal(60f, service.GetSoilHealth("Farm", tile), 5);` |
| Reproduction (post-fix) — SonarCloud analysis | (not available) | skipped | No `sonar-scanner`/credentials locally; cannot re-run SonarCloud to observe issue closure |
| New / updated tests | `dotnet test ... --filter "FullyQualifiedName~SoilHealthServiceContractTests"` | pass | 13 passed, 0 failed |
| Regression suite | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build` (prior run) | pass | 1443 passed, 4 skipped, 0 failed |
| Lint / type-check | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | Exit 0 |

## Output Excerpts

```
$ dotnet test ... --filter "FullyQualifiedName~SoilHealthServiceContractTests"
Aprovado! – Com falha: 0, Aprovado: 13, Ignorado: 0, Total: 13, Duração: 330 ms

$ dotnet format Stardew-LivingRoots.sln --verify-no-changes
EXIT: 0
```

## Residual Risks

- SonarCloud issue `AaFPP3NvEHNTGUcquNLc` has not been re-analyzed; closure is expected based on the added assertion but is not yet observed.
- The `[NEEDS CLARIFICATION]` item in the assessment (whether the no-subscriber path should also assert `OnTileHealthChanged` is never invoked) remains unaddressed; the current assertion only checks the resulting health value.

## Recommendation

Hold — the code-level fix is in place and all local checks pass, but the SonarCloud reproduction was not exercised in this environment. Re-run SonarCloud analysis on `dev`/PR to confirm issue `AaFPP3NvEHNTGUcquNLc` resolves to `CLOSED`; once observed, upgrade the result to `verified`. If SonarCloud still reports the finding, re-run `/speckit.bug.assess` with the new evidence.