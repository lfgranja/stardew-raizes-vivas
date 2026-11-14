#!/usr/bin/env bash
# Deterministic aggregate CI gate (policy-aligned, no game launch/assemblies required).
# Runs local-feasible checks that must stay green for SDD/governance alignment.
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
cd "$REPO_ROOT"

say() { printf '[aggregate-gate] %s\n' "$1"; }
fail() { printf '[aggregate-gate FAIL] %s\n' "$1" >&2; exit "${2:-1}"; }

say "step 1: sdd-gate"
python3 scripts/ci/sdd-gate.py || fail "step 1 failed: sdd-gate" 11

say "step 2: policy drift (hub manifest vs satellite manifest)"
python3 scripts/ci/check_policy_drift.py docs/governance/hub-policy-manifest.json docs/governance/satellite-manifest.json \
  || fail "step 2 failed: policy drift" 12

say "step 3: protected paths"
./scripts/check-protected-paths.sh || fail "step 3 failed: protected paths" 13

say "step 4: docs markers"
./scripts/verify-scenario.sh --check-docs || fail "step 4 failed: docs markers" 14

say "ALL STEPS PASSED"
