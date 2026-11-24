# Asset Briefings — Composting State Machine Tests (spec 003)

**Feature**: `specs/003-composting-tests/` · **Date**: 2026-09-30 · **Status**: open

Spec 003 is a **test-only** feature — it ships no gameplay assets of its own. However it exercises
production code paths that reference content assets which **do not exist in this repository yet**.
This directory holds one briefing per missing asset, so they can be built and tracked independently
of the test suite.

## Inventory

| # | Asset | Type | Blocks | Severity | Briefing |
|---|-------|------|--------|----------|----------|
| 1 | `LivingRoots.Compost` item definition | `Data/Objects` entry + `content.json` | 2 tests in `CompostApplicationServiceTests`, plus all compost behaviour in-game | **Blocking** | [compost-item.md](./compost-item.md) |
| 2 | Compost icon sprite | `.png` in a spritesheet | Visual polish only; item works without it | Non-blocking | [compost-icon.md](./compost-icon.md) |
| 3 | `LivingRoots.CompostingBin` item definition | `Data/Objects` entry | Nothing — constant is orphaned | Deferred | [composting-bin-item.md](./composting-bin-item.md) |

## Not assets — do not brief these

These were investigated and ruled out. Listed so nobody re-audits them.

| Thing | Why it is not an asset |
|-------|------------------------|
| Overlay fill texture | Generated at runtime as a 1×1 white `Texture2D` (`ModController.cs:784`). No file. |
| Accessibility patterns (Stripes/Dots) | Drawn with `SpriteBatch.Draw` rectangles in `VisualizationService.DrawStripesPattern` / `DrawDotsPattern`. No tileable texture file. |
| Tooltip / feedback fonts | Uses the game's native `Game1.smallFont`. |
| Test items (seeds, stone, etc.) | Constructed via `ItemFactory` with `Category` set directly; never need real vanilla assets. |
| `Game1.content` / `ItemRegistry` / `Game1.sounds` | Stubbed in-process by `LivingRoots.Tests/Fixtures/GameStateFixture.cs` — no files involved. |

## The 12 remaining test failures are NOT all asset problems

The current gate state is **798 passing / 12 failing**. The failures split into two unrelated causes:

| Cause | Count | Tests | Is it an asset? |
|-------|-------|-------|-----------------|
| `new Farmer()` throws `NullReferenceException` | 10 | `CompostingBinServiceTests` (all that call `CollectCompost`) | **No** — needs a production seam (`IPlayerInventory`) so `CollectCompost` stops taking a concrete `Farmer`. |
| `QualifiedItemId` never equals `ModConstants.CompostItemId` | 2 | `CompostApplicationServiceTests.TryApplyCompost_ValidTile_IncreasesHealth`, `.TryApplyCompost_HealthCappedAtMax` | **Yes** — fixed by briefing #1. |

The 10 `Farmer` failures are a design problem, not a content problem. See the note at the end of
[compost-item.md](./compost-item.md#why-the-other-10-failures-are-separate).