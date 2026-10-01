# Research Item 3: Written for Non-Technical Stakeholders

**Criterion**: Can a product owner or QA tester who doesn't code read the requirement and verify the implementation matches?

**Spec**: `001-soil-health-visualization/spec.md` (197 lines)

**Date**: 2026-09-09

---

## Search Results Summary

### Source 1: Modern Requirements — SRS Best Practices [\[1\]]
- "Use clear and concise language that everyone, including technical and non-technical team members, can understand."
- "Define measurable criteria. For example, instead of 'The system should be fast,' specify 'The system should respond within 2 seconds for 95% of requests.'"
- "Define key terminologies at the start to avoid ambiguity."
- Distinguishes between **Requirements** (what the system should do, audience: business stakeholders, product managers, end-users) and **Specifications** (how the system will implement, audience: developers, engineers, QA).

### Source 2: Jama Software — Characteristics of Excellent Requirements [\[2\]]
- Requirements should be "clear, unambiguous, and testable."
- The intended audience must be explicitly defined. For non-technical audiences, "explain the concept in context in a way that is meaningful for your audience."
- "It isn't enough to simply provide a textbook definition."

### Source 3: NNGroup — Dealing with Technical Jargon [\[3\]]
- Jargon is relative: "whether or not a given word counts as jargon depends on who is reading it."
- Decision framework: (1) How many readers will know this term? (2) How important is the term in your context?
- When most readers won't know a term: lead with plain-language alternative in format `[plain-language] ([technical])`.
- "AAA: Always Avoid Acronyms" — acronyms are "a particularly insidious form of professional jargon."

### Source 4: JMIR — Jargon and Readability in Plain Language Summaries [\[4\]]
- Study of 1,241 plain-language summaries found that "most... have poor readability due to their complexity and use of jargon."
- Only 21.7% had jargon levels suitable for general comprehension.
- Recommends: "avoid, wherever possible, jargon, abbreviations, and technical terms," "avoid complicated language or uncommon words."

---

## Section-by-Section Accessibility Analysis

### User Stories (lines 13-78) — **Mostly Accessible**

| Aspect | Rating | Evidence |
|--------|--------|----------|
| Story format | Good | Uses "As a player, I want to... so that..." — standard agile format any PO can read |
| Acceptance scenarios | Mostly Good | Given/When/Then format is clear and behavioral |
| Technical leakage | Moderate | Story 2 mentions "tile bounds"; Story 4 mentions "deserialization," "logs a warning" |
| Independent tests | Mixed | Story 1 test is behavioral; Story 4 test references "configuration values" abstractly |

**Verdict**: A non-technical PO can understand *what* each story wants. Some acceptance scenarios (Story 4, scenarios 4-5) leak implementation details ("deserialization," "logs a warning") but the intent is still discernible.

### Edge Cases (lines 82-90) — **Mixed**

| Edge Case | Accessible? | Why |
|-----------|-------------|-----|
| Health value changes mid-render | No | "mid-render" is rendering-pipeline jargon |
| Rapid mouse movement / tooltip flickering | Partially | "flickering" is understandable; "rapid mouse movement across many tiles" is behavioral |
| Viewport changes size | Partially | "viewport" may need definition for non-gamedev PO |
| Tiles at edge of viewport | Partially | Same "viewport" concern |
| Config saved during active rendering | No | "active rendering" is technical |
| Concurrent save/load with visualization state | No | "concurrent," "visualization state" are technical |
| Exact category boundaries | Yes | Behavioral and testable |

**Verdict**: 3/7 edge cases use technical rendering terminology. A non-technical PO would struggle to verify these without developer help.

### Clarifications (lines 92-115) — **Poor**

Heavy technical content throughout:
- "forward-compatible deserialization — missing fields get defaults, unknown fields ignored" (line 110)
- "Cache invalidates on game state changes (tile modification events, save load)" (line 105)
- "Linear interpolation in RGB space between adjacent category colors" (line 109)
- "Three specific scenarios already defined in the spec: (1) health value changing mid-render (FR-020), (2) config hot-reload during active rendering (FR-019), (3) save/load pausing rendering (FR-017)" (line 111)
- "Grid position (X, Y) alone — consistent with existing SoilHealthService and single-player scope" (line 114)

**Verdict**: These read like developer notes, not stakeholder clarifications. A non-technical PO would understand perhaps 30% of this section.

### Functional Requirements (lines 118-146) — **Not Accessible**

This is the most critical section for the stakeholder test. The FRs are the contract against which implementation is verified. Analysis:

