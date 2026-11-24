# Bug Fix: Gate 3 runner now injects, captures, and judges — the final headless run passes with three signals

- **Slug**: gate3-signal-capture
- **Fixed**: 2026-11-04
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`verify-scenario.sh` now runs the whole Gate 3 pipeline instead of launching the game and stopping. `save_prepare` isolates the milestone save, `injection_run` drives the world through the mod-side file queue (stdin stays closed), `signals_capture` copies the soil-health delta, crop-phase dump, and SMAPI excerpt into the bundle, `verdict_evaluate_bundle` derives the five verdict inputs and evaluates the scenario thresholds, and `quit_run` waits for the process to exit cleanly. The verification mod is no longer an empty `Entry`: it loads the isolated save, consumes the queue, writes atomic dumps, advances days, and quits.

The final real run passed end-to-end:

- Exit code `0`; `verdict.json` = `pass`; `signalsComplete`, `signalsAgree`, and `thresholdsOk` all `true`; `expectedVerdict` `pass`; `farmhandBreach` `false`.
- Bundle: `evidence-bundle-run-20261104T082444Z-4acd/`.
- `claims.json`: `honestyLevel: R`, `claims: []` (the `machine-pass` claim lives in `test.md`, not in `claims.json`).
- `specs/004-playable-compost-loop/test.md` now records `Status: machine-pass`, run ID `run-20261104T082444Z-4acd`, and commit `d908b19` (the commit the run used).
- Soil signal: target `Farm/60,15` stayed at health `10` (`before 10`, `after 10`, `delta 0`), tier `poor`.
- Crop signal: `phaseDays` `4 → 5`, `dayOfCurrentPhase` `0 → 1`, day key `spring_1 → spring_6`; `round(4 × 1.15) = 5`, so the Poor-soil multiplier predicate passes.
- SMAPI signal: `artifacts/smapi-log.txt` (1,894 bytes) contains five day transitions with the `Soil growth modifier elapsed` lines.
- Cross-check: `{"agree": true, "divergences": []}`.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/lib/save.sh` | modified | `save_prepare` resolves the scenario's `milestoneSave` when the flag is absent, creates the instance, isolates the milestone, exports the isolated `HOME`/`XDG_*`, and returns `19` with an actionable message when the milestone is missing. |
| `scripts/lib/injection.sh` | modified | `injection_run` writes the queue and drives it through the mod consumer. The queue splits `initCommands` from `steps`, so the `before` dumps observe the prepared world. Session/ack/error files are written atomically; no stdin is used. |
| `scripts/lib/signals.sh` | modified | `signals_capture` composes the soil-health before/after delta, the crop-phase dump, the cross-check, and the SMAPI excerpt. Missing or malformed inputs become distinct anomalies instead of a silent pass. |
| `scripts/lib/quit.sh` | modified | `quit_run` calls `quit_request` and `quit_wait_clean`; the old simulated `sleep 1` stability check is replaced by the mtime/size stability loop. |
| `scripts/lib/verdict.sh` | modified | Added `verdict_evaluate_bundle <bundle> <scenario…>`: derives signal completeness, cross-check agreement, threshold results, `expectedVerdict`, and the host-guard breach, then calls the unchanged five-argument `verdict_evaluate`. Also derives the flat `actual`/`pre` records from the dumps. |
| `scripts/lib/promotion.sh` | modified | The promotion now derives its run ID from the captured bundle basename (`evidence-bundle-*`) instead of generating a fresh timestamp, so `machine-pass` points at a bundle that exists. |
| `scripts/verify-scenario.sh` | modified | Replaced the `declare -F` silent skips with direct calls whose failures map to the documented exit codes; resolves `MILESTONE_SAVE` from the scenario; exports `RUNNER_INJECTION_DIR` before launch; calls the new capture and verdict stages. |
| `LivingRoots.Verification/ModEntry.cs` | modified | Queue consumer: polls `session.json`, auto-loads the isolated save through `SaveGame.Load` + `Game1.exitActiveMenu`, executes queue generations, advances days, writes crop/soil dumps, acknowledges, and quits. Logs the Farm map size on save load. |
| `LivingRoots.Verification/Queue/QueueCommandParser.cs` | added | Strict parser for `lr_set_soil_health`, `lr_place_bin`, `lr_insert_waste`, `lr_plant_crop`, `lr_advance_days`, `lr_dump_crop_phase`, `lr_dump_soil_health`, and `lr_quit`; rejects unknown commands, bad arity, oversized labels, and path traversal. |
| `LivingRoots.Verification/Evidence/CropPhaseDump.cs` | modified | Nullable `soilHealth` plus `soilHealthState` and the five-value `healthTier`, `basePhaseDays`, and `isMainPlayer`; missing/NaN are not fabricated as zero. |
| `LivingRoots.Verification/Evidence/CropPhaseDumpWriter.cs` | modified | Async atomic writes (temp file + rename + `.done` marker) so the runner never reads a partial dump. |
| `LivingRoots.Verification/Evidence/HealthTierClassifier.cs` | added | Maps stored health and capture state to `unknown`/`nonParticipant`/`poor`/`moderate`/`healthy` using the scenario's FR-010 edges (34/67). |
| `LivingRoots.Verification.Runner/Program.cs` | modified | Accepts an optional `pre` JSON record so the multiplier predicate can compare `phaseDays` against `pre.phaseDays`. |
| `LivingRoots/Domain/ISoilHealthService.cs` | modified | Added the narrow snapshot/get/set surface used by the verification API. |
| `LivingRoots/Domain/SoilHealthSnapshotEntry.cs` | added | Snapshot entry carrying the stored value and its state (`measured`/`missing`/`nan`). |
| `LivingRoots/Services/SoilHealthService.cs` | modified | Implements the snapshot/get/set surface over the existing cache and persistence path. |
| `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs` | modified | Added the verification-only API surface: soil-health get/set/TryGet/snapshot, bin placement, waste insertion, and `IsVerificationReady`. |
| `LivingRoots/Services/LivingRootsModApi.cs` | modified | Implements that surface, validates `isTileOnMap`/`isWaterTile` before mutating, and stays fail-closed until `BindVerificationServices` runs. |
| `LivingRoots/ModEntry.cs` | modified | Binds the verification services from the sole composition root. |
| `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json` | modified | Target moved to the on-map `60,15`; the macro placement became `lr_place_bin 60 15`; added `lr_plant_crop 60 15 LivingRoots.Verification.LongPhaseCrop`; waste `itemId` changed from `771` to `24`. |
| `LivingRoots.Tests/Verification/CropPhaseDumpTests.cs` | modified | Pins the dump fields, nullable soil health, missing/NaN handling, and sentinel phase. |
| `LivingRoots.Tests/Verification/CropPhaseDumpWriterTests.cs` | modified | Pins atomic write behavior and the `.done` marker. |
| `LivingRoots.Tests/Verification/HealthTierClassifierTests.cs` | added | Pins the five-value contract and the 34/67 boundaries, including `null` + `measured` → `unknown`. |
| `LivingRoots.Tests/Verification/QueueCommandParserTests.cs` | added | Pins the accepted command set, optional location, bad arity, unknown commands, oversized labels, and path traversal rejection. |
| `scripts/tests/test-save-preservation.sh` | modified | `save_prepare` with a missing milestone returns `19`; a fixture milestone is isolated and the source tree is never written. |
| `scripts/tests/test-injection.sh` | modified | `injection_run` writes the session and queue, waits for the mod ack, and orders `init` before the `before` dump before `steps`; the queue builder is tested directly. |
| `scripts/tests/test-evaluation.sh` | modified | The evaluator receives the `pre` record and the multiplier predicate can resolve `pre.phaseDays`. |
| `scripts/tests/test-promotion.sh` | added assertion | `promotion_apply` uses the captured bundle's run ID and the bundle path is traceable from `test.md`. |
| `scripts/tests/lib/runner-harness.sh` | modified | Harness support for the new queue and save fixtures. |

## Diff Highlights

The queue builder separates preparation from the measured steps, so the `before` dump is taken after the world is seeded but before waste insertion and day advancement:

```python
for c in (d.get("initCommands", []) or []) if phase in ("all", "init") else []:
    ...
