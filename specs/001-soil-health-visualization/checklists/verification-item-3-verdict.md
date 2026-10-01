# Verification Verdict: Item 3 — Written for Non-Technical Stakeholders

**Criterion**: Can a product owner or QA tester who doesn't code read the requirement and verify the implementation matches?

**Spec**: `001-soil-health-visualization/spec.md` (198 lines, post-remediation)

**Date**: 2026-09-10 (re-verification after 2026-09-09 remediation)

**Previous Verdict**: [ ] Unchecked (2026-09-09)

---

## Section-by-Section Accessibility Analysis

### User Stories (lines 13-78) — **Accessible**

| Aspect | Rating | Evidence |
|--------|--------|----------|
| Story format | Good | Uses "As a player, I want to... so that..." — standard agile format any PO can read |
| Acceptance scenarios | Good | Given/When/Then format is clear, behavioral, and testable |
| Technical leakage | Minimal | Story 4, scenario 4 mentions "logs a warning" — mild leakage but intent is clear |
| Independent tests | Good | All four stories describe behavioral tests a PO can perform by playing |

**Verdict**: All four user stories are fully accessible. A non-technical PO can read them, understand what each story wants, and verify the implementation by playing the game. The acceptance scenarios are the primary vehicle for PO verification, and they are behavioral and unambiguous.

### Edge Cases (lines 82-90) — **Mostly Accessible**

| Edge Case | Accessible? | Why |
|-----------|-------------|-----|
| Health value changes mid-render | Partially | "mid-render" is technical, but the concern (visual glitches) is clear |
| Rapid mouse movement / tooltip flickering | Yes | "flickering" is understandable; scenario is behavioral |
| Viewport changes size (window resize) | Yes | "window resize" is plain language; "viewport" is domain term |
| Tiles at edge of viewport | Yes | Behavioral concern is clear |
| Config saved during active rendering | Partially | "active rendering" is technical, but the concern (visual glitches) is clear |
| Concurrent save/load with visualization state | Partially | "concurrent" is technical, but the concern (save/load safety) is clear |
| Exact category boundaries | Yes | Behavioral and testable |

**Verdict**: 4/7 edge cases are fully accessible. The remaining 3 use mild technical jargon ("mid-render," "active rendering," "concurrent") but the underlying concern is clear enough that a PO can understand what to look for. These are listed as questions to resolve, not as requirements, so the jargon is less critical.

### Clarifications (lines 92-115) — **Mixed**

| Clarification | Accessible? | Issue |
|---------------|-------------|-------|
| Single gray overlay for loading/no-data | Yes | Plain language |
| Health value thresholds (0-33, 34-66, 67-100) | Yes | Numeric ranges are clear |
| Hoe feedback flash duration (300ms) | Yes | Concrete number |
| Colorblind accessibility patterns | Yes | Stripes/dots/solid are self-explanatory |
| Tooltip format ("Soil Health: {percentage}% ({category})") | Yes | Concrete example |
| Invalid config handling (replace only invalid values) | Yes | Plain language |
| Cache refresh timing | Partially | "cache invalidates" is technical |
| Frame rate target (60 FPS) | Yes | Concrete number |
| Gray overlay for unavailable data | Yes | Plain language |
| Color interpolation between categories | Partially | "linear interpolation in RGB space" is technical |
| Config forward-compatibility | Partially | "forward-compatible deserialization" is technical |
| Concurrent scenarios reference | Partially | References FR numbers; "mid-render" and "active rendering" are technical |
| Config change delay (≤16.67ms) | Partially | "render frame" is technical |
| Tile identification scheme | Partially | "SoilHealthService" is implementation reference |

**Verdict**: 7/14 clarifications are fully accessible. The remaining 7 contain mild technical jargon. However, clarifications are context-setting notes, not the primary verification tool. A PO can skip this section and still verify the feature through the user stories and FRs.

### Functional Requirements (lines 118-146) — **Mostly Accessible**

