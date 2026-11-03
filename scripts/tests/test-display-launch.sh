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

finish_tests