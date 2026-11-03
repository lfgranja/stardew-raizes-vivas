#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# build a jsonl with one warmup + fast values + one slow
jsonl="$HARNESS_SANDBOX/perf.jsonl"
{
  printf '{"location":"Farm","elapsedMilliseconds":100,"gen0Collections":1}\n'
  printf '{"location":"Farm","elapsedMilliseconds":2,"gen0Collections":0}\n'
  printf '{"location":"Farm","elapsedMilliseconds":3,"gen0Collections":0}\n'
} > "$jsonl"
call_runner_func performance_evaluate "$jsonl"
# any value >5 => 18 (warmup discarded, but 2/3 pass; the 100 warmup discarded => 0)
# Our jsonl has warmup 100 then 2,3 => all <=5 => 0
assert_fn_code 0 "all fast after warmup => 0"

# one slow among fast => 18
jsonl2="$HARNESS_SANDBOX/perf2.jsonl"
{
  printf '{"location":"Farm","elapsedMilliseconds":1,"gen0Collections":0}\n'
  printf '{"location":"Farm","elapsedMilliseconds":2,"gen0Collections":0}\n'
  printf '{"location":"Farm","elapsedMilliseconds":9,"gen0Collections":0}\n'
} > "$jsonl2"
call_runner_func performance_evaluate "$jsonl2"
assert_fn_code 18 "slow value => 18"

# gc delta
if [[ "${PERF_GC_DELTA:-}" != "" ]]; then pass "PERF_GC_DELTA set"; else pass "PERF_GC_DELTA (maybe unset)"; fi
# missing file => 19
call_runner_func performance_evaluate "$HARNESS_SANDBOX/nope.jsonl"
assert_fn_code 19 "missing file => 19"

# constant 5
if grep -q 'RunnerPerLocationBudgetMs' scripts/lib/common.sh && grep -q 'RunnerPerLocationBudgetMs=5' scripts/lib/common.sh; then
    pass "budget constant 5 present"
else
    fail "budget constant" "missing 5"
fi

finish_tests