# Quickstart: Validate the stabilized property

Prerequisites: game assemblies at `/home/luis/stardew-assemblies` (host-specific `GamePath`), .NET SDK, worktree `feature/026-stabilize-overlay-cache`.

1. Build once:
   `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln -c Release -m:1`
2. Run the property 5 consecutive times (expect 5 passes):
   ```
   for i in 1 2 3 4 5; do
     GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build --nologo \
       --filter "FullyQualifiedName~RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget"
   done
   ```
3. Run the full suite (expect 0 failed):
   `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build --nologo`
4. Format gate:
   `dotnet format Stardew-LivingRoots.sln --verify-no-changes`
5. Scope check:
   `git diff --name-only origin/dev | grep '^LivingRoots/'` must return nothing (test-only change).
6. Local authoritative gate:
   `bash scripts/ci/aggregate-gate.sh`

Wall-clock frame budget is Gate 3 evidence via `scripts/verify-scenario.sh`; this change does not touch runnable code, so no new evidence bundle is owed.