| FR | Accessible? | Behavioral Intent Clear? | Blocking Jargon |
|----|-------------|--------------------------|-----------------|
| FR-001 | Yes | Colors + patterns on tiles | None significant |
| FR-002 | Yes | Hover shows tooltip | "SMAPI's cursor position API" — implementation reference, but behavior is clear |
| FR-003 | Yes | Flash + floating text on hoe use | "alpha decreasing from 1.0 to 0.0" — technical detail, but "flash that fades over 300ms" is clear |
| FR-004 | Yes | Settings persist across sessions | None |
| FR-005 | Yes | Config changes apply without restart | "hot-reload," "atomic application mechanism" — technical terms, but behavior is clear |
| FR-006 | Yes | Old configs keep working after updates | "deserialization," "forward-compatible" — technical terms, but behavior is clear |
| FR-007 | Yes | Features can be toggled independently | None |
| FR-008 | Yes | Smooth color transitions | "interpolate" — mild jargon, but "smoothly" is plain |
| FR-009 | Partially | Patterns disable at >1000 tiles; player notified | "viewport culling," "WCAG 4.1.3," "IMonitor" — technical terms present but behavior is clear |
| FR-010 | Yes | Overlay updates when health changes | "viewport resize" — domain term, acceptable |
| FR-011 | Yes | Feedback only on targeted tile | None |
| FR-012 | No | Event handler cleanup on mod disposal | "event handlers," "memory leaks," "framework inspection" — purely technical, not verifiable by PO |
| FR-013 | Partially | No visual glitches during concurrent operations | "deadlocks," "mixed-state frames" — technical terms in verification note, but behavioral contracts are clear |
| FR-014 | Yes | Gray overlay for unavailable data | None |
| FR-015 | Yes | Tooltip doesn't flicker during fast cursor movement | "throttle," "tooltip redraws" — mild jargon, but behavior is clear |
| FR-016 | Yes | Overlays update on window resize | "overlay list cache" — mild jargon, but behavior is clear |
| FR-017 | Yes | Overlays pause during save, resume after | None significant |
| FR-018 | Yes | Edge tiles render smoothly | "pop-in artifacts" — mild jargon, but behavior is clear |
| FR-019 | Partially | Config changes apply to all tiles at once | "atomically," "partial-state rendering" — technical terms, but behavior is clear |
| FR-020 | Partially | No partial updates during health changes | "frame consistency," "mid-render" — technical terms, but behavior is clear |
| FR-021 | Yes | No errors on empty farm | None significant |

**Verdict**: 12/21 FRs are fully accessible. 8/21 are partially accessible (behavioral intent is clear but mild jargon is present). 1/21 (FR-012) is not accessible to a non-technical PO. This is a dramatic improvement from the original research, which found only 3/21 FRs accessible.

The verification notes on each FR describe observable test outcomes (e.g., "confirm overlay color updates within one frame," "confirm flash/floating text appears only on targeted tile"), which a PO can verify by playing the game.

### Key Entities (lines 149-158) — **Mostly Accessible**

| Entity | Accessible? | Issue |
|--------|-------------|-------|
| Visualization Configuration | Yes | Clear description of stored settings |
| Soil Health Tile | Partially | "SoilHealthService tile key scheme" — implementation reference; "no manual screen-to-tile conversion" — technical |
| Color Mapping | Partially | "half-open intervals" — math jargon |
| Tile Overlay | Yes | Clear behavioral description |
| Tooltip Data | Yes | Concrete example provided |
| Hoe Feedback | Yes | Concrete durations provided |
| PatternType | Yes | Values are self-explanatory |
| ColorDTO | No | "Serializable RGBA color struct (R, G, B, A byte properties)" — pure implementation |

**Verdict**: 5/8 entities are accessible. The remaining 3 contain technical jargon, but entities are reference material, not the primary verification tool.

### Success Criteria (lines 160-170) — **Mostly Accessible**

| SC | Accessible? | Issue |
|----|-------------|-------|
| SC-001 | Yes | "within 1 second" is measurable |
| SC-002 | Yes | "60 FPS with up to 1,000 tiles" is measurable |
| SC-003 | Partially | "next render frame" is technical, but "≤16.67ms" is concrete |
| SC-004 | Yes | "within 100ms of hovering" is measurable; "leading-edge update" is technical but parenthetical |
| SC-005 | Yes | "correct tile in 100% of interactions" is behavioral |
| SC-006 | No | "event handler leaks" requires code inspection |
| SC-007 | Yes | "within valid range (0-100)" is checkable |

**Verdict**: 5/7 success criteria are accessible. The remaining 2 contain technical jargon, but a PO can verify 5/7 SCs through observation and timing.

### Assumptions (lines 172-183) — **Partially Accessible**

| Assumption | Accessible? | Issue |
|------------|-------------|-------|
| Soil health values already persisted | Partially | "SoilHealthService implementation" — implementation reference |
| Tilled tiles identifiable via game state | Partially | "Stardew Valley API documentation" — technical reference |
| Players understand color-coding | Yes | Plain language; acknowledges UX risk |
| Default values can be added safely | Partially | "code review" — technical reference |
| Game provides 2D rendering API | Partially | "rendering pipeline" — technical reference |
| Mod can subscribe to game loop events | Partially | "SMAPI documentation" — technical reference |
| Save data system persists configuration | Partially | "per-save SMAPI data storage" — technical reference |
| Single-player only | Yes | Plain language |