for s in (d.get("steps", []) or []) if phase in ("all", "steps") else []:
    ...
```

The verdict wrapper keeps the tested pure function unchanged:

```bash
verdict_evaluate_bundle() {
    # derive signals_complete, signals_agree, thresholds_ok,
    # expectedVerdict, and farmhand_breach from the bundle
    verdict_evaluate "$signals_complete" "$signals_agree" "$thresholds_ok" "$expected" "$farmhand_breach"
}
```

The promotion now trusts the bundle name over a fresh clock reading:

```bash
bundle_name="$(basename -- "$bundle")"
if [[ "$bundle_name" == evidence-bundle-* ]]; then
    run_id="${bundle_name#evidence-bundle-}"
fi
```

## Tests Added or Updated

- `scripts/tests/test-save-preservation.sh` — `save_prepare` missing milestone => `19`; fixture milestone isolates and never mutates the source.
- `scripts/tests/test-injection.sh` — `injection_run` writes the session and queue, waits for the mod ack, and orders `init` before the `before` dump before `steps`; the queue builder is also tested directly.
- `scripts/tests/test-signals.sh` — `signals_capture` locates the mod's dumps through `session.json` and writes `soil-health-before-after.json`, `crop-phase-dump.json`, `smapi-log.txt`, and the preserved before/after captures; missing inputs produce distinct anomalies.
- `scripts/tests/test-verdict.sh` — `verdict_evaluate_bundle` derives the five inputs from a complete bundle, passes the `pre` record, evaluates the multiplier + between predicates, and writes `verdict.json`/`actual.json`/`pre.json`; the pure five-argument `verdict_evaluate` remains tested unchanged.
- `LivingRoots.Tests/Verification/` — `QueueCommandParserTests`, `HealthTierClassifierTests`, `CropPhaseDumpTests`, and `CropPhaseDumpWriterTests` cover the command contract, tier boundaries, dump schema, missing/NaN handling, atomic writes, and path traversal.
- `scripts/tests/test-promotion.sh` — the promotion run ID comes from the captured bundle, not a fresh timestamp.
- `scripts/tests/test-display-launch.sh` keeps the ready-launch-with-no-signals => `19` regression guard.

## Local Verification

- Targeted .NET:
  `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --filter "FullyQualifiedName~QueueCommandParserTests|FullyQualifiedName~HealthTierClassifierTests|FullyQualifiedName~CropPhaseDump|FullyQualifiedName~ModApiContractTests"`
  → **43 passed, 0 failed**.
- Full .NET:
  `dotnet test Stardew-LivingRoots.sln -c Release --no-build`
  → **1621 passed, 4 skipped, 1 failed**. The failure is the pre-existing `ProductionSourceGuardTests.ProductionSources_ExceptionDetailOnlyAtTrace` at `LivingRoots/Controllers/ModController.cs:399` (introduced by `4fae5fc`, not touched by this fix); the assertion text also names stale `SoilHealthService.cs:52` and `CompostingBinService.cs:362`. See Follow-ups.
- Format:
  `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **exit 0**.
