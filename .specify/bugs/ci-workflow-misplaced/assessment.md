# Bug Assessment: CI workflow in wrong directory — never registered by GitHub Actions

- **Slug**: ci-workflow-misplaced
- **Created**: 2026-10-01T08:05:25-04:00
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: high

## Report (verbatim)

> .github/build_and_analyze.yml continua no diretório errado, então a CI nunca rodou (0 runs no histórico). Movê-lo ligaria o gate de build, mas passaria a bloquear PRs

## Symptom

The continuous-integration workflow is committed at `.github/build_and_analyze.yml`, but GitHub Actions only discovers workflow definitions under `.github/workflows/`. GitHub therefore never registers the workflow: the Actions API reports `total_count: 0` and `gh run list` is empty, so no build, test, format, or SonarCloud analysis has ever run. Expected: pushes to `dev`/`main` and pull requests trigger a "Build, Test & Analyze" check.

## Reproduction

1. Clone the repository at `dev` and inspect `.github/` — observe `build_and_analyze.yml` directly under it, with no `workflows/` subdirectory present.
2. Query the Actions API: `gh api repos/lfgranja/stardew-raizes-vivas/actions/workflows` → `{"total_count":0,"workflows":[]}`.
3. Query run history: `gh run list --repo lfgranja/stardew-raizes-vivas` → empty output.
4. Confirm Actions itself is not the cause: `gh api repos/lfgranja/stardew-raizes-vivas/actions/permissions` → `{"enabled":true,...}`.

Steps 2–4 are reproducible as of 2026-10-01. Step 1 is reproducible on the `dev` branch only — see "Scope note".

## Scope note: the file exists on `dev` only

`main` is the initial commit (`a115f83 Initial commit`) containing only `.gitignore` and `LICENSE`; it is 284 commits behind `dev`. The workflow blob is absent from `main` and present on `dev` with identical content. Since `dev` is the repository default branch and the base for pull requests, the `pull_request` trigger is the meaningful one; the `push.branches: [main, dev]` entry for `main` is inert while `main` contains no code.

## Suspected Code Paths

- `.github/build_and_analyze.yml` — the workflow itself; committed at the wrong depth. This is the root cause of the missing run history.
- `.github/workflows/` — the path GitHub Actions actually scans. Does not exist on any branch.
- `.github/build_and_analyze.yml:42-52` — `Begin SonarCloud Analysis` step; **Blocker B**. Reads `secrets.SONAR_TOKEN`, which is not configured in the repository.
- `.github/build_and_analyze.yml:64-65` — `Build` step (`dotnet build --no-restore --configuration Release`); **Blocker C**. Fails on a bare runner because ModBuildConfig requires a game installation.
- `.github/build_and_analyze.yml:69-70` — `Test` step; uses `--collect:"XPlat Code Coverage"`, but `coverlet.collector` is not referenced by the test project, so no `coverage.opencover.xml` is produced.
- `.github/build_and_analyze.yml:55` — comment claims "O pacote Pathoschild.Stardew.ModBuildConfig lidará com as dependências do jogo automaticamente". Factually incorrect; this claim is the likely origin of the author's assumption that a bare runner would build.
- `LivingRoots/LivingRoots.csproj:14` — `<PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.4.0" />`; no `GamePath` property is set, correctly so for a portable repo, but it means the game folder must be supplied out of band.
- `LivingRoots.Tests/LivingRoots.Tests.csproj:26-33` — `SmapiInternalPath` + `CopySMAPIAssemblies` target; copies SMAPI.Toolkit assemblies out of the NuGet package. This satisfies the SMAPI-tooling reference need but **not** the `Stardew Valley.dll` reference.
- `AGENTS.md:29` — documents `.github/` as "CI: build → test → SonarCloud → coverage", confirming the intended behaviour diverges from the actual layout (intended-vs-implemented evidence).

## Root Cause Hypothesis

Confidence: **high** for the primary cause, **high** for the secondary blockers (each reproduced locally).

**Blocker A — wrong directory (primary, explains 0 runs).** GitHub Actions discovers workflows only in `.github/workflows/`. A workflow file committed elsewhere is treated as an ordinary repository file and is silently ignored — no error, no warning in the UI. The file was introduced in commit `3afceca` ("Addresses #22: Implement US-01-01 - Save and Load Soil Health Values (#72)") already at the wrong path, so it has never been live.

**Blocker B — missing `SONAR_TOKEN` (explains failure at step 5, before any gate).** `gh secret list --repo lfgranja/stardew-raizes-vivas` returns nothing: the repository has no secrets at all. `secrets.GITHUB_TOKEN` is supplied automatically by GitHub, but `SONAR_TOKEN` resolves to an empty string. `dotnet sonarscanner begin /d:sonar.token=""` fails authentication, so the run would abort at step 5 of 10 — before `Check Formatting`, `Build`, or `Test` are even reached.

