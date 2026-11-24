# Bug Assessment: Gate 3 runner cannot inject, capture, or judge — it launches and stops

- **Slug**: gate3-signal-capture
- **Created**: 2026-11-04
- **Source**: pasted text (no URL supplied)
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> save_prepare, injection_run, signals_capture, quit_run are not implemented anywhere.
> LivingRoots.Verification/ModEntry.cs is an empty stub; the crop-phase dump writer is unused.
> The mod registers only lr_version; the scenario's lr_set_soil_health -10 5 10 does not exist.
> The milestone save farm-year1-day5 does not exist on this host.
> verdict_evaluate is called with 2 args where it requires 5, so thresholds are never evaluated.

No URL was supplied, so the URL trust policy was not exercised.

## Symptom

`verify-scenario.sh` launches the real game successfully, reaches the title screen, and then has nothing to run: every post-launch stage is guarded by `declare -F <fn>` and the functions do not exist, so the stages are skipped silently. The run produces no soil-health delta, no crop-phase dump, and no SMAPI log excerpt, and the scenario's console/step commands are never executed. The expected behavior is three captured signals and a verdict derived from the scenario thresholds.

Confirmed on the real install: SMAPI 4.5.2 / Stardew Valley 1.6.15 build 24356 reaches `Loaded 4 mods:` and `Mods loaded and ready!`, the run exits `19` (`partial`), and the bundle contains three `missing` anomaly reports. Before the fail-closed fix in `0d90abc` the same run exited `0`.

## Reproduction

1. Confirm the real install: `/home/luis/.steam/steam/steamapps/common/Stardew Valley` (SMAPI 4.5.2, SV 1.6.15).
2. Run:
   ```
   bash scripts/verify-scenario.sh \
     --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
     --game-path "/home/luis/.steam/steam/steamapps/common/Stardew Valley" \
     --skip-build --no-acquire --timeout 120
   ```
3. Observe `RUNNER_EXIT=19`; the newest `evidence-bundle-*` contains only `artifacts/anomaly-report*.json`, `claims.json` (`honestyLevel: W`, `claims: []`), `manifest.json`, and `manifest.sha256`.
4. Observe that the SMAPI log stops at the title screen: no `lr_set_soil_health`, no waste insertion, no day advance, no crop dump.
5. Confirm the save is absent: `ls ~/.config/StardewValley/Saves` lists `Agroeco_421179919`, `Alch_421181235`, `MDMV_423846455`, `MDMV_424795694`, `Mocochinchi_435810789`, `Mocochinchi_435811461`, `Paozinho_420340450`, `Pãozinho_420341761`, `Simfarm_307145120 (backup)` — no `farm-year1-day5`.

## Suspected Code Paths

**Orchestration gaps** — each call is wrapped in `declare -F`, so a missing definition is a silent skip, not an error:

- `scripts/verify-scenario.sh:261` — `save_prepare "$MILESTONE_SAVE" "$RUNNER_RUN_ROOT"`; no definition exists in `scripts/lib/save.sh`.
- `scripts/verify-scenario.sh:292` — `injection_run "${SCENARIO_LIST[@]}"`; no definition exists in `scripts/lib/injection.sh`.
- `scripts/verify-scenario.sh:297` — `signals_capture "$BUNDLE_DIR" ...`; no definition exists in `scripts/lib/signals.sh`.
- `scripts/verify-scenario.sh:373` — `quit_run`; `scripts/lib/quit.sh` defines only `quit_request` and `quit_wait_clean`.

**Primitives that exist but are never orchestrated:**

- `scripts/lib/save.sh:11-120` — `save_create_instance_dir`, `save_setup_env`, `save_isolate`, `save_hash_tree`, `save_verify_source_unchanged`, `save_restore_working_copy`, `save_restore`, `save_assert_no_player_save_access`. `save_isolate` already takes `(instance, source, save_name)` and copies a milestone save into an isolated `XDG` tree.
- `scripts/lib/injection.sh:80` — `injection_build_queue` emits `debug runmacro …`, raw console commands, completion markers, and a trailing `/sleep`. `scripts/lib/injection.sh:108` — `injection_seed` refuses farmhand (`21`) and acquires the seed claim.
- `scripts/lib/signals.sh:230` — `signals_process <bundle> <runId> <owner> [before] [after] [dump_dir] [save] [smapi.log]` already composes the delta, the crop dump, the cross-check, and the log excerpt. It has no caller.
- `scripts/lib/quit.sh:8,15` — `quit_request`, `quit_wait_clean` (the latter currently simulates stability with `sleep 1`).
- `scripts/lib/evidence.sh:56` — `evidence_record_attempt` exists; nothing calls it, so `attempts/` stays empty.