| FR | Verifiable by Non-Technical PO? | Blocking Jargon |
|----|--------------------------------|-----------------|
| FR-001 | Partially | "Pattern opacity," "accessibility compliance" |
| FR-002 | No | "SMAPI's cursor position API," "manual screen-to-tile conversion" |
| FR-003 | Partially | "alpha decreasing from 1.0 to 0.0 over 300ms" |
| FR-004 | No | "JSON file" |
| FR-005 | No | "hot-reload," "atomic application mechanism" |
| FR-006 | No | "Configuration deserialization," "forward-compatible" |
| FR-007 | Yes | Feature toggles are behavioral |
| FR-008 | No | "linear RGB interpolation" |
| FR-009 | No | "viewport culling," "WCAG 4.1.3," "IMonitor," "accessibilityDegradation" |
| FR-010 | No | "cache," "game state changes" |
| FR-011 | Yes | Behavioral |
| FR-012 | No | "event handlers," "SMAPI event handlers," "memory leaks," "framework inspection" |
| FR-013 | No | "deadlocks," "race conditions," "async/await," "lock-free or atomic synchronization," "Interlocked," "Volatile.Read" |
| FR-014 | Yes | Behavioral |
| FR-015 | Partially | "throttle," "leading-edge" |
| FR-016 | Partially | "viewport resizes," "overlay list cache" |
| FR-017 | Partially | "save/load operations" |
| FR-018 | No | "clipping bounds," "Game1.viewport," "Game1.tileSize" |
| FR-019 | No | "atomically," "partial-state rendering" |
| FR-020 | No | "frame consistency," "mid-render" |
| FR-021 | No | "no-op," "draw calls," "cache allocation" |

**Verdict**: Only FR-007, FR-011, and FR-014 are fully verifiable by a non-technical person. The remaining 18 FRs require coding knowledge to verify. This is the strongest evidence that the spec fails the stakeholder test.

### Key Entities (lines 148-157) — **Mixed**

| Entity | Accessible? | Issue |
|--------|-------------|-------|
| Visualization Configuration | Partially | "feature toggles" OK; "accessibility degradation behavior" technical |
| Soil Health Tile | Partially | "SoilHealthService tile key scheme" is implementation-specific |
| Color Mapping | Partially | "half-open intervals" is math jargon |
| Tile Overlay | Yes | Behavioral description |
| Tooltip Data | Yes | Concrete example provided |
| Hoe Feedback | Yes | Concrete durations provided |
| PatternType | Yes | Values are self-explanatory |
| ColorDTO | No | "Serializable RGBA color struct (R, G, B, A byte properties)" is pure implementation |

**Verdict**: 4/8 entities are accessible. The other 4 reference internal code structure.

### Success Criteria (lines 159-169) — **Mixed**

| SC | Measurable by Non-Technical Means? | Issue |
|----|-----------------------------------|-------|
| SC-001 | Partially | "within 1 second" is measurable, but "viewport position update to first rendered overlay frame" is technical |
| SC-002 | Partially | "60 FPS" is a number, but measuring it requires technical tooling |
| SC-003 | No | "next render frame" is a technical concept |
| SC-004 | Yes | "100ms of hovering" is measurable with a stopwatch |
| SC-005 | Yes | "correct tile" is behavioral |
| SC-006 | No | "event handler leaks" requires code inspection |
| SC-007 | Yes | "0-100 range" is checkable |

**Verdict**: Only 3/7 success criteria can be verified without coding knowledge.

### Assumptions (lines 171-182) — **Not Accessible**

Every assumption references implementation details:
- "SoilHealthService implementation" (line 175)
- "Stardew Valley API documentation for tile state queries" (line 176)
- "ModConstants is a static class designed for extension" (line 178)
- "MonoGame/XNA SpriteBatch rendering" (line 179)
- "IModHelper.Events system" (line 180)
- "IModDataService uses per-save SMAPI data storage" (line 181)

**Verdict**: These are developer-to-developer assumptions. A non-technical PO cannot validate any of them.

### Dependencies (lines 184-189) — **Not Accessible**

- "ModController event registration infrastructure" (line 187)
- "ModEntry composition root pattern for DI wiring" (line 188)
- "IModDataService for JSON configuration persistence" (line 189)

**Verdict**: Pure implementation references. "DI wiring" (dependency injection) is architecture-level jargon.

---

## Technical Jargon Inventory

The following terms would confuse or block non-technical stakeholders:

### Platform/Framework Jargon
| Term | Context | Plain-Language Alternative |
|------|---------|--------------------------|
| SMAPI's cursor position API | FR-002 | "the game's built-in cursor tracking" |
| JSON file | FR-004 | "text-based settings file" |
| MonoGame/XNA SpriteBatch | Assumptions | "the game's built-in drawing system" |
| IModHelper.Events | Assumptions | "the game's event notification system" |
| IModDataService | Dependencies | "the mod's save data system" |
| IMonitor | FR-009 | "the mod's logging system" |
| Game1.viewport / Game1.tileSize | FR-018 | "the visible game area / tile size (64 pixels)" |

### Software Architecture Jargon
| Term | Context | Plain-Language Alternative |
|------|---------|--------------------------|
| Composition root pattern for DI wiring | Dependencies | "the mod's startup wiring system" |
| Event handlers / unregister | FR-012 | "event subscriptions / cleanup" |
| Memory leaks | FR-012 | "performance degradation over time" |
| Deadlocks / race conditions | FR-013 | "freezes or conflicting operations" |
| Async/await patterns | FR-013 | "non-blocking operations" |
| Lock-free / atomic synchronization | FR-013 | "thread-safe operations" |
| Interlocked / Volatile.Read | FR-013 | (implementation detail — should not be in spec) |
| No-op | FR-021 | "do nothing" |
| Draw calls | FR-021 | "rendering operations" |

### Rendering/Graphics Jargon
| Term | Context | Plain-Language Alternative |
|------|---------|--------------------------|
| Mid-render | Edge Cases, FR-020 | "while the overlay is being drawn" |
| Viewport culling | FR-009 | "only drawing visible tiles" |
| Linear RGB interpolation | FR-008 | "smooth color blending between categories" |
| Alpha decreasing from 1.0 to 0.0 | FR-003 | "fading from fully visible to invisible" |
| Clipping bounds | FR-018 | "visible area edges" |
| Frame consistency | FR-020 | "visual stability" |
| Partial-state rendering | FR-019 | "mixed old/new settings" |

### Data/Algorithm Jargon
| Term | Context | Plain-Language Alternative |
|------|---------|--------------------------|
| Forward-compatible deserialization | FR-006 | "old config files keep working after updates" |
| Cache invalidates | Clarifications | "stored values are cleared" |
| Half-open intervals | Clarifications, Key Entities | "ranges where the start is included but the end is not" |
| Throttle / leading-edge | FR-015 | "limit updates / immediate first response" |

### Acronyms (per NNGroup's "AAA: Always Avoid Acronyms")
| Acronym | First Appearance | Issue |
|---------|-----------------|-------|
| FR-001 through FR-021 | Throughout | Numbered IDs are fine, but "FR" itself is jargon |
| SC-001 etc. | Success Criteria | Same |
| WCAG 4.1.3 | FR-009 | External standard reference — should be spelled out with plain-language summary |
| RGBA | Key Entities | "Red, Green, Blue, Alpha (transparency)" |
| DTO | Key Entities | "Data Transfer Object" — pure implementation jargon |
| FPS | Throughout | "Frames Per Second (visual smoothness)" |

---

## Analysis: Does the Spec Meet the Criterion?

### The Stakeholder Test Applied

> Can a product owner or QA tester who doesn't code read each section and verify the implementation matches?

| Section | Passes Stakeholder Test? | Confidence |
|---------|--------------------------|------------|
| User Stories | **Yes** (with minor caveats) | High |
| Edge Cases | **Partial** | Medium |
| Clarifications | **No** | High |
| Functional Requirements | **No** — only 3/21 are verifiable | Very High |
| Key Entities | **Partial** | Medium |
| Success Criteria | **Partial** — only 3/7 are measurable | High |
| Assumptions | **No** | Very High |
| Dependencies | **No** | Very High |

### The Requirements vs. Specifications Distinction

Following the Modern Requirements framework [\[1\]], this document mixes two audiences:

- **Requirements** (what): User stories are appropriately written for business stakeholders.
- **Specifications** (how): FRs, entities, assumptions, and dependencies are written for developers.

The problem is that this is a *single document* titled "Feature Specification." A non-technical PO reading it will encounter the user stories (which they understand) followed immediately by FR-013 with "Interlocked" and "Volatile.Read" (which they cannot). The document does not signal which sections are for which audience.

### Is Some Technical Jargon Unavoidable?

Yes. For a SMAPI/Stardew Valley mod, terms like "tile," "overlay," "SMAPI," and "FPS" are domain-of-game-development terms that a technically-inclined PO can learn. The issue is not the presence of these terms but the density of *implementation-specific* jargon:

