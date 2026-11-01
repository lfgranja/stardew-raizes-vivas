# Bug Assessment: FsCheck 3.6.0 breaks the test project build

- **Slug**: fscheck-v3-build-failure
- **Created**: 2026-11-01T01:07:02Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41258811867/job/124707776432 (pasted job log) plus the pasted `Aggregate` step output
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

Pasted report:

```text
https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41258811867/job/124707776432

Annotations
2 errors and 1 notice
Aggregate
Process completed with exit code 1.
Aggregate
build-analysis concluded failure
...
Run static_result="success"
  static_result="success"
  sca_result="success"
  build_result="failure"
  result="success"
  ...
    if [ "${value}" != "success" ]; then
      result="failure"
      echo "::error::${job} concluded ${value}"
    fi
  ...
Error: build-analysis concluded failure
Error: Process completed with exit code 1.
```

The cited job is `Quality Gate / gate` on PR #546 (run `41258811867`, job `124707776432`, branch `fix/jscpd-fail-on-new-clones`). The gate itself only aggregates; the real failure is the sibling `Quality Gate / build-analysis` job (`124707610023`), which fails at `Build`.

**URL trust policy**

- Verbatim URL: `https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41258811867/job/124707776432`
- Host: `github.com`
- Policy branch: **allowlisted** (GitHub is a listed public bug-report source), fetched without prompting.
- Fetch method: authenticated `gh run view` / `gh api` against the repository; no redirects were followed and no other URL was fetched.
- Untrusted content: the pasted log and fetched job output were treated as data. No instruction-like content was present, and nothing from the page was executed.

## Symptom

`Quality Gate / build-analysis` on `dev` fails at `Build`, so `Quality Gate / gate` exits 1 and blocks every pull request. The compiler rejects the test suite because `LivingRoots.Tests` now references **FsCheck 3.6.0** while the tests are written against the **FsCheck 2.x** API.

Representative errors from job `124707610023`:

```text
LivingRoots.Tests/DailyDecayTests.cs(259,16): error CS0103: The name 'Prop' does not exist in the current context
LivingRoots.Tests/DailyDecayTests.cs(260,13): error CS0103: The name 'Arb' does not exist in the current context
LivingRoots.Tests/DailyDecayTests.cs(293,16): error CS0305: Using the generic type 'Gen<T>' requires 1 type arguments
LivingRoots.Tests/Invariants/FsCheckFixture.cs(138,14): error CS1061: 'Gen<int>' does not contain a definition for 'Two'
LivingRoots.Tests/Invariants/SoilHealthBoundsInvariantsTests.cs(92,16): error CS0234: The type or namespace name 'Prop' does not exist in the namespace 'FsCheck'
LivingRoots.Tests/Invariants/SoilHealthBoundsInvariantsTests.cs(333,17): error CS0246: The type or namespace name 'WeightAndValue<>' could not be found
LivingRoots.Tests/VisualizationServiceTests.cs(142,26): error CS0619: 'Record.Exception(Func<Task>)' is obsolete
```

## Reproduction

