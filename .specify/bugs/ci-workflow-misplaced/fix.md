# Bug Fix: CI workflow moved into .github/workflows/ so GitHub Actions registers it

- **Slug**: ci-workflow-misplaced
- **Fixed**: 2026-10-01T08:15:00-04:00
- **Assessment**: ./assessment.md
- **Status**: partial

## Summary

The workflow was moved from `.github/build_and_analyze.yml` to `.github/workflows/build_and_analyze.yml`, which is the only path GitHub Actions scans — this removes the root cause of "0 runs in history". The workflow was rescoped to format + build + test (SonarCloud removed, per your choice to do build+format+test first) and split into two jobs so that a **format gate which is green today** runs on any runner, while the game-assembly-dependent build/test job stays dormant until you decide where the Stardew Valley assemblies come from. `coverlet.collector` was added so the coverage step can actually produce `coverage.opencover.xml` once tests run.

**Status is `partial`, not `applied`:** Blocker A is fully fixed and Blocker B is deferred by your instruction, but Blocker C is only *plumbed*, not *solved*. `build` and `test` cannot be proven to pass on CI because I have no game assemblies to test against. See "Deviations" and "Follow-ups".

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.github/build_and_analyze.yml` → `.github/workflows/build_and_analyze.yml` | moved | `git mv`; tracked as a rename (`RM` in `git status`), history preserved |
| `.github/workflows/build_and_analyze.yml` | rewritten | Two jobs (`format`, `build-test`); SonarCloud steps removed; `paths-ignore` added; SDK pinned to `10.0.x`; `GamePath` plumbing + preflight added |
| `LivingRoots.Tests/LivingRoots.Tests.csproj` | modified | Added `coverlet.collector` 6.0.0 so `--collect:"XPlat Code Coverage"` emits a report |
| `.specify/bugs/ci-workflow-misplaced/assessment.md` | untouched | Contract, per guardrails |
| `.specify/bugs/ci-workflow-misplaced/fix.md` | added | This report |

## What the workflow looks like now

```yaml
jobs:
    # No game-assembly dependency -> runs everywhere, green today.
    format:
        runs-on: ubuntu-latest
        # checkout, setup-dotnet 10.0.x, restore, dotnet format --verify-no-changes

    # Needs the game assemblies. Dormant until the variable is set.
    build-test:
        runs-on: ubuntu-latest
        if: ${{ vars.STARDEW_GAME_PATH != '' }}
        env:
            GamePath: ${{ vars.STARDEW_GAME_PATH }}
        # preflight (6 required files) -> restore -> build -> test -> upload artifact
```

Key decisions:

- **`if: ${{ vars.STARDEW_GAME_PATH != '' }}` on the job.** Rather than shipping a job that is guaranteed to fail, `build-test` reports as *skipped* (neutral) until you configure the variable. Set it and the job activates with **no further edits to the workflow**.
- **`env: GamePath:`** is the officially supported escape hatch: `find-game-folder.targets` only assigns `GamePath` when it does not already exist, and MSBuild promotes environment variables to properties. This suppresses local Steam/GOG autodetection deterministically.
- **Preflight step** converts ModBuildConfig's opaque `the mod build package can't find your game folder` into six specific `::error::missing <filename>` lines naming what to configure.
- **`paths-ignore` on both triggers** so documentation-only changes (`.specify/**`, `docs/**`, `research/**`, `**/*.md`, `specs/**`) skip the gate — the assessment's step 5.
- **`dotnet format Stardew-LivingRoots.sln`** — the solution path is now explicit rather than relying on single-project inference.

## Tests Added or Updated

No new test file. Per the assessment, the regression surface here is CI configuration, not domain logic, and the project's one-file-per-subject convention (`{Subject}Tests.cs`) does not apply. `dotnet test` cannot run in this workspace at all — see "Local Verification".

Instead, the fix is pinned by three machine-checkable properties, all verified below: the workflow lives under `.github/workflows/`, its YAML parses and exposes the expected trigger/job graph, and the preflight script's exit code discriminates a bad `GamePath` from a good one.

## Local Verification

Commands run on `dev`, .NET SDK 10.0.400:

| Check | Command | Result |
|---|---|---|
| Restore with new dependency | `dotnet restore` | **PASS** — `coverlet.collector/6.0.0` resolved into `project.assets.json` |
| Format gate (exact CI command) | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **PASS** — exit 0, no changes |
| Workflow YAML parses | `python3 -c "yaml.safe_load(...)"` | **PASS** — 2 triggers, 8 `paths-ignore` entries each, jobs `format` + `build-test`, **0** remaining `sonar` references |
| Preflight rejects bad `GamePath` | `GamePath=<empty dir> bash preflight.sh` | **PASS** — 6 × `::error::missing …`, exit 1 |
| Preflight accepts good `GamePath` | `GamePath=<complete fixture> bash preflight.sh` | **PASS** — `looks complete`, exit 0 |
| Rename preserved | `git status --porcelain` | **PASS** — `RM .github/build_and_analyze.yml -> .github/workflows/build_and_analyze.yml` |
| No stray old path | `ls .github/` | **PASS** — only `ISSUE_TEMPLATE/`, `pull_request_template.md`, `workflows/` |
| **Build** | `dotnet build --configuration Release` | **FAIL — pre-existing, unchanged by this fix** |
| **Test** | `dotnet test` | **NOT RUN — pre-existing, unchanged by this fix** |

Manual checks:

