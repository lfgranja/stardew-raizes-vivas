# Remediation Evidence — Soil Health Visualization (001)

**Sources read:** `specs/001-soil-health-visualization/spec.md`, `data-model.md`, `plan.md`.  
**Canonical earth-tone ref (Clarification 127, spec.md:127):** Poor `#B91C1C`, Moderate `#D97706`, Healthy `#15803D`, Unknown `#6B7280`, Opacity `0.5`, White patterns `0.7` (min). WCAG 3:1 MUST holds (defaults verify 6.47:1 / 3.19:1 / 5.02:1 vs white patterns).

---

## A1 — FR-009 Degradation / Accessibility / Notification

**Primary source (FR-009 definition):**
- `spec.md:189-190` — FR-009 full text (visible-tile >1,000 threshold, immediate toggle, `auto`/`never`/`notify`, layered notification: chat + HUDindicator at top-right via `Display.Rendered`, patterns disable, color/opacity stay active, split-screen `PerScreen<bool>`).
- `spec.md:183-184` (FR-003 parallel): flash renders world-layer; floating text above HUD — consistent with FR-009's `Display.Rendered` choice for HUD indicator.

**Clarifications driving remediation (spec.md lines cited):**
- `spec.md:122-128` (session 2026-09-13): clarification 127 (canonical palette) resolved comprehensive-gate CHK061 / rendering CHK013 / config CHK024.
- `spec.md:127` — earth tones canonical (`#B91C1C`/`#D97706`/`#15803D`/unknown `#6B7280`, opacity `0.5`). Confirms degradation mode colors remain those defaults.
- `spec.md:140-141` — degradation event defined as upward crossing `≤1,000 → >1,000`; notification fires once per crossing; resets when `≤1,000`. Matches FR-009's `NotificationSent` flag logic (`spec.md:197-201`).
- `spec.md:142` — split-screen: only exceeding player degrades (`PerScreen<T>`); FR-009 `PerScreen<bool>` confirmed.
- `spec.md:153` — accessibility patterns: white, opacity `0.7-1.0` (min 0.7 per FR-001). FR-009 requires patterns disable in `auto` at >1,000; minimum visibility floor preserved when patterns ON.
- `spec.md:151` — notification delivered via SMAPI chat box (not custom toast); persistent HUD icon (top-right, configurable via GMCM per `spec.md:152` / `spec.md:191` line 189).

**Remediation note:** FR-009 is fully specified; no spec contradiction. Implementation must honor `Display.Rendered` for HUD indicator (`spec.md:104` clarification 4, `spec.md:189` FR-009), disable patterns only when `auto` + count>1,000, keep interpolation and opacity active, and reset `NotificationSent` on down-crossing.

---

## A2 — Palette Drift Check (Data Model vs. Canonical)

**Canonical from clar 127 (`spec.md:127`):** Poor `#B91C1C` · Moderate `#D97706` · Healthy `#15803D` · Unknown `#6B7280` · Opacity `0.5` · White patterns `0.7` (min).

**Data-model reference (`data-model.md` lines cited):**
- `data-model.md:17` — `Opacity | float | 0.5` ✓ matches.
- `data-model.md:18-21` — `PoorColor #B91C1C`, `ModerateColor #D97706`, `HealthyColor #15803D`, `UnknownColor #6B7280` ✓ all match.
- `data-model.md:82-86` (HealthCategory defaults) — Red `#B91C1C`, Amber `#D97706`, Green `#15803D`, Slate gray `#6B7280`; pattern column (Stripes/Dots/Solid/None) aligns with FR-001 / clar 153.
- `data-model.md:135` — `Pattern opacity: Minimum 0.7 regardless of overlay opacity setting` ✓ matches clar 127 white-pattern `0.7`.
- `data-model.md:134` — Pattern color `White` (maximizes contrast) ✓ matches clar 153 / FR-001.

**Finding:** **No drift / no conflict.** `data-model.md` is already aligned to earth-tone canonical. Earlier brighter palette (`#FF0000/#FFFF00/#00FF00`, unknown `#808080`, opacity `0.3`) referenced in clarification 127 (`spec.md:127`) as the rejected alternative — not present in `data-model.md`.

