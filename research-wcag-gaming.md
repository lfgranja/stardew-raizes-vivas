# WCAG 2.1 AA Compliance for Video Game Mods: A Critical Analysis

**Date:** 2026-09-09
**Subject:** Evaluating whether WCAG 2.1 AA is an appropriate target for a Stardew Valley mod design system

---

## Executive Summary

Claiming WCAG 2.1 AA compliance as a target for a Stardew Valley mod design system is **inappropriate as stated**, though not without some conceptual merit. WCAG 2.1 AA is a **web standard** designed for web content and web applications. Video games are interactive real-time software with fundamentally different interaction models, rendering pipelines, and accessibility needs. There are **game-specific accessibility guidelines** that are far more applicable and should be used instead. However, some individual WCAG principles (especially around color contrast and non-color-dependent information) are transferable and align with game accessibility best practices.

**Recommendation:** The design system should reference the **Game Accessibility Guidelines** (gameaccessibilityguidelines.com) as its primary standard, supplemented by the **IGDA Game Accessibility SIG** guidelines and **Xbox Accessibility Guidelines**. It can optionally note alignment with WCAG 2.1 AA *principles* (not compliance) where overlap exists.

---

## 1. WCAG for Games: Is WCAG 2.1 AA Applicable to Video Games?

### What is WCAG 2.1 AA?

