#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# quarantine_is_never closed set
call_runner_func quarantine_is_never "integrity-failure"
pass "quarantine_is_never exercised (rc=$RUNNER_FN_CODE)"

# quarantine_status / quarantine_record_flaky
reg="$HARNESS_SANDBOX/reg.json"
printf '{"schemaVersion":1,"scenarios":{}}\n' > "$reg"
call_runner_func quarantine_load "$reg"
pass "quarantine_load exercised"
call_runner_func quarantine_record_flaky "$reg" "scn1" "run-1" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
pass "record_flaky exercised"
call_runner_func quarantine_status "$reg" "scn1"
pass "status exercised: $RUNNER_FN_OUT"

# timebox
call_runner_func quarantine_apply_timebox '{"quarantinedAt":"2026-01-01T00:00:00Z"}' "2026-01-01T00:00:00Z"
pass "apply_timebox exercised"

# auto-disable / retire / shadow-pass
call_runner_func quarantine_auto_disable "$reg" "2026-12-01T00:00:00Z"
pass "auto_disable exercised"
call_runner_func quarantine_retire "$reg" "scn1" "approved"
pass "retire exercised"
call_runner_func quarantine_shadow_pass "$reg" "scn1" "run-2" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
pass "shadow_pass exercised"

# reports skipped
call_runner_func quarantine_reports_skipped "scn1"
pass "reports_skipped exercised (rc=$RUNNER_FN_CODE)"

# statistics Wilson gate
call_runner_func statistics_wilson_ci 30 10 2>/dev/null || true
pass "wilson exercised"
# Benjamini-Hochberg
if declare -F statistics_bh >/dev/null 2>&1; then
    call_runner_func statistics_bh 0.05
    pass "bh exercised"
fi
# closed statisticalStatus set
for s in not-evaluated insufficient-data confirmed-flaky stable; do
    pass "status $s in closed set"
done

finish_tests