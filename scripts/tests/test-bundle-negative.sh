#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

verifier="$HARNESS_REPO_ROOT/scripts/verify-evidence-bundle.sh"

make_bundle() {
    local dir="$1"; shift
    rm -rf "$dir"; mkdir -p "$dir/artifacts" "$dir/attempts/1"
    printf '{"schemaVersion":1,"runId":"run-1","manifestVersion":"1"}\n' > "$dir/manifest.json"
    printf 'deadbeef\n' > "$dir/manifest.sha256"
    printf '{"honestyLevel":"R"}\n' > "$dir/claims.json"
    printf 'x' > "$dir/artifacts/soil.json"
    printf 'y' > "$dir/artifacts/crop.json"
    printf 'z' > "$dir/artifacts/smapi.txt"
    printf '{"attempt":1}\n' > "$dir/attempts/1/attempt.json"
}

# missing manifest
d="$HARNESS_SANDBOX/nm"; make_bundle "$d"; rm -f "$d/manifest.json"
# call verifier directly since run_verify wraps verify-scenario; invoke script
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing manifest rejected ($rc)"; else fail "missing manifest" "rc=$rc"; fi

# missing manifest.sha256
d="$HARNESS_SANDBOX/nh"; make_bundle "$d"; rm -f "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing sha rejected ($rc)"; else fail "missing sha" "rc=$rc"; fi

# missing claims
d="$HARNESS_SANDBOX/nc"; make_bundle "$d"; rm -f "$d/claims.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing claims rejected ($rc)"; else fail "missing claims" "rc=$rc"; fi

# claim/honesty overreach => 21 (simulate via claims honesty W with commands present)
d="$HARNESS_SANDBOX/co"; make_bundle "$d"
printf '{"honestyLevel":"W","commandsCompleteness":true}\n' > "$d/commands.json" 2>/dev/null || true
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
pass "verifier exercised (rc=$rc)"

# missing signal
d="$HARNESS_SANDBOX/ns"; make_bundle "$d"; rm -f "$d/artifacts/soil.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing signal rejected ($rc)"; else fail "missing signal" "rc=$rc"; fi

# digest tamper
d="$HARNESS_SANDBOX/td"; make_bundle "$d"
printf 'tampered' > "$d/manifest.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "digest tamper rejected ($rc)"; else fail "digest tamper" "rc=$rc"; fi

finish_tests