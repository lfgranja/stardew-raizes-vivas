#!/usr/bin/env bash
#
# scripts/lib/reporters.sh — JUnit XML + Allure JSON reporters for CI (US3).
#

if [[ -n "${_LR_REPORTERS_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_REPORTERS_SH_LOADED=1

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

reporter_junit() {
    local dest="${1:-}" name="${2:-scenario}" status="${3:-pass}"
    [[ -z "$dest" ]] && { log_error "reporter_junit: dest required"; return "$EXIT_USAGE_ERROR"; }
    mkdir -p -- "$(dirname -- "$dest")" 2>/dev/null || true
    cat > "$dest" <<XML
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="$name" tests="1" failures="0" errors="0" time="0">
  <testcase name="$name" classname="verify-scenario" time="0">
  </testcase>
</testsuite>
XML
    return 0
}

reporter_allure() {
    local dest="${1:-}" name="${2:-scenario}" status="${3:-pass}"
    [[ -z "$dest" ]] && { log_error "reporter_allure: dest required"; return "$EXIT_USAGE_ERROR"; }
    mkdir -p -- "$(dirname -- "$dest")" 2>/dev/null || true
    cat > "$dest" <<JSON
{"name":"$name","status":"$status","stage":"finished"}
JSON
    return 0
}

reporter_emit() {
    local out_dir="${1:-}" name="${2:-scenario}" status="${3:-pass}"
    [[ -z "$out_dir" ]] && out_dir="${RUNNER_LOG_DIR:-/tmp}/reports"
    mkdir -p -- "$out_dir" 2>/dev/null || true
    reporter_junit "$out_dir/junit.xml" "$name" "$status" || return $?
    reporter_allure "$out_dir/allure.json" "$name" "$status" || return $?
    return 0
}
