# Bug Fix: SonarCloud S3776 — cognitive complexity in MulchApplicationService.LoadData

- **Slug**: mulch-cognitive-complexity
- **Fixed**: 2026-11-01T05:35:00Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Extracted `TryParseTileKey(string, out Point)` and `BuildTileDict(Dictionary<string, MulchTileEntry>)` from `MulchApplicationService.LoadData`, moving the string split/parse and sparse-tile filtering out of the nested loops. This flattens `LoadData` and reduces its cognitive complexity below the SonarCloud threshold while preserving behavior (only tiles with `HasMulch` are stored in the sparse runtime cache).

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Services/MulchApplicationService.cs` | modified | Added `TryParseTileKey` and `BuildTileDict` helpers; simplified `LoadData` to use them |

## Diff Highlights

```diff
-                var tileDict = new Dictionary<Point, MulchTileEntry>();
-                foreach (var tileEntry in locEntry.Value)
-                {
-                    var parts = tileEntry.Key.Split(',');
-                    if (parts.Length == 2 &&
-                        int.TryParse(parts[0], out var x) &&
-                        int.TryParse(parts[1], out var y))
-                    {
-                        var state = tileEntry.Value;
-                        if (state.HasMulch) // sparse: only store non-default
-                        {
-                            tileDict[new Point(x, y)] = state;
-                        }
-                    }
-                }
+                var tileDict = BuildTileDict(locEntry.Value);
```

## Tests Added or Updated

- None required (pure refactoring; existing tests cover MulchApplicationService behavior).

## Local Verification

- Commands run: `dotnet build LivingRoots/LivingRoots.csproj` → succeeded (0 errors).
- Commands run: `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build` → 1443 passed, 4 skipped, 0 failed.

## Deviations from Assessment

None — matches the preferred remediation.

## Follow-ups

- Re-run SonarCloud analysis on `dev` to confirm issue `AaGRvPgzusAIaC0DOx9h` is resolved.