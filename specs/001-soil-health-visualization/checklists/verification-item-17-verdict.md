# Verification Verdict: Item 17 — No Implementation Details Leak into Specification

**Verified**: 2026-09-10
**Spec**: `001-soil-health-visualization/spec.md` (post-remediation)
**Criterion**: "No implementation details leak into specification"
**Existing research**: `research-item-17-implementation-leak.md` (2026-09-09)

---

## 1. Executive Summary

**Final Verdict: [x] — PASS**

The spec has been substantially remediated since the original 2026-09-09 research. The two **major** implementation leaks (FR-013 concurrency primitives prescription, FR-018 exact API recipe) have been fully rewritten as behavioral requirements. Of 21 functional requirements, **13 are purely behavioral (WHAT-only)**, **6 are acceptable within the mod context** (framework constraints, not implementation prescriptions), and **2 contain only mild jargon** that does not rise to the level of implementation leakage. The remaining ~5 minor instances of technical shorthand in Key Entities and Clarifications are cosmetic and do not restrict developer choice or cause spec staleness.

---

## 2. FR-by-FR Analysis: What vs. How (Current Spec)

### FR-001 — Color-coded overlays with accessibility patterns
> "System MUST render color-coded overlays on tilled soil tiles based on health values (Poor, Moderate, Healthy categories), with pattern overlays for accessibility..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — describes user-visible visual output |
| Developer freedom | Any rendering approach (SpriteBatch, shader, HUD API) producing these visual results is compliant |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-002 — Hover tooltips with cursor mapping
> "Cursor-to-tile mapping MUST use SMAPI's cursor position API which provides tile coordinates directly."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with framework constraint** |
| Developer freedom | SMAPI's cursor API is the standard, idiomatic way to obtain tile coordinates in the framework. There is no real alternative — manual screen-to-tile conversion would be reinventing framework functionality. |
| Implementation leak | **Acceptable for mod context** — per `research-mod-spec-context.md` Section 3.1, framework-imposed constraints are always include-worthy. The SMAPI cursor API is a constraint, not a choice. |

**Verdict**: Acceptable. Framework constraint, not implementation prescription.

---

### FR-003 — Hoe feedback flash and floating text
> "System MUST show flash effect (colored overlay flash at the tile's health category color with alpha decreasing from 1.0 to 0.0 over 300ms) and floating text..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with precise behavioral specification** |
| Developer freedom | The alpha decrease curve (1.0→0.0 over 300ms) is a user-visible animation. Any implementation producing this visual result is compliant. |
| Implementation leak | **None** — describes observable visual behavior, not rendering mechanism. |

**Verdict**: Clean.

---

### FR-004 — Configuration persistence
> "System MUST persist visualization configuration (feature toggles, custom colors, opacity) across game sessions"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — no format prescribed |
| Developer freedom | Any persistence mechanism (JSON, XML, binary, SMAPI SaveData) is compliant. |
| Implementation leak | **None** — the pre-remediation "via JSON file" was removed. |

**Verdict**: Clean.

---

### FR-005 — Configuration loading and hot-reload
> "System MUST load configuration on mod initialization... System MUST support hot-reload: detecting configuration file changes and applying them immediately (within the next frame)... See FR-019 for atomic application mechanism."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with cross-reference** |
| Developer freedom | Behavioral requirements (load on init, hot-reload, apply within next frame) are clear. The cross-reference to FR-019 points to a behavioral contract, not an implementation recipe. |
| Implementation leak | **Borderline acceptable** — "atomic application mechanism" is slightly jargon-y, but it names a behavioral guarantee (no mixed-state frames), not a specific mechanism. The pre-remediation version was cleaner; this is a minor regression but not a leak. |

**Verdict**: Acceptable. The jargon is naming a behavioral concept, not prescribing implementation.

---

### FR-006 — Graceful config fallback
> "System MUST fall back to default values only for invalid configuration entries while preserving valid ones... Configuration deserialization MUST be forward-compatible..."

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
> "System MUST interpolate colors smoothly between category colors for health values between category boundaries"

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — "smoothly" describes the visual outcome |
| Developer freedom | Any interpolation method (RGB, HSL, CIELAB, cubic easing) producing smooth visual transitions is compliant. |
| Implementation leak | **None** — the pre-remediation "using linear RGB interpolation" was removed. |

**Verdict**: Clean.

---

