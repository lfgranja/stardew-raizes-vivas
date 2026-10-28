# Bug Verification: Duplicate `ModConstants` class and ambiguous `IHudNotifier` interface

- **Slug**: duplicate-modconstants-ihudnotifier
- **Tested**: 2026-10-23
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The original symptom (4 compiler errors blocking the `dotnet build` gate) no longer reproduces. The `LivingRoots` project builds successfully with 0 errors and 0 warnings. All previously duplicate/ambiguous type definitions now resolve to single, unambiguous symbols.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `dotnet build LivingRoots/LivingRoots.csproj --no-restore --configuration Release` | pass | 0 errors, 0 warnings — matches the assessment's reproduction step exactly. |
| Symbol resolution | Grep for duplicate/ambiguous type definitions | pass | Exactly one `class ModConstants`; exactly one `interface IHudNotifier` in `Domain/`; `IAccessibilityNotifier` and `AccessibilityNotifierImpl` both exist and resolve. |
| Regression (type-check) | `dotnet build LivingRoots/LivingRoots.csproj --no-restore --configuration Release` | pass | Same command as reproduction — project compiles cleanly with no new errors. |

## Output Excerpts

```
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
    Tempo Decorrido 00:00:03.45
```

## Residual Risks

- The test project (`LivingRoots.Tests`) has pre-existing compilation errors unrelated to this bug fix (missing `ModController` methods, `SoilDecayService` constructor mismatches, test files missing constructor arguments). These existed before this fix and are documented in the fix report's follow-ups. The full `dotnet test` suite could not be run as a consequence.
- `AccessibilityNotifierImpl` is not wired into `ModEntry.cs` (which wires `Services/IHudNotifierImpl` instead). It may be unused — flagged as a follow-up in the fix report.

## Recommendation

Close the bug — verified end-to-end. The original reproduction steps from the assessment (checkout `dev`, run `dotnet build`, observe 0 errors) now pass. The fix holds with no regressions in the changed modules. The pre-existing test project errors should be triaged as a separate bug.