#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

game="$HARNESS_SANDBOX/game"
make_fake_game_dir "$game"
mods="$game/Mods"
make_fake_mods "$mods"

scn="$HARNESS_SANDBOX/min.scenario.json"
write_min_scenario "$scn"

# missing game-path (no env) => 14 when required? per contract missing value usage 14
run_verify --scenario "$scn" --game-path
assert_verify_code 14 "missing --game-path value exits 14"

# empty
run_verify --scenario "$scn" --game-path ""
assert_verify_code 14 "empty --game-path exits 14"

# invalid non-empty path follows acquisition/9
run_verify --scenario "$scn" --game-path "$HARNESS_SANDBOX/nope" --no-acquire
assert_verify_code 9 "invalid game path under --no-acquire exits 9"

# valid path reuses (no 9/14); fake game has no real assemblies but preflight may pass assemblies check (files exist)
run_verify --scenario "$scn" --game-path "$game" --no-acquire
if (( RUN_VERIFY_CODE == 14 || RUN_VERIFY_CODE == 9 )); then
    fail "valid game path should not be usage/missing dep" "got $RUN_VERIFY_CODE"
else
    pass "valid game path passes preflight-ish"
fi

# preflight_check_assemblies direct
missing="$HARNESS_SANDBOX/broken"
make_fake_game_dir "$missing"
rm -f "$missing/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll"
call_runner_func preflight_check_assemblies "$missing"
assert_fn_code 9 "missing assembly exits 9"
assert_contains "$RUNNER_FN_OUT" "SMAPI.Toolkit.CoreInterfaces.dll" "missing path named"

# preflight_check_deployment direct
call_runner_func preflight_check_deployment "$HARNESS_SANDBOX/emptymods"
assert_fn_code 12 "missing deployment exits 12"

# preflight_record_blocked
bdir="$HARNESS_SANDBOX/bundle"
mkdir -p "$bdir"
call_runner_func preflight_record_blocked "$bdir" "STARDEW_GAME_PATH" "$game"
assert_fn_code 0 "record blocked returns 0"
assert_json_eq "$bdir/blocked.json" '.status' "BLOCKED" "blocked status"
assert_json_eq "$bdir/blocked.json" '.missingPrerequisite' "STARDEW_GAME_PATH" "blocked prereq"

# non-root guard constant exists
call_runner_func preflight_non_root
# running as non-root in this env => 0
if (( EUID != 0 )); then assert_fn_code 0 "non-root passes"; fi

# preflight_resolve_game_path empty => 14
call_runner_func preflight_resolve_game_path ""
assert_fn_code 14 "empty resolve exits 14"
# existing dir => 0
call_runner_func preflight_resolve_game_path "$game"
assert_fn_code 0 "existing dir resolves"

finish_tests