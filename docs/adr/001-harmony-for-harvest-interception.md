# ADR-001: Harmony for Harvest Interception

**Date**: 2026-11-02
**Status**: Accepted
**Spec**: specs/015-harmony-harvest-seam

## Context

Spec 014 (soil-health-crop-quality) requires intercepting `Crop.harvest` to apply tier-based quality effects (Poor → 0 stars, Living → +0.08 quality + extra harvest, Moderate → vanilla). `Crop.harvest` is a `public virtual` method, so public-field manipulation (used for the growth modifier in spec 005) cannot intercept it. A Harmony patch is required.

## Decision

Use SMAPI-bundled Harmony 2.2.x via `<EnableHarmony>true</EnableHarmony>` (no separate version pin). Register via the code API (`new Harmony(ModManifest.UniqueID).Patch(...)`), not annotations or `PatchAll`. Use a non-suppressing two-phase patch (Prefix returns `true` for state capture + crop-scope guard; Postfix forces Quality=0 for Poor, applies Healthy boost as conditional upgrade, drives feedback, handles inventory-full). All patch methods are `static`, wrapped in `try/catch` with fallback to original game logic. Transpiler is the documented fallback (last resort).

All tier logic lives in a pure `HarvestQualityService` seam (xUnit/FsCheck-testable without a game runtime). The patch is a Humble Object verified only by Gate 3 in-game.

## Consequences

- First Harmony use in the project. SMAPI shows a stability warning at load (expected and acceptable).
- Harmony is higher risk on Android; accepted as "untested on Android" (consistent with desktop-only policy).
- Multiplayer: seam is pure/deterministic given inputs (replicated deterministic roll, A8). Patch runs locally on triggering client. Live 2-player verification (D1) deferred.
- Guard compliance: `ProductionSourceGuardTests` satisfied (seam references `ModConstants.SoilQualityHealthyBoost`/`SoilExtraHarvestHealthyBoost`).
