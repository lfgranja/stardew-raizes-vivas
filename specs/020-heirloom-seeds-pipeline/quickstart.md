# Quickstart: Heirloom Seeds / Sementes Crioulas Pipeline — Validation Guide

**Feature**: `020-heirloom-seeds-pipeline` | **Audience**: maintainer / verifier

Prerequisites

- Game assemblies present at `GamePath` (default used in this repo: `/home/luis/stardew-assemblies`; set `GamePath=…` when invoking build/test).
- `git status` clean or only 020 artifacts staged.
- .NET 6 SDK + `dotnet` on PATH; repo builds with `--locked-mode`.

Setup / build / tests

```bash
cd /home/luis/development/stardew-raizes-vivas
GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln -c Release -m:1 --locked-mode
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build --filter "FullyQualifiedName~Heirloom"
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build   # full suite — expect no regression vs 1937/0/4
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

End-to-end (host-only, headless — no game window interaction)

```bash
./scripts/verify-scenario.sh --scenario specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json \
  --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
```

Expected: `verdict: pass`, signals `soilHealthStateDelta` (measured +~2 after plant, bounded), `cropPhaseDelta` (advancing over sleep), `smapiLog` (host/main-player check true); bundle written under `evidence-bundle-run-*/` (gitignored); honesty **R** (host-only machine-pass); **no** `human-verified`/`d1` claims.

Manual spot-check (optional; never substitutes Gate 3)

1. Load a save; visit the Traveler in Spring (Friday/Sunday, Cindersap Forest) — crioulo parsnip seeds should appear in the special stock (Player-limited; 35g), subject to cart odds (verify seed presence via debug grant if cart does not roll it).
2. Plant on tilled soil; confirm soil-health tile increases by 2 (hover/hoe).
3. Wait 7 days; harvest; expect crioulo produce (45g) + 1 saved heirloom seed (or full-inventory refusal copy).
4. In a co-op save, plant/harvest as farmhand; confirm no local soil write — host snapshot sync wins (message pattern as HE-1/HE-2); refusal copy appears if host is unavailable/unsupported.

Failure modes & triage

- Cart entry never appears: check `Traveler` edit branch fired (log line at `AssetRequested`); fallback `Condition` deviation per D1/FR-003; record deviation, never silent.
- Seed-save missing at harvest: confirm harvest went through the 014 seam (not a direct `ItemRegistry.Create`); junimo path exempt by design (FR-009/Clarif-2026-11-09).
- Soil write overflow/NaN: expected NaN→100 path; verify `SetSoilHealth` clamp at 0–100; if measured delta outside `[HeirloomPlantBonus ± epsilon]`, inspect lock correlation (HE-1) before relaxing tolerance.
- 014 crop behavior mismatch: crioulo crop must ride 014 unchanged; any delta → `IHarvestQualityService` inputs differ → check isPaddy/fibre/Qi filters — do not patch around in the heirloom seam.

Done-when for the cycle

- Above commands pass; new targeted heirloom tests green; suite ≥ baseline; format clean; `machine-pass` recorded for the heirloom scenario (honesty R); validate.md records **no release/tag actions**; `machine-pass` ≠ `verified`.
