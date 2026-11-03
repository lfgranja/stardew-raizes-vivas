#!/usr/bin/env bash
# scripts/tests/test-doc-links.sh — T125b
# Assert VERIFICATION_PROTOCOL.md §3 and the gate3 verification contract exist,
# required anchors present, and both link back to spec 010 FR-021.
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"

EXIT_OK=0
EXIT_POLICY=21

fail() { echo "FAIL: $*" >&2; exit "$EXIT_POLICY"; }

VP="$REPO_ROOT/VERIFICATION_PROTOCOL.md"
CONTRACT="$REPO_ROOT/specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md"
SPEC="$REPO_ROOT/specs/010-headless-verification-runner/spec.md"

[[ -f "$VP" ]] || fail "VERIFICATION_PROTOCOL.md missing"
[[ -f "$CONTRACT" ]] || fail "gate3-verification-contract.md missing"
[[ -f "$SPEC" ]] || fail "spec.md missing"

grep -q "FR-021" "$VP" || fail "VERIFICATION_PROTOCOL.md missing FR-021 reference"
grep -q "FR-021" "$CONTRACT" || fail "gate3 contract missing FR-021 reference"

# required anchors (best-effort)
grep -q "§3\|Section 3\|## 3" "$VP" || fail "VERIFICATION_PROTOCOL.md missing §3 anchor"

echo "PASS: doc links OK"
exit "$EXIT_OK"
EOF
chmod +x scripts/tests/test-doc-links.sh
bash scripts/tests/test-doc-links.sh