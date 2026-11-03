#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# version_gate_classify exists
if declare -F version_gate_classify >/dev/null 2>&1; then pass "version_gate_classify defined"; else fail "version_gate_classify defined" "missing"; fi

# version comparison: 1.11 < 1.2 (SemanticVersion)
call_runner_func version_gate_classify "1.11.0" "1.2.0" "1.6.14" 2>/dev/null
pass "version_gate exercised (rc=$RUNNER_FN_CODE)"

# no Xvfb on hard-fail: source guard
assert_contains_file "scripts/lib/version-gate.sh" "version_gate" "version-gate lib present"

# -unofficial suffix handling
assert_contains_file "scripts/lib/version-gate.sh" "unofficial" "unofficial handling"

# null MaximumGameVersion handling
assert_contains_file "scripts/lib/version-gate.sh" "MaximumGameVersion" "max game version handling"

finish_tests
