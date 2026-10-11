# Bug Verification: Triage of residual risks — not a defect report

- **Slug**: residual-risks-triage
- **Tested**: 2026-10-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

There was no fix to validate: the fix recorded status `not-applied` and changed no source
file, which `git status` confirms. Every factual claim in `fix.md` re-verified correctly,
`dotnet format --verify-no-changes` passes clean, and all five residual-risk items from the
assessment reproduce their described state. The result is `partial` **solely** because the
build and the test suite could not be executed on this machine (no Stardew Valley game
folder), leaving the two inherited `CompostApplicationServiceTests` failures unresolved. No
check failed; two were impossible to run.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Fix modified no source | `git status --porcelain` | **pass** | Only `.specify/bugs/residual-risks-triage/fix.md` added. 11 modified files are all `.specify/scripts` + `AGENTS.md` tooling churn, pre-existing and untouched by the fix. |
| `fix.md` claim: commits exist | `git log -1 f59fa09 301726e 19e2eb5 67d3a33` | **pass** | All four exist, all dated 2026-10-03, messages match as cited. |
| `fix.md` claim: constants reconciled | `sed -n '63,66p' LivingRoots/Constants.cs` | **pass** | `CompostingBinRecipeId = "LivingRoots.CompostingBinRecipe"` — dot convention, not the underscore the assessment recorded. |
| `fix.md` claim: constants now consumed | `rg -n CompostingBinRecipeId LivingRoots/ModEntry.cs` | **pass** | `ModEntry.cs:121`, inside the `Data/CraftingRecipes` `AssetRequested` handler. |
| `fix.md` claim: 3-way item comparison | `sed -n '44,50p' LivingRoots/Services/CompostApplicationService.cs` | **pass** | Checks `QualifiedItemId != CompostItemId && != QualifiedCompostItemId && ItemId != CompostItemId`. |
| Lint / format gate | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | Exit 0, no output. The only CI gate that does not need `GamePath`. |
| Build | `dotnet build Stardew-LivingRoots.sln --configuration Release` | **not-run** | Blocked: `ModBuildConfig.targets(74,5): error : The mod build package can't find your game folder.` 2 errors, 0 warnings. |
| Regression suite | `dotnet test` | **not-run** | Requires the build above. Unavailable for the same reason. |
| Assessment item 1: "2 asset-scoped failures" | — | **not-run** | Inherits the build blocker. Unverifiable here; see §Residual Risks. |
| Assessment item 2: `Ship` cue unverifiable | `rg -n '"Ship"' LivingRoots/ LivingRoots.Tests/` | **pass** | Exactly 2 call sites (`CompostingBinService.cs:91,126`), **0** test assertions. Claim holds. |
| Assessment item 3: full inventory consumes bin | `sed -n '73,86p' LivingRoots/Services/CompostingBinService.cs` | **pass** | Logs at `Warn` on refusal, then resets the bin unconditionally. Claim holds. |
| Assessment item 3: pinned by a test | `rg CollectCompost_WhenInventoryRefuses_StillConsumesBin` | **pass** | Present at `CompostingBinServiceTests.cs:339`. |
| Assessment item 4: no co-op coverage | `rg -in "multiplayer\|getPlayers\|farms\b" LivingRoots/` | **pass** | **Zero matches.** Claim holds — co-op is entirely unexercised. |
| Assessment item 5: no external consumers | `rg '"Version"' manifest.json`; `git tag` | **pass** | Version `0.0.1`, 0 tags. Blast radius genuinely zero. |
| Assessment file path claim | `ls LivingRoots/*Constants*.cs` | **fail (assessment's error)** | `LivingRoots/ModConstants.cs` does not exist; class is in `LivingRoots/Constants.cs`. Pre-existing assessment defect, already recorded in `fix.md`. |

## Output Excerpts

Build blocker — the reason the suite could not run:

```
Pathoschild.Stardew.ModBuildConfig.targets(74,5): error : The mod build package
can't find your game folder. You can specify where to find it;
see https://smapi.io/package/custom-game-path. [LivingRoots.Tests.csproj]
Pathoschild.Stardew.ModBuildConfig.targets(74,5): error : The mod build package
can't find your game folder. ... [LivingRoots.csproj]

FALHA da compilação.
    0 Aviso(s)
    2 Erro(s)
```

`fix.md` claim verification — constants now on a single convention, both consumed:

```
public const string CompostItemId          = "LivingRoots.Compost";
public const string QualifiedCompostItemId  = "(O)LivingRoots.Compost";
public const string CompostingBinItemId    = "LivingRoots.CompostingBin";
public const string CompostingBinRecipeId  = "LivingRoots.CompostingBinRecipe";

ModEntry.cs:121:
  data[ModConstants.CompostingBinRecipeId] = $"... 771/Home/{ModConstants.CompostingBinItemId}/true/default/";
```

Format gate — clean:

```
$ dotnet format Stardew-LivingRoots.sln --verify-no-changes
FORMAT_EXIT=0        # no output
```

## Residual Risks

- **The two `CompostApplicationServiceTests` failures remain unverified — this is the one
  genuinely open thread.** The assessment recorded "exactly 2 failures, both
  `CompostApplicationServiceTests`" when it ran the suite. It could not be re-run here. This
  matters more than it did at assessment time, because `CompostApplicationService` was edited
  *after* that run by `f59fa09` and `301726e`. Those failures are therefore either stale, or
  newly caused by the item-id normalization — **the evidence does not distinguish these**, and
  the file has 6 test methods (`rg -c` on `[Fact]`/`[Theory]`). Resolving this requires a
  machine with the game assemblies.
- **`dotnet format` is a whitespace/indentation gate only.** Per project `AGENTS.md` it does not
  enforce `csharp_style_expression_bodied_methods = false`. A clean run is weak evidence of
  correctness and none at all of runtime behaviour.
- **No reproduction was performed, and none was possible.** The assessment is explicitly
  `invalid` — "No symptom… Not applicable — there is no failing behaviour" (`assessment.md:26,32`).
  The "original symptom no longer reproduces" criterion is therefore vacuous here rather than
  satisfied. Nothing was proven fixed; nothing was proven broken.
- **Nothing was exercised at runtime.** The mod was never launched. Static analysis and a format
  check cannot detect a wiring defect — which is precisely the class of bug present elsewhere in
  this repo (the visualization layer: `Initialize()` never called, `SetTileHealthData` and
  `TriggerHoeFeedback` with no call site). A green static story here is not evidence the mod runs.
- **CI cannot arbitrate.** `build_and_test.yml:53` is gated behind `vars.BUILDDEPS_ENABLED == 'true'`,
  so even a green local suite would have no CI counterpart right now.

## Recommendation

**Close the bug loop, but keep the closure explicitly scoped.** The assessment's verdict is
sustained on the evidence: it is a triage of a residual-risk list, not a defect report, and all
five items re-verified as described. The `fix.md` record is accurate — its central correction
(that the `QualifiedItemId` hazard was already remediated by `f59fa09`/`301726e`, and that the
`ModConstants` convention was already reconciled with a live consumer at `ModEntry.cs:121`) is
confirmed on every point. Do not reopen this slug.

`partial` reflects an environment limit, not a doubt about the closure. Do not read it as "the
fix might not hold" — there was no fix, and the four verifiable claims all held.

Carry one item forward as a **new** bug, since it is a test-failure report and this slug's
verdict forbids treating it as a defect here:

> **`compost-application-tests-failing`** — 2 failing tests in `CompostApplicationServiceTests`,
> observed before the 2026-10-03 item-id edits and not re-run since. Run
> `GamePath=$GP dotnet test --no-build -c Release --filter "FullyQualifiedName~CompostApplicationServiceTests"`
> on a machine with the game assemblies to determine whether they are stale or newly introduced.
