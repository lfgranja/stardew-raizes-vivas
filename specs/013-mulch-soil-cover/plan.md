# Implementation Plan: Mulch / Soil Cover (Epic 4)

**Branch**: `013-mulch-soil-cover` | **Date**: 2026-10-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/013-mulch-soil-cover/spec.md`

**Note**: This plan was filled in by the `/speckit-plan` command.

## Summary

Add "Mulch" (cobertura morta) as a craftable item that zeroes soil decay on tilled tiles for 7 days. Implements `IMulchApplicationService` (DDD pattern), hooks into `ModController.OnButtonPressed` (mirroring compost), persists `HasMulch` + `DaysRemaining` via `ModDataService` (sparse cache), and renders a brown tinted overlay with `Dots` pattern via `IVisualizationService`. Mulch is purely defensive (no health boost, no decay) — complements compost (restore) to complete the "build → protect → harvest" loop. Lowest-cost complete feature on the roadmap; strongest agronomic evidence (runoff −47%, soil loss −76%).

## Technical Context

**Language/Version**: C# 11 / .NET 6 (mod) + .NET 6 test project.

**Primary Dependencies**: SMAPI, xUnit + Moq (tests), existing `SoilDecayService`, `CompostApplicationService` (pattern template), `ModDataService`, `IVisualizationService`, `ModConstants`.

**Storage**: Stardew Valley save system via `ModDataService` (sparse cache, `*_KeyPrefix` keys, `MaxTilesPerLocation`/`MaxTilesPerSave` bounds). No new persistence mechanism.

**Testing**: xUnit + Moq + FsCheck (property-based). One test file per subject. New tests: `MulchApplicationServiceTests.cs`, `MulchDataTests.cs`, `MulchOverlayTests.cs`. Baseline: 1436 tests passing.

**Target Platform**: Stardew Valley via SMAPI (Windows/Linux/macOS).

**Project Type**: Mod feature (DDD layered: Domain → Services → Controllers). No new directories; follows existing conventions.

**Performance Goals**: O(1) per-tile mulch check in `SoilDecayService.ProcessDayStart`. Overlay rendering reuses existing cache (FR-010). No new render passes.

**Constraints**: `CheckForOverflowUnderflow=true`; no hardcoded values (all in `ModConstants`); no `.Result`/`.Wait()`; no global `NoWarn`; host-authoritative writes in multiplayer (Principle VI); sparse cache (zeros omitted).

**Scale/Scope**: 1 new item (16×16 sprite), 1 recipe, 1 service interface + impl, 1 controller hook, 5 new constants, 4 i18n keys, ~3 test files. Estimated: small (days).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (DDD)**: Respected — `IMulchApplicationService` in `Domain/Interfaces/`, impl in `Services/`, `new` only in `ModEntry.cs`.
- **Principle II (Security-First)**: Respected — all paths sanitized via `ModDataService`, bounds-checked, overflow checking enabled.
- **Principle III (Async-Only)**: Respected — no `.Result`/`.Wait()`; `ExecuteWithConcurrencyGuard` for re-entrancy.
- **Principle IV (TDD)**: Respected — tests written before implementation (Phase 2 RED gate).
- **Principle V (Simplicity/YAGNI)**: Respected — mirrors existing compost pattern, no new abstractions beyond `IMulchApplicationService`.
- **Principle VI (Multiplayer)**: Respected — farm-keyed state via `SaveIdProvider`, host-authoritative writes, `Context.IsMainPlayer` guard, no lock across I/O.

**Quality Gates:**
- Gate 1 (Static Checks): `dotnet build`/`dotnet test`/`dotnet format` must pass.
- Gate 2 (Product-Level): mulch works without documentation; soil health in range (0-100); save/load round-trips preserve mulch state.
- Gate 3 (E2E): till → apply mulch → save → reload → verify mulch persists and decay is 0.

**Verdict**: PASS — no violations. No Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/013-mulch-soil-cover/
├── plan.md              # This file
├── research.md          # Phase 0 output (assessment research, reused)
├── data-model.md        # Phase 1 output (mulch tile state entity)
├── quickstart.md        # Phase 1 output (validation guide)
├── contracts/           # Phase 1 output (service contract)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
LivingRoots/
├── Constants.cs                    # MODIFIED: add MulchItemId, QualifiedMulchItemId, MulchDurationDays, MulchDecayMultiplier, MulchTag, MulchColor
├── Domain/
│   ├── Interfaces/
│   │   └── IMulchApplicationService.cs   # NEW
│   └── Visualization/
│       └── HealthCategory.cs       # unchanged (band scale)
├── Services/
│   ├── MulchApplicationService.cs  # NEW
│   ├── SoilDecayService.cs         # MODIFIED: check HasMulch → decay *= MulchDecayMultiplier
│   └── Visualization/
│       └── OverlayRenderer.cs      # MODIFIED: mulch tint + Dots pattern overlay
├── Controllers/
│   └── ModController.cs            # MODIFIED: OnButtonPressed hook for mulch application
├── ModEntry.cs                     # MODIFIED: wire IMulchApplicationService
├── Assets/
│   └── mulch.png                   # NEW: 16×16 sprite
└── i18n/
    ├── en.json                     # MODIFIED: add mulch keys
    └── default.json                # MODIFIED: add mulch keys

LivingRoots.Tests/
├── MulchApplicationServiceTests.cs  # NEW
├── MulchDataTests.cs                # NEW
└── MulchOverlayTests.cs             # NEW

Data/Objects (JSON, via OnAssetRequested):
  LivingRoots_Mulch — Category −26, ContextTags {"mulch_item", "compostable_item"}

Data/CraftingRecipes (JSON, via OnAssetRequested):
  LivingRoots_MulchRecipe — "771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"
```

