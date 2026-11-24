#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

assert_file_exists "scripts/lib/reporters.sh" "reporters lib exists"

assert_contains_file "scripts/lib/reporters.sh" "reporter_junit" "junit reporter"
assert_contains_file "scripts/lib/reporters.sh" "reporter_allure" "allure reporter"
assert_contains_file "scripts/lib/reporters.sh" "reporter_emit" "emit reporter"
assert_contains_file "scripts/lib/reporters.sh" "testsuite" "junit testsuite element"

finish_tests
