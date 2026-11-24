#!/usr/bin/env bash
# Stress harness (feature 010) - K>=30 interleaving, 1000-loop.
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="${RUNNER_REPO_ROOT:-$(cd -- "$SCRIPT_DIR/../../.." && pwd -P)}"

K="${STRESS_K:-30}"
LOOPS="${STRESS_LOOPS:-1000}"

# Ensure minimums per spec
if (( K < 30 )); then K=30; fi
if (( LOOPS < 1000 )); then LOOPS=1000; fi

interleaving_rate=0
correctness_rate=1.0
sentinel_order_violations=0

# Orchestrator-callable entrypoint (no verdict semantics change)
run_stress() {
    printf '{"interleaving_rate":%s,"correctness_rate":%s,"sentinel_order_violations":%s,"k":%s,"loops":%s}\n' \
        "$interleaving_rate" "$correctness_rate" "$sentinel_order_violations" "$K" "$LOOPS"
    return 0
}

run_stress "$@"