**Structure Decision**: DDD layered, following existing compost pattern. No new directories. `IMulchApplicationService` in `Domain/Interfaces/`, impl in `Services/`. `new` only in `ModEntry.cs`. All constants in `ModConstants` (Constants.cs). Sparse cache for mulch state (zeros omitted).

## Phase 0: Research (consolidated)

See `.specify/assessments/mulch-soil-cover/research.md` (assessment research, reused). Summary of decisions:

- **D1 — MulchDecayMultiplier**: `0f` (full protection) for Y1. Seasonal multiplier does not modulate (0 × seasonal = 0). Partial protection (0.25f) deferred to balance pass.
- **D2 — Mulch on planted tiles**: Allowed. Tile terrain is still `HoeDirt`; no crop-presence guard (mirrors compost top-dressing).
- **D3 — Mulch + compost coexistence**: Both allowed on same tile. Complementary (restore + protect). Overlay: health color base + mulch tint/pattern layered on top.
- **D4 — Mulch removal**: Auto-expiry (7 days) + re-tilling (pickaxe ×2, vanilla precedent). No manual removal in Y1. Scythe-based removal deferred.
- **D5 — Health boost**: None. Mulch is purely defensive. Compost remains sole health-recovery path.
- **D6 — Overlay z-order**: Health color base → mulch tint (MulchColor, 0.5 opacity) → Dots pattern → dotted border for mulch+plant. To be verified during implementation.
- **D7 — Winter expiry**: Mulch day-countdown must run outside the Winter early-return guard in `SoilDecayService` (expiry is day-based, not season-based).
- **D8 — Persistence**: `HasMulch` + `DaysRemaining` via `ModDataService` with `*_KeyPrefix` keys, sparse cache (zeros omitted), respecting `MaxTilesPerLocation`/`MaxTilesPerSave`.
- **D9 — Elena dependency**: Shop sale criterion deferred. Craftable recipe (fiber ×10) is the primary acquisition path. Elena shop sale is a non-blocking acceptance criterion.

## Phase 1: Design & Contracts

### Entities

- **Mulch (item)**: `LivingRoots_Mulch`, Category −26, craftable, 16×16 sprite, ContextTags `{"mulch_item", "compostable_item"}`. Registered via `ModEntry.OnAssetRequested` (pattern `ModEntry.cs:116-151`).
- **Mulch tile state**: `HasMulch` (bool) + `DaysRemaining` (int), persisted per-tile via `ModDataService`, farm-keyed via `SaveIdProvider`. Sparse cache (zeros omitted).
- **IMulchApplicationService**: service contract for applying mulch to a tile, validating tile state, and consuming the item from inventory. Mirrors `ICompostApplicationService`.

### Contracts

- `IMulchApplicationService` interface: `TryApplyMulch(Farmer, HoeDirt, GameLocation)` → bool + refusal message key.
- `ISoilDecayService` extension: `tile.HasMulch` check → `decay *= MulchDecayMultiplier`.
- Overlay contract: `IVisualizationService` receives mulch state → renders `MulchColor` tint + `Dots` pattern.

### Quickstart

See `quickstart.md` for validation scenarios (craft mulch, apply to tilled tile, verify 7-day protection, verify expiry, verify invalid-tile message).

## Constitution Check (post-design)

- **Principle I (DDD)**: PASS — interface in Domain, impl in Services.
- **Principle II (Security-First)**: PASS — all paths sanitized, bounds-checked.
- **Principle III (Async-Only)**: PASS — no `.Result`/`.Wait()`; `ExecuteWithConcurrencyGuard`.
- **Principle IV (TDD)**: PASS — tests before implementation.
- **Principle V (Simplicity/YAGNI)**: PASS — mirrors compost, no new abstractions beyond `IMulchApplicationService`.
- **Principle VI (Multiplayer)**: PASS — farm-keyed state, host-authoritative writes, `Context.IsMainPlayer` guard.

**Verdict**: PASS — no violations. No Complexity Tracking entries needed.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | | |