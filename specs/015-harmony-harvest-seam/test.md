# Feature Test Record: 015-harmony-harvest-seam

<!-- GATE3:BEGIN -->
- **Status**: partial
- **Verdict**: —
- **Flaky**: false
- **Run ID**: —
- **Commit**: —
- **Date**: —
- **Player role**: —
- **Signals**:
  - SoilHealthState delta: —
  - Crop-phase delta: —
  - SMAPI log: —
- **Bundle**: —
- **Honesty level**: —
- **Claim**: Not yet verified. Gate 3 evidence incomplete — headless runner requires a complete Stardew Valley 1.6.x + SMAPI 4.4.0+ install at STARDEW_GAME_PATH to execute the harmony-harvest-quality scenario. Implementation (spec 015) is complete: Harmony patch on Crop.harvest, HarvestQualityService seam, EnableHarmony=true, all unit tests green, build clean, format clean.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->

## Append-only evidence notes (2026-11-07, MDMV 80/20; no new authorization)

- **SC-001 (HarvestQualityService seam, no game runtime):** `GamePath=/home/luis/stardew-assemblies dotnet test --filter "FullyQualifiedName~HarvestQualityServiceTests" -c Release --no-restore` → **17 passed, 0 failed, 0 skipped** (559 ms, fresh). Real test evidence.
- **SC-002/SC-005 (build/format context):** solution builds 0 errors in this session (`dotnet build Stardew-LivingRoots.sln -c Release --no-restore`); format not re-run in this note (see prior records/pre-push).
- **SC-003 (Harmony registration, real smapiLog signal):** headless bundles show `Living Roots: Harvest Harmony patch registered` with no Harmony/harvest-patch errors in checked logs: `evidence-bundle-run-20261106T014750Z-14b6/artifacts/smapi-log.txt` (014 harvest-matrix machine-pass), `evidence-bundle-run-20261107T193756Z-6d97/artifacts/smapi-log.txt`. Real-signal evidence (log), not GUI/visual.
- **SC-004 (in-game Gate 3, 3 signals, D1 second player):** **not satisfied** by this headless evidence. 014 achieved runner `machine-pass` for harvest-quality/matrix host paths using the 015-enabled seam (`run-20261106T014750Z-14b6` primary, plus 014 test.md records) — strong runtime evidence of the seam, but 015's own SC-004 (explicit Gate 3 + D1 multiplayer verification) remains **partial/pending**. Do not promote 015 to `verified`/`machine-pass` on this basis.
- Honesty: `machine-pass` (014 paths) ≠ 015 SC-004 complete; D1/render/balance/human out of scope here.
