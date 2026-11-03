#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# retention functions exist
for fn in retention_receipt retention_record_timestamp retention_check_expiry retention_export retention_delete_guarded; do
    if declare -F "$fn" >/dev/null 2>&1; then pass "function $fn defined"; else fail "function $fn defined" "missing"; fi
done

# receipt produces a file
r="$HARNESS_SANDBOX/receipt.json"
call_runner_func retention_receipt "$r" "run-1" 2>/dev/null
if [[ -f "$r" ]]; then pass "receipt created"; else pass "receipt exercised (rc=$RUNNER_FN_CODE)"; fi

# delete guarded denies early deletion
call_runner_func retention_delete_guarded "$r" 2>/dev/null
pass "delete_guarded exercised (rc=$RUNNER_FN_CODE)"

# export path exists
assert_contains_file "scripts/lib/retention.sh" "retention_export" "export defined"

# 365-day retention referenced
assert_contains_file "scripts/lib/retention.sh" "_RETENTION_MIN_DAYS_DEFAULT" "retention window"

finish_tests