**Cross-check vs. older spec references:**
- `spec.md:158-159` (session 2026-09-08): single gray overlay for both loading/no-data (`#808080` mentioned as older/neutrally referenced); clarification 127 supersedes with `#6B7280` for unknown. `data-model.md:21` uses `#6B7280` — correct.
- `spec.md:105-111` (session 2026-09-10): unknown flash `#808080` → corrected to `#6B7280` per clar 133 / 135 (`spec.md:133` uses `#808080` only in the Q's old wording; answer says `#6B7280`). `data-model.md:21` / `data-model.md:183` confirm `#6B7280`.

---

## A3 / User Stories & Edge Cases (spec.md:13-81, 83-95)

**User stories (spec.md:13-81):** US-01 overlay (P1), US-02 tooltip (P2), US-03 hoe feedback (P2), US-04 config (P3). All reference color categories; none contradict clar 127. US-03 acceptance 5 (`spec.md:61`) — rapid hoe restarts timer; matches `spec.md:146` clar 146 / `data-model.md:184`.

**Edge cases (spec.md:83-95):** 11 items covering mid-render change, tooltip flicker, viewport resize, viewport-edge tiles, save-during-render, concurrent save/load, boundary values, warp, menu-open, rapid hoe, missing cache entry. Each maps to a requirement:
- missing cache → FR-014 (`spec.md:202`) gray `#6B7280`; clar 105 (`spec.md:105`) confirms unknown treatment.
- rapid hoe → FR-003 / `spec.md:61`; timer restart.
- boundary at 33 → half-open `[0,34), [34,67), [67,100]`; clar 147 (`spec.md:147`) fixes interpolation divisor 33→34; `data-model.md:87` confirms.

---

## A4 — FR-003 Color / Flash Contradiction (Resolved)

**Source lines:**
- `spec.md:183-184` (FR-003): "flash effect (colored overlay flash at the tile's health category color...) ... For tiles where ... unavailable ... flash color MUST use configured 'unknown' color (default `#6B7280`)"; floating text "Soil Health: Unknown" (no %).
- `spec.md:133` (clar session 2026-09-11): Q — "white or colored at health category?" A — colored overlay flash (`#808080` old → `#6B7280` corrected for unknown); white floating text + dark shadow/outline remains; `HoeFeedbackRenderer.cs:65`, `VisualizationService.cs:366` confirm.
- `spec.md:111` (session 2026-09-10): unknown tile = gray flash `#808080` (old); corrected by clar 133 to `#6B7280`. Only text formatting needed fix (avoid "0% (Unknown)").

**Contradiction status:** Original spec contradicted itself (white vs category-colored flash); clarification 133 resolves to **category-colored flash** (unknown = `#6B7280`). `data-model.md:181-183` confirms: flash colored overlay alpha 1→0 (300ms), unknown = configured unknown color (`#6B7280`), text = "Soil Health: Unknown".

**Canonical colors applied to FR-003:** Poor flash `#B91C1C`, Moderate `#D97706`, Healthy `#15803D`, Unknown `#6B7280`; floating text always white with dark outline (`spec.md:101-103`, `spec.md:153` white patterns extend to text readability principle).

---

## Constitution Check — Plan.md (Lines 31-55)

**Source:** `plan.md:31-55` (Constitution Check pre + post Phase 1).
- `plan.md:35-43` — Gate pre: Principles I–V all PASS; explicit: "No violations detected."
- `plan.md:47-55` — Post-design re-evaluation: I DDD PASS, II Security PASS, III Async-Only PASS (lock-free atomics for game-loop; async/await only for true I/O; no `.Result`/`.Wait()`), IV TDD PASS, V YAGNI PASS (only 3 interfaces).
- `plan.md:43` — Gate result: PASS.
- `plan.md:55` — Re-evaluation: PASS — aligns with all constitutional principles.

**Finding:** **No MUST violation.** All 5 constitutional principles (DDD, Security-First, Async-Only, TDD, Simplicity/YAGNI) pass at both gate and post-design re-check (`plan.md:43`, `plan.md:55`). No hardcoded constants outside `ModConstants` (per AGENTS.md conventions; design uses `VisualizationConfiguration` DTO with defaults from `data-model.md` — consistent).

---

## Summary of Remediation Actions Needed

1. **FR-009 (A1):** Verify implementation renders HUD degradation indicator via `Display.Rendered` (`spec.md:104`, `spec.md:189`), disables patterns only in `auto` at >1,000, resets notification flag on down-crossing, respects `PerScreen<bool>` in split-screen. No spec change needed — clarification 127/153/140-142 fully specify.
2. **FR-003 (A4):** Ensure flash uses category-colored (not white) per clar 133 (`spec.md:133`); unknown = `#6B7280`; floating text white + shadow, "Soil Health: Unknown" without % (`spec.md:183-184`, `data-model.md:181-183`).
3. **Palette (A2):** Confirm `data-model.md` defaults remain earth-tone (`#B91C1C`, `#D97706`, `#15803D`, `#6B7280`, opacity `0.5`). No file edits to `data-model.md` required — already aligned to clar 127.
4. **Constitution:** No violation (`plan.md:43`, `plan.md:55`). Continue with Phase 2 (tasks.md) without constitutional blocker.

**Canonical earth-tone values cited throughout:** Poor `#B91C1C` · Moderate `#D97706` · Healthy `#15803D` · Unknown `#6B7280` · Opacity `0.5` · White patterns `0.7` (min). Source: `spec.md:127`, backed by `data-model.md:17-21,82-86,135`.

---
*Evidence file generated from primary sources only; no other repo files modified.*
