<!-- Sync Impact Report:
      Version change: 1.0.0 → 1.1.0 (MINOR: expanded Principle III with same-thread frame coordination guidance — preferred snapshot-diff, permitted lock-free atomics)
      Modified principles: III (added frame coordination patterns: preferred synchronous snapshot-diff, permitted lock-free atomics)
      Added sections: None
      Removed sections: None
      Deferred items: None
 -->

# Living Roots Constitution

## Core Principles

### I. Domain-Driven Design (NON-NEGOTIABLE)
Living Roots models the agroecology domain explicitly. The codebase uses Ubiquitous Language throughout: `SoilHealth`, `Compost`, `CropRotation`, `Polyculture`, `HeirloomSeed`, `CompanionPlanting`. The layered architecture MUST be preserved: Domain (interfaces, business logic, models) → Services (implementations, persistence) → Controllers (game event handlers). Every service interface lives in `Domain/` with its implementation in `Services/`. This separation ensures the domain logic remains testable and independent of SMAPI or game-specific concerns.

### II. Security-First Data Handling
All user-influenced input paths MUST be sanitized. File name sanitization, Unicode normalization (homoglyph detection), and path traversal validation are mandatory for any operation touching the filesystem. Soil health values are always validated to stay within bounds (0-100% per `ModConstants`). `CheckForOverflowUnderflow` is enabled project-wide and MUST NOT be suppressed. These measures protect save data integrity and prevent injection attacks through tile key parsing.

### III. Async-Only Concurrency (NON-NEGOTIABLE)
Async/await is the only acceptable pattern for I/O-bound asynchronous operations. `.Result`, `.Wait()`, and synchronous blocking on async calls are forbidden. The game loop is inherently concurrent; blocking calls risk deadlocks and UI freezes. For same-thread frame coordination (render state snapshots, atomic config swaps, pause/resume flags, disposal state flags), synchronous snapshot-diff patterns (capture → compare → react within a single game tick) are the preferred approach per SMAPI conventions. Lock-free atomic synchronization (`Interlocked`, `Volatile.Read`, `volatile` fields) is permitted where snapshot-diff is insufficient (e.g., re-entrancy guards, cross-frame state flags). `ModEntry.Dispose()` MUST use the established lock + `_disposed` pattern for thread-safe cleanup. All event handlers registered with SMAPI MUST be unregistered on disposal.

### IV. Test-Driven Development
Tests are written before production code. The Red-Green-Refactor cycle is enforced: tests must fail for the right reason before implementation makes them pass. Integration tests cover save/load round-trips, tile key parsing, and security validation paths. Test stubs (`ThreadSafeGameLoopEventsStub`) simulate concurrent game events. One test file per subject: `{Subject}Tests.cs`.

### V. Simplicity and YAGNI
Start with the simplest solution. Do not implement features not needed now. Avoid premature abstractions—remove unused interfaces rather than maintaining them. Constructor injection is preferred over factory patterns unless complexity justifies it. Direct `IMonitor` usage is acceptable when a wrapper adds no value.

## Technology & Architecture Constraints

- **Platform**: .NET 6 with C# latest language version
- **Game Integration**: SMAPI via `Pathoschild.Stardew.ModBuildConfig`—this dependency is permanent
- **Persistence**: Through Stardew Valley's save system only—no external databases
- **Scope**: Single-player only—multiplayer sync is architecturally out of scope
- **Bounds**: All constants come from `ModConstants`—no hardcoded tile coordinates or health values
- **Nullable**: Enabled project-wide; null checks are enforced at compile time

## Quality Gates & Definition of Done

Every change clears all three gates:

**Gate 1 — Static Checks and Tests Pass**
`dotnet build --nologo` and `dotnet test --nologo` must succeed. `dotnet format` must produce no changes.

**Gate 2 — Product-Level Quality Bar**
New user-facing features are usable without documentation. Soil health values are always in range (0-100). Save/load round-trips preserve all data. No regressions in existing test suite.

**Gate 3 — End-to-End Verification**
The core journey passes as a real user: till → modify → save → reload → verify. Values match what a player would expect. This applies to every change touching runnable code, including seemingly unrelated ones.

## Governance

The constitution supersedes all other practices. Amendments require:

1. **Documentation**: Proposed changes stated with rationale in a PR
2. **Approval**: Human review and merge (not automated factory changes)
3. **Migration Plan**: When principles change, existing code must be updated or explicitly grandfathered

**Versioning Policy**: Semantic versioning (MAJOR.MINOR.PATCH):
- MAJOR: Backward-incompatible principle removals or redefinitions
- MINOR: New principles or materially expanded guidance
- PATCH: Clarifications, wording fixes, non-semantic refinements

**Compliance**: All PRs verify alignment with these principles. Complexity must be justified by concrete requirements, not hypothetical future needs. The runtime development guidance lives in `AGENTS.md` files throughout the project.

**Version**: 1.1.0 | **Ratified**: 2026-09-05 | **Last Amended**: 2026-09-05
