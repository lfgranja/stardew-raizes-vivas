# Research: Flash Effect Visual Feedback Patterns

**Date:** 2026-09-08
**Spec:** FR-003 — Hoe action flash effect
**Question:** Is the flash a white overlay or a colored overlay matching the tile's health category?

---

## 1. The Ambiguity

FR-003 currently states:

> "System MUST show flash effect (**white overlay flash at the tile's health category color**) and floating text when a hoe is used on tilled soil tiles"

This is self-contradictory — a flash cannot be both universally white and simultaneously colored per category. The phrase combines two distinct visual patterns into one requirement.

---

## 2. Common Flash Effect Patterns in Game Mods and Game UX

### 2.1 White Flash (Universal "Hit" Feedback)

- **Pattern:** A white (or near-white) overlay that flashes on a sprite/tile for a few frames, then fades.
- **Meaning:** Generic action confirmation — "something happened here." Carries no semantic content beyond "registered."
- **Prevalence:** Dominant in retro platformers and arcade games as a damage indicator. Enemies blink white when hit ([Retrocomputing StackExchange](https://retrocomputing.stackexchange.com/questions/12211/why-do-old-games-use-flashing-as-means-of-showing-damage)).
- **Modern use:** Hit markers in FPS games, combat feedback in RPGs, generic "selected" highlights.
- **Pros:** Instantly recognizable; high contrast against most backgrounds; works regardless of game state.
- **Cons:** Conveys no information beyond "action landed"; can be confused with damage or error states.

### 2.2 Colored Flash (Contextual/Semantic Feedback)

- **Pattern:** A colored overlay matching the context — element type, team color, health status, terrain type.
- **Meaning:** The color encodes information about *what* the feedback represents.
- **Prevalence:** Ubiquitous in strategy games (team colors), RPGs (elemental damage colors), and tile-based games (terrain/state highlighting).
- **Examples:**
  - Range highlighting in Stardew Valley mods uses distinct colors per item type (blue for sprinklers, green for scarecrows, yellow for bee houses) ([StardewRangeHighlight](https://github.com/jltaylor-us/StardewRangeHighlight)).
  - Status effects in games use colored auras (e.g., Jarate in Team Fortress 2 uses a yellow tint) ([Accessible Game Design](https://accessiblegamedesign.com/guidelines/statuseffects.html)).
- **Pros:** Reinforces game state information; supports learnability (color → meaning mapping); accessible when combined with other cues.
- **Cons:** Requires the player to learn the color mapping; less effective for colorblind users without secondary indicators.

### 2.3 Fade-Out Animation

- **Pattern:** The overlay starts at full intensity and linearly fades to transparent over a short duration (typically 150–500ms).
- **Meaning:** Temporal marker — "this just happened." The fade recedes into the background, avoiding persistent visual clutter.
- **Prevalence:** Standard across both white and colored flash patterns. Used in everything from damage flashes to selection indicators.
- **Implementation:** `opacity = 1.0 - (elapsedTime / duration)` applied to the overlay's alpha channel.

### 2.4 Hybrid: White Flash + Colored Border/Tint

- **Pattern:** A brief white flash followed by a colored afterglow, or a white flash with a colored edge.
- **Meaning:** Combines universal "hit" recognition with contextual information.
- **Prevalence:** Common in action games and MOBAs where both immediate feedback and status information matter.

---

## 3. Typical Implementation Approaches

### 3.1 White Overlay That Fades Out

The most basic approach: draw a white semi-transparent quad over the target, fading alpha from 1.0 to 0.0.

```
// Pseudocode
float alpha = 1.0f - (elapsed / duration);
DrawWhiteTexture(tileRect, new Color(255, 255, 255, (byte)(alpha * 255)));
```

**When to use:** When the feedback is purely confirmatory ("your click registered") and no additional state information needs to be conveyed.

### 3.2 Colored Overlay Matching Context

Draw a tinted quad using a color derived from game state (health category, element type, etc.).

```
// Pseudocode
Color categoryColor = GetColorForCategory(healthCategory);
float alpha = 1.0f - (elapsed / duration);
categoryColor.A = (byte)(alpha * 255);
DrawTexture(tileRect, categoryColor);
```

**When to use:** When the feedback should reinforce existing information or teach the player about game state.

### 3.3 Current Implementation (LivingRoots)

The existing `HoeFeedbackRenderer.cs` and `VisualizationService.cs` both implement **colored overlay matching context**:

```csharp
// HoeFeedbackRenderer.cs line 65
Color flashColor = _colorInterpolationService.GetColorForHealth(feedback.HealthValue, opacity);

// VisualizationService.cs line 366
var flashColor = _colorService.GetColorForHealth(feedback.HealthValue, config.Opacity);
```

The flash uses the interpolated health category color (Poor=red, Moderate=yellow, Healthy=green) with a linear fade-out over 300ms. This is a **colored flash**, not a white flash.

---

## 4. Analysis: What Is Most Intuitive for a Hoe Action on a Tile?

### 4.1 The Hoe Action Context

When a player uses a hoe on tilled soil in LivingRoots, the purpose is to **inspect soil health**. The feedback must:
1. Confirm the action registered (the hoe hit *this* tile).
2. Communicate the soil health status of that tile.

The floating text already handles #2 explicitly ("Soil Health: 45% (Moderate)"). The flash serves as the immediate, pre-attentive cue that directs the player's attention to the relevant tile.

### 4.2 White Flash: Pros and Cons

| Aspect | Assessment |
|--------|-----------|
| Action confirmation | Excellent — white flash is the universal "hit" signal |
| Information conveyance | None — color carries no meaning |
| Consistency with overlays | Poor — the persistent overlay is colored; a white flash creates visual dissonance |
| Learnability | Not applicable (no mapping to learn) |

### 4.3 Colored Flash: Pros and Cons

| Aspect | Assessment |
|--------|-----------|
| Action confirmation | Good — any bright flash confirms the action |
| Information conveyance | Excellent — color reinforces the health category before the text is read |
| Consistency with overlays | Excellent — the flash color matches the persistent overlay color on that tile |
| Learnability | Good — strengthens the color→category association each time the player checks soil |

### 4.4 Recommendation

**A colored flash matching the tile's health category is the more intuitive choice** for this specific use case. Reasons:

1. **Reinforces the color-language mapping.** The persistent overlay system already uses color to encode health (red/yellow/green). A colored flash strengthens this association every time the player inspects a tile, building intuition faster than a white flash would.

2. **Visual consistency.** The flash appears on the same tile that already has a colored overlay. A white flash would clash with the existing color language; a colored flash extends it.

3. **Redundant information is acceptable.** The floating text already provides explicit health information. The colored flash is not the sole carrier of the category information — it is a supplementary cue. This follows the "multiple layers of feedback" principle from accessible game design ([Accessible Game Design - Status Effects](https://accessiblegamedesign.com/guidelines/statuseffects.html)).

4. **Pre-attentive processing.** Color is processed pre-attentively by the visual system. A colored flash draws the eye to the relevant tile *and* begins conveying its health status before the player consciously reads the text.

5. **Precedent in Stardew modding.** The StardewRangeHighlight mod uses distinct colors per range type, demonstrating that the Stardew modding community expects color-coded tile overlays to carry semantic meaning ([StardewRangeHighlight](https://github.com/jltaylor-us/StardewRangeHighlight)).

---

## 5. Examples from Stardew Valley and Mods

### 5.1 Base Game

- **Hoe use:** The base game shows a dirt-turning animation but no color overlay or flash. The visual feedback is purely the sprite change (grass → dirt).
- **Watering:** No flash — the tile sprite changes to a darker, watered variant.
- **Planting:** No flash — the seed sprite appears on the tile.
- **Tool range (1.6+):** When holding a tool that affects multiple tiles (e.g., watering can), affected tiles show a subtle highlight — a **colored overlay** pattern.

### 5.2 StardewRangeHighlight Mod

- Uses **colored overlays** per item type: blue for sprinklers, green for scarecrows, yellow for bee houses, brown for mushroom logs, white for Junimo huts, red/orange for bombs.
- Each color is configurable, demonstrating that the community expects color to carry meaning.
- Source: [github.com/jltaylor-us/StardewRangeHighlight](https://github.com/jltaylor-us/StardewRangeHighlight)

### 5.3 UI Info Suite / UI Info Suite 2

- Shows hover tooltips with contextual information (e.g., scarecrow range, sprinkler range).
- Uses colored overlays for range visualization, consistent with the color-encoding convention.

### 5.4 Better Sprinklers

- Draws colored overlays around sprinklers to indicate range.
- Uses a distinct color (typically blue) that differs from other range mods, reinforcing the "color = meaning" pattern.

---

## 6. Spec Clarification Recommendation

### Current (contradictory):
> "System MUST show flash effect (white overlay flash at the tile's health category color) and floating text when a hoe is used on tilled soil tiles"

### Recommended (unambiguous):
> "System MUST show a colored overlay flash at the tile's health category color (matching the persistent overlay color for that health category) and floating text when a hoe is used on tilled soil tiles. The flash MUST fade out linearly over 300ms."

### Rationale for the change:

1. Removes the contradictory "white overlay flash at the tile's health category color" phrasing.
2. Explicitly ties the flash color to the persistent overlay color system, ensuring visual consistency.
3. Specifies the fade-out animation (linear, 300ms) which is already implemented.
4. The current code (`HoeFeedbackRenderer.cs`, `VisualizationService.cs`) already implements a colored flash — this change aligns the spec with the implementation.

### Alternative considered:
> "System MUST show a white overlay flash followed by a colored flash at the tile's health category color..."

This hybrid approach was rejected because:
- It adds complexity (two-phase flash) without clear benefit.
- The floating text already provides explicit information; the flash's primary role is attention direction, which a single colored flash accomplishes.
- No evidence that hybrid flashes are a convention in tile-based farming games.

---

## 7. Sources

1. [Retrocomputing StackExchange — Why do old games use flashing as means of showing damage?](https://retrocomputing.stackexchange.com/questions/12211/why-do-old-games-use-flashing-as-means-of-showing-damage) — Documents the white-flash-as-damage-feedback convention in retro games.
2. [Accessible Game Design — Status Effect Guidelines](https://accessiblegamedesign.com/guidelines/statuseffects.html) — Covers colored auras, multiple layers of feedback, and colorblind-friendly status indicators.
3. [StardewRangeHighlight — GitHub](https://github.com/jltaylor-us/StardewRangeHighlight) — Stardew Valley mod using distinct colors per range type for tile highlighting.
4. [Game Mechanics — Player Damage Feedback](https://gamemechanics.org/patterns/player-damage-feedback) — Documents the history and conventions of damage feedback (red screen flash, directional indicators).
5. [Reddit r/gamedev — What kind of hit indicators do you prefer?](https://www.reddit.com/r/gamedev/comments/12w0x2q/what_kind_of_hit_indicators_do_you_prefer/) — Community discussion on flash feedback preferences.
6. [Reddit r/Unity3D — White flash effect implementation](https://www.reddit.com/r/Unity3D/comments/xx0jto/im_trying_to_find_out_how_to_make_my_model_flash/) — Technical discussion of white flash implementation.
