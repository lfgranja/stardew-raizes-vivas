#!/usr/bin/env bash
set -Eeuo pipefail
. "$(dirname "$0")/lib/harness.sh"
. "$(dirname "$0")/../lib/common.sh"

pass=0; fail=0
check() { if eval "$2"; then pass=$((pass+1)); else echo "FAIL: $1"; fail=$((fail+1)); fi; }

check "exit pass code is 0" '[ "$EXIT_PASS" = "0" ]'
check "exit usage is 14" '[ "$EXIT_USAGE_ERROR" = "14" ]'
check "exit fail is 18" '[ "$EXIT_FAIL" = "18" ]'
check "exit errored is 19" '[ "$EXIT_ERRORED" = "19" ]'
check "exit no tests is 20" '[ "$EXIT_NO_TESTS" = "20" ]'
check "exit policy is 21" '[ "$EXIT_POLICY" = "21" ]'
check "runner max suite reruns is 4" '[ "$RunnerMaxSuiteReruns" = "4" ]'

echo "# tests: $((pass+fail)), passed: $pass, failed: $fail"
if [ "$fail" -gt 0 ]; then exit 1; fi
exit 0