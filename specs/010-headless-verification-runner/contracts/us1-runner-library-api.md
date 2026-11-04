# US1 Runner Library API (internal contract)

**Feature**: 010-headless-verification-runner · **Phase**: 3 (US1)

This file freezes the seams used by the US1 bash tests and the US1 bash
libraries. Tests are written first against this contract; libraries MUST expose
these names. The names are internal (not a public API), but a rename breaks the
suite, so treat the table as a contract.

Sourced libraries live in `scripts/lib/`. Every library is safe to source more
than once (source guard), never executes work at source time, and never calls
`exit` — functions return an exit code from `contracts/exit-codes.md`. Only the
orchestrator `scripts/verify-scenario.sh` calls `exit`.

Shared conventions:

- Errors go to stderr as a single line:
  `runner: error: code=<N> class=<CLASS> message=<text> [field=<path>]`.
  Tests match `code=` and `field=`, never prose.
- Machine-readable results are JSON files or JSON on stdout. Diagnostics go to
  stderr.
- No library logs a secret; redaction happens before every sink
  (`scripts/lib/logging.sh`).
- The runner writes `machine-pass` at most; the string `verified` is never
  written by any US1 code path except as a refusal/diagnostic (promotion tests
  assert this).

## Environment seams (test and CI only)

| Variable | Meaning | Default |
|---|---|---|
| `RUNNER_REPO_ROOT` | Repository root | resolved from the script path |
| `RUNNER_GAME_PATH` | Effective game install (set by `--game-path`/`STARDEW_GAME_PATH`) | unset |
| `RUNNER_MODS_DIR` | Mods directory used for deployment checks | `${RUNNER_GAME_PATH}/Mods` |
| `RUNNER_LAUNCHER` | SMAPI launcher executable | `${RUNNER_GAME_PATH}/StardewModdingAPI` |
| `RUNNER_XVFB` | Xvfb binary | `Xvfb` |
| `RUNNER_SMAPI_LOG` | SMAPI log path | `${XDG_CONFIG_HOME:-$HOME/.config}/StardewValley/ErrorLogs/SMAPI-latest.txt` |
| `RUNNER_CACHE_DIR` | Game-file cache | `${XDG_CACHE_HOME:-$HOME/.cache}/livingroots/game` |
| `RUNNER_EVIDENCE_ROOT` | Directory that holds `evidence-bundle-<runId>/` | `${RUNNER_REPO_ROOT}` |
| `RUNNER_RUN_ROOT` | Disposable per-instance root | `mktemp -d` under `TMPDIR` |
| `RUNNER_CLAIM_ROOT` | Host-shared claim/lock root | `${XDG_RUNTIME_DIR:-/tmp}/livingroots-runner` |
| `RUNNER_RETRY_COOLDOWN_MS` | Override cooldown (tests) | `RunnerRetryCooldownMs` (5000) |
| `RUNNER_QUIET` | Suppress non-error output | `0` |
| `BUILDDEPS_TOKEN` / `GH_TOKEN` | Fine-grained PAT for the private builddeps repo | unset |
| `QUARANTINE_OWNER` | Quarantine owner fallback | unset |

`RUNNER_LAUNCHER`/`RUNNER_XVFB` are explicit seams so the launch-free suites can
substitute stubs; they carry no verdict authority.

## `scripts/verify-scenario.sh`

```
./scripts/verify-scenario.sh --scenario <file|dir> [options]
./scripts/verify-scenario.sh --verify-bundle <dir>
./scripts/verify-scenario.sh --check-docs [--scenario <file>]
```

Options: `--scenario <file|dir>`, `--scenario-glob <glob>`, `--game-path <dir>`,
`--skip-build`, `--override <key>=<value>` (repeatable), `--milestone-save <name>`,
`--quarantine-owner <name>`, `--no-acquire`, `--force-refresh`,
`--ignore-exit-code <list>`, `--verify-bundle <dir>`, `--check-docs`,
`--timeout <seconds>`, `--verbose`, `--help`.

- Unknown flag/positional, missing required value, malformed value, unknown or
  forbidden override key, `--ignore-exit-code 0|22|<unknown>`,
  `--no-acquire` + `--force-refresh`, missing `--scenario` (for a run) → `14`.
