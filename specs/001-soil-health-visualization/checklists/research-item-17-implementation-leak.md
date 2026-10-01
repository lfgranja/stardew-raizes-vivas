# Research Item 17: Implementation Details Leak into Specification

**Researched**: 2026-09-09
**Spec**: `001-soil-health-visualization/spec.md`
**Criterion**: "No implementation details leak into specification"

---

## 1. Search Results Summary

### Source 1: Specification vs. Design Document vs. Implementation Details (2025–2026)
*Sources: [Software Engineering Stack Exchange](https://softwareengineering.stackexchange.com/questions/445012/specification-vs-design-document-implementation-details), [Atlassian Engineering](https://www.atlassian.com/blog/technology/software-design-document-vs-functional-spec), [Allegro Tech](https://allegro.tech/blog/spec-driven-development-ai-era), [mmapped.blog](https://mmapped.blog/posts/implementation-details-leak-architecture.html)*

**Key findings**:
- **Specification** defines business rules, functional goals, API contracts, and non-functional constraints. It remains strictly technology-agnostic and client-focused.
- **Design Document** outlines component interaction, system topology, data flow, and external interfaces without exposing concrete code structures.
- **Implementation Details** are concrete class definitions, database queries, framework attributes, and library configurations.
- An **implementation leak** occurs when low-level code mechanics bleed into high-level specifications. *Symptom*: documentation requires edits every time internal function signatures are refactored.
- **Best practice**: Specs should define stable interface contracts; underlying implementation can change freely without invalidating the spec.

### Source 2: Design Constraints vs. Implementation Prescriptions
*Sources: [University of Toronto / IEEE SRS Standards](https://www.cs.toronto.edu/~jm/2108S/SRS.pdf), [Software Engineering Stack Exchange](https://softwareengineering.stackexchange.com/questions/requirements-vs-implementation-prescriptions), [Jama Software](https://www.jamasoftware.com/requirements-management-guide/writing-requirements/srs-quality-attributes)*

**Key findings**:
- **Design constraints** are valid SRS requirements — external boundaries (regulatory, hardware, compatibility, platform mandates) that restrict choices to ensure feasibility.
- **Implementation prescriptions** are over-specification — dictating specific algorithms, design patterns, or code structures. These harm SRS quality by tying verification to internal mechanisms rather than business outcomes.
- **Verifiability principle**: Requirements should be verified by observing system behavior, not internal implementation mechanics.
- **Maintainability impact**: Dictating implementation details causes requirement documents to become stale whenever technical refactoring occurs.

### Source 3: Feature Specification Readiness & Quality Gates
*Sources: [Docsie](https://www.docsie.io), [Testomat.io](https://testomat.io), [SonarSource](https://www.sonarsource.com)*

**Key findings**:
- Feature specification readiness requires clear user stories, functional requirements, and measurable acceptance criteria — all describing *what* the system does.
- Implementation details belong in the development phase (code quality, infrastructure, security), not in the specification.
- Quality gates verify readiness by checking requirement sign-offs and testable outcomes, not internal architecture.

---

## 2. FR-by-FR Analysis: What vs. How

### FR-001 — Color-coded overlays with accessibility patterns
> "System MUST render color-coded overlays on tilled soil tiles based on health values (Poor, Moderate, Healthy categories), with pattern overlays for accessibility (stripes for poor, dots for moderate, solid for healthy). Pattern opacity MUST remain at minimum 0.7..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — describes user-visible visual output |
| Developer freedom | Two developers could implement rendering differently (XNA SpriteBatch, Game HUD API, shader-based) and both satisfy this |
| Implementation leak | **None** |

**Verdict**: Clean. No leakage.

---

### FR-002 — Hover tooltips with cursor mapping
> "Cursor-to-tile mapping MUST use SMAPI's cursor position API which provides tile coordinates directly, avoiding manual screen-to-tile conversion."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Mostly WHAT, with HOW leakage** |
| Developer freedom | The first sentence is behavioral (show tooltips). The second prescribes a specific API. The third ("avoiding manual screen-to-tile conversion") explicitly forbids an implementation approach. |
| Implementation leak | **Mild** — "MUST use SMAPI's cursor position API" could be argued as a design constraint (framework requirement), but "avoiding manual screen-to-tile conversion" is a directive about internal code structure. A developer using a different valid approach (e.g., `Game1.currentLocation` tile lookup from screen coordinates) would be non-compliant despite achieving identical behavior. |

**Verdict**: Mild leakage. The behavioral requirement is clear, but the implementation path is over-specified.

---

### FR-003 — Hoe feedback flash and floating text
> "System MUST show flash effect (colored overlay flash at the tile's health category color with alpha decreasing from 1.0 to 0.0 over 300ms) and floating text when a hoe is used on tilled soil tiles"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. WHAT | **WHAT with precise behavioral specification** |
| Developer freedom | The alpha decrease from 1.0→0.0 over 300ms is a user-visible animation curve. Any implementation producing this visual result is compliant. |
| Implementation leak | **None** — this describes the observable visual behavior, not the rendering mechanism. |

**Verdict**: Clean. Precise behavioral specification is not implementation leakage.

---

### FR-004 — Configuration persistence
> "System MUST persist visualization configuration (feature toggles, custom colors, opacity) via JSON file"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation detail** |
| Developer freedom | "via JSON file" narrows the persistence format. A developer using XML, binary, or SMAPI's built-in `SaveData` system would be non-compliant. |
| Implementation leak | **Mild** — the format is prescribed. For a mod spec where SMAPI's `IModDataService` is the standard, this may be acceptable as a design constraint, but strictly speaking the WHAT is "persist configuration" and the HOW is "JSON file." |

**Verdict**: Mild leakage. The format choice is an implementation decision.

---

### FR-005 — Configuration loading and hot-reload
> "System MUST load configuration on mod initialization... System MUST support hot-reload: detecting configuration file changes and applying them immediately (within the next frame)... See FR-019 for atomic application mechanism."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with cross-reference to HOW** |
| Developer freedom | The behavioral requirements (load on init, hot-reload, apply within next frame) are clear. The cross-reference to FR-019 points to implementation details. |
| Implementation leak | **Indirect** — this FR itself is mostly behavioral, but it funnels the reader to FR-019 which contains implementation prescriptions. |

**Verdict**: Mild indirect leakage via cross-reference.

---

### FR-006 — Graceful config fallback
> "System MUST fall back to default values only for invalid configuration entries while preserving valid ones... Configuration deserialization MUST be forward-compatible: missing fields receive defaults, unknown fields are ignored..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — describes behavioral contract |
| Developer freedom | Any implementation achieving this fallback behavior is compliant. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-007 — Independent feature toggles
> "System MUST support enabling/disabling overlay rendering, tooltip rendering, and hoe feedback independently from each other"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | Behavioral requirement only. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-008 — Color interpolation
> "System MUST interpolate colors using linear RGB interpolation for health values between category boundaries"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with algorithm prescription** |
| Developer freedom | "Linear RGB interpolation" prescribes a specific algorithm. A developer using HSL interpolation, CIELAB interpolation, or cubic easing would be non-compliant despite producing visually similar (or arguably better) results. |
| Implementation leak | **Mild** — the interpolation algorithm is an implementation choice. The WHAT is "colors transition smoothly between categories"; the HOW is "linear RGB interpolation." |

**Verdict**: Mild leakage. The specific algorithm is prescribed.

---

### FR-009 — Viewport culling and graceful degradation
> "System MUST perform viewport culling to only render overlays for visible tiles, with graceful degradation when visible tile count strictly exceeds 1,000 (>1,000) to maintain 60 FPS. Graceful degradation MUST disable accessibility pattern rendering..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Mostly WHAT** |
| Developer freedom | The behavioral requirements are clear: cull to visible tiles, degrade patterns at >1,000 tiles, maintain 60 FPS. The specific threshold (1,000) is a measurable performance boundary. |
| Implementation leak | **Minimal** — "viewport culling" is a technique but also a user-visible behavior (only visible tiles render). The degradation rules are behavioral. The WCAG reference is a valid design constraint. |

**Verdict**: Acceptable. The technical terms describe observable behavior.

---

### FR-010 — Health value caching
> "System MUST cache computed health values to avoid redundant calculations, invalidating the cache on game state changes (tile modification events, save load, viewport resize)."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation concept** |
| Developer freedom | "Cache" is an internal mechanism. A developer using memoization, pre-computation, or a different caching strategy might technically violate the letter while achieving the spirit. |
| Implementation leak | **Mild** — "cache" and "invalidating the cache" are implementation concepts. The WHAT is "health values must be efficiently retrievable and update promptly on game state changes." |

**Verdict**: Mild leakage. The word "cache" presumes an internal mechanism.

---

### FR-011 — Targeted hoe feedback
> "System MUST provide hoe action feedback only on the tile targeted by the player's cursor."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | Behavioral requirement only. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-012 — Event handler lifecycle
> "System MUST register all event handlers on mod initialization and unregister all event handlers on mod disposal to prevent memory leaks."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with framework concept** |
| Developer freedom | "Event handlers" is a SMAPI framework concept, but for a SMAPI mod this is the standard lifecycle pattern. |
| Implementation leak | **Minimal** — for a mod spec, referencing the framework's event system is a legitimate design constraint. |

**Verdict**: Acceptable for mod context.

---

### FR-013 — Concurrency safety
> "I/O-bound operations (config file load, save/load data persistence): MUST use async/await patterns per Constitution Principle III — no `.Result` or `.Wait()` blocking. Game-loop-thread coordination (render state snapshots, atomic config swaps, pause/resume flags): MUST use lock-free or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines) on the game loop thread."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Significant HOW** |
| Developer freedom | Severely restricted. The FR prescribes: (1) specific async/await pattern, (2) forbids specific APIs (`.Result`, `.Wait()`), (3) mandates specific synchronization primitives (`Interlocked`, `Volatile.Read`), (4) mandates "atomic flag-based state machines," (5) references internal "Constitution Principle III." |
| Implementation leak | **MAJOR** — This FR reads like a design document. It dictates: specific language features (async/await), specific APIs to avoid (`.Result`, `.Wait()`), specific synchronization primitives (`Interlocked`, `Volatile.Read`), specific patterns (atomic flag-based state machines), and references an internal project document. Two developers could handle concurrency safely using different primitives (e.g., `SemaphoreSlim`, `ReaderWriterLockSlim`, `Monitor`, `Channel<T>`) and all would be non-compliant. |

**Verdict**: Major implementation leakage. The WHAT is "the system must handle concurrency safely without deadlocks, race conditions, or mixed-state frames." The HOW is the entire second paragraph.

---

### FR-014 — Neutral gray overlay for unavailable data
> "System MUST render a neutral gray overlay for tiles where soil health data is unavailable or hasn't loaded yet."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | Behavioral requirement only. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-015 — Tooltip throttling
> "System MUST throttle tooltip updates to a minimum 50ms interval during cursor movement, suppressing successive tooltip redraws that occur within the throttle window to prevent tooltip flickering. Tooltip MUST update immediately when the cursor first enters a new tile (leading-edge), then suppress further updates until 50ms has elapsed since the last rendered update."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation concept** |
| Developer freedom | The 50ms interval and "leading-edge" behavior are verifiable. However, "leading-edge" vs "trailing-edge" is a throttling implementation concept. |
| Implementation leak | **Mild** — "leading-edge" is a specific throttling pattern. The WHAT is "tooltip updates are rate-limited to 50ms with immediate first update." |

**Verdict**: Mild leakage. The "leading-edge" terminology presumes a specific throttling implementation.

---

### FR-016 — Viewport resize recalculation
> "System MUST recalculate visible tile overlays when the viewport resizes, updating the overlay list cache within a single frame."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation concept** |
| Developer freedom | "overlay list cache" presumes a cached data structure. |
| Implementation leak | **Mild** — "cache" is an implementation concept. |

**Verdict**: Mild leakage.

---

### FR-017 — Pause rendering during save/load
> "System MUST pause overlay rendering during save/load operations and resume with refreshed data once the operation completes, ensuring visualization state consistency."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | Behavioral requirement only. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-018 — Viewport edge tile rendering
> "Visible tile range MUST be calculated from `Game1.viewport` pixel bounds divided by `Game1.tileSize` (64), then extended by ±1 tile and clamped to map bounds (`Map.DisplayWidth/Height / tileSize`)."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **MAJOR HOW** |
| Developer freedom | Severely restricted. The FR prescribes: (1) specific game API calls (`Game1.viewport`, `Game1.tileSize`, `Map.DisplayWidth/Height`), (2) specific magic number (64), (3) specific math operations (division, ±1 extension), (4) specific clamping logic. A developer using `Game1.currentLocation.Map.Layers[0].LayerWidth/Height` or a different margin strategy would be non-compliant. |
| Implementation leak | **MAJOR** — This is a precise implementation recipe, not a behavioral requirement. The WHAT is "render partial overlays at viewport edges with a small margin to prevent pop-in." The HOW is the entire calculation formula. |

**Verdict**: Major implementation leakage. The spec dictates exact API usage, math, and clamping logic.

---

### FR-019 — Atomic config application
> "System MUST apply configuration changes atomically during the next render frame, avoiding partial-state rendering where some tiles use old config and others use new config."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation concept** |
| Developer freedom | "Atomically" is an implementation concept, but the behavioral outcome ("no mixed-state frames") is clear and verifiable. |
| Implementation leak | **Borderline** — "atomically" implies a mechanism, but the behavioral contract is unambiguous. |

**Verdict**: Acceptable. The behavioral outcome is well-defined.

---

### FR-020 — Frame consistency during mid-render changes
> "System MUST maintain frame consistency during mid-render health value changes by completing the current frame with the previous state and applying changes on the next frame."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with mechanism description** |
| Developer freedom | The mechanism is described ("completing current frame with previous state"), but this IS the behavioral contract for frame consistency. |
| Implementation leak | **Borderline** — the mechanism described is also the observable behavior guarantee. |

**Verdict**: Acceptable. The mechanism IS the behavioral contract here.

---

### FR-021 — Zero-tilled-tiles case
> "System MUST handle the zero-tilled-tiles case as a no-op with minimal resource usage (no overlay draw calls, no cache allocation) and no errors thrown."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation-level constraints** |
| Developer freedom | "No overlay draw calls, no cache allocation" prescribes internal behavior. A developer using a different rendering approach (e.g., instanced draw calls) might technically violate "no overlay draw calls" while still achieving zero resource usage. |
| Implementation leak | **Mild** — "no overlay draw calls, no cache allocation" are implementation-level constraints. The WHAT is "zero resource usage and no errors when no tiles are tilled." |

**Verdict**: Mild leakage. Internal resource constraints are specified.

---

## 3. Assumptions Section Analysis

| Assumption | Type | Leakage |
|------------|------|---------|
| "Soil health values are already persisted and accessible from the existing save system" | Behavioral | None — describes data availability |
| "Tilled soil tiles can be identified through existing game state queries" | Behavioral | None — describes capability |
| "Players understand color-coding conventions" | UX assumption | None |
| "The existing `ModConstants` structure will be extended with visualization default values" | **Implementation** | **Yes** — references internal class `ModConstants` and assumes its extensibility |
| "MonoGame/XNA `SpriteBatch` rendering is available for overlay drawing" | **Implementation** | **Yes** — assumes specific rendering technology (`SpriteBatch`) |
| "The existing `IModHelper.Events` system provides the necessary game loop events" | **Implementation** | **Yes** — assumes specific framework API (`IModHelper.Events`) |
| "Configuration file lives in the mod's directory alongside existing mod files" | **Implementation** | **Yes** — assumes specific file system layout |
| "Single-player game context only" | Design constraint | None — valid boundary |

**Summary**: 4 of 8 assumptions reference internal implementation details (`ModConstants`, `SpriteBatch`, `IModHelper.Events`, file system layout). These are implementation confirmations, not behavioral assumptions.

---

## 4. Dependencies Section Analysis

| Dependency | Type | Leakage |
|------------|------|---------|
| "Requires soil health save/load system (US-01-01, Issue #22) to be functional" | External feature dependency | None — clear cross-reference |
| "Requires existing `ModController` event registration infrastructure" | **Implementation** | **Yes** — references internal class `ModController` |
| "Requires existing `ModEntry` composition root pattern for DI wiring" | **Implementation** | **Yes** — references internal class `ModEntry` and pattern |
| "Relies on `IModDataService` for JSON configuration persistence" | **Implementation** | **Yes** — references internal interface `IModDataService` |

**Summary**: 3 of 4 dependencies reference internal implementation classes/interfaces. These describe the internal architecture, not external behavioral dependencies.

---

## 5. Success Criteria Analysis

| SC | Assessment |
|----|------------|
| SC-001 (overlay visible within 1s) | Clean — measurable behavioral outcome |
| SC-002 (60 FPS with 1,000 tiles) | Clean — measurable performance target |
| SC-003 (config changes ≤16.67ms) | Clean — measurable timing constraint |
| SC-004 (tooltip within 100ms) | Clean — measurable timing constraint |
| SC-005 (correct tile 100%) | Clean — measurable accuracy target |
| SC-006 (zero event handler leaks) | **Mild leak** — "event handler leaks" is an implementation concept, but it's verifiable through framework inspection |
| SC-007 (values within 0-100) | Clean — measurable data integrity |

**Summary**: SC-006 contains a minor implementation concept but is verifiable. Overall, success criteria are well-written as measurable outcomes.

---

## 6. Key Examples of Implementation Leakage

### Example 1: FR-013 — Concurrency Primitives Prescription
```
I/O-bound operations ... MUST use async/await patterns per Constitution Principle III — 
no `.Result` or `.Wait()` blocking. Game-loop-thread coordination ... MUST use lock-free 
or atomic synchronization (`Interlocked`, `Volatile.Read`, atomic flag-based state machines)
```
**Problem**: Prescribes specific C# language features, forbids specific APIs, mandates specific synchronization primitives, and references an internal project document. The WHAT is "handle concurrency safely."

### Example 2: FR-018 — Exact API Recipe
```
Visible tile range MUST be calculated from `Game1.viewport` pixel bounds divided by 
`Game1.tileSize` (64), then extended by ±1 tile and clamped to map bounds 
(`Map.DisplayWidth/Height / tileSize`)
```
**Problem**: Dictates exact game API calls, magic number (64), math operations, and clamping logic. The WHAT is "render partial overlays at viewport edges with margin."

### Example 3: Assumptions — Internal Class References
```
The existing `ModConstants` structure will be extended...
MonoGame/XNA `SpriteBatch` rendering is available...
The existing `IModHelper.Events` system provides...
```
**Problem**: Assumes specific internal classes and framework APIs rather than describing behavioral capabilities.

### Example 4: Dependencies — Internal Architecture
```
Requires existing `ModController` event registration infrastructure
Requires existing `ModEntry` composition root pattern for DI wiring
Relies on `IModDataService` for JSON configuration persistence
```
**Problem**: Dependencies describe internal classes and patterns, not external behavioral contracts.

---

## 7. Contextual Consideration: Mod Spec vs. General SRS

For a **Stardew Valley mod specification**, some technical detail is unavoidable and acceptable:

- The mod operates within the SMAPI framework, which has specific APIs and patterns.
- The mod extends an existing codebase with established architecture (DDD, `ModController`, `ModEntry`, etc.).
- Performance constraints (60 FPS, frame timing) are user-visible and must be specified precisely.
- The "two competent developers" test is somewhat muted when both developers work in the same codebase, same framework, and same language.

However, even within this context, there is a meaningful distinction between:
- **Legitimate design constraints**: "Must use SMAPI's event system" (framework requirement), "Must maintain 60 FPS" (user-visible performance)
- **Implementation prescriptions**: "Must use `Interlocked`" (specific primitive), "Must calculate from `Game1.viewport / 64`" (specific API recipe)

The spec crosses this line in FR-013 and FR-018 most prominently.

---

## 8. Final Verdict

### Should Item 17 be [x] or [ ]?

**Verdict: [ ] — Item 17 should remain UNCHECKED.**

### Rationale

The spec contains **significant implementation leakage** in at least two functional requirements (FR-013, FR-018) and **moderate leakage** in the Assumptions and Dependencies sections. While many FRs are well-written as behavioral requirements (FR-001, FR-006, FR-007, FR-011, FR-014, FR-017), the presence of implementation prescriptions in the spec means it does not fully meet the "no implementation details leak" criterion.

### Leakage Summary

| Category | Count |
|----------|-------|
| Major leakage | 2 FRs (FR-013, FR-018) |
| Mild leakage | 5 FRs (FR-002, FR-004, FR-008, FR-010, FR-015, FR-016, FR-021) |
| Indirect leakage | 1 FR (FR-005 via cross-reference) |
| Clean WHAT | 13 FRs |
| Assumptions with implementation | 4 of 8 |
| Dependencies with implementation | 3 of 4 |

### What Would Make It [x]?

To pass this criterion, the following would need revision:

1. **FR-013**: Replace implementation prescriptions with behavioral outcomes. Instead of "MUST use `Interlocked`," write "System MUST handle concurrent access without deadlocks, race conditions, or mixed-state frames. Verification: trigger each concurrent scenario 100 times and confirm no deadlocks, no mixed-state frames, and no UI freezes."
2. **FR-018**: Replace the API recipe with behavioral specification. Instead of prescribing `Game1.viewport / 64`, write "System MUST render partial overlays for tiles at viewport edges with a 1-tile margin beyond visible bounds to prevent pop-in artifacts."
3. **Assumptions**: Replace internal class references with behavioral capabilities. Instead of "`SpriteBatch` is available," write "The game provides a 2D rendering API capable of drawing textured quads at tile positions."
4. **Dependencies**: Replace internal class references with capability descriptions. Instead of "`ModController` event registration infrastructure," write "The mod provides a game event subscription system for update tick, cursor change, and input events."

---

## 9. Recommendations

### Priority 1 — Major (should fix)
1. **Rewrite FR-013** to describe concurrency safety as behavioral outcomes, removing specific primitive mandates and the "Constitution Principle III" reference.
2. **Rewrite FR-018** to describe viewport edge rendering behaviorally, removing the `Game1.viewport / 64` recipe.
3. **Rewrite Dependencies** to describe external capabilities rather than internal class names.

### Priority 2 — Minor (should fix)
4. **Rewrite Assumptions** to describe behavioral capabilities rather than internal structures.
5. **FR-002**: Remove "avoiding manual screen-to-tile conversion" — the behavioral requirement is sufficient.
6. **FR-004**: Remove "via JSON file" or reframe as a design constraint.
7. **FR-008**: Remove "linear RGB interpolation" — specify the visual outcome instead.
8. **FR-010**: Replace "cache" with behavioral language ("efficiently retrievable").
9. **FR-015**: Remove "leading-edge" — the 50ms interval and immediate first update are sufficient.
10. **FR-021**: Replace "no overlay draw calls, no cache allocation" with "minimal resource usage."

### Priority 3 — Optional (nice to have)
11. Add a "Design Constraints" section to explicitly separate framework-mandated constraints from behavioral requirements.
12. Consider whether FR-009's detailed degradation notification behavior belongs in the spec or in a design document.

---

## 10. Sources

1. [Specification vs. Design Document vs. Implementation Details — Software Engineering Stack Exchange](https://softwareengineering.stackexchange.com/questions/445012/specification-vs-design-document-implementation-details) (2025-08-15)
2. [Preventing Implementation Leaks in Technical Documentation — mmapped.blog](https://mmapped.blog/posts/implementation-details-leak-architecture.html) (2025-11-02)
3. [Architecture Boundaries: Decoupling Design from Infrastructure — milanjovanovic.tech](https://milanjovanovic.tech/blog/architecture-boundaries-decoupling-design-from-infrastructure) (2026-01-20)
4. [Spec-Driven Development in the Era of AI-Assisted Coding — allegro.tech](https://allegro.tech/blog/spec-driven-development-ai-era) (2025-09-10)
5. [How to Write Software Design Documents vs Functional Specifications — Atlassian Engineering](https://www.atlassian.com/blog/technology/software-design-document-vs-functional-spec) (2025-05-14)
6. [Software Requirements Specification: Design Constraints vs Implementation Details — University of Toronto / IEEE](https://www.cs.toronto.edu/~jm/2108S/SRS.pdf) (2023-01-01)
7. [Requirements vs Design vs Implementation Prescriptions — Software Engineering Stack Exchange](https://softwareengineering.stackexchange.com/questions/requirements-vs-implementation-prescriptions) (2022-05-15)
8. [Writing High-Quality Software Requirements Specifications — Jama Software](https://www.jamasoftware.com/requirements-management-guide/writing-requirements/srs-quality-attributes) (2023-08-10)
9. [Product Specification & PRD Readiness Checklist — Docsie](https://www.docsie.io) (2025-09-05)
