# LIVINGROOTS PROJECT

**OVERVIEW:** SMAPI mod composition root — wires domain interfaces to service implementations and orchestrates game events.

## STRUCTURE

```
ModEntry.cs              # Primary composition root (only `new` site for services)
ModConstants.cs          # All magic numbers — costs, durations, limits
AssemblyInfo.cs          # InternalsVisibleTo("LivingRoots.Tests")
Controllers/
  ModController.cs       # Event orchestrator (atomic state machine, ~1k LOC)
  CompostingBinController.cs  # Right-click bin interaction
Domain/
  Interfaces/            # 14 service contracts (ISoilHealthService, etc.)
  Models/                # Persistence DTOs (SoilHealthState, CompostingBinData)
  Services/              # Pure domain logic (OrganicWasteValidator, SeasonalDecayMultiplier)
  Visualization/         # Color/health DTOs, categories, overlay types
Services/
  *.cs                   # Application services (SoilHealthService, CompostingBinService, etc.)
  Visualization/         # XNA renderers: overlays, tooltips, hoe feedback
```

## WHERE TO LOOK

| Task | File |
|------|------|
| Wire new dependency | `ModEntry.cs` — constructor-injection site |
| Add game event | `ModController.cs` — use `ExecuteWithConcurrencyGuard` for re-entrancy |
| Change magic numbers | `ModConstants.cs` — single source of truth |
| Add domain concept | `Domain/Interfaces/` (contract) + `Services/` (impl) |
| Add rendering effect | `Services/Visualization/` — implement `IVisualizationService` |
| Thread-safe state | `ModController.cs` — atomic bit flags on `_state` field |

## CONVENTIONS

- **DDD layering:** Contract in `Domain/Interfaces/`, impl in `Services/` — never invert
- **Constructor injection only** — `ModEntry.cs` is the sole composition root
- **Atomic state machine:** `_state` uses bit flags; mutate via `Interlocked.CompareExchange`
- **Primary constructors** — C# 12 `params` syntax on controllers
- **No expression-bodied members** — editorconfig `csharp_style_expression_bodied_methods = false`
- **DDD language:** `SoilHealth`, `Compost`, `LivingSoil`, `HeirloomSeed`, `CompanionPlanting`

## ANTI-PATTERNS

- **DO NOT** `new` services outside `ModEntry.cs` — breaks DI pattern
- **DO NOT** mutate `_state` without `Interlocked` — races with event unsubscription
- **DO NOT** implement domain logic in `Controllers/` — use `Domain/Services/` or `Services/`
- **DO NOT** use expression-bodied methods — violates editorconfig and root convention
- **DO NOT** suppress overflow — `CheckForOverflowUnderflow=true` project-wide
