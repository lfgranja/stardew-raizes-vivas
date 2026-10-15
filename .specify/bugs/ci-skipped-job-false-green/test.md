# Bug Verification: Skipped build/test job makes a green CI run imply tests ran when they did not

- **Slug**: ci-skipped-job-false-green
- **Tested**: 2026-10-01T13:06:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

Verified end-to-end against live GitHub Actions. Pull request #176 produced the first two runs in this repository's history: `Code Style (format only)` completed **success** in 1m4s, and `Build and Test` reported **skipped**. The false-green signal is gone — build/test dormancy is now a visibly skipped, separately named workflow rather than a green check beside a green sibling. No regressions: the format gate passes and the build failure remains the single pre-existing game-folder error.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| **Reproduction (post-fix)** | Open PR #176, observe checks | pass | `Check Formatting` **pass**, `Build and Test` **skipping**. No green test check exists |
| **Run history (was 0 runs)** | `gh run list --repo lfgranja/stardew-raizes-vivas` | pass | **2 runs**, the first ever: `Code Style (format only)` success, `Build and Test` skipped |
| **Workflow registration (was `total_count: 0`)** | `gh api …/actions/workflows` | pass | **`total_count: 2`** |
| Format gate on a live runner | `Check Formatting` job, run 36866120410 | pass | success in 1m4s |
| Dormancy is visible, not green | `Build and Test` job, run 36866120365 | pass | Reported `skipped` in 0s, as its own workflow |
| Name/scope honesty | `gh pr checks 176` output | pass | Two distinctly named entries: `Check Formatting`, `Build and Test`. Neither implies the other ran |
| Preflight script (moved verbatim) | `GamePath=<empty>` / `<complete>` | pass | exit 1 (6 `::error::missing`) / exit 0 |
| Format gate (local) | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | exit 0, zero bytes |
| Structural assertions | `python3` + `yaml.safe_load`, 14 assertions | pass | Includes the bug's acceptance criteria |
| Build (regression) | `dotnet build --configuration Release --nologo` | fail (pre-existing) | Same single error, `The mod build package can't find your game folder.` No new diagnostics |
| Regression suite | `dotnet test` | **not-run** | Still blocked by the pre-existing game-folder error; 0 tests executed |
| `actionlint` | — | not-run | Not installed; would need network and writes outside the workspace |

## Output Excerpts

The original symptom — zero runs, zero registered workflows — is resolved:

```
$ gh run list --repo lfgranja/stardew-raizes-vivas
completed  success   Code Style (format only)  pull_request  36866120410  1m8s
completed  skipped   Build and Test            pull_request  36866120365  0s

$ gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows --jq .total_count
2
```

The fix behaves exactly as designed on a live runner:

```
$ gh pr checks 176
Check Formatting   pass       1m4s
Build and Test     skipping   0
```

Note what this looks like in the UI: `Build and Test` is a distinct, skipped check. Under the pre-fix single workflow it would have been a skipped job nested inside a **green** workflow — indistinguishable at a glance from a passing test run.

Local regression, unchanged by this diff:

```
$ dotnet build --configuration Release --nologo
exit=1
The mod build package can't find your game folder.
grep -iE "coverlet|NU1[0-9]{3}|CS[0-9]{4}|MSB[0-9]{4}"  ->  (no matches)
```

## Residual Risks

- **`dotnet test` still has never run — not here, not on the runner.** The suite needs the Stardew Valley assemblies, so `Build and Test` stayed skipped and 44 test files remain unexecuted. The `coverlet.collector` reference added by `ci-workflow-misplaced` is therefore still unvalidated against a real test run. This is a pre-existing blocker, not introduced here, but it is the one loose thread that touches runtime code.
- **`total_count: 2` reflects the branch, not `dev`.** The registry reads from the default branch; the count reached 2 because the PR branch carries the workflows. On merge the count stays 2. Until then, `dev` itself has no workflows registered.
- **Nothing is a required check.** `required_status_checks.contexts` is still `[]`, so neither `Check Formatting` nor `Build and Test` blocks a merge. That is intentional and correct — but it also means the gate is advisory only until someone promotes it.
- **`paths-ignore` on `Build and Test` remains a latent hang.** The workflow's header comment warns that the filter must be dropped before that workflow is ever made required. That warning lives only in the file and in `AGENTS.md`; nothing enforces it.
- **`actionlint` never ran** against either workflow. They are accepted by GitHub's parser — proven by the live runs — so the highest-risk version of this gap is now closed for these two files, but any future workflow edit is still unlinted locally.
- **One push deviated from the plan.** `git push origin dev` was rejected (`GH006: Protected branch update failed` — `dev` requires a pull request), so the commit was pushed to `ci/register-github-actions-workflows` and proposed via PR #176 instead. This is a better outcome, not a worse one: it means the new workflows were exercised by a real pull-request event, and the fix lands through the same review path as every other change in this repository.

## Recommendation

Close the bug — verified end-to-end against a live runner. The symptom no longer reproduces, both workflows are registered, the format gate passes on a real runner, and build/test dormancy is reported as `skipping` under its own name instead of hiding inside a green run.

Two follow-ups remain open and are **not** closed by this verification. First, the test suite has still never executed; run it once against a provisioned `GamePath`, which also finally settles the `coverlet.collector` change. Second, do not promote either check to `required_status_checks` until `Build and Test` has produced a genuinely green run. Note that PR #176 is still open — the root-cause fix for `ci-workflow-misplaced` merges with it.