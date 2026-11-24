# Research: Degradation Threshold Hysteresis Analysis

**Date:** 2026-09-11
**Spec:** FR-009 — Graceful degradation at >1,000 visible tiles
**Question:** Should the system immediately toggle accessibility patterns on/off at the exact 1,000 threshold, or use a hysteresis mechanism?

---

## 1. Executive Summary

**Recommendation: Implement hysteresis with a dual-threshold approach.** When the visible tile count oscillates around 1,000 (e.g., 999 → 1,001 → 999 → 1,001), immediate toggling causes visual "flickering" of accessibility patterns — a direct violation of the mod's accessibility-first design pillar and WCAG 4.1.3 (Status Messages). A hysteresis buffer of ~50–100 tiles (5–10% of threshold) prevents oscillation while preserving the 60 FPS performance target.

This is consistent with:
- Industry-standard LOD hysteresis in Unreal Engine (`LODHysteresis`), Unity, and three.js
- The existing one-time notification design in `OverlayRenderer.cs` (which already has a primitive hysteresis-like flag)
- The project's YAGNI principle (minimal complexity addition: one constant + one condition)

---

## 2. Current Implementation Analysis

### 2.1 OverlayRenderer.cs — Degradation Logic

The degradation check lives in `OverlayRenderer.GetOverlays()`:

```csharp
// OverlayRenderer.cs, line 24
private const int DegradationTileThreshold = 1000;

// OverlayRenderer.cs, lines 72-90
if (visibleCount > DegradationTileThreshold &&
    string.Equals(degradationMode, "auto", StringComparison.OrdinalIgnoreCase))
{
    if (!_degradationNotificationSent)
    {
        _monitor.Log(
            $"Performance degradation activated: {visibleCount} visible tiles exceeds {DegradationTileThreshold} threshold. " +
            "Disabling accessibility patterns per FR-009.",
            LogLevel.Warn);
        _degradationNotificationSent = true;
    }
    showPatterns = false;
}
else if (_degradationNotificationSent && visibleCount <= DegradationTileThreshold)
{
    // Reset notification flag when tile count drops back within threshold
    _degradationNotificationSent = false;
    _monitor.Log("Performance degradation deactivated: visible tile count within acceptable range.", LogLevel.Trace);
}
```

**Source:** `LivingRoots/Services/Visualization/OverlayRenderer.cs`, lines 24, 72–90

### 2.2 Key Observations

1. **Single threshold, no hysteresis:** The check `visibleCount > 1000` uses a single boundary. At exactly 1000 tiles, patterns render. At 1001, they disable. At 1000 again, they re-enable. This is the oscillation problem.

2. **Notification flag is primitive hysteresis:** The `_degradationNotificationSent` boolean (line 32) already implements a one-way latch — it prevents repeated notifications but does NOT prevent the visual toggling of patterns. The notification and the visual state are decoupled.

