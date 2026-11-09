# T034 — Mutation checks (negative controls)

**Date:** 2026-11-08
**Scope:** Two representative scenarios closed in this pass: 004 `compost-growth-hardening` (3-signal) and 013 `mulch-7day-expiry` (v4, 2-signal).
**Method:** break one production constant used by the scenario's asserted behavior, rebuild and deploy to the real game `Mods` folder, run the scenario headlessly, confirm the scenario fails at the expected threshold predicate, then restore the constant, rebuild, and re-run to confirm the baseline passes again. No self-attestation: every claim below names the bundle and the observed JSON.

## Why these two mutants

| Scenario | Constant | Baseline | Mutant | Asserted behavior |
|---|---|---|---|---|
| 004 `compost-growth-hardening` | `ModConstants.SoilGrowthPoorMultiplier` | `1.15f` | `1.00f` | Poor soil slows growth: `phaseDays` 4 → 5 (`round(4 × 1.15)`). Mutant makes the code skip the multiplier (`|1.00 − 1.00| < 0.0001` short-circuit), so `phaseDays` stays 4. |
| 013 `mulch-7day-expiry` | `ModConstants.MulchDurationDays` | `7` | `8` | Mulch expires on the 7th day-start tick, then spring decay applies once: soil 50 → 49. Mutant keeps the tile protected on day 7, so soil stays 50. |

The 004 mutant also changes `SoilGrowthPoorMultiplier`, which is used by other poor-soil scenarios; the run window was limited to one scenario and the source was restored immediately after the run. The 013 mutant similarly affects all mulch scenarios; same containment.

## 004 — mutant killed

- **Mutation:** `LivingRoots/Constants.cs:14` `SoilGrowthPoorMultiplier = 1.15f` → `1.00f`.
- **Build/deploy:** `dotnet build LivingRoots.Verification/LivingRoots.Verification.csproj -c Release -p:GamePath="$GAME" -p:GameModsPath="$GAME/Mods"` → exit 0.
- **Run:** `./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --no-acquire --game-path "$GAME" --skip-build --timeout 420`
- **Bundle:** `evidence-bundle-run-20261108T142806Z-67d7`
- **Runner exit:** `19` (`EXIT_ERRORED`; scenario expected `pass`, actual `fail`).
- **Observed:**
  - `evaluation/pre.json`: `phaseDays` 4, `soilHealth` 10, tier `poor`.
  - `evaluation/actual.json`: `phaseDays` 4, `soilHealth` 10, tier `poor`.
  - `evaluation/thresholds.jsonl`: `predicate-0` (`multiplier`, `field=phaseDays`, `base=pre.phaseDays`, `factor=1.15`) → **`passed: false`**; `predicate-1` (poor soil band) → `passed: true`.
  - `verdict.json`: `actualVerdict: fail`, `thresholdsOk: false`, `signalsComplete: true`, `signalsAgree: true`.
  - `manifest.json`: `honestyLevel: W`, `verdict: errored` — expected for a negative control, **not a claim bundle**.