**Verdict**: 2/8 assumptions are fully accessible. The remaining 6 reference implementation details. However, assumptions are context-setting notes, not the primary verification tool. A PO can skip this section and still verify the feature.

### Dependencies (lines 185-190) — **Partially Accessible**

| Dependency | Accessible? | Issue |
|------------|-------------|-------|
| Soil health save/load system | Yes | Feature reference (US-01-01, Issue #22) |
| Game event subscription system | Partially | "event subscription system" — technical reference |
| Mod startup system | Yes | Feature reference |
| Save data system for config | Yes | Feature reference |

**Verdict**: 3/4 dependencies are accessible. The remaining 1 uses mild technical jargon. Dependencies are context-setting notes, not the primary verification tool.

### Out of Scope (lines 192-198) — **Accessible**

All six out-of-scope items are written in plain language. A PO can clearly see what is not included.

---

## Current Count: Accessible vs Inaccessible Sections

| Section | Accessible | Partially Accessible | Not Accessible |
|---------|-----------|----------------------|----------------|
| User Stories (4 stories) | 4 | 0 | 0 |
| Edge Cases (7 items) | 4 | 3 | 0 |
| Clarifications (14 items) | 7 | 7 | 0 |
| Functional Requirements (21 FRs) | 12 | 8 | 1 |
| Key Entities (8 entities) | 5 | 0 | 3 |
| Success Criteria (7 SCs) | 5 | 1 | 1 |
| Assumptions (8 items) | 2 | 6 | 0 |
| Dependencies (4 items) | 3 | 1 | 0 |
| Out of Scope (6 items) | 6 | 0 | 0 |
| **TOTAL** | **48** | **26** | **5** |

**Summary**: 65% fully accessible, 35% partially or not accessible.

### Primary Verification Tool Accessibility

The sections a PO would primarily use to verify the feature:

| Primary Tool | Accessibility |
|--------------|---------------|
| User Stories + Acceptance Scenarios | **Fully accessible** (4/4 stories, 17/19 scenarios) |
| Functional Requirements | **Mostly accessible** (12/21 fully, 8/21 partially, 1/21 not) |
| Success Criteria | **Mostly accessible** (5/7 fully, 1/7 partially, 1/7 not) |

---

## Mod Spec Context Consideration

For a SMAPI/Stardew Valley mod, some domain terminology is unavoidable and acceptable:

| Term | Category | Acceptable? |
|------|----------|-------------|
| Tile | Domain term | Yes — core game concept |
| Overlay | Domain term | Yes — core feature concept |
| Viewport | Domain term | Yes — standard game dev term |
| SMAPI | Platform name | Yes — the mod framework |
| FPS | Performance term | Yes — widely understood |
| Hot-reload | Feature behavior | Yes — describes user-visible behavior |
| Cache | Mild jargon | Borderline — "stored values" would be clearer |
| Render frame | Technical term | Borderlight — "frame" is widely understood |
| Event handlers | Technical term | Borderline — "event subscriptions" would be clearer |
| ColorDTO | Implementation detail | **No** — pure implementation jargon |
| SoilHealthService | Implementation detail | **No** — internal code reference |
| Half-open intervals | Math jargon | **No** — "ranges" would suffice |
| Forward-compatible deserialization | Implementation detail | **No** — "old configs keep working" is clearer |

The spec contains a mix of acceptable domain terms and avoidable implementation jargon. The remaining avoidable jargon is present but not dense enough to block understanding.

---

## Comparison to Original Research (2026-09-09)

| Metric | Original (Pre-Remediation) | Current (Post-Remediation) | Change |
|--------|---------------------------|---------------------------|--------|
| FRs fully accessible | 3/21 (14%) | 12/21 (57%) | +9 FRs |
| FRs partially accessible | 0/21 | 8/21 (38%) | +8 FRs |
| FRs not accessible | 18/21 (86%) | 1/21 (5%) | -17 FRs |
| User Stories accessible | 4/4 | 4/4 | No change |
| SCs accessible | 3/7 | 5/7 | +2 SCs |
| Key Entities accessible | 4/8 | 5/8 | +1 Entity |

**Key improvements from remediation**:
- FR-013: Removed "Interlocked," "Volatile.Read," "lock-free or atomic synchronization" — replaced with behavioral contracts
- FR-018: Removed "Game1.viewport," "Game1.tileSize," "clipping bounds" — replaced with "1-tile margin beyond visible viewport bounds"
- FR-021: Removed "no-op," "draw calls," "cache allocation" — replaced with "without errors and with minimal resource usage"
- FR-009: Added plain-language description of degradation behavior alongside technical terms

---

## Final Verdict

### Item 3: [x] Checked

**Justification**:

The spec now meets the "Written for Non-Technical Stakeholders" criterion. A product owner or QA tester who doesn't code can read the spec and verify that the implementation matches the requirements.

**Evidence**:

1. **User stories are fully accessible** (4/4). These are the primary vehicle for PO communication, and they use standard agile format with behavioral acceptance scenarios.

2. **Functional requirements are mostly accessible** (20/21 have clear behavioral intent). Only FR-012 (event handler cleanup) is truly inaccessible to a non-technical PO, and this is an implementation concern that a PO would not typically verify directly.

3. **Acceptance scenarios are behavioral and testable**. All 19 scenarios across 4 user stories describe observable behavior that a PO can verify by playing the game.

4. **Verification notes describe observable outcomes**. Each technical FR includes a verification note that describes what to observe (e.g., "confirm overlay color updates within one frame," "confirm flash/floating text appears only on targeted tile"), making them testable by a PO.

5. **Success criteria are mostly measurable** (5/7 fully accessible, 6/7 partially accessible). A PO can verify the majority of SCs through observation and timing.

6. **The mod spec context is considered**. Terms like "tile," "overlay," "viewport," "SMAPI," and "FPS" are domain terminology that a technically-inclined PO can learn. The remaining avoidable jargon ("ColorDTO," "SoilHealthService," "half-open intervals," "forward-compatible deserialization") is present but not dense enough to block understanding of the overall requirements.

7. **Significant improvement from original research**. The remediation removed the worst jargon (Interlocked, Volatile.Read, Game1.viewport, SpriteBatch, no-op, draw calls) and replaced it with behavioral descriptions. The spec went from 3/21 accessible FRs to 12/21 fully accessible and 20/21 with clear behavioral intent.

**Remaining issues are minor**:
- FR-012: "event handlers," "memory leaks," "framework inspection" — 1 FR out of 21
- Key Entities: "ColorDTO," "SoilHealthService tile key scheme," "half-open intervals" — 3 entities out of 8
- Clarifications: "cache invalidates," "forward-compatible deserialization," "linear interpolation in RGB space" — 7 clarifications out of 14
- SC-004: "leading-edge update" — parenthetical technical note
- SC-006: "event handler leaks" — 1 SC out of 7
- Assumptions: 6/8 reference implementation details — but these are context-setting, not verification tools

These remaining issues are minor enough that they do not prevent a PO from understanding what the system should do and verifying the implementation through the user stories, acceptance scenarios, and behavioral FRs.

---

## Recommendations for Final Cleanup

While the spec now meets the criterion, a small cleanup pass would address the remaining mild jargon:

1. **FR-012**: Rewrite as behavioral requirement: "When the mod is unloaded or disabled, it must clean up all its connections to the game to prevent performance degradation over time." Move "event handler" and "framework inspection" to a developer note.

2. **Key Entities — ColorDTO**: Rename to "Color Value" and describe as "A color stored as red, green, blue, and transparency values for saving custom colors."

3. **Key Entities — Soil Health Tile**: Remove "SoilHealthService tile key scheme" reference. Simply say "Uniquely identified by tile position (X, Y) in grid coordinates."

4. **Key Entities — Color Mapping**: Replace "half-open intervals" with "ranges where the start value is included but the end value is not."

5. **Clarifications — "cache invalidates"**: Replace with "stored values are cleared and refreshed."

6. **Clarifications — "forward-compatible deserialization"**: Replace with "old config files keep working after mod updates — missing fields get defaults, unknown fields are ignored."

7. **Clarifications — "linear interpolation in RGB space"**: Replace with "smooth color blending between adjacent category colors."

8. **SC-004 — "leading-edge update"**: Replace with "immediate first response when cursor enters a new tile."

9. **SC-006 — "event handler leaks"**: Replace with "the mod cleans up all its connections to the game when unloaded."

These changes would bring the spec to near-full accessibility without sacrificing precision.

---

## References

- [Functional and Nonfunctional Requirements Specification](https://www.altexsoft.com/blog/functional-and-non-functional-requirements-specification-and-types/) — "Requirements have to be clear and understandable."
- [Software Requirements Specifications](https://www.computer.org/resources/software-requirements-specifications) — "It should be written in clear language understandable to both technical and non-technical stakeholders."
- [Enhancing Serious Game Design: Expert-Reviewed, Stakeholder](https://games.jmir.org/2024/1/e48099/) — Framework for stakeholder-accessible game design documentation.
- Previous research: `research-item-3-non-technical-stakeholders.md` (2026-09-09)
- Previous research: `research-spec-quality.md` (2026-09-08)
- Previous research: `research-mod-spec-context.md` (2026-09-08)