**Verdict arity mismatch:**

- `scripts/lib/verdict.sh:55-57` — `verdict_evaluate` requires exactly 5 args: `signals_complete signals_agree thresholds_ok expected farmhand_breach`.
- `scripts/verify-scenario.sh:316-318` — calls `verdict_evaluate "$BUNDLE_DIR" "${SCENARIO_LIST[@]}"` (2 args). The function returns `EXIT_USAGE_ERROR` immediately and the call is wrapped in `|| true`. No threshold is ever evaluated, and no `expectedVerdict` comparison is made.

**Verification mod is a scaffold:**

- `LivingRoots.Verification/ModEntry.cs:7-9` — `Entry` is empty. The file comment defers the real implementation to T102.
- `LivingRoots.Verification/Evidence/CropPhaseDumpWriter.cs:9` — `Write(directory, dayKey, dump)` is the only writer; a repo-wide search finds no caller outside its own declaration.
- `LivingRoots.Verification/LivingRoots.Verification.csproj:23` — the project already references `LivingRoots.csproj`, so the API is available to a real implementation.

**Missing console command:**

- `LivingRoots/Controllers/ModController.cs:2440` — the only registration is `_helper.ConsoleCommands.Add("lr_version", …)`.
- `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json:30` — `"command": "lr_set_soil_health -10 5 10"`.
- The scenario's `steps` (`:36` `insertWaste`, `:44` `sleep`, `:49` `dumpCropPhase`, `:53` `observe`) have no executor.

**Milestone save:**

- `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json:21` — `"milestoneSave": "farm-year1-day5"`.
- `scripts/verify-scenario.sh:42,72,200,262` — `MILESTONE_SAVE` is set only by `--milestone-save` (default `""`). Nothing reads the scenario's `milestoneSave` field, so even with `save_prepare` implemented the value would be empty unless the flag is passed.

**Design conflict to resolve before writing injection:**

- `scripts/lib/launch.sh:40,52` — the launcher is started with `</dev/null`.
- `scripts/tests/test-injection.sh:76-81` — asserts that stdin is closed via `</dev/null`.
- `scripts/lib/launch.sh:125` — `launch_assert_no_stdin_events` returns `19` if the log mentions `stdin`.
- Conclusion: injection must not use stdin. The file-based queue produced by `injection_build_queue` needs a consumer (a mod-side command-file poller), which does not exist.

## Root Cause Hypothesis

Confidence: high. The runner was built primitives-first: `save.sh`, `injection.sh`, `signals.sh`, `quit.sh`, and `verdict.sh` each contain the low-level operations plus unit tests, but the single orchestration function per stage (`save_prepare`, `injection_run`, `signals_capture`, `quit_run`) was never written. Because the orchestrator probes with `declare -F` and swallows failures with `|| true`, the gap is invisible at runtime rather than loud. The same scaffold-first pattern left the verification mod as an empty `Entry`, so even a wired `signals_capture` would have no dump to copy. The verdict call was written against an imagined bundle-shaped API instead of the existing 5-argument pure function, so it fails arity validation and is discarded. Finally, the milestone save is referenced by every scenario but never produced, and the runner never reads the scenario's `milestoneSave` field.

## Proposed Remediation

**Preferred**: implement the orchestration layer as thin functions over the existing primitives, and make absence loud.

1. `save_prepare <milestone> <run_root>` in `scripts/lib/save.sh`: resolve the milestone under `${XDG_CONFIG_HOME:-$HOME/.config}/StardewValley/Saves/<milestone>`, create the instance dir, call `save_isolate`, and export the isolated environment. Missing milestone => `19` with an actionable message, never a silent skip. Resolve `MILESTONE_SAVE` from the scenario's `milestoneSave` when the flag is absent.
2. `injection_run <scenario…>`: use `injection_build_queue` to write the queue, then drive it through the mod-side consumer (see 5). No stdin.
3. `signals_capture <bundle> <scenario…>`: call `signals_process` with the before/after soil-health JSON, the dump directory, the save file, and `$RUNNER_SMAPI_LOG`.
4. `quit_run`: `quit_request` + `quit_wait_clean`, and replace the simulated stability `sleep 1` with the mtime/size stability loop the contract requires.
5. `LivingRoots.Verification`: implement `Entry` to register `lr_set_soil_health`, `lr_insert_waste`, `lr_advance_days`, and `lr_dump_crop_phase` (calling `CropPhaseDumpWriter.Write`), plus a poller for the injection queue file. Refuse to act when `!Context.IsMainPlayer`.
6. Verdict: add `verdict_evaluate_bundle <bundle> <scenario>` that derives the five inputs (`signals_all_present`, cross-check agreement, threshold evaluation, `expectedVerdict`, host-guard breach) and calls the existing `verdict_evaluate`. Keep the pure 5-arg function unchanged.
7. Make the `declare -F` guards fail closed once the functions exist: replace `if declare -F x; then x || true; fi` with a direct call whose failure maps to the documented exit code.

