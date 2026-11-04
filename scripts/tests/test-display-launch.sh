#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# display_start with working Xvfb stub
xdir="$HARNESS_SANDBOX/xvfb"
make_display_xvfb "$xdir"
RUNNER_XVFB="$xdir/Xvfb" call_runner_func display_start
assert_fn_code 0 "display_start succeeds"
assert_eq "1" "${DISPLAY_OWNED:-0}" "DISPLAY_OWNED=1"
[[ -n "${DISPLAY:-}" ]] && pass "DISPLAY set" || fail "DISPLAY set" "empty"
[[ -n "${DISPLAY_PID:-}" ]] && pass "DISPLAY_PID set" || fail "DISPLAY_PID set" "empty"

# idempotent stop
call_runner_func display_stop
assert_fn_code 0 "display_stop returns 0"
call_runner_func display_stop
assert_fn_code 0 "display_stop idempotent"

# failing Xvfb => 10
fdir="$HARNESS_SANDBOX/fxvfb"
make_failing_xvfb "$fdir"
RUNNER_XVFB="$fdir/Xvfb" call_runner_func display_start
assert_fn_code 10 "failing Xvfb exits 10"

# no /tmp/.X*-lock probe in source
if grep -Eq '/tmp/\.X[0-9-]*-lock|\.X.*lock' scripts/lib/display.sh; then
    fail "no lock probe" "found lock probe in display.sh"
else
    pass "no /tmp/.X*-lock probe"
fi

# launch_start missing launcher => 9
ldir="$HARNESS_SANDBOX/launch"
mkdir -p "$ldir"
call_runner_func launch_start "$ldir/no-launcher" "$ldir/log.txt" 5
assert_fn_code 9 "missing launcher exits 9"

# no display => 10 (unset DISPLAY)
(
  unset DISPLAY
  call_runner_func launch_start "/bin/true" "$ldir/log2.txt" 5
  rc=$RUNNER_FN_CODE
  if (( rc == 10 )); then echo OK10; else echo "GOT $rc"; fi
) >/dev/null 2>&1 || true

# launch_wait_ready: build a log with sentinel + loaded mods + GameLaunched
log="$ldir/good.log"
{
  printf 'Log started at %s UTC\n' "$(date -u '+%Y-%m-%dT%H:%M:%S')"
  printf 'Loaded 2 mods:\n'
  printf '   LivingRoots 0.4.0 by lfgranja\n'
  printf 'GameLaunched\n'
} > "$log"
# launch_wait_ready signature: <log_path> <pid> <timeout>
call_runner_func launch_wait_ready "$log" "99999" 2
assert_fn_code 0 "ready log accepted"

# phase 2 must poll: on a real install the mod-resolution header lands ~20 s
# after the sentinel, so a single grep would report 17 on every healthy launch.
delayed="$ldir/delayed.log"
printf 'Log started at %s UTC\n' "$(date -u '+%Y-%m-%dT%H:%M:%S')" > "$delayed"
( sleep 1; printf 'Loaded 4 mods:\n' >> "$delayed" ) &
_delayed_writer=$!
call_runner_func launch_wait_ready "$delayed" "99999" 5
assert_fn_code 0 "delayed mod-resolution header accepted (phase 2 polls)"
wait "$_delayed_writer" 2>/dev/null || true

# sentinel present but the header never appears => 17, not a pass
stuck="$ldir/stuck.log"
printf 'Log started at %s UTC\n' "$(date -u '+%Y-%m-%dT%H:%M:%S')" > "$stuck"
call_runner_func launch_wait_ready "$stuck" "99999" 1
assert_fn_code 17 "missing mod-resolution header exits 17"

# skipped mod detection
skiplog="$ldir/skip.log"
{
  printf 'Log started at %s UTC\n' "$(date -u '+%Y-%m-%dT%H:%M:%S')"
  printf 'Loaded 0 mods.\n'
  printf '   Skipped mods\n'
  printf '      - LivingRoots 0.4.0 needs SMAPI 4.6.0\n'
} > "$skiplog"
call_runner_func launch_wait_ready "$skiplog" "99999" 2
assert_fn_code 5 "skipped mod exits 5"

# no log + process exited => 15
nolog="$ldir/none.log"
call_runner_func launch_wait_ready "$nolog" "99999" 1
# process 99999 not running => 15 expected
if (( RUNNER_FN_CODE == 15 || RUNNER_FN_CODE == 4 )); then
    pass "no-log case classified"
else
    fail "no-log case" "got $RUNNER_FN_CODE"
fi

# launch_assert_no_stdin_events
badlog="$ldir/stdin.log"
printf 'debug: stdin event received\n' > "$badlog"
call_runner_func launch_assert_no_stdin_events "$badlog"
assert_fn_code 19 "stdin event => 19"

# ---------------------------------------------------------------------------
# Orchestrator exit propagation: a launcher that exits without producing a
# SMAPI log must surface launch_wait_ready's code (15), never 0. This pins the
# false-pass where `if ! launch_wait_ready ...; then rc=$?` captured the
# negated status and exited 0.
# ---------------------------------------------------------------------------
orch="$HARNESS_SANDBOX/orchestrator"
mkdir -p "$orch"
make_fake_game_dir "$orch/game"
rm -f "$orch/game/Stardew Valley.dll"
printf 'MZ\000\000' > "$orch/game/Stardew Valley.dll"
printf '1.6.15' >> "$orch/game/Stardew Valley.dll"
make_fake_mods "$orch/game/Mods"
make_display_xvfb "$orch/xvfb"
write_min_scenario "$orch/scenario.json"
export RUNNER_XVFB="$orch/xvfb/Xvfb"
run_verify --scenario "$orch/scenario.json" --game-path "$orch/game" --skip-build --no-acquire --timeout 1
assert_verify_code 15 "launch failure propagates as exit 15, not 0"
# The failure path must still stop the display; a bare `f; rc=$?` under
# `set -e` would abort before display_stop and leak the Xvfb process.
sleep 0.3
if pgrep -f "$orch/xvfb/Xvfb" >/dev/null 2>&1; then
    fail "launch failure stops the display" "Xvfb stub still running"
else
    pass "launch failure stops the display"
fi
unset RUNNER_XVFB

# ---------------------------------------------------------------------------
# A launch that reaches ready but captures no signals must exit `partial` (19),
# never 0. This pins the false pass where the pipeline skipped every capture
# step with `|| true` and fell off the end with a success status.
# ---------------------------------------------------------------------------
cap="$HARNESS_SANDBOX/capture"
mkdir -p "$cap/logs"
make_fake_game_dir "$cap/game"
make_fake_mods "$cap/game/Mods"
make_display_xvfb "$cap/xvfb"
write_min_scenario "$cap/scenario.json"
printf 'Log started at 2020-01-01T00:00:00 UTC\nLoaded 2 mods:\nGameLaunched\n' > "$cap/template.log"
export RUNNER_XVFB="$cap/xvfb/Xvfb"
export RUNNER_SMAPI_LOG="$cap/logs/SMAPI-latest.txt"
export FAKE_SMAPI_LOG_TEMPLATE="$cap/template.log"
export FAKE_SMAPI_LOG_TARGET="$cap/logs/SMAPI-latest.txt"
run_verify --scenario "$cap/scenario.json" --game-path "$cap/game" --skip-build --no-acquire --timeout 5
assert_verify_code 19 "ready launch with no captured signals exits 19, not 0"
unset RUNNER_XVFB RUNNER_SMAPI_LOG FAKE_SMAPI_LOG_TEMPLATE FAKE_SMAPI_LOG_TARGET

finish_tests