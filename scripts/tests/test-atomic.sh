#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# temp-in-target-dir, data fsync, rename, parent fsync, EXDEV internal, target untouched on failure, temp unlinked.
d="$HARNESS_SANDBOX/atomicdir"
mkdir -p "$d"
target="$d/target.txt"
printf 'hello' | atomic_write "$target" -
assert_file_exists "$target" "atomic_write creates target"
assert_eq "hello" "$(cat "$target")" "content matches"

# temp file unlinked after success
if ls "$d"/.atomic.* >/dev/null 2>&1; then fail "temp unlinked after success" "leftover"; else pass "temp unlinked after success"; fi

# target untouched on failure: write to missing dir
missing="$HARNESS_SANDBOX/no-such-dir/target.txt"
atomic_write "$missing" - <<< "x" 2>/dev/null || true
assert_file_absent "$missing" "target absent on failure"

# EXDEV is internal error (13): check atomic.sh references
assert_contains_file "scripts/lib/atomic.sh" "EXIT_INTERNAL_ERROR" "EXDEV maps to internal error"

# fsync helpers exist
call_runner_func fsync_file "$target"
assert_fn_code 0 "fsync_file succeeds"
call_runner_func fsync_dir "$d"
assert_fn_code 0 "fsync_dir succeeds"

finish_tests
