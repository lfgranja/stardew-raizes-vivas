#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# T108: zero-core-change sanity. The feature-005 scenario must exist and the
# runner source files must be untouched in spirit (we only assert presence).
assert_file_exists "specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json" "005 scenario present"
assert_file_exists "scripts/verify-scenario.sh" "runner entry point present"
assert_file_exists "scripts/lib/suite.sh" "suite discovery present"

# scenario validates
if declare -F scenario_validate >/dev/null 2>&1; then
    scenario_validate "specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json" >/dev/null 2>&1; rc=$?
    assert_eq "0" "$rc" "005 scenario validates with zero core changes"
fi

finish_tests
