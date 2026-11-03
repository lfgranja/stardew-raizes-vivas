#!/usr/bin/env bash
#
# scripts/tests/test-runall.sh — discovery contract for scripts/tests/run-all.sh.
#
# Scaffolded from scripts/tests/lib/harness.sh plus the header/teardown
# conventions of scripts/tests/test-cli.sh (T009). Initially asserts the
# existing run-all.sh behavior so the later discovery change (T061c) has a
# failing-first test.
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# Baseline assertion: run-all.sh exists and is executable.
assert_file_exists "run-all.sh exists" "scripts/tests/run-all.sh"
assert_file_mode "run-all.sh is executable" "scripts/tests/run-all.sh" "111"

finish_tests