**Blocker C — unresolvable game assemblies on the runner (explains failure at step 8).** `Pathoschild.Stardew.ModBuildConfig` 4.4.0 defines an unconditional `BeforeBuild` target with hard `Error` conditions: `!Exists('$(GamePath)')`, `!Exists('$(GamePath)\Stardew Valley.dll')`, and `!Exists('$(GamePath)\StardewModdingAPI.dll')`. There is no opt-out flag, and `find-game-folder.targets` only probes local Steam/GOG/registry locations. `ubuntu-latest` has none of these. Reproduced locally on Linux:

```
error : The mod build package can't find your game folder. You can specify where to find it;
        see https://smapi.io/package/custom-game-path.
```

This is not merely a build-step issue: 39 of 44 test files import `Stardew*` namespaces, so the test project cannot compile without the game's managed assemblies. The dependency is fundamental to the current test design, not a packaging oversight.

Net effect: moving the file alone converts "CI never ran" into "CI runs and goes red on every push and PR", which is worse than the current state for signal quality.

## Verified Gate Status (local reproduction on `dev`, .NET SDK 10.0.400)

| Workflow step | Command | Result |
|---|---|---|
| 2. Setup JDK 17 | `actions/setup-java@v4` | would succeed |
| 3. Setup .NET | `dotnet-version: 6.0.x` | would succeed (local SDK is 10.0.400 — see Risks) |
| 4. Install scanner | `dotnet tool install --global dotnet-sonarscanner` | would succeed |
| 5. Sonar begin | `sonarscanner begin` | **FAIL — `SONAR_TOKEN` empty** |
| 6. Restore | `dotnet restore` | PASS |
| 7. Check Formatting | `dotnet format --verify-no-changes` | **PASS (exit 0)** |
| 8. Build | `dotnet build --configuration Release` | **FAIL — game folder not found** |
| 9. Test | `dotnet test --collect:"XPlat Code Coverage"` | never reached; also no `coverlet.collector` |
| 10. Sonar end | `sonarscanner end` | never reached |

## Assessment of the "would block PRs" premise

The report's stated concern is **not currently realized**, and this materially changes the remediation.

- `GET /branches/dev/protection` → `required_status_checks.contexts: []`, `checks: []`. Same for `main`.
- Therefore no CI check is required to merge today. Moving the workflow produces a red check that is *visible* but *non-blocking*; merges remain possible.
- `gh pr list` → 0 open pull requests, so nothing is blocked at the moment of the change.
- The blocking risk is **latent**: it activates only when someone later adds "Build and Analyze" to `required_status_checks`. At that point the unresolved Blockers B and C would hard-stop every PR.
- Blast radius if unfiltered: the workflow has no `paths:` filter, so all ~64 historical pull requests were predominantly documentation/spec-only (`#168`–`#175` are docs). Enabling it as-is would run a full build and SonarCloud analysis on documentation-only changes, wasting minutes and polluting the quality gate with runs that cannot fail for a real reason.

## Proposed Remediation

**Preferred**: resolve Blockers B and C *before* moving the workflow, so the first ever run is green and the check is trustworthy from the start.

Order of operations:

1. **Make the build resolvable on a bare runner (Blocker C).** `GamePath` must point at a folder containing `Stardew Valley.dll`, `StardewValley.GameData.dll`, `MonoGame.Framework.dll`, `xTile.dll`, `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`. Stardew Valley is commercial software and cannot be redistributed or downloaded on a GitHub-hosted runner, so the credible options are all secret-backed: store the game's managed assemblies as a repository secret (or a release artifact of a private repo) and materialise them into `$(GamePath)` in a setup step, or move to a self-hosted runner on a machine that already has the game installed. This is the single largest piece of work in this assessment and deserves its own bug; the choice between secret-backed assemblies and a self-hosted runner is a project decision, not an implementation detail.
2. **Provision `SONAR_TOKEN` (Blocker B).** Add the secret via `gh secret set SONAR_TOKEN`. Verify the SonarCloud organisation and project key hard-coded at `.github/build_and_analyze.yml:48-49` (`lfgranja_stardew-raizes-vivas` / `lfgranja`) actually exist and that the token's account is a member of that organisation.
3. **Add `coverlet.collector`** to `LivingRoots.Tests/LivingRoots.Tests.csproj` so `--collect:"XPlat Code Coverage"` actually emits `coverage.opencover.xml` for the path already configured at `.github/build_and_analyze.yml:52`.
4. **Only then move the file** to `.github/workflows/build_and_analyze.yml` (use `git mv` so history is preserved), and correct the misleading comment at line 55.
5. **Add a `paths:` filter** (or a separate lightweight workflow) so documentation and `.specify/` changes do not trigger a full build plus SonarCloud analysis.
6. **Consider promoting `Check Formatting` to a separate early workflow** that has no game-assembly dependency. It already passes today, so it would deliver an immediately useful merge gate that is immune to Blockers B and C.

