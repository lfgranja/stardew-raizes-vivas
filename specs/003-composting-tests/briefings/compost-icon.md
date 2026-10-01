# Asset Briefing: Compost item icon

**Priority**: P2 — cosmetic · **Type**: PNG sprite in the `i.png` spritesheet · **Status**: not started
**Blocks**: nothing functional. The item falls back to a vanilla placeholder if the sprite is absent.

---

## 1. Context

[`compost-item.md`](./compost-item.md) registers the `LivingRoots.Compost` item and sets:

```json
"Icon": "i"
```

`"i"` refers to the sprite named `i` inside the `i.png` spritesheet — Stardew's default item icon
sheet. Supplying that sprite is optional: without it the game shows a placeholder, so the item is
fully functional. This briefing covers making it look intentional.

## 2. What must be delivered

`LivingRoots/assets/i.png` — a copy of the vanilla `i.png` spritesheet with the compost sprite added.

```bash
# 1. Unpack the game's Content/Items.xnb to obtain i.png
#    (https://stardewvalleywiki.com/Modding:Editing_XNB_files)
# 2. Place the working copy at LivingRoots/assets/i.png
```

Then reference it from `content.json`:

```json
{
  "Format": "2.3.0",
  "Changes": {
    "Data/Objects": {
      "LivingRoots.Compost": { "...": "..." }
    }
  },
  "DynamicTokens": [
    {
      "Name": "LivingRootsIcon",
      "FromFile": "assets/i.png",
      "To": "i"
    }
  ]
}
```

## 3. Sprite specification

| Property | Value |
|----------|-------|
| Sheet | `i.png` |
| Name in sheet | `i` |
| Frame size | **16 × 16 px** (Stardew's standard icon size) |
| Suggested index | next free frame in the sheet |
| Format | 32-bit PNG with alpha |
| Palette | stay within vanilla's colour range — no new colours |

## 4. Art direction

From `AGENTS.md` (`LivingRoots/Services/AGENTS.md`) and `CONTRIBUTING.md`:

> **Pixel Art:** If contributing visual assets, try to maintain the Stardew Valley pixel art style.

Suggested subject — a small pile or sack of dark, crumbly earth:

| Element | Guidance |
|---------|----------|
| Base | Dark brown soil mass, roughly triangular or bag-shaped |
| Highlight | 2–3 px of lighter brown on the upper-left, matching vanilla lighting |
| Accent | A few green flecks hinting at organic matter |
| Silhouette | Must read at 16×16 — favour a bold outline over interior detail |
| Do not | Use the earth-tone overlay palette (`#B91C1C`/`#D97706`/`#15803D`) as flat fills — those are UI overlay colours for soil health, not item art. A subtle green accent ties in without competing. |

Review the result at 1× and 4× zoom against neighbouring vanilla material icons (silt, fertilizer,
bone meal) before submitting.

## 5. Accessibility

The soil health overlay uses distinct **patterns** (Stripes / Dots / Solid) rather than colour alone,
per FR-001. The compost icon is not part of that system, but keep it distinguishable from
`Object.Fertilizer` and `Object.SpeedGro` at a glance — players will confuse them.

## 6. Alternatives considered

| Option | Verdict |
|--------|---------|
| Icon-only sprite sheet named `LivingRoots` (not `i`) | Cleaner — avoids copying all of vanilla `i.png`. Requires `"Icon": "LivingRoots"` and a `DynamicTokens` entry mapping the whole sheet. **Recommended if the team prefers not to vendor vanilla art**, since redistributing vanilla pixels in the repo is a licensing question. |
| Generate the icon in code | Not possible — item icons are content-pipeline assets. |

## 7. Out of scope

- Compost **bin** icon — deferred, see [`composting-bin-item.md`](./composting-bin-item.md)
- Recipe icon, gift icon, trash icon
- Portraits, overlays, pattern textures (all generated at runtime — see the parent
  [`README.md`](./README.md#not-assets--do-not-brief-these))