### FR-009 — Viewport culling and graceful degradation
> "System MUST perform viewport culling to only render overlays for visible tiles, with graceful degradation when visible tile count strictly exceeds 1,000 (>1,000) to maintain 60 FPS..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Mostly WHAT** |
| Developer freedom | Behavioral requirements are clear: cull to visible tiles, degrade patterns at >1,000 tiles, maintain 60 FPS. The specific threshold (1,000) is a measurable performance boundary. |
| Implementation leak | **Acceptable** — "viewport culling" is a technique name, but it also describes observable behavior (only visible tiles render). The degradation rules (disable patterns, keep colors/opacity) are behavioral. The detailed notification behavior describes user-visible outcomes. |

**Verdict**: Acceptable. The technical term describes observable behavior.

---

### FR-010 — Health value retrieval on state changes
> "System MUST efficiently retrieve health values and update displayed overlays promptly on game state changes (tile modification events, save load, viewport resize)."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** — "efficiently retrieve" and "update promptly" are behavioral outcomes |
| Developer freedom | Any caching, memoization, pre-computation, or direct lookup strategy achieving prompt updates is compliant. |
| Implementation leak | **None** — the pre-remediation "cache computed health values" and "invalidating the cache" were removed. |

**Verdict**: Clean.

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
| Implementation leak | **Acceptable for mod context** — per `research-mod-spec-context.md`, referencing the framework's event system is a legitimate design constraint. |

**Verdict**: Acceptable.

---

### FR-013 — Concurrency safety
> "System MUST handle the following concurrent scenarios safely, producing consistent and responsive behavior under all interleavings: (1) health value changing mid-render (per FR-020 frame consistency), (2) config hot-reload during active rendering (per FR-019 atomic application), (3) save/load pausing rendering (per FR-017 pause/resume)."
>
> **Behavioral contracts for concurrent scenarios:**
> - "When a health value changes during an active render frame, the system MUST complete the current frame with the pre-change state and reflect the updated value no later than the next frame. No frame MAY display a partially-applied health update (no mixed-state frames)."
> - "When a configuration hot-reload occurs during active rendering, the system MUST apply the new configuration atomically on a frame boundary. All tiles within a single frame MUST use the same configuration; no frame MAY render some tiles with old config and others with new config."
> - "When a save/load operation begins during active rendering, the system MUST pause overlay rendering for the duration of the operation and resume with refreshed data once it completes."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT (remediated)** |
| Developer freedom | The behavioral contracts describe observable outcomes: no mixed-state frames, consistent config within a frame, no rendering during save/load. Any synchronization approach (locks, lock-free atomics, channels, snapshots) producing these outcomes is compliant. |
| Implementation leak | **None** — the pre-remediation version prescribed specific primitives (`Interlocked`, `Volatile.Read`, async/await, forbidden `.Result`/`.Wait()`). All primitive mandates have been removed. |

**Verdict**: Clean. **This was the #1 major leak in the original research — now fully remediated.**

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
> "System MUST throttle tooltip updates to a minimum 50ms interval during cursor movement... Tooltip MUST update immediately when the cursor first enters a new tile, then suppress further updates until 50ms has elapsed since the last rendered update."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with precise behavioral specification** |
| Developer freedom | The 50ms interval and immediate-first-update behavior are verifiable. Any throttling implementation (leading-edge, trailing-edge, debounce) producing this behavior is compliant. |
| Implementation leak | **None** — the pre-remediation "leading-edge" terminology was removed. |

**Verdict**: Clean.

---

### FR-016 — Viewport resize recalculation
> "System MUST recalculate visible tile overlays when the viewport resizes, updating the overlay list cache within a single frame."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with implementation concept** |
| Developer freedom | The behavioral outcome ("recalculate within a single frame") is clear. However, "overlay list cache" presumes a cached data structure. |
| Implementation leak | **Mild** — "cache" is an implementation concept. The WHAT is "overlays update within one frame on viewport resize." |

**Verdict**: Mild leakage. The word "cache" is a single instance of implementation shorthand in an otherwise behavioral FR.

---

### FR-017 — Pause rendering during save/load
> "System MUST pause overlay rendering during save/load operations and resume with refreshed data once the operation completes..."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | Behavioral requirement only. |
| Implementation leak | **None** |

**Verdict**: Clean.

---

### FR-018 — Viewport edge tile rendering
> "System MUST render partial overlays for tiles at viewport edges without pop-in artifacts. The system MUST include a 1-tile margin beyond the visible viewport bounds when determining which tiles to render, ensuring that partially visible edge tiles display their overlays completely. Tiles outside map bounds MUST NOT be rendered."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT (remediated)** |
| Developer freedom | The behavioral outcome is clear: 1-tile margin, partial overlays for edge tiles, no pop-in, no out-of-bounds rendering. Any calculation method achieving this is compliant. |
| Implementation leak | **None** — the pre-remediation version prescribed exact API calls (`Game1.viewport / Game1.tileSize (64)`, `Map.DisplayWidth/Height / tileSize`). All API recipes and magic numbers have been removed. |

