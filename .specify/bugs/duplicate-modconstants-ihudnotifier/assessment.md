# Bug Assessment: Duplicate `ModConstants` class and ambiguous `IHudNotifier` interface break the build

- **Slug**: duplicate-modconstants-ihudnotifier
- **Created**: 2026-10-23
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/39935519800/job/120353978322
- **Verdict**: valid
- **Severity**: critical

## Report (summarized)

GitHub Actions "Build and Test" workflow failed 2 minutes ago in 20s on merge of PR #313 (`pr/test-invariants-viz-degradation-frame`). Four compiler errors:

```
error CS0101: The namespace 'LivingRoots' already contains a definition for 'ModConstants'
  LivingRoots/ModConstants.cs(6,25)
error CS0104: 'IHudNotifier' is an ambiguous reference between 'LivingRoots.Domain.IHudNotifier' and 'LivingRoots.Domain.Visualization.IHudNotifier'
  LivingRoots/Services/Visualization/OverlayRenderer.cs(42,9)
error CS0104: 'IHudNotifier' is an ambiguous reference between 'LivingRoots.Domain.IHudNotifier' and 'LivingRoots.Domain.Visualization.IHudNotifier'
  LivingRoots/Services/Visualization/VisualizationService.cs(20,9)
error CS0104: 'IHudNotifier' is an ambiguous reference between 'LivingRoots.Domain.IHudNotifier' and 'LivingRoots.Domain.Visualization.IHudNotifier'
  LivingRoots/Services/Visualization/OverlayRenderer.cs(53,26)
```

Source: CI job URL above; pasted error log included verbatim in the bug report.

## Symptom

The `dotnet build --no-restore --configuration Release` step fails with 4 errors and 0 warnings. The build cannot produce the mod assembly, so `dotnet test` cannot run either. All four errors are duplicate/ambiguous type definitions introduced by overlapping PR branches merged into `dev`.

## Reproduction

