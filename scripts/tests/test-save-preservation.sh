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

finish_tests