- Confirmed via `gh api …/actions/workflows` that the repository currently reports `total_count: 0`. After this commit is pushed, that count must become `1`. **Not yet verified** — I did not push.
- Confirmed by hand that none of the four Linux autodetect paths exist on this machine (`$HOME/GOG Games/…`, `~/.steam/steam/…`, `~/.local/share/Steam/…`, Flatpak path), which is why build/test remain unverifiable here. The `bin/` and `obj/` artifacts present in this workspace predate the session and came from a build elsewhere.

## Deviations from Assessment

1. **Assessment step 1 was "resolve Blocker C first, *then* move the file." I moved the file first and made `build-test` dormant instead.** You answered the assembly question with "Sei lá", so there was no decision to implement. Moving the file with `build-test` gated behind an unset variable gives the same end state (root cause fixed, no red runs) without inventing an assembly-provisioning scheme on your behalf. Consequence: a docs- or code-agnostic push now produces a **green** `Check Formatting` check immediately, rather than the red run the assessment was designed to avoid.

2. **Skipped assessment step 2 (`SONAR_TOKEN`) and steps 4/10's Sonar edits by removing Sonar entirely** rather than provisioning the secret. This follows your explicit "build+format+test primeiro". The old `/k:"lfgranja_stardew-raizes-vivas"` and `/o:"lfgranja"` values are gone with the removed steps, so the open question about whether that SonarCloud project exists is now moot until Sonar is reintroduced.

3. **Assessment said "correct the misleading comment at line 55".** Done by deletion — the comment no longer exists, and the correct requirement is now documented in the `build-test` job comment.

4. **Deviation on SDK pin: raised `dotnet-version` from `6.0.x` to `10.0.x`.** The assessment flagged SDK drift as a risk but did not decide it. I matched CI to the locally verified toolchain (10.0.400) on the reasoning that a `dotnet format` verdict must be identical in both places, and I verified green under 10. If you would rather keep 6.0.x, change both `setup-dotnet` steps — but expect to re-verify the format gate, since analyzers differ between SDK majors.

5. **Added `actions/upload-artifact@v4` for the coverage report**, which the assessment did not list. With `if-no-files-found: error` this is the enforcement mechanism for the assessment's requested "CI smoke assertion that `coverage.opencover.xml` is produced" — it fails the job if the report is missing, rather than letting Sonar silently import 0%. This is scope I added deliberately to satisfy an in-contract requirement; flagging it per instructions.

6. **`dotnet test` step reformatted to a YAML folded scalar (`>`) and the sln was made explicit in `dotnet format`.** Cosmetic; no behavioural change intended.

7. **Out of contract, not changed:** I did **not** touch `AGENTS.md:29`, which still describes `.github/` as hosting CI. It is now technically imprecise (the CI files live one level deeper). Left alone to avoid unrelated churn — listed as a follow-up.

## Risks & Considerations

- **`paths-ignore` + required status checks is a known GitHub trap.** If you later add `Check Formatting` to `required_status_checks`, a **docs-only PR will hang forever** waiting for a check that never reports, because the workflow is filtered out and GitHub still expects it. If you want the gate required, drop the `paths-ignore` on the `pull_request` trigger, or keep docs PRs unfiltered. Decide this *before* promoting the check. This is the same latent-blocking risk the assessment raised, now with a second edge to it.
- **`build-test` will silently stay dormant.** A skipped job looks like nothing happened, so a green workflow does **not** mean tests ran. Anyone auditing CI health must check that `Build and Test` actually reports, not just that the workflow is green. This is a real gap and the reason the status here is `partial`.
- **Commercial binaries.** Whoever provisions `STARDEW_GAME_PATH` must not push Stardew Valley assemblies into this **public** repository. A secret-backed zip or a private artifact both transit GitHub infrastructure. A self-hosted runner with the game installed avoids that entirely, and also sidesteps GitHub's public-repo rate limits for self-hosted runners — but it requires the runner to be registered and online before the job can run. I did not pick one for you.
- **SDK 10 against `net6.0`.** `net6.0` is out of support; SDK 10 will build it but emits EOL diagnostics. It does not affect `dotnet format`, and it is a pre-existing condition of the project, not introduced here.
- **Windows path separators.** ModBuildConfig validates with `$(GamePath)\Stardew Valley.dll`. MSBuild normalises this on Linux and my preflight uses forward slashes, but the actual `Build` step's behaviour under a provisioned `GamePath` is **untested on Linux**.

## Follow-ups

1. **Decide where the game assemblies come from** — self-hosted runner (recommended: no licensing transit, no binary in GitHub) vs. secret-backed provisioning (needs a format and raises the licensing question above). Then either register the runner and set `runs-on`, or point `STARDEW_GAME_PATH` at a provisioned path.
2. **Set `STARDEW_GAME_PATH`** (or the equivalent) and confirm `Build and Test` reports green — this is the step that converts this fix from `partial` to `applied`.
3. **Decide the `paths-ignore` + required-checks question before promoting the gate** (see Risks).
4. **Reintroduce SonarCloud** as a separate bug: provision `SONAR_TOKEN`, confirm the organisation and project key exist, and re-add the scanner steps. Kept out of scope here by your instruction.
5. **Verify after pushing**: `gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows` must return `total_count: 1`. That is the direct confirmation that the root cause is gone.
6. **Update `AGENTS.md:29`**, which still points at `.github/` rather than `.github/workflows/`.
7. **Consider renaming** `build_and_analyze.yml` to something like `build-and-test.yml` now that no analysis step remains. I kept the original name to preserve the rename-only history the assessment asked for.
8. **Reconsider the `main`/`dev` split** — `main` is 284 commits behind and contains no code, so the `push: [main, dev]` entry for `main` stays inert. Out of scope here.