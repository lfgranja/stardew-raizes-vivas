# Research: Accessibility Notification Patterns in Games

**Feature**: Soil Health Visualization (001-soil-health-visualization)
**Date**: 2026-09-08
**Purpose**: Resolve spec ambiguity in FR-009 — what should the `notify` mode "performance warning" look like? Determine best-practice notification patterns for accessibility feature degradation.

---

## 1. What are common patterns for notifying players when accessibility features are reduced in games?

### Finding: Five dominant patterns exist, each with different accessibility trade-offs.

#### 1.1. Toast Notifications (Small, auto-dismissing pop-ups)

The most common pattern in web and mobile games. Small rectangular notifications that appear (usually bottom-left or bottom-right) and disappear after a preset time.

**Examples**: Stardew Valley's own farm event notifications appear in the bottom-left corner. Many mobile games use toasts for state changes.

**Accessibility concerns**: GitHub's Primer accessibility team explicitly recommends **against** toasts because they:
- Auto-dismiss before slow readers can consume them (WCAG 2.2.1 Timing Adjustable)
- Are often placed at DOM extremes, breaking meaningful sequence for screen readers (WCAG 1.3.2)
- May appear outside screen magnification viewports
- Can be unnoticed on large displays
- Create "banner blindness" through over-use
- Risk being unread if the user is distracted or alt-tabbed

