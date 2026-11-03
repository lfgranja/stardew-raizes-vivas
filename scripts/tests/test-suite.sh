#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

tmp="$(mktemp -d)"
mkdir -p "$tmp/empty"
# empty dir -> no tests -> exit 20
rc=0; suite_discover "$tmp/empty" >/dev/null 2>&1 || rc=$?
assert_eq "20" "$rc" "empty directory discovery returns 20 (no tests)"

mkdir -p "$tmp/suite"
printf '{}' > "$tmp/suite/b.scenario.json"
printf '{}' > "$tmp/suite/a.scenario.json"
printf '{}' > "$tmp/suite/c.json"

out="$(suite_discover "$tmp/suite")"
assert_contains "$out" "a.scenario.json" "discovers a.scenario.json"
assert_contains "$out" "b.scenario.json" "discovers b.scenario.json"
assert_not_contains "$out" "c.json" "fallback *.json not used when *.scenario.json present"

first="$(printf '%s\n' "$out" | head -n1)"
assert_contains "$first" "a.scenario.json" "lexicographic order: a first"

mkdir -p "$tmp/fallback"
printf '{}' > "$tmp/fallback/z.json"
printf '{}' > "$tmp/fallback/a.json"
out2="$(suite_discover "$tmp/fallback")"
assert_contains "$out2" "a.json" "fallback discovers a.json"
assert_contains "$out2" "z.json" "fallback discovers z.json"

cap="$(suite_rerun_cap)"
assert_eq "4" "$cap" "suite rerun cap is 4"

agg="$(suite_aggregate 0 18)"
assert_eq "18" "$agg" "aggregate fail over pass"
agg="$(suite_aggregate 18 19)"
assert_eq "19" "$agg" "aggregate errored over fail"
agg="$(suite_aggregate 20 0)"
assert_eq "0" "$agg" "aggregate pass over no-tests when pass present"
agg="$(suite_aggregate 20)"
assert_eq "20" "$agg" "aggregate no-tests alone"
agg="$(suite_aggregate 21 18)"
assert_eq "18" "$agg" "aggregate fail over policy"
agg="$(suite_aggregate 21 19)"
assert_eq "19" "$agg" "aggregate errored over policy"

mkdir -p "$tmp/badsuite"
printf '{not json' > "$tmp/badsuite/x.scenario.json"
if declare -F scenario_validate >/dev/null 2>&1; then
    rc=0; scenario_validate "$tmp/badsuite/x.scenario.json" >/dev/null 2>&1 || rc=$?
    assert_eq "14" "$rc" "malformed scenario in suite rejected with 14"
fi

rm -rf "$tmp"
finish_tests