1. Check out `origin/dev` at `e819a34` (merge of dependabot PR #528) or any branch based on it.
2. Restore/build `LivingRoots.Tests/LivingRoots.Tests.csproj` with the game assemblies available (the `Quality Gate / build-analysis` job does this via `gamepath.zip`).
3. The build fails with the `CS0103` / `CS0305` / `CS1061` / `CS0246` / `CS0234` errors above. `dotnet test` never starts.
4. Open or update a pull request; `Quality Gate / gate` reports `build-analysis concluded failure` and exits 1.

Confirmed runs:

- PR #546, run `41258811867`, `build-analysis` job `124707610023` — failure at `Build`.
- PR #528 (the bump that introduced the problem), run `41198867115`, `build-analysis` job `124530490944` — same FsCheck compile errors, already red before merge.

## Suspected Code Paths

- `LivingRoots.Tests/LivingRoots.Tests.csproj:51-52` — `FsCheck` and `FsCheck.Xunit` are pinned to `3.6.0`; the source code calls removed 2.x APIs.
- `LivingRoots.Tests/packages.lock.json` — lock file resolves `FsCheck`/`FsCheck.Xunit` to `3.6.0` and records the v3 dependency graph.
- `LivingRoots.Tests/Invariants/FsCheckFixture.cs` — central `Arb.From`, `Gen.*`, `Gen<T>.Two()`, and `Arb.Register` usage; the greatest concentration of breakage.
- `LivingRoots.Tests/DailyDecayTests.cs` — `Prop.ForAll`, `Arb.From`, `Gen<string>` usage.
- `LivingRoots.Tests/Invariants/SoilHealthBoundsInvariantsTests.cs` — `FsCheck.Prop.ForAll` and `WeightAndValue<>` usage.
- `LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs` — `Gen<float>.Two()` usage.
- `LivingRoots.Tests/VisualizationServiceTests.cs` — `Prop.ForAll`/`Arb.From` plus a `Record.Exception(Func<Task>)` obsolete diagnostic at line 142.
- `.github/dependabot.yml` — both `/LivingRoots` and `/LivingRoots.Tests` are on weekly, unrestricted NuGet updates, so major bumps are proposed automatically.
- `.github/workflows/quality-gate.yml` — `build-analysis` runs `dotnet build`/`dotnet test`; its failure is aggregated by the `gate` job.

Scale of the affected test code:

- 29 test files import `FsCheck`.
- 104 `[Property]` attributes.
- 18 `Prop.ForAll` calls.
- 31 `Arb.From` / `Arb.Register` calls.
- 33 static `Gen.*` calls.
- 5 `WeightAndValue<>` references.

## Root Cause Hypothesis

**Confidence: high.** Dependabot PR #528 (commit `8d3c8fd`, merged as `e819a34`) bumped `FsCheck` and `FsCheck.Xunit` from `2.16.6` to `3.6.0`, a semver-major release. FsCheck 3 removed or relocated the 2.x C# surface the suite relies on:

- the `Arb` static class (including `Arb.From` and global `Arb.Register`) is gone; v3 resolves arbitrary instances through `IArbMap` / `ArbMap` and `PropertyAttribute.Arbitrary`;
- the `Prop` static class moved to `FsCheck.Fluent.Prop`, and `Gen` helpers moved to `FsCheck.FSharp.Gen`;
- `Gen<T>` no longer exposes the `Two()`/`Three()`/… combinators the fixture uses;
- `WeightAndValue<>` is no longer public.

The bump was merged even though `build-analysis` was already failing on PR #528, so `dev` has been unbuildable since `e819a34`. Every subsequent PR inherits the failure because all branches are cut from `dev`.

The `CS0619` at `VisualizationServiceTests.cs:142` is suspicious because `SetTileHealthData` returns `void` and should bind to `Record.Exception(Action)`. It is most likely a cascade from the unresolved `Prop`/`Arb` symbols, but it must be re-checked after the downgrade; if it survives, it is a second, independent issue.

## Proposed Remediation

**Preferred (short term, already prepared in PR #537)**: restore the FsCheck 2.x line and stop automatic major bumps until the suite is migrated.

1. Set `FsCheck` and `FsCheck.Xunit` back to `2.16.6` in `LivingRoots.Tests/LivingRoots.Tests.csproj`.
2. Regenerate `LivingRoots.Tests/packages.lock.json` with the downgraded versions (`dotnet restore LivingRoots.Tests/LivingRoots.Tests.csproj --force-evaluate`). PR #537 already contains a consistent lock (`8346725` + `eb75616`); those commits can be cherry-picked or the PR merged.
3. Add `ignore` rules for semver-major `FsCheck`/`FsCheck.Xunit` updates under the `/LivingRoots.Tests` dependabot entry so the same breakage is not proposed again automatically.
4. Add a repository-policy test that parses the csproj and `.github/dependabot.yml` and fails if the FsCheck major version is not 2 or the ignore rule disappears.
5. Re-run `Quality Gate / build-analysis` (or `Build and Test`, which passed on PR #537) to confirm the build and tests are green, and confirm whether the `Record.Exception` diagnostic disappears.

**Alternative (long term)**: migrate the suite to FsCheck 3.

- Replace `Prop.ForAll` with `FsCheck.Fluent.Prop` equivalents.
- Replace `Arb.From`/`Arb.Register` with `ArbMap` and `PropertyAttribute.Arbitrary`/config-based resolution.
- Move generator helpers to `FsCheck.FSharp.Gen` and replace `Gen<T>.Two()` with explicit tuple composition.
- This touches 29 files and 104 properties and changes how fixtures register arbitraries, so it should be a separate, reviewed change with its own TDD cycle — not part of this bug fix.

**Files likely to change**:

- `LivingRoots.Tests/LivingRoots.Tests.csproj` — pin `2.16.6`.
- `LivingRoots.Tests/packages.lock.json` — regenerated lock.
- `.github/dependabot.yml` — ignore semver-major `FsCheck`/`FsCheck.Xunit`.
- `scripts/quality-gate/tests/test_dependency_pins.py` (new) — policy test for the pin and ignore rule.
- `.specify/bugs/fscheck-v3-build-failure/` — assessment, fix, and verification reports.

**Tests to add or update**:

- `test_fscheck_major_version_is_pinned_to_2` — parse `LivingRoots.Tests.csproj` and assert both package references resolve to a `2.x` version.
- `test_dependabot_ignores_fscheck_major_updates` — parse `.github/dependabot.yml` and assert semver-major ignores exist for both packages.
- The real regression check is the existing `build-analysis` build; it must be re-run on the fix branch because a policy test cannot detect a compile break by itself.

## Risks & Considerations

- **Pinning is temporary debt.** FsCheck 2.16.6 is a test-only dependency, but the suite will need a planned migration to v3 to stay current. Track it as a follow-up issue rather than letting the pin rot silently.
- **Suppressed updates can hide fixes.** Ignoring semver-major bumps for these packages also hides major-version security or compatibility fixes; revisit the ignore rule when the migration lands.
- **Lock file regeneration must be real.** Hand-editing `packages.lock.json` risks restore failures; use `dotnet restore --force-evaluate` and commit the generated file.
- **Local reproduction is limited.** The build requires proprietary Stardew Valley + SMAPI assemblies. Without a local `GamePath`, verification depends on CI (`Build and Test` or `build-analysis`).
- **The `Record.Exception` diagnostic may be independent.** If it survives the FsCheck downgrade, it needs its own fix (`Record.ExceptionAsync` or an `Action`-compatible call) and should be recorded as a deviation.
- **Process gap.** PR #528 was merged with a red `build-analysis` check. A version pin alone does not prevent a future major bump from being merged the same way; making `Quality Gate / gate` (or `Build and Test`) a required check is the durable guard.
- **Scope overlap with other red fronts.** The current `Quality Gate / gate` also fails for coverage exclusions (PR #543), formatting (PR #544), and jscpd (PR #546). Fixing FsCheck removes the `Build` failure but does not by itself turn every `dev` check green.

## Open Questions

- [NEEDS CLARIFICATION: should the team pin FsCheck 2.16.6 now and schedule the v3 migration, or invest in the migration immediately? The recommendation is pin now, migrate in a dedicated change.]
- [NEEDS CLARIFICATION: should `Quality Gate / gate` or `Build and Test` be promoted to a required status check so a red build cannot be merged again? This is a branch-protection decision outside the code fix.]
- [NEEDS CLARIFICATION: is `FsCheck` 2.16.6 still free of known security advisories for the project's supported .NET versions? It is test-only, but the pin should be reviewed before it becomes long-lived.]
