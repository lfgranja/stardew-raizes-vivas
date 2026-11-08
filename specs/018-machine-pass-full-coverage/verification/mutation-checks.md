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
