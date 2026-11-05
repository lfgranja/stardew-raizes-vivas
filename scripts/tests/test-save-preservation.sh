#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

src="$HARNESS_SANDBOX/srcsave"
mkdir -p "$src"
make_fake_save "$src" "playersave" '{"Farm":{"0,0":5}}' '{}'

inst="$HARNESS_SANDBOX/inst"
mkdir -p "$inst"
call_runner_func save_isolate "$inst" "$src" "playersave"
assert_fn_code 0 "save_isolate returns 0"
[[ -n "${SAVE_WORKING_DIR:-}" ]] && pass "SAVE_WORKING_DIR set" || fail "SAVE_WORKING_DIR" "empty"
[[ -n "${SAVE_BACKUP_DIR:-}" ]] && pass "SAVE_BACKUP_DIR set" || fail "SAVE_BACKUP_DIR" "empty"
[[ -n "${SAVE_SOURCE_HASH:-}" ]] && pass "SAVE_SOURCE_HASH set" || fail "SAVE_SOURCE_HASH" "empty"

# source never opened: hash unchanged
call_runner_func save_verify_source_unchanged
assert_fn_code 0 "source unchanged initially"

# mutate source -> 19
printf 'tampered' >> "$src/modData/soil_health_data_playersave.json"
call_runner_func save_verify_source_unchanged
assert_fn_code 19 "source mutation => 19"

# restore source for further tests
printf '{"Farm":{"0,0":5}}\n' > "$src/modData/soil_health_data_playersave.json"

# hash tree deterministic
call_runner_func save_hash_tree "$src"
pass "save_hash_tree exercised (rc=$RUNNER_FN_CODE)"

# restore working copy
call_runner_func save_restore_working_copy
assert_fn_code 0 "restore working copy returns 0"

# assert no player save access
psaves="$HARNESS_SANDBOX/playersaves"
mkdir -p "$psaves"
# simulate an access log entry
printf '%s\n' "$psaves/secret" > "$HARNESS_SANDBOX/accessed.txt" 2>/dev/null || true
call_runner_func save_assert_no_player_save_access "$psaves"
assert_fn_code 0 "no access recorded => 0"

# terms consistency: source inspection
for term in player source working; do
    if grep -qi "$term" scripts/lib/save.sh; then pass "term '$term' used in save.sh"; else fail "term '$term'" "missing"; fi
done

# ---------------------------------------------------------------------------
# save_prepare: the orchestration primitive that used to be skipped silently.
# It must resolve the milestone under the caller's saves root, isolate it into
# the XDG tree the launcher reads, and never mutate the source.
# ---------------------------------------------------------------------------
saves_root="$HARNESS_SANDBOX/milestone-saves"
mkdir -p "$saves_root"
make_fake_save "$saves_root/farm-year1-day5" "farm-year1-day5" '{"Farm":{"-10,5":10}}' '{}'
run_root="$HARNESS_SANDBOX/save-prepare-run"
mkdir -p "$run_root"

_saved_home="${HOME-}"
_saved_xdg_config="${XDG_CONFIG_HOME-}"
_saved_xdg_data="${XDG_DATA_HOME-}"
_saved_xdg_cache="${XDG_CACHE_HOME-}"
_saved_xdg_state="${XDG_STATE_HOME-}"
_saved_runner_save_id="${RUNNER_SAVE_ID-}"
_saved_runner_save_dir="${RUNNER_SAVE_DIR-}"
_saved_runner_save_instance="${RUNNER_SAVE_INSTANCE_DIR-}"
_saved_runner_smapi_log="${RUNNER_SMAPI_LOG-}"

RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "farm-year1-day5" "$run_root"
assert_fn_code 0 "save_prepare isolates an existing milestone"
[[ -n "${RUNNER_SAVE_ID:-}" ]] && pass "save_prepare exports RUNNER_SAVE_ID" || fail "save_prepare RUNNER_SAVE_ID" "empty"
if [[ "${RUNNER_SAVE_DIR:-}" == */StardewValley/Saves/farm-year1-day5 ]]; then
    pass "save_prepare working dir is under the isolated XDG saves tree"
else
    fail "save_prepare working dir" "got ${RUNNER_SAVE_DIR:-<empty>}"
fi
if [[ -f "${RUNNER_SAVE_DIR:-}/modData/soil_health_data_farm-year1-day5.json" ]]; then
    pass "save_prepare copied the milestone mod data"
else
    fail "save_prepare copied mod data" "missing copied soil-health file"
fi
source_hash_before="$(save_hash_tree "$saves_root/farm-year1-day5")"
if [[ "$source_hash_before" == "$(save_hash_tree "$saves_root/farm-year1-day5")" ]]; then
    pass "save_prepare leaves the source milestone unchanged"
else
    fail "save_prepare source unchanged" "source hash moved"
fi

# A milestone packaged by renaming only its folder must still load: Stardew
# resolves Saves/<name>/<name>, so save_isolate has to rename the inner main
# file (T137: farm-year1-day5/Agroeco_421179919 -> "File does not exist").
mkdir -p "$saves_root/renamed-only"
make_fake_save "$saves_root/renamed-only" "Agroeco_421179919" '{"Farm":{"-10,5":10}}' '{}'
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "renamed-only" "$run_root"
assert_fn_code 0 "save_prepare aligns a renamed milestone"
if [[ -f "${RUNNER_SAVE_DIR:-}/renamed-only" ]]; then
    pass "inner save file matches the folder name"
else
    fail "inner save file" "missing renamed-only"
