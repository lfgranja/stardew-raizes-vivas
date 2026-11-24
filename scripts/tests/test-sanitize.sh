#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# Bash sanitizer parity: sanitize_path accepts/rejects per vectors.
vectors="scripts/tests/fixtures/sanitize-vectors.tsv"
assert_file_exists "$vectors" "vectors file exists"

pass_count=0
fail_count=0
while IFS=$'\t' read -r input expected category; do
    [[ -z "$input" || "$input" == \#* ]] && continue
    # skip comment lines
    [[ "$input" == \#* ]] && continue
    call_runner_func sanitize_path "$input"
    rc="$RUNNER_FN_CODE"
    if [[ "$expected" == "accept" ]]; then
        if (( rc == 0 )); then pass "sanitize accept: $input"; else fail "sanitize accept: $input" "rc=$rc"; fi
    else
        if (( rc != 0 )); then pass "sanitize reject: $input"; else fail "sanitize reject: $input" "rc=$rc"; fi
    fi
done < "$vectors"

# C# parity: SanitizerPropertyTests exists
assert_file_exists "LivingRoots.Tests/Verification/SanitizerPropertyTests.cs" "C# sanitizer tests exist"

# sanitize.sh referenced by ModDataService parity claim
assert_contains_file "scripts/lib/sanitize.sh" "sanitize_path" "sanitize_path defined"

finish_tests
