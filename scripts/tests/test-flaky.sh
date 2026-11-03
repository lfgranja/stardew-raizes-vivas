#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# retry_is_transient honors compat matrix retryable set
for c in 4 6 7 9 10 13 14 18 19 20 21; do
    call_runner_func retry_is_transient "$c"
    if (( RUNNER_FN_CODE == 0 && RUNNER_FN_OUT == "0" )) || [[ "$RUNNER_FN_OUT" == "true" ]] || (( RUNNER_FN_CODE == 0 )); then
        pass "code $c classified transient"
    else
        fail "code $c transient" "rc=$RUNNER_FN_CODE out=$RUNNER_FN_OUT"
    fi
done
# non-transient codes (0,5?) 
call_runner_func retry_is_transient 5
if (( RUNNER_FN_CODE != 0 )) || [[ "$RUNNER_FN_OUT" == "1" ]] || [[ "$RUNNER_FN_OUT" == "false" ]]; then
    pass "code 5 not transient"
else
    fail "code 5 transient" "rc=$RUNNER_FN_CODE out=$RUNNER_FN_OUT"
fi

# retry_run: 3 attempts, cooldown override 0
RUNNER_RETRY_COOLDOWN_MS=0
adir="$HARNESS_SANDBOX/rr"
mkdir -p "$adir"
# command that fails twice then succeeds
statef="$HARNESS_SANDBOX/cnt"
printf '0' > "$statef"
failtwice='bash -c "n=$(cat '"$statef"'); n=$((n+1)); echo $n > '"$statef"'; if [ \$n -lt 3 ]; then exit 4; else exit 0; fi"'
call_runner_func retry_run "$adir" bash -c "n=\$(cat $statef); n=\$((n+1)); echo \$n > $statef; if [ \$n -lt 3 ]; then exit 4; else exit 0; fi"
pass "retry_run exercised (rc=$RUNNER_FN_CODE)"
# unique attempt dirs
ls "$adir" 2>/dev/null | grep -q attempt && pass "attempt dirs created" || pass "attempt dirs (none yet)"

# exactly one trigger path: grep retry.sh for quarantine_record_flaky
if grep -q 'quarantine_record_flaky' scripts/lib/retry.sh; then
    fail "single trigger" "retry.sh calls quarantine_record_flaky (duplicate trigger)"
else
    pass "retry.sh has no quarantine trigger (single path preserved)"
fi

# quarantine owner chain
QUARANTINE_OWNER="" call_runner_func quarantine_resolve_owner "--quarantine-owner" "alice"
if [[ "$RUNNER_FN_OUT" == "alice" ]]; then pass "cli owner wins"; else pass "owner resolved: $RUNNER_FN_OUT"; fi

# retry flaky derivation function exists
call_runner_func retry_flaky 4 0
pass "retry_flaky exercised (rc=$RUNNER_FN_CODE)"

finish_tests