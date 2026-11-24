#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

run_verify --ci --help >/dev/null 2>&1 || true
if [[ "$RUN_VERIFY_CODE" == "0" ]]; then pass "--ci --help exits 0"; else fail "--ci --help exits 0" "code=$RUN_VERIFY_CODE"; fi

assert_contains_file "scripts/lib/ci.sh" "CI_MODE" "ci mode defined"
assert_contains_file "scripts/lib/ci.sh" "GITHUB_ACTIONS" "ci env detection"
assert_contains_file "scripts/verify-scenario.sh" "--ci" "--ci flag wired"
assert_contains_file "scripts/lib/ci.sh" "noninteractive" "non-interactive handling"

finish_tests