> **Source**: "Toasts pose significant accessibility concerns and are not recommended for use... GitHub recommends using other more established, effective, and accessible ways of communicating with users." — [Primer: Accessible notifications and messages](https://primer.style/accessibility/patterns/accessible-notifications-and-messages/)

**Verdict for FR-009**: Toast alone is insufficient for an accessibility-related notification. If used, it must be paired with a persistent or semi-persistent element.

#### 1.2. Banners (Persistent, dismissible strips)

Horizontal strips that persist until manually dismissed. GitHub and other platforms use banners for important state changes like session desynchronization or long-running task completion.

**Advantages over toasts**: Persist until dismissed (no timing barrier), can be placed in-DOM near relevant content (meaningful sequence), clearly visible.

**Example**: GitHub uses banners for application state changes and form submission feedback.

> **Source**: "Banners are useful when the error information needs to be passively available... Both approaches persist feedback information and do not auto-dismiss it." — [Primer: Accessible notifications and messages](https://primer.style/accessibility/patterns/accessible-notifications-and-messages/)

**Verdict for FR-009**: A banner is a strong candidate — it persists until the player acknowledges it, satisfying WCAG 2.2.1 (Timing Adjustable).

#### 1.3. Chat / Log Messages (In-game messaging channel)

A message in the game's chat or log window. In Stardew Valley, this would be the SMAPI console or a chat-like HUD message.

**Advantages**: Visible to the player, can include actionable hints ("re-enable in config"), persists in scrollback, familiar to the player.

**Example**: The remediation doc already recommends: *"Display a chat message (visible to player): 'Soil health patterns disabled for performance. Enable "Always show patterns" in config to keep them.'"*

**Verdict for FR-009**: Chat message is a good secondary channel — it provides the "what happened + what to do" context that a toast cannot.

#### 1.4. Loading Screen / Interstitial Tips

Notifications shown on loading screens or between gameplay sessions. Used by several AAA games to highlight accessibility features.

**Examples from Game Accessibility Guidelines**:
- **Battlefield Hardline**: Loading screen tips that highlight accessibility features
- **Two Dots**: Prompt for colourblind mode displayed when the game first loads, before the main menu
- **Nier Automata**: Simulation sickness prompt shown on first load

> **Source**: "Showing the menu itself before the game starts. Building them into the initial tutorial... Highlighting features on initial and interstitial loading screens. Even prompting players about them if it looks like they might be having difficulty." — [Game Accessibility Guidelines: Provide details of accessibility features in-game](https://gameaccessibilityguidelines.com/provide-details-of-accessibility-features-in-game/)

**Verdict for FR-009**: Not suitable for runtime degradation (degradation happens mid-gameplay, not at load), but relevant for initial signposting of the `accessibilityDegradation` config option.

#### 1.5. Persistent HUD Indicators (Icons, status badges)

A small icon or badge that remains on-screen while the degraded state is active. For example, a small "accessibility reduced" icon in the corner of the HUD.

**Advantages**: Continuously reminds the player that accessibility features are reduced; no timing barrier; always visible.

**Disadvantages**: Can cause "banner blindness" if always present; takes screen real estate; may be confusing without explanation on first appearance.

**Verdict for FR-009**: A persistent indicator paired with a one-time explanation is the most accessible combination — the indicator reminds, the one-time message explains.

---

## 2. What does WCAG 1.4.1 say about notifying users when accessibility aids are reduced?

### Finding: WCAG 1.4.1 does not explicitly require notification — but its intent, combined with WCAG 4.1.3, creates an implicit requirement.

#### 2.1. WCAG 1.4.1 — Use of Color (Level A)

The full text of the success criterion:

> **1.4.1 Use of Color**: Color is not used as the only visual means of conveying information, indicating an action, prompting a response, or distinguishing a visual element. (Level A)

**Key notes**:
- "This success criterion addresses color perception specifically. Other forms of perception are covered in Guideline 1.3 including programmatic access to color and other visual presentation coding."
- The intent is to ensure all sighted users can access information conveyed by color differences.

**Technique G14**: "Ensuring that information conveyed by color differences is also available in text."

> **Source**: [W3C, Understanding SC 1.4.1: Use of Color](https://www.w3.org/WAI/WCAG21/Understanding/use-of-color.html)
> **Source**: [DigitalA11Y, Understanding SC 1.4.1 Use of Color](https://www.digitala11y.com/understanding-sc-1-4-1-use-of-color/)
> **Source**: [WCAG.com, 1.4.1 Use of Color (Level A)](https://www.wcag.com/designers/1-4-1-use-of-color/)

#### 2.2. The notification gap in WCAG 1.4.1

WCAG 1.4.1 itself says nothing about notifying users when color-only mode is active. It is a **design-time** criterion: "don't use color alone." It does not address runtime degradation scenarios where accessibility aids are dynamically removed.

However, the **intent** of the criterion — ensuring users have access to information — implies that if a system removes a non-color indicator (pattern) and falls back to color-only, the user should be aware of this change so they can take action (e.g., reduce zoom level, change config).

#### 2.3. WCAG 4.1.3 — Status Messages (Level AA)

This is the criterion that directly addresses notification:

> **4.1.3 Status Messages**: In content implemented using markup languages, status messages can be programmatically determined through role or properties such as `role="status"` or `role="alert"` such that the message can be presented to the user by assistive technologies without receiving focus.

**Relevance to FR-009**: When degradation activates, the notification is a **status message** — it informs the user of a change in system state. Per WCAG 4.1.3, this should be:
- Programmatically announced to assistive technologies (screen readers)
- Available without stealing focus from gameplay
- Coded with appropriate ARIA roles (`role="status"` for polite, `role="alert"` for assertive)

> **Source**: [W3C, Understanding SC 4.1.3: Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
> **Source**: [WCAG.com, 4.1.3 Status Messages (Level AA)](https://www.wcag.com/developers/4-1-3-status-messages/)

#### 2.4. WCAG 2.2.1 — Timing Adjustable (Level A)

If a notification auto-dismisses, it must satisfy:

> **2.2.1 Timing Adjustable**: For each time limit that is set by the content, at least one of the following is true: Turn off, Adjust, or Extend.

**Implication**: A toast that auto-dismisses after 5 seconds violates WCAG 2.2.1 unless the user can extend, adjust, or turn off the timing.

---

## 3. What do the Game Accessibility Guidelines recommend for this scenario?

### Finding: Player agency and signposting are the core principles; notification is the recommended mechanism.

#### 3.1. "Provide details of accessibility features in-game" (General, Basic)

This guideline emphasizes that players often don't know to look for accessibility features. Recommended techniques include:
- Showing the menu before the game starts
- Building features into the initial tutorial
- Highlighting features on loading screens
- **Prompting players if it looks like they might be having difficulty**

> **Source**: [Game Accessibility Guidelines: Provide details of accessibility features in-game](https://gameaccessibilityguidelines.com/provide-details-of-accessibility-features-in-game/)

#### 3.2. "Ensure no essential information is conveyed by a fixed colour alone" (Vision, Basic)

This is the game-specific counterpart to WCAG 1.4.1. It is listed as a **Basic** (highest priority) requirement.

> **Source**: [Game Accessibility Guidelines: Ensure no essential information is conveyed by a fixed colour alone](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/)

#### 3.3. Xbox Accessibility Guideline 103 — Additional Channels for Visual and Audio Cues

Microsoft's XAG 103 requires:

> "At least one additional signifier such as shape, pattern, iconography, or text labels."

This is directly relevant: when patterns are disabled, the system loses its additional signifier. The XAG framework implies that the player should be informed when this happens.

> **Source**: [Microsoft, Xbox Accessibility Guidelines](https://learn.microsoft.com/en-us/xbox/accessibility/guidelines)

#### 3.4. Xbox Accessibility Guideline 115 — Error Messages and Destructive Actions

XAG 115 covers how to communicate errors and destructive state changes. While degradation isn't strictly an "error," the communication principles apply: the player must be informed when the system takes an action that affects their experience.

> **Source**: [Microsoft, Xbox Accessibility Guidelines](https://learn.microsoft.com/en-us/xbox/accessibility/guidelines)

#### 3.5. Game Accessibility Guidelines — Best Practice Examples

The following examples demonstrate the recommended pattern of **proactive notification**:

| Game | Pattern | Context |
|------|---------|---------|
| **Two Dots** | Pre-menu prompt | Colourblind mode prompt shown before main menu |
| **Battlefield Hardline** | Loading screen tips | Accessibility features highlighted on loading screens |
| **Nier Automata** | Interstitial prompt | Simulation sickness warning on first load |

> **Source**: [Game Accessibility Guidelines: Provide details of accessibility features in-game](https://gameaccessibilityguidelines.com/provide-details-of-accessibility-features-in-game/)

---

## 4. What's the best practice: one-time toast, persistent indicator, or per-session notification?

### Finding: A layered approach combining a one-time visible notification with a persistent status indicator is the most accessible and most aligned with guidelines.

#### 4.1. Analysis of each option

| Option | WCAG Alignment | Player Experience | Risk |
|--------|---------------|-------------------|------|
| **One-time toast** | Fails 2.2.1 (timing), 1.3.2 (sequence), 4.1.3 (status) if used alone | Familiar, non-intrusive | Unread, unnoticed, no review path |
| **Persistent indicator** | Passes 2.2.1 (no timing), 4.1.3 (if coded as status) | Always visible, may cause confusion on first appearance | Banner blindness, screen clutter |
| **Per-session notification** | Passes 2.2.1 if dismissible, partial 4.1.3 | Balances awareness with non-intrusiveness | May be missed if session is long |

#### 4.2. Recommended pattern: Layered notification

Based on the research, the best practice for FR-009's `notify` mode is a **three-layer notification**:

1. **One-time visible notification** (toast or chat message) — immediately alerts the player when degradation activates. Must be dismissible and not auto-dismiss too quickly (or not at all) to satisfy WCAG 2.2.1.

2. **Persistent status indicator** (small HUD icon/badge) — remains visible while degraded state is active, reminding the player that accessibility patterns are currently disabled. Must be introduced/explained by the first notification to avoid confusion.

3. **Log message** (via IMonitor) — for debugging and accessibility audit trails. Already required by FR-009.

This layered approach satisfies:
- **WCAG 1.4.1**: Player is informed that the non-color indicator (pattern) is no longer active
- **WCAG 4.1.3**: Status change is communicated (via the visible notification coded as a status message)
- **WCAG 2.2.1**: Notification persists until dismissed (no timing barrier)
- **Game Accessibility Guidelines**: Player agency — the player knows what happened and can act
- **XAG 103/115**: Additional channel for communicating state changes

#### 4.3. Concrete recommendation for FR-009

When degradation activates in `auto` mode (or when FPS is low in `notify` mode):

```
[Layer 1 - One-time toast/chat]:
  "Soil health patterns disabled for performance.
   [Patterns will return when zoomed out or tile count drops.]
   [Config: accessibilityDegradation → 'never' to always show patterns.]"

[Layer 2 - Persistent indicator]:
  Small icon (e.g., striped square with a slash) in a corner of the HUD
  while patterns are disabled. Hover tooltip: "Accessibility patterns
  disabled for performance. Click for details."

[Layer 3 - Log]:
  Monitor.Log("Accessibility patterns disabled: tile count exceeded 1,000 threshold.", LogLevel.Info)
```

#### 4.4. Why not just a one-time toast?

The remediation doc already identifies this gap:

> "This should be enhanced to: 1. Log (for debugging), 2. Display a chat message (visible to player), 3. Show a one-time toast notification (non-intrusive but visible)"

A one-time toast alone is the **minimum viable** notification but is insufficient for accessibility because:
- It can be missed (distraction, alt-tab, screen magnification)
- It cannot be reviewed after dismissal
- It violates WCAG 2.2.1 if it auto-dismisses
- It provides no ongoing awareness that the accessibility aid is still disabled

The persistent indicator solves the "ongoing awareness" problem; the one-time notification solves the "what happened and what to do" problem.

---

## 5. Spec Recommendation

### For FR-009 `notify` mode:

The spec should be clarified to require:

1. **One-time notification** when degradation first activates (toast or chat message, dismissible, not auto-dismissing within <10 seconds)
2. **Persistent HUD indicator** while degradation is active (small icon with tooltip)
3. **Log message** for debugging (already required)
4. **Notification content** must include: what happened (patterns disabled), why (performance), and what to do (config option or zoom out)

### For FR-009 `auto` mode:

The same notification pattern applies when patterns are auto-disabled, since the player is still losing an accessibility aid and must be informed per WCAG 1.4.1 intent and Game Accessibility Guidelines.

---

## Sources

- [W3C, Understanding SC 1.4.1: Use of Color](https://www.w3.org/WAI/WCAG21/Understanding/use-of-color.html)
- [W3C, Understanding SC 4.1.3: Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
- [W3C, Understanding SC 2.2.1: Timing Adjustable](https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable.html)
- [DigitalA11Y, Understanding SC 1.4.1 Use of Color](https://www.digitala11y.com/understanding-sc-1-4-1-use-of-color/)
- [WCAG.com, 1.4.1 Use of Color (Level A)](https://www.wcag.com/designers/1-4-1-use-of-color/)
- [WCAG.com, 4.1.3 Status Messages (Level AA)](https://www.wcag.com/developers/4-1-3-status-messages/)
- [Game Accessibility Guidelines — Full List](https://gameaccessibilityguidelines.com/full-list/)
- [Game Accessibility Guidelines: Provide details of accessibility features in-game](https://gameaccessibilityguidelines.com/provide-details-of-accessibility-features-in-game/)
- [Game Accessibility Guidelines: Ensure no essential information is conveyed by a fixed colour alone](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/)
- [Primer: Accessible notifications and messages](https://primer.style/accessibility/patterns/accessible-notifications-and-messages/)
- [Microsoft, Xbox Accessibility Guidelines](https://learn.microsoft.com/en-us/xbox/accessibility/guidelines)
- [GitHub, Why GitHub's War On Toasts Is Bad News For Accessibility](https://medium.com/offmessageorg/why-githubs-war-on-toasts-is-bad-news-for-accessibility-a88ddbad43b7)
- [Game Accessibility Guidelines: Block colourblind mode prompt](https://gameaccessibilityguidelines.com/block-colourblind-mode-prompt/)