1. Check out `dev` at commit `c3b5118` (or any commit after PRs #295–#313 were merged).
2. Run `dotnet build --no-restore --configuration Release` from the repo root.
3. Observe CS0101 on `ModConstants.cs` and three CS0104 errors on `IHudNotifier`.

[NEEDS CLARIFICATION: none — all four errors are directly observed in the CI log.]

## Suspected Code Paths

- `LivingRoots/ModConstants.cs` — pre-existing file defining `public static class ModConstants` in `namespace LivingRoots`.
- `LivingRoots/Constants.cs` — newly added file (PR #299) ALSO defining `public static class ModConstants` in `namespace LivingRoots`. → CS0101 duplicate.
- `LivingRoots/Domain/IHudNotifier.cs` — newly added (PR #306) defining `interface IHudNotifier` in `namespace LivingRoots.Domain` with `ShowNotification(string)`.
- `LivingRoots/Domain/Visualization/IHudNotifier.cs` — newly added (PR #302/#303) defining `interface IHudNotifier` in `namespace LivingRoots.Domain.Visualization` with `NotifyAccessibilityPatternsDegraded(int)`. → CS0104 ambiguous reference.
- `LivingRoots/Services/Visualization/OverlayRenderer.cs` lines 42, 53 — constructor param and field reference unqualified `IHudNotifier`.
- `LivingRoots/Services/Visualization/VisualizationService.cs` line 20 — constructor param references unqualified `IHudNotifier`.
- `LivingRoots/Services/Visualization/HudNotifierImpl.cs` — implements `LivingRoots.Domain.Visualization.IHudNotifier` (`NotifyAccessibilityPatternsDegraded`).
- `LivingRoots/Services/IHudNotifierImpl.cs` — implements `LivingRoots.Domain.IHudNotifier` (`ShowNotification`). Duplicate implementation class name `HudNotifierImpl` in a different namespace.

## Root Cause Hypothesis

**Confidence: high.**

When PR branches #295–#313 were merged into `dev` (and then into `origin/dev` via `git push origin dev`), each branch introduced overlapping type definitions that were never reconciled:

1. **Duplicate `ModConstants`**: PR #299 (`pr/composting-bin-modentry-assets`) created `LivingRoots/Constants.cs` defining `class ModConstants` in `namespace LivingRoots`, but `LivingRoots/ModConstants.cs` already existed with the same fully-qualified type name. C# does not allow two types with the same full name in the same compilation → CS0101.

2. **Ambiguous `IHudNotifier`**: PR #306 (`pr/feat-modcontroller-integration`) created `LivingRoots/Domain/IHudNotifier.cs`, while PR #302/#303 (`pr/spec-001-overlay-renderer`, `pr/spec-001-tooltip-hud-config`) created `LivingRoots/Domain/Visualization/IHudNotifier.cs`. Both are `interface IHudNotifier` but in different namespaces with different members. `OverlayRenderer.cs` and `VisualizationService.cs` reference `IHudNotifier` without a namespace qualifier, and since both namespaces are in scope (via `using` or file-scoped namespace), the compiler cannot resolve which one → CS0104.

Additionally, `HudNotifierImpl` exists in both `Services/Visualization/HudNotifierImpl.cs` (implements `Domain.Visualization.IHudNotifier`) and `Services/IHudNotifierImpl.cs` (implements `Domain.IHudNotifier`). Same class name in different namespaces is legal in C#, but it signals the same duplication pattern and will cause confusion.

## Proposed Remediation

**Preferred**: Consolidate to a single canonical definition for each type, and update all references to use fully-qualified names or consistent `using` directives.

1. **`ModConstants`**: Keep ONE file. Delete either `LivingRoots/Constants.cs` or `LivingRoots/ModConstants.cs` (or merge their contents into a single file). The AGENTS.md convention states constants live in `LivingRoots/Constants.cs` ("There is no `ModConstants.cs`"), so the cleaner choice is to **merge `ModConstants.cs` contents into `Constants.cs` and delete `ModConstants.cs`**, OR rename `Constants.cs` class to something else if both are intended to coexist. Given the duplicate class name is the error, merging is the correct fix.

2. **`IHudNotifier`**: Keep ONE interface. The two interfaces serve different purposes:
   - `Domain.IHudNotifier.ShowNotification(string)` — general player-facing HUD toast.
   - `Domain.Visualization.IHudNotifier.NotifyAccessibilityPatternsDegraded(int)` — accessibility-degradation notice (FR-009).
   
   Since they have different signatures and purposes, they should be **renamed to distinct names** (e.g., `IHudNotifier` → `IAccessibilityNotifier` for the visualization one, or `IPlayerNotifier` for the domain one). Then update `OverlayRenderer.cs`, `VisualizationService.cs`, `HudNotifierImpl.cs` (both), and `IHudNotifierImpl.cs` to reference the correct, renamed interface. Also update all test files that `new Mock<IHudNotifier>()` to use the correct type.

3. **`HudNotifierImpl`**: After renaming the interfaces, ensure each implementation implements exactly one interface. Consider renaming one class (e.g., `AccessibilityNotifierImpl`) to avoid confusion.

**Alternatives**:
- Keep both `IHudNotifier` interfaces but add `using` aliases (`global::LivingRoots.Domain.IHudNotifier`, `global::LivingRoots.Domain.Visualization.IHudNotifier`) at every call site. This avoids renaming but is fragile and verbose — every new file referencing `IHudNotifier` would reintroduce the ambiguity. Not recommended.
- Put both interfaces in the same namespace. This would make them still duplicate-type (same name, same namespace) → CS0101. Not viable.

**Files likely to change**:
- `LivingRoots/Constants.cs` — merge `ModConstants.cs` contents here, or delete `ModConstants.cs`
- `LivingRoots/ModConstants.cs` — delete (if merging into `Constants.cs`)
- `LivingRoots/Domain/IHudNotifier.cs` — rename interface (e.g., to `IPlayerNotifier`) or delete
- `LivingRoots/Domain/Visualization/IHudNotifier.cs` — rename interface (e.g., to `IAccessibilityNotifier`) or delete
- `LivingRoots/Services/Visualization/OverlayRenderer.cs` — update `IHudNotifier` references to the correct renamed type
- `LivingRoots/Services/Visualization/VisualizationService.cs` — update `IHudNotifier` reference
- `LivingRoots/Services/Visualization/HudNotifierImpl.cs` — update to implement renamed interface
- `LivingRoots/Services/IHudNotifierImpl.cs` — update to implement renamed interface
- `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs` — update `Mock<IHudNotifier>` references
- `LivingRoots.Tests/Invariants/VisualizationDegradationInvariantsTests.cs` — update `Mock<IHudNotifier>` references
- `LivingRoots.Tests/HudNotifierTranslationTests.cs` — update `IHudNotifier` reference
- `LivingRoots.Tests/OverlayHealthReflectionTests.cs` — update `Mock<IHudNotifier>` reference
- `LivingRoots.Tests/SoilHealthVisualizationTests.cs` — update `Mock<IHudNotifier>` references

**Tests to add or update**:
- After the fix, `dotnet build --no-restore --configuration Release` must pass with 0 errors (this is the primary lock test).
- Ensure existing tests that mock `IHudNotifier` compile against the correct renamed interface.

## Risks & Considerations

- **API breakage**: Renaming `IHudNotifier` is a breaking change for any external consumer, but this is an internal mod with no published API surface, so the risk is low.
- **Test files**: Multiple test files reference `IHudNotifier` via `Mock<IHudNotifier>()`; each must be updated to the correct renamed type or the build will still fail.
- **No data migration needed** — this is purely a compile-time type-definition conflict.

## Open Questions

- [NEEDS CLARIFICATION: Which `IHudNotifier` should `OverlayRenderer` and `VisualizationService` use? Based on the code, they construct with `IHudNotifier hudNotifier` and pass it to renderers — this matches the accessibility-degradation notifier (`Domain.Visualization.IHudNotifier`), but confirm with the PR authors.]
- [NEEDS CLARIFICATION: Should `Constants.cs` or `ModConstants.cs` be the canonical file? AGENTS.md says `Constants.cs` is canonical ("There is no `ModConstants.cs`"), suggesting `ModConstants.cs` should be deleted and its contents merged into `Constants.cs`.]

Slug: duplicate-modconstants-ihudnotifier

Next step: `/speckit.bug.fix slug=duplicate-modconstants-ihudnotifier`