The **Web Content Accessibility Guidelines (WCAG) 2.1** are published by the W3C as a standard for making **web content** accessible to people with disabilities. Level AA is the middle tier of conformance and is the level most commonly adopted by organizations and mandated by law (e.g., ADA Title II in the U.S., Section 508). [W3C WCAG 2.1](https://www.w3.org/TR/WCAG21/) | [ADA Title II Rule](https://www.ada.gov/resources/2024-03-08-web-rule/)

### WCAG is explicitly a web standard

WCAG's full name — **Web** Content Accessibility Guidelines — makes its scope clear. The W3C states:

> "Web Content Accessibility Guidelines (WCAG) 2.1 covers a wide range of recommendations for making web content more accessible." — [W3C](https://www.w3.org/TR/WCAG21/)

The guidelines are built around web-specific technologies: HTML, CSS, ARIA, DOM, screen reader APIs, keyboard navigation via Tab/Shift+Tab, browser rendering, and web-assistive technology interoperability. Games built on MonoGame/XNA (like Stardew Valley) have none of these — they use sprite batch rendering, game loops, direct input handling, and have no DOM, no accessibility tree, and no native screen reader integration.

### What experts say about WCAG for games

**Level Access**, a leading accessibility consultancy, states:

> "Additionally, the W3C's Web Content Accessibility Guidelines (WCAG) can be broadly applied to the content in video games." — [Level Access](https://www.levelaccess.com/blog/three-ways-to-level-up-video-game-accessibility/)

Note the qualifier: "broadly applied to the **content** in video games" — not to games as a whole. This is an important distinction. WCAG principles can inform game accessibility thinking, but WCAG *compliance* is not meaningful for a game or game mod.

**Filament Games**, an educational game developer, published a WCAG 2.1 AA glossary for game developers, but even they frame it carefully:

> "This glossary explains the most important accessibility terms for teams building educational games or **web-based interactive content**." — [Filament Games](https://www.filamentgames.com/blog/accessibility-terms-for-game-developers-a-wcag-2-1-aa-glossary)

They explicitly acknowledge that WCAG was designed for web-based content, and their educational games often run in browsers (web-based), making WCAG more directly applicable to their context than to a native MonoGame mod.

### Game-specific standards exist

The video game industry has developed its own accessibility standards. A detailed history by **Ian Hamilton** (co-director of the Gaming Accessibility Conference) documents the evolution of game-specific guidelines from 2004 to 2021. [A History of Game Accessibility Guidelines](https://ian-hamilton.com/a-history-of-game-accessibility-guidelines/)

Key game accessibility standards include:

| Standard | Year | Source |
|----------|------|--------|
| Game Accessibility Guidelines | 2012–ongoing | [gameaccessibilityguidelines.com](https://gameaccessibilityguidelines.com/) |
| IGDA Game Accessibility SIG Guidelines | 2004–2021 (multiple versions) | [igda-gasig.org](https://igda-gasig.org/) |
| Xbox Accessibility Guidelines (XAG) | 2019–2021 | [Microsoft Learn](https://learn.microsoft.com/en-us/gaming/accessibility/guidelines) |
| Includification (AbleGamers) | 2012 | [includification.com](http://www.includification.com/) |
| Accessible Player Experiences (APE) | 2019 | [accessible.games](https://accessible.games/) |
| ETSI TR 103 852 | 2023 | [ETSI](https://www.etsi.org/deliver/etsi_tr/103800_103899/103852/01.01.01_60/tr_103852v010101p.pdf) |

The **ETSI TR 103 852** (2023) is notable as a formal European telecommunications standard for video game accessibility, and it explicitly references WCAG as an informative reference while building game-specific requirements.

### Conclusion on WCAG applicability

**WCAG 2.1 AA compliance is not a meaningful or appropriate target for a Stardew Valley mod** because:

1. WCAG is a web standard, and Stardew Valley is a native game using MonoGame
2. WCAG conformance requires testing against web-specific success criteria (e.g., 4.1.2 Name, Role, Value for UI components — which requires ARIA/DOM)
3. Many WCAG criteria are inapplicable to games (e.g., 1.4.4 Resize Text refers to browser zoom; 1.4.10 Reflow refers to viewport reflow)
4. Game accessibility has its own mature, domain-specific standards
5. Claiming "compliance" with a standard that wasn't designed for your medium is misleading

---

## 2. Game Accessibility Guidelines

### What are the Game Accessibility Guidelines?

The **[Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/)** (GAG) is the most widely referenced game accessibility standard. Launched in 2012 as a collaboration between studios, specialists, and academics, it provides a straightforward developer-friendly reference organized by disability category (Motor, Cognitive, Vision, Hearing, Speech) and difficulty level (Basic, Intermediate, Advanced). [Full List](https://gameaccessibilityguidelines.com/full-list/)

### Key recommendations relevant to a Stardew Valley mod design system

**Vision — Basic:**
- **Provide high contrast between text/UI and background** — directly applicable to UI overlays
- **Ensure no essential information is conveyed by a fixed colour alone** — critical for a mod adding visual indicators
- **Use an easily readable default font size** — applicable to tooltips and UI text

**Vision — Intermediate:**
- **Provide an option to adjust contrast** — relevant if the mod adds visual overlays
- **Provide a choice of cursor/crosshair colours/designs** — less relevant for Stardew
- **Provide an option to turn off/hide background movement** — relevant for animation-heavy mods

**Cognitive — Intermediate:**
- **Provide a choice of text colour, low/high contrast choice as a minimum** — applicable
- **Provide an option to turn off/hide background movement** — applicable

**General — Basic:**
- **Ensure that all settings are saved/remembered** — applicable
- **Provide details of accessibility features in-game** — applicable

### How GAG compares to WCAG

| Aspect | WCAG 2.1 AA | Game Accessibility Guidelines |
|--------|-------------|-------------------------------|
| **Scope** | Web content | Video games |
| **Format** | Testable success criteria with pass/fail | Best practice recommendations with levels (Basic/Intermediate/Advanced) |
| **Contrast requirements** | 4.5:1 for normal text, 3:1 for large text/UI components | Recommends "high contrast" but no fixed ratio; Xbox XAG recommends 7:1 for UI |
| **Color use** | 1.4.1 Use of Color — color not sole means | "Ensure no essential information is conveyed by a fixed colour alone" |
| **Standardization** | Formal W3C standard, legally mandated | Community standard, no legal standing |
| **Applicability to Stardew Valley** | Low — requires DOM, ARIA, web rendering | High — designed for games like Stardew |

### What game accessibility guidelines say about color and contrast

The **Xbox Accessibility Guidelines (XAG) 102: Contrast** provides the most specific guidance for games:

> "A high contrast mode (either light, dark, or both) should be provided. When enabled, the contrast ratios should equal or exceed 7:1 for all UI elements against their background." — [Microsoft Learn](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102)

Notably, the Xbox XAG recommends **7:1** for UI elements in games — significantly higher than WCAG's 4.5:1 for text and 3:1 for UI components. This is because games are typically played at greater distances, on varied displays, and in less controlled lighting conditions than web browsing.

The XAG also states:
> "Avoid relying on color alone to communicate information. When this isn't possible, provide players the option to choose the color of key game elements."

---

## 3. Stardew Valley Accessibility

### Base game accessibility features

Stardew Valley, despite being a solo-developer title, offers more accessibility options than many AAA games. As documented by [Nic Chan](https://www.nicchan.me/blog/modding-stardew-valley-for-accessibility/) and the [Stardew Valley Wiki](https://stardewvalleywiki.com/Options):

**Controls:**
- Full keybinding remapping (every action can be rebound)
- Mouse interactions achievable via keyboard
- Controller support with rumble toggle

**Visual:**
- UI zoom up to 150%
- Background zoom up to 200%
- Option to use default hardware cursor (beneficial for customized cursors)
- Option to disable flashing lights during thunderstorms
- Menu background toggle
- Toolbar lock option

**Audio:**
- Individual sound toggles (players can create custom sound experiences)
- Separate volume controls for effects, speech, and background/music

**What the base game lacks:**
- No colorblind modes
- No text size adjustment (only zoom)
- No screen reader support
- No high contrast mode
- No option to reduce motion/animation
- Many gameplay states communicated primarily through color (crop ripeness, object states)

### Popular accessibility mods

The Stardew Valley modding community has created several accessibility-focused mods:

| Mod | Purpose | Source |
|-----|---------|--------|
| **Stardew Access** | Screen reader support, keyboard-only play | [GitHub](https://github.com/stardew-access/stardew-access) |
| **Accessible Tiles** | Grid-based navigation, object tracking | [GrumpyCrouton](https://stardew.grumpycrouton.com/) |
| **Auto Travel** | Teleport waypoints for blind players | GrumpyCrouton |
| **Wasteless Watering** | Prevents accidental watering of wrong tiles | GrumpyCrouton |
| **SDV Colorblind Mod** | Recolors plants/forage for red/green CVD | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/10380) |
| **Pelican TTS** | Text-to-speech for in-game text | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/1079) |
| **StardewSpeak** | Voice control for gameplay | [GitHub](https://github.com/evfredericksen/StardewSpeak) |
| **A Wittily Named Recolor** | Muted palette for eye strain | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/2995) |
| **Dark User Interface** | Dark mode for UI | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/4056) |
| **Highlight Empty Jars** | Visual indicator (speech bubble) for empty jars | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/6833) |
| **NPC Map Locations** | Real-time NPC tracking on map | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/239) |
| **Lookup Anything** | In-game wiki / contextual info | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/541) |
| **Timespeed** | Control over game time passage | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/169) |
| **Fishing Made Easy** | Simplifies fishing minigame | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/3623) |

These mods address real accessibility barriers in the base game. Notably, **none** of them reference WCAG — they reference the Game Accessibility Guidelines or address specific player needs directly.

---

## 4. Color Vision Deficiency in Games

### Best practices for CVD in pixel art games

Color Vision Deficiency (CVD) affects approximately 8% of males and 0.5% of females of Northern European descent. For a pixel art game like Stardew Valley, this is particularly relevant because many gameplay states are communicated through color:

- **Crop ripeness** (green → gold)
- **Object states** (empty jars vs. full jars)
- **Forage item visibility** (moss on trees, seasonal items)
- **Soil health** (if a mod introduces health overlays)

### What game accessibility experts recommend

The **Game Accessibility Guidelines** state under "Ensure no essential information is conveyed by a fixed colour alone":

> "So ideally offer both – e.g. 'colours: deuteranopia/protanopia/tritanopia/custom', where the first three are sets of colours for team/map/cross hairs etc optimised for each of those types, and 'custom' allowing free choice of colour for those elements." — [GAG](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/)

Best practice examples cited by GAG include:
- [Destiny colorblind modes](https://gameaccessibilityguidelines.com/destiny-colorblind-modes/)
- [Faster Than Light colourblind mode](https://gameaccessibilityguidelines.com/faster-than-light-colourblind-mode/)
- [Two Dots colorblind mode](https://gameaccessibilityguidelines.com/two-dots-colorblind-mode/)

### Are patterns (stripes, dots) actually used in games?

**Yes, patterns are a recommended and used technique**, though they are more common in board games, charts, and UI than in pixel art game sprites.

**Forforallwe** (an accessibility-focused site) recommends for games:
> "1. Icons, symbols, text
> 3. Item rarity: not just color, but edge shape, number of stars, pattern.
> 4. Patterns and textures" — [Forforallwe](https://www.forallwe.com/en/post/color-blindness-video-games-accessibility-filters)

**Chris Fairfield**, an accessibility consultant, states:
> "Instead, pair them with some sort of pattern, texture, or icon to help distinguish them from each other." — [Chris Fairfield](https://chrisfairfield.com/unlocking-colorblind-friendly-game-design/)

**Audioeye** (a web accessibility company, but the principle transfers):
> "Adding textures and patterns like checkers, lines, and dots can help make each item stand out as unique and easily identifiable." — [Audioeye](https://www.audioeye.com/post/8-ways-to-design-a-color-blind-friendly-website/)

**Practical application to Stardew Valley:**
- The **SDV Colorblind Mod** ([Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/10380)) retextures plants and forage items rather than adding patterns, because pixel art sprites are small and adding patterns to individual sprites can look noisy
- **Highlight Empty Jars** ([Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/6833)) adds a **speech bubble icon** above empty jars — this is a non-color visual indicator, exactly the technique recommended by accessibility guidelines
- The **Xbox XAG** recommends outlining characters/platforms to increase contrast — a technique directly applicable to pixel art

### Recommendation for the mod design system

For a Stardew Valley mod dealing with soil health visualization:
1. **Use patterns/textures** in addition to color (e.g., stripes for low health, dots for medium, solid for high)
2. **Use icons/symbols** alongside color (e.g., a leaf icon that changes shape)
3. **Provide a colorblind mode** with alternative palettes
4. **Use lightness/value contrast** in addition to hue differences
5. **Test with grayscale** — if you can't distinguish states without color, the design fails

---

## 5. `prefers-reduced-motion` — Applicability to a Game Mod

### What is `prefers-reduced-motion`?

`prefers-reduced-motion` is a **CSS media query** defined in the W3C CSS Media Queries Level 5 specification. It detects whether a user has enabled a system-level setting to minimize animations and motion. [MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/prefers-reduced-motion)

It is used in CSS and JavaScript for **websites and web applications**:

```css
@media (prefers-reduced-motion: reduce) {
  .animation {
    animation: none;
  }
}
```

### Does it exist in Stardew Valley or MonoGame?

**No.** `prefers-reduced-motion` is a web/CSS concept. Stardew Valley is built on **MonoGame** (formerly XNA), a game framework that has:
- No CSS
- No media queries
- No browser rendering pipeline
- No concept of system-level reduced motion preferences

MonoGame has no built-in mechanism to query the operating system for reduced motion preferences. While it is *technically possible* to query the OS-level setting via platform-specific APIs (e.g., `SystemParametersInfo` on Windows, `NSWorkspace` on macOS), MonoGame does not expose this, and it would require custom platform invocation code.

### Is this concept applicable to a game mod?

**The underlying concept is applicable; the specific mechanism is not.**

Reducing motion in games is a legitimate accessibility need. The **Game Accessibility Guidelines** includes:

> "Provide an option to turn off / hide background movement" — [GAG](https://gameaccessibilityguidelines.com/provide-an-option-to-turn-off-hide-background-movement/)

This applies to:
- Animated backgrounds (e.g., swaying trees, flowing water)
- Particle effects
- Camera shake
- UI animations
- Weather effects (rain, snow)

**However**, the design system's framing of `prefers-reduced-motion` as "future-proofing for Stardew Valley updates" is problematic because:

1. **It's a web CSS concept, not a game concept** — referencing it in a game mod design system shows confusion about the medium
2. **Stardew Valley updates are unlikely to implement this** — there is no indication that ConcernedApe plans to add reduced motion support, and MonoGame doesn't support CSS media queries
3. **"Future-proofing" implies the mod would need to change** — if the mod properly implements its own reduced motion setting (independent of any CSS media query), it wouldn't need future-proofing against a web standard
4. **The mod can implement reduced motion directly** — a Stardew Valley mod can simply add a configuration option to disable or reduce animations, without needing any external signal

### Correct approach for the design system

The design system should reference **"Provide an option to turn off / hide background movement"** from the Game Accessibility Guidelines, not `prefers-reduced-motion`. If the mod adds animations, it should include a mod configuration option (via GMCM or the mod's own config) to disable or reduce them.

---

## 6. Non-Diegetic UI — Relevance to Stardew Valley

### What is non-diegetic UI?

The concept comes from film studies (diegesis = the narrative world) and was adapted for game UI design. **Nicolas Kraj**, UX Director at Ubisoft, defines it in his widely-cited Medium article:

> "This is the most common type of UI: **it acts as an overlay over the game. It doesn't exist in the 3D view, neither does it in the game universe**: in the Street Fighter series, neither Chun-Li nor Ryu are aware of the existence of their combo meters or the visual representation of their health." — [Nicolas Kraj, Medium](https://medium.com/@nicolaskraj/designing-efficient-user-interfaces-for-games-be20b516f1c2)

The four categories of game UI are:

| Category | Exists in 3D world? | Exists in narrative? | Example |
|----------|---------------------|----------------------|---------|
| **Non-Diegetic** | No | No | Health bar, minimap, ammo counter |
| **Diegetic** | Yes | Yes | Dead Space's health display on the character's suit |
| **Spatial** | Yes | No | Objective markers in the 3D world |
| **Meta** | No | Yes | GTA5's phone interface, Forza's speedometer |

### Is this concept relevant to Stardew Valley?

**Yes, the concept is relevant and correctly applicable.**

Stardew Valley's UI is predominantly **non-diegetic**:
- Health bar (top-left)
- Stamina bar
- Time display
- Money display
- Toolbar
- Minimap
- Quest log

These are all overlays that exist outside the game's narrative world. The farmer character is not "aware" of having a health bar or a toolbar.

The concept is correctly applied to Stardew Valley's UI system because:
1. Stardew Valley is a 2D game with a fixed camera — non-diegetic UI is the standard and most practical approach
2. The game's UI elements (health, stamina, money, time, toolbar) are classic examples of non-diegetic UI
3. Understanding this distinction helps the design system make informed decisions about **where** and **how** to add new UI elements

### How this relates to the mod design system

For a mod adding soil health visualization, the non-diegetic framework suggests:
- **Tooltips and health indicators** should be non-diegetic overlays (like the existing toolbar)
- **Tile overlays** showing soil health could be either:
  - **Spatial** (drawn on the tile in the game world) — more immersive but can be missed
  - **Non-diegetic** (HUD overlay) — more visible but less immersive
- The choice depends on the mod's design goals and the importance of the information

The Medium article by Nicolas Kraj is a **legitimate and well-regarded source** on game UI design. Citing it is appropriate.

---

## 7. Existing Accessible Stardew Valley Mods

### Stardew Access

The most significant accessibility mod for Stardew Valley is **[Stardew Access](https://github.com/stardew-access/stardew-access)** by Shoaib Khan. It provides:

- **Screen reader support** via NVDA, JAWS, and other screen readers (using Tolk/libspeechdwrapper/libspeak)
- **Keyboard-only play** (no mouse required)
- **Tile narration** — reads the name of the tile the player is facing
- **Coordinate display** (press K)
- **Status readout** (Q for time, R for money, H for health/stamina)
- **Menu accessibility** — all menus read by screen reader
- **Building placement** via debug commands
- **Object tracking** through integration with Accessible Tiles

Source: [GitHub](https://github.com/stardew-access/stardew-access) | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/16205)

### Accessible Tiles

By GrumpyCrouton, this mod:
- Makes the character move in a **grid pattern** (hopping tile to tile)
- Allows **tracking of objects, resources, characters** by category
- Reports **distance and direction** to tracked objects

Source: [stardew.grumpycrouton.com](https://stardew.grumpycrouton.com/)

### SDV Colorblind Mod

This mod retextures several plants and forage items that are difficult to distinguish for people with red/green colourblindness (deuteranopia, protanopia). It addresses specific in-game visibility issues.

Source: [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/10380) | [Forum Thread](https://forums.stardewvalley.net/threads/colorblind-accessibility-options.10687/)

### Pelican TTS

A Text-to-Speech mod that reads in-game text aloud using the system's TTS engine, with customizable voices per NPC.

Source: [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/1079)

### StardewSpeak

A voice control mod that allows playing Stardew Valley entirely by voice, with commands like "go to farm" that trigger automatic pathfinding.

Source: [GitHub](https://github.com/evfredericksen/StardewSpeak)

### What these mods do right (from an accessibility standpoint)

1. **They address specific player needs** rather than trying to be "compliant" with a standard
2. **They follow the Game Accessibility Guidelines** implicitly — e.g., Stardew Access follows "Ensure screen reader support" and "Ensure that all areas of the UI can be accessed using the same input method as gameplay"
3. **They are configurable** — players can adjust or disable features
4. **They integrate with the game's existing systems** rather than fighting against them

---

## 8. Recommendations for the Design System

### What the design system should reference

| Standard | Why |
|----------|-----|
| **[Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/)** | Primary standard — designed for games, directly applicable |
| **[IGDA Game Accessibility SIG Guidelines](https://igda-gasig.org/)** | Industry-standard reference since 2004 |
| **[Xbox Accessibility Guidelines](https://learn.microsoft.com/en-us/gaming/accessibility/guidelines)** | Most comprehensive game-specific standard with specific contrast ratios |
| **[Nicolas Kraj's UI article](https://medium.com/@nicolaskraj/designing-efficient-user-interfaces-for-games-be20b516f1c2)** | Legitimate source for non-diegetic UI concept |

### What the design system should NOT claim

1. **WCAG 2.1 AA compliance** — misleading and inappropriate for a game mod
2. **`prefers-reduced-motion` support** — a web CSS concept, not applicable to MonoGame

### What the design system CAN claim (with caveats)

1. **"Informed by WCAG 2.1 AA principles"** — acceptable if the design system genuinely applies transferable principles (contrast, non-color-dependent information) but acknowledges the difference in medium
2. **"Aligned with Game Accessibility Guidelines (Basic level)"** — appropriate and verifiable
3. **"Non-diegetic UI following industry-standard game UI taxonomy"** — appropriate, citing Nicolas Kraj
4. **"Includes option to reduce motion"** — appropriate, citing GAG

### Practical accessibility features the mod should implement

Based on the research, a Stardew Valley mod design system should consider:

1. **Color & Contrast:**
   - All UI elements should meet at least 4.5:1 contrast ratio against background (WCAG-aligned)
   - For gameplay-critical visual indicators, aim for 7:1 (Xbox XAG)
   - Never rely on color alone — use patterns, icons, or text labels
   - Test with a CVD simulator (e.g., Color Oracle, Sim Daltonism)

2. **Colorblind Modes:**
   - Offer presets for deuteranopia, protanopia, and tritanopia
   - Or use CVD-safe palettes (e.g., orange vs. blue, with lightness contrast)

3. **Motion:**
   - Include a mod config option to disable or reduce any animations added by the mod
   - Do not reference `prefers-reduced-motion` — implement directly

4. **Text & Typography:**
   - Use readable font sizes (at least 12px equivalent in pixel space)
   - Provide high contrast between text and background
   - Avoid text in images where possible

5. **Input:**
   - Ensure all mod features are accessible via keyboard
   - Support remappable keys
   - Do not require simultaneous complex inputs

6. **Screen Reader Compatibility:**
   - Consider integration with Stardew Access API for narrating mod-specific information
   - Ensure any text output is accessible to TTS/screen reader software

---

## 9. Summary Verdict

| Claim in Design System | Verdict | Reasoning |
|------------------------|---------|-----------|
| **WCAG 2.1 AA compliance as a target** | ❌ **Inappropriate** | WCAG is a web standard; games have their own standards. Claiming compliance is misleading. |
| **`prefers-reduced-motion` as future-proofing** | ❌ **Inappropriate** | A web CSS media query that doesn't exist in MonoGame. The concept (reducing motion) is valid, but the mechanism is wrong. |
| **Non-diegetic UI concept** | ✅ **Appropriate** | A legitimate and well-established game UI concept. Correctly applied to Stardew Valley. The Medium article is a credible source. |

### Final Recommendation

The design system should be reframed to reference **game-specific accessibility standards** as its primary framework, with optional acknowledgment of WCAG principles where they overlap (contrast, color independence, text accessibility). The non-diegetic UI reference should be retained as it is appropriate and well-sourced.

---

## Sources

1. [W3C WCAG 2.1](https://www.w3.org/TR/WCAG21/)
2. [Game Accessibility Guidelines — Full List](https://gameaccessibilityguidelines.com/full-list/)
3. [IGDA Game Accessibility SIG](https://igda-gasig.org/)
4. [A History of Game Accessibility Guidelines — Ian Hamilton](https://ian-hamilton.com/a-history-of-game-accessibility-guidelines/)
5. [Xbox Accessibility Guidelines 102: Contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102)
6. [Three Ways to Level Up Video Game Accessibility — Level Access](https://www.levelaccess.com/blog/three-ways-to-level-up-video-game-accessibility/)
7. [Accessibility Terms for Game Developers: A WCAG 2.1 AA Glossary — Filament Games](https://www.filamentgames.com/blog/accessibility-terms-for-game-developers-a-wcag-2-1-aa-glossary)
8. [Modding Stardew Valley for Accessibility — Nic Chan](https://www.nicchan.me/blog/modding-stardew-valley-for-accessibility/)
9. [Stardew Valley Accessibility Review — Black Screen Gaming Blog](https://blog.blackscreengaming.com/reviews/stardew-valley-accessibility-review-giveaway/02/22/2022/)
10. [Stardew Access — GitHub](https://github.com/stardew-access/stardew-access)
11. [Stardew Access — Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/16205)
12. [Designing Efficient User Interfaces For Games — Nicolas Kraj (Medium)](https://medium.com/@nicolaskraj/designing-efficient-user-interfaces-for-games-be20b516f1c2)
13. [prefers-reduced-motion — MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/prefers-reduced-motion)
14. [Ensure no essential information is conveyed by a fixed colour alone — GAG](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/)
15. [Gaming and color blindness — Forforallwe](https://www.forallwe.com/en/post/color-blindness-video-games-accessibility-filters)
16. [Unlocking Colorblind Friendly Game Design — Chris Fairfield](https://chrisfairfield.com/unlocking-colorblind-friendly-game-design/)
17. [8 Ways to Design a Color Blind Friendly Website — Audioeye](https://www.audioeye.com/post/8-ways-to-design-a-color-blind-friendly-website/)
18. [ETSI TR 103 852 V1.1.1 (2023-04) — Video Game Accessibility](https://www.etsi.org/deliver/etsi_tr/103800_103899/103852/01.01.01_60/tr_103852v010101p.pdf)
19. [Diegetic and Non-Diegetic UI in Games — NastyRodent](https://nastyrodent.com/diegetic-and-non-diegetic-ui/)
20. [Stardew Valley Options — Wiki](https://stardewvalleywiki.com/Options)
21. [SDV Colorblind Mod — Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/10380)
22. [Accessibility and Ease of Play (1.6) — Stardew Modding Wiki](https://stardewmodding.wiki.gg/wiki/Accessibility_and_Ease_of_Play)
23. [ADA Title II Web Rule](https://www.ada.gov/resources/2024-03-08-web-rule/)
