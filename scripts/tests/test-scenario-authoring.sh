#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

scn="specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json"
tm="specs/005-soil-health-to-crop-growth/test.md"

# scenario exists
assert_file_exists "$scn" "feature-005 scenario exists"

# validates against scenario_validate
if declare -F scenario_validate >/dev/null 2>&1; then
    scenario_validate "$scn" >/dev/null 2>&1; rc=$?
    assert_eq "0" "$rc" "feature-005 scenario validates"
fi

# testMdPath exists with exactly one GATE3 block
assert_file_exists "$tm" "testMdPath exists"
b=$(grep -c 'GATE3:BEGIN' "$tm" 2>/dev/null || echo 0)
e=$(grep -c 'GATE3:END' "$tm" 2>/dev/null || echo 0)
assert_eq "1" "$b" "exactly one GATE3:BEGIN"
assert_eq "1" "$e" "exactly one GATE3:END"

# optional Markdown companion does not affect validation
comp="specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.md"
printf '# companion\n' > "$comp"
if declare -F scenario_validate >/dev/null 2>&1; then
    scenario_validate "$scn" >/dev/null 2>&1; rc=$?
    assert_eq "0" "$rc" "companion markdown does not affect validation"
fi
rm -f "$comp"

finish_tests
