# Briefing: `compost.png` — item icon for `(O)LivingRoots.Compost`

- **Feature:** `specs/004-playable-compost-loop/` (FR-005: compost MUST exist as a real in-game item with a player-visible name, icon, and stack behaviour; SC-002: every loop step produces a visible in-game result).
- **Status:** asset file exists, **unwired** — game renders a wrong vanilla sprite today.
- **Scope of this briefing:** the compost *object* icon only (`Data/Objects` + content pipeline + art spec + verification). Bin sprites, audio, and machine art are out of scope (see §5).
- **Stack:** Stardew Valley 1.6 (verified against 1.6.15/1.6.16 docs revisions Aug 2026), .NET 6, SMAPI 4.x, `Pathoschild.Stardew.ModBuildConfig` 4.4.0. No Content Patcher dependency (per spec spike: register programmatically via `AssetRequested`; CP is fallback-only).

---

## 1) Estado atual (verified 2026-10-05, working tree)

### 1.1 The file exists and ships, but nothing references it

| Fact | Evidence |
|---|---|
| `LivingRoots/Assets/compost.png` is **16×16, 8-bit/color RGBA, non-interlaced** (= 32-bit PNG: 8 bits × 4 channels) | `file` + PIL re-inspection 2026-10-05: `PNG image data, 16 x 16, 8-bit/color RGBA, non-interlaced`, mode `RGBA` |
| Only **6 unique colors**: 141 fully-transparent px + browns `#553722` (46 px), `#26180F` (35 px), `#372216` (18 px), `#7A5232` (10 px) + one green `#2E7D32` (6 px, i.e. RGB 46,125,50) | pixel census — placeholder-grade art, but already the correct grid size, correct 32-bit RGBA encoding, and roughly the right earth-tone family. Green accent is close to but NOT equal to either mod green (`#15803D` Healthy / `#166534` CompostReady) — normalize to a mod constant on redraw (§3.2) |
| `.csproj` copies `Assets/**/*.*` to build output (`PreserveNewest`) | `LivingRoots/LivingRoots.csproj:26-28` — the PNG ships in the mod folder, dead weight |
| **Zero** C# references: no `ModContent.Load`, no `LoadFromModFile`, no `GetActualAssetKey` anywhere in `LivingRoots/` | grep for `Texture\|ModContent\|Content\.Load\|SpriteIndex` — only hits are the procedural 1×1 white overlay textures, unrelated |
| Sibling bin sprites (`composting_bin_{empty,processing,ready}.png`) are 16×32 RGBA — also unwired | same inspection; bin wiring is a separate briefing |

### 1.2 Registration renders the wrong sprite

`LivingRoots/ModEntry.cs:141-149` registers the compost entry **without `Texture`/`SpriteIndex`**:

```csharp
data[ModConstants.CompostItemId] = new StardewValley.GameData.Objects.ObjectData
{
    Name = "Compost",
    DisplayName = "Compost",       // hardcoded literal, see §1.3
    Description = "Organic compost rich in microbial life. Restores 15 soil health to tilled tiles.",
    Type = "Basic",
    Category = ModConstants.CompostCategoryId,   // -26, see §1.4
    Price = 10
    // Texture == null  ->  game falls back to Maps/springobjects, SpriteIndex defaults to 0
};
```

