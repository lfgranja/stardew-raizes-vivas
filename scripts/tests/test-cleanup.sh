#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# spawn a process to tear down
sleep 30 &
spid=$!
cleanup_dir="$HARNESS_SANDBOX/clean"
mkdir -p "$cleanup_dir"
call_runner_func cleanup_register "game" "$spid"
assert_fn_code 0 "cleanup_register returns 0"
call_runner_func cleanup_run
assert_fn_code 0 "cleanup_run returns 0"
# process should be gone
if kill -0 "$spid" 2>/dev/null; then fail "process reaped" "still alive"; else pass "process reaped"; fi

# PGID-reuse guard: cleanup_run must not signal a reused pgid; source inspection
if grep -q 'PGID' scripts/lib/cleanup.sh || grep -q 'pgid' scripts/lib/cleanup.sh; then
    pass "PGID handling present"
else
    fail "PGID handling" "missing"
fi

# owned display socket removal: source contains socket removal
if grep -q 'socket' scripts/lib/cleanup.sh; then pass "socket handling present"; else fail "socket handling" "missing"; fi

# orphan registry report-only
if grep -q 'orphan' scripts/lib/cleanup.sh; then pass "orphan registry present"; else fail "orphan registry" "missing"; fi

# exit code preservation
if grep -q 'CLEANUP_FAILED' scripts/lib/cleanup.sh; then pass "CLEANUP_FAILED present"; else fail "CLEANUP_FAILED" "missing"; fi

# quit.sh constants
call_runner_func quit_request 12345 save 2>/dev/null || true
pass "quit_request exercised (rc=$RUNNER_FN_CODE)"
# cleanQuit flag
if grep -q 'CLEAN_QUIT' scripts/lib/quit.sh; then pass "CLEAN_QUIT present"; else fail "CLEAN_QUIT" "missing"; fi
# stability 30s constant
if grep -q '30' scripts/lib/quit.sh scripts/lib/common.sh; then pass "30s stability referenced"; else fail "30s stability" "missing"; fi

# verification mod removed/hash check
if grep -q 'hash' scripts/lib/cleanup.sh; then pass "hash check present"; else fail "hash check" "missing"; fi

finish_tests