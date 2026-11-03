#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

wf=".github/workflows/verify-scenario.yml"
assert_file_exists "$wf" "workflow exists"

assert_contains_file "$wf" "self-hosted" "self-hosted runner"
assert_contains_file "$wf" "BUILDDEPS_TOKEN" "builddeps token"
assert_contains_file "$wf" "permissions:" "permissions block"
assert_contains_file "$wf" "contents: read" "least-privilege contents read"
assert_contains_file "$wf" "verify-scenario.sh" "invokes runner"
assert_contains_file "$wf" "--ci" "ci flag"

# dormant on public hosted: workflow should be disabled or guarded
if grep -q "if:.*self-hosted\|runs-on:.*self-hosted" "$wf"; then pass "self-hosted gated"; else fail "self-hosted gated" "not gated"; fi

finish_tests
