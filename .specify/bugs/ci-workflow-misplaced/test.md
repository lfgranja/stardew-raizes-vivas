# Bug Verification: CI workflow moved into .github/workflows/ so GitHub Actions registers it

- **Slug**: ci-workflow-misplaced
- **Tested**: 2026-10-01T08:26:00-04:00
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

**The bug is not closed.** The symptom still reproduces against GitHub: the Actions API reports `total_count: 0` and run history is still empty. The reason is not that the remediation is wrong — every local and static check passes — but that the fix exists **only in an uncommitted working tree**. `origin/dev` still carries `.github/build_and_analyze.yml` at the wrong path, so GitHub has never seen the corrected file. Separately, the regression suite could not be run at all (0 tests executed), so the `LivingRoots.Tests.csproj` change is unvalidated.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix, local path) | `test -f .github/workflows/build_and_analyze.yml` | pass | File now at the scanned path; old path gone |
| **Reproduction (post-fix, authoritative)** | `gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows` | **fail** | Still `{"total_count":0,"workflows":[]}` — the original symptom |
| **Reproduction (post-fix, run history)** | `gh run list --repo lfgranja/stardew-raizes-vivas` | **fail** | Still empty; 0 runs, same as before the fix |
| **Remote branch state** | `git ls-tree -r --name-only origin/dev -- .github/` | **fail** | `origin/dev` still has `.github/build_and_analyze.yml`; fix never pushed |
| Working-tree state | `git status --porcelain` | — | `RM .github/… -> .github/workflows/…` and ` M LivingRoots.Tests.csproj`, both unstaged-to-origin |
| Workflow structure (27 assertions) | `python3` + `yaml.safe_load` assertions | pass | Triggers, 8 `paths-ignore` per trigger, jobs `format`/`build-test`, job-level `if` on `vars.STARDIEW_GAME_PATH`, job-level `env`, every step has exactly one of `uses`/`run`, zero `sonar` references |
| Preflight script (new code) | `GamePath=<empty>` / `GamePath=<complete>` | pass | exit 1 with 6 `::error::missing` lines / exit 0 |
| Format gate (the gate the fix relies on) | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | exit 0, empty output |
| Restore after csproj change | `dotnet restore` | pass | `coverlet.collector/6.0.0` resolved into `project.assets.json` |
| Coverage collector name | `strings coverlet.collector.dll` | pass | Contains `XPlat code coverage`; matches `--collect:"XPlat Code Coverage"` |
| Build (regression) | `dotnet build --configuration Release --nologo` | fail (pre-existing) | Same single error: `The mod build package can't find your game folder.` No coverlet/NuGet/`CS####` diagnostics introduced |
| **Regression suite** | `dotnet test --nologo` | **not-run** | Blocked by the same pre-existing game-folder error; **0 tests executed** |
| Lint / type-check | `dotnet format --verify-no-changes` (project's lint gate) | pass | exit 0 |
| Workflow linter | `actionlint` | **not-run** | Not installed. Installing it needs network access and writes outside the workspace, which the guardrails forbid without consent |
| End-to-end on GitHub | push + observe run | **not-run** | Requires committing and pushing; not performed |

## Output Excerpts

Authoritative reproduction — the symptom is unchanged:

```
$ gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows
{"total_count":0,"workflows":[]}

$ gh run list --repo lfgranja/stardew-raizes-vivas
(empty)
```

Remote branch still carries the wrong path:

```
$ git ls-tree -r --name-only origin/dev -- .github/
.github/ISSUE_TEMPLATE/…  …  .github/build_and_analyze.yml  .github/pull_request_template.md
```

Regression suite could not execute:

```
$ dotnet test --nologo
exit=1
The mod build package can't find your game folder. …
$ grep -cE "Passed |Failed |Total tests" test.log
0
```

Build failure is unchanged and not caused by the fix:

```
$ dotnet build --configuration Release --nologo
exit=1
The mod build package can't find your game folder.   <- only error
grep -iE "coverlet|NU1[0-9]{3}|CS[0-9]{4}"  ->  (no matches)
```

Checks that did pass, notably the gate the fix depends on:

```
$ dotnet format Stardew-LivingRoots.sln --verify-no-changes
exit=0        # empty output

$ GamePath=<empty fixture>   bash preflight.sh ; echo $?
exit=1
$ GamePath=<complete fixture> bash preflight.sh ; echo $?
exit=0
```

## Residual Risks

- **The original symptom is fully intact on GitHub.** Until the commit lands on `dev`, the bug is exactly as reported: 0 runs, `total_count: 0`. Everything verified so far proves the *remediation is correct*, not that it is *live*.
- **The regression suite has never been run — not now, not in this workspace.** I modified `LivingRoots.Tests.csproj` by adding a `PackageReference`. Restore succeeds and the package resolves, which is meaningful but not sufficient evidence: nothing has confirmed that the 44-file suite still compiles and passes with the new collector referenced. On `net6.0` with `copyLocalLockFileAssemblies=true`, a new build asset is a plausible (if unlikely) source of assembly conflicts.
- **`build-test` remains dormant.** Even after pushing, the workflow will report green while `Build and Test` is *skipped*, because `STARDIEW_GAME_PATH` is unset. A green check must not be read as "tests ran".
- **The workflow has never been validated by GitHub's own parser.** YAML structure and Actions-context usage were asserted offline, but only a real push can confirm the file is accepted. `actionlint` would have narrowed this gap and was not run.
- **`paths-ignore` + future required status checks** remains an unaddressed trap: a docs-only PR would hang waiting for a filtered-out check. Not exercised here.
- **`Build` step behaviour under a provisioned `GamePath` is untested on Linux.** ModBuildConfig validates with Windows-style separators (`$(GamePath)\Stardew Valley.dll`); MSBuild normally normalises this, but my preflight used forward slashes and the real build step never ran.

## Recommendation

**Hold — do not close.** The remediation is sound: every local check passes, the workflow sits where GitHub scans it, and it is scoped exactly as agreed. But the end-to-end reproduction from `assessment.md` was **not** satisfied, because nothing was committed or pushed, so the authoritative symptom is unchanged. Re-running `/speckit.bug.assess` would add nothing — the assessment was correct, and the finding here is a deployment gap rather than a defect in the fix. Instead: commit and push to `dev`, then re-verify with a single command — `gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows` must return `total_count: 1`. Only after that, and after the regression suite has actually been executed once against a provisioned `GamePath`, should this close. Treat the two outstanding items as blocking: **push the fix**, and **run the 44-file test suite**.