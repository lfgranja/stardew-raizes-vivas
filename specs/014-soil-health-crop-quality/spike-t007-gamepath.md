# GamePath Spike Result — T007 (spec 014)

**Date**: 2026-11-01
**Binary**: `/home/luis/.local/share/Steam/steamapps/common/Stardew Valley/Stardew Valley.dll` (Stardew Valley 1.6.x)
**Method**: decompiled `Crop` class via cached decompilation (`/tmp/opencode/decomp-crop/StardewValley.Crop.decompiled.cs`); `strings` on the assembly for `HoeDirt`/`Object`.

## Confirmed member names

| Member (spec 014 Q2) | Status | Signature / notes |
|---|---|---|
| `Crop.harvest` | ✅ confirmed | `public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null, bool isForcedScytheHarvest = false)` — **virtual method**, so interception requires Harmony (or subclass replacement, not feasible). Public-field manipulation cannot intercept a method call. |
| `Crop.newDay` | ✅ confirmed | present (used by spec 005 growth modifier) |
| `Crop.dayOfCurrentPhase` | ✅ confirmed | present |
| `Crop.phaseDays` | ✅ confirmed | present |
| `HoeDirt.dayUpdate` | ✅ confirmed | `dayUpdate` method exists on `HoeDirt` |
| `Object.Quality` | ✅ confirmed | public property with `get_Quality` / `set_Quality` — can be set directly |

## Implications for hook strategy (Q2)

- `harvest` is a **virtual method**, not a field. Public-field manipulation (the preferred strategy) **cannot** intercept it. Harmony is **required** as the fallback for harvest-quality interception.
- `Object.Quality` is a public settable property, so once the hook fires, setting quality is direct (no reflection needed).
- The project currently has **zero** Harmony dependency (`Lib.Harmony`). Adding it is an architectural decision gated by this spike.

## Decision required (not executed this session)

1. **Add `Lib.Harmony` (0Harmony) NuGet dependency** to `LivingRoots.csproj` — required to patch `Crop.harvest`.
2. **Write a Harmony `Prefix`/`Postfix` patch** on `Crop.harvest` that:
   - reads the tile's soil health from `SoilHealthService` (host-authoritative; replicated deterministic roll per A8),
   - applies tier rules (Poor → force `Quality = 0`; Healthy → `+0.08` on `p`; extra-harvest `+0.10`),
   - drives `VisualizationService` transient feedback,
   - handles inventory-full (main item vanilla-blocked; extra item debris).
3. **Testing**: Harmony patches cannot be exercised in the headless xUnit host (no game runtime). Verification relies on the in-game Gate 3 session (T075) + manual SMAPI play-through. RED-phase tests (T024–T035) must therefore test the **logic** (tier→outcome mapping) via a testable seam (e.g., a `HarvestQualityService` that the hook delegates to), not the patch itself.

## Gate status

- T007 (GamePath spike): **COMPLETE** — member names confirmed; Harmony required (fallback activated per Q2).
- T017 (hook design doc): covered above.
- T044–T054 (harvest-hook implementation): **BLOCKED** until (1) Harmony dependency is added and (2) RED-phase tests (T024–T035) are written and failing. Neither is done this session.

## Recommendation

Proceed with adding `Lib.Harmony` and writing a `HarvestQualityService` (testable seam) + Harmony patch, behind RED-phase tests. This is a substantial engineering step; recommend a dedicated session with the GamePath build available for runtime verification.
