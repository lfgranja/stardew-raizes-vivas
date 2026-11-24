# Research: Game Mod Specification Context & Implementation Detail Acceptability

**Feature**: Soil Health Visualization (001-soil-health-visualization)
**Date**: 2026-09-08
**Purpose**: Determine the appropriate level of technical detail for feature specifications in the SMAPI/Stardew Valley modding domain.

---

## 1. Are game mod specifications typically more implementation-heavy than regular software specs?

### Finding: Yes — but with important caveats.

Game mod specifications are typically **more implementation-heavy** than specifications for greenfield or framework-agnostic software, for three structural reasons:

#### 1.1. Tight coupling to a closed ecosystem

Mods are not standalone applications; they are **guest code** running inside a host game's process. The host game (Stardew Valley) dictates the runtime environment: .NET 6, MonoGame for rendering, SMAPI for lifecycle and event access, and the game's own `Game1` class for state. A mod spec that omits these constraints is incomplete because the implementer cannot reason about feasibility, performance, or compatibility without them.

> **Source**: The SMAPI getting-started guide explicitly states: "A SMAPI mod uses the SMAPI modding API to extend the game logic... SMAPI mods are written in C# using .NET, and Stardew Valley uses MonoGame for the game logic." The guide also notes that mods must target .NET 6 specifically because "that's the version installed and used by the game." [Modding:Modder Guide/Get Started](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started)

#### 1.2. Event-driven architecture is the primary design surface

In conventional software, a spec might describe behavior ("the system shall respond to user input"). In SMAPI modding, the **event model is the architecture** — the available events (`RenderedHud`, `ButtonPressed`, `DayStarted`, `SaveLoaded`, etc.) are not implementation choices but **constraints imposed by the framework**. A spec that says "render overlays" without specifying which render event to hook is underspecified, because choosing the wrong event (e.g., `UpdateTicked` instead of `RenderedHud`) produces visible bugs (drawing under the HUD, or drawing before the world exists).

> **Source**: The SMAPI events reference documents over 40 distinct events across Content, Display, GameLoop, Input, Multiplayer, Player, and World categories, each with precise timing semantics (e.g., "RenderedHud fires after HUD rendering, appropriate for in-game tile overlays"). [Modding:Modder Guide/APIs/Events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events)

#### 1.3. Coordinate systems and rendering pipelines are domain fundamentals

Stardew Valley uses three coordinate systems (tile, absolute, screen) with explicit conversion formulas, plus separate UI/non-UI scaling modes. A spec for a visualization mod that does not reference these coordinate systems is ambiguous — "draw on tile (5, 10)" is meaningless without specifying which coordinate space and whether UI scaling applies.

> **Source**: The game fundamentals guide documents tile coordinates, absolute positions, screen positions, zoom levels (75%–200%), and UI scaling (75%–150%) as core concepts that "are useful for modders." [Modding:Modder Guide/Game Fundamentals](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals)

#### Caveat: "Implementation-heavy" ≠ "Implementation-prescriptive"

Being implementation-heavy in the mod domain means **naming the framework constraints that bound the solution space** — not dictating algorithmic minutiae that the implementer can freely choose. The spec should say "hook `RenderedHud`" (framework constraint) but not "use a `for` loop with `i++`" (implementation detail).

---

## 2. What's the convention for SMAPI/Stardew Valley mod documentation?

### Finding: Layered documentation with framework-aware contracts.

The SMAPI ecosystem follows a **layered documentation convention** that separates concerns by audience:

#### 2.1. Manifest.json — Machine-readable identity

Every SMAPI mod must have a `manifest.json` with fields: `Name`, `Author`, `Version`, `Description`, `UniqueID`, `EntryDll` (or `ContentPackFor`), and optional `MinimumApiVersion`, `Dependencies`, and `UpdateKeys`. This is the **deployment spec** — it tells SMAPI how to load, order, and validate the mod.

> **Source**: "Every SMAPI mod or content pack must have a manifest.json file in its folder. SMAPI uses this to identify and load the mod, perform update checks, etc." [Modding:Modder Guide/APIs/Manifest](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest)

