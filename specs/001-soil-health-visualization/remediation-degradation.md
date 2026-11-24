# Remediation: Graceful Degradation Definition for FR-009

**Date**: 2026-09-05
**Feature**: 001-soil-health-visualization
**Scope**: Clarify "graceful degradation" in FR-009

---

## 1. Problem Statement

FR-009 currently defines graceful degradation only via parenthetical:

> "graceful degradation (simplified solid-color rendering without patterns)"

This is ambiguous. It is unclear whether degradation affects **only** pattern rendering or also impacts color interpolation, opacity calculations, viewport culling precision, or other rendering features.

---

## 2. Technical Analysis: Rendering Pipeline Costs

From `research.md` and `contracts/IVisualizationService.md`, the per-tile rendering pipeline is:

| Step | Operation | Cost | Can Be Simplified? |
|------|-----------|------|-------------------|
| 1. Viewport culling | Filter tiles to visible range | O(n) where n = tilled tiles | **No** — essential for performance |
| 2. Color computation | `Color.Lerp` interpolation | Cached per data-model.md | Cheap (O(1) lookup) |
| 3. Base overlay draw | SpriteBatch draw call (1x1 texture tinted) | 1 draw call per tile | **No** — required for visualization |
| 4. Pattern draw | Separate SpriteBatch draw call with pattern texture | 1 additional draw call per tile | **Yes** — explicit target of degradation |

### Key Insight

From `research.md` §3 (Accessibility Patterns):

> "Implementation: separate SpriteBatch draw call with pattern texture per visible tile"

**Pattern rendering doubles the draw calls per tile.** With 1,000+ visible tiles, disabling patterns is the single highest-leverage simplification.

### What Costs What

- **Viewport culling**: Essential — without it, we'd render all tiles. Must remain active.
- **Color interpolation**: Results are cached per `data-model.md` (Color Computation Cache). Cache lookup is O(1). Negligible cost.
- **Opacity**: Single `Color.A` channel assignment. Negligible cost.
- **Patterns**: Separate texture bind + draw call per tile. Significant cost at scale.

---

## 3. YAGNI Analysis

Per Constitution Principle V:

> "Start with the simplest solution. Do not implement features not needed now. Avoid premature abstractions."

Applying YAGNI to degradation:

1. **Do specify**: What features are disabled (the observable behavior).
2. **Don't specify**: Implementation details (how the toggle works internally, threshold detection timing, hysteresis behavior).
3. **Don't over-engineer**: No need for multi-level degradation, no need to simplify beyond what achieves 60 FPS.

The minimal viable degradation: **disable patterns, keep everything else.**

---

## 4. Proposed Definition

### Degraded Mode Trigger
- **When**: Visible tile count (after viewport culling) exceeds 1,000.
- **What changes**: Accessibility pattern rendering is disabled.
- **What stays the same**: Color interpolation, opacity, viewport culling, tooltip rendering, hoe feedback.

### Concrete Behavior

| Feature | Normal Mode (≤1,000 tiles) | Degraded Mode (>1,000 tiles) |
|---------|---------------------------|------------------------------|
| Viewport culling | Active | Active (unchanged) |
| Color interpolation | Active (cached) | Active (unchanged) |
| Opacity | User-configured | User-configured (unchanged) |
| Pattern rendering (stripes/dots/solid) | Enabled | **Disabled** |
| Tooltip rendering | Active | Active (unchanged) |
| Hoe feedback | Active | Active (unchanged) |

### Visual Difference

- **Normal**: Tile shows interpolated color + pattern overlay (stripes for poor, dots for moderate, solid for healthy).
- **Degraded**: Tile shows interpolated color only, no pattern overlay.

---

## 5. Proposed FR-009 Rewording

### Current Text

> **FR-009**: System MUST perform viewport culling to only render overlays for visible tiles, with graceful degradation (simplified solid-color rendering without patterns) when visible tile count exceeds 1,000 to maintain 60 FPS. *Verification: Render 1,001+ visible tiles and confirm frame time stays ≤16.67ms with patterns disabled.*

### Proposed Text

> **FR-009**: System MUST perform viewport culling to only render overlays for visible tiles, with graceful degradation when visible tile count exceeds 1,000 to maintain 60 FPS. Graceful degradation MUST disable accessibility pattern rendering (stripes, dots) for all tiles, rendering only solid color overlays. Color interpolation, opacity, and viewport culling MUST remain active during degradation. *Verification: Render 1,001+ visible tiles and confirm frame time stays ≤16.67ms with patterns disabled but colors and opacity unchanged.*

### Rationale for Changes

1. **Separated degradation definition from trigger**: The trigger (>1,000 tiles) is stated first, then the degradation behavior is defined explicitly.
2. **Explicitly lists what stays active**: Prevents ambiguity about whether interpolation or opacity are affected.
3. **Observable verification**: The verification note now checks both what's disabled (patterns) and what's preserved (colors, opacity).

---

## 6. Alternative Interpretations Considered

### Alternative A: Also Disable Color Interpolation

**Proposal**: In degraded mode, use category base colors (red/yellow/green) instead of interpolated colors.

**Rejected because**:
- Color computation is cached per `data-model.md` — lookup cost is O(1), not a bottleneck.
- Adds implementation complexity (two color computation paths).
- Visual regression is unnecessary — interpolation is cheap.
- Violates YAGNI: no evidence this is needed to hit 60 FPS.

### Alternative B: Reduce Opacity in Degraded Mode

**Proposal**: Use lower opacity in degraded mode to reduce overdraw.

**Rejected because**:
- Opacity is a single channel assignment — not a performance factor.
- Would change user-configured appearance without clear benefit.
- Violates YAGNI: no evidence this is needed.

### Alternative C: Disable Viewport Culling Margin

**Proposal**: Remove the 1-tile margin (FR-018) in degraded mode.

**Rejected because**:
- The margin prevents pop-in artifacts — disabling it degrades UX.
- The margin adds at most 2-4 extra tiles — negligible cost.
- Violates YAGNI: no evidence this is needed.

### Alternative D: Multi-Level Degradation

**Proposal**: Define multiple degradation levels (e.g., >1,000: no patterns; >2,000: no interpolation; >5,000: reduced opacity).

**Rejected because**:
- Over-engineered for current requirements.
- No evidence multiple levels are needed to hit 60 FPS.
- Violates YAGNI: "Do not implement features not needed now."

---

## 7. Impact Assessment

### SC-002 Compatibility

> SC-002: Overlay rendering maintains 60 FPS (16.67ms per frame) with up to 1,000 visible tilled tiles

The proposed degradation is **compatible** with SC-002:
- SC-002 defines the target at ≤1,000 tiles (normal mode).
- FR-009 defines the mechanism for >1,000 tiles (degraded mode).
- Together they cover all cases: normal mode up to 1,000, degraded mode beyond.

### Implementation Complexity

The degradation requires:
1. A tile count check after viewport culling.
2. A boolean flag passed to the render method (or checked internally).
3. Skip pattern draw calls when flag is true.

This is **minimal complexity** — a single conditional in the render loop.

---

## 8. Recommendation

**Adopt the proposed FR-009 rewording** (Section 5). It:

1. Defines degradation concretely (patterns disabled only).
2. Preserves all other rendering features.
3. Is testable (observable behavior).
4. Respects YAGNI (no over-specification).
5. Maintains 60 FPS target as the hard requirement.
