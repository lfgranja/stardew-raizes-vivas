#!/usr/bin/env bash
# scripts/tests/test-red-gate-evidence.sh
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

REPO_ROOT="${RUNNER_REPO_ROOT}"
SPEC_DIR="$REPO_ROOT/specs/010-headless-verification-runner"

DEVIATION_PHRASES=(
    "Implementation pre-existed"
    "genuine RED evidence cannot be reconstructed"
)

log_has_red_or_deviation() {
    local file="$1"
    if grep -Eq 'not ok|FAIL' "$file"; then
        return 0
    fi
    local phrase
    for phrase in "${DEVIATION_PHRASES[@]}"; do
        if grep -Fqi -- "$phrase" "$file"; then
            return 0
        fi
    done
    return 1
}

for phase in 2 3 4 5; do
    assert_file_exists "$SPEC_DIR/red-gate-phase-$phase.log" "red-gate-phase-$phase.log exists"
done

shopt -s nullglob
logs=( "$SPEC_DIR"/red-gate-phase-*.log )
shopt -u nullglob
if (( ${#logs[@]} == 0 )); then
    fail "red-gate phase logs present" "no red-gate-phase-*.log found"
else
    for log in "${logs[@]}"; do
        base="$(basename -- "$log")"
        if log_has_red_or_deviation "$log"; then
            pass "$base carries RED evidence or a documented deviation"
        else
            fail "$base carries RED evidence or a documented deviation" "no 'not ok'/'FAIL' line and no deviation statement in $log"
        fi
    done
fi

tasks_file="$SPEC_DIR/tasks.md"
assert_file_exists "$tasks_file" "tasks.md exists"
t130_block="$(awk '/T130/{f=1} f{print} /T131/{if(f) exit}' "$tasks_file" 2>/dev/null || true)"
t130_ok=0
for phrase in "${DEVIATION_PHRASES[@]}" "deviation" "Deviation"; do
    if printf '%s\n' "$t130_block" | grep -Fqi -- "$phrase"; then
        t130_ok=1
        break
    fi
done
if (( t130_ok == 1 )); then
    pass "T130 reflects the documented deviation"
else
    fail "T130 reflects the documented deviation" "no deviation statement found in T130 block of $tasks_file"
fi

finish_tests