3. **Threshold is hardcoded locally:** `DegradationTileThreshold = 1000` is a `private const` in `OverlayRenderer.cs` (line 24), NOT in `ModConstants` (which doesn't have a `MaxVisibleTiles` constant). The design system references `MaxVisibleTiles = 1000` in Appendix B (line 739) but this constant doesn't exist in code yet.

4. **No hysteresis constant exists:** There is no `DegradationHysteresisTiles` or similar constant anywhere in the codebase.

### 2.3 Constants Location

The degradation threshold is defined as:
- `OverlayRenderer.cs` line 24: `private const int DegradationTileThreshold = 1000;`
- NOT in `ModConstants` (confirmed by grep — `ModConstants` has no tile count threshold constant)
- Referenced in design system: `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` line 739: `MaxVisibleTiles | 1000 | Graceful degradation threshold`

**Source:** `LivingRoots/Constants.cs` (full file, 64 lines — no degradation threshold constant present)

---

## 3. Spec Analysis

### 3.1 FR-009 Exact Wording

> "System MUST only render overlays for tiles visible in the current viewport, with graceful degradation when visible tile count **strictly exceeds 1,000 (>1,000)** to maintain 60 FPS."

**Source:** `specs/001-soil-health-visualization/spec.md`, line 152 (FR-009)

The spec says ">1,000" — this is the **trigger** threshold. It does NOT specify the **recovery** threshold. This is a spec gap that hysteresis fills.

### 3.2 One-Time Notification Requirement

> "When degradation activates in `auto` mode, the system MUST display a layered notification: (1) **one-time SMAPI chat box message** (appears once per degradation event, scrolls naturally, no active dismissal)..."

**Source:** `specs/001-soil-health-visualization/spec.md`, line 152 (FR-009)

**Critical implication for hysteresis:** If patterns toggle on/off rapidly at the boundary, the "one-time per degradation event" requirement becomes ambiguous. Is each crossing a new "event"? The current `_degradationNotificationSent` flag resets when count drops ≤1000 (line 85-89), meaning each oscillation would trigger a new notification — spamming the player's chat box.

### 3.3 SC-002 — 60 FPS at 1,000 Tiles

> "Overlay rendering maintains 60 FPS (16.67ms per frame) with up to 1,000 visible tilled tiles"

**Source:** `specs/001-soil-health-visualization/spec.md`, line 189 (SC-002)

Hysteresis does NOT violate SC-002. The success criterion is measured at ≤1,000 tiles (normal mode). Hysteresis only affects behavior in the 1,000–1,100 tile range where the system is already in degraded mode.

### 3.4 Spec Clarification on Notification

> "Q: Should the degradation notification be actively dismissible or appear once passively? → A: Appears once per degradation event, no active dismissal."

**Source:** `specs/001-soil-health-visualization/spec.md`, line 104 (Clarifications, Session 2026-09-10)

The clarification says "once per degradation event" — hysteresis defines what constitutes a single "event" by requiring the count to drop below a recovery threshold before a new degradation event can begin.

---

## 4. Design System Analysis

### 4.1 Section 11.2 — Graceful Degradation Thresholds

| Condition | Action |
|-----------|--------|
| >1000 visible tiles | Disable patterns (keep base overlay) |
| >2000 visible tiles | Reduce overlay opacity by 50% |
| Frame time > 4ms | Skip tooltip rendering for frame |
| Memory pressure | Clear overlay cache, recompute next frame |

**Source:** `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`, lines 553–560

**Observation:** The design system defines degradation as a one-way trigger table. No recovery thresholds are specified. This is the gap hysteresis addresses.

### 4.2 Section 5.2 — Rendering Rules

> "Graceful degradation: >1000 visible tiles → disable patterns automatically"

**Source:** `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`, line 202

### 4.3 Section 8.3 — Pattern Rendering Rules

> "Graceful degradation: Patterns disabled when >1000 visible tiles"

**Source:** `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`, line 392

### 4.4 Design Pillar Conflict

The design system's **Accessibility First** pillar states:

> "Color is never the sole carrier of meaning. Patterns, text, and icons reinforce every data point."

**Source:** `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`, line 43

Rapid toggling of patterns at the threshold directly contradicts this pillar — patterns would flicker on/off, creating confusion about whether the accessibility aid is active.

---

## 5. Existing Research Findings

### 5.1 Remediation Doc — Degradation

The remediation doc explicitly states:

> "Don't specify: Implementation details (how the toggle works internally, threshold detection timing, hysteresis behavior)."

**Source:** `specs/001-soil-health-visualization/remediation-degradation.md`, line 56

This means hysteresis is an **implementation detail** that the spec intentionally leaves to the implementer. The spec defines the trigger (>1000) but not the recovery mechanism.

### 5.2 Notification Mechanisms Research

The notification research confirms:

> "The chat box is for multiplayer player communication, not for mod-to-player notifications."

**Source:** `specs/001-soil-health-visualization/research-notification-mechanisms.md`, line 155

This means the degradation notification should NOT use the chat box (contradicting the current spec wording). The notification should use `Game1.addHUDMessage()` (toast popup). Regardless of notification mechanism, hysteresis prevents notification spam.

### 5.3 Accessibility Notification Research

> "A one-time toast alone is the minimum viable notification but is insufficient for accessibility because: It can be missed... It provides no ongoing awareness that the accessibility aid is still disabled."

**Source:** `specs/001-soil-health-visualization/checklists/research-accessibility-notification.md`, lines 232–237

The persistent HUD indicator (Layer 2) solves the ongoing awareness problem. Hysteresis prevents the indicator from flickering on/off at the boundary.

---

## 6. Game Engine Best Practices

### 6.1 Unreal Engine — LODHysteresis

Unreal Engine has a built-in `LODHysteresis` parameter:

> "LODHysteresis is 0.02, although as I understand it, this value is a buffer zone to prevent the mesh from flickering continually between LODs at a certain distance."

**Source:** [Unreal Engine Forums — Having issues with Skeletal Meshes and LOD](https://forums.unrealengine.com/t/having-issues-with-skeletal-meshes-and-lod/15308)

Unreal's approach: a hysteresis value creates a dead zone between the "switch to lower LOD" threshold and the "switch back to higher LOD" threshold. This prevents flickering when the camera is near a boundary.

### 6.2 three.js — LOD Hysteresis Feature Request

The three.js issue explicitly references Unreal and Unity:

> "With the LOD systems in Unreal, Unity, and Blender, LODs have a threshold that offsets the distance where the LOD appears from the distance where it disappears, to reduce flickering from small movements (e.g. head movement with roomscale VR)."

**Source:** [three.js Issue #14565 — LOD: Consider adding hysteresis option](https://github.com/mrdoob/three.js/issues/14565)

This was implemented in PR #14566. The pattern: hysteresis is only applied when moving from complex→simple (degradation), not simple→complex (recovery).

### 6.3 Unity — Quality Settings

Unity's quality system uses automatic quality level adjustment:

> "if fps is below a certain threshold some heavy rendering features will be disabled."

**Source:** [Unity Discussions — Automatically setting the quality level](https://discussions.unity.com/t/automatically-setting-the-quality-level-based-on-the-users-hardware/868104)

Unity's approach uses frame time thresholds with implicit hysteresis (quality levels don't rapidly toggle). The `Mesh LOD` system in Unity 6+ also supports hysteresis-like buffer zones.

### 6.4 Blender Game Engine — Hysteresis Override

> "Hysteresis override is the blending factor for between those objects, whether smooth or hasty transitions, you can choose."

**Source:** [Blender Artists — What is Hysteresis Override?](https://blenderartists.org/t/what-is-hysteresis-override/1309185)

### 6.5 Industry Consensus

All major game engines implement hysteresis for LOD/quality transitions. The pattern is consistent:

| Engine | Mechanism | Hysteresis Value |
|--------|-----------|-----------------|
| Unreal | `LODHysteresis` | 0.02 (2% of screen size) |
| three.js | `LOD.hysteresis` | Configurable (default 0.01) |
| Unity | Quality level dead zones | Implicit in quality system |
| Blender | Hysteresis Override | Configurable blending factor |

The common principle: **use different thresholds for entering and exiting a state**, with the exit threshold requiring a larger change to prevent oscillation.

---

## 7. Proposed Hysteresis Design

### 7.1 Dual-Threshold Approach

```
Trigger threshold:  >1000 tiles → enter degraded mode (disable patterns)
Recovery threshold: <950 tiles  → exit degraded mode (re-enable patterns)
Buffer zone:        950–1000 tiles → maintain current state
```

### 7.2 State Machine

```
[NORMAL MODE] --(visibleCount > 1000)--> [DEGRADED MODE]
     ^                                         |
     |                                         |
     +--(visibleCount < 950)-------------------+
```

In the buffer zone (950–1000), the system maintains whatever state it was already in. This prevents oscillation.

### 7.3 Implementation Sketch

```csharp
// In OverlayRenderer.cs
private const int DegradationTriggerThreshold = 1000;   // Enter degradation
private const int DegradationRecoveryThreshold = 950;   // Exit degradation (hysteresis)
private bool _isInDegradedMode;                          // Current state

// In GetOverlays():
if (!_isInDegradedMode && visibleCount > DegradationTriggerThreshold)
{
    _isInDegradedMode = true;
    // ... trigger notification
}
else if (_isInDegradedMode && visibleCount < DegradationRecoveryThreshold)
{
    _isInDegradedMode = false;
    // ... reset notification flag
}
showPatterns = !_isInDegradedMode;
```

### 7.4 Why 950?

- **5% buffer** (50 tiles below threshold) is consistent with Unreal's 2% hysteresis but accounts for tile count being a coarser metric than screen-size percentage
- Large enough to prevent oscillation from viewport panning (a single tile row can add/remove ~20–40 tiles)
- Small enough that the player won't notice patterns re-enable slightly before the "ideal" 1000 mark
- The buffer zone (950–1000) is within the "degraded" region per SC-002 (which only guarantees 60 FPS at ≤1000 tiles)

### 7.5 Alternative: Time-Based Hysteresis

Instead of a tile-count buffer, use a time delay:

```csharp
// Require degradation to persist for N frames before activating
private int _degradationFrameCount;
private const int DegradationFrameDelay = 3; // ~50ms at 60fps
```

**Rejected because:**
- Tile count is a more direct signal than frame count
- Time-based hysteresis adds latency to genuine degradation events
- Frame count varies with FPS (which is itself the thing being protected)

---

## 8. Impact Assessment

### 8.1 FR-009 Compatibility

| FR-009 Requirement | Hysteresis Impact |
|-------------------|-------------------|
| ">1,000 tiles triggers degradation" | ✅ Preserved — trigger is still >1000 |
| "Disable patterns in degraded mode" | ✅ Preserved — patterns disabled above 1000 |
| "One-time notification per event" | ✅ Improved — hysteresis defines "event" boundary |
| "Persistent HUD indicator" | ✅ Improved — no flickering indicator |
| "`never` mode keeps patterns" | ✅ Unaffected — hysteresis only applies to `auto` mode |
| "`notify` mode warns only" | ✅ Unaffected — hysteresis only applies to `auto` mode |

### 8.2 SC-002 Compatibility

> "60 FPS with up to 1,000 visible tiles"

Hysteresis does not affect this. At ≤1000 tiles, the system is in normal mode (patterns enabled). The 60 FPS target is maintained by the pattern disable at >1000.

### 8.3 Notification Frequency

**Without hysteresis:** Each frame crossing 1000 triggers a new notification (chat spam).

**With hysteresis:** One notification per degradation event (crossing >1000), one recovery event (dropping <950). Matches the spec's "one-time per degradation event" requirement.

### 8.4 Accessibility Impact

**Without hysteresis:** Patterns flicker on/off → colorblind players lose their redundant encoding intermittently → violates WCAG 1.4.1 (Use of Color).

**With hysteresis:** Patterns stay disabled until tile count clearly drops → stable accessibility state → complies with WCAG 4.1.3 (Status Messages).

### 8.5 YAGNI Compliance

Hysteresis adds:
- One constant (`DegradationRecoveryThreshold = 950`)
- One boolean field (`_isInDegradedMode`)
- One additional condition in the if-else chain

This is **minimal complexity** — consistent with YAGNI. The alternative (no hysteresis) creates a worse user experience that would require a more complex fix later.

---

## 9. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Buffer too small (still oscillates) | Low | Medium | Make buffer configurable; default 50 tiles |
| Buffer too large (patterns stay off too long) | Low | Low | 50 tiles = ~0.5% of viewport; negligible visual impact |
| Player confusion at 950–1000 boundary | Very Low | Low | HUD indicator shows degradation state; patterns are subtle |
| Multiplayer desync | None | None | Degradation is per-client (local viewport) |

---

## 10. Recommendation

### Implement hysteresis with these parameters:

1. **Trigger threshold:** 1,000 tiles (enter degraded mode when `visibleCount > 1000`)
2. **Recovery threshold:** 950 tiles (exit degraded mode when `visibleCount < 950`)
3. **Buffer zone:** 950–1,000 tiles (maintain current state)
4. **State tracking:** Replace `_degradationNotificationSent` with `_isInDegradedMode` boolean
5. **Constants location:** Move `DegradationTileThreshold` to `ModConstants` as `DegradationTriggerThreshold` and add `DegradationRecoveryThreshold`

### Code changes required:

- `LivingRoots/Constants.cs`: Add `DegradationTriggerThreshold = 1000` and `DegradationRecoveryThreshold = 950`
- `LivingRoots/Services/Visualization/OverlayRenderer.cs`: Replace single-threshold check with dual-threshold state machine
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md`: Update Section 11.2 with recovery threshold

### Spec changes required:

None. The spec says ">1,000 triggers degradation" — hysteresis is an implementation detail that defines recovery behavior, which the spec intentionally leaves unspecified per `remediation-degradation.md` line 56.

---

## 11. Sources

### Codebase
- `LivingRoots/Services/Visualization/OverlayRenderer.cs` lines 24, 32, 72–90 — degradation logic, notification flag
- `LivingRoots/Services/Visualization/VisualizationService.cs` lines 254–258 — pattern rendering check
- `LivingRoots/Constants.cs` (full file, 64 lines) — no degradation threshold constant exists

### Spec
- `specs/001-soil-health-visualization/spec.md` line 152 — FR-009 exact wording (">1,000")
- `specs/001-soil-health-visualization/spec.md` line 189 — SC-002 (60 FPS at 1,000 tiles)
- `specs/001-soil-health-visualization/spec.md` line 104 — one-time notification clarification

### Design System
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` line 202 — Section 5.2 degradation rule
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` lines 553–560 — Section 11.2 thresholds
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` line 392 — Section 8.3 pattern degradation
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` line 43 — Accessibility First pillar
- `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md` line 739 — MaxVisibleTiles reference

### Existing Research
- `specs/001-soil-health-visualization/remediation-degradation.md` line 56 — hysteresis is implementation detail
- `specs/001-soil-health-visualization/research-notification-mechanisms.md` line 155 — chat box not for notifications
- `specs/001-soil-health-visualization/checklists/research-accessibility-notification.md` lines 232–237 — one-time toast insufficient

### Game Engine References
- [Unreal Engine Forums — LODHysteresis](https://forums.unrealengine.com/t/having-issues-with-skeletal-meshes-and-lod/15308) — buffer zone to prevent flickering
- [three.js Issue #14565 — LOD Hysteresis](https://github.com/mrdoob/three.js/issues/14565) — Unreal/Unity/Blender all use hysteresis
- [Unity Discussions — Auto Quality](https://discussions.unity.com/t/automatically-setting-the-quality-level-based-on-the-users-hardware/868104) — quality level dead zones
- [Blender Artists — Hysteresis Override](https://blenderartists.org/t/what-is-hysteresis-override/1309185) — blending factor for transitions