- Shell suites (each run in isolation):
  `test-injection.sh` 24/24, `test-signals.sh` 31/31, `test-verdict.sh` 36/36, `test-evaluation.sh` 10/10, `test-save-preservation.sh` 19/19, `test-verify-cli.sh` 19/19, `test-promotion.sh` 8/8, `test-cleanup.sh` 11/11.
- Gate 3 (real install, real game):
  ```
  RUNNER_INJECTION_TIMEOUT=420 bash scripts/verify-scenario.sh \
    --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
    --game-path "/home/luis/.steam/steam/steamapps/common/Stardew Valley" \
    --skip-build --no-acquire --timeout 240
  ```
  → **exit 0**, verdict **pass**, honesty **R**, bundle `evidence-bundle-run-20261104T082444Z-4acd/`.
- `test.md` promotion:
  `Status: machine-pass`, `Run ID: run-20261104T082444Z-4acd`, `Commit: d908b19`, three signal paths resolve to the bundle.

## Deviations from Assessment

1. **Production API scope expansion (not in the assessment's file list).** The assessment offered `LivingRoots/Controllers/ModController.cs` as the place for `lr_set_soil_health` if it belonged in production. Instead, the verification mod now calls a narrow surface on `ILivingRootsModApi` (`GetSoilHealth`, `SetSoilHealth`, `TryGetSoilHealth`, `SnapshotSoilHealth`, `PlaceBin`, `InsertWaste`, `IsVerificationReady`), bound to `ISoilHealthService`/`ICompostingBinService` from `ModEntry.cs`. This keeps the production rules in one place and avoids a second soil-health implementation in the dev mod. Files added or changed for this: `ISoilHealthService.cs`, `SoilHealthSnapshotEntry.cs`, `SoilHealthService.cs`, `ILivingRootsModApi.cs`, `LivingRootsModApi.cs`, and `LivingRoots/ModEntry.cs`.
2. **Queue consumer instead of console-command registration.** The assessment's remediation item 5 said "register `lr_set_soil_health`, …". The launcher closes stdin and `launch_assert_no_stdin_events` rejects stdin events, so the commands are consumed from `RUNNER_INJECTION_DIR/queue-<generation>.txt` instead. `QueueCommandParserTests` replaces the "command registration" test named in the assessment; the commands are still executable and host-authoritative in the queue.
3. **Farmhand refusal is enforced at the verdict layer, not in the mod.** The dump records `isMainPlayer`, and `verdict_evaluate_bundle` marks a farmhand run that claims host-side mutation as `farmhandBreach` and fails it. The runner is host-only, so this is defense in depth rather than a runtime `!Context.IsMainPlayer` refusal. A mod-level refusal remains a follow-up.
4. **Scenario fixture changes beyond the assessment's "only if the milestone or command names change".** The original target `-10,5` is off the Farm map; `GameLocation.DayUpdate` deletes terrain features where `isTileOnMap` is false, so the crop and bin were silently removed on the first night. The target moved to the on-map `60,15`, the macro placement became `lr_place_bin 60 15`, a verification-only dry crop (`LivingRoots.Verification.LongPhaseCrop`) is planted, and the waste item changed from `771` to `24`. `PlaceBin` and `PlantCrop` now reject off-map/water tiles, and the verification mod logs the Farm map size on save load.
5. **Health-tier edges 34/67 instead of `ModConstants` 31/71.** `ModConstants.PoorModerateBandEdge`/`ModerateHealthyBandEdge` are the visualization/interpolation edges (31/71). The scenario predicate and FR-010 use `(0,34)`/`[34,67)`/`[67,100]`, so `HealthTierClassifier` uses 34/67. The values are named constants inside the classifier; a follow-up should add the spec edges to `ModConstants` so the "single source of magic numbers" rule holds for both contracts.
6. **Evaluator `pre` argument.** The assessment only asked for a verdict wrapper. The multiplier predicate compares `phaseDays` to `pre.phaseDays`, but the evaluator previously received only the actual record, so `LivingRoots.Verification.Runner/Program.cs` now accepts an optional `pre` JSON and `verdict_evaluate_bundle` writes `evaluation/pre.json` from the before dump.
7. **Promotion run-ID fix.** Discovered during verification: `promotion_apply` generated a fresh `runner_run_id`, so `test.md` pointed at a bundle that did not exist. It now derives the ID from the captured bundle basename. This was not in the assessment but is required for the `machine-pass` claim to be traceable.
8. **Dump-directory contract.** `session.json` carries `saveId` and `dumpDir`; the mod writes atomic dumps into `dumpDir` and acknowledges each generation. The assessment only said "calling `CropPhaseDumpWriter.Write`"; the async atomic writer and the session handshake are implementation details needed to avoid partial reads.
9. **`init` before `before` dump ordering.** The queue builder emits `initCommands`, then the `before` dump, then the `steps`, then the `after` dump. Without this split, the `before` record would observe the pre-seeded world and the multiplier predicate would be meaningless.
10. **Milestone provenance remains host-local.** `farm-year1-day5` was staged from `Agroeco_421179919` on this host and is not committed. `save_prepare` fails loudly (`19`) when it is absent. The assessment's open question (committed fixture vs. builddeps asset vs. scripted first run) is still open; the final bundle therefore proves this host, not a clean CI checkout.
11. **Stale fixture entry at `-10,5`.** The host-local milestone fixture still contains a bin/health entry at `-10,5` from the pre-isolation scenario revision. It appears in `soil-health-before-after.json` with `before 10`, `after 10`, `delta 0`, but the scenario's measured target is `60,15`; the stale entry does not participate in either threshold.

## Follow-ups

- **Pre-existing full-suite failure**: `ProductionSourceGuardTests.ProductionSources_ExceptionDetailOnlyAtTrace` flags `ModController.cs:399` because it logs `GetType().FullName`/`HResult` at `Warn`. The other rollback handlers already log the same detail at `Trace`; changing that line to `Trace` should green the suite. It predates this fix (`4fae5fc`) and is unrelated to Gate 3.
- **Milestone fixture**: commit a pinned `farm-year1-day5` fixture or fetch it from the builddeps release with a digest. Until then, CI cannot reproduce the final bundle without a host-local save.
- **Farmhand runtime refusal**: add a `!Context.IsMainPlayer` guard in `LivingRoots.Verification/ModEntry.cs` and a test for it, so a farmhand run stops before mutating anything instead of relying on the post-hoc verdict.
- **Health-tier constants**: add the FR-010 edges (34/67) to `ModConstants` and have `HealthTierClassifier` reference them, keeping the visualization edges (31/71) separate.
- **Pre-existing shell failures** outside this fix: `test-cli.sh`, `test-logparse.sh`, and `test-preflight.sh` still fail for unrelated reasons; `test-display-launch.sh` can leak an Xvfb child that hangs `run-all.sh` in a batch.
- **Promotion scope**: only `specs/004-playable-compost-loop/test.md` was promoted to `machine-pass`. `ROADMAP.md` and `COMPLETION_SUMMARY.md` still carry `partial` GATE3 blocks; promote them separately if the project wants the same run reflected there.
