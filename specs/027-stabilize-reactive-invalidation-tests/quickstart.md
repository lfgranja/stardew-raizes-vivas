# Quickstart Verification Guide

- **Feature**: `027-stabilize-reactive-invalidation-tests`

## 1. Confirm zero Stopwatch/MaxRenderTimeMs hazards in target test files
```bash
grep -n "Stopwatch\|MaxRenderTimeMs\|16\.67" LivingRoots.Tests/SoilHealthVisualizationTests.cs LivingRoots.Tests/Invariants/VisualizationInvariantsTests.cs
```
Expected: only XML doc/comment references documenting Gate 3 ownership (`ModConstants.MaxRenderTimeMs`), zero `Stopwatch` usages or `<= ModConstants.MaxRenderTimeMs` assertions.

## 2. Run updated tests 5 consecutive times
```bash
for i in 1 2 3 4 5; do
  GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~SoilHealthVisualizationTests|FullyQualifiedName~T031b_OverlayTiming_ReactiveInvalidation_WithinFrameBudget" --verbosity quiet
done
```

## 3. Run full authoritative gates
```bash
bash scripts/ci/aggregate-gate.sh
GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" bash scripts/release-gate.sh --allow-dirty
python3 scripts/ci/i18n-parity-check.py
git diff --name-only origin/dev -- LivingRoots/
```
