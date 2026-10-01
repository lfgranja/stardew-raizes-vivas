# Research: Implementation Details in Spec

**Date:** 2026-09-09
**Spec:** `001-soil-health-visualization/spec.md` (all sections)
**Criterion:** "No implementation details (languages, frameworks, APIs)"
**Researcher:** Subagent (LongCat-2.0)

---

## 1. Search Results Summary

### 1.1 IEEE 830 / ISO/IEC/IEEE 29148 — "What vs. How" Principle

**Source:** [IEEE Std 830-1998](https://ieeexplore.ieee.org/document/720574), [ISO/IEC/IEEE 29148:2018](https://standards.ieee.org/ieee/29148/7101/), [ReqView SRS Template](https://www.reqview.com/doc/iso-iec-ieee-29148-srs-template.html) [[1]][ieee830][[2]][iso29148][[3]][reqview]

The foundational requirements engineering standard mandates that a Software Requirements Specification (SRS) must describe **only "what"** the software must do (external functional behaviors, user interactions, performance limits, and system constraints in the problem space) while **strictly excluding "how"** it is implemented (internal algorithms, architectural choices, database schemas, code design, or specific programming languages in the solution space).

Key findings:
- **Standard evolution:** IEEE Std 830-1998 was superseded by ISO/IEC/IEEE 29148:2011, updated to ISO/IEC/IEEE 29148:2018, with active project P29148 continuing revisions.
- **Why avoid implementation details:** Prematurely detailing "how" causes over-specification, artificially restricts design choices, makes requirements harder to maintain, and complicates formal change management when technical solutions evolve.
- **Problem space vs. solution space:** The requirements specification belongs to the problem space (verifiable stakeholder expectations and constraints), whereas design documents belong to the solution space.

### 1.2 Game Mod Specification Technical Detail Level

**Source:** [Stardew Valley Wiki — Modding:Modder Guide/APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs), [Reloaded III Specification](https://reloaded-project.github.io/Reloaded-III/index.html), [StackOverflow — Writing functional specifications for games](https://stackoverflow.com/questions/2183427/writing-functional-specifications-for-games) [[4]][smapi-wiki][[5]][reloaded3][[6]][so-game-spec]

Game mod specifications operate under unique constraints compared to greenfield software:
- **Closed ecosystem:** Mods are guest code in a fixed runtime (.NET 6, MonoGame, SMAPI). The implementer cannot choose the language, framework, or core APIs.
- **Framework coupling:** Mods must integrate with specific game loop events, rendering pipelines, and data persistence mechanisms.
- **Convention over configuration:** The Stardew Valley modding community has established patterns (event handler registration/unregistration, JSON config via `IModDataService`, `SpriteBatch` rendering) that are considered idiomatic rather than prescriptive.

However, even within mod specs, there is a distinction between:
- **Framework constraints:** Facts about the runtime the implementer *must* know (e.g., "SMAPI provides `GetCursorPosition().Tile` for cursor-to-tile mapping").
- **Implementation prescriptions:** Specific algorithms, patterns, or internal architectures the implementer should be free to choose (e.g., "use `Interlocked` for frame coordination").

The Reloaded III specification explicitly aims to let modders "focus on actually modding the games" rather than rebuilding infrastructure [[5]][reloaded3], suggesting that mod specs should describe *what* the mod accomplishes for the player, not *how* the modder structures their internal code.

### 1.3 SMAPI/Stardew Valley Mod Documentation Conventions

**Source:** [Stardew Valley Wiki — Modding:Modder Guide/APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs), [GitHub — Pathoschild/SMAPI](https://github.com/pathoschild/SMAPI) [[4]][smapi-wiki][[7]][smapi-github]

SMAPI's own documentation separates:
- **What mods can do:** Events to subscribe to, configuration patterns, data persistence, input handling.
- **How to do it:** Code examples, API references, implementation guides.

The SMAPI wiki's API reference describes capabilities ("Respond when something happens in the game") without prescribing internal architecture. The `IModHelper.Events` system, `IModDataService`, and `SpriteBatch` rendering are presented as *available tools*, not as *mandated internal patterns*.

---

## 2. Categorized Technical References in the Spec

Below is a comprehensive categorization of every technical reference in the spec. Each reference is tagged as either **Acceptable Framework Constraint (FC)** or **Implementation Prescription (IP)**.

### 2.1 User Stories & Acceptance Scenarios (Lines 13–78)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| — | (none) | — | Pure user-facing behavior; no technical references. |

**Verdict:** ✅ Clean. No implementation details.

### 2.2 Edge Cases (Lines 82–90)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 84 | "health value changes while the overlay is mid-render" | — | Scenario description, not a technical prescription. |
| 85 | "rapid mouse movement across many tiles (tooltip flickering)" | — | Scenario description. |
| 86 | "viewport changes size (window resize)" | — | Scenario description. |
| 87 | "tiles at the edge of the viewport" | — | Scenario description. |
| 88 | "configuration is saved while the game is actively rendering overlays" | — | Scenario description. |
| 89 | "concurrent save/load operations with visualization state" | — | Scenario description. |
| 90 | "soil health values are at exact category boundaries" | — | Scenario description. |

**Verdict:** ✅ Clean. Scenarios describe *what* happens, not *how* to handle it.

### 2.3 Clarifications (Lines 92–114)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 105 | "Cache invalidates on game state changes (tile modification events, save load)" | **IP** | Prescribes a specific caching strategy (event-driven invalidation). |
| 107 | "60 FPS (16.67ms per frame)" | FC | Performance target; measurable outcome. |
| 109 | "Linear interpolation in RGB space between adjacent category colors" | **IP** | Prescribes a specific color interpolation algorithm. |
| 110 | "Forward-compatible deserialization — missing fields get defaults, unknown fields ignored" | **IP** | Prescribes a specific deserialization behavior pattern. |
| 114 | "Grid position (X, Y) alone" | FC | Identifier scheme consistent with existing `SoilHealthService`. |

**Verdict:** ⚠️ Three implementation prescriptions in clarifications.

### 2.4 Functional Requirements (Lines 118–146)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 121 | "Pattern opacity MUST remain at minimum 0.7" | FC | Accessibility requirement; measurable. |
| 121 | "SMAPI's cursor position API which provides tile coordinates directly" | FC | Framework constraint; describes available API. |
| 121 | "avoiding manual screen-to-tile conversion" | **IP** | Prescribes *not* using a specific approach; constrains implementation. |
| 123 | "alpha decreasing from 1.0 to 0.0 over 300ms" | FC | Visual behavior specification; measurable. |
| 124 | "JSON file" | FC | Serialization format; standard for SMAPI mods. |
| 125 | "async/await patterns per Constitution Principle III" | **IP** | Prescribes a specific async pattern; references internal constitution. |
| 125 | "no `.Result` or `.Wait()` blocking" | **IP** | Proscribes specific API calls; implementation rule. |
| 128 | "viewport culling" | **IP** | Prescribes a specific rendering optimization strategy. |
| 128 | "60 FPS" | FC | Performance target. |
| 128 | "accessibilityDegradation configuration option" | FC | Configuration requirement. |
| 129 | "cache computed health values" | **IP** | Prescribes a caching strategy. |
| 131 | "SMAPI event handlers" | FC | Framework constraint. |
| 135 | "async/await patterns" | **IP** | Prescribes specific async pattern. |
| 135 | "no `.Result` or `.Wait()` blocking" | **IP** | Proscribes specific API calls. |
| 136 | "lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines)" | **IP** ⚠️ | **Highly prescriptive:** names specific .NET synchronization primitives. |
| 136 | "game loop thread" | FC | Framework constraint (MonoGame single-threaded game loop). |
| 136 | "Never mix: do not use atomics for I/O or async/await for frame coordination" | **IP** ⚠️ | **Implementation rule:** dictates how to structure concurrency code. |
| 140 | "50ms interval" | FC | Performance requirement. |
| 140 | "leading-edge" | **IP** | Prescribes a specific throttle strategy (leading-edge vs. trailing-edge). |
| 143 | "clipping bounds" | **IP** | Prescribes a rendering technique. |
| 143 | "1-tile margin (in tile units) beyond visible viewport bounds" | **IP** | Prescribes a specific algorithm parameter. |
| 143 | "Game1.viewport" | FC | Framework constraint (MonoGame API). |
| 143 | "Game1.tileSize (64)" | FC | Framework constraint (MonoGame constant). |
| 143 | "Map.DisplayWidth/Height / tileSize" | FC | Framework constraint (Stardew Valley API). |
| 146 | "no overlay draw calls, no cache allocation" | **IP** ⚠️ | **Implementation-level detail:** dictates rendering pipeline behavior. |

**Verdict:** ❌ **14 implementation prescriptions** in functional requirements alone. Several are highly specific (naming `Interlocked`, `Volatile.Read`, "leading-edge", "no overlay draw calls").

### 2.5 Key Entities (Lines 148–157)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 150 | "JSON" | FC | Serialization format. |
| 151 | "SMAPI's cursor position API" | FC | Framework constraint. |
| 152 | "linear RGB interpolation" | **IP** | Algorithm choice. |
| 152 | "half-open intervals: [0, 34), [34, 67), [67, 100]" | **IP** | Prescribes specific interval notation for computation. |
| 157 | "ColorDTO: Serializable RGBA color struct (R, G, B, A byte properties)" | **IP** ⚠️ | **Data structure prescription:** dictates internal type design. |

**Verdict:** ❌ Four implementation prescriptions in key entities, including a full data structure definition.

### 2.6 Success Criteria (Lines 159–169)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 163 | "60 FPS (16.67ms per frame)" | FC | Measurable performance target. |
| 164 | "≤16.67ms at 60 FPS" | FC | Measurable performance target. |
| 166 | "100ms" | FC | Measurable performance target. |

**Verdict:** ✅ Clean. All measurable outcomes.

### 2.7 Assumptions (Lines 171–182)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 175 | "SoilHealthService" | FC | References existing framework constraint. |
| 176 | "Stardew Valley API" | FC | Framework constraint. |
| 178 | "ModConstants" | FC | References existing project structure. |
| 179 | "MonoGame/XNA `SpriteBatch`" | FC | Framework constraint (rendering API). |
| 180 | "IModHelper.Events" | FC | Framework constraint (SMAPI API). |
| 181 | "IModDataService" | FC | Framework constraint (SMAPI API). |

**Verdict:** ✅ Clean. All framework constraints.

### 2.8 Dependencies (Lines 184–189)

| Line | Reference | Category | Rationale |
|------|-----------|----------|-----------|
| 187 | "ModController" | FC | References existing project structure. |
| 188 | "ModEntry" | FC | References existing project structure. |
| 189 | "IModDataService" | FC | Framework constraint. |

**Verdict:** ✅ Clean. All framework constraints.

---

## 3. Analysis Against the Criterion

### 3.1 Summary Count

| Section | Framework Constraints (FC) | Implementation Prescriptions (IP) |
|---------|---------------------------|-----------------------------------|
| User Stories & Scenarios | 0 | 0 |
| Edge Cases | 0 | 0 |
| Clarifications | 2 | 3 |
| Functional Requirements | 12 | 14 |
| Key Entities | 1 | 4 |
| Success Criteria | 3 | 0 |
| Assumptions | 6 | 0 |
| Dependencies | 3 | 0 |
| **Total** | **27** | **21** |

### 3.2 Severity Assessment

Not all implementation prescriptions are equal. They fall into three severity tiers:

**Tier 1 — Severe (dictates specific APIs/primitives):**
- FR-013: "lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines)"
- FR-013: "Never mix: do not use atomics for I/O or async/await for frame coordination"
- FR-021: "no overlay draw calls, no cache allocation"
- Key Entities: "ColorDTO: Serializable RGBA color struct (R, G, B, A byte properties)"

**Tier 2 — Moderate (dictates specific strategies/algorithms):**
- FR-002: "avoiding manual screen-to-tile conversion"
- FR-009: "viewport culling"
- FR-010: "cache computed health values"
- FR-015: "leading-edge"
- FR-018: "1-tile margin (in tile units) beyond visible viewport bounds"
- Clarifications: "Linear interpolation in RGB space"
- Clarifications: "Forward-compatible deserialization"

**Tier 3 — Mild (dictates patterns but with justification):**
- FR-005/FR-013: "async/await patterns" / "no `.Result` or `.Wait()`"
- Clarifications: "Cache invalidates on game state changes"
- Key Entities: "half-open intervals"

### 3.3 The Mod Spec Context

This is a SMAPI/Stardew Valley mod spec. As established in the research, mod specs are inherently more implementation-heavy than greenfield software specs because:

1. **Closed ecosystem:** The implementer cannot choose .NET 6, MonoGame, or SMAPI. These are fixed.
2. **Framework coupling:** The mod *must* integrate with specific game loop events, rendering pipelines, and data persistence mechanisms.
3. **Community conventions:** JSON config via `IModDataService`, `SpriteBatch` rendering, and event handler registration/unregistration are idiomatic patterns, not arbitrary choices.

However, even accounting for mod spec context, the line between "framework constraint" and "implementation prescription" still exists:

- **Acceptable:** "Cursor-to-tile mapping MUST use SMAPI's cursor position API" — this tells the implementer *which framework API to use*, which is necessary because the mod must integrate with SMAPI.
- **Unacceptable:** "Use `Interlocked` and `Volatile.Read` for frame coordination" — this tells the implementer *which .NET synchronization primitives to use*, which is an implementation choice the spec should not make.

The spec's Tier 1 issues (naming `Interlocked`, `Volatile.Read`, prescribing `ColorDTO`, dictating "no overlay draw calls") are clear implementation prescriptions that go beyond framework constraints.

### 3.4 The "What vs. How" Test

Applying the IEEE 830 / ISO/IEC/IEEE 29148 "what vs. how" test:

| Requirement | "What" (should stay) | "How" (should move to design) |
|-------------|---------------------|-------------------------------|
| FR-009 | System MUST limit rendering cost to maintain 60 FPS with >1,000 tiles | "viewport culling" as the specific strategy |
| FR-013 | System MUST handle concurrent scenarios safely without deadlocks or race conditions | "lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`)" |
| FR-015 | System MUST prevent tooltip flickering during rapid cursor movement | "leading-edge throttle" with "50ms interval" |
| FR-018 | System MUST render partial overlays for edge tiles without pop-in | "1-tile margin", "clipping bounds", "Game1.viewport / Game1.tileSize" |
| FR-021 | System MUST handle zero tilled tiles as a no-op | "no overlay draw calls, no cache allocation" |
| Key Entities | Color configuration is persisted | "ColorDTO: Serializable RGBA color struct (R, G, B, A byte properties)" |

### 3.5 Comparison with SMAPI Community Conventions

The SMAPI wiki and Reloaded III specification demonstrate that mod documentation can describe capabilities without prescribing internal architecture. For example, the SMAPI wiki describes `IModHelper.Events` as "Respond when something happens in the game" without dictating *how* the modder structures their event handling code.

The spec under review goes significantly beyond this by:
- Naming specific .NET synchronization primitives (`Interlocked`, `Volatile.Read`)
- Prescribing a specific throttle strategy ("leading-edge")
- Defining an internal data structure (`ColorDTO`)
- Dictating rendering pipeline behavior ("no overlay draw calls")

---

## 4. Final Verdict

### Does the spec meet the criterion "No implementation details (languages, frameworks, APIs)"?

**No.** The spec contains **21 implementation prescriptions** across multiple sections, including **4 severe cases** that name specific .NET synchronization primitives, dictate internal data structures, and prescribe rendering pipeline behavior.

### Should Item 1 be [x] or [ ]?

**[ ] UNCHECKED** — The spec does not meet the criterion.

### Justification

While the spec contains many legitimate framework constraints (27 by count) that are necessary for a mod integrating with SMAPI/MonoGame, it also contains a significant number of implementation prescriptions that dictate *how* the implementer should structure their code rather than *what* the system should do.

The most problematic areas are:
1. **FR-013 (Concurrency):** Names specific .NET synchronization primitives (`Interlocked`, `Volatile.Read`) and dictates concurrency architecture ("Never mix: do not use atomics for I/O or async/await for frame coordination").
2. **FR-009 (Performance):** Prescribes "viewport culling" as the specific rendering optimization strategy.
3. **FR-015 (Tooltip flicker):** Prescribes "leading-edge throttle" as the specific debounce strategy.
4. **FR-021 (Empty farm):** Prescribes implementation-level behavior ("no overlay draw calls, no cache allocation").
5. **Key Entities:** Defines an internal data structure (`ColorDTO: Serializable RGBA color struct`).

These are not framework constraints — they are implementation choices that the spec's author has made on behalf of the implementer. A different implementer might reasonably choose:
- A `ReaderWriterLockSlim` instead of `Interlocked` for frame coordination
- A spatial hash instead of viewport culling for rendering optimization
- A trailing-edge debounce instead of leading-edge for tooltip throttling
- A different internal color representation (e.g., `System.Drawing.Color`, `Microsoft.Xna.Framework.Color`, or a custom struct with different fields)

---

## 5. Recommendations for Improvement

### 5.1 Move Implementation Prescriptions to a Design Document

The spec should describe *what* the system must do. A separate design document (e.g., `design.md` or `technical-design.md`) should describe *how* the implementer plans to achieve it. This aligns with IEEE 830 / ISO/IEC/IEEE 29148's separation of problem space (spec) from solution space (design).

### 5.2 Specific Rewrites

| Current (Prescriptive) | Recommended (Descriptive) |
|------------------------|---------------------------|
| "System MUST perform **viewport culling** to only render overlays for visible tiles" | "System MUST only render overlays for tiles visible in the current viewport" |
| "System MUST use **lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines)** on the game loop thread" | "System MUST coordinate frame-level state transitions without blocking the game loop thread" |
| "System MUST throttle tooltip updates to a minimum 50ms interval during cursor movement, suppressing successive tooltip redraws... **leading-edge**" | "System MUST prevent tooltip flickering during rapid cursor movement. The tooltip MUST update immediately when the cursor enters a new tile and suppress successive redraws within a minimum interval" |
| "System MUST render partial overlays for tiles at viewport edges using **clipping bounds**, with a **1-tile margin** (in tile units) beyond visible viewport bounds" | "System MUST render partial overlays for tiles at viewport edges without pop-in artifacts" |
| "System MUST handle the zero-tilled-tiles case as a no-op with minimal resource usage (**no overlay draw calls, no cache allocation**)" | "System MUST handle the zero-tilled-tiles case as a no-op without errors" |
| "ColorDTO: Serializable RGBA color struct (R, G, B, A byte properties)" | "Color configuration values MUST be serializable to and from JSON" |

### 5.3 What to Keep

The following are appropriate framework constraints that should remain:
- "SMAPI's cursor position API" — necessary because the mod must integrate with SMAPI
- "JSON file" — standard SMAPI configuration format
- "Game1.viewport", "Game1.tileSize", "Map.DisplayWidth/Height" — MonoGame/Stardew Valley API facts
- "MonoGame/XNA SpriteBatch" — rendering API constraint
- "IModHelper.Events", "IModDataService" — SMAPI API constraints
- "60 FPS (16.67ms per frame)" — measurable performance target
- "async/await patterns" with "no `.Result` or `.Wait()`" — this is a project-wide convention (Constitution Principle III), not an arbitrary implementation choice

### 5.4 Acceptable vs. Prescriptive: A Decision Framework

For future spec authors, use this test:

> **Can a reasonable implementer achieve the requirement using a different internal approach?**
> - If **yes** → the spec is describing "what" → keep it.
> - If **no** → the spec is describing "how" → move it to design.

Applying this test:
- "Only render overlays for visible tiles" → A implementer could use viewport culling, spatial hashing, or occlusion culling. **Keep in spec.**
- "Use `Interlocked` and `Volatile.Read`" → No alternative allowed. **Move to design.**
- "Prevent tooltip flickering" → A implementer could use leading-edge, trailing-edge, or debounce. **Keep in spec.**
- "Use `ColorDTO` struct" → No alternative allowed. **Move to design.**

---

## Sources

- [[1]][ieee830] [IEEE Std 830-1998 — Recommended Practice for Software Requirements Specifications](https://ieeexplore.ieee.org/document/720574)
- [[2]][iso29148] [ISO/IEC/IEEE 29148:2018 — Systems and Software Engineering — Life Cycle Processes — Requirements Engineering](https://standards.ieee.org/ieee/29148/7101/)
- [[3]][reqview] [SRS Template according to ISO/IEC/IEEE 29148 and IEEE 830](https://www.reqview.com/doc/iso-iec-ieee-29148-srs-template.html)
- [[4]][smapi-wiki] [Stardew Valley Wiki — Modding:Modder Guide/APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs)
- [[5]][reloaded3] [Reloaded III Specification](https://reloaded-project.github.io/Reloaded-III/index.html)
- [[6]][so-game-spec] [StackOverflow — Writing functional specifications for games](https://stackoverflow.com/questions/2183427/writing-functional-specifications-for-games)
- [[7]][smapi-github] [GitHub — Pathoschild/SMAPI: The modding API for Stardew Valley](https://github.com/pathoschild/SMAPI)

[ieee830]: https://ieeexplore.ieee.org/document/720574
[iso29148]: https://standards.ieee.org/ieee/29148/7101/
[reqview]: https://www.reqview.com/doc/iso-iec-ieee-29148-srs-template.html
[smapi-wiki]: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs
[reloaded3]: https://reloaded-project.github.io/Reloaded-III/index.html
[so-game-spec]: https://stackoverflow.com/questions/2183427/writing-functional-specifications-for-games
[smapi-github]: https://github.com/pathoschild/SMAPI