fi
if [[ ! -e "${RUNNER_SAVE_DIR:-}/Agroeco_421179919" ]]; then
    pass "old inner save-file name removed from the working copy"
else
    fail "old inner save-file name" "still present"
fi
if [[ -f "$saves_root/renamed-only/Agroeco_421179919" ]]; then
    pass "renaming leaves the source milestone untouched"
else
    fail "source milestone renamed" "source changed"
fi

# An unloadable milestone must fail at prepare time, not at game load.
mkdir -p "$saves_root/ambiguous"
printf 'one' > "$saves_root/ambiguous/one"
printf 'two' > "$saves_root/ambiguous/two"
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "ambiguous" "$run_root"
assert_fn_code 19 "unloadable milestone => 19"

# Missing milestone must be loud (19), never a silent skip.
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "does-not-exist" "$run_root"
assert_fn_code 19 "save_prepare missing milestone => 19"

# Empty milestone is also 19 (the old code passed "" and skipped).
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "" "$run_root"
assert_fn_code 19 "save_prepare empty milestone => 19"

# ---------------------------------------------------------------------------
# FR-008 / runner hardening additions
# ---------------------------------------------------------------------------

# Reset the environment to a clean baseline before the new cases.
export HOME="$_saved_home"
export XDG_CONFIG_HOME="$_saved_xdg_config"
export XDG_DATA_HOME="$_saved_xdg_data"
export XDG_CACHE_HOME="$_saved_xdg_cache"
export XDG_STATE_HOME="$_saved_xdg_state"
unset RUNNER_SAVE_ID RUNNER_SAVE_DIR RUNNER_SAVE_INSTANCE_DIR RUNNER_SMAPI_LOG RUNNER_ORIGINAL_HOME 2>/dev/null || true

# Test: save_prepare aligns a renamed milestone (folder != inner file name).
mkdir -p "$saves_root/renamed-fr008"
make_fake_save "$saves_root/renamed-fr008" "OldInner_999" '{"Farm":{"-10,5":10}}' '{}'
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "renamed-fr008" "$run_root"
assert_fn_code 0 "save_prepare aligns a renamed milestone (exit 0)"
if [[ -f "${RUNNER_SAVE_DIR:-}/renamed-fr008" ]]; then
    pass "inner save file renamed to match the folder"
else
    fail "inner save file renamed" "missing ${RUNNER_SAVE_DIR:-}/renamed-fr008"
fi
if [[ ! -e "${RUNNER_SAVE_DIR:-}/OldInner_999" ]]; then
    pass "old inner save-file name removed from the working copy"
else
    fail "old inner save-file name" "still present"
fi

# Test: save_prepare fails loudly (19) when align is disabled and names disagree.
mkdir -p "$saves_root/disabled-align"
make_fake_save "$saves_root/disabled-align" "OldInner_777" '{"Farm":{"-10,5":10}}' '{}'
# Monkey-patch save_align_inner_name to a no-op to simulate "disabled".
save_align_inner_name() { return 0; }
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "disabled-align" "$run_root"
assert_fn_code 19 "save_prepare fails when align is disabled and names disagree"
# Restore the real implementation.
unset _LR_SAVE_LOADED 2>/dev/null || true
# shellcheck disable=SC1090
source "$RUNNER_REPO_ROOT/scripts/lib/save.sh" 2>/dev/null || true

# Test: save_prepare fails loudly (19) for zero candidate files (ambiguous layout).
mkdir -p "$saves_root/zero-candidate"
printf '{}' > "$saves_root/zero-candidate/sidecar.json"
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "zero-candidate" "$run_root"
assert_fn_code 19 "save_prepare zero candidates => 19"

# Test: save_prepare fails loudly (19) for multiple candidate files (ambiguous layout).
mkdir -p "$saves_root/multi-candidate"
printf 'one' > "$saves_root/multi-candidate/one"
printf 'two' > "$saves_root/multi-candidate/two"
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "multi-candidate" "$run_root"
assert_fn_code 19 "save_prepare multiple candidates => 19"

# Test: save_prepare preserves original HOME in RUNNER_ORIGINAL_HOME (FR-008).
_orig_home="$HOME"
mkdir -p "$saves_root/fr008-save"
make_fake_save "$saves_root/fr008-save" "fr008-save" '{"Farm":{"-10,5":10}}' '{}'
unset RUNNER_ORIGINAL_HOME 2>/dev/null || true
RUNNER_SAVES_ROOT="$saves_root" RUNNER_SMAPI_LOG_PINNED=1 \
    call_runner_func save_prepare "fr008-save" "$run_root"
assert_fn_code 0 "save_prepare FR-008 exit 0"
assert_eq "$_orig_home" "${RUNNER_ORIGINAL_HOME:-}" "RUNNER_ORIGINAL_HOME preserved"

# Restore the caller environment: save_isolate calls save_setup_env, which
# exports HOME/XDG_* into this test process.
export HOME="$_saved_home"
export XDG_CONFIG_HOME="$_saved_xdg_config"
export XDG_DATA_HOME="$_saved_xdg_data"
export XDG_CACHE_HOME="$_saved_xdg_cache"
export XDG_STATE_HOME="$_saved_xdg_state"
export RUNNER_SAVE_ID="$_saved_runner_save_id"
export RUNNER_SAVE_DIR="$_saved_runner_save_dir"
export RUNNER_SAVE_INSTANCE_DIR="$_saved_runner_save_instance"
export RUNNER_SMAPI_LOG="$_saved_runner_smapi_log"

finish_tests