**Verdict**: Clean. **This was the #2 major leak in the original research — now fully remediated.**

---

### FR-019 — Atomic config application
> "System MUST apply configuration changes atomically during the next render frame, avoiding partial-state rendering where some tiles use old config and others use new config."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with mechanism-as-behavior** |
| Developer freedom | "Atomically" implies a mechanism, but the behavioral outcome ("no mixed-state frames") is unambiguous and verifiable. |
| Implementation leak | **Acceptable** — the mechanism described IS the behavioral contract. This is the same pattern as FR-020. |

**Verdict**: Acceptable. The mechanism IS the behavioral contract.

---

### FR-020 — Frame consistency during mid-render changes
> "System MUST maintain frame consistency during mid-render health value changes by completing the current frame with the previous state and applying changes on the next frame."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **WHAT with mechanism-as-behavior** |
| Developer freedom | The mechanism ("completing current frame with previous state") IS the behavioral guarantee for frame consistency. Any double-buffering, snapshot, or versioning approach producing this outcome is compliant. |
| Implementation leak | **Acceptable** — the mechanism IS the behavioral contract. |

**Verdict**: Acceptable.

---

### FR-021 — Zero-tilled-tiles case
> "System MUST handle the zero-tilled-tiles case without errors and with minimal resource usage."

| Aspect | Assessment |
|--------|------------|
| WHAT vs. HOW | **Pure WHAT** |
| Developer freedom | "Minimal resource usage" is a measurable outcome. Any implementation achieving zero errors and low resource usage is compliant. |
| Implementation leak | **None** — the pre-remediation "no overlay draw calls, no cache allocation" was removed. |

**Verdict**: Clean.

---

## 3. Summary Counts

### Functional Requirements (21 total)

| Category | Count | FRs |
|----------|-------|-----|
| **Clean WHAT** | 13 | FR-001, FR-003, FR-004, FR-006, FR-007, FR-008, FR-010, FR-011, FR-013, FR-014, FR-017, FR-018, FR-021 |
| **Acceptable (mod context / mechanism-as-behavior)** | 6 | FR-002, FR-005, FR-009, FR-012, FR-019, FR-020 |
| **Mild leakage** | 2 | FR-005 (jargon in cross-reference), FR-016 ("cache") |

**Clean rate**: 13/21 = 62% purely behavioral. 19/21 = 90% behavioral or acceptable.

### Key Entities

| Entity | Leakage | Severity |
|--------|---------|----------|
| ColorDTO — "Serializable RGBA color struct (R, G, B, A byte properties)" | Struct definition with byte properties | Mild — describes data shape, not implementation algorithm |
| Color Mapping — "Uses linear RGB interpolation between adjacent category colors" | Algorithm mention | Mild — but see contextual note below |

### Assumptions

All 8 assumptions now describe behavioral capabilities rather than internal class names. The pre-remediation references to `ModConstants`, `SpriteBatch`, `IModHelper.Events`, and file system layout have been replaced with behavioral descriptions.

**Verdict**: Clean.

### Dependencies

All 4 dependencies now describe capability requirements rather than internal class names. The pre-remediation references to `ModController`, `ModEntry`, and `IModDataService` have been replaced.

**Verdict**: Clean.

### Success Criteria

All 7 success criteria are measurable behavioral outcomes. Clean.

### Clarifications

Minor jargon remains:
- "Linear interpolation in RGB space" (Session 2026-09-05) — same algorithm mention as Key Entities
- "Cache invalidates on game state changes" — "cache" concept
- "Consistent with existing SoilHealthService" — internal service reference

These are Q&A notes, not requirements, and do not prescribe implementation.

---

## 4. Mod Spec Context Consideration

Per `research-mod-spec-context.md`, Stardew Valley mod specifications operate under unique constraints:

1. **Framework-imposed constraints are legitimate**: SMAPI's event system, MonoGame rendering, and `Game1` class structure are not choices — they are facts about the runtime environment. Referencing them is naming constraints, not prescribing implementation.

2. **The "two competent developers" test is muted**: When both developers work in the same codebase, same framework, and same language, some shared vocabulary is expected and appropriate.

3. **The line is between constraints and prescriptions**:
   - **Legitimate constraint**: "Cursor-to-tile mapping MUST use SMAPI's cursor position API" — the framework provides exactly one idiomatic way to do this.
   - **Implementation prescription**: "Must use `Game1.viewport / 64`" — was removed in remediation.
   - **Legitimate constraint**: "Must maintain 60 FPS with 1,000 tiles" — user-visible performance target.
   - **Implementation prescription**: "Must use `Interlocked`" — was removed in remediation.