- **Expected-vs-observed:** expected `round(4 × 1.15) = 5`; observed 4. The assertion detects the broken multiplier.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`; `grep` confirms `1.15f`; `git diff --stat -- LivingRoots/Constants.cs` empty; rebuild → exit 0.

## 013 — mutant killed

- **Mutation:** `LivingRoots/Constants.cs:176` `MulchDurationDays = 7` → `8`.
- **Build/deploy:** same build command → exit 0.
- **Run:** `./scripts/verify-scenario.sh --scenario specs/013-mulch-soil-cover/scenarios/mulch-7day-expiry.scenario.json --no-acquire --game-path "$GAME" --skip-build --timeout 420`
- **Bundle:** `evidence-bundle-run-20261108T143113Z-42e6`
- **Runner exit:** `19` (`EXIT_ERRORED`).
- **Observed:**
  - `evaluation/pre.json`: `soilHealth` 50, tier `moderate`.
  - `evaluation/actual.json`: `soilHealth` 50, tier `moderate`.
  - `artifacts/soil-health-before-after.json`: tile `61,15` before 50, after 50, delta 0.
  - `evaluation/thresholds.jsonl`: `predicate-0` (`multiplier`, `field=soilHealth`, `base=pre.soilHealth`, `factor=0.98`) → **`passed: false`**; `predicate-1` (`isMainPlayer == true`) → `passed: true`.
  - `verdict.json`: `actualVerdict: fail`, `thresholdsOk: false`, `signalsComplete: true`, `signalsAgree: true`.
  - `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Expected-vs-observed:** expected `round(50 × 0.98) = 49`; observed 50. The assertion detects the extended mulch lifetime.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`; `grep` confirms `7` and `1.15f`; `git diff --stat -- LivingRoots/Constants.cs` empty; rebuild → exit 0.

## Post-restore revalidation

After restoring and rebuilding the baseline, both scenarios pass again with the deployed baseline DLLs:

| Scenario | Bundle | Verdict | Key value | `--verify-bundle` |
|---|---|---|---|---|
| 004 | `evidence-bundle-run-20261108T143604Z-0f8f` | pass | `phaseDays` 5, `soilHealth` 10 | exit 0 |
| 013 | `evidence-bundle-run-20261108T143357Z-760a` | pass | `soilHealth` 49 | exit 0 |

## Integrity

`./scripts/verify-scenario.sh --verify-bundle` exit 0 for all eight bundles in this pass (four determinism, two mutants, two post-restore):

```
0 evidence-bundle-run-20261108T141912Z-6948
0 evidence-bundle-run-20261108T142058Z-4b8d
0 evidence-bundle-run-20261108T142252Z-6371
0 evidence-bundle-run-20261108T142501Z-266f
0 evidence-bundle-run-20261108T142806Z-67d7
0 evidence-bundle-run-20261108T143113Z-42e6
0 evidence-bundle-run-20261108T143357Z-760a
0 evidence-bundle-run-20261108T143604Z-0f8f
```

## Limits

- This is a **targeted** mutation check for two asserted behaviors (2 of 19 scenario files), not a mutation-testing campaign over the whole codebase.
- The `errored` / honesty `W` bundles are deliberately retained as negative-control evidence; they must never be promoted or cited as machine-pass.
- A passing post-restore run proves the restored source and deployed binaries agree on these scenarios; it does not prove every other scenario is unaffected.
- No human verification, D1/farmhand, SC-004, render, or balance claim is implied.

## Pass 13 addition (2026-11-08) — 005 and 017 (coverage now 4 of 19)

### 005 — mutant killed (`SoilGrowthHealthyMultiplier 0.85f → 1.00f`)

- **Mutation:** `LivingRoots/Constants.cs:15` `SoilGrowthHealthyMultiplier = 0.85f` → `1.00f`.
- **Scenario:** `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json` (healthy tier, 5 day-start ticks, seed 987654321).
- **Build/deploy:** same command, exit 0.
- **Run:** `TMPDIR=/home/luis/.runner-tmp ./scripts/verify-scenario.sh --scenario … --skip-build --timeout 420`
- **Bundle:** `evidence-bundle-run-20261108T173204Z-08a0`
- **Runner exit:** `19` (`ERRORED`; expected pass, actual fail).
- **Observed:** `evaluation/actual.json`: `soilHealth` 80, `healthTier` healthy, `phaseDays` 4 (baseline healthy multiplier produces 3). `evaluation/thresholds.jsonl`: predicate-0 (`multiplier`, `field=phaseDays`, `base=pre.phaseDays`, `factor=0.85`) → **`passed: false`**; predicate-1 (healthy soil band `[67,100)`) → `passed: true`. `verdict.json`: `actualVerdict: fail`, `thresholdsOk: false`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control, not a claim.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`, `grep` confirms `0.85f`; rebuild → exit 0.

### 017 — mutant killed (`AgrochemicalSoilPenalty 10f → 20f`)

