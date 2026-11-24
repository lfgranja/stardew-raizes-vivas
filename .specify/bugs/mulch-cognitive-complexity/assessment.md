# Bug Assessment: SonarCloud S3776 — cognitive complexity in MulchApplicationService.LoadData

- **Slug**: mulch-cognitive-complexity
- **Created**: 2026-11-01T05:28:00Z
- **Source**: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&branch=dev&s=IMPACT_RANK&sinceLeakPeriod=true&issueStatuses=OPEN%2CCONFIRMED (issue key `AaGRvPgzusAIaC0DOx9h`)
- **Source host**: sonarcloud.io
- **URL fetch policy**: confirmed-by-user (host not on allowlist; user approved fetch) — issue details retrieved via SonarCloud API.
- **Verdict**: valid
- **Severity**: medium

## Report

SonarCloud issue `AaGRvPgzusAIaC0DOx9h`, rule `csharpsquid:S3776`, type `CODE_SMELL`, SonarCloud severity `CRITICAL`, impact `MAINTAINABILITY / HIGH`. Component `LivingRoots/Services/MulchApplicationService.cs`, line 30 (`LoadData`). Message: "Refactor this method to reduce its Cognitive Complexity from 16 to the 15 allowed." Effort 6min.

## Symptom

`MulchApplicationService.LoadData` contains nested loops/ifs when rebuilding the runtime cache from saved data (iterating locations and tiles, splitting keys, parsing ints, checking `HasMulch`). The branching increases cognitive complexity above the allowed threshold (16 > 15), triggering a CRITICAL code smell in SonarCloud.

## Reproduction

1. Open `LivingRoots/Services/MulchApplicationService.cs`, method `LoadData` (starts ~line 30).
2. Run SonarCloud analysis on branch `dev` — issue reported on `LoadData`.
3. Cognitive complexity is 16 due to nested control flow when processing `savedData.LocationMulchData`.

## Suspected Code Paths

- `LivingRoots/Services/MulchApplicationService.cs:30-107` — `LoadData` method with nested loops and parsing logic.
- `LivingRoots/Services/MulchApplicationService.cs:78-107` — inner loop over tiles doing string split/parse and conditional storage.

## Root Cause Hypothesis

All parsing/validation logic is inlined in `LoadData`, creating nested ifs/loops. Extracting helpers (e.g., `TryParseTileKey`, `BuildTileDictForLocation`) flattens the method and moves complexity to smaller, focused methods. Confidence: high.

## Proposed Remediation

**Preferred**: Extract helper methods to reduce nesting:
- `private static bool TryParseTileKey(string key, out Point point)` to parse `"x,y"` (returns false on failure).
- `private Dictionary<Point, MulchTileEntry> BuildTileDict(Dictionary<string, MulchTileEntry> savedTileDict)` to build the runtime tile dict for a location (skipping defaults).
- Simplify `LoadData` to iterate locations and call helpers, keeping locks minimal.

This preserves behavior (only stores tiles with `HasMulch`, sparse cache) while reducing cognitive complexity below 16.

**Alternatives**:
- Use LINQ — may not reduce complexity and can hurt readability/performance in hot paths.
- Flatten with early returns — partial improvement, likely not enough alone.

**Files likely to change**:
- `LivingRoots/Services/MulchApplicationService.cs`

**Tests to add or update**:
- Existing tests for `MulchApplicationService` should cover load/save; no new tests required if behavior is unchanged.

## Risks & Considerations

- Behavior must remain identical (sparse cache, parsing, lock usage).
- Must follow project conventions (primary constructors, no expression-bodied methods, DI).
- Low risk since it's pure refactoring of parsing/loop structure.

## Open Questions

- [NEEDS CLARIFICATION: does SonarCloud count complexity after helpers differently in this project? The goal is to get LoadData complexity ≤15.]