Consequences (per [Modding:Objects](https://wiki.stardewvalley.net/Modding:Objects)):

- `Texture` omitted ⇒ defaults to `Maps/springobjects`; `SpriteIndex` omitted ⇒ `0` (default `int`).
- The compost item therefore renders as **vanilla sprite #0** (a weeds/stone-adjacent sprite from `springobjects.png`, a 384×624 sheet of 24×39 16×16 cells) — wrong icon, confusing next to real fertilizers, and SC-002 ("identifiable as compost") fails.
- This is *not* the 🛇 error item (that appears only when the data entry itself is missing, e.g. mod removed — [Modding:Items](https://wiki.stardewvalley.net/Modding:Items)). The entry exists; only the art pointer is absent.

### 1.3 Display text is hardcoded, i18n keys exist but are disconnected

- Canonical strings already exist: `compost.name` / `compost.description` in `LivingRoots/i18n/default.json` **and** `en.json` (identical content), plus refusal keys `message.compost.*` / `message.harvest.inventory-full` (tasks T003 done).
- `ObjectData.DisplayName/Description` are **plain literals**, not wired to the i18n keys. `DisplayName`/`Description` accept [tokenizable strings](https://wiki.stardewvalley.net/Modding:Tokenizable_strings), but `[LocalizedText …]` resolves against *game content* assets, not SMAPI mod i18n — there is no direct binding. Per-locale display names are a follow-up (see §2.4); FR-008a's i18n scope covers refusal HUD messages only, which are already wired.

### 1.4 Open item-data question: `Category = -26` (mislabeled "fertilizer" — it is artisan goods)

- `ModConstants.CompostCategoryId = -26` (`LivingRoots/Constants.cs:74`).
- Per the [Modding:Items §Categories table](https://wiki.stardewvalley.net/Modding:Items) (rev. Aug 2026), **-26 is `Object.artisanGoodsCategory`** ("Artisan Goods", `#009B6F`, affected by the Artisan profession) — **not** fertilizer. Vanilla fertilizer category is **-19** (`Object.fertilizerCategory`, "Fertilizer … Is Placeable, is always passable").
- Consequence 1 — `specs/002-soil-decay-compost/data-model.md:89` ("Category … Fixed: -26 | Fertilizer category") mislabels the category. The value is inherited as-is (FR-003 freezes the set; this briefing changes no values), but the label is wrong and should be corrected to "Artisan Goods" wherever the data model is next touched.
- Consequence 2 — side effects of -26 beyond the collections tab: Artisan-profession price bonus applies on sale (`Price = 10` → +40% with Artisan), and NPC gift-taste / shipping-collection grouping follows artisan-goods rules. None of this blocks the icon, but it is game behavior owned by the T022 decision.
- **Do not blindly switch to -19.** Category -19 makes the game treat the item as *placeable vanilla fertilizer* (right-click onto tilled soil), which would run the vanilla placement path in parallel with this mod's own `ICompostApplicationService.TryApplyCompost` flow (consumes 1 item, +15 health, custom refusal messages). Changing it alters game behavior, not just the collections tab. Decision required during implementation: keep `-26` (custom item, mod-owned apply path) **or** adopt `-19` and reconcile with the vanilla placement path. Record the decision in the task/PR; either way the icon wiring in §2 is unaffected.

### 1.5 Clarification: "Content Patcher DynamicTokens" do not apply here

The research request mentioned CP DynamicTokens. This mod is a **C# SMAPI mod**, not a Content Patcher pack: there is no `content.json`, and the spec spike explicitly prefers dependency-free programmatic registration (CP is the fallback if native `Data/Machines` registration fails). The equivalents in this stack are:

- **Icon/decoration:** `ObjectData.Texture` + `SpriteIndex` (this briefing).
- **Localizable display text:** tokenizable strings + SMAPI `ITranslationHelper` (already used for refusal messages).
- Do **not** add a Content Patcher dependency or `content.json` just to ship the icon — that contradicts the spike exit criteria.

### 1.6 Supersession note: the 003 briefing describes a pre-1.6 route — do not follow it

`specs/003-composting-tests/briefings/compost-icon.md` (P2, "not started") briefs the *old* approach: vendoring a copy of vanilla `i.png`, adding a frame, and referencing it via `"Icon": "i"` + `content.json` DynamicTokens. That route is **superseded**:

- Stardew 1.6 replaced index-based sprites with per-item `Texture` + `SpriteIndex` fields ([Migrate to 1.6 §Custom objects](https://wiki.stardewvalley.net/Modding:Migrate_to_Stardew_Valley_1.6)); `ParentSheetIndex` is explicitly documented as *not a valid identifier*.
- Copying `i.png` into the repo would vendor proprietary ConcernedApe pixels — the 003 briefing itself flags this as "a licensing question". The §3.4 hard constraint below (original-only art, no vendored vanilla pixels) resolves that question: do not do it.
- `content.json` does not exist in this repo and must not be created for the icon (§1.5).
- Keep the 003 file as history; the normative spec is this briefing + T022 (`specs/004-playable-compost-loop/tasks.md:86`).

### 1.7 `Type` values: compost stays `Basic`, bin is the one that moves

- Compost `Type = "Basic"` (`ModEntry.cs:146`) is correct for a consumable inventory item applied by mod code. `Type` governs collections-tab grouping (paired with `Category`), **not** the icon — changing it has no art effect. `Arch` is the one type to never adopt (breaks artifact spots — [Modding:Objects §Basic info](https://wiki.stardewvalley.net/Modding:Objects)).
- The bin's `Type = "Crafting"` in `Data/Objects` (`ModEntry.cs:136`) is the provisional registration that the 004 machine-native design (FR-001a, `Data/Machines` + `(BC)` key) revisits. That migration owns the bin's future `Texture`/`SpriteIndex` against `TileSheets/Craftables` defaults — it does not touch `Assets/compost.png`.

---

## 2) O que deve ser entregue

### 2.1 Asset file (keep the path)

| Requirement | Value | Rationale |
|---|---|---|
| Path | `LivingRoots/Assets/compost.png` (unchanged) | Already copied by `.csproj`; `LoadFromModFile` paths are relative to the mod folder |
| Dimensions | **16×16 px**, single icon filling the frame | Object icons are 16×16 cells; `springobjects.png` is a 24-column grid of 16×16 cells (384×624 total). One icon ⇒ `SpriteIndex = 0` |
| Format | **PNG, 32-bit RGBA, non-interlaced** (on disk: 8-bit/color RGBA = 8×4 channels; `Texture2D` via `.png` → `Texture2D`) | What `LoadFromModFile<Texture2D>` accepts (`Modding:Modder Guide/APIs/Content` — `.png` → `Texture2D`); matches current file's encoding, keep it. Never ship paletted (color type 3), grayscale, or interlaced exports — the §4.2 IHDR guard rejects them |
| Layout | Icon at top-left origin; no padding sheets, no multi-frame strip | `SpriteIndex = 0` reads the 16×16 rect at (0,0). A strip/spritesheet is unnecessary for one icon (JsonAssets precedent: dedicated 16×16 texture per item, `SpriteIndex = 0`). If the texture ever grows to N icons, index counts left-to-right then top-to-bottom across the grid |
| Alpha | Interior fully opaque (`A=255`); exterior fully transparent (`A=0`); at most 1 px semi-transparent AA fringe | Semi-alpha halos bloom at the 4× inventory scale (64×64) and over dark tooltip backgrounds |

### 2.2 Pipeline wiring (recommended: pure SMAPI, no new dependency)

Two coordinated edits in `LivingRoots/ModEntry.cs` (`OnAssetRequested`), plus one constant. All string literals for asset names go in `LivingRoots/Constants.cs` per repo convention (no magic strings in controllers/entry).

**Step 1 — provide the texture as a named game asset** (pattern: [Content API §Replace a game asset / Add a new asset](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content)):

```csharp
// New branch in OnAssetRequested, alongside the Data/Objects branch.
else if (e.Name.IsEquivalentTo(ModConstants.CompostTextureName))
{
    e.LoadFromModFile<Texture2D>("Assets/compost.png", AssetLoadPriority.Medium);
}
```

- Asset name MUST be `Mods/lfgranja.LivingRoots/Compost` (convention `Mods/<UniqueID>/<name>`; UniqueID from `LivingRoots/manifest.json`). Forward slashes — SMAPI normalizes separators (`ModContent` docs: "don't worry about which path separators you use"); compare with `IsEquivalentTo` (never `==`; asset names are case/format-insensitive — `Characters/Abigail` ≡ `CHARACTERS\ABIGAIL`).
- `AssetLoadPriority.Medium` is the standard level for providing a new asset. `Exclusive` is forbidden (breaks compat — if two mods claim it, SMAPI applies *neither* and logs an error).
- Use `e.Name` (not `NameWithoutLocale`): custom `Mods/…` assets have no locale variants. The existing `Data/*` branches correctly use `NameWithoutLocale` (those assets *do* have `.fr-FR`-style variants); the new branch is the exception. ⚠️ Test-double tripwire: `AssetRegistrationTests.CreateAssetRequestedEventArgs` currently mocks **only** `IAssetInfo.NameWithoutLocale` — a test for the texture branch must also mock `IAssetInfo.Name` (same `Mock<IAssetName>` pattern), or the branch is unreachable under test.
- `LoadFromModFile` paths are mod-folder-relative (`"Assets/compost.png"` — capital-A matches the on-disk `LivingRoots/Assets/` dir; spelling-exact, case behavior is platform-dependent so keep exact case). No `helper.ModContent.Load<Texture2D>` at runtime and no `GetActualAssetKey` indirection needed: nothing in game code consumes the texture by key except the `ObjectData.Texture` pointer set in Step 2.
- Do **not** patch/extend `Maps/springobjects` (`PatchImage`/`ExtendImage`): editing the vanilla sheet risks conflicts with every other mod and redistributes vanilla-adjacent layout assumptions. A dedicated texture is the documented 1.6 pattern (`"Texture": "Mods/{{ModId}}/Objects"` in the [Modding:Items custom-item example](https://wiki.stardewvalley.net/Modding:Items)).

**Step 2 — point the item at it** (pattern: [Migrate to 1.6 §Custom items](https://wiki.stardewvalley.net/Modding:Migrate_to_Stardew_Valley_1.6) — `Texture` + `SpriteIndex` per item):

```csharp
data[ModConstants.CompostItemId] = new StardewValley.GameData.Objects.ObjectData
{
    Name = "Compost",
    DisplayName = "Compost",
    Description = "Organic compost rich in microbial life. Restores 15 soil health to tilled tiles.",
    Type = "Basic",
    Category = ModConstants.CompostCategoryId,
    Price = 10,
    Texture = ModConstants.CompostTextureName,  // "Mods/lfgranja.LivingRoots/Compost"
    SpriteIndex = 0,
};
```

**Step 3 — constant** (in `LivingRoots/Constants.cs`, next to `CompostItemId`):

```csharp
public const string CompostTextureName = "Mods/lfgranja.LivingRoots/Compost";
```

Downstream effects that come for free once this is done (no extra work):

- **Crafting recipe icon:** `Data/CraftingRecipes` output display reuses the output item's icon — the bin recipe (`ModEntry.cs:122`) and any future compost recipe pick it up automatically.
- **Machine output:** `Data/Machines` `OutputItem.ItemId = (O)LivingRoots.Compost` (`ModEntry.cs:169`) renders this icon on harvest; nothing to change in machine data.
- **Shops/tooltips/debris:** all item draws (`Object.drawInMenu`, world debris, shipping UI) resolve through `ObjectData.Texture/SpriteIndex`.

### 2.3 What is explicitly NOT required for the icon to work

- No Content Patcher pack, no `content.json`, no DynamicTokens.
- No `helper.ModContent.Load<Texture2D>` call at runtime — `LoadFromModFile` registers the asset lazily in the content pipeline; eager loading would waste GPU texture instances ([Content API §Read mod assets](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content) warns against per-frame loads; load-once discipline applies if caching is ever added).
- No `helper.GameContent.InvalidateCache` — needed only for hot-swapping art at runtime, not for load-time provision.
- No changes to `Data/CraftingRecipes` or `Data/Machines` for icon purposes.

### 2.4 Follow-up (not this delivery): per-locale DisplayName

`Data/Objects` is requested per locale (`Data/Objects.fr-FR`, …). A locale-aware name would set `DisplayName` from `helper.Translation.Get("compost.name")` inside the `e.Edit` closure. **Deferred** because `Translation.Get` resolves for the *current* locale at edit time while the pipeline caches per locale asset — correct behavior across mid-session language switches needs verification against SMAPI 4.x caching semantics, and FR-008a does not require it. English literals stay, matching current `default.json`.

---

## 3) Spec de arte (pixel-art, Stardew house style)

Sources: [Tutorial: How to make pixel art](https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art) (stardewmodding.wiki.gg, rev. 2026-06-27), the [Sundrop Art Guide](https://web.archive.org/web/20220204164234/https://sundrop.kvdk.net/files/SundropArtGuide.pdf) (community standard for mimicking ConcernedApe's style), vanilla `springobjects` reference. Tooling: Aseprite, GIMP, Krita, or Paint.NET ([Helpful Programs](https://stardewmodding.wiki.gg/wiki/Helpful_Programs)).

### 3.1 Silhouette — heap, not sack (accessibility-critical)

Vanilla fertilizers are all **bags/sacks**: Basic Fertilizer `(O)368` (pale sack), Quality Fertilizer `(O)369`, Speed-Gro `(O)465`, Deluxe Speed-Gro, Hyper Speed-Gro, Retaining Soils (dark sacks). To be instantly distinguishable (spec FR-005 "identifiable as compost", SC-002 "every step produces a visible in-game result"):

- Draw a **loose mound/heap** with 2–3 granular lumps and an uneven top edge — organic, unbagged. A sack/bag shape is explicitly rejected: at 16×16 it would read as "another fertilizer" next to `(O)368/369/465`.
- Add a **small living accent**: one two-leaf sprout *or* 2–3 short microbial "spark" flecks in mod green (`#15803D` Healthy / `#166534` CompostReady — normalize the placeholder's `#2E7D32` to one of these on redraw). Small area (<15% of pixels) so the read stays "soil", with a "living" cue tying into the LivingRoots palette.
- **Shape first, color second.** Shape is the primary channel for colorblind players and for the 1× (16 px) read; hue must never be the only differentiator (see `research-wcag-gaming.md` in repo root for the project's a11y baseline). The mound-vs-sack contrast is the whole differentiator against the fertilizer family — verify side-by-side at 4× against `(O)368`, `(O)369`, `(O)465`.

### 3.2 Palette — earth tones + one living green

Base the browns on the current placeholder (already in-family) and expand to full Stardew shading depth:

| Role | Hex | Notes |
|---|---|---|
| Outline / deepest shadow | `#26180F` | Reuse current darkest brown; outlines are dark brown, **never pure black** |
| Shadow | `#372216` | Current mid-dark; shift slightly cool vs base (hue-shifting rule) |
| Base | `#553722` | Current dominant brown (46 px) — mound body |
| Light | `#7A5232` | Current light brown — top-left planes (light source convention) |
| Warm highlight | `#A8763E`-ish | 1–3 px on the top/upper-left crest; highlights shift **warm**, shadows shift **cool** ([pixel-art tutorial §Hue shifting](https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art)) |
| Living accent | `#15803D` / `#166534` | `ModConstants.HealthyColor` / `CompostReadyColor` — sprout/flecks only |

Rules:

- **3–4 shades per hue maximum** (SDV item-sprite colour-limit convention; same section as above).
- **1 px outline** in the darkest brown, broken (1–2 px gaps) on the lit edge to let the highlight breathe — standard ConcernedApe item treatment; verify against vanilla fertilizer sprites at 4× zoom.
- No saturation clash: keep browns muted; the green accent carries the saturation budget.

### 3.3 Shading, light, and scale checks

- Light source **top / top-left** for items. (Caution: the pixel-art tutorial's explicit light-source note covers *portraits* — top-right & slightly forward. Items follow the top/top-left convention visible across vanilla object sprites; do not mix the two.)
- Granular texture via 1-px dithering clusters on the mound body, not noise: 2–3 darker pits + 2–3 lighter grains.
- **Acceptance at two scales:** crisp at 1× (16×16, world drop) and clean at 4× (64×64, inventory — the standard in-game display scale). Check against three backgrounds: tilled soil, inventory dark slot, tooltip/parchment.
- Preserve the current file's best trait: it already passes the 6-color discipline (SDV item convention is 3–4 shades per hue — [pixel-art tutorial §Colour limits](https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art)) — the redraw should land at roughly 6–9 unique colors, not 20.

### 3.4 Licensing — hard constraint

- Stardew Valley art and `springobjects.png` are **proprietary (ConcernedApe LLC)**. This repo's `AGENTS.md` already quarantines `GamePath` assemblies out of the tree for exactly this reason.
- The new icon MUST be **original pixel art**. Reference vanilla sprites for style (pose, outline weight, shading depth) but **do not copy, trace, recolor, or vendor any vanilla pixel** into `LivingRoots/Assets/`. Do not commit extracted `springobjects` crops, `i.png`-style vanilla dumps, or "edited vanilla fertilizer" derivatives.
- Recommended tooling: Aseprite, GIMP, Krita, or Paint.NET ([Helpful Programs](https://stardewmodding.wiki.gg/wiki/Helpful_Programs)). Optional: keep the layered source (`.aseprite`/`.xcf`) next to the export outside the shipped `Assets/` tree or in `docs/` — never ship sources inside `Assets/` (everything under it is copied to build output).

---

## 4) Verificação in-game + teste automatizado

### 4.1 Manual in-game checklist (maintainer machine; no automation substitutes — spec Assumptions)

Setup: build with `GamePath=$GP dotnet build … -c Release`, run via SMAPI, new or test save.

| # | Step | Expected | Failure signature → cause |
|---|---|---|---|
| V1 | SMAPI console: no red errors on load; `list_items` shows `LivingRoots.Compost` | Clean log; ID resolves | `Couldn't load asset Mods/lfgranja.LivingRoots/Compost` → wrong relative path in `LoadFromModFile` (paths are mod-folder-relative, case-insensitive but spelling-exact) or duplicate loader with higher priority |
| V2 | Give item (CJB Item Spawner or SMAPI `player_additem`), open inventory | **Mound icon**, not vanilla sprite #0, not 🛇 | Vanilla sprite → `Texture`/`SpriteIndex` not applied (edit ordering, wrong dictionary key — key must equal `CompostItemId` exactly). 🛇 error item → `Data/Objects` entry missing at load (exception in `e.Edit` swallowed, or locale asset path issue) |
| V3 | Hover tooltip | Name "Compost" + full description, matching `i18n/default.json` | Mismatch → literals drifted from canonical keys; reconcile (keep literals until §2.4 follow-up) |
| V4 | Toolbar select + drop on ground | 16×16 world sprite reads as compost at 1×; no halo/fringe | Halo → semi-alpha fringe in PNG (§2.1); re-export with hard alpha |
| V5 | Full loop: bin → 2 days → collect → apply to tilled tile | Collected item stacks with same icon; application consumes exactly 1; +15 health overlay (FR-007/FR-010) | Icon differs between stages → second `ObjectData` registration path or stale cache; run V1–V4 after `player_additem` vs bin-harvested item |
| V6 | Save → reload → re-inspect icon | Identical icon (asset is load-time, not save data) | Different icon → save contains legacy item with different ID; check `QualifiedCompostItemId` handling, not art |
| V7 | Second client has mod installed (multiplayer smoke) | Same icon on farmhand screen | Icon is per-client pipeline content from each client's own mod files — **every client needs the mod**; no netcode carries textures. Missing icon on farmhand = mod absent there, not a sync bug |

Screenshots at 4× for V2 (inventory) and V4 (world) go in the PR (repo PR template requires in-game manual testing).

### 4.2 Automated tests (extend `LivingRoots.Tests/AssetRegistrationTests.cs`)

These run under the existing suite (`GamePath=$GP dotnet test --no-build -c Release`) and need no game launch. Current file state (verified 2026-10-05): 4 facts covering `Data/CraftingRecipes` (T010 recipe contract), `Data/Objects` (bin + compost name/description/type/category/price — **no `Texture`/`SpriteIndex` assertions yet**), `Data/Machines` (`(BC)` output rules), and unrelated-asset no-op. Helpers `CreateAssetRequestedEventArgs` (reflection-constructed `AssetRequestedEventArgs` + `Mock<IAssetName>`) and `ApplyEditOperation` (`Mock<IAssetData>` → `AsDictionary`) are reusable as-is:

1. **Data assertions** (T022-adjacent): after invoking `ModEntry.OnAssetRequested` with a stubbed `AssetRequestedEventArgs` for `Data/Objects`, assert the `LivingRoots.Compost` entry has `Texture == ModConstants.CompostTextureName` and `SpriteIndex == 0`. (Follows the existing T010/T013 test pattern for recipe + bin registration.)
2. **Texture-asset assertion**: stub request for `Mods/lfgranja.LivingRoots/Compost`, assert the handler calls `LoadFromModFile<Texture2D>` with path `Assets/compost.png` (mock `IModEvents`/`AssetRequestedEventArgs` per existing test doubles in `LivingRoots.Tests/Fixtures`). ⚠️ Requires extending `CreateAssetRequestedEventArgs` to also mock `IAssetInfo.Name` (today only `NameWithoutLocale` is mocked, so an `e.Name.IsEquivalentTo(...)` branch never fires under test) — see §2.2.
3. **PNG-format guard** (no `GamePath`, no GPU, no `System.Drawing`): parse the IHDR chunk with plain file I/O and assert width == 16, height == 16, bit depth == 8, color type == 6 (truecolor+alpha), interlace == 0. Fails the build on accidental re-export as 32×32, paletted, or interlaced PNG. Keep it dependency-free — `System.Drawing.Common` is a non-starter on Linux CI. Resolve the PNG path by walking up from the test binary (same technique as `ArchitecturalPlanTests`, which locates `Stardew-LivingRoots.sln`), not by hardcoded relative path.
4. **Palette-budget guard** (optional, cheap): decode PNG pixels (pure-C# inflate over IDAT, or shell out to nothing — keep in-test) and assert unique opaque colors ≤ 12 and at least 30% fully-transparent pixels (icon must not be a full-bleed square; current file: 141/256 = 55% transparent, 5 opaque colors). Only if (3) proves the harness pattern first — YAGNI otherwise.

Repo-specific tripwires while editing:

- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` is the always-on CI gate — run `dotnet format` after edits; no expression-bodied members (`.editorconfig`, severity silent so the gate won't catch it — convention is manual).
- `FeatureStatusTests` reads `README.md` as an embedded resource asserting three `"(Planned)"` strings — **do not touch README claims** until FR-016/SC-006 verification (V1–V6) actually passes, and rebuild before re-running tests (`--no-build` reads a stale embedded copy).
- End every edited `.md` with a trailing newline (ArchitecturalPlanTests enforces it under `LivingRoots/docs/…`; adopt everywhere).

---

## 5) Out-of-scope (explicitly not this delivery)

1. **Composting-bin sprites** (`composting_bin_{empty,processing,ready}.png`, 16×32): analogous wiring via `Data/BigCraftables` (`Texture` defaults to `TileSheets/Craftables`, `SpriteIndex` per [Modding:Big craftables](https://wiki.stardewvalley.net/Modding:Big_craftables)) — separate briefing/task. Note the current code registers the bin in `Data/Objects` (`ModEntry.cs:131`), which the 004 machine-native design (FR-001a) will revisit; icon work must not preempt that.
2. **Machine state visuals/audio** (`Data/Machines` sprites, `Assets/Audio/*.ogg` placeholders): untouched.
3. **Content Patcher route**: fallback only, per spike exit criteria. No `content.json`, no CP `Load` action (`{"Action":"Load","Target":"Mods/{{ModId}}/…","FromFile":…}`), no CP dependency in `manifest.json`.
4. **Localized item names/descriptions** beyond `default.json` canonical text (§2.4 follow-up).
5. **Category decision** (`-26` vs `-19`, §1.4): owned by the item-registration task (T022), recorded in its PR — this briefing only constrains that the icon wiring is identical either way.
6. **Known inherited gap**: artisan-goods exclusion in the compostable set (spec Assumptions — owned by the soil-decay spec, FR-003 inherits by reference). Not reopened here.
7. **Soil overlay art and refusal messaging**: covered by Feature 001 interpolation rules and FR-008a (`Game1.showRedMessage` + existing i18n keys); FR-008a explicitly states refusals MUST NOT require new artwork.

---

## Sources

- Stardew Valley Wiki — [Modding:Objects](https://wiki.stardewvalley.net/Modding:Objects) (Texture/SpriteIndex, defaults to `Maps/springobjects`; rev. 8 Aug 2026)
- Stardew Valley Wiki — [Modding:Items](https://wiki.stardewvalley.net/Modding:Items) (custom-item pattern `Mods/{{ModId}}/Objects`, categories incl. -19 fertilizer / -26 artisan goods, 🛇 error item; rev. 4 Aug 2026)
- Stardew Valley Wiki — [Modding:Big craftables](https://wiki.stardewvalley.net/Modding:Big_craftables) (bin-side analogue)
- Stardew Valley Wiki — [Migrate to Stardew Valley 1.6](https://wiki.stardewvalley.net/Modding:Migrate_to_Stardew_Valley_1.6) (per-item Texture/SpriteIndex; `ParentSheetIndex` not a valid identifier)
- Stardew Valley Wiki — [Modder Guide/APIs/Content](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Content) (`LoadFromModFile`, `e.Edit`, `IsEquivalentTo`, `AssetLoadPriority`, `GetActualAssetKey`; rev. 21 May 2026)
- Stardew Valley Wiki — [Modder Guide/APIs/Translation](https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Translation) (`i18n/` structure, locale fallback)
- Stardew Valley Wiki — [Modding:Tokenizable strings](https://wiki.stardewvalley.net/Modding:Tokenizable_strings) (`[LocalizedText]`, token rules)
- stardewmodding.wiki.gg — [Tutorial: How to make pixel art](https://stardewmodding.wiki.gg/wiki/Tutorial:_How_to_make_pixel_art) (portrait light source, colour limits, hue shifting; rev. 27 Jun 2026)
- Sundrop team — [Sundrop Art Guide](https://web.archive.org/web/20220204164234/https://sundrop.kvdk.net/files/SundropArtGuide.pdf) (CA-style reference)
- spacechase0/JsonAssets — [`ObjectData.cs`](https://github.com/spacechase0/StardewValleyMods/blob/develop/framework/JsonAssets/Data/ObjectData.cs) (dedicated-texture-per-item precedent: `Texture = $"JA\\Object\\…"`, `SpriteIndex = 0`)
- Local evidence: `LivingRoots/ModEntry.cs:114-151`, `LivingRoots/Constants.cs:63-74`, `LivingRoots/Assets/compost.png` (16×16 RGBA, 6 colors — PIL census 2026-10-05), `LivingRoots/LivingRoots.csproj:26-28`, `LivingRoots/manifest.json` (UniqueID `lfgranja.LivingRoots`), `LivingRoots/i18n/{default,en}.json`, `LivingRoots.Tests/AssetRegistrationTests.cs` (4 facts, `NameWithoutLocale`-only mock), `specs/004-playable-compost-loop/{spec,plan,tasks}.md` (FR-005/SC-002, T022), `specs/002-soil-decay-compost/data-model.md:91`, `specs/003-composting-tests/briefings/compost-icon.md` (superseded pre-1.6 route)
