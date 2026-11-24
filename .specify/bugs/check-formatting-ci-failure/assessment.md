# Bug Assessment: Check Formatting CI job fails on SonarAnalyzer findings and missing game assemblies

- **Slug**: check-formatting-ci-failure
- **Created**: 2026-11-01T02:20:00Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41263990758/job/124722842239
- **Verdict**: likely valid, needs reproduction
- **Severity**: high

## Report (verbatim or summarized)

CI run `41263990758`, job `124722842239` ("Check Formatting", workflow "Code Style (format only)") failed on 2026-11-01 after 58 s with 10 errors and 1 notice. The job runs `dotnet format Stardew-LivingRoots.sln --verify-no-changes` on `ubuntu-latest` with the .NET 10 SDK after `dotnet restore`, with no game assemblies provisioned.

Errors reported (host `github.com`, allowlisted):

- `LivingRoots/Controllers/ModController.cs(2643,21): error S2325: Make 'TryPlaceBin' a static method.`
- `LivingRoots/Controllers/ModController.cs(2394,35): error S1172: Remove this unused method parameter 'command'.`
- `LivingRoots/ModEntry.cs(227,23): error S2325: Make 'OnAssetRequested' a static method.`
- `LivingRoots/Services/CompostingBinService.cs(119,88): error S8969: Remove this null-forgiving operator; the compiler already knows this expression is not null here.`
- `LivingRoots/Services/LocationProvider.cs(12,26): error S2325: Make 'GetLocationByName' a static method.`
- `LivingRoots/Services/PlayerInventory.cs(14,21): error S2325: Make 'TryAddItem' a static method.`
- `LivingRoots/Services/PlayerProvider.cs(13,19): error S2325: Make 'CurrentPlayer' a static property.`
- `LivingRoots/Services/PlayerProvider.cs(16,18): error S2325: Make 'CurrentItem' a static property.`
- `LivingRoots/Services/ProgressCarryingBinItem.cs(35,29): error S3400: Remove this method and declare a constant for this value.`
- `LivingRoots/Domain/ISoilHealthService.cs(8,30): error CS0246: The type or namespace name 'Vector2' could not be found`
- (and many more `CS0246` for `StardewValley`, `IMonitor`, `IModHelper`, `IGameLoopEvents`, `Texture2D`, `Point`, `AssetRequestedEventArgs`, `Vector2`)

The `CS0246` cascade indicates the project cannot resolve Stardew Valley assemblies on the runner, so `dotnet format` cannot compile the solution. The `S*` diagnostics are SonarAnalyzer.CSharp (10.35.0) findings that the project treats as errors.

## URL Trust Policy

- Supplied URL: `https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41263990758/job/124722842239`
- Host: `github.com` (allowlisted public bug-report source)
- Branch taken: `allowlisted` — fetched without prompting
- No redirect followed; log content retrieved via `gh run view ... --log-failed` for the same run/job

## Symptom

The "Code Style (format only)" workflow, which is the only CI check that always runs, is red on `dev` and on every pull request. It reports 10 SonarAnalyzer findings plus a cascade of `CS0246` "missing type" errors because the Stardew Valley game assemblies are not available on the GitHub-hosted runner. As a result the check provides no useful signal and any future promotion to a required status check would be impossible.

## Reproduction

1. Open a pull request (or push to `dev`) — the "Code Style (format only)" workflow triggers.
2. The `Check Formatting` job runs `dotnet restore` then `dotnet format Stardew-LivingRoots.sln --verify-no-changes`.
3. The job fails with the 10 `S*` diagnostics and the `CS0246` cascade.

[NEEDS CLARIFICATION: whether `dotnet format` fails locally on a machine with the game installed — the S-rules may be the only real findings, or the CS0246 cascade may mask a different baseline issue.]

## Suspected Code Paths

