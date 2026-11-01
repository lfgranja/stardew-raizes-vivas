# Bug Verification: FsCheck 3.6.0 build failure is resolved

- **Slug**: fscheck-v3-build-failure
- **Tested**: 2026-11-01T01:51:44Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The original FsCheck compile failure no longer reproduces. On the fixed branch the test project builds with 0 errors and the full suite passes (1443 passed, 0 failed, 4 skipped); CI `Build and Test` is green, and `Quality Gate / build-analysis` now gets past `Build`/`Test` and fails only at the separate coverage gate. No regression attributable to the fix was found.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (pre-fix baseline) | `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj -c Release` in a clean `origin/dev` worktree at `e819a34` | fail (expected) | Reproduced `CS0103` (`Prop`/`Arb`), `CS0305` (`Gen<T>`), `CS1061` (`Gen<float>.Two`), `CS0619` (`Record.Exception(Func<Task>)`) |
| Reproduction (post-fix) | `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-restore` on `fix/fscheck-v3-build-failure` | pass | `0 Erro(s)`, 2 pre-existing `System.Collections.Immutable` net6.0 warnings |
| New / updated tests | `PYTHONPATH=scripts/quality-gate /tmp/opencode/qg-venv/bin/python -m pytest scripts/quality-gate/tests/test_dependency_pins.py -q` | pass | `2 passed` |
| Regression suite (full .NET) | `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-build --verbosity minimal` | pass | `1443 passed, 0 failed, 4 skipped` (total 1447) |
| Regression suite (quality gate) | `PYTHONPATH=scripts/quality-gate /tmp/opencode/qg-venv/bin/python -m pytest scripts/quality-gate/tests -q` | pass with pre-existing failures | `169 passed, 4 failed`; the same 4 fail on `origin/dev` with `167 passed, 4 failed` |
| Locked-mode restore | `dotnet restore LivingRoots.Tests/LivingRoots.Tests.csproj --locked-mode` | pass | Lock matches the pinned packages |
| CI — `Build and Test` | Run `41261132063`, job `124714448631` | pass | `Total tests: 1447`, `Passed: 1443` |
| CI — `Quality Gate / build-analysis` | Run `41261132017`, job `124714448885` | pass at `Build`; fail at coverage | No `error CS`; only `< 79.5%` coverage errors (separate front, PR #543) |
| CI — `Quality Gate / static` | Run `41261132017`, job `124714449068` | fail (unrelated) | jscpd `--fail-on-new-clones` error (separate front, PR #546) |
| CI — `Check Formatting` | Run `41261132021`, job `124714448628` | fail (unrelated) | Pre-existing SonarAnalyzer findings in `ModController.cs`, `ModEntry.cs`, etc.; no C# files changed by this fix |
| Lint / type-check | Not run locally | skipped | No C# changed; `dotnet format` was not run because it times out in this environment and the CI formatting failure is pre-existing |

## Output Excerpts

Pre-fix baseline on `origin/dev`:

```text
LivingRoots.Tests/VisualizationServiceTests.cs(138,16): error CS0103: The name 'Prop' does not exist in the current context
LivingRoots.Tests/VisualizationServiceTests.cs(142,26): error CS0619: 'Record.Exception(Func<Task>)' is obsolete
LivingRoots.Tests/Invariants/FsCheckFixture.cs(138,14): error CS1061: 'Gen<int>' does not contain a definition for 'Two'
```

Post-fix build:

```text
Compilação com êxito.
    2 Aviso(s)
    0 Erro(s)
```

Full test suite:

```text
Aprovado!  – Com falha:     0, Aprovado:  1443, Ignorado:     4, Total:  1447
```

CI `Build and Test`:

```text
Total tests: 1447
     Passed: 1443
```

CI `build-analysis` (after the fix — no compile errors, only coverage):

```text
##[error]LivingRoots.Services.TimeProvider 0.0% < 79.5%
##[error]LivingRoots.Services.MulchApplicationService 6.7% < 79.5%
##[error]Process completed with exit code 1.
```

Quality-gate Python suite:

```text
4 failed, 169 passed in 11.75s
```

The same four failures on `origin/dev`:

```text
4 failed, 167 passed in 7.37s
```

## Residual Risks

- **The FsCheck pin is temporary.** FsCheck 2.16.6 is test-only, but the suite still needs a migration to FsCheck 3 (`FsCheck.Fluent.Prop`, `ArbMap`, `PropertyAttribute.Arbitrary`); the dependabot ignore hides future major releases until then.
- **The policy tests are not executed by CI.** `Quality Gate / static` does not run `pytest scripts/quality-gate/tests`, so `test_dependency_pins.py` runs locally (or in any workflow that invokes pytest) but not on the PR.
- **The aggregate gate remains red for unrelated fronts.** Coverage exclusions (PR #543), jscpd (PR #546), and formatting (PR #544) are separate. This fix only removes the FsCheck `Build` failure.
- **A similar regression could merge again** if `Build and Test`/`build-analysis` is not a required status check; PR #528 was merged with the same red job.
- **The fix was exercised on a host with a Stardew Valley install.** The CI run also exercised it on the GitHub runner, so this is not a local-only result.

## Recommendation

Close the bug — the FsCheck 3.6.0 build failure is verified fixed both locally and in CI. Keep the coverage, jscpd, and formatting fronts tracked separately (PRs #543, #546, #544), and schedule the FsCheck 3 migration plus the required-check decision as follow-ups.
