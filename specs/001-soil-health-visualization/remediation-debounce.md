# Remediation: FR-015 Debounce Specification Ambiguity

**Date**: 2026-09-05
**Status**: Proposed
**Related**: FR-015 (tooltip debouncing), SC-004 (tooltip appearance latency)

---

## 1. Ambiguity Analysis

### Original FR-015 Text

> System MUST debounce tooltip updates during rapid cursor movement, suppressing tooltip redraws until cursor velocity drops below a threshold (minimum 50ms between tooltip updates) [Resolves: CHK036 tooltip flickering].
>
> *Verification: Sweep cursor across 20 tiles in <100ms and confirm tooltip updates ≤2 times during movement.*

### Identified Issues

| Issue | Description | Impact |
|-------|-------------|--------|
| **Dual mechanism conflict** | "Cursor velocity drops below a threshold" and "minimum 50ms between tooltip updates" describe two fundamentally different control mechanisms. They cannot both be literally true simultaneously. | Implementer must choose one, creating risk of misalignment with spec author's intent. |
| **Velocity undefined** | "Cursor velocity" implies distance/time, but no unit is specified (pixels/second? tiles/frame? screen-widths/second?). No threshold value is provided. | Cannot be implemented without guessing the missing parameters. |
| **Contradictory behavior** | A velocity-based approach would *update the tooltip when cursor slows down*, while a time-based approach would *update on a fixed interval regardless of speed*. These produce different user experiences. | Confusion about whether tooltip should update during slow cursor drift or only at fixed intervals. |
| **Verification mismatch** | The verification test uses a time-based measurement (sweep in <100ms, expect ≤2 updates) which aligns with the 50ms interval framing, not velocity. | The velocity framing is never actually tested. |

### Root Cause

The spec author conflated two concepts:
- **What the user perceives**: "The tooltip stops flickering when I move the cursor quickly" → described as "velocity threshold"
- **What the implementation needs**: A measurable, testable parameter → "50ms minimum interval"

The velocity framing is a *user-observable outcome* of time-based throttling, not a distinct implementation mechanism. When cursor moves fast, the 50ms throttle naturally causes the tooltip to appear "stuck" until the cursor either stops or slows enough that the 50ms window elapses between position changes. This is the debounce/throttle behavior.

---

## 2. Correct Behavior Analysis

The intended behavior is **time-based throttling**:

1. **Tooltip updates must be separated by at least 50ms** — this is the measurable, testable requirement.
2. **During rapid cursor movement**, successive tooltip update requests within the 50ms window are suppressed (debounced).
3. **When the cursor stops or moves slowly enough** that 50ms elapses between distinct tile positions, the tooltip updates to reflect the new tile.

This is a classic "leading-edge throttle" pattern:
- First hover event over a new tile → tooltip renders immediately
- Subsequent hover events within 50ms → suppressed
- After 50ms with no new distinct tile → tooltip stable
- New distinct tile after 50ms elapsed → tooltip updates

**Key insight**: The 50ms interval is the *mechanism*; the "velocity" effect is the *emergent behavior*.

---

## 3. Supporting Evidence

### From spec.md (Edge Cases)

> *How does the system handle rapid mouse movement across many tiles (tooltip flickering)?*

This edge case confirms the *symptom* being addressed is tooltip flickering during rapid movement, not velocity-based filtering.

### From spec.md (Success Criteria)

> **SC-004**: Tooltips appear within 100ms of hovering over a soil tile

This is a separate latency requirement for *initial* appearance, distinct from the debounce interval. The 50ms debounce (FR-015) and 100ms initial appearance (SC-004) are compatible: first appearance is fast, subsequent updates during movement are throttled.

### From contracts/IVisualizationService.md

The `RenderTooltip` method signature accepts `gameTime`, providing access to timing information needed for throttling logic. No velocity-related parameters exist in the contract.

---

## 4. Proposed Remediation

### 4.1 Rewrite of FR-015

Replace the original FR-015 with the following:

> **FR-015**: System MUST throttle tooltip updates to a minimum 50ms interval during cursor movement, suppressing successive tooltip redraws that occur within the throttle window to prevent tooltip flickering. Tooltip MUST update immediately when the cursor first enters a new tile (leading-edge), then suppress further updates until 50ms has elapsed since the last rendered update. [Resolves: CHK036 tooltip flickering].
>
> *Verification: Sweep cursor across 20 tiles in <100ms and confirm tooltip renders at most 2 times (initial entry + one update after 50ms window). Verify that a cursor held stationary over a new tile for >50ms updates tooltip within the next render frame.*

### 4.2 Rationale for Changes

| Change | Rationale |
|--------|-----------|
| "throttle tooltip updates to a minimum 50ms interval" | Replaces vague "velocity drops below threshold" with measurable time-based mechanism |
| "suppressing successive tooltip redraws that occur within the throttle window" | Explicitly describes the suppression behavior without referencing velocity |
| "update immediately when the cursor first enters a new tile (leading-edge)" | Clarifies the throttle pattern — initial render is immediate, not delayed |
| "50ms has elapsed since the last rendered update" | Precise definition of the throttle window |
| Updated verification | Makes the test deterministic: "at most 2 times (initial + one after 50ms)" and adds stationary-cursor verification |

### 4.3 No Changes Required

| Item | Reason |
|------|--------|
| 50ms value | Retained as the measurable requirement |
| CHK036 reference | Unchanged — still resolves the tooltip flickering check |
| SC-004 | Unchanged — initial appearance latency is separate from throttling |
| IVisualizationService contract | Unchanged — RenderTooltip already accepts GameTime for timing |

---

## 5. Implementation Guidance (Non-Normative)

For implementers, the throttling logic in `VisualizationService.RenderTooltip` should:

1. Track `_lastTooltipUpdateTime` (GameTime value)
2. On each call, compute elapsed time since last update
3. If `elapsed >= 50ms` AND cursor is over a different tile than last rendered → update tooltip, record new timestamp
4. If `elapsed < 50ms` → skip update (suppress)
5. If cursor is over same tile as last rendered → no-op (no change needed)

This produces the emergent behavior: during rapid movement, updates are naturally throttled; when cursor slows or stops, updates resume — without any explicit velocity calculation.

---

## 6. Verification of Remediation

The proposed FR-015:
- [x] Uses measurable, testable parameters (50ms interval)
- [x] Eliminates the undefined "velocity threshold" concept
- [x] Preserves the original intent (prevent tooltip flickering during rapid movement)
- [x] Aligns with the existing verification test (≤2 updates in <100ms sweep)
- [x] Compatible with SC-004 (100ms initial appearance)
- [x] Compatible with IVisualizationService contract (uses GameTime)
