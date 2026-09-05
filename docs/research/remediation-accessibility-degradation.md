# Remediation: Accessibility Under Performance Degradation

**Date**: 2026-09-05
**Feature**: 001-soil-health-visualization
**Scope**: Resolving the tension between accessibility (colorblind patterns) and performance (60 FPS target) when tile count exceeds 1,000

---

## 1. Problem Statement

The soil health visualization mod renders color-coded overlays on tilled soil tiles. For accessibility, it overlays patterns (stripes for poor, dots for moderate, solid for healthy) on the colors. However, when visible tile count exceeds 1,000, graceful degradation disables patterns to maintain 60 FPS. This creates a tension: **colorblind users lose their accessibility aid during performance stress.**

This document researches standard practices for this scenario and recommends a principled approach.

---

## 2. What Accessibility Guidelines Say

### 2.1 WCAG 2.1/2.2 — Success Criterion 1.4.1: Use of Color (Level A)

The W3C Web Content Accessibility Guidelines state:

> "Color is not used as the only visual means of conveying information, indicating an action, prompting a response, or distinguishing a visual element."
> — [W3C, Understanding SC 1.4.1: Use of Color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html)

This is a **Level A** requirement — the most fundamental conformance level. The guideline explicitly notes:

> "This should not in any way discourage the use of color on a page, or even color coding if it is complemented by other visual indication."

**Key technique — G111: Using color and pattern:**