- `--game-path` with an empty value → `14`; a non-empty invalid path follows the
  acquisition branch and exits `9` when no token/cache can supply the game.
- `--verify-bundle` and `--check-docs` are launch-free; they never build,
  acquire, allocate a display, or launch the game.
- The orchestrator runs cleanup on every path and preserves the exit code;
  cleanup failure forces `19` and records `cleanup_failed: true`.

## `scripts/lib/common.sh` additions (constants only)

```
RunnerMaxAttempts=3
RunnerFlakyObservationWindowDays=14
RunnerFlakyQuarantineTriggerCount=2
RunnerQuarantineTimeBoxDays=14
RunnerQuarantineExtendedTimeBoxDays=28
RunnerCleanQuitStabilitySeconds=30
RunnerCleanQuitBackstopSeconds=120
RunnerPerLocationBudgetMs=5
RunnerGenerationBudget=1          # warmup count discarded
RunnerRunIdPattern='^run-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{4}$'
RunnerIntegrityClasses='checksum archive-member extraction token-invalid partial-download'
RunnerAllowedIgnoreExitCodes='4 6 7 9 10 13 14 18 19 20 21'
RunnerAllowedOverrideKeys='environment.gameVersion environment.smapiVersion environment.modVersion environment.saveId environment.config.*'
```

`runner_exit_name <code>` returns the class name (`PASS`, `FAIL`, `ERRORED`,
`NO_TESTS`, `POLICY`, …). `runner_now_iso` and `runner_run_id` are also provided
(`runner_run_id` emits `run-YYYYMMDDTHHMMSSZ-<4 hex>`).

## `scripts/lib/acquisition.sh` (T084)

```
acquisition_validate_lock <gamepath.lock.json>
    Sets ACQ_RELEASE_ID ACQ_ASSET_ID ACQ_ASSET_NAME ACQ_ARCHIVE_SHA ACQ_FILES_JSON.
    0 valid; 19 on placeholder/failClosed, mutable "latest", missing/empty
    per-file map, or non-string digest.

acquisition_validate_redirect <url>
    0 only for https:// and a host in {github.com, api.github.com,
    objects.githubusercontent.com, codeload.github.com}; 19 otherwise.
    Rejects http://, embedded credentials, and any other host.

acquisition_cache_verify <dir> <lockfile>
    0 when <dir>/.acquired.json carries the pinned release/asset/sha and every
    file in the lock's digest map exists with the recorded SHA-256.
    1 when the cache is absent/incomplete/mismatched (a miss, not yet an error).

acquisition_token_available
    0 when BUILDDEPS_TOKEN, GH_TOKEN, or `gh auth token` yields a non-empty
    token; 1 otherwise.

acquisition_token_value
    Prints the token on stdout; never logs it, never accepts it via argv.

acquisition_fetch_asset <dest> [url]
    Downloads with `curl --config <temp 0600 file>` (token in the config, never
    argv/URL). Writes `<dest>.part`, then atomically renames. Retries 429/5xx
    `RunnerMaxTransientRetries` times with `RunnerRetryCooldownMs`; 401/403/404
    are terminal. Follows no redirect outside `acquisition_validate_redirect`.
    0 ok, 9 token/dependency missing, 19 integrity/terminal HTTP.

acquisition_run <--no-acquire|--force-refresh|-->
    Cache-first orchestration. Sets ACQ_CACHE_DIR and ACQ_ARCHIVE.
    0 ok, 9 cache miss under --no-acquire / no token, 19 integrity failure.
    On 19 writes <bundle>/integrity-failure.json when RUNNER_BUNDLE_DIR is set.
```

`integrity-failure.json` shape:
`{schemaVersion, runId, class, artifact, expectedSha256?, observedSha256?, httpStatus?, recordedAt}`.

## `scripts/lib/preflight.sh` (T085)