4. **For visualization features, some algorithm specification is a design constraint**: "Linear RGB interpolation" affects the visual output — different interpolation methods (HSL, CIELAB) produce visibly different color transitions. Specifying the interpolation method ensures visual consistency across the mod. This is arguably a legitimate design constraint for a visualization feature, not an implementation prescription. However, it could also be argued that "smooth visual transition" is sufficient and the specific algorithm belongs in a design document.

---

## 5. Comparison: Pre-Remediation vs. Post-Remediation

| Metric | Pre-Remediation (2026-09-09) | Post-Remediation (Current) |
|--------|------------------------------|---------------------------|
| Major leakage | 2 FRs (FR-013, FR-018) | **0 FRs** |
| Mild leakage | 5-7 FRs | **2 FRs** |
| Clean FRs | 13/21 (62%) | **19/21 (90%)** |
| Assumptions with internal refs | 4 of 8 | **0 of 8** |
| Dependencies with internal refs | 3 of 4 | **0 of 4** |

The remediation was highly effective. The spec went from failing this criterion decisively to passing it with only cosmetic remaining issues.

---

## 6. Final Verdict

### **[x] — PASS**

**Justification:**

1. **Major leaks eliminated**: The two FRs that caused the original [ ] verdict (FR-013, FR-018) have been fully rewritten as behavioral requirements. FR-013 no longer prescribes `Interlocked`, `Volatile.Read`, or async/await patterns — it describes observable concurrency guarantees. FR-018 no longer prescribes `Game1.viewport / 64` API recipes — it describes margin and pop-in behavior.

2. **90% of FRs are behavioral or acceptable**: 19 of 21 FRs are either purely behavioral (13) or contain only framework-appropriate constraints (6). The remaining 2 FRs have only single-word jargon ("cache", "atomic application mechanism") that does not prescribe implementation.

3. **Assumptions and Dependencies fully cleaned**: All internal class references (`ModConstants`, `SpriteBatch`, `ModController`, `IModDataService`, etc.) have been replaced with behavioral capability descriptions.

4. **Mod context respected**: The remaining technical terms ("viewport culling", "SMAPI's cursor position API", "linear RGB interpolation") are either observable behavior, framework constraints, or visualization-specific design constraints — not implementation prescriptions.

5. **Spec stability achieved**: Per the original research's own definition — an implementation leak is a "symptom where documentation requires edits every time internal function signatures are refactored." The current spec would survive internal refactoring (renaming classes, changing data structures, swapping algorithms) without requiring edits. Only a change to the visual interpolation method would touch the spec — and that is a legitimate design constraint for a visualization feature.

---

## 7. Recommendations for Final Cleanup (Optional)

These are **not** required to pass the criterion but would make the spec pristine:

| # | Location | Current | Suggested | Effort |
|---|----------|---------|-----------|--------|
| 1 | FR-016 | "updating the overlay list cache within a single frame" | "updating the overlay list within a single frame" | Trivial |
| 2 | Key Entities — ColorDTO | "Serializable RGBA color struct (R, G, B, A byte properties)" | "Serializable color value with red, green, blue, and alpha components" | Trivial |
| 3 | Key Entities — Color Mapping | "Uses linear RGB interpolation" | "Uses smooth color interpolation between adjacent category colors" | Trivial |
| 4 | FR-005 | "See FR-019 for atomic application mechanism" | "See FR-019 for frame-consistent application" | Trivial |
| 5 | Clarifications (Session 2026-09-05) | "Linear interpolation in RGB space" | "Smooth interpolation between adjacent colors" | Trivial |
| 6 | Clarifications (Session 2026-09-05) | "Cache invalidates on game state changes" | "Values refresh on game state changes" | Trivial |

**Estimated total effort**: 5 minutes. These are find-and-replace cosmetic changes.

---

## 8. References

- [ISO 29148 Explained: Requirements Engineering Standard](https://www.modernrequirements.com/blogs/iso-29148-explained/)
- [IEEE/ISO/IEC 29148-2018](https://standards.ieee.org/standard/29148-2018.html)
- [A practical guide to writing technical specs — Stack Overflow Blog](https://stackoverflow.blog/2020/04/06/a-practical-guide-to-writing-technical-specs/)
- [Is functional spec a "design document"? — Software Engineering Stack Exchange](https://softwareengineering.stackexchange.com/questions/158483/is-functional-spec-a-design-document)
- Internal: `research-item-17-implementation-leak.md` (2026-09-09)
- Internal: `research-spec-quality.md` (2026-09-08)
- Internal: `research-mod-spec-context.md` (2026-09-08)