- **Mutation:** `LivingRoots/Constants.cs:223` `AgrochemicalSoilPenalty = 10f` → `20f`.
- **Scenario:** `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json` (measured 50, one Joja application, factor 0.8).
- **Build/deploy:** exit 0.
- **Bundle:** `evidence-bundle-run-20261108T173425Z-7088`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `evaluation/actual.json`: `soilHealth` 30 (expected 40; delta −20), `healthTier` poor, `phaseDays` 5 (grown slower than the 1.0× predicate). `evaluation/thresholds.jsonl`: predicate-0 (`multiplier`, `field=soilHealth`, `base=pre.soilHealth`, `factor=0.8`) → **`passed: false`**; predicate-1 (`multiplier`, `field=phaseDays`, `factor=1.0`) → **`passed: false`**; predicate-2 (`isMainPlayer eq true`) → `passed: true`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`, `grep` confirms `10f`; rebuild → exit 0.

### Post-restore revalidation

| Scenario | Bundle | Verdict | Key values | `--verify-bundle` |
|---|---|---|---|---|
| 005 | `evidence-bundle-run-20261108T173619Z-070e` | pass | `soilHealth` 80 healthy, `phaseDays` 3 | exit 0 |
| 017 | `evidence-bundle-run-20261108T173759Z-7c21` | pass | `soilHealth` 40 moderate, `phaseDays` 4 | exit 0 |

### Integrity (pass 13)

All eight bundles produced in pass 13 pass `./scripts/verify-scenario.sh --verify-bundle` exit 0:

```
0 evidence-bundle-run-20261108T172115Z-060e
0 evidence-bundle-run-20261108T172354Z-5b8c
0 evidence-bundle-run-20261108T172717Z-31c7
0 evidence-bundle-run-20261108T172927Z-5f08
0 evidence-bundle-run-20261108T173204Z-08a0
0 evidence-bundle-run-20261108T173425Z-7088
0 evidence-bundle-run-20261108T173619Z-070e
0 evidence-bundle-run-20261108T173759Z-7c21
```

The `errored` bundles `173204Z-08a0` and `173425Z-7088` are negative controls; they are never promoted or cited as machine-pass.

Coverage: **4 of 19** scenario files (004, 013, 005-healthy, 017-tradeoff). The remaining 15 host/farmhand scenario files are still residual.

## Pass 15 addition (2026-11-08) — 013 bare-tile and 014 (coverage now 6 of 19)

### 013 — mutant killed (`MulchDecayMultiplier 0f → 1f`)

- **Mutation:** `LivingRoots/Constants.cs:177` `MulchDecayMultiplier = 0f` → `1f`.
- **Scenario:** `specs/013-mulch-soil-cover/scenarios/mulch-bare-tile-decay-exemption.scenario.json` (v4, bare HoeDirt, seed 123456789).
- **Build/deploy:** exit 0.
- **Run:** `TMPDIR=/home/luis/.runner-tmp ./scripts/verify-scenario.sh --scenario … --skip-build --timeout 420`
- **Bundle:** `evidence-bundle-run-20261108T212701Z-0ce2`
- **Runner exit:** `19` (`ERRORED`; expected pass, actual fail).
- **Observed:** `evaluation/actual.json`: soil 50 → **44** (one unprotected spring decay per tick ×6), tier moderate. `evaluation/thresholds.jsonl`: predicate-0 (`multiplier factor 1.0`) → **`passed: false`**; predicate-1 (`isMainPlayer`) → `true`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`, `grep` confirms `0f`; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261108T213615Z-51b4`, soil 50, **pass**.

### 014 — mutant killed (decision seam: Poor branch `ForceQualityZero` `true → false`)

- **Mutation:** `LivingRoots/Domain/Services/HarvestQualityService.cs` Poor branch (`soilHealth < PoorModerateBandEdge`): `ForceQualityZero = true` → `false`.
- **Scenario:** `specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-quality.scenario.json` (12 predicates, seed 1).
- **Build/deploy:** exit 0.
- **Bundle:** `evidence-bundle-run-20261108T213324Z-3925`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** poor probe `poorHarvestQuality` = **1** (scenario requires 0; the Poor-tier forced quality penalty was removed). `evaluation/thresholds.jsonl`: predicate-1 → **`passed: false`**; **all other 11 predicates `passed: true`** — a single-assertion kill. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Domain/Services/HarvestQualityService.cs`, `git diff` clean; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261108T213820Z-4874`, **pass**.

### 014 — discarded mutant (`SoilQualityHealthyBoost 0.08f → 0f`), recorded for honesty

- Bundle `evidence-bundle-run-20261108T212933Z-7976`: `verification: harvest matrix could not fill every probe from 4188 candidate tiles around 60,15.` The planner's band is `[vanillaP, vanillaP + SoilQualityHealthyBoost)`; with the mutant it became empty, so no probes were selected and **every predicate returned `unmeasured-actual`**.
- This run detects the broken mutation (scenario fails), but **not at the intended assertion**: SC-MP-6 requires the failing check to be the asserted contract. The mutation was therefore discarded as evidence and replaced by the decision-seam mutant above. `--verify-bundle` exit 0 (bundle is structurally sound); it remains a negative control but is **never cited** as machine-pass/kill evidence.
- Lesson: mutation selection must respect harness-witnessed preconditions; constants shared with probe selection are not valid negative controls for this scenario.

### Integrity (pass 15)

`--verify-bundle` exit 0 for all nine bundles in this pass:

```
0 evidence-bundle-run-20261108T211743Z-62f5
0 evidence-bundle-run-20261108T211930Z-69c9
0 evidence-bundle-run-20261108T212204Z-79e7
0 evidence-bundle-run-20261108T212407Z-6576
0 evidence-bundle-run-20261108T212701Z-0ce2
0 evidence-bundle-run-20261108T212933Z-7976
0 evidence-bundle-run-20261108T213324Z-3925
0 evidence-bundle-run-20261108T213615Z-51b4
0 evidence-bundle-run-20261108T213820Z-4874
```

Coverage: **6 of 19** scenario files (004, 013-expiry, 005, 017, 013-bare-tile, 014). The remaining 13 host/farmhand scenario files are still residual.

## Pass 16 addition (2026-11-08) — 005-poor and 017-clamped (coverage now 8 of 19)

### 005-poor — mutant killed (`SoilGrowthPoorMultiplier 1.15f → 1.00f`)

- **Mutation:** `LivingRoots/Constants.cs:14` `SoilGrowthPoorMultiplier = 1.15f` → `1.00f`.
- **Scenario:** `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth-poor.scenario.json` (poor 10, seed 987654321).
- **Bundle:** `evidence-bundle-run-20261109T030033Z-4c21`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `evaluation/actual.json`: `phaseDays` 4 (expected 5), tier poor. `evaluation/thresholds.jsonl`: predicate-0 (×1.15) → **`passed: false`**; predicate-1 (poor band) → `passed: true`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261109T030410Z-4cd0`, `phaseDays` 5, **pass**.