```
preflight_resolve_game_path <value>
    Empty/whitespace -> 14. Existing directory -> sets PREFLIGHT_GAME_PATH, 0.
    Non-directory (non-empty) -> 9.

preflight_check_assemblies <game_dir>
    Six assemblies from the plan. Missing -> 9 and prints the missing paths.
    Sets PREFLIGHT_MISSING.

preflight_check_deployment <mods_dir>
    Requires <mods_dir>/LivingRoots/LivingRoots.dll and
    <mods_dir>/LivingRoots.Verification/LivingRoots.Verification.dll.
    Missing -> 12 with the missing path.

preflight_host_tools <tool>...
    Missing -> 9 naming the tool.

preflight_non_root
    EUID 0 -> 21 (POLICY, documented non-root refusal). Otherwise 0.
    Never allocates a display or launches anything.

preflight_record_blocked <bundle_dir> <prerequisite> <checked_path>
    Writes <bundle_dir>/blocked.json:
    {"schemaVersion":1,"status":"BLOCKED","missingPrerequisite":"<name>",
     "checkedPath":"<path>","recordedAt":"<iso>"}.
    BLOCKED is never a pass.

preflight_version_gate <game_dir> <manifest>
    Delegates to scripts/lib/version-gate.sh; HARD-FAIL -> 19.
```

## `scripts/lib/display.sh` (T086)

```
display_start
    Starts "$RUNNER_XVFB -nolisten tcp -displayfd 3 3><fd-file>" with stdin from
    /dev/null in its own process group. Waits for the display number; sets
    DISPLAY, DISPLAY_NUM, DISPLAY_PID, DISPLAY_SOCKET, DISPLAY_OWNED=1.
    Must never read or probe /tmp/.X*-lock. Failure -> 10 (no orphans).

display_stop
    SIGTERM the owned group, wait, SIGKILL after a bounded grace, then waitpid.
    Removes only the socket it owns. Idempotent. Never signals a group whose
    PID identity token no longer matches (PGID-reuse guard).

display_orphan_report <bundle_dir>
    Report-only entry in orphaned-displays.json; never kills another owner.
```

## `scripts/lib/save.sh` (T087)

```
save_isolate <instance_root> <source_save_dir> <save_name>
    Creates <instance_root>/xdg/StardewValley/Saves/<save_name> as the working
    copy and <instance_root>/save-backup as the sidecar backup. Sets
    SAVE_WORKING_DIR, SAVE_BACKUP_DIR, SAVE_SOURCE_DIR, SAVE_SOURCE_HASH.
    Source is read-only; never opens the player save. 0/9/19.

save_hash_tree <dir>       # stable sorted path+sha256 digest of a directory
save_verify_source_unchanged   # 19 when the source digest changed
save_restore_working_copy      # atomic restore from backup; 19 on mismatch
save_assert_no_player_save_access <player_saves_dir>
    Fails 19 when any path under the player save was opened; the function
    itself never reads it.
```

## `scripts/lib/launch.sh` (T088)

```
launch_start <launcher> <log_path> [timeout_seconds]
    Removes a stale log, records L1 (launch timestamp) and log inode, starts the
    launcher with stdin </dev/null in its own process group, sets LAUNCH_PID.
    No display -> 10. Missing launcher -> 9.

launch_wait_ready <log_path> <pid> <timeout_seconds>
    Two-phase: (1) run sentinel `Log started at … UTC` newer than L1 and log
    inode changed; (2) `Loaded N mods:` block, `GameLaunched` marker, and no
    skipped-mod entry for LivingRoots. Phase-1 timeout -> 4; no log at all and
    the process exited -> 15; skipped mod -> 5.

launch_assert_no_stdin_events <log_path>
    Production assertion: zero stdin/interactive events. Returns 19 when an
    interactive event is present.
```

## `scripts/lib/injection.sh` (T089)

```
injection_validate_commands <scenario.json>
    Typed initCommands only (`kind` in {macro,console}); 14 on malformed.

injection_validate_macro <file>
    .txt extension, UTF-8 (optional BOM), every non-empty line starts `/`,
    no nested `runmacro`, exactly one `/sleep` and it is the last line, no
    leading whitespace before `/`. 14 otherwise.

injection_build_queue <scenario.json> <out_file>
    Emits the commands the orchestrator feeds to SMAPI: one
    `debug runmacro <path>` per macro command, one console command per
    console command (never prefixed with `debug `), then `/sleep <n>` last.
    Completion markers are appended as `console` entries when declared.
    14 on malformed input.

injection_seed <location> <tile> <item_id> <count>
    Seeds through the host-authoritative lock (`claim_acquire`); never seeds
    from a farmhand path and returns 21 when playerRole is farmhand.
```