- `.github/workflows/code-style.yml` — runs `dotnet format` on `ubuntu-latest` with no game assemblies and no `BUILDDEPS_TOKEN`/gamepath provisioning, unlike `build-analysis`.
- `LivingRoots/LivingRoots.csproj` — references `SonarAnalyzer.CSharp` 10.35.0 and `Pathoschild.Stardew.ModBuildConfig`, which requires a game install.
- `LivingRoots/Controllers/ModController.cs:2643` (`TryPlaceBin`) and `:2394` (`PrintVersion`) — S2325 / S1172 findings.
- `LivingRoots/ModEntry.cs:227` (`OnAssetRequested`) — S2325.
- `LivingRoots/Services/CompostingBinService.cs:119` — S8969 (null-forgiving operator).
- `LivingRoots/Services/LocationProvider.cs:12` (`GetLocationByName`) — S2325.
- `LivingRoots/Services/PlayerInventory.cs:14` (`TryAddItem`) — S2325.
- `LivingRoots/Services/PlayerProvider.cs:13` / `:16` (`CurrentPlayer`, `CurrentItem`) — S2325.
- `LivingRoots/Services/ProgressCarryingBinItem.cs:35` (`maximumStackSize`) — S3400.
- `LivingRoots/Domain/ISoilHealthService.cs:8` — CS0246 cascade (missing `Vector2`/Stardew assemblies).

## Root Cause Hypothesis

Two independent causes. (1) The code-style workflow does not provision the Stardew Valley assemblies that `ModBuildConfig` requires, so `dotnet format` cannot compile the solution and reports `CS0246` for every Stardew/XNA/SMAPI type — this is an environment gap, not a code defect, and mirrors why `Build and Test` is dormant. (2) Separately, `SonarAnalyzer.CSharp` raises 10 real style findings (S2325, S1172, S8969, S3400) that `dotnet format --verify-no-changes` treats as errors; these would fail the check even on a machine with the game installed. Confidence: medium (the CS0246 cascade was not reproduced locally in this assessment; the S-rules are visible in the log and reference concrete lines).

## Proposed Remediation

**Preferred**: Fix the 10 SonarAnalyzer findings in the cited files (make the listed methods/properties static where safe, drop the unused `command` parameter or mark it used, remove the redundant `!`, and convert `maximumStackSize()` to a constant), and provision the game assemblies in `code-style.yml` the same way `build-analysis` does (materialise `gamepath.zip` via `BUILDDEPS_TOKEN`, or run format against a stubbed build). This makes the always-on check green and meaningful.

**Alternatives**:
- Disable/relax the specific SonarAnalyzer rules in `.editorconfig` (faster, but loses the style guarantee and conflicts with the quality-gate analyzer-triage work in spec 011).
- Remove SonarAnalyzer.CSharp from the main project and rely on the quality-gate Semgrep step (changes the local dev experience).

**Files likely to change**:
- `.github/workflows/code-style.yml`
- `LivingRoots/Controllers/ModController.cs`
- `LivingRoots/ModEntry.cs`
- `LivingRoots/Services/CompostingBinService.cs`
- `LivingRoots/Services/LocationProvider.cs`
- `LivingRoots/Services/PlayerInventory.cs`
- `LivingRoots/Services/PlayerProvider.cs`
- `LivingRoots/Services/ProgressCarryingBinItem.cs`
- `.editorconfig` (if rule severities change)

**Tests to add or update**:
- Extend `scripts/quality-gate/tests/test_analyzer_config.py` to assert the code-style workflow provisions game assemblies (or that the S-rules are resolved).
- Add a regression check that `dotnet format --verify-no-changes` succeeds on a provisioned runner.

## Risks & Considerations

- Making methods static (S2325) can change call sites and virtual dispatch; verify each is genuinely stateless.
- `PrintVersion(string command, string[])` removing `command` touches console-command registration; check callers.
- Provisioning game assemblies in the format job increases runtime and requires `BUILDDEPS_TOKEN`, mirroring build-analysis's dependency.
- The quality-gate `test_analyzer_config.py` and `test_workflow_contract.py` tests already encode expectations about analyzer configuration; changes must stay consistent with spec 011.

## Open Questions

- [NEEDS CLARIFICATION: does `dotnet format` succeed locally with the game installed after only fixing the 10 S-rules?]
- [NEEDS CLARIFICATION: is the format job intended to be game-assembly-free, in which case the CS0246 cascade must be suppressed another way?]