### 017-clamped — mutant killed (`AgrochemicalSoilPenalty 10f → 0f`)

- **Mutation:** `LivingRoots/Constants.cs:223` `AgrochemicalSoilPenalty = 10f` → `0f`.
- **Scenario:** `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-clamped.scenario.json` (measured 5, factor `soil cmp eq 0`, seed 123456789).
- **Bundle:** `evidence-bundle-run-20261109T030225Z-2cb3`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `evaluation/actual.json`: `soilHealth` 5 (expected 0 — no clamp), tier poor, `phaseDays` 5. `evaluation/thresholds.jsonl`: predicate-0 (`cmp eq 0`) → **`passed: false`**; predicate-1 (×1.15) → `true`; predicate-2 (`isMainPlayer`) → `true`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261109T030553Z-391b`, `soilHealth` 0, **pass**.

### Integrity (pass 16)

`--verify-bundle` exit 0 for all eight bundles in this pass:

```
0 evidence-bundle-run-20261109T025423Z-3865
0 evidence-bundle-run-20261109T025553Z-7ba7
0 evidence-bundle-run-20261109T025730Z-1080
0 evidence-bundle-run-20261109T025841Z-2be9
0 evidence-bundle-run-20261109T030033Z-4c21
0 evidence-bundle-run-20261109T030225Z-2cb3
0 evidence-bundle-run-20261109T030410Z-4cd0
0 evidence-bundle-run-20261109T030553Z-391b
```

Coverage: **8 of 19** scenario files (004, 013-expiry, 005-healthy, 017-tradeoff, 013-bare-tile, 014, 005-poor, 017-clamped). The remaining 11 host/farmhand scenario files are still residual.

## Pass 17 addition (2026-11-08) — 015-harmony, 014-zero, 004-healthy (coverage now 11 of 19)

### 015-harmony — mutant killed (decision seam: Poor `ForceQualityZero` `true → false`)

- **Mutation:** `LivingRoots/Domain/Services/HarvestQualityService.cs` Poor branch: `ForceQualityZero = true` → `false`.
- **Scenario:** `specs/015-harmony-harvest-seam/scenarios/harmony-harvest-quality-host.scenario.json` (seed 1).
- **Bundle:** `evidence-bundle-run-20261109T032436Z-61fb`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `poorHarvestQuality` = **1** (scenario requires 0); predicates 0, 2–11 `passed: true`; predicate-1 `false`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Domain/Services/HarvestQualityService.cs`; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261109T032903Z-29cf`, **pass**.

### 014-zero — mutant killed (same decision seam)

- **Mutation:** identical to 015 above.
- **Scenario:** `specs/014-soil-health-crop-quality/scenarios/soil-health-harvest-measured-zero.scenario.json` (measured 0/Poor).
- **Bundle:** `evidence-bundle-run-20261109T032550Z-1e3b`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `zeroHarvestQuality` = **1** (scenario requires 0), `zeroHealthTier` poor; predicates 0–1, 3 `passed: true`; predicate-2 (`zeroHarvestQuality eq 0`) → **`false`**. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** same as 015; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261109T033015Z-0e1b`, **pass** (`zeroHarvestQuality` 0).