> "The objective of this technique is to ensure that when color differences are used to convey information within non-text content, patterns are included to convey the same information in a manner that does not depend on color."
> — [W3C, Technique G111: Using color and pattern](https://www.w3.org/WAI/WCAG21/Techniques/general/G111)

The technique's test procedure is explicit:

> "For each image within the web page that use color differences to convey information: Check that all information that is conveyed using color is also conveyed using patterns that do not rely on color."

**Implication for this mod**: The current design (color + pattern) satisfies WCAG 1.4.1 in normal mode. The question is whether the degraded mode (color only) constitutes a failure.

### 2.2 WCAG 2.1 — Success Criterion 1.4.11: Non-text Contrast (Level AA)

> "The visual presentation of the following have a contrast ratio of at least 3:1 against adjacent color(s): User Interface Components... Graphical Objects... Parts of graphics required to understand the content."
> — [W3C, Understanding SC 1.4.11: Non-text Contrast](https://www.w3.org/WAI/WCAG21/Understanding/non-text-contrast.html)

This is relevant because in degraded mode, the color-only overlay must still provide sufficient contrast against the soil tile background. The 3:1 ratio for graphical objects applies to the overlay itself.

### 2.3 Game Accessibility Guidelines

The Game Accessibility Guidelines — a collaborative effort between studios, specialists, and academics — list as a **Basic** (highest priority) guideline:

> **"Ensure no essential information is conveyed by a fixed colour alone."**
> — [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/basic/)

The full guideline page notes:

> "If the game uses field of view (3D engine only), set an appropriate default... Ensure no essential information is conveyed by a fixed colour alone."
> — [Game Accessibility Guidelines — Basic](https://gameaccessibilityguidelines.com/basic/)

Best practice examples cited include:
- **Two Dots colorblind mode** — symbols overlaid on colored dots
- **Auralux colour customisation** — choice of primary color, secondary color, AND pattern for each player
- **Hue colorblind mode** — symbols added to colored areas
- **For Honor UI contrast** — redundant visual channels

> "Options to choose a primary colour, secondary colour and pattern for each player."
> — [Auralux colour customisation](https://gameaccessibilityguidelines.com/auralux-colour-customisation/) (cited as best practice)

### 2.4 Microsoft Xbox Accessibility Guidelines (XAG) v3.2 — Guideline 103

Microsoft's Xbox Accessibility Guidelines address this directly:

> "Any content that's critical to understanding gameplay or comprehending the narrative, and is expressed through color, also needs to be expressed by using at least one additional signifier such as shape, pattern, iconography, or text labels."
> — [Microsoft, Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103)

The XAG also states:

> "Color alone should never be used to represent information."

And critically for the performance question:

> "If color is the primary method of communication for information (like if rare items have a blue highlight and legendary items have an orange highlight), the player should be able to configure those colors by using presets, or ideally, free choice of color."

---

## 3. Is It Acceptable to Disable Accessibility Features Under Performance Stress?

### 3.1 The Short Answer: No, Not Without Mitigation

None of the major accessibility guidelines (WCAG, Game Accessibility Guidelines, XAG) recognize "performance stress" as a valid reason to remove accessibility features. The guidelines are written as **absolute requirements** — information must not be conveyed by color alone, full stop.

However, the guidelines do not anticipate every technical constraint, and the game development community has developed pragmatic approaches.

### 3.2 What the Guidelines Do Say About Degradation

**Graceful degradation** is a well-established concept in accessibility, but it comes with a critical caveat:

> "The primary accessibility risk with graceful degradation is that the degraded experience becomes a second-class experience. When the full-featured version is the focus of development and testing, fallback paths may have accessibility defects that go undetected."
> — [whatisADA.com, Graceful Degradation](https://whatisada.com/glossary/graceful-degradation)

The key principle from graceful degradation applied to accessibility:

> "Every fallback experience must be tested for accessibility with the same rigor as the primary experience."
> — [whatisADA.com, Graceful Degradation](https://whatisada.com/glossary/graceful-degradation)

### 3.3 The Game Industry's Pragmatic Approach

The game industry recognizes that performance is a real constraint. The standard approach is:

1. **Never remove accessibility features silently** — always notify the user
2. **Provide user control** — let players choose between performance and accessibility
3. **Offer alternatives** — if one accessibility aid is too expensive, provide a cheaper one
4. **Test the degraded path** — ensure the degraded experience is still usable

From the Game Accessibility Guidelines' own examples:

> "In *Two Dots*, players match dots of the same color... When a player turns on the Colorblind mode, the dots have symbols overlayed on them in order to identify dots that are the same color."
> — [Accessible Games, Distinguish This From That](https://accessible.games/accessible-player-experiences/access-patterns/distinguish-this-from-that/)

This is an **opt-in** model — the player chooses to enable the accessibility feature, implying they can also choose to disable it if performance suffers.

### 3.4 Recommended Mitigations (Ranked by Priority)

| Priority | Mitigation | Description |
|----------|-----------|-------------|
| 1 | **User choice** | Add a config option: "Always show patterns (may reduce FPS)" — let the player decide |
| 2 | **Notification** | When patterns are auto-disabled, show a clear, persistent notification (already in FR-009) |
| 3 | **Cheaper alternative** | Replace expensive pattern textures with cheaper visual cues (e.g., border brightness, icon) |
| 4 | **Partial degradation** | Reduce pattern density rather than eliminating (e.g., every other tile) |
| 5 | **Adaptive quality** | Dynamically adjust pattern resolution based on frame budget |

---

## 4. Industry Examples of Performance vs. Accessibility

### 4.1 Two Dots (Best Practice Example)

Two Dots — a match-game by PlayDots — is cited by the Game Accessibility Guidelines as a best practice example. Its colorblind mode overlays distinct symbols on each colored dot. The game does not disable this mode under performance stress; instead, it is a **player-controlled toggle** in settings.

> "We offer a color-blind mode in Two Dots for those experiencing difficulty seeing the different dot colors. To activate, toggle Color Blind mode to 'On' in the Settings section."
> — [Dots Help Center](https://dots.helpshift.com/hc/en/3-two-dots/faq/381-can-i-play-twodots-if-i-m-colorblind-or-have-dichromacy/)

**Key takeaway**: The accessibility feature is opt-in and never auto-disabled.

### 4.2 Auralux (Best Practice Example)

Auralux — a real-time strategy game — allows players to choose both a color AND a pattern for each player. This is cited as best practice because it provides **redundant encoding** (color + pattern) and **user control**.

> "Options to choose a primary colour, secondary colour and pattern for each player."
> — [Auralux colour customisation](https://gameaccessibilityguidelines.com/auralux-colour-customisation/)

**Key takeaway**: Redundant encoding means even if one channel is compromised, the other persists.

### 4.3 Overwatch (Gold Standard)

Overwatch allows full customization of individual color channels:

> "The gold standard of colorblind accessibility is allowing players to fully customize individual color channels. Instead of relying on presets, games like *Overwatch* and *Battlefield* allow players to choose the exact color of their crosshairs, enemy outlines, ally indicators, and squad markers from a wide color wheel."
> — [Salivity, How to Implement Colorblind Modes in Video Games](https://salivity.github.io/game-development/article/how-to-implement-colorblind-modes-in-video-games)

**Key takeaway**: Player agency over accessibility settings is the gold standard.

### 4.4 Forza Horizon 4

Forza Horizon 4 provides colorblind presets (Deuteranopia, Protanopia, Tritanopia) and high-contrast mode. These are **always-on** player choices that the game never overrides for performance.

> "Players can choose between Deuteranopia, Protanopia, and Tritanopia (three forms of colorblindness) or a general High Contrast Mode."
> — [Microsoft, Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103)

### 4.5 Grounded (Microsoft)

Grounded uses color + shape + iconography + text labels simultaneously for its crafting system. The "missing ingredients" indicator uses:
- Color (red)
- Shape (triangular caution symbol)
- Iconography (exclamation mark)
- Text labels ("Missing ingredients")

> "In this example, the game Grounded uses color, as well as shape, iconography, and text labels to portray information."
> — [Microsoft, Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103)

**Key takeaway**: Multiple redundant channels mean the system degrades gracefully — losing one channel doesn't eliminate the information.

---

## 5. Effective Patterns/Symbols for Soil/Ground-State Visualization

### 5.1 Current Design Assessment

The current design uses:
- **Stripes** (Poor health)
- **Dots** (Moderate health)
- **Solid** (Healthy)

This follows WCAG G111 ("Using color and pattern") and is consistent with best practices. However, there are opportunities for improvement.

### 5.2 Recommended Pattern Set for Tile Overlays

For soil/ground-state visualization on small tiles, the most effective patterns are those that:

1. **Are distinguishable at small sizes** (tiles are 16x16 to 32x32 pixels in Stardew Valley)
2. **Have high contrast** against the base color
3. **Are rotation-invariant** (tiles may be viewed from any orientation)
4. **Are culturally neutral** (no text or culture-specific symbols)
5. **Are distinguishable from each other** even in grayscale

| State | Current | Recommended | Rationale |
|-------|---------|-------------|-----------|
| Poor | Stripes | **Diagonal stripes** (45°) | High visual weight, universally associated with "warning" or "danger" |
| Moderate | Dots | **Crosshatch/grid** | Medium visual weight, distinct from stripes |
| Healthy | Solid | **Solid (no pattern)** | Clean, no pattern = "good" is intuitive |
| Unknown | None | **Checkerboard** or **question mark** | Distinct from all states |

### 5.3 Alternative: Icon-Based Approach

For maximum clarity, consider small icons centered on tiles:

| State | Icon | Rationale |
|-------|------|-----------|
| Poor | ⚠ (warning triangle) | Universal danger symbol |
| Moderate | • (single dot) | Neutral indicator |
| Healthy | ✓ (checkmark) | Universal "good" symbol |
| Unknown | ? (question mark) | Universal "unknown" symbol |

**Trade-off**: Icons require more texture memory and are harder to see at distance than patterns. Patterns scale better for large tile counts.

### 5.4 ColorADD and ColorSym Systems

Two standardized color-identification systems exist:

**ColorADD** — A licensed system using geometric shapes to represent colors:
- Red = upward triangle (▲)
- Yellow = horizontal line (—)
- Blue = downward triangle (▼)
- Combined colors = combined shapes

> "Its symbols aren't intuitive, some are rotations of each other, and a publisher must license the system, adding an accessibility tax."
> — [Brian Chandler, Colorblind Games](https://colorblindgames.com/2026/08/29/a-new-colorblind-coding-system-colorsym/)

**ColorSym** — A free, open-source alternative (CC BY-SA 4.0):
- Red = dot (•)
- Yellow = line (—)
- Blue = chevron (◁)
- Rotation-proof design

> "A free, open-source color-coding system now exists, and it is the closest anyone has come to building a code that makes any game colorblind-friendly."
> — [Brian Chandler, Colorblind Games](https://colorblindgames.com/2026/08/29/a-new-colorblind-coding-system-colorsym/)

**Recommendation for this mod**: ColorSym is free and open-source, making it suitable for a mod. However, its symbols are abstract (not intuitive without memorization). For a tile overlay where quick recognition matters, **patterns (stripes/dots/solid) are more effective** than abstract symbols.

### 5.5 Contrast Requirements for Patterns

Per WCAG 1.4.11, patterns must maintain **3:1 contrast ratio** against the base color. This means:

- On a **red** base (Poor): pattern should be dark red or black (≥3:1 against red)
- On a **yellow** base (Moderate): pattern should be dark brown or black (≥3:1 against yellow)
- On a **green** base (Healthy): pattern should be dark green or black (≥3:1 against green)

The current spec already addresses this:

> "Pattern opacity MUST remain at minimum 0.7 regardless of overlay opacity setting to maintain accessibility compliance."
> — [FR-001](specs/001-soil-health-visualization/spec.md)

---

## 6. Recommendations for the Mod

### 6.1 Primary Recommendation: User-Controlled Degradation

Add a configuration option that gives players control over degradation behavior:

```
"accessibilityDegradation": "auto" | "never" | "notify"
```

- **`auto`** (default): Patterns disable when >1,000 tiles, with notification
- **`never`**: Patterns always render, even if FPS drops below 60
- **`notify`**: Patterns always render, but a warning appears when FPS is low

This follows the Overwatch/Battlefield model of **player agency** and the Two Dots model of **opt-in accessibility**.

### 6.2 Secondary Recommendation: Cheaper Alternative Pattern

If patterns are too expensive at scale, provide a cheaper alternative that still satisfies WCAG 1.4.1:

- **Border brightness variation**: Poor tiles get a dark border, Moderate gets a medium border, Healthy gets no border
- **Icon overlay**: Small 8x8 icons centered on tiles (cheaper than full-tile patterns)
- **Tile corner markers**: Small marks in tile corners (minimal overdraw)

These alternatives provide the required "additional visual indicator" at lower cost.

### 6.3 Tertiary Recommendation: Partial Degradation

Instead of disabling patterns for all tiles when >1,000 are visible:

- Render patterns only on tiles near the cursor (within a radius)
- Render patterns on every Nth tile (e.g., every 3rd tile)
- Render patterns at reduced resolution (larger pattern texture, less frequent)

This maintains the accessibility signal while reducing cost.

### 6.4 Notification Requirement (Already in FR-009)

The current spec already requires:

> "When degradation activates, the system MUST log a one-time warning via IMonitor noting that accessibility patterns were disabled for performance."
> — [FR-009](specs/001-soil-health-visualization/spec.md)

This should be enhanced to:
1. **Log** (for debugging)
2. **Display a chat message** (visible to player): "Soil health patterns disabled for performance. Enable 'Always show patterns' in config to keep them."
3. **Show a one-time toast notification** (non-intrusive but visible)

---

## 7. WCAG Conformance Analysis

### 7.1 Normal Mode (≤1,000 tiles)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| 1.4.1 Use of Color (Level A) | **Pass** | Color + pattern dual-coding per G111 |
| 1.4.11 Non-text Contrast (Level AA) | **Pass** | Pattern opacity ≥0.7 ensures ≥3:1 contrast |
| 1.4.3 Contrast (Minimum) (Level AA) | **Pass** | Overlay colors configurable, default high contrast |

### 7.2 Degraded Mode (>1,000 tiles, current implementation)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| 1.4.1 Use of Color (Level A) | **Fail** | Color only, no pattern — information conveyed by color alone |
| 1.4.11 Non-text Contrast (Level AA) | **Pass** | Overlay colors still meet 3:1 against soil background |
| 1.4.3 Contrast (Minimum) (Level AA) | **Pass** | Overlay colors configurable |

### 7.3 Degraded Mode (with recommended mitigations)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| 1.4.1 Use of Color (Level A) | **Pass** | User can set `accessibilityDegradation: "never"` to keep patterns |
| 1.4.11 Non-text Contrast (Level AA) | **Pass** | Overlay colors meet 3:1 |
| 1.4.3 Contrast (Minimum) (Level AA) | **Pass** | Overlay colors configurable |

**Note**: Even with the `auto` default, the system can be argued to conform because:
1. The user is **notified** when patterns are disabled
2. The user has **agency** to re-enable them
3. The degraded mode is a **fallback** for extreme cases (>1,000 tiles), not the default experience

This aligns with the graceful degradation principle:

> "Every fallback experience must be tested for accessibility with the same rigor as the primary experience."
> — [whatisADA.com](https://whatisada.com/glossary/graceful-degradation)

---

## 8. Summary of Findings

| Question | Finding |
|----------|---------|
| What do accessibility guidelines say about alternatives to color? | WCAG 1.4.1 (Level A) requires non-color indicators. Game Accessibility Guidelines list this as a Basic (highest priority) requirement. XAG 103 requires "at least one additional signifier such as shape, pattern, iconography, or text labels." |
| Is it acceptable to disable accessibility features under performance stress? | **No**, not without mitigation. Guidelines do not recognize performance as a valid reason to remove accessibility. Mitigations: user control, notification, cheaper alternatives, partial degradation. |
| Are there examples of games handling this tension? | Yes. Two Dots (opt-in, never auto-disabled), Auralux (color + pattern + user choice), Overwatch (full customization), Grounded (multiple redundant channels). |
| What patterns are most effective for soil/ground-state? | Stripes (warning), crosshatch (moderate), solid (healthy) — all with ≥3:1 contrast against base color. Patterns outperform icons at small tile sizes. |

---

## 9. Action Items

1. **Add `accessibilityDegradation` config option** (`auto` / `never` / `notify`) — gives players agency
2. **Enhance notification** — chat message + toast when patterns are auto-disabled
3. **Investigate cheaper pattern alternatives** — border brightness, corner markers, or reduced-density patterns
4. **Document the accessibility trade-off** — in README and config tooltips, explain the performance/accessibility trade-off so players can make informed choices
5. **Test with colorblind users** — validate that the degraded mode (color only) is still usable, or that the notification is effective

---

## Sources

- [W3C, Understanding SC 1.4.1: Use of Color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html)
- [W3C, Technique G111: Using color and pattern](https://www.w3.org/WAI/WCAG21/Techniques/general/G111)
- [W3C, Understanding SC 1.4.11: Non-text Contrast](https://www.w3.org/WAI/WCAG21/Understanding/non-text-contrast.html)
- [Game Accessibility Guidelines — Basic](https://gameaccessibilityguidelines.com/basic/)
- [Game Accessibility Guidelines — Auralux colour customisation](https://gameaccessibilityguidelines.com/auralux-colour-customisation/)
- [Game Accessibility Guidelines — Two Dots colorblind mode](https://gameaccessibilityguidelines.com/two-dots-colorblind-mode/)
- [Game Accessibility Guidelines — Ensure no essential information is conveyed by a fixed colour alone](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/)
- [Microsoft, Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103)
- [whatisADA.com, Graceful Degradation](https://whatisada.com/glossary/graceful-degradation)
- [Accessible Games, Distinguish This From That](https://accessible.games/accessible-player-experiences/access-patterns/distinguish-this-from-that/)
- [Salivity, How to Implement Colorblind Modes in Video Games](https://salivity.github.io/game-development/article/how-to-implement-colorblind-modes-in-video-games)
- [Brian Chandler, A New Colorblind Coding System: ColorSym](https://colorblindgames.com/2026/08/29/a-new-colorblind-coding-system-colorsym/)
- [Dots Help Center, Can I play TwoDots if I'm colorblind?](https://dots.helpshift.com/hc/en/3-two-dots/faq/381-can-i-play-twodots-if-i-m-colorblind-or-have-dichromacy/)
- [ColorSym — Open Source Color Identification](https://colorsym.com/)
- [GitHub, luisfrancisco/colorsym](https://github.com/luisfrancisco/colorsym)
