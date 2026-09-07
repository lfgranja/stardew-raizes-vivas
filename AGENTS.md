# LIVING ROOTS — PROJECT KNOWLEDGE BASE

**Generated:** 2026-09-07
**Commit:** f4832f8
**Branch:** feat/002-04-composting-bin-service

## OVERVIEW

Stardew Valley agroecological mod (.NET 6 / C# latest) implementing soil health, composting, and sustainable farming mechanics via SMAPI. Follows Domain-Driven Design with layered architecture: Domain (interfaces, models) → Services (implementations, persistence) → Controllers (SMAPI event handlers).

## STRUCTURE

```
.
├── LivingRoots/               # Mod entry point + composition root
│   ├── ModEntry.cs            # Entry point, wires all dependencies
│   ├── ModConstants.cs        # All magic numbers live here
│   ├── Controllers/           # SMAPI event handlers, thread-safe state machine
│   ├── Domain/                # Business logic, interfaces, models
│   │   ├── Interfaces/        # Service contracts (ICompostApplicationService, etc.)
│   │   ├── Models/            # Persistence DTOs (SoilHealthState, CompostingBinData)
│   │   ├── Services/          # Domain services (OrganicWasteValidator, SeasonalDecayMultiplier)
│   │   └── Visualization/     # Color/health DTOs, categories, overlay types
│   └── Services/              # Application services, SMAPI integration
│       ├── Visualization/     # XNA rendering: overlays, tooltips, hoe feedback
│       └── *.cs               # CompostingBinService, SoilHealthService, etc.
├── LivingRoots.Tests/         # xUnit + Moq, one-file-per-subject
├── .specify/                  # Spec-kit SDD workflows
├── .github/                   # CI: build → test → SonarCloud → coverage
└── .editorconfig              # .NET formatting rules (space indent, expression-bodied OFF)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add new game event handler | `LivingRoots/Controllers/ModController.cs` | Must use `ExecuteWithConcurrencyGuard` for re-entrancy |
| Add new domain concept | `LivingRoots/Domain/` (interface) + `LivingRoots/Services/` (impl) | DDD pattern: interface in Domain, impl in Services |
| Change soil health limits | `LivingRoots/ModConstants.cs` | No hardcoded values anywhere else |
| Add console command | `ModController.RegisterConsoleCommand()` | Follow existing lock + flag pattern |
| Add new rendering effect | `LivingRoots/Services/Visualization/` | Implement via `IVisualizationService` |
| Thread-safe state | `ModController.cs` | Atomic flags + `Interlocked` operations |
| Security validation | `ModDataService.SanitizePathSegments()` | Path traversal, homoglyph, Unicode normalization |

## CODE MAP

| Symbol | Type | Location | Refs | Role |
|--------|------|----------|------|------|
| `ModEntry` | Class | `LivingRoots/ModEntry.cs` | SMAPI | Composition root, wires all dependencies |
| `ModController` | Class | `LivingRoots/Controllers/ModController.cs` | ModEntry | Event handler orchestrator with atomic state machine |
| `SoilHealthService` | Class | `LivingRoots/Services/SoilHealthService.cs` | ModController | Core game logic: load/save/query soil health per tile |
| `SoilHealthState` | Class | `LiveRoots/Domain/SoilHealthState.cs` | SoilHealthState | Persistence DTO: `Dictionary<LocationName, Dictionary<TileKey, HealthValue>>` |
| `ModDataService` | Class | `LivingRoots/Services/ModDataService.cs` | SoilHealthService | SMAPI data persistence layer with sanitization |
| `VisualizationService` | Class | `LivingRoots/Services/Visualization/VisualizationService.cs` | ModController | XNA render orchestrator: overlays, tooltips, hoe feedback |
| `CompostingBinService` | Class | `LivingRoots/Services/CompostingBinService.cs` | ModController | Machine state machine: Empty→Processing→Ready |
| `ModConstants` | Static | `LivingRoots/ModConstants.cs` | Everywhere | Single source of truth for all constants |

## CONVENTIONS

- **DDD Ubiquitous Language**: `SoilHealth`, `Compost`, `LivingSoil`, `HeirloomSeed`, `CompanionPlanting` — use these names
- **One test file per subject**: `{Subject}Tests.cs` in `LivingRoots.Tests/`
- **Constructor injection** everywhere — primary DI pattern
- **No hardcoded values** — everything in `ModConstants`
- **Sparse cache**: don't store default (0) health values — keeps save files small
- **Async-only for I/O** — never use `.Result` or `.Wait()` on async calls
- **Lock-free atomics for frame coordination** — `Interlocked`, `Volatile.Read`, `volatile` fields
- **Security-first**: all input paths sanitized, Unicode normalized (FormC), bounds-checked

## ANTI-PATTERNS (THIS PROJECT)

- **DO NOT** use `.Result` or `.Wait()` on async calls
- **DO NOT** hardcode tile coordinates or health values — use `ModConstants`
- **DO NOT** store zero health values in runtime cache
- **DO NOT** expose raw exception messages to logs (security)
- **DO NOT** create premature abstractions — YAGNI enforced
- **DO NOT** use expression-bodied methods (editorconfig forbids)
- **DO NOT** suppress overflow checking — `CheckForOverflowUnderflow=true` project-wide

## UNIQUE STYLES

- **Atomic flag state machine** in `ModController` — `_state` field with bit flags for thread-safe registration/unregistration/disposal
- **Rollback on unsubscribe failure** — if event unsubscription partially fails, re-subscribe successfully removed handlers
- **Temp cache pattern** in `SoilHealthService.LoadData` — build in temp, swap on success to prevent partial state
- **Snapshot outside lock** — capture data, release lock, then do I/O
- **ThreadSafeGameLoopEventsStub** — test stub simulating concurrent game events

## COMMANDS

```bash
# Build
dotnet build Stardew-LivingRoots.sln --configuration Release

# Test
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal

# Format check (CI gate)
dotnet format Stardew-LivingRoots.sln --verify-no-changes

# Local dev with game
# Set GamePath in LivingRoots.csproj, press F5
```

## NOTES

- `LivingRoots.csproj` has `AppendTargetFrameworkToOutputPath=false` → output goes to `bin/` not `bin/net6.0/`
- Test project references ModBuildConfig with `EnableModDeploy=false` — SMAPI assemblies copied via custom MSBUILD target
- `InternalsVisibleTo("LivingRoots.Tests")` allows testing internal members
- CI runs SonarCloud with OpenCover coverage format
- lefthook pre-commit: `dotnet format --verify-no-changes`; pre-push: `dotnet test`