### 004-healthy — mutant killed (`SoilGrowthHealthyMultiplier 0.85f → 1.00f`)

- **Mutation:** `LivingRoots/Constants.cs:15` `SoilGrowthHealthyMultiplier = 0.85f` → `1.00f`.
- **Scenario:** `specs/004-playable-compost-loop/scenarios/compost-growth-healthy.scenario.json` (healthy 80, seed 123456789).
- **Bundle:** `evidence-bundle-run-20261109T032724Z-2b7d`
- **Runner exit:** `19` (`ERRORED`).
- **Observed:** `phaseDays` **4** (scenario expects healthy growth → 3); predicate-0 (×0.85) → **`passed: false`**; predicate-1 (healthy band) → `true`. `manifest.json`: `honestyLevel: W`, `verdict: errored` — negative control only.
- **Restore:** `git checkout -- LivingRoots/Constants.cs`; rebuild → exit 0.
- **Post-restore:** `evidence-bundle-run-20261109T033119Z-6fda`, `phaseDays` 3, **pass**.

### Integrity (pass 17)

`--verify-bundle` exit 0 for all twelve bundles in this pass:

```
0 evidence-bundle-run-20261109T031507Z-702d
0 evidence-bundle-run-20261109T031644Z-755d
0 evidence-bundle-run-20261109T031831Z-3838
0 evidence-bundle-run-20261109T032001Z-77c0
0 evidence-bundle-run-20261109T032111Z-4e06
0 evidence-bundle-run-20261109T032235Z-3b83
0 evidence-bundle-run-20261109T032436Z-61fb
0 evidence-bundle-run-20261109T032550Z-1e3b
0 evidence-bundle-run-20261109T032724Z-2b7d
0 evidence-bundle-run-20261109T032903Z-29cf
0 evidence-bundle-run-20261109T033015Z-0e1b
0 evidence-bundle-run-20261109T033119Z-6fda
```

Coverage: **11 of 19** scenario files (004/013-expiry/005/017/013-bare-tile/014/005-poor/017-clamped/015/014-zero/004-healthy). The remaining 8 are still residual.
