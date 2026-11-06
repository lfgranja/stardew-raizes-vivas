#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# T127/T130 guard: Cornell CSHA 31/71 is the governing scale (spec 014 FR-013).
# Fail on live 34/67 band-scale references outside migration history.
# Allowed migration/history paths: evidence bundles, gate3/red-phase records,
# historical specs/assessments/briefings/bugs/research, and this guard itself.
# POOR_POSITIVE_LT_34 is allowed only as a migration alias alongside LT_31.
cd "$(git rev-parse --show-toplevel)"

hits="$(
  git grep -nE 'PoorModerateBandEdge\s*=\s*34f|ModerateHealthyBandEdge\s*=\s*67f|\[0,\s*34\)|\[34,\s*67\)|POOR_POSITIVE_LT_34' -- \
    ':(exclude)scripts/tests/test-band-scale-31-71.sh' \
    ':(exclude)specs/010-headless-verification-runner/contracts/*' \
    ':(exclude)specs/010-headless-verification-runner/fixtures/*' \
    ':(exclude)specs/010-headless-verification-runner/data-model.md' \
    ':(exclude)specs/010-headless-verification-runner/tasks.md' \
    ':(exclude)specs/010-headless-verification-runner/spec.md' \
    ':(exclude)specs/014-soil-health-crop-quality/red-phase-convergence.md' \
    ':(exclude)specs/014-soil-health-crop-quality/gate3-evidence.md' \
    ':(exclude)specs/014-soil-health-crop-quality/tasks.md' \
    ':(exclude)**/evidence-bundle-*' \
    ':(exclude)**/gate3-evidence/*' \
    ':(exclude).specify/assessments/*' \
    ':(exclude).specify/bugs/*' \
    ':(exclude).specify/research/*' \
    ':(exclude).specify/specs/*' \
    ':(exclude)specs/001-soil-health-visualization/*' \
    ':(exclude)specs/004-playable-compost-loop/*' \
    ':(exclude)specs/005-soil-health-to-crop-growth/*' \
    ':(exclude)specs/009-end-to-end-game-verification/*' \
    ':(exclude)specs/future-roadmap/*' \
    ':(exclude)docs/design-system/REALITY-CHECK-EVALUATION.md' \
    2>/dev/null | grep -v 'POOR_POSITIVE_LT_31.*POOR_POSITIVE_LT_34\|POOR_POSITIVE_LT_34.*POOR_POSITIVE_LT_31' || true
)"

if [[ -n "$hits" ]]; then
  fail "34/67 band-scale references found outside migration history" "$hits"
else
  pass "No live 34/67 band-scale references outside migration history"
fi

finish_tests
