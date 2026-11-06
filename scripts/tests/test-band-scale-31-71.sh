#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# T127/T130 guard: Cornell CSHA 31/71 is the governing scale (spec 014 FR-013).
# Fail on live 34/67 band-scale references outside migration history.
# Allowed migration/history paths: evidence bundles, gate3/red-phase records,
# spec 010 contracts/fixtures/data-model (historical runner spec), spec 014
# migration notes, and this guard itself.
cd "$(git rev-parse --show-toplevel)"

hits="$(
  git grep -nE 'PoorModerateBandEdge\s*=\s*34f|ModerateHealthyBandEdge\s*=\s*67f|\[0,\s*34\)|\[34,\s*67\)|POOR_POSITIVE_LT_34' -- \
    ':(exclude)scripts/tests/test-band-scale-31-71.sh' \
    ':(exclude)specs/010-headless-verification-runner/contracts/*' \
    ':(exclude)specs/010-headless-verification-runner/fixtures/*' \
    ':(exclude)specs/010-headless-verification-runner/data-model.md' \
    ':(exclude)specs/010-headless-verification-runner/tasks.md' \
    ':(exclude)specs/014-soil-health-crop-quality/red-phase-convergence.md' \
    ':(exclude)specs/014-soil-health-crop-quality/gate3-evidence.md' \
    ':(exclude)specs/014-soil-health-crop-quality/tasks.md' \
    ':(exclude)**/evidence-bundle-*' \
    ':(exclude)**/gate3-evidence/*' \
    2>/dev/null || true
)"

if [[ -n "$hits" ]]; then
  fail "34/67 band-scale references found outside migration history" "$hits"
else
  pass "No live 34/67 band-scale references outside migration history"
fi

finish_tests
