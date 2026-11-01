# Bug Verification: Check Formatting CI job — SonarAnalyzer findings resolved locally

- **Slug**: check-formatting-ci-failure
- **Tested**: 2026-11-01T02:50:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The 10 SonarAnalyzer findings no longer reproduce: `dotnet format --verify-no-changes` exits 0 on a machine with the Stardew Valley assemblies, the mod project builds with 0 errors, and the full test suite passes. The CI job itself was not re-run, and its `CS0246` cascade from missing game assemblies remains unaddressed, so the job is expected to stay red on GitHub-hosted runners.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | exit 0 (only workspace-load warnings) |
| New / updated tests | none added | not-run | fix relied on existing format/build/test checks |
| Regression suite (mod build) | `dotnet build LivingRoots/LivingRoots.csproj -c Release` | pass | 0 errors |
| Regression suite (tests) | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-build` | pass | 1443 passed, 0 failed, 4 skipped |
| Lint / type-check | `dotnet format ... --verify-no-changes` | pass | exit 0 |
| Quality-gate suite | `pytest scripts/quality-gate/tests` | pass with pre-existing failures | 169 passed, 4 failed (same set as origin/dev) |
| CI job reproduction | GitHub Actions run `41263990758` | not-run | not re-triggered; environmental CS0246 cascade not exercised |

## Output Excerpts

```text
$ dotnet format Stardew-LivingRoots.sln --verify-no-changes
Foram encontrados avisos ao carregar o workspace...
exit=0

$ dotnet build LivingRoots/LivingRoots.csproj -c Release
... 0 Erro(s)

$ dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-build
Aprovado! – Com falha: 0, Aprovado: 1443, Ignorado: 4, Total: 1447
```

## Residual Risks

- The "Code Style (format only)" job on GitHub-hosted runners still fails with `CS0246` because the Stardew Valley assemblies are not provisioned; the code-level fix does not address that half.
- The S-rules are suppressed via justified pragmas rather than refactored; future findings could accumulate.
- `test_analyzer_config.py` suppression scan passed for the modified files (no new violations).

## Recommendation

Hold — the SonarAnalyzer findings are verified gone locally, but the CI job's environmental `CS0246` cascade was not exercised or fixed. Either provision game assemblies in `code-style.yml` (mirroring `build-analysis`) or re-scope the job to a game-assembly-free check, then re-run `/speckit.bug.test`. Do not promote this check to required until the environmental half is resolved.
