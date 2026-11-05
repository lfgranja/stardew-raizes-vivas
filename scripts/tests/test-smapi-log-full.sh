#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

log="$HARNESS_SANDBOX/smapi-full.log"
{
    printf '[12:00:00 INFO  SMAPI] Loaded 3 mods:\n'
    printf '[12:00:00 INFO  SMAPI]    LivingRoots 0.4.0 by lfgranja\n'
    printf '[12:00:00 TRACE LivingRoots] Soil growth modifier elapsed: 1 ms (location=Farm).\n'
    printf '[12:00:00 DEBUG game] DebugOutput: File does not exist\n'
    printf '[12:00:00 WARN  SMAPI] Something warning\n'
    printf '[12:00:00 ERROR game] boom\n'
    printf '[12:00:00 INFO  SMAPI] DayEnding\n'
    printf '[12:00:00 TRACE LivingRoots] Soil growth modifier skipped: not main player.\n'
} > "$log"

root="$HARNESS_SANDBOX/fullroot"
rid="run-smapi-log-full"
bdir="$root/evidence-bundle-$rid"
mkdir -p "$bdir/artifacts"

out="$bdir/artifacts/smapi-log.txt"
call_runner_func signals_smapi_log_excerpt "$log" "$out"
assert_fn_code 0 "signals_smapi_log_excerpt returns 0"
assert_file_exists "$out" "artifacts/smapi-log.txt exists"

src_hash="$(sha256sum "$log" | awk '{print $1}')"
dst_hash="$(sha256sum "$out" | awk '{print $1}')"
assert_eq "$src_hash" "$dst_hash" "smapi-log.txt byte-identical to source (sha256)"
if cmp -s "$log" "$out"; then
    pass "cmp: source and artifact are byte-identical"
else
    fail "cmp: source and artifact" "differ"
fi
assert_contains_file "$out" "File does not exist" "full log retains the save-load failure line"

full_lines="$(wc -l < "$log")"
filtered_lines="$(grep -Ec 'elapsed|DayEnding|not main player' "$log" || true)"
if (( full_lines > filtered_lines )); then
    pass "old three-pattern grep produces fewer lines ($filtered_lines < $full_lines)"
else
    fail "old grep line count" "expected full ($full_lines) > filtered ($filtered_lines)"
fi

export RUNNER_SMAPI_CAPTURE=excerpt
out2="$HARNESS_SANDBOX/smapi-excerpt.txt"
call_runner_func signals_smapi_log_excerpt "$log" "$out2"
assert_fn_code 0 "excerpt-mode call returns 0"
dst2_hash="$(sha256sum "$out2" | awk '{print $1}')"
assert_eq "$src_hash" "$dst2_hash" "RUNNER_SMAPI_CAPTURE=excerpt (unimplemented) still full copy"
unset RUNNER_SMAPI_CAPTURE

out3="$HARNESS_SANDBOX/smapi-default.txt"
call_runner_func signals_smapi_log_excerpt "$log" "$out3"
assert_fn_code 0 "default capture returns 0"
dst3_hash="$(sha256sum "$out3" | awk '{print $1}')"
assert_eq "$src_hash" "$dst3_hash" "default (unset) capture is full"

finish_tests