**Alternatives**:
- *Move the file now, accept red runs, iterate.* Simplest change, but the first impressions of the gate are permanent failures, and it wastes SonarCloud quota on failed analyses. Not recommended.
- *Gate only the test project.* `LivingRoots.Tests` still transitively references `LivingRoots`, which still references ModBuildConfig, so this does not avoid Blocker C without also removing the game-assembly dependency from the mod project. Not a viable shortcut.
- *Branch protection deferral.* Do nothing about `required_status_checks` for now; revisit only once a run has completed green. Explicitly document this so the latent blocking risk is not forgotten.

**Files likely to change**:
- `.github/build_and_analyze.yml` → moved to `.github/workflows/build_and_analyze.yml` (last)
- `LivingRoots.Tests/LivingRoots.Tests.csproj` — add `coverlet.collector`
- `.github/workflows/build_and_analyze.yml` — new step(s) to materialise `GamePath` from a secret; fix the line 55 comment; add `paths:` filter
- Possibly `.gitignore` — if a local `stardewvalley.targets` or a vendored assembly folder is introduced, ensure it is ignored

**Tests to add or update**:
- There is no unit test that can assert workflow placement; this is verified by the GitHub Actions API (`total_count >= 1`) and by a green first run.
- Add a CI smoke assertion that `coverage.opencover.xml` is produced after the test step, so the SonarCloud coverage import cannot silently regress to 0%.
- The existing 44-file suite must be re-run green locally against the chosen assembly-provisioning approach to confirm the `GamePath` wiring does not change compile semantics.
- Per the project convention (one test file per subject), no new `{Subject}Tests.cs` is warranted for this bug — the regression surface is CI configuration, not domain logic.

## Risks & Considerations

- **Secret-handling risk is the dominant concern.** Shipping game assemblies through repository secrets or artifacts means commercially licensed binaries transit through GitHub infrastructure. This needs a deliberate decision, not an implementation convenience. Note that commit `d565a20` ("chore(gitignore): protect secrets on a public repository") shows the project is already security-conscious and that this repository is **public**.
- **SDK drift.** The workflow pins `dotnet-version: 6.0.x` while the local toolchain is 10.0.400 with `<RollForward>Major</RollForward>`. Formatting and analysis behaviour can differ between SDK majors, so a green local `dotnet format` does not guarantee a green CI `dotnet format`. Consider aligning the pinned SDK with the developer toolchain.
- **Runner cost and quota.** A self-hosted runner is free but requires upkeep; secret-backed assemblies on `ubuntu-latest` consume Actions minutes and are subject to repository storage limits for binary artifacts.
- **Path separator portability.** ModBuildConfig's validation uses Windows-style paths such as `$(GamePath)\Stardew Valley.dll`. Under MSBuild on Linux these generally normalise, but any custom `GamePath` provisioning step must be validated on Linux specifically, not assumed.
- **Blanket trigger cost.** Without a `paths:` filter, documentation-only pull requests incur a full build and SonarCloud analysis.
- **Latent blocking risk.** Once the workflow exists, adding its check to `required_status_checks` becomes a one-click change with immediate merge-blocking effect. The gate must be green before that happens.
- **`main` divergence is out of scope but adjacent.** `main` is 284 commits behind `dev` and contains no code. This bug does not fix that, and merging `dev` into `main` would activate the `push` trigger for the first time.

## Open Questions

- [NEEDS CLARIFICATION: Where do the game managed assemblies come from on a CI runner — a repository secret, a private-repo release artifact, a self-hosted runner with the game installed, or a locally vendored (gitignored) folder?] This choice drives the entire remediation shape.
- [NEEDS CLARIFICATION: Do the SonarCloud organisation `lfgranja` and project key `lfgranja_stardew-raizes-vivas` referenced at `.github/build_and_analyze.yml:48-49` actually exist? Not verified during this assessment — the SonarCloud host was not fetched under the URL trust policy for this assessment.]
- [NEEDS CLARIFICATION: Should SonarCloud analysis be mandatory from day one, or should the first increment be build + format + test only, with Sonar added once the token and project key are confirmed?]
- [NEEDS CLARIFICATION: Should documentation and `.specify/` changes skip the full gate via a `paths:` filter, or is a complete run on every change acceptable?]
- [NEEDS CLARIFICATION: Should the CI-pinned .NET SDK be moved from `6.0.x` to match the developer toolchain (currently 10.0.400)?]
- [NEEDS CLARIFICATION: Is the `main`/`dev` divergence (284 commits) an intentional long-lived split, or should `main` be re-based? This determines whether the `push.branches` `main` entry should remain.]
- [NEEDS CLARIFICATION: Should Blocker C (game-assembly provisioning) be split into its own bug, given that it is the largest and most architecturally significant part of this work?]

## Unverified

No suspicious or instruction-like content was encountered: the bug report was pasted text with no URL, so no external content was fetched and no instructions were followed from any external source.