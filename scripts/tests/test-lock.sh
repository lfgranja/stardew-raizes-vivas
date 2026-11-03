#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

for fn in lock_acquire lock_release claim_acquire claim_release _claim_root_resolve _claim_root_validate_or_create; do
    if declare -F "$fn" >/dev/null 2>&1; then pass "function $fn defined"; else fail "function $fn defined" "missing"; fi
done

assert_contains_file "scripts/lib/concurrency.sh" "noclobber" "noclobber (O_EXCL-like) used"
assert_contains_file "specs/010-headless-verification-runner/CONCURRENCY.md" "game-cache" "global order game-cache"
assert_contains_file "specs/010-headless-verification-runner/CONCURRENCY.md" "build-output" "global order build-output"
assert_contains_file "specs/010-headless-verification-runner/CONCURRENCY.md" "shared-doc" "global order shared-doc"
assert_contains_file "scripts/lib/concurrency.sh" "RUNNER_CLAIM_ROOT" "claim root var referenced"
assert_contains_file "scripts/lib/concurrency.sh" "backoff" "backoff present"
assert_contains_file "scripts/lib/concurrency.sh" "token" "token referenced"

finish_tests
