#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

if (( EUID != 0 )); then
    call_runner_func preflight_run "" "" "" 0
    if (( RUNNER_FN_CODE == 21 )); then fail "non-root must not be policy-blocked" "got 21"; else pass "non-root passes root guard"; fi
else
    skip "root-guard non-root case" "non-root environment"
fi

assert_contains_file "scripts/lib/preflight.sh" "EUID == 0" "root guard present in preflight"
assert_contains_file "scripts/lib/preflight.sh" "Non-root guard first" "guard precedes launch"

finish_tests
