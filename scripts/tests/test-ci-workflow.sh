#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

wf=".github/workflows/verify-scenario.yml"
assert_file_exists "$wf" "workflow exists"

# PR #765 moved to GitHub-hosted infra (best-effort, builddeps-gated). Assertions
# match the current workflow contract; do not require self-hosted.
assert_contains_file "$wf" "ubuntu-latest" "github-hosted runner"
assert_contains_file "$wf" "BUILDDEPS_TOKEN" "builddeps token"
assert_contains_file "$wf" "permissions:" "permissions block"
assert_contains_file "$wf" "contents: read" "least-privilege contents read"
assert_contains_file "$wf" "verify-scenario.sh" "invokes runner"
assert_contains_file "$wf" "--ci" "ci flag"

# Best-effort on public hosted infra: guarded by builddeps presence and/or clearly
# marked non-blocking; must not silently claim full success without deps.
if grep -Eq 'HAS_BUILDDEPS|BUILDDEPS_TOKEN|continue-on-error: true|::notice::|::warning::' "$wf"; then
  pass "hosted execution gated or explicitly best-effort"
else
  fail "hosted execution gated or explicitly best-effort" "missing builddeps gate/non-blocking notice"
fi

finish_tests