- **Acceptable**: "tile," "overlay," "tooltip," "viewport," "FPS," "SMAPI"
- **Unavoidable but should be defined**: "hot-reload," "cache," "deserialization," "async/await"
- **Should not appear in a feature spec**: "Interlocked," "Volatile.Read," "Game1.viewport," "SpriteBatch," "DTO," "composition root," "DI wiring," "no-op," "draw calls"

The spec contains far more of the third category than the first two.

### Comparison to Best Practices

| Best Practice [\[1\]\[2\]\[3\]\] | Spec Compliance |
|----------------------------------|-----------------|
| "Use clear and unambiguous language that everyone can understand" | Not met in FRs, Assumptions, Dependencies |
| "Define measurable criteria" | Partially met — some SCs are measurable, most FRs are not |
| "Define key terminologies at the start" | Not met — no glossary or definitions section |
| "Lead with plain-language alternative for unfamiliar terms" | Not met — technical terms appear without plain-language equivalents |
| "Avoid acronyms" | Not met — RGBA, DTO, FR, SC, WCAG used without definition |
| "Explain concepts in meaningful context" | Not met — clarifications are developer-to-developer notes |

---

## Final Verdict

### Item 3: [ ] Unchecked

The spec does **not** meet the "Written for Non-Technical Stakeholders" criterion.

**Rationale**: While the user stories (lines 13-78) are accessible, the majority of the document — particularly the Functional Requirements (18/21 not verifiable), Assumptions (0/7 accessible), and Dependencies (0/4 accessible) — is written for a developer audience. A non-technical product owner or QA tester cannot read this spec and independently verify that the implementation matches the requirements. They would need a developer to translate the FRs into behavioral tests.

The spec conflates "what the system should do" (requirements, for stakeholders) with "how the system should implement them" (specifications, for developers) without signaling which sections serve which audience.

---

## Recommendations

### Structural Changes

1. **Add a "Definitions & Acronyms" section** at the top of the spec. Define: tile, overlay, viewport, SMAPI, FPS, hot-reload, cache, deserialization, async/await, and any term that a non-technical reader might not know.

2. **Separate "what" from "how"** either by:
   - Splitting into two documents (Feature Requirements + Technical Specification), or
   - Adding clear section labels: "For Product Owners & QA" vs "For Developers"

3. **Rewrite FR-009 through FR-021** as behavioral requirements. Example:
   - Instead of: "FR-013: System MUST handle concurrent scenarios without deadlocks or race conditions... MUST use lock-free or atomic synchronization (Interlocked, Volatile.Read)"
   - Write: "FR-013: When the game saves or loads while overlays are visible, the overlays must freeze smoothly during the save and resume correctly after — no visual glitches, freezes, or crashes."

### Specific Rewrites

4. **Assumptions section**: Replace implementation references with behavioral assumptions:
   - Instead of: "MonoGame/XNA SpriteBatch rendering is available"
   - Write: "The game provides a way to draw colored shapes on top of tiles"

5. **Dependencies section**: Replace code references with feature references:
   - Instead of: "Requires existing ModEntry composition root pattern for DI wiring"
   - Write: "Requires the mod's existing startup system to register the visualization feature"

6. **Clarifications section**: Add plain-language summaries to each Q&A entry. Example:
   - Q: "How should colors be interpolated between category boundaries?"
   - A: "Smoothly blend from one color to the next as the health value increases. (Technical note: this is done using linear RGB interpolation.)"

7. **Verification notes on technical FRs**: Move all "Verification: confirm via code inspection/framework inspection" notes into a separate "Developer Verification" subsection that POs can skip.

### Jargon Reduction

8. **Apply the NNGroup rule [\[3\]]**: For every technical term that most readers won't know, lead with the plain-language alternative followed by the technical term in parentheses.
9. **Remove implementation-detail FRs** (FR-012, FR-013, FR-018, FR-019, FR-020, FR-021) from the feature spec entirely. These belong in a technical design document.

---

## References

[\[1\]] Modern Requirements. "Complete Guide to Writing Software Requirements Specification (SRS) Documents Like a Pro." March 2025. https://www.modernrequirements.com/blogs/requirements-specification/

[\[2\]] Jama Software. "Characteristics of Effective Software Requirements and Software Requirements Specifications (SRS)." https://www.jamasoftware.com/requirements-management-guide/writing-requirements/the-characteristics-of-excellent-requirements/

[\[3\]] Moran, K. "Dealing with Technical or Professional Jargon." NNGroup, March 2023. https://www.nngroup.com/articles/technical-jargon/

[\[4\]] Lang IA, et al. "Jargon and Readability in Plain Language Summaries of Health Research: Cross-Sectional Observational Study." JMIR, January 2025. https://pmc.ncbi.nlm.nih.gov/articles/PMC11773280/