Macros are written to `<instance_root>/macros/<name>.txt`; no macro may
reference another macro file.

## `scripts/lib/signals.sh` (T090)

```
signals_soil_health_delta <before.json> <after.json> <out.json>
    Uses the helper `evaluate`/SaveComparator contract (0.0001f tolerance,
    missing key = 0, NaN ignored). 0/19.

signals_crop_phase_dump <dump_dir> <out.json> [timeout_seconds]
    Waits for `*.json.done`, rejects a stale sentinel, validates `done: true`
    and sidecar/field agreement, copies the dump to <out.json>. 0/19.

signals_smapi_log_excerpt <log> <out.txt>
    Copies the full SMAPI log into the bundle. It keeps the historical name,
    but no longer filters: a three-pattern excerpt hid the T137 save-load
    failure ("File does not exist") and made a live game look stalled. 0/19.

signals_cross_check <dump.json> <save_after.json> <out.json>
    Independent channels must agree (locationsProcessed, per-tile phaseDays /
    soilHealth). Divergence -> 1 with a divergence record.

signals_write_anomaly <bundle_dir> <runId> <signal> <kind> <observed> <expected> <owner> <rootCause>
    Writes <bundle_dir>/artifacts/anomaly-report.json with exactly
    {runId, signal, kind, observed, expected, owner, date, rootCause}. date is
    ISO-8601 UTC. Always returns 0.

signals_all_present <bundle_dir>
    0 only when all three mandatory signal artifacts exist and are non-empty.
```

Missing/incomplete signal → the caller records `partial`; a capture-path error
(not absence) → `errored`. Farmhand runs assert absence of mutation and the
`Soil growth modifier skipped: not main player.` line instead of a phase delta.

## `scripts/lib/verdict.sh` (T091)

```
verdict_from_exit <code>            # prints pass|fail|errored|partial|skipped
verdict_exit_for <verdict>          # pass=0 fail=18 errored=19 partial=19 skipped=20
verdict_expected_match <actual> <expected>
    0 match, 1 mismatch. Missing expected for positive = "pass".
verdict_evaluate <signals_complete> <signals_agree> <thresholds_ok> <expected> <farmhand_breach>
    Prints the base verdict. Missing signal -> partial; missing bundle artifact
    is handled by evidence.sh and yields errored; host-guard breach -> fail;
    threshold violation -> fail; otherwise pass.
verdict_aggregate <code>...         # 19 > 18 > 21 > 20 > 0 (delegates to suite_aggregate)
verdict_apply_ignore_exit_code <code> <list> <integrity_flag>
    Demotes a listed code to 0. Refuses (prints original code) for integrity
    classes and for POLITICY claim overreach (21); the caller records
    ignoreExitCodeRefused=true. 0/22/unknown -> 14.
verdict_write_attempt <attempt_dir> <attempt> <verdict> <failureClass> <signals> <exitCode>
    Writes attempts/<n>/attempt.json.
```

`verdict_from_exit` maps: 0→pass, 18→fail, 19→errored (partial is a distinct
verdict set by the signal layer and also exits 19), 20→skipped, 4/10/13→errored
with the inherited exit preserved. `flaky` is derived metadata only.

## `scripts/lib/evidence.sh` (T092)

```
evidence_run_id                       # run-YYYYMMDDTHHMMSSZ-<4 lowercase hex>
evidence_init <evidence_root> <runId> # creates evidence-bundle-<runId>/{artifacts,attempts}
evidence_bundle_dir <evidence_root> <runId>
evidence_add_artifact <bundle> <path> # copies into artifacts/ and records the relative name
evidence_attempt_dir <bundle> <n>     # attempts/<n>/
evidence_finalize <bundle> <manifest-input.json>
    Invokes the helper `build-manifest`; the helper is the sole producer of
    manifest.json + manifest.sha256. Never hand-writes manifest JSON.
evidence_write_claims <bundle> <runId> <honestyLevel>
evidence_write_integrity_failure <bundle> <class> <artifact> [detail]
evidence_validate <bundle>            # schema + digest + mandatory artifacts
```

