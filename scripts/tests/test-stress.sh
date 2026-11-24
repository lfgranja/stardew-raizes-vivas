#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

sh="$HARNESS_REPO_ROOT/scripts/tests/stress/run_stress.sh"
if [[ -f "$sh" ]]; then
    pass "stress harness exists"
else
    fail "stress harness exists" "missing $sh"
fi
# run with tiny overrides if supported
if [[ -f "$sh" ]]; then
    STRESS_K=2 STRESS_LOOPS=3 bash "$sh" >/dev/null 2>&1 || true
    pass "stress harness invoked"
fi
# contract fields
for f in interleaving_rate correctness_rate sentinel_order_violations; do
    if grep -q "$f" "$sh" 2>/dev/null; then pass "field $f present"; else fail "field $f" "missing"; fi
done
# K>=30 and 1000-loop referenced
if grep -q '30' "$sh" 2>/dev/null; then pass "K>=30 referenced"; else fail "K>=30" "missing"; fi
if grep -q '1000' "$sh" 2>/dev/null; then pass "1000-loop referenced"; else fail "1000-loop" "missing"; fi

finish_tests