# Quickstart: Harmony Patch for Crop.harvest (HarvestQualityService Seam) — Validation Guide

**Feature**: `015-harmony-harvest-seam` | **Branch**: `015-harmony-harvest-seam`

---

## Prerequisites

1. Spec 014 band-scale migration complete (31/71) and CollectCompost fix (Q4) green.
2. T007 GamePath spike complete (member names confirmed — see `specs/014-soil-health-crop-quality/spike-t007-gamepath.md`).
3. Maintainer approval to add `Lib.Harmony` (0Harmony) as a dependency.
4. Stardew Valley + SMAPI installed (for Gate 3 in-game session).
5. .NET 6 SDK; repo builds (`dotnet build -c Release`).

## Setup (add Harmony)

1. Edit `LivingRoots/LivingRoots.csproj` — add `<EnableHarmony>true</EnableHarmony>` to the first `<PropertyGroup>`.
2. Build: `GamePath=... dotnet build Stardew-LivingRoots.sln --configuration Release` (expect 0 errors; SMAPI-bundled Harmony 2.2.x).
3. Verify SMAPI log at launch shows the patch registered and the expected stability warning (acceptable for Harmony mods).

## Validation Scenarios

### Scenario 1 — Build with EnableHarmony
1. Add `<EnableHarmony>true</EnableHarmony>`; build Release.
2. Confirm 0 errors; `Lib.Harmony` resolved via SMAPI (no separate version pin).

### Scenario 2 — Seam unit tests (no game runtime)
1. Run `HarvestQualityServiceTests` (xUnit + FsCheck).
2. Confirm: tier mapping (Poor/Moderate/Healthy/Unknown/OutOfScope), `+0.08`/`+0.10` boosts, inventory-full debris policy, NaN→vanilla, measured 0→Poor, forage exclusion, feedback conditionality/co-occurrence (B1/B2) — all pass without a game runtime.

### Scenario 3 — Patch registration (in-game)
1. Launch via SMAPI; inspect SMAPI log: patch registered on `Crop.harvest`; stability warning present (expected); no Harmony errors.
2. Confirm no annotations or `PatchAll` used (code API only); all patch methods static; try/catch with fallback.

### Scenario 4 — Harvest quality at runtime (Gate 3)
1. Run in-game SMAPI session (~30 min, rebuild stale dll).
2. Harvest on Poor tile → item Quality = 0; transient "Soil too poor for quality crops" + `cancel` SFX.
3. Harvest on Living tile → possible silver/gold/iridium + occasional extra; "Living Soil"/"Extra harvest!" feedback per conditionality/co-occurrence.
4. Harvest on Medium tile → byte-identical to vanilla (no feedback).
5. Capture 3 signals: observation notes + save diff (`modData/soil_health_data_{saveId}.json`, float tolerance 0.0001f) + SMAPI log; include second player for multiplayer (D1).

### Scenario 5 — Patch failure fallback
1. Induce a patch exception (e.g., null soil); confirm patch logs error and falls back to original game logic (no crash, no data corruption).

### Scenario 6 — Android/cross-platform (documentation)
1. Document in README/Nexus: "Android: not tested / not supported" (do not tick Android Compatible tag). Optional runtime guard logs a warning on Android.

## Expected Outcomes

- `dotnet build -c Release` passes with 0 errors (with `EnableHarmony=true`).
- `HarvestQualityServiceTests` pass with 0 failures (no game runtime).
- SMAPI log shows patch registered + stability warning; no Harmony errors during harvest.
- Gate 3 attested by maintainer with multi-signal evidence (SC-004).
- `dotnet format --verify-no-changes` clean.

## Test Commands (filter examples)

```bash
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~HarvestQualityService"
dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~HarvestQuality"
```