`attempts[]` contains only `attempts/<n>/attempt.json`; per-attempt manifests
are listed in `subject[]`; `attempts/warmup/` is never an attempt. `files`, when
present, uses the `{name,digest:{sha256}}` alias shape.

## `scripts/lib/promotion.sh` (T093)

```
promotion_check_docs <repo_root> [bundle_dir]
    0 no drift, 21 drift, 14 missing/duplicate/inverted/malformed markers.
    Never launches the game.

promotion_apply <repo_root> <feature_test_md> <bundle_dir>
    Only on a clean non-flaky pass with three agreeing signals and honesty >= R.
    Replaces only the text between GATE3 markers, byte-preserving outside the
    block, idempotent, timestamps only on state change. Writes `machine-pass`,
    never `verified`. Appends a transition to promotion-triggers.json.
    Returns 0 applied/unchanged, 21 refused (not eligible), 14 marker error.
```

## `scripts/lib/cleanup.sh` (T094) and `quit.sh` (T094b)

```
cleanup_register <kind> <pid> [display_socket]
cleanup_run
    Process-group SIGTERM → bounded wait → SIGKILL → waitpid; PGID-reuse guard;
    removes only the owned display socket; per-instance temp mods; verification
    mod removed from the real Mods/ (hash-checked); orphan registry report-only;
    temp cleanup preserves the recorded exit code. Sets CLEANUP_FAILED=1 when a
    postcondition cannot be met; the orchestrator then exits 19 and records
    cleanup_failed: true.

quit_request <pid> <save_mode>
quit_wait_clean <pid> <save_dir>
    Save-mode completion, mtime/size stability >= RunnerCleanQuitStabilitySeconds,
    graceful SMAPI exit, SIGKILL backstop after RunnerCleanQuitBackstopSeconds.
    Sets CLEAN_QUIT=1 only when: no children, clean mods path, restored working
    copy, no owned display socket.
```

## `scripts/lib/retry.sh` (T075) and `quarantine.sh` (T076)

```
retry_is_transient <code>   # from compat-matrix.json failureClasses
retry_run <attempt-dir> <command...>
    3 attempts total, 5 s cooldown, unique attempt dirs (never overwritten);
    stops on success/non-transient. Sets RETRY_ATTEMPTS, RETRY_FLAKY.
retry_flaky <first_code> <final_code>

quarantine_resolve_owner [cli]     # cli → QUARANTINE_OWNER → git identity → lfgranja
quarantine_load <registry.json>
quarantine_is_never <class>        # never-quarantine.json closed set
quarantine_record_flaky <registry> <scenarioId> <runId> <date>
                                     # 2 flaky runs in 14 days => candidate
quarantine_apply_timebox <entry> <date>
                                     # 14 days + one 14-day extension = 28-day cap
quarantine_auto_disable <registry> <date>
quarantine_retire <registry> <scenarioId> <approval>
quarantine_shadow_pass <registry> <scenarioId> <runId> <date>
quarantine_status <registry> <scenarioId>     # active|candidate|quarantined|disabled|retired
quarantine_reports_skipped <scenarioId>       # 0 when status is quarantined/disabled
```

There is exactly one 14-day flaky/quarantine trigger path:
`quarantine_record_flaky` is the only writer of candidates; no second trigger
exists in retry.sh.

## `scripts/lib/performance.sh` (T080)

```
performance_evaluate <jsonl>
    Each line {"location":"…","elapsedMilliseconds":N,"gen0Collections":N}.
    Discards one warmup line; every remaining per-location value is compared
    independently with RunnerPerLocationBudgetMs (5). Any value > 5 → 18.
    No averaging, p95, or sum. Sets PERF_GC_DELTA (sum of gen0 deltas) and
    PERF_GC_ATTRIBUTED=gc-attributed when > 0.
```

## `scripts/verify-evidence-bundle.sh` (T103)

```
scripts/verify-evidence-bundle.sh <bundle-dir>
```

Exit `0` valid, `19` schema/digest/mandatory-evidence failure, `21` claim or
honesty overreach. Never launches the game, never writes to the bundle.
