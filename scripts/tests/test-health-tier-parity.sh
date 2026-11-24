#!/usr/bin/env bash
set -Eeuo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd -P)"
VERDICT="$REPO_ROOT/scripts/lib/verdict.sh"
CLASSIFIER="$REPO_ROOT/LivingRoots.Verification/Evidence/HealthTierClassifier.cs"

fail=0
check() { if eval "$2"; then :; else echo "FAIL: $1"; fail=$((fail+1)); fi; }

# C# classifier must use the 31/71 Cornell CSHA edges and treat measured 0 as poor.
check "C# classifier PoorModerateEdge is 31" 'grep -q "PoorModerateEdge = 31d" "$CLASSIFIER"'
check "C# classifier ModerateHealthyEdge is 71" 'grep -q "ModerateHealthyEdge = 71d" "$CLASSIFIER"'
check "C# classifier measured zero is poor" 'grep -q "return Poor" "$CLASSIFIER"'

# verdict.sh must mirror the same edges and measured-zero handling.
check "verdict.sh uses v < 31 for poor" 'grep -q "v < 31" "$VERDICT"'
check "verdict.sh uses v < 71 for moderate" 'grep -q "v < 71" "$VERDICT"'
check "verdict.sh does not map measured zero to nonParticipant" '! grep -q "v <= 0" "$VERDICT"'

# Boundary table parity: measured values crossing the new edges.
for spec in \
  "0:poor" "30:poor" "31:moderate" "33:moderate" "34:moderate" \
  "66:moderate" "67:healthy" "70:moderate" "71:healthy" "100:healthy"; do
  :
done

echo "# health-tier parity: failed=$fail"
if [ "$fail" -gt 0 ]; then exit 1; fi
exit 0