**Alternatives**:
- Drive the game with `xdotool`/X11 input. Rejected: `scripts/tests/test-injection.sh:69` forbids `xdotool`, and synthetic input is not deterministic.
- Reuse SMAPI's `debug runmacro` with a macro file only, skipping mod-side commands. Rejected: `lr_set_soil_health` and the crop dump need mod internals; `debug runmacro` cannot express them.
- Change `verdict_evaluate` to accept a bundle path. Rejected: it is a tested pure function (`scripts/tests/test-verdict.sh:28-38`); a wrapper is cheaper and keeps the unit tests meaningful.

**Files likely to change**:
- `scripts/lib/save.sh`, `scripts/lib/injection.sh`, `scripts/lib/signals.sh`, `scripts/lib/quit.sh`
- `scripts/verify-scenario.sh` (direct calls, milestone resolution, verdict wrapper)
- `scripts/lib/verdict.sh` (new wrapper only)
- `LivingRoots.Verification/ModEntry.cs`, `LivingRoots.Verification/Evidence/CropPhaseDumpWriter.cs`
- `LivingRoots/Controllers/ModController.cs` (only if `lr_set_soil_health` belongs in the production mod rather than the verification mod)
- `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json` (only if the milestone or command names change)

**Tests to add or update**:
- `scripts/tests/test-save-preservation.sh` — `save_prepare` with a missing milestone returns `19`; with a fixture milestone it isolates and never writes the source.
- `scripts/tests/test-injection.sh` — `injection_run` consumes the queue without stdin; a queue consumer that reports failure propagates.
- `scripts/tests/test-signals.sh` — `signals_capture` with all three inputs produces `soil-health-before-after.json`, `crop-phase-dump.json`, and `smapi-log.txt`; missing inputs produce distinct anomalies.
- `scripts/tests/test-verdict.sh` — `verdict_evaluate_bundle` maps complete/agreeing/passing to `pass` and each failure mode to its verdict.
- `LivingRoots.Tests/Verification/` — command registration, farmhand refusal, and dump-writer invocation.
- `scripts/tests/test-display-launch.sh` — keep the ready-launch-with-no-signals => `19` case as the regression guard.

## Risks & Considerations

- **False pass regression risk.** The `declare -F` guards are what made the gap silent. Replacing them with direct calls will surface every still-missing function at once; land the functions before removing the guards, or the suite will go red for the wrong reason.
- **Save mutation.** `save_isolate` copies the milestone; any code that writes back to the source save is a data-loss risk. `save_assert_no_player_save_access` must stay on the path.
- **Milestone provenance.** `farm-year1-day5` is referenced by scenarios 004 and 005 and by the evidence-bundle schema. It needs a committed fixture or a builddeps asset with a pinned digest; it should not be hand-created on one host.
- **Injection channel.** The no-stdin constraint is enforced by tests and by `launch_assert_no_stdin_events`. A file-based queue poller must be race-free (write-temp-then-rename, delete-after-ack) and must not deadlock when the game is at the title screen.
- **Multiplayer.** Console commands that mutate farm state must be host-authoritative; the verification mod must refuse when `!Context.IsMainPlayer` (constitution Principle VI).
- **Honesty.** None of this may write `verified`; only the human attestation (T148) can. The `machine-pass` promotion must stay gated on the three real signals.

## Open Questions

- [NEEDS CLARIFICATION: should the `farm-year1-day5` milestone be committed to the repo, fetched from the builddeps release, or generated by a scripted first-run?]
- [NEEDS CLARIFICATION: should `lr_set_soil_health` live in the production mod (useful to players/testers) or only in `LivingRoots.Verification`?]
- [NEEDS CLARIFICATION: which side owns the injection-queue contract — the runner writes it, but the mod must agree on path, format, and acknowledgement semantics.]
- [NEEDS CLARIFICATION: should `--milestone-save` override the scenario's `milestoneSave`, or should a mismatch be a scenario-contract error (exit 14)?]
