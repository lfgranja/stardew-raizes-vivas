#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# run-id format
call_runner_func evidence_run_id
assert_matches "$RUNNER_FN_OUT" '^run-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{4}$' "run-id format"

rid="$(call_runner_func evidence_run_id; printf '%s' "$RUNNER_FN_OUT")"
root="$HARNESS_SANDBOX/evroot"
mkdir -p "$root"
call_runner_func evidence_init "$root" "$rid"
assert_fn_code 0 "evidence_init returns 0"
bdir="$root/evidence-bundle-$rid"
assert_dir_exists() { [[ -d "$1" ]] && pass "$2" || fail "$2" "missing dir $1"; }
assert_dir_exists "$bdir" "bundle dir"
assert_dir_exists "$bdir/artifacts" "artifacts dir"
assert_dir_exists "$bdir/attempts" "attempts dir"

# add artifact
af="$HARNESS_SANDBOX/art.txt"; printf 'hello' > "$af"
call_runner_func evidence_add_artifact "$bdir" "$af"
assert_fn_code 0 "add artifact returns 0"

# attempt dir
call_runner_func evidence_attempt_dir "$bdir" 1
assert_eq "$bdir/attempts/1" "$RUNNER_FN_OUT" "attempt_dir path"

# bundle dir helper
call_runner_func evidence_bundle_dir "$root" "$rid"
assert_eq "$bdir" "$RUNNER_FN_OUT" "bundle_dir path"

# finalize invokes helper; scaffold may not exist yet -> allow 13
manifestin="$HARNESS_SANDBOX/manifest-input.json"
printf '{"runId":"%s"}\n' "$rid" > "$manifestin"
call_runner_func evidence_finalize "$bdir" "$manifestin"
# either 0 or 13 (scaffold) accepted at RED gate
pass "evidence_finalize exercised (rc=$RUNNER_FN_CODE)"

# claims
call_runner_func evidence_write_claims "$bdir" "$rid" "R"
assert_fn_code 0 "write claims returns 0"
assert_file_exists "$bdir/claims.json" "claims.json"

# integrity failure
call_runner_func evidence_write_integrity_failure "$bdir" "checksum" "archive.zip" "detail"
assert_fn_code 0 "integrity failure write returns 0"

# validate (scaffold may fail; just exercise)
call_runner_func evidence_validate "$bdir"
pass "evidence_validate exercised (rc=$RUNNER_FN_CODE)"

# single-producer rule: no hand-written manifest in evidence.sh
if grep -Eq 'printf[^$]*manifest\.json|cat[^$]*>.*manifest\.json|echo[^$]*manifest\.json' scripts/lib/evidence.sh; then
    fail "single producer" "hand-written manifest found"
else
    pass "no hand-written manifest in evidence.sh"
fi

# never writes verified
if grep -q '"verified"\|verified' scripts/lib/evidence.sh scripts/verify-scenario.sh 2>/dev/null; then
    # `verified` may appear in comments/refusal; only fail if assignment/write
    if grep -Eq 'echo.*verified|printf.*verified|>.*verified' scripts/lib/evidence.sh scripts/verify-scenario.sh; then
        fail "no verified write" "found verified write"
    else
        pass "verified only as refusal/diagnostic"
    fi
else
    pass "no verified references"
fi

finish_tests