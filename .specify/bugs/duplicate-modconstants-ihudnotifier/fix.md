# Bug Fix: Duplicate `ModConstants` class and ambiguous `IHudNotifier` interface

- **Slug**: duplicate-modconstants-ihudnotifier
- **Fixed**: 2026-10-23
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Resolved 4 compiler errors (CS0101 ×1, CS0104 ×3) that blocked the `dotnet build` gate. The `LivingRoots` project now compiles cleanly with 0 errors and 0 warnings.

Two root causes were addressed:

1. **Duplicate `ModConstants`** (`LivingRoots/ModConstants.cs` vs `LivingRoots/Constants.cs`): both files defined `public static class ModConstants` in `namespace LivingRoots`. Merged `ModConstants.cs` contents into `Constants.cs` (the canonical file per AGENTS.md) and deleted `ModConstants.cs`. For conflicting values (`MachineCompostingBinId`, `CompostTextureAssetName`, etc.), the `Constants.cs` values were kept — these are verified correct by `CompostingBinConstantsTests.cs`.

2. **Ambiguous `IHudNotifier`** (`LivingRoots/Domain/IHudNotifier.cs` vs `LivingRoots/Domain/Visualization/IHudNotifier.cs`): both defined `interface IHudNotifier` with different members. Renamed `Domain.Visualization.IHudNotifier` → `IAccessibilityNotifier` (it has a different signature: `NotifyAccessibilityPatternsDegraded(int)` vs `ShowNotification(string)`), and renamed its implementation `HudNotifierImpl` → `AccessibilityNotifierImpl`. `Domain.IHudNotifier` (used by `OverlayRenderer`/`VisualizationService` via `ShowNotification`) is unchanged, eliminating the ambiguity.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Constants.cs` | modified | Added `BinInvalidDropInShakeTimerMs` (750) and `PatternMinOpacity` (0.7f) — unique constants from `ModConstants.cs` not already present. Kept `Constants.cs` values for all conflicts (verified correct by `CompostingBinConstantsTests.cs`). |
| `LivingRoots/ModConstants.cs` | removed | Duplicate `class ModConstants` in `namespace LivingRoots` — contents merged into `Constants.cs`. |
| `LivingRoots/Domain/Visualization/IHudNotifier.cs` | removed | Renamed to `IAccessibilityNotifier.cs`. |
| `LivingRoots/Domain/Visualization/IAccessibilityNotifier.cs` | added | Interface renamed from `IHudNotifier` to `IAccessibilityNotifier`; same member (`NotifyAccessibilityPatternsDegraded(int)`). |
| `LivingRoots/Services/Visualization/HudNotifierImpl.cs` | removed | Renamed to `AccessibilityNotifierImpl.cs`. |
| `LivingRoots/Services/Visualization/AccessibilityNotifierImpl.cs` | added | Class renamed from `HudNotifierImpl` to `AccessibilityNotifierImpl`; implements `IAccessibilityNotifier`. |
| `LivingRoots.Tests/HudNotifierTranslationTests.cs` | modified | `IHudNotifier` → `IAccessibilityNotifier`, `HudNotifierImpl` → `AccessibilityNotifierImpl`, updated XML doc `<see cref>` references. |

## Tests Added or Updated

- No new tests added. Existing test `HudNotifierTranslationTests.cs` updated to reference renamed types. All other test files using `Mock<IHudNotifier>()` resolve unambiguously to `Domain.IHudNotifier` (unchanged interface) and require no changes.

## Local Verification

- Commands run: `dotnet build LivingRoots/LivingRoots.csproj --no-restore --configuration Release` → **0 errors, 0 warnings** (success)
- The full `dotnet build --no-restore --configuration Release` (including test project) has pre-existing errors unrelated to this bug fix (missing methods on `ModController`, `SoilDecayService` constructor mismatches, test files missing constructor arguments). These existed before this fix and are out of scope.

## Deviations from Assessment

None. The fix follows the proposed remediation exactly:
- `Constants.cs` is the canonical file (per AGENTS.md)
- `ModConstants.cs` deleted
- `Domain.Visualization.IHudNotifier` renamed to `IAccessibilityNotifier`
- `Services/Visualization/HudNotifierImpl` renamed to `AccessibilityNotifierImpl`
- Test file updated for renamed types

## Follow-ups

- The test project (`LivingRoots.Tests`) has pre-existing compilation errors unrelated to this fix (missing `ModController` methods, `SoilDecayService` constructor signature, missing constructor arguments). These should be triaged separately.
- Consider whether `AccessibilityNotifierImpl` is actually wired anywhere in production code (`ModEntry.cs` wires `Services/IHudNotifierImpl`, not `Services/Visualization/AccessibilityNotifierImpl`) — if unused, consider removing it.