#### 2.2. Wiki reference — API surface documentation

The [Stardew Valley Wiki modding section](https://stardewvalleywiki.com/Modding:Modder_Guide) serves as the canonical API reference. It is organized as:
- **Getting Started** (tutorial)
- **Game Fundamentals** (coordinate systems, time format, net fields)
- **API Reference** (Manifest, Events, Config, Content, Data, Input, Logging, Reflection, Multiplayer, Translation, Utilities)
- **Advanced Topics** (Content Packs, Console Commands, Mod Integrations, Harmony Patching)

Each API page documents the **method signatures, event arguments, and usage constraints** — not what the mod should do, but what the framework allows.

#### 2.3. NexusMods / README — User-facing feature docs

Published mods on [NexusMods](https://www.nexusmods.com/stardewvalley) typically include a README describing features, configuration options, compatibility notes, and installation instructions. This is the **user-facing spec** — it describes what the mod does, not how it does it.

#### 2.4. In-repo contracts — Developer-facing interface specs

For mods with public source code (nearly 70% of SMAPI mods have public source, per the getting-started guide), the repository itself serves as documentation. The convention in well-structured mods (like this project) is:
- **Domain interfaces** (`IVisualizationService`, `IColorInterpolationService`) define the contract
- **Implementation classes** in `Services/` fulfill the contract
- **Event handlers** in `Controllers/` wire SMAPI events to services

> **Source**: "Nearly 70% of SMAPI mods have public source code." [Modding:Modder Guide/Get Started](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started)

#### 2.5. Summary of the convention layer

| Layer | Audience | Content | Format |
|-------|----------|---------|--------|
| `manifest.json` | SMAPI runtime | Identity, dependencies, entry point | JSON |
| Wiki reference | Modders | API surface, event semantics, constraints | Wiki pages |
| README / NexusMods | Players | Features, config, compatibility | Markdown / HTML |
| In-repo contracts | Developers | Interface signatures, pre/postconditions | C# interfaces + XML docs |
| Feature specs | Team / AI | Behavior, acceptance scenarios, framework hooks | Markdown |

---

## 3. When is it acceptable to include framework-specific details in a specification?

### Finding: Framework details are acceptable — and often necessary — when they are **constraints on the solution space** rather than **choices by the spec author**.

Based on the SMAPI documentation patterns and the existing project's spec/contract structure, framework-specific details fall into three categories:

#### 3.1. ALWAYS include: Framework-imposed constraints

These are not optional — they are facts about the runtime environment that the implementer must know:

- **Event names and timing**: "Subscribe to `RenderedHud`" is a constraint because SMAPI only provides specific render events. The spec must name the event; otherwise the implementer might choose the wrong one.
- **Coordinate systems**: "Tile coordinates use (x, y) with (0,0) at top-left" is a constraint from the game's rendering pipeline.
- **Manifest requirements**: "The mod must declare `EntryDll` and `UniqueID`" is a constraint from SMAPI's loading mechanism.
- **Target framework**: "The mod targets .NET 6" is a constraint from the game's runtime.
- **Data persistence format**: "Soil health data is saved via SMAPI's `SaveData` event with a specific data model" is a constraint from the save/load lifecycle.

> **Source**: The SMAPI mod structure guide states: "A SMAPI mod must have a compiled DLL file... and a manifest.json file." It also documents that the `Entry` method is called early and that events like `GameLaunched`, `SaveLoaded`, or `DayStarted` must be used to access full features. [Modding:Modder Guide/APIs/Mod structure](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Mod_structure)

#### 3.2. SOMETIMES include: Framework conventions that reduce ambiguity

These are not strictly required but prevent the implementer from making incompatible choices:

- **MonoGame types in interfaces**: Using `Microsoft.Xna.Framework.Color` and `Microsoft.Xna.Framework.Graphics.SpriteBatch` in a contract is acceptable because MonoGame is the only rendering option in Stardew Valley. There is no alternative to choose from.
- **SMAPI event argument types**: Referencing `ButtonPressedEventArgs` or `DayStartedEventArgs` is acceptable because these are the framework's data shapes.
- **JSON serialization format**: Specifying that config uses `config.json` via SMAPI's `helper.ReadConfig<T>()` pattern is acceptable because it is the standard convention.
- **i18n folder structure**: Mentioning the `i18n/` folder for translations is acceptable because SMAPI expects this structure.

> **Source**: The SMAPI getting-started guide notes: "For serialization, SMAPI usually uses NewtonSoft" and documents the `i18n` folder for translations. [Modding:Modder Guide/Get Started](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started)

#### 3.3. RARELY include: Implementation choices disguised as spec

These should generally **not** appear in a feature spec because they are decisions the implementer should make:

- **Specific algorithm choices**: "Use a `Dictionary<TileKey, HealthValue>` for caching" — unless the data model document already defines this.
- **Private method signatures**: Internal helper methods are implementation details.
- **Specific loop constructs or LINQ patterns**: How the code iterates is an implementation detail.
- **Third-party library choices within the framework**: If SMAPI offers multiple ways to achieve something, the spec should describe the goal, not the method.

#### 3.4. Decision framework: Should this framework detail be in the spec?

Use this test:

| Question | If Yes | If No |
|----------|--------|-------|
| Does the implementer need this information to write correct code? | Include it | Consider omitting |
| Is this detail imposed by the framework (not chosen by the spec author)? | Include it | Consider omitting |
| Would omitting this detail lead to an incompatible or broken implementation? | Include it | Consider omitting |
| Is this detail about *what* the system does vs. *how* it does it? | Include it (what) | Omit it (how) |
| Is there an alternative the implementer could freely choose instead? | Omit it (let them choose) | Include it (no alternative) |

---

## 4. Application to This Project

The existing spec structure for `001-soil-health-visualization` already follows these conventions well:

- **`spec.md`** uses behavioral acceptance scenarios ("Given/When/Then") — appropriate for feature behavior.
- **`contracts/`** uses C# interface definitions with MonoGame types (`SpriteBatch`, `Color`, `GameTime`) — appropriate because MonoGame is the only rendering option.
- **`data-model.md`** defines the persistence DTO shape — appropriate because SMAPI's save/load lifecycle constrains the format.
- **`research.md`** documents framework-specific decisions (SpriteBatch rendering, SMAPI event selection, Color.Lerp interpolation) — appropriate because these are framework-constrained choices.

The project's layered approach (spec → contracts → data-model → research) correctly separates **what** the system does from **how** the framework constrains the implementation.

---

## 5. Sources

| # | Source | URL |
|---|--------|-----|
| 1 | Stardew Valley Wiki — Modding:Modder Guide/APIs | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs |
| 2 | Stardew Valley Wiki — Modding:Modder Guide/Get Started | https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started |
| 3 | Stardew Valley Wiki — Modding:Modder Guide/APIs/Manifest | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest |
| 4 | Stardew Valley Wiki — Modding:Modder Guide/APIs/Events | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events |
| 5 | Stardew Valley Wiki — Modding:Modder Guide/APIs/Mod structure | https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Mod_structure |
| 6 | Stardew Valley Wiki — Modding:Modder Guide/Game Fundamentals | https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals |
| 7 | GitHub — Pathoschild/SMAPI | https://github.com/pathoschild/SMAPI |
| 8 | SMAPI Official Site | https://smapi.io/ |
| 9 | NexusMods — SMAPI (Stardew Modding API) | https://www.nexusmods.com/stardewvalley/mods/2400 |
| 10 | Gitbook — How to write a game design document | https://www.gitbook.com/blog/how-to-write-a-game-design-document |
| 11 | Game Developer — The Anatomy of a Design Document | https://www.gamedeveloper.com/design/the-anatomy-of-a-design-document-part-1-documentation-guidelines-for-the-game-concept-and-proposal |
| 12 | Perforce — How to Define API Requirements | https://www.perforce.com/blog/aka/api-requirements-what-consider |
